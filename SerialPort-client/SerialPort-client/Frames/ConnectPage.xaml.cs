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

        public ConnectPage()
        {
            InitializeComponent();

            string portName = checkPedometerConnection();
            if (portName != null)
            {
                ComboboxItem newPort = new ComboboxItem();
                newPort.Text = portName;
                newPort.Value = portName;
                portsAvailable.Items.Add(newPort);
                btnStart.IsEnabled = true;
            }
        }

        private void getAvailablePorts()
        {
            ComboboxItem newPort;
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                newPort = new ComboboxItem();
                newPort.Text = port;
                newPort.Value = port;
                portsAvailable.Items.Add(newPort);
            }

        }

        private string checkPedometerConnection()
        {
            string[] portNames = SerialPort.GetPortNames();
            SerialPort port;

            foreach (string portName in portNames)
            {
                port = new SerialPort(portName, 9600);

                try
                {
                    port.Open();

                    if (port.IsOpen)
                    {
                        return portName;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            port.PortName = portsAvailable.SelectedValue.ToString();
            port.BaudRate = 9600;

            port.Open();
            if (port.IsOpen)
            {
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Close();
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
            }
        }
    }
}
