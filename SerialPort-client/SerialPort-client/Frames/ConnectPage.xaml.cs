using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Data.SqlServerCe;
using SerialPort_client.Sources;

namespace SerialPort_client.Frames
{
    /// <summary>
    /// Interaction logic for ConnectPage.xaml
    /// </summary>
    public partial class ConnectPage : Page
    {
        // tuple to store uid and index
        public struct fileName
        {
            public int Uid;
            public int Index;

            public fileName(int uid, int index)
            {
                this.Uid = uid;
                this.Index = index;
            }
        }

        private SerialPort port = null;
        private int deviceStatus = 0;
        private List<fileName> newFileNames;
        private string allData;
        private int lastCommand;
        private int lastParam;
        private int totalNumOfFiles = 0;
        private int curNumOfFiles = 0;
        private SerialDataReceivedEventHandler onByteReceived, onDataReceived;

        private string conString = "Data Source=C:\\Users\\Kaizhi\\exerciseData.sdf;Password=admin;Persist Security Info=True";

        public ConnectPage()
        {
            InitializeComponent();

            this.prefillComboBox();
            this.onByteReceived = new SerialDataReceivedEventHandler(port_ByteReceived);
            this.onDataReceived = new SerialDataReceivedEventHandler(port_DataReceived);
        }

        // fill in comboboxes with options
        private void prefillComboBox()
        {
            List<User> users = new List<User>();

            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();
                // Read in all values in the table.
                using (SqlCeCommand com = new SqlCeCommand("SELECT id, name, gender, age, height, weight FROM Users", con))
                {
                    SqlCeDataReader reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string gender = reader.GetString(2);
                        int age = reader.GetInt32(3);
                        int height = reader.GetInt32(4);
                        int weight = reader.GetInt32(5);

                        User user = new User(id, name, gender, age, height, weight);
                        users.Add(user);
                    }
                }
            }

