﻿using System;
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
                // clear canvas
                this.canvasCalorie.Children.Clear();
                this.canvasCalorieAccu.Children.Clear();
                this.canvasDistance.Children.Clear();
                this.canvasDistanceAccu.Children.Clear();
                this.canvasStep.Children.Clear();
                this.canvasStepAccu.Children.Clear();

                Session selectedSession = (Session)item.DataContext;
                List<History> sortedRecords = selectedSession.Records.OrderBy(record =>  record.Min).ToList();
                
                int width = sortedRecords.Count;
                double maxCount = sortedRecords.Max(record => record.Count);
                double maxDistance = sortedRecords.Max(record => record.Distance);
                double maxCalories = sortedRecords.Max(record => record.Calories);

                foreach (History record in sortedRecords)
                {
                    this.drawRectangle(record.Min, record.Count, width, maxCount, Colors.CadetBlue, this.canvasStep);
                    this.drawRectangle(record.Min, record.Distance, width, maxDistance, Colors.PaleVioletRed, this.canvasDistance);
                    this.drawRectangle(record.Min, record.Calories, width, maxCalories, Colors.DarkOrchid, this.canvasCalorie);
                }

                this.drawPath(sortedRecords, width, selectedSession.TotalCount, Colors.CadetBlue, this.canvasStepAccu);
                this.drawPath(sortedRecords, width, selectedSession.TotalDistance, Colors.PaleVioletRed, this.canvasDistanceAccu);
                this.drawPath(sortedRecords, width, selectedSession.TotalCalories, Colors.DarkOrchid, this.canvasCalorieAccu);
            }

            this.displayStats();
        }

        private void drawPath(List<History> records, double width, double height, Color color, Canvas canvas)
        {
            Polyline path = new Polyline();
            path.Stroke = new SolidColorBrush(color);
            path.StrokeThickness = 2;
            path.HorizontalAlignment = HorizontalAlignment.Center;
            path.VerticalAlignment = VerticalAlignment.Center;

            PointCollection points = new PointCollection();
            double sum = 0;
            foreach (History record in records)
            {
                double x = record.Min / width * 300 + 20;
                sum += record.Count;
                double y = (height - sum) / height * 100 + 40;
                Point point = new Point(x, y);
                points.Add(point);

                // add textblock
                TextBlock textBlock = new TextBlock();
                textBlock.Text = sum.ToString();
                textBlock.Foreground = new SolidColorBrush(color);
                textBlock.FontSize = 10;
                Canvas.SetLeft(textBlock, x);
                Canvas.SetBottom(textBlock, sum / height * 100 + 45);
                canvas.Children.Add(textBlock);

                // add point
                Ellipse ellipse = new Ellipse();
                ellipse.Fill = new SolidColorBrush(color);
                ellipse.Stroke = new SolidColorBrush(color);
                ellipse.StrokeThickness = 1;

                ellipse.Width = 4;
                ellipse.Height = 4;
                Canvas.SetLeft(ellipse, x);
                Canvas.SetBottom(ellipse, sum / height * 100 + 35);
                canvas.Children.Add(ellipse);
            }

            path.Points = points;
            canvas.Children.Add(path);
        }

        private void drawRectangle(int x, double y, int width, double height, Color color, Canvas canvas)
        {
            // add bar
            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(color);
            rect.Width = 240 / (double) width;
            rect.Height = 100 * y / height;
            Canvas.SetLeft(rect, 20 + x * 300 / (double) width);
            Canvas.SetBottom(rect, 40);
            canvas.Children.Add(rect);

            // add textblock
            TextBlock textBlock = new TextBlock();
            textBlock.Text = y.ToString();
            textBlock.Foreground = new SolidColorBrush(color);
            Canvas.SetLeft(textBlock, 20 + x * 300 / (double)width);
            Canvas.SetBottom(textBlock, 45 + 100 * y / height);
            canvas.Children.Add(textBlock);
        }

        private void displayStats()
        {
            this.gridStats.Visibility = System.Windows.Visibility.Visible;
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

        private void tgBtnAccumulativeEnable_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked ?? false)
            {
                this.canvasStepAccu.Visibility = Visibility.Visible;
                this.canvasStep.Visibility = Visibility.Hidden;
                this.canvasCalorieAccu.Visibility = Visibility.Visible;
                this.canvasCalorie.Visibility = Visibility.Hidden;
                this.canvasDistanceAccu.Visibility = Visibility.Visible;
                this.canvasDistance.Visibility = Visibility.Hidden;
            }
            else
            {
                this.canvasStepAccu.Visibility = Visibility.Hidden;
                this.canvasStep.Visibility = Visibility.Visible;
                this.canvasCalorieAccu.Visibility = Visibility.Hidden;
                this.canvasCalorie.Visibility = Visibility.Visible;
                this.canvasDistanceAccu.Visibility = Visibility.Hidden;
                this.canvasDistance.Visibility = Visibility.Visible;
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
