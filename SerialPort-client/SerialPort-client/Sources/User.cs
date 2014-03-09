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
        public int Height { get; set; }
        public int Id { get; set; }

        public User(int id, string name, int age, int height)
        {
            this.Name = name;
            this.Age = age;
            this.Height = height;
            this.Id = id;
        }
    }
}
