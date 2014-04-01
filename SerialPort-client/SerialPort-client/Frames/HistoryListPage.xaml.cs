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
        private string conString = "Data Source=C:\\Users\\Kaizhi\\exerciseData.sdf;Password=admin;Persist Security Info=True";
        
        public HistoryListPage()
        {
            InitializeComponent();

            //string conString = Properties.Settings.Default.exerciseDataConnectionString;
            

            /*
            // Open the connection using the connection string.
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();

                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand com = new SqlCeCommand("INSERT INTO Users (name, age, height, gender) VALUES (@name, @age, @height, @gender)", con))
                {
                    com.Parameters.AddWithValue("@name", "Scott");
                    com.Parameters.AddWithValue("@age", 23);
                    com.Parameters.AddWithValue("@height", 174);
                    com.Parameters.AddWithValue("@gender", "male");
                    com.ExecuteNonQuery();
                }
            }
            */

            /*
            // Open the connection using the connection string.
            using (SqlCeConnection con = new SqlCeConnection(conString))
            {
                con.Open();

                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand com = new SqlCeCommand("INSERT INTO History (uid, date, hid, min, steps, distance, calories) VALUES (@uid, @date, @index, @min, @steps, @distance, @calories)", con))
                {
                    com.Parameters.AddWithValue("@uid", 2);
                    com.Parameters.AddWithValue("@date", DateTime.Now);
                    com.Parameters.AddWithValue("@index", 0);
                    com.Parameters.AddWithValue("@min", 5);
                    com.Parameters.AddWithValue("@steps", 110);
                    com.Parameters.AddWithValue("@distance", 3322);
                    com.Parameters.AddWithValue("@calories", 32);
                    com.ExecuteNonQuery();
                }
            }
            */

            // Open the connection using the connection string.
            using (SqlCeConnection newCon = new SqlCeConnection(conString))
            {
                newCon.Open();
                // Read in all values in the table.
                using (SqlCeCommand com = new SqlCeCommand("SELECT id,name,gender,age,height,weight FROM Users", newCon))
                {
                    List<User> users = new List<User>();
                    SqlCeDataReader reader = com.ExecuteReader();
                    while (reader.Read()) 
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string gender = reader.GetString(2);
                        int age = reader.GetInt32(3);
                        int height = reader.GetInt32(4);
                        int weight = reader.GetInt32(5);

                        User user = new User(id, name, gender, age, height, weight);
                        users.Add(user); 
                    }

                    this.lstBoxUser.ItemsSource = users;
                }
            }
            
        }

        private void lstBoxHistory_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(this.lstBoxHistory, e.OriginalSource as DependencyObject) as ListBoxItem;

            if (item != null)
            {
                Session selectedSession = (Session)item.DataContext;
                List<History> sortedRecords = selectedSession.Records.OrderBy(o=>o.Min).ToList();
                int width = sortedRecords.Count;
                int prevX = int.MinValue;
                double prevY = 0;

                foreach (History record in sortedRecords)
                {
                    //if (prevX != int.MinValue && prevY != 0)
                    //{
                    //    this.drawLines(prevX, record.Min, prevY, record.Count + prevY, sortedRecords.Capacity, selectedSession.TotalCount, this.canvasStep);
                    //}
                    //prevX = record.Min;
                    //prevY += record.Count;
                    this.drawRectangle(record.Min, record.Count, width, selectedSession.TotalCount, this.canvasStep);
                }
            }

            this.displayStats();
        }

        private void drawLines(int leftX, int rightX, double leftY, double rightY, int width, double height, Canvas canvas)
        {
            Line line = new Line();
            line.X1 = 200 * (leftX / width);
            line.Y1 = 100 * (leftY / height);
            line.X2 = 200 * (rightX / width);
            line.Y2 = 100 * (rightY / height);
            canvas.Children.Add(line);
        }

        private void drawRectangle(int x, double y, int width, double height, Canvas canvas)
        {
            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(Colors.Navy);
            rect.Width = 200 / width;
            rect.Height = 200 * y / height;
            Canvas.SetLeft(rect, 20 + x * 200 / width);
            Canvas.SetBottom(rect, 40);
            canvas.Children.Add(rect);
        }

        private void displayStats()
        {
            this.tabStats.Visibility = System.Windows.Visibility.Visible;
            this.txtBlkNoSelection.Visibility = System.Windows.Visibility.Hidden;
        }

        private void lstBoxUser_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(this.lstBoxUser, e.OriginalSource as DependencyObject) as ListBoxItem;

            if (item != null)
            {
                User selectedUser = (User)item.DataContext;
                
                // Open the connection using the connection string.
                using (SqlCeConnection newCon = new SqlCeConnection(conString))
                {
                    newCon.Open();
                    // Read in all values in the table.
                    using (SqlCeCommand com = new SqlCeCommand("SELECT date,hid,min,uid,steps,distance,calories FROM History WHERE uid = @id", newCon))
                    {
                        com.Parameters.AddWithValue("@id", selectedUser.Id);
                        List<Session> sessions = new List<Session>();
                        Session curSession = null;
                        SqlCeDataReader reader = com.ExecuteReader();
                        while (reader.Read())
                        {
                            DateTime date = reader.GetDateTime(0);
                            int index = reader.GetInt32(1);
                            int min = reader.GetInt32(2);
                            int id = reader.GetInt32(3);
                            int count = reader.GetInt32(4);
                            double distance = reader.GetDouble(5);
                            double calorie = reader.GetDouble(6);

                            History record = new History(id, date, min, index, count, distance, calorie);

                            if (null == curSession)
                            {
                                curSession = new Session(record.ID, record.Date, record.Index);
                            }
                            else if (curSession.ID != record.ID ||
                                     curSession.Index != record.Index ||
                                     curSession.Date != record.Date)
                            {
                                sessions.Add(curSession);
                                curSession = new Session(record.ID, record.Date, record.Index); 
                            }
                            curSession.addRecord(record);
                        }

                        sessions.Add(curSession);
                        this.lstBoxHistory.ItemsSource = sessions;
                    }
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class ShowNewDataTagConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
