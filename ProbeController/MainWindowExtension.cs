using JHStreamReceiver;
using ImageCV2 = OpenCvSharp;
using ProbeController.Robot;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using ImageProcessing;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        // Constants related to frame
        private const int FRAME_WIDTH = 640;
        private const int FRAME_HEIGHT = 480;
        private const int FRAME_DPI_X = 96;
        private const int FRAME_DPI_Y = 96;

        private BackgroundWorker RealTimeStreamingWorker { get; }

        private AutoResetEvent streamHasFinishedEvent { get; }

        private async Task<bool> connectToStreamerAsync(string streamURL, StreamReceiver receiver)
        {
            Debug.Assert(streamURL != null && streamURL.Length > 0 && receiver != null && receiver.IsConnected == false);
            return await receiver.ConnectToURLAsync(streamURL);
        }

        private void RealTimeStreamWorkerRoutine(object sender, DoWorkEventArgs e)
        {
            Debug.Assert(e.Argument != null && e.Argument is StreamReceiver && ((StreamReceiver)e.Argument).IsConnected);

            BackgroundWorker thisWorker = sender as BackgroundWorker;
            StreamReceiver receiver = e.Argument as StreamReceiver;

            while (true)
            {
                var frameAsByteArray = receiver.GetFrameAsByteArray();
                using (var currentFrameMat = ImageCV2.Cv2.ImDecode(frameAsByteArray, ImageCV2.ImreadModes.Unchanged))
                {
                    // User requests this thread to be terminated, so grab the last image 
                    // in order to user to capture the last moment.
                    if (thisWorker.CancellationPending == true)
                    {
                        // 이 if문에서는 마지막 프레임을 캡쳐하는 기능을 수행한다.
                        e.Cancel = true;

                        // currentFrameMat이 곧 소멸될 것이므로 Clone해둔다. 
                        mGrappedMat = currentFrameMat.Clone();
                        mStreamReceiver.Disconnect();

                        // UI Thread에게, 현재프레임이 캡쳐됐으니, 이후 기능을 수행하라고 일러준다. 
                        streamHasFinishedEvent.Set();

                        // 그리고 이 async background thread는 종료한다.
                        break;
                    }
                    else
                    {
                        // only can UI Thread change UI contents
                        // So call Invoke method of Dispatcher, that method will be run on UI thread
                        Dispatcher.Invoke(() =>
                        {
                            if(Tracker.IsReadyToTrack == true)
                            {
                                using (var trackingResult = Tracker.DoTrackUsing(currentFrameMat))
                                {
                                    ImageCV2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(trackingResult.Frame, mWb);
                                }
                            }
                            else
                            {
                                ImageCV2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(currentFrameMat, mWb);
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Common Function to toggle LED 
        /// </summary>
        private async Task<bool> OrderToggleLEDAsync(RobotProtocol.LEDSide side)
        {
            bool bSucceeded = false;

            if (mRobotController != null)
            {
                bSucceeded = await mRobotController.ToggleLEDAsync(side);
            }
            else
            {
                bSucceeded = false;
            }

            return bSucceeded;
        }

        /// <summary>
        /// Common Function to rotate servo motors using 2 text boxes(2 theta text boxes)
        /// </summary>
        /// <param name="horizontalServoThetaText"> horizontal theta text box's Text </param>
        /// <param name="verticalServoThetaText"> vertical theta text box's Text </param>
        /// <returns> Check whether ordering rotation of servo motors did well or not </returns>
        private async Task<bool> OrderRotateServoMotorsUsingTextBoxesAsync(string horizontalServoThetaText, string verticalServoThetaText)
        {
            bool bSucceeded = true;
            double horizontalServoTheta, verticalServoTheta;

            // fetch the value of 2 text boxes (horizontal servo theta value, vertical servo theta value)
            if (double.TryParse(horizontalServoThetaText, out horizontalServoTheta) && double.TryParse(verticalServoThetaText, out verticalServoTheta))
            {
                bSucceeded &= await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Horizontal, horizontalServoTheta);
                bSucceeded &= await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Vertical, verticalServoTheta);
            }
            // when input values are not valid 
            else
            {
                bSucceeded = false;
            }

            return bSucceeded;
        }
    }
}
