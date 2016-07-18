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
using System.Windows.Controls;

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

        // user defined frame snippet (subset of a frame)
        private Cv2.Point mFrameSnippetLocation;
        private Cv2.Size mFrameSnippetSize;

        private bool mbGrapping;
        private Cv2.Mat mGrappedFrameMat;

        private static string STREAM_URL = "http://devjhlab.iptime.org:8080/?action=stream";

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

            //mFrameSnippetBottomRightLocaiton = new Cv2.Point(0.0f, 0.0f);
            mFrameSnippetLocation = new Cv2.Point(0.0f, 0.0f);
            mFrameSnippetSize = new Cv2.Size(0.0f, 0.0f);

            verticalServoTextBox.Text = "0";
            horizontalServoTextBox.Text = "0";

            mbGrapping = false;
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

            e.Handled = true;
            // initate connection to remote camera using ConnectToURLAsync()
            bSuccess = await mStreamReceiver.ConnectToURLAsync(STREAM_URL);
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

            startStreamButton.Content = "Working..";
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

            if (mbGrapping == true)
            {
                mGrappedFrameMat = await mStreamReceiver.GetFrameAsMatAsync();
                Console.WriteLine("{0},{1}", mGrappedFrameMat.Width, mGrappedFrameMat.Height);
            }

            // after the while loop expired, streamReceiver should be disconnected.
            mStreamReceiver.Disconnect();
        }
        private void onEndStreamButton(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // camera should turn off
            bIsCameraWorking = false;

            startStreamButton.Content = "Start Stream";
            startStreamButton.IsEnabled = true;
            endStreamButton.IsEnabled = false;
        }
        private void onGaussianBlurButton(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void onCannyButton(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void onConnectButton(object sender, RoutedEventArgs e)
        {
            int portNumber;
            string ipAddress;

            e.Handled = true;
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
            e.Handled = true;

            connectButton.IsEnabled = true;
            disconnectButton.IsEnabled = false;
            
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
                    //await mCommunicator.IssueLEDCommandAsync(RobotProtocol.LEDSide.Left, bLeftLEDButtonClicked);
                    //bLeftLEDButtonClicked = !bLeftLEDButtonClicked;
                    break;
                case Key.F4:
                    //await mCommunicator.IssueLEDCommandAsync(RobotProtocol.LEDSide.Right, bRightLEDButtonClicked);
                    //bRightLEDButtonClicked = !bRightLEDButtonClicked;
                    break;
                case Key.W:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 200, RobotProtocol.DCMotorMode.Forward, 165);
                    break;
                case Key.A:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 0, RobotProtocol.DCMotorMode.Forward, 150);
                    //await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Backward, 140, RobotProtocol.DCMotorMode.Forward, 80);
                    break;
                case Key.S:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Backward, 160, RobotProtocol.DCMotorMode.Backward, 165);
                    break;
                case Key.D:
                    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 150, RobotProtocol.DCMotorMode.Forward, 0);
                    //for(int i = 0; i < 14; ++i)
                    //{
                    //    await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 70, RobotProtocol.DCMotorMode.Backward, 140);
                    //}
                    break;
            }
        }
        
        /**********************************************************************/
        /*                                                                    */
        /*       Grabbing Snippet Image Event Handlers (Drag Feature)         */
        /*                                                                    */
        /**********************************************************************/
        private void onMouseUpAtFrame(object sender, MouseButtonEventArgs e)
        {
            var mouseUpPosition = e.GetPosition(sender as IInputElement);
            e.Handled = true;
            
            // only when user is now grapping an image.. calculate the region.
            if (mbGrapping == true)
            {
                // calculate the Width and Height of snippet frame.
                mFrameSnippetSize.Width = (int)Math.Abs(mouseUpPosition.X - mFrameSnippetLocation.X);
                mFrameSnippetSize.Height = (int)Math.Abs(mouseUpPosition.Y - mFrameSnippetLocation.Y);

                // calculate the effective origin(top left corner point). 
                if ((int)mouseUpPosition.X <= mFrameSnippetLocation.X)
                {
                    mFrameSnippetLocation.X = (int)mouseUpPosition.X;
                }

                if ((int)mouseUpPosition.Y <= mFrameSnippetLocation.Y)
                {
                    mFrameSnippetLocation.Y = (int)mouseUpPosition.Y;
                }

                // update bottom dashboard
                snippetOriginLabel.Content = string.Format("({0}, {1})", mFrameSnippetLocation.X, mFrameSnippetLocation.Y);
                snippetSizeLabel.Content = string.Format("({0}, {1})", mFrameSnippetSize.Width, mFrameSnippetSize.Height);

                // make subset of last 
                if (mGrappedFrameMat != null)
                {
                    // ROI
                    if (mFrameSnippetSize.Height == 0 || mFrameSnippetSize.Width == 0)
                    {
                        mFrameSnippetSize.Height = mFrameSnippetSize.Width = 1;
                    }

                    mGrappedFrameMat = mGrappedFrameMat?.SubMat(new Cv2.Rect(mFrameSnippetLocation, mFrameSnippetSize));
                }

                // Grapping is done
                mbGrapping = false;

                // restart streaming 
                onStartStreamButton(startStreamButton, e);
            }
        }
        private void onMouseDownAtFrame(object sender, MouseButtonEventArgs e)
        {
            var mouseDownPosition = e.GetPosition(sender as IInputElement);
            e.Handled = true;

            // only when user starts to pick an part of the frame.
            if (bIsCameraWorking == true)
            {
                // release the old grapped frame only when mGrappedFrameMat is not null.
                mGrappedFrameMat?.Release();

                // get start location.
                mFrameSnippetLocation.X = (int)mouseDownPosition.X;
                mFrameSnippetLocation.Y = (int)mouseDownPosition.Y;
                
                // set grapping flag to true
                mbGrapping = true;

                // pause streaming
                bIsCameraWorking = false;
                // modify the content of start stream button
                startStreamButton.Content = "Grabbing..";
            }
        }
        /// <summary>
        /// Grab Button Event Handler
        /// </summary>
        /// <param name="sender"> A reference to Grab Button Control </param>
        /// <param name="e"> routed event args </param>
        private void onGrabButton(object sender, RoutedEventArgs e)
        {
            // Modal Window
            GrabWindow grabWindow = null;
            e.Handled = true;

            // if there exists an grapped mat
            if (mGrappedFrameMat != null)
            {
                // initialize GrabWindow by using grapped matrix(it should be cloned)
                grabWindow = new GrabWindow(mGrappedFrameMat.Clone());
                grabWindow.Owner = this;

                // release the grabpped mat since there is no need to use
                mGrappedFrameMat.Release();

                // make it as a null
                mGrappedFrameMat = null;

                // show dialog
                bool? bCloseWell = grabWindow.ShowDialog();

                // check if there is an error
                if (bCloseWell.HasValue && bCloseWell == true)
                {
                    Console.WriteLine("Okay!");
                }
            }
            else
            {
                MessageBox.Show("You should select the region!!!");
            }
        }

        /**********************************************************************/
        /*                                                                    */
        /*               Servo Motor GroupBox Event Handlers                  */
        /*                                                                    */
        /**********************************************************************/
        /// <summary>
        /// ServoMotor Confirm Button Event handler
        /// </summary>
        /// <param name="sender"> A reference to confirm button control</param>
        /// <param name="e"> Event args </param>
        private async void onServoConfirmButton(object sender, RoutedEventArgs e)
        {
            bool bDidWell = true;

            double horizontalServoTheta;
            double verticalServoTheta;
            e.Handled = true;

            // fetch the value of 2 text boxes (horizontal servo theta value, vertical servo theta value)
            if (double.TryParse(horizontalServoTextBox.Text, out horizontalServoTheta) == true && double.TryParse(verticalServoTextBox.Text, out verticalServoTheta))
            {
                bDidWell &= await mRobotController.RotateServoMotors(RobotProtocol.ServoMotorsSide.Horizontal, horizontalServoTheta);
                bDidWell &= await mRobotController.RotateServoMotors(RobotProtocol.ServoMotorsSide.Vertical, verticalServoTheta);
                
                if (bDidWell == false)
                {
                    MessageBox.Show("Rotation of servo motors has failed.. ");
                }
            }
            else
            {
                MessageBox.Show("Either horizontal theta or vertical theta is invalid.. Check Connection!");
            }

        }
        private async void onHorizontalServoSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            const int MAX_HORIZONTAL_SERVO_THETA = 60;
            bool bSucceeded = false;
            double newTheta = 12 * e.NewValue - MAX_HORIZONTAL_SERVO_THETA;
            e.Handled = true;

            if (mRobotController != null)
            {
                bSucceeded = await mRobotController.RotateServoMotors(RobotProtocol.ServoMotorsSide.Horizontal, newTheta);
                if (bSucceeded == true)
                {
                    horizontalServoTextBox.Text = Convert.ToString(newTheta);
                }
                else
                {
                    MessageBox.Show("Servo Motor Command has failed.");
                }
            }
        }
        private async void onVerticalServoSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            const int MAX_VERTICAL_SERVO_DOWN_THETA = 10;
            bool bSucceeded = false;
            double newTheta = -10 * e.NewValue + MAX_VERTICAL_SERVO_DOWN_THETA;
            e.Handled = true;

            if (mRobotController != null)
            {
                bSucceeded = await mRobotController.RotateServoMotors(RobotProtocol.ServoMotorsSide.Vertical, newTheta);
                if (bSucceeded == true)
                {
                    verticalServoTextBox.Text = Convert.ToString(newTheta);
                }
                else
                {
                    MessageBox.Show("Servo Motor Command has failed.");
                }
            }
        }

        /**********************************************************************/
        /*                                                                    */
        /*                DC Motor GroupBox Event Handlers                    */
        /*                                                                    */
        /**********************************************************************/
        /// <summary>
        /// DC Motor GroupBox Common Event Handler
        /// </summary>
        /// <param name="sender"> It is a reference to DC Motor GroupBox  </param>
        /// <param name="e"> Routed Event </param>
        private async void onDCMotorGroupBoxCommonHandler(object sender, RoutedEventArgs e)
        {
            var eventSource = e.Source as FrameworkElement;
            bool bSucceeded = false;
            e.Handled = true;

            switch(eventSource.Name)
            {
                // DC Motor Related Buttons
                case "MoveLeftButton":
                    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 0, RobotProtocol.DCMotorMode.Forward, 150);
                    break;
                case "MoveRightButton":
                    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 80, RobotProtocol.DCMotorMode.Backward, 130);
                    break;
                case "MoveForwardButton":
                    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 200, RobotProtocol.DCMotorMode.Forward, 165);
                    break;
                case "MoveBackwardButton":
                    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Backward, 160, RobotProtocol.DCMotorMode.Backward, 165);
                    break;
            }
            if (bSucceeded == false)
            {
                // inform the user that task has failed.
                MessageBox.Show("DC Motor Command has failed..");
            }
        }

        /**********************************************************************/
        /*                                                                    */
        /*                  LED GroupBox Event Handlers                       */
        /*                                                                    */
        /**********************************************************************/
        /// <summary>
        /// LED GroupBox Common Event Handler
        /// </summary>
        /// <param name="sender"> It is a reference to LED GroupBox </param>
        /// <param name="e"> Routed Event </param>
        private async void onLEDGroupBoxCommonHandler(object sender, RoutedEventArgs e)
        {
            var eventSource = e.Source as FrameworkElement;
            bool bSucceeded = false;

            e.Handled = true;

            // identify the name of the source that raised event  
            switch(eventSource.Name)
            {
                case "leftLEDButton":
                    bSucceeded = await mRobotController.ToggleLED(RobotProtocol.LEDSide.Left);
                    break;
                case "rightLEDButton":
                    bSucceeded = await mRobotController.ToggleLED(RobotProtocol.LEDSide.Right);
                    break;
            }
            
            // When toggling LED task has failed, show user a messagebox
            if (bSucceeded == false)
            {
                MessageBox.Show("LED Command failed!!!");
            }
        }
    }
}
