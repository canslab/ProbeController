using JHStreamReceiver;
using ImageCV2 = OpenCvSharp;
using ProbeController.Robot;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using ImageProcessing;
using System;
using System.Threading;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        // Constants related to frame
        private const int FRAME_WIDTH = 640;
        private const int FRAME_HEIGHT = 480;
        private const int FRAME_DPI_X = 96;
        private const int FRAME_DPI_Y = 96;
        private struct VHDegrees
        {
            public double hTheta;
            public double vTheta;
        }

        private int TotalSearchCount { get; set; }
        private int SearchIndex { get; set; }

        private StreamWorker RealTimeStreamingWorker { get; set; }
        private VHDegrees[] SearchOrder { get; set; }

        /// <summary>
        /// 물체가 사라졌을 때 다시 탐색해야하는데, 그 탐색 경로(각도들)을 만든다.
        /// </summary>
        /// <param name="searchOrder"> searchOrder에 그 각도들이 담긴다.</param>
        private VHDegrees[] MakeSearchOrder(int nRow, int nCol)
        {
            const int vThetaCoeff = -15;
            int hThetaCoeff = -30;

            VHDegrees[] retOrder = new VHDegrees[nRow * nCol * 2];

            for (int row = 0; row < nRow * 2; ++row)
            {
                for (int col = 0; col < nCol; ++col)
                {
                    if (row < nCol)
                    {
                        retOrder[row * nCol + col].vTheta = vThetaCoeff + (-vThetaCoeff * row);
                        retOrder[row * nCol + col].hTheta = hThetaCoeff + (-hThetaCoeff * col);
                    }
                    else
                    {
                        // (5 - row) * nCol + ( 2 - col) 
                        // 
                        retOrder[row * nCol + col] = retOrder[((nRow * 2 - 1) - row) * nCol + (nCol - 1 - col)];
                    }
                }
                hThetaCoeff = -hThetaCoeff;
            }

            return retOrder;
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

        // 트랙킹 된 후에는, 아래 함수가 호출된다.
        public async Task OnReceivedTrackingResult(int centerX, int centerY, bool IsExistTarget, double stdev, AutoResetEvent trackingSynchronizer)
        {
            // trackingSynchronizer에 set을 하기전까지는, streaming이 진행되지 않는다.
            if (IsExistTarget == true)
            {
#if MY_DEBUG
                Console.WriteLine("x = {0}, y = {1} stdev = {2}", centerX, centerY, stdev);
#endif
                var offsetX = centerX - 320;
                var offsetY = 240 - centerY;

                double hThetaRadians = Math.Atan2(offsetX, 512);
                var hDiffDegress = Math.Floor(hThetaRadians * (180.0 / Math.PI));

                double vThetaRadians = Math.Atan2(offsetY * Math.Cos(hThetaRadians), 512);
                var vDiffDegress = -Math.Floor(vThetaRadians * (180.0 / Math.PI));
#if MY_DEBUG
                Console.WriteLine("vtheta = {0}, hTheta = {1}", vDiffDegress, hDiffDegress);
#endif

                if (Math.Abs(hDiffDegress) >= 2)
                {
                    var hFianlDegrees = mRobotController.CurrentHorizontalDegress + hDiffDegress;
                    await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Horizontal, hFianlDegrees);
                }
                if (Math.Abs(vDiffDegress) >= 2)
                {
                    var vFianlDegrees = mRobotController.CurrentVerticalDegrees + vDiffDegress;
                    await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Vertical, vFianlDegrees);
                }
                // 다시 토탈 서치카운트는 원상태로! (2 바퀴 돈다)
                TotalSearchCount = SearchOrder.Length;
            }
            else
            {
                // 물체가 없으면 두리번 거리기는 한다..
                if (TotalSearchCount == 0)
                {
                    MessageBox.Show("물체가 없어요...다시 트랙킹해주세요");
                    PauseTracking();
                    TotalSearchCount = SearchOrder.Length;
                }
                else
                {
                    // 물체가 존재하지 않으므로 탐색을 수행해야 합니다.
                    // 수직각도는 0도로..
#if MY_DEBUG
                    Console.WriteLine(" 탐색 모드로 전환!");
                    Console.WriteLine(" No Target stdev={0}", stdev);
#endif

                    await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Vertical, SearchOrder[SearchIndex].vTheta);
                    await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorSide.Horizontal, SearchOrder[SearchIndex].hTheta);
                    Thread.Sleep(100);

                    SearchIndex = (SearchIndex + 1) % SearchOrder.Length;
                    TotalSearchCount--;
                }
            }

            RealTimeStreamingWorker.ChangeToNormalMode();
            trackingSynchronizer.Set();
        }



    }
}
