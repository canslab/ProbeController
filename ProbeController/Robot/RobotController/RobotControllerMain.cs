using System.Threading.Tasks;

namespace ProbeController.Robot
{
    /// <summary>
    /// RobotController enables you to control the remote robot easily.
    /// The only way to issue commands is to use this class.
    /// It is equivalent to the remote robot.
    /// It has statuses of all devices of robot.
    /// </summary>
    public partial class RobotController
    {
        /// <summary>
        /// Whether this controller has valid(communicatable) communicator or not
        /// </summary>
        public bool CanCommunicate
        {
            get
            {
                bool? bRet = Communicator?.Connected;
                if (bRet == null)   // check whether Communicator is null or not
                {
                    return false;
                }
                else // if not null, return its connection status
                {
                    return bRet.Value;
                }
            }
        }

        /// <summary>
        /// Communicator which is used to communicate with the remote robot.
        /// </summary>
        protected RobotCommunicator Communicator { get; private set; }

        /// <summary>
        /// Add Communicator using given url address(=ipAddress), port number
        /// </summary>
        /// <param name="remoteURL">URL of the remote robot</param>
        /// <param name="remotePortNumber">Port Number of the remote robot</param>
        /// <returns> Succeded or not </returns>
        public async Task<bool> AddCommunicatorAsync(string remoteURL, int remotePortNumber)
        {
            bool bSucceeded = false;

            Communicator = new RobotCommunicator();
            bSucceeded =  await Communicator.ConnectToURLAsync(remoteURL, remotePortNumber);

            return bSucceeded;
        }

        /// <summary>
        /// Designate communicator to this RobotController.
        /// Afterwards, this controller control the remote robot using this communicator.
        /// 
        /// [CAUTION] : If there exists attached communicator already, it returns fail !
        /// so you should detach task first.
        /// </summary>
        /// <param name="communicator"> to be attached communicator </param>
        /// <returns> false return case : 1. There exists attached communicator, 2. communicator is null, 3. communicator is not connected</returns>
        public bool AddCommunicator(RobotCommunicator communicator)
        {
            bool bSucceeded = false;
            bool? bArgCommunicatorConnected = communicator?.Connected;

            // First Condition : There doesn't exist communicator which has already been attched and connected.
            // Second Condition: bArgCommunicatorConnected has a valid value(true or false)
            // Third Condition : and its value is true
            // It means communicator is not only valid but also it is connected
            // This is the perfect candidate for this class' communicator
            if (CanCommunicate == false && bArgCommunicatorConnected.HasValue && bArgCommunicatorConnected.Value == true)
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
            // first disconnect !
            Communicator?.Disconnect();

            //second make Communicator Property null
            Communicator = null;
        }
    }
}
