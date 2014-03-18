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

            
            makeConnection();
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

        // send a request to every available COM port
        private bool checkPedometerConnection()
        {
            string[] portNames = SerialPort.GetPortNames();

            foreach (string portName in portNames)
            {
                port = new SerialPort(portName, 9600);

                try
                {
                      port.Open();

                    if (port.IsOpen)
                    {
                        port.Write("a");
                        port.ReadTimeout = 2000;
                        this.btnStart.IsEnabled = false;
                        return true;
                        if (port.ReadChar() == 'b')
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

        // event handler for receiving data
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (isTransferringByte)
            {
                int bytes = port.BytesToRead;

                byte[] comBuffer = new byte[bytes];
                port.Read(comBuffer, 0, bytes);
            }
            else
            {
                string msg = port.ReadExisting();
            }
        }
    }
}
