using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Configuration;
using System.Threading;

namespace ImportDataTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string windDataServiceURL;
        int intervalInSecond;
        int minHeartRate;
        int maxHeartRate;
        bool isRequestedToStop = false;
        string deviceUID;

        RestClient rc;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            intervalInSecond = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"].ToString());
            minHeartRate = Convert.ToInt32(ConfigurationManager.AppSettings["MinHeartRate"].ToString());
            maxHeartRate = Convert.ToInt32(ConfigurationManager.AppSettings["MaxHeartRate"].ToString());
            deviceUID = ConfigurationManager.AppSettings["DeviceUID"].ToString();
            windDataServiceURL = string.Format(ConfigurationManager.AppSettings["WindDataService"].ToString(), deviceUID);

            rc = new RestClient(windDataServiceURL, HttpVerb.POST, "application/json");
        }

        private void Button_StartStreaming_Click(object sender, RoutedEventArgs e)
        {
            isRequestedToStop = false;

            Thread thread = new Thread(new ThreadStart(DoPost));
            thread.IsBackground = true;
            thread.Start();
        }

        private void DoPost()
        {
            Random rd = new Random();

            while (!isRequestedToStop)
            {
                long timeStamp = CurrentMillis.Millis;
                int value = rd.Next(minHeartRate, maxHeartRate);
                int quality = 3;
                
                string postData = string.Format("{{" + 
                                                        "\"dataPoints\":" +
                                                        "[" +
                                                                "{{" +  
                                                                    "\"timeStamp\": {0}," +
                                                                    "\"value\": {1}," + 
                                                                    "\"quality\": {2} " +
                                                                "}}" +
                                                        "]" + 
                                                  "}}", timeStamp.ToString(), value.ToString(), quality.ToString());

                rc.PostData = postData;

                string result = rc.MakeRequest();

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ListBox_History.Items.Insert(0, string.Format("TimeStamp: {0}, value: {1}, quality: {2})", timeStamp, value, quality));
                }));


                Thread.Sleep(intervalInSecond * 1000);
            }
        }


        private void Button_EndStreaming_Click(object sender, RoutedEventArgs e)
        {
            isRequestedToStop = true;
        }
    }

    static class CurrentMillis
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
    }
}
