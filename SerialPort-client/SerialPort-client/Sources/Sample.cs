using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPort_client.Sources
{
    class Sample
    {
        public int TimeStamp { get; set; }
        public int XAcc { get; set; }
        public int YAcc { get; set; }
        public int ZAcc { get; set; }

        public Sample(int timeStamp, int xAcc, int yAcc, int zAcc)
        {
            this.TimeStamp = timeStamp;
            this.XAcc = xAcc;
            this.YAcc = yAcc;
            this.ZAcc = zAcc;
        }
    }
}
