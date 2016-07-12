using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeController.Robot
{
    /// <summary>
    /// RobotController enables you to control the remote robot easily.
    /// You should attach RobotCommunicator to this class ! 
    /// Otherwise, this class is useless
    /// </summary>
    public partial class RobotController
    {
        /// <summary>
        /// Whether this controller has valid(communicatable) communicator or not
        /// </summary>
        public bool IsCommunicatorConnected
        {
            get
            {
                bool? bRet = Communicator?.Connected;
                if (bRet == null)
                {
                    return false;
                }
                else
                {
                    return bRet.Value;
                }
            }
        }

        /// <summary>
        /// Whether this 
        /// </summary>
        public bool IsAttached { get; private set; }

        /// <summary>
        /// Communicator which is used to communicate with the remote robot.
        /// </summary>
        protected RobotCommunicator Communicator { get; private set; }

        /// <summary>
        /// Designate communicator to this RobotController.
        /// Afterwards, this controller control the remote robot using this communicator.
        /// 
        /// [CAUTION] : If there exists attached communicator already, it returns fail !
        /// so you should detach task first.
        /// </summary>
        /// <param name="communicator"> to be attached communicator </param>
        /// <returns> false return case : 1. There exists attached communicator, 2. communicator is null, 3. communicator is not connected</returns>
        public bool AttachCommunicator(RobotCommunicator communicator)
        {
            bool bSucceeded = false;
            bool? bArgCommunicatorConnected = communicator?.Connected;

            // First Condition : There doesn't exist communicator which has already been attched and connected.
            // Second Condition: bSucceeded has a valid value(true or false)
            // Third Condition : and its value is true
            // It means communicator is not only valid but also it is connected
            if (IsCommunicatorConnected == false && bArgCommunicatorConnected.HasValue && bArgCommunicatorConnected.Value == true)
            {
                Communicator = communicator;
                bSucceeded = true;
            }
            // 
            // 1. There exists attached communicator
            // 2. communicator is null
            // 3. communicator is not connected 
            else
            {
                bSucceeded = false;
            }

            return bSucceeded;
        }        
        
        /// <summary>
        /// Just Detach the commnicator, it doesn't deallocate any resource related to the communicator
        /// you should manually dispose them.
        /// </summary>
        public void DetachCommunicator()
        {
            Communicator = null;
        }

        /// <summary>
        /// Detach communicator and close all resources related to the communicator
        /// </summary>
        public void DetachCommunicatorAndCloseIt()
        {
            Communicator?.Disconnect();

            Communicator = null;
        }

        

    }
}
