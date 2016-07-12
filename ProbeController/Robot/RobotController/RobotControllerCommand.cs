using System.Threading.Tasks;
using System.Windows;

namespace ProbeController.Robot
{
    // this partial class copes with command related methods
    public partial class RobotController
    {
        public readonly Vector FACEVECTOR = new Vector(0, 1);

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

        //public async Task<bool> MoveRobot(Vector directionVector, double duration)
        //{
        //    directionVector.Normalize();
                
        //}

        public async Task<bool> FaceRobotUsingVector(Vector directionVector)
        {
            double theta = Vector.AngleBetween(FACEVECTOR, directionVector);
            
        }

    }
}
