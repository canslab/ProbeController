using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Streamer;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        private static WriteableBitmap _wb = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr24, null);
        private StreamReceiver streamReceiver = new StreamReceiver();
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }
        public MainWindow()
        {
            InitializeComponent();
            frame.Stretch = Stretch.None;
            frame.Source = _wb;

            // dispatcher timer is used to render frame every given milli seconds. 
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.IsEnabled = false;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 90);
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // first it should be stopped, because we don't know how much time the grabbing a frame will take.
            dispatcherTimer.Stop();    
            OpenCvSharp.Mat eachFrame = null;

            // GetFrameAsMat() will take some time, so it is needed to be exeuted within the worker thread
            // to improve UI response time
            await Task.Factory.StartNew(() => 
            {
                // this would take some time.. 
                streamReceiver.GetFrameAsMat(out eachFrame);
            });

            // after back from async job, UI thread need to render real time image using the result matrix which is given by 
            // async job.
            OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(eachFrame, _wb);

            // after using that, you should release it, otherwise memory leak will be occured.
            eachFrame.Release();

            // restart the timer, in order to make sure real time streaming job works.
            dispatcherTimer.Start();
        }

        private async void startStreamButton_Click(object sender, RoutedEventArgs e)
        {
            bool bSuccess = false;

            // initate connection to remote camera using ConnectToURLAsync()
            bSuccess = await streamReceiver.ConnectAsync("http://devjhlab.iptime.org:8080/?action=stream");
          
            // if the connection has failed, show a result messagebox to the user
            // and return 
            if (bSuccess == false)
            {
                MessageBox.Show("Connection Failure.");
                return;
            }
            // otherwise, set the button's text to "Connected"
            // and make that button disabled.
            else
            {
                startStreamButton.Content = "Connected";
                startStreamButton.IsEnabled = false;
                dispatcherTimer.Start();
            }
        }

        private void frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show(frame.ActualHeight.ToString());
        }

        private void endStreamButton_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                streamReceiver.Disconnect();
            }

            startStreamButton.Content = "Start Stream";
            startStreamButton.IsEnabled = true;
        }
    }
}
