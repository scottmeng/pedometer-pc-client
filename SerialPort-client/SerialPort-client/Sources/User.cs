using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPort_client.Sources
{
    class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public int Height { get; set; }
        public int Id { get; set; }
        public bool hasNewData { get; set; }

        public User(int id, string name, string gender, int age, int height)
        {
            this.Name = name;
            this.Age = age;
            this.Gender = gender;
            this.Height = height;
            this.Id = id;

            this.hasNewData = false;
        }
    }
}
