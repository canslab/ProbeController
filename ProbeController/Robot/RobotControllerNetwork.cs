using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        /// This class represents the communicator to communicate with remote robot
        /// 
        /// It offers you convenient methods to tell commands to remote robot
        /// 
        /// </summary>
        protected class RobotCommunicator
        {
            /// <summary>
            /// Initialize this robot object 
            /// It doesn't connect to the robot automatically, you should
            /// call ConnectAsync() method manually. 
            /// 
            /// </summary>
            /// <param name="ipAddress"> IP address of the robot such as 192.168.0.43 </param>
            /// <param name="port"> Port Number of the robot such as 44444 </param>
            public RobotCommunicator()
            {
                // allocate client socket
                ClientSocket = new TcpClient();
            }

            /// <summary>
            /// Connect to the remote robot designated by IP address and port number that has already given from constructor
            /// 
            /// Caution : If the robot has been already connected then this function returns false. 
            /// If you want to connect to another device, invoke Disconnect() and try this again.
            /// </summary>
            /// <param name="ip"> ip Address of the remote robot </param>
            /// <param name="remoteRobotPortNumber"> port number of the remote robot</param>
            /// <returns> Whether the connection has failed or not </returns>
            public async Task<bool> ConnectToURLAsync(string remoteRobotURL, int remoteRobotPortNumber)
            {
                // at least one argument is not valid  --> return false
                if (Connected == true || remoteRobotURL == null || (remoteRobotPortNumber < 0 || remoteRobotPortNumber > 65536))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        await ClientSocket.ConnectAsync(remoteRobotURL, remoteRobotPortNumber);

                        // try to find the IP Address of the URL 
                        //convertedRemoteRobotIPAddress = Dns.GetHostAddresses(remoteRobotURL)[0];
                        //await ClientSocket.ConnectAsync(convertedRemoteRobotIPAddress, remoteRobotPortNumber);
                    }
                    catch (SocketException socketEx)
                    {
                        // if there is an error, return false
                        Debug.WriteLine("ConnectToURLAsync() Error, RSN > {0}", socketEx.Message);
                        return false;
                    }

                    // If connection has succeded 
                    return true;
                }
            }

            /// <summary>
            /// Disconnect the connection, after this function is called, you can 
            /// connect to the other remote robot again.
            /// </summary>
            /// <returns> Whether the connection has unlinked or not</returns>
            public void Disconnect()
            {
                StreamToRobot.Close();
                ClientSocket.Close();

                // reallocate the tcp client object in order to use this Class again.
                ClientSocket = new TcpClient();
            }

            /// <summary>
            /// Send raw message to robot 
            /// </summary>
            /// <param name="byteArray"> Array of byte to be sent </param>
            /// <param name="offset"> the offset of given byteArray </param>
            /// <param name="length"> the total size of bytes to be sent from byteArray </param>
            public async Task<bool> SendBytesAsync(byte[] byteArray, int offset, int length)
            {
                try
                {
                    await StreamToRobot.WriteAsync(byteArray, offset, length);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SendBytesAsync() version error, RSN> {0}", e.Message);
                    return false;
                }
                return true;
            }

            /// <summary>
            /// send JSON data given as a string asynchronosuly
            /// </summary>
            /// <param name="jsonString"> the string of type json </param>
            /// <returns> Whether this function works well or not </returns>
            public async Task<bool> SendJSONStringAsnyc(string jsonString)
            {
                byte[] jsonByteArray = convertJSONStringToByteArray(jsonString);
                return await SendBytesAsync(jsonByteArray, 0, jsonByteArray.Length);
            }

            /************************************************************/
            /******                   Properties                   ******/
            /************************************************************/
            public TcpClient ClientSocket { get; private set; }
            public NetworkStream StreamToRobot
            {
                get
                {
                    if (Connected)
                    {
                        return ClientSocket.GetStream();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            public bool Connected
            {
                get
                {
                    bool? bConnected = ClientSocket?.Connected;

                    if (bConnected == null)
                    {
                        return false;
                    }
                    else
                    {
                        return bConnected.Value;
                    }
                }
            }

            protected byte[] convertJSONStringToByteArray(string jsonData)
            {
                // first 4 byte is the length of json data
                Int32 strDataLength = jsonData.Length;
                byte[] sentByteArray = new byte[4 + jsonData.Length];
                Encoding.ASCII.GetBytes(jsonData, 0, jsonData.Length, sentByteArray, 4);

                // little endian style
                for (int i = 0; i < 4; ++i)
                {
                    Int32 temp = strDataLength >> (8 * i);
                    sentByteArray[i] = Convert.ToByte(temp & 0xFF);
                }

                return sentByteArray;
            }
        }

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
        public async Task<bool> ConnectAsync(string remoteURL, int remotePortNumber)
        {
            bool bSucceeded = false;

            Communicator = new RobotCommunicator();
            bSucceeded =  await Communicator.ConnectToURLAsync(remoteURL, remotePortNumber);

            return bSucceeded;
        }
        
        /// <summary>
        /// Disconnect from robot 
        /// </summary>
        public void Disconnect()
        {
            // first disconnect !
            Communicator?.Disconnect();

            //second make Communicator Property null
            Communicator = null;
        }
    }
}