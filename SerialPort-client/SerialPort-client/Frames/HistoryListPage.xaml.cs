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

using System.Data;
using System.Data.SqlServerCe;

using SerialPort_client.Sources;

namespace SerialPort_client.Frames
{
    /// <summary>
    /// Interaction logic for HistoryListPage.xaml
    /// </summary>
    public partial class HistoryListPage : Page
    {
        public HistoryListPage()
        {
            InitializeComponent();

            // Retrieve the connection string from the settings file.
            // Your connection string name will end in "ConnectionString"
            // So it could be coolConnectionString or something like that.
            string conString = Properties.Settings.Default.exerciseDataConnectionString;

            /*
            // Open the connection using the connection string.
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();

                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand com = new SqlCeCommand("INSERT INTO Users (name, age, height) VALUES (@name, @age, @height)", con))
                {
                    com.Parameters.AddWithValue("@name", "Scott");
                    com.Parameters.AddWithValue("@age", 23);
                    com.Parameters.AddWithValue("@height", 174);
                    com.ExecuteNonQuery();
                }
            }
            */

            // Open the connection using the connection string.
            using (SqlCeConnection newCon = new SqlCeConnection(conString))
            {
                newCon.Open();
                // Read in all values in the table.
                using (SqlCeCommand com = new SqlCeCommand("SELECT id,name,age,height FROM Users", newCon))
                {
                    List<User> users = new List<User>();
                    SqlCeDataReader reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        int age = reader.GetInt32(2);
                        int height = reader.GetInt32(3);

                        User user = new User(id, name, age, height);
                        users.Add(user);
                    }

                    this.lstBoxUser.ItemsSource = users;
                }
            }
            
        }
    }
}
