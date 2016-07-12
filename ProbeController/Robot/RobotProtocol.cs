using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProbeController.Robot
{
    /// <summary>
    /// It contains protocols and type information in order to control robot.
    /// 
    /// use MakeXXX().
    /// </summary>
    public static class RobotProtocol
    {
        /// <summary>
        /// Device type 
        /// </summary>
        public enum DeviceType { LED };

        /// <summary>
        /// LED type
        /// </summary>
        public enum LEDSide { LEFT, RIGHT };

        /// <summary>
        /// DC Motor Related enum
        /// </summary>
        public enum DCMotorMode { FORWARD, BACKWARD, BREAK, RELEASE };

        /// <summary>
        /// It represents data packet class and it'll be used to be converted into json string
        /// </summary>
        protected class DataPacket
        {
            public string Target { get; set; }
            public List<object> Params { get; set; }
        
            /// <summary>
            /// Make DataPacket using device type, and a set of parameter values
            /// </summary>
            /// <param name="deviceType"> device type, ex) LED</param>
            /// <param name="values"> associated values, ex) "Left", 0 </param>
            public DataPacket(string deviceType, params object[] values)
            {
                Target = deviceType;
                Params = new List<object>(values.Length);

                foreach(string value in values)
                {
                    Params.Add(value);
                }
            }
            public DataPacket()
            {
                Target = null;
                Params = new List<object>();
            }
        }

        /// <summary>
        /// Make LED control command(=json string)
        /// </summary>
        /// <param name="ledType"> LED Side </param>
        /// <param name="bOn"> Turn on or off </param>
        /// <returns> made json string, send it to the robot </returns>
        public static string MakeLEDCommand(LEDSide ledType, bool bOn)
        {
            string commandPacket = null;
            DataPacket dataPacket = new DataPacket();
            
            dataPacket.Target = "LED";

            if(ledType == LEDSide.LEFT)
            {
                dataPacket.Params.Add("Left");
            }
            else if(ledType == LEDSide.RIGHT)
            {
                dataPacket.Params.Add("Right");
            }
            else
            {
                return null;
            }

            dataPacket.Params.Add(Convert.ToInt32(bOn));
            commandPacket = JsonConvert.SerializeObject(dataPacket);

            return commandPacket;
        }

        /// <summary>
        /// Make DC Motors control command(=jsong string)
        /// </summary>
        /// <param name="leftDCMotorMode"> mode of the left DC Motor (Forward, Backward, Break, Release)</param>
        /// <param name="leftDCMotorValue">value of the left DC Motor(0~255)</param>
        /// <param name="rightDCMotorMode"> mode of the right DC Motor (Forward, Backward, Break, Release)</param>
        /// <param name="rightDCMotorValue">value of the right DC Motor(0~255)</param>
        /// <returns></returns>
        public static string MakeDCMotorCommand(DCMotorMode leftDCMotorMode, int leftDCMotorValue, DCMotorMode rightDCMotorMode, int rightDCMotorValue)
        {
            string commandPacket = null;
            DataPacket dataPacket = new DataPacket();

            // set type
            dataPacket.Target = "DCMotors";

            // Left DC Motor mode & value setting
            inputMotorData(dataPacket, leftDCMotorMode, leftDCMotorValue);
            // Right DC Motor mode & value setting
            inputMotorData(dataPacket, rightDCMotorMode, rightDCMotorValue);
            
            // serialize object before returning commandPacket
            commandPacket = JsonConvert.SerializeObject(dataPacket);
            return commandPacket;
        }

        private static void inputMotorData(DataPacket dataPacket, DCMotorMode leftDCMotorMode, int leftDCMotorValue)
        {
            // Left DC Motor mode & value setting
            if (leftDCMotorMode == DCMotorMode.FORWARD)
            {
                dataPacket.Params.Add("Forward");
            }
            else if (leftDCMotorMode == DCMotorMode.BACKWARD)
            {
                dataPacket.Params.Add("Backward");
            }
            else if (leftDCMotorMode == DCMotorMode.BREAK)
            {
                dataPacket.Params.Add("Break");
            }
            else
            {
                dataPacket.Params.Add("Release");
            }
            dataPacket.Params.Add(leftDCMotorValue);
        }
    }
}
