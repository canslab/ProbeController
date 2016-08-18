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
using ImageCV2 = OpenCvSharp;
using System.Diagnostics;
using ImageProcessing;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap mWb;
        private StreamReceiver mStreamReceiver;     // Stream Receiver
        private RobotController mRobotController;   // Robot Controller
        private ImageCV2.Mat mGrappedMat;

        private ObjectTracker Tracker
        {
            get;
        }

        // user defined frame snippet (subset of a frame)
        public GrabWindow.GrabWindowResult GrapWindowResult
        {
            get; set;
        }

        private readonly string STREAM_URL = "http://devjhlab.iptime.org:8080/?action=stream";

        public MainWindow()
        {
            InitializeComponent();
            mWb = new WriteableBitmap(FRAME_WIDTH, FRAME_HEIGHT, FRAME_DPI_X, FRAME_DPI_Y, PixelFormats.Bgr24, null);
            mStreamReceiver = new StreamReceiver();

            // initialization of background worker
            RealTimeStreamingWorker = new System.ComponentModel.BackgroundWorker();
            RealTimeStreamingWorker.WorkerSupportsCancellation = true;
            RealTimeStreamingWorker.DoWork += RealTimeStreamWorkerRoutine;

            streamHasFinishedEvent = new System.Threading.AutoResetEvent(false);

            frame.Stretch = Stretch.None;
            frame.Source = mWb;

            endStreamButton.IsEnabled = false;
            disconnectButton.IsEnabled = false;
            mRobotController = new RobotController();

            verticalServoTextBox.Text = "0";
            horizontalServoTextBox.Text = "0";

            startTrackingButton.IsEnabled = false;
            Tracker = ObjectTracker.Instance;
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
            Debug.Assert(RealTimeStreamingWorker.IsBusy == false);
            bool bSucceeded = false;
            e.Handled = true;

            bSucceeded = await connectToStreamerAsync(STREAM_URL, mStreamReceiver);
            if (bSucceeded == false)
            {
                MessageBox.Show("streaming connection has failed..", "Streaming Connection Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RealTimeStreamingWorker.RunWorkerAsync(mStreamReceiver);
            changeUIWhenStreamingOK();
        }
        private void changeUIWhenStreamingOK()
        {
            startStreamButton.Content = "Working";
            startStreamButton.IsEnabled = false;
            endStreamButton.IsEnabled = true;
        }
        private void changeUIWhenStreamingEnd()
        {
            startStreamButton.Content = "Start Stream";
            startStreamButton.IsEnabled = true;
            endStreamButton.IsEnabled = false;
        }

        private void onEndStreamButton(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            // Cancel streaming task.
            RealTimeStreamingWorker.CancelAsync();
            changeUIWhenStreamingEnd();
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
            int remoteRobotPortNumber;
            string remoteRobotIPAddress;
            bool bConnectionSucceded = false;

            e.Handled = true;
            remoteRobotIPAddress = ipTextBox.Text;

            try
            {
                remoteRobotPortNumber = int.Parse(portTextBox.Text);
            }
            catch (Exception argException)
            {
                MessageBox.Show("Robot Connection Failed due to input info is not valid... RSN> " + argException.Message);
                return;
            }

            bConnectionSucceded = await mRobotController.ConnectAsync(ipTextBox.Text, remoteRobotPortNumber);

            if (bConnectionSucceded == false)
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
            mRobotController.Disconnect();
            e.Handled = true;

            connectButton.IsEnabled = true;
            disconnectButton.IsEnabled = false;
        }

        private async void onMainWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (mRobotController.CanCommunicate)
            {
                bool bSucceeded = true;
                switch (e.Key)
                {
                    case Key.F3:
                        bSucceeded = await OrderToggleLEDAsync(RobotProtocol.LEDSide.Left);
                        e.Handled = true;
                        break;
                    case Key.F4:
                        bSucceeded = await OrderToggleLEDAsync(RobotProtocol.LEDSide.Right);
                        e.Handled = true;
                        break;
                    //case Key.W:
                    //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 200, RobotProtocol.DCMotorMode.Forward, 165);
                    //    break;
                    //case Key.A:
                    //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 0, RobotProtocol.DCMotorMode.Forward, 150);
                    //    break;
                    //case Key.S:
                    //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Backward, 160, RobotProtocol.DCMotorMode.Backward, 165);
                    //    break;
                    //case Key.D:
                    //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 150, RobotProtocol.DCMotorMode.Forward, 0);
                    //    break;

                    case Key.Left:
                        horinzontalServoSlider.Value -= 0.2;
                        break;
                    case Key.Right:
                        horinzontalServoSlider.Value += 0.2;
                        break;
                    case Key.Up:
                        verticalServoSlider.Value += 0.2;
                        break;
                    case Key.Down:
                        verticalServoSlider.Value -= 0.2;
                        break;

                }

                if (bSucceeded == false)
                {
                    MessageBox.Show("Keyboard command has failed...");
                }
            }
        }

        /**********************************************************************/
        /*                                                                    */
        /*       Grabbing Snippet Image Event Handlers (Drag Feature)         */
        /*                                                                    */
        /**********************************************************************/

        private void onGrapButton(object sender, RoutedEventArgs e)
        {
            // Modal Window
            GrabWindow grabWindow = null;
            e.Handled = true;

            // Cancel Streaming Task
            RealTimeStreamingWorker.CancelAsync();

            // wait until RealTimeStreamingWorker has finished..
            streamHasFinishedEvent.WaitOne();

            // 마지막 프레임을 얻어와서 mGrappedMat에 저장되어 있다.
            grabWindow = new GrabWindow(mGrappedMat);
            grabWindow.Owner = this;
            grabWindow.ShowDialog();
            Console.WriteLine("Grap Window 닫힘!");

            mGrappedMat.Release();
            mGrappedMat = null;
            
            // GrapWindow의 결과를 저장한다. 쓰고 나서 반드시 Dispose해줘야 한다.
            GrapWindowResult = grabWindow.Result;
            startTrackingButton.IsEnabled = true;

            // restart streaming
            onStartStreamButton(sender, e);
        }

        /**********************************************************************/
        /*                                                                    */
        /*               Servo Motor GroupBox Event Handlers                  */
        /*                                                                    */
        /**********************************************************************/
        private async void onServoConfirmButton(object sender, RoutedEventArgs e)
        {
            bool bSucceeded = await OrderRotateServoMotorsUsingTextBoxesAsync(horizontalServoTextBox.Text, verticalServoTextBox.Text);

            if (bSucceeded == false)
            {
                MessageBox.Show("Rotate Servo Motor Command has failed..");
            }
        }
        private async void onKeyDownFromServoTextBoxes(object sender, KeyEventArgs e)
        {
            // when user pressed enter key
            if (e.Key == Key.Enter)
            {
                bool bSucceded = await OrderRotateServoMotorsUsingTextBoxesAsync(horizontalServoTextBox.Text, verticalServoTextBox.Text);
                if (bSucceded == false)
                {
                    MessageBox.Show("Rotate Servo Motor Command has failed..");
                }
            }
            else if ((e.Key != Key.OemMinus && e.Key != Key.Tab) && (e.Key < Key.D0 || e.Key > Key.D9))
            {
                e.Handled = true;
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
                bSucceeded = await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Horizontal, newTheta);
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
                bSucceeded = await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Vertical, newTheta);
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
        private async void onDCMotorGroupBoxCommonHandler(object sender, RoutedEventArgs e)
        {
            var eventSource = e.Source as FrameworkElement;
            bool bSucceeded = false;
            e.Handled = true;

            switch (eventSource.Name)
            {
                // DC Motor Related Buttons
                //case "MoveLeftButton":
                //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 0, RobotProtocol.DCMotorMode.Forward, 150);
                //    break;
                //case "MoveRightButton":
                //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 80, RobotProtocol.DCMotorMode.Backward, 130);
                //    break;
                //case "MoveForwardButton":
                //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Forward, 200, RobotProtocol.DCMotorMode.Forward, 165);
                //    break;
                //case "MoveBackwardButton":
                //    bSucceeded = await mCommunicator.IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode.Backward, 160, RobotProtocol.DCMotorMode.Backward, 165);
                //    break;
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
        private async void onLEDGroupBoxCommonHandler(object sender, RoutedEventArgs e)
        {
            var eventSource = e.Source as FrameworkElement;
            bool bSucceeded = false;

            e.Handled = true;

            // identify the name of the source that raised event  
            switch (eventSource.Name)
            {
                case "leftLEDButton":
                    bSucceeded = await OrderToggleLEDAsync(RobotProtocol.LEDSide.Left);
                    break;
                case "rightLEDButton":
                    bSucceeded = await OrderToggleLEDAsync(RobotProtocol.LEDSide.Right);
                    break;
            }

            // When toggling LED task has failed, show user a messagebox
            if (bSucceeded == false)
            {
                MessageBox.Show("LED Command failed!!!");
            }
        }

        private void onStartTrackingButtonClicked(object sender, RoutedEventArgs e)
        {
            // 여기서 이제 GrappedMat을 가지고 Tracking 작업을 수행해야 한다.
            // Tracker의 값들을 설정한다. 
            Tracker.SetModelImageAsMat(GrapWindowResult.ROIFrame, new int[] { 0, 255 }, new int[] { 0, 1 }, new int[] { 180, 256 }, ObjectTracker.hsRanges);
            Tracker.SetInitialTrackingWindowProperties(GrapWindowResult.ROIRect);
            Tracker.MaxIterationCount = 10;
            Tracker.Epsilon = 1;
        }

        private void onPauseTrackingButtonClicked(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
