using System;
using System.Collections.Generic;
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
        public enum LEDSide { Left, Right };

        /// <summary>
        /// The types of servo motors 
        /// One of Horizontal, Vertical
        /// </summary>
        public enum ServoMotorsSide { Horizontal, Vertical }

        /// <summary>
        /// DC Motor Related enum
        /// </summary>
        public enum DCMotorMode { Forward, Backward, Break, Release };

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

            if(ledType == LEDSide.Left)
            {
                dataPacket.Params.Add("Left");
            }
            else if(ledType == LEDSide.Right)
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
        /// Make DC Motors control command(=json string)
        /// </summary>
        /// <param name="leftDCMotorMode"> mode of the left DC Motor (Forward, Backward, Break, Release)</param>
        /// <param name="numLeftDCMotorValue">value of the left DC Motor(0~255)</param>
        /// <param name="rightDCMotorMode"> mode of the right DC Motor (Forward, Backward, Break, Release)</param>
        /// <param name="numRightDCMotorValue">value of the right DC Motor(0~255)</param>
        /// <returns></returns>
        public static string MakeDCMotorsCommand(DCMotorMode leftDCMotorMode, int numLeftDCMotorValue, DCMotorMode rightDCMotorMode, int numRightDCMotorValue)
        {
            string commandPacket = null;
            DataPacket dataPacket = new DataPacket();

            // set type
            dataPacket.Target = "DCMotors";

            // Left DC Motor mode & value setting
            inputMotorData(ref dataPacket, leftDCMotorMode, numLeftDCMotorValue);
            // Right DC Motor mode & value setting
            inputMotorData(ref dataPacket, rightDCMotorMode, numRightDCMotorValue);
            
            // serialize object before returning commandPacket
            commandPacket = JsonConvert.SerializeObject(dataPacket);
            return commandPacket;
        }

        /// <summary>
        /// Make Servo Motors control command(=json string)
        /// </summary>
        /// <param name="side"> The upper one or bottome one? </param>
        /// <param name="numDutyCycle"> The value of duty cycle </param>
        /// <returns> made JSON string(serialized packet) </returns>
        public static string MakeServoMotorsCommand(ServoMotorsSide side, int numDutyCycle)
        {
            string retJSONCommand = null;
            DataPacket dataPacket = null;
            string strSide = null;

            // convert ServoMotorsSide type to string type
            strSide = (side == ServoMotorsSide.Horizontal) ? "Horizontal" : "Vertical";
            
            // make packet
            dataPacket = new DataPacket() { Target = "ServoMotors", Params = { strSide, numDutyCycle } };

            // serialize data packet which is assigned to retJSONCommand
            retJSONCommand = JsonConvert.SerializeObject(dataPacket);
            
            return retJSONCommand;
        }

        private static void inputMotorData(ref DataPacket dataPacket, DCMotorMode DCMotorMode, int numDCMotorValue)
        {
            // Left DC Motor mode & value setting
            if (DCMotorMode == DCMotorMode.Forward)
            {
                dataPacket.Params.Add("Forward");
            }
            else if (DCMotorMode == DCMotorMode.Backward)
            {
                dataPacket.Params.Add("Backward");
            }
            else if (DCMotorMode == DCMotorMode.Break)
            {
                dataPacket.Params.Add("Break");
            }
            else
            {
                dataPacket.Params.Add("Release");
            }
            dataPacket.Params.Add(numDCMotorValue);
        }
    }
}
