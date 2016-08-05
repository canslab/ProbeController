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
        /// LED type
        /// </summary>
        public enum LEDSide { Left, Right, Undefined };

        /// <summary>
        /// The types of servo motors 
        /// One of Horizontal, Vertical
        /// </summary>
        public enum ServoMotorSide { Horizontal, Vertical, Undefined }

        /// <summary>
        /// DC Motor Related enum
        /// </summary>
        public enum DCMotorMode { Forward, Backward, Break, Release, Undefined };

        /// <summary>
        /// It represents the Command(packet) that will be delievered to the remote device.
        /// This command conforms to RobotProtocol class' specification.
        /// </summary>
        public class Command
        {
            public string Target { get; set; }
            public List<object> Params { get; set; }

            // Static Methods
            public static string CreateLEDCommand(LEDSide ledSide, bool bOn)
            {
                Command ledCommand = new Command();

                ledCommand.Target = "LED";

                if (ledSide == LEDSide.Left)
                {
                    ledCommand.Params.Add("Left");
                }
                else if (ledSide == LEDSide.Right)
                {
                    ledCommand.Params.Add("Right");
                }
                else
                {
                    return null;
                }

                ledCommand.Params.Add(Convert.ToInt32(bOn));

                return ledCommand.getSerializedPacket();
            }
            public static string CreateDCMotorCommand(DCMotorMode leftDCMotorMode, uint numLeftDCMotorValue, DCMotorMode rightDCMotorMode, uint numRightDCMotorValue)
            {
                Command dcMotorCommand = new Command();

                // set type
                dcMotorCommand.Target = "DCMotors";

                // Left DC Motor mode & value setting
                inputMotorData(ref dcMotorCommand, leftDCMotorMode, numLeftDCMotorValue);
                // Right DC Motor mode & value setting
                inputMotorData(ref dcMotorCommand, rightDCMotorMode, numRightDCMotorValue);

                return dcMotorCommand.getSerializedPacket();
            }
            public static string CreateServoMotorCommand(ServoMotorSide side, uint numDutyCycle)
            {
                Command servoMotorCommand = null;
                string strSide = null;

                // convert ServoMotorsSide type to string type
                strSide = (side == ServoMotorSide.Horizontal) ? "Horizontal" : "Vertical";

                // make packet
                servoMotorCommand = new Command() { Target = "ServoMotors", Params = { strSide, numDutyCycle } };

                return servoMotorCommand.getSerializedPacket();
            }
            private static void inputMotorData(ref Command dataPacket, DCMotorMode DCMotorMode, uint numDCMotorValue)
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

            /// <summary>
            /// Make DataPacket using device type, and a set of parameter values
            /// </summary>
            /// <param name="deviceType"> device type, ex) LED</param>
            /// <param name="values"> associated values, ex) "Left", 0 </param>
            protected Command(string deviceType, params object[] values)
            {
                Target = deviceType;
                Params = new List<object>(values.Length);

                foreach (string value in values)
                {
                    Params.Add(value);
                }
            }
            protected Command()
            {
                Target = null;
                Params = new List<object>();
            }

            protected string getSerializedPacket()
            {

                return JsonConvert.SerializeObject(this);
            }
        }
    }
}
