/*******************************************************************
 * 
 * This file only deals with the event handlers, GUI related works 
 * 
 * 
 *******************************************************************/
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JHStreamReceiver;
using ProbeController.Robot;
using Cv2 = OpenCvSharp;
using System.Threading.Tasks;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap mWb;
        // stream receiver
        private StreamReceiver mStreamReceiver;
        // communicator to robot
        private RobotCommunicator mCommunicator;
        private RobotController mRobotController;
        

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
            mCommunicator = new RobotCommunicator();
            mRobotController = new RobotController();
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
                Cv2.Mat eachFrame = null;

                eachFrame = await mStreamReceiver.GetFrameAsMatAsync();
                
                //OpenCvSharp.Cv2.Blur(eachFrame, eachFrame, new OpenCvSharp.Size(10, 10));
                //OpenCvSharp.Mat rr = new OpenCvSharp.Mat(480, 640, OpenCvSharp.MatType.CV_8UC3);
                //OpenCvSharp.Cv2.Canny(eachFrame, rr, 50, 200, 3);
                //OpenCvSharp.Cv2.CvtColor(rr, rr, OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                // after back from async job, UI thread need to render real time image using the result matrix which is given by 
                // async job.
                Cv2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(eachFrame, mWb);

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

        private async void onConnectButton(object sender, RoutedEventArgs e)
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
                bool bAttachedWell = mRobotController.AttachCommunicator(mCommunicator);
                if (bAttachedWell == false)
                {
                    MessageBox.Show("Attachment task has failed!! check it!");
                    mRobotController.DetachCommunicator();
                }
            }
        }
        private void onDisconnectButton(object sender, RoutedEventArgs e)
        {
            mCommunicator.Disconnect();

            connectButton.IsEnabled = true;
            disconnectButton.IsEnabled = false;
            
        }
        private async void onLeftLEDButton(object sender, RoutedEventArgs e)
        {
            bool bDidWell = false;
            bDidWell = await mRobotController.TurnOnLED(RobotProtocol.LEDSide.LEFT, bLeftLEDButtonClicked);

            if (bDidWell == false)
            {
                MessageBox.Show("LED Turn Failed..");                
            }
            else
            {
                bLeftLEDButtonClicked = !bLeftLEDButtonClicked;
            }
        }
        private async void onRightLEDButton(object sender, RoutedEventArgs e)
        {
            bool bDidWell = await mRobotController.TurnOnLED(RobotProtocol.LEDSide.RIGHT, bRightLEDButtonClicked);
            if (bDidWell == false)
            {
                MessageBox.Show("LED Turn Failed..");
            }
            else
            {
                bRightLEDButtonClicked = !bRightLEDButtonClicked;
            }
        }

        /// <summary>
        /// Keyboard event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void onMainKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F3:
                    await mCommunicator.IssueLEDCommandAsync(RobotProtocol.LEDSide.LEFT, bLeftLEDButtonClicked);
                    bLeftLEDButtonClicked = !bLeftLEDButtonClicked;
                    break;
                case Key.F4:
                    await mCommunicator.IssueLEDCommandAsync(RobotProtocol.LEDSide.RIGHT, bRightLEDButtonClicked);
                    bRightLEDButtonClicked = !bRightLEDButtonClicked;
                    break;
                case Key.W:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 200, RobotProtocol.DCMotorMode.FORWARD, 165);
                    break;
                case Key.A:
                    //await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 0, RobotProtocol.DCMotorMode.FORWARD, 150);
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.BACKWARD, 140, RobotProtocol.DCMotorMode.FORWARD, 80);

                    break;
                case Key.S:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.BACKWARD, 160, RobotProtocol.DCMotorMode.BACKWARD, 165);
                    break;
                case Key.D:
                    for(int i = 0; i < 14; ++i)
                    {
                        await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 70, RobotProtocol.DCMotorMode.BACKWARD, 140);
                    }
                    break;
            }
        }

        private async void onMoveLeftButton(object sender, RoutedEventArgs e)
        {
            await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 0, RobotProtocol.DCMotorMode.FORWARD, 150);
        }
        private async void onMoveRightButton(object sender, RoutedEventArgs e)
        {
            // 150, 0 
            //await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 80, RobotProtocol.DCMotorMode.BACKWARD, 130);
        }
        private async void onMoveForwardButton(object sender, RoutedEventArgs e)
        {
            await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.FORWARD, 200, RobotProtocol.DCMotorMode.FORWARD, 165);
        }
        private async void onMoveBackwardButton(object sender, RoutedEventArgs e)
        {
            await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.BACKWARD, 160, RobotProtocol.DCMotorMode.BACKWARD, 165);
        }
    }
}
