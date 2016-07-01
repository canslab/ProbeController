using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProbeController.Robot
{
    /// <summary>
    /// This class represents robot(=tank) 
    /// 
    /// It offers you convenient methods to control remote robot
    /// 
    /// </summary>
    class RobotCommunicator
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
        /// Caution : if the robot is connected already, it can't connect more. if you want, call Disconnect() and try this again.
        /// </summary>
        /// <param name="ip"> ip Address of the remote robot </param>
        /// <param name="portNumber"> port number of the remote robot</param>
        /// <returns> Whether the connection has failed or not </returns>
        public async Task<bool> ConnectAsync(string ip, int portNumber)
        {
            if (Connected == true) 
            {
                // If there is already connection, return false!  
                return false;
            }

            try
            {
                // try connection
                await ClientSocket.ConnectAsync(IPAddress.Parse(ip), portNumber);
            }
            catch(Exception e)
            {
                Debug.WriteLine("robot connection failed RSN > {0}", e.Message);
                return false;
            }
            
            // if the connection has succeed, store the ip address and the port number 
            IP = ip;
            PortNumber = portNumber;
            return true;
        }

        /// <summary>
        /// Disconnect the connection, after this function is called, you can 
        /// connect to the other remote robot again.
        /// </summary>
        /// <returns> Whether the connection has unlinked or not</returns>
        public bool Disconnect()
        {
            if (Connected == false)
            {
                return false;
            }
            else
            {
                StreamToRobot.Close();
                ClientSocket.Close();
                IP = null;
                PortNumber = -1;
                ClientSocket.Close();

                // reallocate the tcp client object in order to use this Class again.
                ClientSocket = new TcpClient();

                return true;
            }
        }

        /// <summary>
        /// Send raw message to robot 
        /// </summary>
        /// <param name="byteArray"> Array of byte to be sent </param>
        public async Task<bool> SendRawMessage(byte[] byteArray, int offset, int length)
        {
            try
            {
                await StreamToRobot.WriteAsync(byteArray, offset, length);
            }
            catch(Exception e)
            {
                Debug.WriteLine("SendRawMessage() byte version error, RSN> {0}", e.Message);
                return false;
            }
            return true;
        }


        public void SendRawMessage(string stringData)
        {

        }

        public string IP { get; private set; }
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

    }
}
