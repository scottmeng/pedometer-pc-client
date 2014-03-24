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
        private SerialPort port;
        private bool connected = false;
        private bool isTransferringByte = false;
        private List<string> newFileNames;
        private string allData;

        private string conString = "Data Source=C:\\Users\\Kaizhi\\exerciseData.sdf;Password=admin;Persist Security Info=True";

        public ConnectPage()
        {
            InitializeComponent();

            //makeConnection();
            //DetectArduino();
            processData("2.txt");
        }

        private void makeConnection()
        {
            this.btnStart.IsEnabled = false;
            this.txtBlkStatus.Text = "Connecting";
            this.txtBlkStatus.Foreground = Brushes.Orange;

            connected = checkPedometerConnection();

            if (connected)
            {
                this.txtBlkStatus.Text = "Connected through " + port.PortName;
                this.txtBlkStatus.Foreground = Brushes.Green;
            }
            else
            {
                this.btnStart.IsEnabled = true;
                this.txtBlkStatus.Text = "Not connected";
                this.txtBlkStatus.Foreground = Brushes.Red;
            }
        }

        private void processData(int userId, int sessionIndex)
        {
            string fileName = userId.ToString() + "_" + sessionIndex.ToString() + ".txt";
            string line;
            List<Sample> samples = new List<Sample>();
            System.IO.StreamReader dataFile = new System.IO.StreamReader(fileName);

            while ((line = dataFile.ReadLine()) != null)
            {
                var parts = line.Split(',');
                int timestamp = Convert.ToInt32(parts[0]);
                int x_acc = Convert.ToInt32(parts[1]);
                int y_acc = Convert.ToInt32(parts[2]);
                int z_acc = Convert.ToInt32(parts[3]);

                Sample sample = new Sample(timestamp, x_acc, y_acc, z_acc);
                samples.Add(sample);
            }

            samples = lowPassFilter(samples, 10);
            List<double> thresholds = this.calThresholds(samples, 50);
            List<int> stepTimes = this.countSteps(samples, thresholds);
            this.recordSteps(userId, sessionIndex, stepTimes);
        }

        private double calDistanceFromStep(int userHeight, List<int> stepTimes)
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

            return distance;
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
                    distance += stepsPerFrame * height / 5;
                    break;
                case 2:
                    distance += stepsPerFrame * height / 5;
                    break;
                case 3:
                    distance += stepsPerFrame * height / 4;
                    break;
                case 4:
                    distance += stepsPerFrame * height / 3;
                    break;
                case 5:
                    distance += stepsPerFrame * height / 2;
                    break;
                case 6:
                    distance += stepsPerFrame * height / 1.2;
                    break;
                case 7:
                    distance += stepsPerFrame * height;
                    break;
                default:
                    distance += stepsPerFrame * height;
                    break;
            }

            return distance;
        }

        private void recordSteps(int userId, int sessionIndex, List<int> stepTimes)
        {
            int startTime = stepTimes[0];
            int curMin = 0;
            int countInMin = 0;

            User user = getUserByID(userId);
            int height = user.Height;

            foreach (int stepTime in stepTimes)
            {
                if ((stepTime - startTime) / 60000 == curMin)
                {
                    countInMin += 1;
                }
                else
                {
                    this.saveRecordToDB(userId, sessionIndex, curMin, countInMin);
                    curMin = (stepTime - startTime) / 60000;
                    countInMin = 0;
                }
            }

        }

        private User getUserByID(int userId)
        {
            User user;
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();
                // Read in all values in the table.
                using (SqlCeCommand com = new SqlCeCommand("SELECT id,name,gender,age,height FROM Users WHERE id = @id", con))
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

                        user = new User(id, name, gender, age, height);
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

        private void sendCommand(string command)
        {
            byte[] msg = new byte[1];

            switch(command)
            {
                case "a":
                    msg[0] = Convert.ToByte('a');
                    break;
                case "d":
                    msg[0] = Convert.ToByte('d');
                    break;
                case "i":
                    msg[0] = Convert.ToByte('i');
                    break;
                case "u":
                    msg[0] = Convert.ToByte('u');
                    break;
                default:
                    break;
            }
            if (!port.IsOpen)
            {
                port.Open();
            }

            port.Write(msg, 0, 1);
        }

        // send a request to every available COM port
        private bool checkPedometerConnection()
        {
            string[] portNames = SerialPort.GetPortNames();

            foreach (string portName in portNames)
            {
                port = new SerialPort(portName, 9600);
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
                        this.sendCommand("a");
                        port.ReadTimeout = 1000;

                        int deviceState = port.ReadByte();
                        if (deviceState == 'n' || deviceState == 'o')
                        {
                            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                            return true;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            makeConnection();
        }

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

        // event handler for receiving data
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int uid, index;
            string fileName;
            int bytes = port.BytesToRead;
            byte[] comBuffer = new byte[bytes];
            port.Read(comBuffer, 0, bytes);         // every byte in this packet has been stored in the array

            string buf = System.Text.Encoding.ASCII.GetString(comBuffer);
            allData += buf;

            if (buf.Contains("--"))
            {
                allData = allData.Remove(allData.Length - 2, 2); 
                string[] parts = allData.Split('+').ToArray<string>();

                foreach (string part in parts)
                {
                    if (part.Length != 0)
                    {
                        byte[] partData = System.Text.Encoding.ASCII.GetBytes(part);
                        uid = (int)partData[0];
                        index = (int)partData[1];
                        partData = partData.Skip(2).ToArray<byte>();

                        fileName = uid.ToString() + "_" + index.ToString() + ".txt";
                        newFileNames.Add(fileName);
                        this.ByteArrayToFile(fileName, partData);
                    }
                }
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
            catch (Exception _Exception)
            {
            }

            // error occured, return false
            return false;
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            allData = "";
            newFileNames = new List<string>();
            this.sendCommand("d");
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
