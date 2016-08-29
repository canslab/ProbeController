using System.Threading.Tasks;
using System.Windows;

namespace ProbeController.Robot
{
    // this partial class copes with command related methods
    // This also contains the status of robot (such as LED status) 
    public partial class RobotController
    {
        protected enum MoveDirection { LEFT, RIGHT, STILL, UNDEF };

        public double CurrentHorizontalDegress { get; private set; }
        public double CurrentVerticalDegrees { get; private set; }

        /// <summary>
        /// Whether the left side LED is turned on or not 
        /// </summary>
        public bool IsLeftLEDOn { get; private set; } = false;

        /// <summary>
        /// Whether the right side LED is turned on or not 
        /// </summary>
        public bool IsRightLEDOn { get; private set; } = false;

        /// <summary>
        /// Toggle LED using TurnOnLED
        /// </summary>
        /// <param name="side"> Which side user want to toggle, (Left or Right) </param>
        /// <returns> The result of this method </returns>
        public async Task<bool> ToggleLEDAsync(RobotProtocol.LEDSide side)
        {
            bool bSucceeded = false;

            // If turning on led task has been done successfully, toggle the corresponding property
            // otherwise, let it be.
            if (side == RobotProtocol.LEDSide.Left)
            {
                // invoke TurnOnLED method asynchronosuly.
                bSucceeded = await TurnOnLEDAsync(side, !IsLeftLEDOn);
                IsLeftLEDOn = (bSucceeded == true) ? !IsLeftLEDOn : IsLeftLEDOn;
            }
            else if (side == RobotProtocol.LEDSide.Right)
            {
                bSucceeded = await TurnOnLEDAsync(side, !IsRightLEDOn);
                IsRightLEDOn = (bSucceeded == true) ? !IsRightLEDOn : IsRightLEDOn;
            }

            return bSucceeded;
        }
        
        /// <summary>
        /// Rotate Servo Motor which is specified by the first argument
        /// and how much it'll roate by is given by the second argument
        /// 
        /// </summary>
        /// <param name="side"> horizontal servo or vertical servo </param>
        /// <param name="theta"> the value of theta </param>
        /// <returns> whether this task succeeded or not </returns>
        public async Task<bool> RotateServoMotorsAsync(RobotProtocol.ServoMotorSide side, double theta)
        {
            bool bSucceeded = false;
            uint numDutyCycle = 0;
            string madeJSONCommand = null;
            
            // First of all, can we send a message by checking CanCommunicate property
            if (CanCommunicate)
            {
                // get duty cycle that corresponds to the given theta 
                numDutyCycle = getDutyCycleWhenThetaIs(side, theta);

                // make JSON command 
                madeJSONCommand = RobotProtocol.Command.CreateServoMotorCommand(side, numDutyCycle);

                if (madeJSONCommand == null)
                {
                    bSucceeded = false;
                }
                else
                {
                    bSucceeded = await Communicator.SendJSONStringAsnyc(madeJSONCommand);
                    if (bSucceeded == true)
                    {
                        if (side == RobotProtocol.ServoMotorSide.Horizontal)
                        {
                            CurrentHorizontalDegress = theta;
                        }
                        else if(side == RobotProtocol.ServoMotorSide.Vertical)
                        {
                            CurrentVerticalDegrees = theta;
                        }
                    }
                }
            }
            return bSucceeded;
        }

        /// <summary>
        /// Turn on Robot's LED
        /// </summary>
        /// <param name="side"> Left side or Right side</param>
        /// <param name="bOn"> Turn on or not</param>
        /// <returns> whether the command has been successfully submitted or not </returns>
        protected async Task<bool> TurnOnLEDAsync(RobotProtocol.LEDSide side, bool bOn)
        {
            bool bSucceeded = false;

            // First of all, CanCommunicate should be true in order to send command 
            if (CanCommunicate)
            {
                // retrieve LED json command
                // CommandFactory.CreateLEDCommand()
                string madeJSONCommand = RobotProtocol.Command.CreateLEDCommand(side, bOn);

                // if made json command is invalid, bSucceeded must be assigned to false
                if (madeJSONCommand == null)
                {
                    bSucceeded = false;
                }
                // otherwise, invoke SendJSONStringAsync()
                else
                {
                    bSucceeded = await Communicator.SendJSONStringAsnyc(madeJSONCommand);
                }
            }
            
            // return whether this function works well or not
            return bSucceeded;
        }

        /// <summary>
        /// Transforms theta to corresponding duty cycle
        /// </summary>
        /// <param name="side"> Which side do you want to get duty cycle </param>
        /// <param name="theta"> Theta value</param>
        /// <returns> Corresponding duty cycle </returns>
        protected uint getDutyCycleWhenThetaIs(RobotProtocol.ServoMotorSide side ,double theta)
        {
            return (uint)((side == RobotProtocol.ServoMotorSide.Horizontal) ? (-2 * theta + 451) : (-2.27 * theta + 309));
        }
    }
}