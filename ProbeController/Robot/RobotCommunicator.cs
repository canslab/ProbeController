using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProbeController.Robot
{
    /// <summary>
    /// This class represents the communicator to communicate with remote robot
    /// 
    /// It offers you convenient methods to tell commands to remote robot
    /// 
    /// </summary>
    public class RobotCommunicator
    {
        /// <summary>
        /// Issues command related to robot's 2 LEDs
        /// </summary>
        /// <param name="side">Left side or Right side</param>
        /// <param name="bOn"> Turn on or Off</param>
        /// <returns> did well or not </returns>
        public async Task<bool> IssueLEDCommandAsync(RobotProtocol.LEDSide side, bool bOn)
        {
            string madeJsonCommand = RobotProtocol.MakeLEDCommand(side, bOn);
            bool bDidWell = false;

            if (madeJsonCommand == null)
            {
                bDidWell = false;
            }
            else
            {
                bDidWell = await SendJSONStringAsnyc(madeJsonCommand);
            }

            return bDidWell;
        }

        /// <summary>
        /// Issusing DC Motor control command 
        /// </summary>
        /// <param name="leftDCMotorMode"> Left DC Motor mode (Forward, Backward, Break, Release) </param>
        /// <param name="leftDCMotorValue"> Left DC Motor Value (0 ~ 255)</param>
        /// <param name="rightDCMotorMode"> Right DC Motor mode (Forward, Backward, Break, Release) </param>
        /// <param name="rightDCMotorValue"> Right DC Motor Value (0 ~ 255)</param>
        /// <returns> did well or not </returns>
        public async Task<bool> IssueDCMotorCommandAsync(RobotProtocol.DCMotorMode leftDCMotorMode, int leftDCMotorValue, 
                                                        RobotProtocol.DCMotorMode rightDCMotorMode, int rightDCMotorValue)
        {
            string madeJsonCommand = RobotProtocol.MakeDCMotorsCommand(leftDCMotorMode, leftDCMotorValue, rightDCMotorMode, rightDCMotorValue);
            bool bDidWell = false;

            if (madeJsonCommand == null)
            {
                bDidWell = false;
            }
            else
            {
                bDidWell = await SendJSONStringAsnyc(madeJsonCommand);
            }
            return bDidWell;
        }

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
        /// If you want to connect to the other device, then call Disconnect() and try this again.
        /// </summary>
        /// <param name="ip"> ip Address of the remote robot </param>
        /// <param name="portNumber"> port number of the remote robot</param>
        /// <returns> Whether the connection has failed or not </returns>
        public async Task<bool> ConnectToURLAsync(string url, int portNumber)
        {
            IPAddress hostIPAddress = null;
            bool bSucceed = false;

            // at least one argument is not valid  --> return false
            if (url == null || (portNumber < 0 || portNumber > 65536 ))
            {
                return false;
            }

            try
            {
                // try to find the IP Address of the URL 
                hostIPAddress = Dns.GetHostAddresses(url)[0];
                
            }
            catch (SocketException socketEx)
            {
                // if there is an error, return false
                Debug.WriteLine("ConnectToURLAsync() Error, RSN > {0}", socketEx.Message);
                return false;
            }

            // connect to the remote device using host ip address, and port number
            bSucceed = await connectAsync(hostIPAddress, portNumber);
            if(bSucceed)
            {
                return true;
            }
            else
            {
                return false;
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
            IP = null;
            PortNumber = -1;
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
            catch(Exception e)
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
            byte[] jsonByteArray = makePacket(jsonString);
            return await SendBytesAsync(jsonByteArray, 0, jsonByteArray.Length);
        }          

        /************************************************************/
        /******                   Properties                   ******/
        /************************************************************/
        public IPAddress IP { get; private set; }
        public int PortNumber { get; private set; }
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
                return ClientSocket.Connected;
            }
        }        

        protected byte[] makePacket(string jsonData)
        {
            // first 4 byte is the length of json data
            Int32 strDataLength = jsonData.Length;
            byte[] sentByteArray = new byte[4 + jsonData.Length];
            Encoding.ASCII.GetBytes(jsonData, 0, jsonData.Length, sentByteArray, 4);
                
            // little endian style
            for(int i = 0; i < 4; ++i)
            {
                Int32 temp = strDataLength >> (8 * i);
                sentByteArray[i] = Convert.ToByte(temp & 0xFF);
            }

            return sentByteArray;
        }
       
        /// <summary>
        /// Connect to the remote robot designated by IP address and port number that has already given from constructor
        /// 
        /// Protected method
        /// Caution : If the robot has been already connected then this function returns false. 
        /// If you want to connect to the other device, then call Disconnect() and try this again.
        /// </summary>
        /// <param name="ip"> ip Address of the remote robot </param>
        /// <param name="portNumber"> port number of the remote robot</param>
        /// <returns> Whether the connection has failed or not </returns>
        protected async Task<bool> connectAsync(IPAddress ip, int portNumber)
        {
            if (Connected == true) 
            {
                // If there is already connection, return false!  
                return false;
            }

            try
            {
                // try connection
                await ClientSocket.ConnectAsync(ip, portNumber);
            }
            catch(Exception e)
            {
                Debug.WriteLine("robot connection failed RSN > {0}", e.Message);
                return false;
            }
            IP = ip;
            // if the connection has succeed, store the ip address and the port number 
            PortNumber = portNumber;
            return true;
        }

    }
}
