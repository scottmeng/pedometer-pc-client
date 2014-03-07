using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPort_client.Sources
{
    class Accelerations
    {
        public int XAccel { get; set; }
        public int YAccel { get; set; }
        public int ZAccel { get; set; }
        public int TimeStamp { get; set; }

        public Accelerations(int xAccel, int yAccel, int zAccel, int timeStamp)
        {
            XAccel = xAccel;
            YAccel = yAccel;
            ZAccel = zAccel;
            TimeStamp = timeStamp;
        }
    }
}
