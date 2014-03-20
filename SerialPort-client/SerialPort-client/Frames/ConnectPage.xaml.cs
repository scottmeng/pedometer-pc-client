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

        public ConnectPage()
        {
            InitializeComponent();

            makeConnection();
            //DetectArduino();
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

        private void processData(string _FileName)
        {
            string line;
            List<Sample> samples = new List<Sample>();
            System.IO.StreamReader dataFile = new System.IO.StreamReader(_FileName);

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
