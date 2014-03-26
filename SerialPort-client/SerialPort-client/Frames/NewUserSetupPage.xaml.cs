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
using System.Collections.ObjectModel;

using System.Data.SqlServerCe;

using SerialPort_client.Sources;

namespace SerialPort_client.Frames
{
    /// <summary>
    /// Interaction logic for NewUserSetupPage.xaml
    /// </summary>
    public partial class NewUserSetupPage : Page
    {
        private ObservableCollection<int> ageList, heightList, weightList;
        private string conString = "Data Source=C:\\Users\\Kaizhi\\exerciseData.sdf;Password=admin;Persist Security Info=True";

        public NewUserSetupPage()
        {
            InitializeComponent();
            
            initializeForm();
        }

        private void initializeForm()
        {
            ageList = new ObservableCollection<int>();
            heightList = new ObservableCollection<int>();
            weightList = new ObservableCollection<int>();

            for (int age = 12; age < 80; ++age)
            {
                ageList.Add(age);
            }

            for (int height = 140; height < 210; ++height)
            {
                heightList.Add(height);
            }

            for (int weight = 40; weight < 200; ++weight)
            {
                weightList.Add(weight);
            }

            this.combBxAge.ItemsSource = ageList;
            this.combBxHeight.ItemsSource = heightList;
            this.combBxWeight.ItemsSource = weightList;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (isFormValid())
            {
                string name = this.txtBxName.Text;
                int age = (int)this.combBxAge.SelectedItem;
                string gender = ((ComboBoxItem)this.combBxGender.SelectedItem).Content.ToString();
                int height = (int)this.combBxHeight.SelectedItem;
                int weight = (int)this.combBxWeight.SelectedItem;

                int uid = this.addNewUserToDB(name, age, height, weight, gender);
            }
        }

        private bool isFormValid()
        {
            if (string.IsNullOrEmpty(this.txtBxName.Text))
            {
                MessageBox.Show("Name cannot be empty!");
                return false;
            }

            if (string.IsNullOrEmpty(this.combBxAge.Text))
            {
                MessageBox.Show("Select an age!");
                return false;
            }

            if (string.IsNullOrEmpty(this.combBxGender.Text))
            {
                MessageBox.Show("Select a gender");
                return false;
            }

            if (string.IsNullOrEmpty(this.combBxHeight.Text))
            {
                MessageBox.Show("Select a height");
                return false;
            }

            if (string.IsNullOrEmpty(this.combBxWeight.Text))
            {
                MessageBox.Show("Select a weight");
                return false;
            }

            return true;
        }

        /*
         * insert user data into DB and return the uid
         */
        private int addNewUserToDB(string name, int age, int height, int weight, string gender)
        {
            int uid = 0;

            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();

                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand com = new SqlCeCommand("INSERT INTO Users (name, age, height, weight, gender) VALUES (@name, @age, @height, @weight, @gender)", con))
                {
                    com.Parameters.AddWithValue("@name", name);
                    com.Parameters.AddWithValue("@age", age);
                    com.Parameters.AddWithValue("@height", height);
                    com.Parameters.AddWithValue("@weight", weight);
                    com.Parameters.AddWithValue("@gender", gender);
                    com.ExecuteNonQuery();
                }

                using (SqlCeCommand com = new SqlCeCommand("SELECT id FROM Users WHERE name = @name AND age = @age AND height = @height AND weight = @weight AND gender = @gender", con))
                {
                    com.Parameters.AddWithValue("@name", name);
                    com.Parameters.AddWithValue("@age", age);
                    com.Parameters.AddWithValue("@height", height);
                    com.Parameters.AddWithValue("@weight", weight);
                    com.Parameters.AddWithValue("@gender", gender);
                    SqlCeDataReader reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        uid = reader.GetInt32(0);
                    }
                }
            }

            return uid;

        }
    }
}
