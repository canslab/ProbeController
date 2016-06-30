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
        // Constants related to frame 
        private static int FRAME_WIDTH = 640;
        private static int FRAME_HEIGHT = 480;
        private static int FRAME_DPI_X = 96;
        private static int FRAME_DPI_Y = 96;

        private WriteableBitmap _wb;
        private StreamReceiver streamReceiver;

        /// <summary>
        /// whether the streaming job is working or not.
        /// </summary>
        public bool bIsCameraWorking = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _wb = new WriteableBitmap(FRAME_WIDTH, FRAME_HEIGHT, FRAME_DPI_X, FRAME_DPI_Y, PixelFormats.Bgr24, null);
            streamReceiver = new StreamReceiver();

            frame.Stretch = Stretch.None;   
            frame.Source = _wb;

            endStreamButton.IsEnabled = false;
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        /*******************************************************************/
        /*********                  Event Handler                   ********/
        /*******************************************************************/
        private async void OnStartStream(object sender, RoutedEventArgs e)
        {
            bool bSuccess = false;

            // initate connection to remote camera using ConnectToURLAsync()
            bSuccess = await streamReceiver.ConnectToURLAsync("http://devjhlab.iptime.org:8080/?action=stream");
            if (bSuccess == false)
            {
                MessageBox.Show("Connection Failure.");
                return;
            }
            // if the connection has suceed.
            else   
            {
                startStreamButton.Content = "Connected";
                startStreamButton.IsEnabled = false;
                endStreamButton.IsEnabled = true;
            }

            // camera is now working....
            bIsCameraWorking = true;
            
            // While the bCameraWork is true, grab a frame from the remote IP camera
            // If disconnect button has been pushed, bCameraWork would be false
            // it causes this while loop expired.  
            while(bIsCameraWorking == true)
            {
                OpenCvSharp.Mat eachFrame = await streamReceiver.GetFrameAsMatAsync();

                // after back from async job, UI thread need to render real time image using the result matrix which is given by 
                // async job.
                OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(eachFrame, _wb);

                // after using that, you should release it, otherwise memory leak will be occured.
                eachFrame.Release();
            }

            // after the while loop expired, streamReceiver should be disconnected.
            streamReceiver.Disconnect();
        }
        private void OnEndStream(object sender, RoutedEventArgs e)
        {
            // camera should turn off
            bIsCameraWorking = false;

            startStreamButton.Content = "Start Stream";
            startStreamButton.IsEnabled = true;
            endStreamButton.IsEnabled = false;
        }
    }
}
