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

namespace SerialPort_client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Public serial port 
        SerialPort serialPort1;

        public MainWindow()
        {
            InitializeComponent();

            serialPort1 = new SerialPort();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            serialPort1.PortName = "COM1";
            serialPort1.BaudRate = 9600;

            serialPort1.Open();
            if (serialPort1.IsOpen)
            {
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
            }
        }
    }
}
