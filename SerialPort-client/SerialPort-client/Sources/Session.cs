using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPort_client.Sources
{
    class Session
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int Index { get; set; }
        public List<History> Records { get; set; }
        public int TotalCount { get; set; }
        public double TotalDistance { get; set; }
        public double TotalCalories { get; set; }
        public string DateDisplay { get; set; }

        public Session(int id, DateTime date, int index)
        {
            this.ID = id;
            this.Date = date;
            this.Index = index;
            this.Records = new List<History>();
            this.TotalCalories = 0;
            this.TotalDistance = 0;
            this.TotalCount = 0;
            this.DateDisplay = this.Date.Date.ToShortDateString();
        }

        public void addRecord(History record)
        {
            this.Records.Add(record);

            this.TotalCount += record.Count;
            this.TotalDistance += record.Distance;
            this.TotalCalories += record.Calories;
        }
    }
}
