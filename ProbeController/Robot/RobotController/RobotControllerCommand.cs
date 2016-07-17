using System;
using System.Threading.Tasks;
using System.Windows;

namespace ProbeController.Robot
{
    // this partial class copes with command related methods
    public partial class RobotController
    {
        public readonly Vector FACEVECTOR = new Vector(0, 1);
        protected enum MoveDirection { LEFT, RIGHT, STILL, UNDEF };

        /// <summary>
        /// Turn on Robot's LED
        /// </summary>
        /// <param name="side"> Left side or Right side</param>
        /// <param name="bOn"> Turn on or not</param>
        /// <returns> whether the command has been successfully submitted or not </returns>
        public async Task<bool> TurnOnLED(RobotProtocol.LEDSide side, bool bOn)
        {
            bool bSucceeded = false;
            string madeJSONCommand = RobotProtocol.MakeLEDCommand(side, bOn);

            // if made json command is invalid, return false! 
            if (madeJSONCommand == null)
            {
                bSucceeded = false;
            }
            // if made json command is valid, send this command to remote robot using Communicator(RobotCommunicator)
            else
            {
                // asynchronously send made json command to the remote device
                bSucceeded = await Communicator.SendJSONStringAsnyc(madeJSONCommand);
            }

            // return whether this function works well or not
            return bSucceeded;
        }
        
        /// <summary>
        /// Rotate Servo Motor which is specified by the first argument
        /// and how much it'll roate by is given by the second argument
        /// 
        /// </summary>
        /// <param name="whatMotor"> horizontal servo or vertical servo </param>
        /// <param name="theta"> the value of theta </param>
        /// <returns> whether this task succeeded or not </returns>
        public async Task<bool> RotateServoMotors(RobotProtocol.ServoMotorsSide whatMotor, double theta)
        {
            bool bSucceeded = false;

            var dutyCycleDouble = (whatMotor == RobotProtocol.ServoMotorsSide.Horizontal) ? (-2 * theta + 451) : (-2.27 * theta + 309);

            // theta to duty cycle function is needed ... 
            var numDutyCycle = (int)Math.Floor(dutyCycleDouble);

            string madeJSONCommand = RobotProtocol.MakeServoMotorsCommand(whatMotor, numDutyCycle);
            
            if (madeJSONCommand == null)
            {
                bSucceeded = false;
            }
            else
            {
                bSucceeded = await Communicator.SendJSONStringAsnyc(madeJSONCommand);
            }
            return bSucceeded;
        }

        public async Task<bool> FaceRobotUsingVector(Vector directionVector)
        {
            // dot product between FACEVECTOR(0,1) and directionVector to get theta value
            // also the sign of theta indicates the direction to which this robot should go
            // ex) theta > 0 --> left
            // ex) theta < 0 --> right
            // ex) theta = 0 --> still
            double theta = Vector.AngleBetween(FACEVECTOR, directionVector);

            // when theta is greater than 0, it means go to the left direction
            if (theta > 0)
            {
                
            }
            // it means go to the right direction
            // 14 times , (forward,70, backward,130) --> about 90 degrees
            // 7 times , (forward 70 backward 130) --> about 45 degrees
            else if(theta < 0)
            {

            }
            // when theta is 0, stay still
            else
            {
                
            }

            return true;
        }
        protected MoveDirection getDirectionBasedOnVector(Vector targetVector)
        {
            MoveDirection retDirection = MoveDirection.UNDEF;

            // when targetVector is toward up right to the origin
            if (targetVector.X == 0 && targetVector.Y >= 0)
            {
                retDirection = MoveDirection.STILL;
            }
            else if (targetVector.X >= 0)
            {
                retDirection = MoveDirection.RIGHT;
            }
            else
            {
                retDirection = MoveDirection.LEFT;
            }

            return retDirection;
        }
        protected int getDutyCycleWhenThetaIs(double theta)
        {
            int retDutyCycle = 0;




            return retDutyCycle;
        }
    }
}
