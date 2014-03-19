using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPort_client.Sources
{
    class History
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int Min { get; set; }
        public int Index { get; set; }
        public int Count { get; set; }
        public double Distance { get; set; }
        public double Calories { get; set; }
        public string DateDisplay { get; set; }

        public History(int id, DateTime date, int min, int index, int count, double distance, double calories)
        {
            this.ID = id;
            this.Date = date;
            this.Min = min;
            this.Index = index;
            this.Count = count;
            this.Distance = distance;
            this.Calories = calories;
            this.DateDisplay = this.Date.Date.ToString();
        }
    }
}