            this.cmBoxUsers.ItemsSource = users;
            if (users.Count > 0)
            {
                this.cmBoxUsers.SelectedItem = users.Last();
            }
        }

        private void makeConnection()
        {
            //this.btnStart.IsEnabled = false;
            this.txtBlkStatus.Text = "Connecting";
            this.txtBlkStatus.Foreground = Brushes.Orange;

            deviceStatus = checkPedometerConnection();

            // TODO: change to data binding and style
            if (deviceStatus != 0)
            {
                this.showConnected();

                if (this.deviceStatus == 'n')
                {
                    this.initializeDevice();
                }
            }
            else
            {
                this.showDisconnected();

                MessageBox.Show("No device is detected.");
            }
        }

        private void showConnected()
        {
            this.btnAddUser.IsEnabled = true;
            this.btnSync.IsEnabled = true;
            this.txtBlkStatus.Text = "Connected through " + port.PortName;
            this.txtBlkStatus.Foreground = Brushes.Green;
            this.btnStart.Style = this.Resources["DisconnectButton"] as Style;
        }

        private void showDisconnected()
        {
            this.btnAddUser.IsEnabled = false;
            this.btnSync.IsEnabled = false;
            this.txtBlkStatus.Text = "Not connected";
            this.txtBlkStatus.Foreground = Brushes.Red;
            this.btnStart.Style = this.Resources["ConnectButton"] as Style;
        }

        private void processData(int userId, int sessionIndex)
        {
            string fileName = userId.ToString() + "_" + sessionIndex.ToString() + ".txt";
            string line;
            int timestamp = 0, x_acc = 0, y_acc = 0, z_acc = 0;
            List<Sample> samples = new List<Sample>();
            System.IO.StreamReader dataFile = new System.IO.StreamReader(fileName);

            while ((line = dataFile.ReadLine()) != null)
            {
                string[] parts = line.Split(',');

                if (parts.Length == 4)
                {
                    try
                    {
                        timestamp = Convert.ToInt32(parts[0]);
                        x_acc = Convert.ToInt32(parts[1]);
                        y_acc = Convert.ToInt32(parts[2]);
                        z_acc = Convert.ToInt32(parts[3]);
                    }
                    catch { }
                }
                else
                {
                    List<string> newParts = new List<string>();
                    foreach (string part in parts)
                    {
                        if (part.Contains(' '))
                        {
                            string[] splitedParts = part.Split(' ');
                            foreach (string splitedPart in splitedParts)
                            {
                                newParts.Add(splitedPart);
                            }
                        }
                        else
                        {
                            newParts.Add(part);
                        }
                    }

                    if (newParts.Count == 4)
                    {
                        timestamp = Convert.ToInt32(newParts[0]);
                        x_acc = Convert.ToInt32(newParts[1]);
                        y_acc = Convert.ToInt32(newParts[2]);
                        z_acc = Convert.ToInt32(newParts[3]); 
                    }
                }

                Sample sample = new Sample(timestamp, x_acc, y_acc, z_acc);
                samples.Add(sample);
            }

            samples = this.lowPassFilter(samples, 10);
            List<double> thresholds = this.calThresholds(samples, 40);
            List<int> stepTimes = this.countSteps(samples, thresholds);
            this.recordSteps(userId, sessionIndex, stepTimes);
        }

        private double calDistanceFromSteps(int userHeight, List<int> stepTimes)
        {
            double height = (double) userHeight;
            int startTime = stepTimes[0];
            int curFrame = 0;
            int countInFrame = 0;
            double distance = 0;

            foreach (int stepTime in stepTimes)
            {
                if ((stepTime - startTime) / 2000 == curFrame)
                {
                    countInFrame += 1;
                }
                else
                {
                    distance += calDistancePerFrame(height, countInFrame);
                    countInFrame = 0;
                    curFrame = (stepTime - startTime) / 2000;
                }
            }
            if (countInFrame != 0)
            {
                distance += calDistancePerFrame(height, countInFrame);
            }

            return distance / 100;
        }

        private double calDistancePerFrame(double height, int stepsPerFrame)
        {
            double distance = 0;
            switch (stepsPerFrame)
            {
                case 0:
                    distance += 0;
                    break;
                case 1:
                    distance += stepsPerFrame * height / 4;
                    break;
                case 2:
                    distance += stepsPerFrame * height / 4;
                    break;
                case 3:
                    distance += stepsPerFrame * height / 3;
                    break;
                case 4:
                    distance += stepsPerFrame * height / 2;
                    break;
                case 5:
                    distance += stepsPerFrame * height / 1.2;
                    break;
                case 6:
                    distance += stepsPerFrame * height;
                    break;
                case 7:
                    distance += stepsPerFrame * height * 1.2;
                    break;
                default:
                    distance += stepsPerFrame * height * 1.2;
                    break;
            }

            return distance;
        }

        private double calCalorieFromSteps(int userHeight, int userWeight, List<int> stepTimes)
        {
            double calorie = 0;
            double height = (double)userHeight;
            double weight = (double)userWeight;
            int startTime = stepTimes[0];
            int curFrame = 0;
            int countInFrame = 0;
            double speed;

            foreach (int stepTime in stepTimes)
            {
                if ((stepTime - startTime) / 2000 == curFrame)
                {
                    countInFrame += 1;
                }
                else
                {
                    speed = calDistancePerFrame(height, countInFrame);
                    calorie += speed * weight / 40000;
                    countInFrame = 0;
                    curFrame = (stepTime - startTime) / 2000;
                }
            }

            if (countInFrame != 0)
            {
                speed = calDistancePerFrame(height, countInFrame);
                calorie += speed * weight / 40000;
            }

            return calorie;
        }

        private void recordSteps(int userId, int sessionIndex, List<int> stepTimes)
        {
            // error handling, when no step was detected
            if (stepTimes.Count == 0)
            {
                return;
            }
            int startTime = stepTimes[0];
            int curMin = 1;
            int countInMin = 0;
            double distance = 0;
            double calorie = 0;
            List<int> stepTimesPerMin = new List<int>();

            User user = getUserByID(userId);

            foreach (int stepTime in stepTimes)
            {
                if ((stepTime - startTime) / 60000 == (curMin - 1))
                {
                    countInMin += 1;
                    stepTimesPerMin.Add(stepTime);
                }
                else
                {
                    distance = this.calDistanceFromSteps(user.Height, stepTimesPerMin);
                    calorie = this.calCalorieFromSteps(user.Height, user.Weight, stepTimesPerMin);
                    this.saveRecordToDB(userId, sessionIndex, curMin, countInMin, distance, calorie);
                    curMin = (stepTime - startTime) / 60000 + 1;
                    countInMin = 0;
                    stepTimesPerMin.Clear();
                }
            }
            if (countInMin != 0)
            {
                distance = this.calDistanceFromSteps(user.Height, stepTimesPerMin);
                calorie = this.calCalorieFromSteps(user.Height, user.Weight, stepTimesPerMin);
                this.saveRecordToDB(userId, sessionIndex, curMin, countInMin, distance, calorie);
            }
        }

        private User getUserByID(int userId)
        {
            User user = null;
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();
                // Read in all values in the table.
                using (SqlCeCommand com = new SqlCeCommand("SELECT id,name,gender,age,height,weight FROM Users WHERE id = @id", con))
                {
                    com.Parameters.AddWithValue("@id", userId);
                    SqlCeDataReader reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string gender = reader.GetString(2);
                        int age = reader.GetInt32(3);
                        int height = reader.GetInt32(4);
                        int weight = reader.GetInt32(5);

                        user = new User(id, name, gender, age, height, weight);
                    }
                }
            }

            return user;
        }

        private void saveRecordToDB(int userId, int sessionIndex, int min, int stepCount, double distance, double calorie)
        {
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();

                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand com = new SqlCeCommand("INSERT INTO History (uid, date, min, steps, distance, calories, hid) VALUES (@uid, @date, @min, @steps, @distance, @calories, @hid)", con))
                {
                    com.Parameters.AddWithValue("@uid", userId);
                    com.Parameters.AddWithValue("@date", DateTime.Today);
                    com.Parameters.AddWithValue("@min", min);
                    com.Parameters.AddWithValue("@steps", stepCount);
                    com.Parameters.AddWithValue("@distance", distance);
                    com.Parameters.AddWithValue("@calories", calorie);
                    com.Parameters.AddWithValue("@hid", sessionIndex);
                    com.ExecuteNonQuery();
                }
            }
        }

        private List<Sample> lowPassFilter(List<Sample> rawSamples, int filterLength)
        {
            double average;
            List<Sample> filteredSamples = new List<Sample>();
            for (int index = (filterLength / 2 - 1); index < (rawSamples.Count - (filterLength / 2 + 1)); ++index)
            {
                average = 0;

                for (int i = (index - (filterLength / 2 - 1)); i < (index + (filterLength / 2 + 1)); ++i)
                {
                    average += rawSamples[i].rootSumSquare;
                }

                average = average / filterLength;

                Sample filteredSample = new Sample(rawSamples[index].TimeStamp , average);
                filteredSamples.Add(filteredSample);
            }

            return filteredSamples;
        }

        private List<double> calThresholds(List<Sample> filteredSamples, int windowSize)
        {
            List<double> thresholds = new List<double>();
            double max = double.MinValue, min = double.MaxValue;
            int index = 0;

            foreach (Sample sample in filteredSamples)
            {
                if (sample.rootSumSquare < min)
                {
                    min = sample.rootSumSquare;
                }

                if (sample.rootSumSquare > max)
                {
                    max = sample.rootSumSquare;
                }

                if (index >= windowSize - 1)
                {
                    if ((max - min) > 1.5)
                    {
                        thresholds.Add((max + min) / 2);
                    }
                    else
                    {
                        thresholds.Add(double.MinValue);
                    }
                    index = 0;
                    min = double.MaxValue;
                    max = double.MinValue;
                }
                else
                {
                    index += 1;
                }
            }

            if (index != 1)
            {
                thresholds.Add((max + min) / 2);
            }

            return thresholds;
        }

        private List<int> countSteps(List<Sample> samples, List<double> thresholds)
        {
            List<int> stepTimes = new List<int>();
            int index = 0, lastStepTime = 0;
            bool isPrevLarger = false;

            foreach (Sample sample in samples)
            {
                if (sample.rootSumSquare < thresholds[index / 50] 
                 && isPrevLarger
                 && (sample.TimeStamp - lastStepTime) > 150)
                {
                    stepTimes.Add(sample.TimeStamp);
                    lastStepTime = sample.TimeStamp;
                }

                isPrevLarger = sample.rootSumSquare > thresholds[index / 50];

                index += 1;
            }

            return stepTimes;
        }

        private void sendByte(byte command)
        {
            byte[] msg = new byte[1];

            msg[0] = command;

            if (null != port && !port.IsOpen)
            {
                port.Open();
            }

            port.Write(msg, 0, 1);
        }

        private void initializeDevice()
        {
            this.lastCommand = 'i';
            this.sendByte(Convert.ToByte('i'));
        }

        // send a request to every available COM port
        private int checkPedometerConnection()
        {
            string[] portNames = SerialPort.GetPortNames();

            foreach (string portName in portNames)
            {
                port = new SerialPort(portName, 115200);
                port.DtrEnable = true;
                port.RtsEnable = true;              // set flags to be true to enable receiving

                try
                {
                    if (!port.IsOpen)
                    {
                        port.Open();
                    }

                    if (port.IsOpen)
                    {
                        this.sendByte(Convert.ToByte('a'));
                        port.ReadTimeout = 1000;

                        int deviceState = port.ReadByte();
                        if (deviceState == 'n' || deviceState == 'o')
                        {
                            port.DataReceived += this.onByteReceived;           // hook up event handler
                            return deviceState;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return 0;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            string buttonContent = this.btnStart.Content as string;
            if (buttonContent == "Connect")
            {
                this.makeConnection();
            }
            else
            {
                // send connection termination signal and close serial port
                this.stopConnection();
                this.port.Close();
                this.port = null;
            }
        }

        private void stopConnection()
        {
            this.sendByte(Convert.ToByte('e'));

            this.showDisconnected();
        }

        /*
         * convert string to byte array
         */
        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private void port_ByteReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int byteReceived = port.ReadByte();
            switch (this.lastCommand)
            {
                case 'i':
                    if (byteReceived == 'i')
                    {
                        MessageBox.Show("This device is a brand new device and has been initialized. Please register users under this device.");
                    }

                    break;
                case 'n':                                   // get number of new files command
                    this.totalNumOfFiles = byteReceived;
                    this.curNumOfFiles = 0;

                    if (this.totalNumOfFiles != 0)
                    {
                        port.DataReceived -= this.onByteReceived;
                        port.DataReceived += this.onDataReceived;

                        this.sendByte(Convert.ToByte('d'));     // send sync data command
                        
                        //invoke progress bar
                        Dispatcher.BeginInvoke((Action)delegate()
                        {
                            this.popUpTransferData.IsOpen = true;
                        });
                    }
                    else
                    {
                        Dispatcher.BeginInvoke((Action)delegate()
                        {
                            MessageBox.Show("Data is synchronized.");
                        });
                    }
                    
                    break;
                case 'u':                                   // add user command
                    // hide popup box when adding user is completed
                    Dispatcher.BeginInvoke((Action)delegate()
                    {
                        this.popUpScanFinger.IsOpen = false;
                    });

                    if (byteReceived == this.lastParam)
                    {
                        MessageBox.Show("User has been successfully added!");
                    }
                    else
                    {
                        MessageBox.Show("Adding user was not successful. Please try again later.");
                    }
                    break;
                default:
                    break;
            }
        }

        // event handler for receiving data
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = port.BytesToRead;
            byte[] comBuffer = new byte[bytes];
            port.Read(comBuffer, 0, bytes);         // every byte in this packet has been stored in the array

            string buf = System.Text.Encoding.ASCII.GetString(comBuffer);
            allData += buf;

            // cumulate the number of files
            this.curNumOfFiles += buf.Count(f => f == '+') / 2;             
            
            // update the progress bar
            Dispatcher.BeginInvoke((Action)delegate()
            {
                this.pgBarDataTransfer.Value = 100 * this.curNumOfFiles / this.totalNumOfFiles;
            });

            if (buf.Contains("--"))
            {
                allData = allData.Remove(allData.Length - 2, 2); 
                string[] parts = allData.Split('+').ToArray<string>();

                foreach (string part in parts)
                {
                    if (part.Length != 0)
                    {
                        byte[] partData = System.Text.Encoding.ASCII.GetBytes(part);
                        int uid = (int)partData[0];
                        int index = (int)partData[1];
                        partData = partData.Skip(2).ToArray<byte>();

                        string file = uid.ToString() + "_" + index.ToString() + ".txt";

                        // if data is successfully storing in txt file
                        // store the uid and index
                        if (this.ByteArrayToFile(file, partData))
                        {
                            newFileNames.Add(new fileName(uid, index));
                        }
                    }
                }

                foreach ( fileName newFileName in newFileNames)
                {
                    this.processData(newFileName.Uid, newFileName.Index);
                }

                // revert back to use the byte received event handler
                port.DataReceived -= this.onDataReceived;
                port.DataReceived += this.onByteReceived;

                // hide popup box when data transfer and processing is completed
                Dispatcher.BeginInvoke((Action)delegate()
                {
                    this.popUpTransferData.IsOpen = false;
                });
            }            
        }

        private static bool isFileHeader(byte data)
        {
            if (data == '+')
            {
                return true;
            }
            return false;
        }

        public bool ByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                return true;
            }
            catch (Exception _Exception) {}

            // error occured, return false
            return false;
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            newFileNames = new List<fileName>();
            allData = string.Empty;

            this.lastCommand = 'n';
            this.sendByte(Convert.ToByte('n'));
        }

        private void addUser(int uid)
        {
            this.lastCommand = 'u';
            this.lastParam = uid;
            this.sendByte(Convert.ToByte('u'));
            this.sendByte(Convert.ToByte(uid));

            this.popUpScanFinger.IsOpen = true;
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (this.rdBtnNewUser.IsChecked == true)
            {
                NavigationService.Navigate(new Uri("Frames/NewUserSetupPage.xaml", UriKind.Relative));
            }
            else if (this.cmBoxUsers.SelectedItem == null)
            {
                MessageBox.Show("Please select an existing user!");
            }
            else
            {
                User selectedUser = this.cmBoxUsers.SelectedItem as User;
                int uid = selectedUser.Id;

                this.addUser(uid);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void connectPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (null != this.port)
            {
                this.stopConnection();
                this.port.Close();
                this.port = null;
            }
        }
    }
}
