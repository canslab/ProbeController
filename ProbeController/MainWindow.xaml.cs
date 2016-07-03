using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Streamer;
using System.Windows.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using ProbeController.Robot;

namespace ProbeController
{ 
    public partial class MainWindow : Window
    {
        // Constants related to frame
        private static int FRAME_WIDTH = 640;
        private static int FRAME_HEIGHT = 480;
        private static int FRAME_DPI_X = 96;
        private static int FRAME_DPI_Y = 96;

        private WriteableBitmap mWb;
        private StreamReceiver mStreamReceiver;
        private RobotCommunicator mCommunicator;

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
            mWb = new WriteableBitmap(FRAME_WIDTH, FRAME_HEIGHT, FRAME_DPI_X, FRAME_DPI_Y, PixelFormats.Bgr24, null);
            mStreamReceiver = new StreamReceiver();

            frame.Stretch = Stretch.None;   
            frame.Source = mWb;

            endStreamButton.IsEnabled = false;
            disconnectButton.IsEnabled = false;
            mCommunicator = new Robot.RobotCommunicator();
        }
        protected override void OnInitialized(EventArgs e)
        {
            //EventManager.RegisterClassHandler(typeof())
            base.OnInitialized(e);
        }

        /*******************************************************************/
        /*********                  Event Handler                   ********/
        /*******************************************************************/
        private async void onStartStreamButton(object sender, RoutedEventArgs e)
        {
            bool bSuccess = false;

            // initate connection to remote camera using ConnectToURLAsync()
            bSuccess = await mStreamReceiver.ConnectToURLAsync("http://devjhlab.iptime.org:8080/?action=stream");
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
                OpenCvSharp.Mat eachFrame = null;

                eachFrame = await mStreamReceiver.GetFrameAsMatAsync();
                
                //OpenCvSharp.Cv2.Blur(eachFrame, eachFrame, new OpenCvSharp.Size(10, 10));
                //OpenCvSharp.Mat rr = new OpenCvSharp.Mat(480, 640, OpenCvSharp.MatType.CV_8UC3);
                //OpenCvSharp.Cv2.Canny(eachFrame, rr, 50, 200, 3);
                //OpenCvSharp.Cv2.CvtColor(rr, rr, OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                // after back from async job, UI thread need to render real time image using the result matrix which is given by 
                // async job.
                OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(eachFrame, mWb);

                // after using that, you should release it, otherwise memory leak will be occured.
                eachFrame.Release();
            }

            // after the while loop expired, streamReceiver should be disconnected.
            mStreamReceiver.Disconnect();
        }
        private void onEndStreamButton(object sender, RoutedEventArgs e)
        {
            // camera should turn off
            bIsCameraWorking = false;

            startStreamButton.Content = "Start Stream";
            startStreamButton.IsEnabled = true;
            endStreamButton.IsEnabled = false;
        }

        private void onGaussianBlurButton(object sender, RoutedEventArgs e)
        {

        }
        private void onCannyButton(object sender, RoutedEventArgs e)
        {

        }

        private async void onConnectionButton(object sender, RoutedEventArgs e)
        {
            int portNumber;
            string ipAddress;

            ipAddress = ipTextBox.Text;
            try
            {
                portNumber = int.Parse(portTextBox.Text);
            }
            catch(Exception argException)
            {
                MessageBox.Show("Robot Connection Failed due to input info is not valid... RSN> " + argException.Message);
                return;
            }

            bool bConnectionSuccess = await mCommunicator.ConnectToURLAsync(ipTextBox.Text, int.Parse(portTextBox.Text));
                        
            if (bConnectionSuccess == false)
            {
                MessageBox.Show("Robot Connection Failed, check your remote deivce");
            }
            else
            {
                disconnectButton.IsEnabled = true;
                connectButton.IsEnabled = false;
            }
                
        }

        private void onDisconnectButton(object sender, RoutedEventArgs e)
        {
            bool bSuccess = false;

            bSuccess = mCommunicator.Disconnect();
            if (bSuccess == false)
            {
                MessageBox.Show("Disconnection task has failed");
            }
            else
            {
                connectButton.IsEnabled = true;
                disconnectButton.IsEnabled = false;
            }
        }

        private void LeftLEDButton_Click(object sender, RoutedEventArgs e)
        {
            //mCommunicator.SendJSONStringAsnyc()
        }

        private void RightLEDButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
