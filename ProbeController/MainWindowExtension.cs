/*******************************************************************
 * 
 * This file deals with the internal methods, and logiccal components.
 * 
 * 
 *******************************************************************/

using JHStreamReceiver;
using ProbeController.Robot;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        // Constants related to frame
        private const int FRAME_WIDTH = 640;
        private const int FRAME_HEIGHT = 480;
        private const int FRAME_DPI_X = 96;
        private const int FRAME_DPI_Y = 96;
        
        /// <summary>
        /// whether the streaming job is working or not.
        /// </summary>
        public bool bIsCameraWorking = false;
        
        /// <summary>
        /// Common Function to toggle LED 
        /// </summary>
        private async Task<bool> OrderToggleLEDAsync(RobotProtocol.LEDSide side)
        {
            bool bSucceeded = false;

            if(mRobotController != null)
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
            if (double.TryParse(horizontalServoThetaText, out horizontalServoTheta) == true && double.TryParse(verticalServoThetaText, out verticalServoTheta))
            {
                bSucceeded &= await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorsSide.Horizontal, horizontalServoTheta);
                bSucceeded &= await mRobotController.RotateServoMotorsAsync(RobotProtocol.ServoMotorsSide.Vertical, verticalServoTheta);

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
