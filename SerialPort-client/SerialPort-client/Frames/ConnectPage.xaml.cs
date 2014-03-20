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

        public ConnectPage()
        {
            InitializeComponent();

            //makeConnection();
            DetectArduino();
        }

        private void makeConnection()
        {
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

        private bool DetectArduino()
        {
            port = new SerialPort("COM4", 9600);
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.DtrEnable = true;
            port.RtsEnable = true;
            try
            {
                //The below setting are for the Hello handshake
                byte[] buffer = new byte[5];
                buffer[0] = Convert.ToByte(16);
                buffer[1] = Convert.ToByte(128);
                buffer[2] = Convert.ToByte(0);
                buffer[3] = Convert.ToByte(0);
                buffer[4] = Convert.ToByte(4);
                int intReturnASCII = 0;
                char charReturnValue = (Char)intReturnASCII;
                port.Open();
                //port.Write("test");
                while (0 == port.BytesToRead)
                    Thread.Sleep(100);
                char[] first = new char[port.BytesToRead];
                port.Read(first, 0, first.Length);
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }


        // send a request to every available COM port
        private bool checkPedometerConnection()
        {
            string[] portNames = SerialPort.GetPortNames();

            port = new SerialPort("COM4", 9600);
            port.Open();
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

            //foreach (string portName in portNames)
            //{
            //    port = new SerialPort(portName, 9600);

            //    try
            //    {
            //        port.Open();

            //        if (port.IsOpen)
            //        {
            //            //this.sendCommand("a");
            //            port.ReadTimeout = 1000;
            //            this.btnStart.IsEnabled = false;
            //            //int deviceState = port.ReadByte();
            //            //if (deviceState != 0)
            //            //{
            //            //    port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            //            //    return true;
            //            //}
            //            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            //        }
            //    }
            //    catch
            //    {
            //        continue;
            //    }
            //}

            return false;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            makeConnection();
        }

        // event handler for receiving data
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = port.BytesToRead;
            byte[] comBuffer = new byte[bytes];
            port.Read(comBuffer, 0, bytes);
            this.txtBlkData.Text = comBuffer.ToString();
            //if (isTransferringByte)
            //{
            //    int bytes = port.BytesToRead;

            //    byte[] comBuffer = new byte[bytes];
            //    port.Read(comBuffer, 0, bytes);
            //}
            //else
            //{
            //    string msg = port.ReadExisting();
            //}
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
            this.sendCommand("d");

            processData("C:\\Users\\Kaizhi\\KAIZHI_5.txt");
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
