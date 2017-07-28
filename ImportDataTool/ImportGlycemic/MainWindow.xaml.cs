using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImportGlycemic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string windDataServiceURL;
        int intervalInSecond;
        int minGlycemic;
        int maxGlycemic;
        double timeToPostInSecond = 1 * 60;
        bool isProcessRunning = false;
        string deviceUID;

        RestClient rc;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        public bool IsProcessRunning
        {
            get
            {
                return isProcessRunning;
            }

            set
            {
                isProcessRunning = value;

                Button_StartStreaming.IsEnabled = !isProcessRunning;
                Button_EndStreaming.IsEnabled = isProcessRunning;
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            intervalInSecond = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"].ToString());
            minGlycemic = Convert.ToInt32(ConfigurationManager.AppSettings["MinGlycemic"].ToString());
            maxGlycemic = Convert.ToInt32(ConfigurationManager.AppSettings["MaxGlycemic"].ToString());
            deviceUID = ConfigurationManager.AppSettings["DeviceUID"].ToString();
            windDataServiceURL = string.Format(ConfigurationManager.AppSettings["WindDataService"].ToString(), deviceUID);
            timeToPostInSecond = Convert.ToInt32(ConfigurationManager.AppSettings["TimeToPostInMinute"].ToString()) * 60;

            rc = new RestClient(windDataServiceURL, HttpVerb.POST, "application/json");

            IsProcessRunning = false;
        }

        private void Button_StartStreaming_Click(object sender, RoutedEventArgs e)
        {
            ResetScreen();

            Thread thread = new Thread(new ThreadStart(DoPost));
            thread.IsBackground = true;
            thread.Start();
        }

        private void ResetScreen()
        {
            IsProcessRunning = true;
            pbStatus.Value = 0;
            ListBox_History.Items.Clear();
        }

        private void DoPost()
        {
            DateTime dtStart = DateTime.Now;
            Random rd = new Random();
            int count = 0;

            while (isProcessRunning)
            {
                DateTime dt = DateTime.UtcNow;
                long timeStamp = CurrentMillis.Millis;
                int value = rd.Next(minGlycemic, maxGlycemic);
                int quality = 3;

                string postData = string.Format("{{" +
                                                        "\"dataPoints\":" +
                                                        "[" +
                                                                "{{" +
                                                                    "\"timeStamp\": {0}," +
                                                                    "\"value\": {1}," +
                                                                    "\"quality\": {2} " +
                                                                "}}" +
                                                        "]," +
                                                        "\"attributes\":" +
                                                        "{{" +
                                                            "\"sensorDataType\": \"{3}\"" +
                                                        "}}" +
                                                  "}}", timeStamp.ToString(), value.ToString(), quality.ToString(), "Glycemic");

                rc.PostData = postData;

                string result = rc.MakeRequest();

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ListBox_History.Items.Insert(0, string.Format("{0}. TimeStamp: {1} - {2},   Glycemic: {3},   Quality: {4})", (++count).ToString("00"), dt.ToString(), timeStamp, value, quality));

                    Double percent = ((DateTime.Now - dtStart).TotalSeconds / timeToPostInSecond) * 100;
                    pbStatus.Value = percent;

                    if (pbStatus.Value >= 100)
                    {
                        IsProcessRunning = false;
                    }
                }));


                Thread.Sleep(intervalInSecond * 1000);
            }
        }


        private void Button_EndStreaming_Click(object sender, RoutedEventArgs e)
        {
            IsProcessRunning = false;
        }
    }

    static class CurrentMillis
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
    }
}
