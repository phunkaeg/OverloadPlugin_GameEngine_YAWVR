using OverloadPluginV2.Properties;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YawGEAPI;
using System.Resources;
namespace OverloadPlugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Overload")] // Name that will appear in the plugin list.
    [ExportMetadata("Version", "1.0")]

    public class OverloadPlugin : Game
    {
        public int STEAM_ID => 448850; // Will start this game on steam based on Steam ID

        public string PROCESS_NAME => "olmod"; // Put here the exe name (without .exe) monitored by GE to maintain the plugin active.

        public bool PATCH_AVAILABLE => false; // Needs patch

        public string AUTHOR => "PhunkaeG";

        public System.Drawing.Image Logo => Resources.logo;

        public System.Drawing.Image SmallLogo => Resources.small;

        public System.Drawing.Image Background => Resources.background;

        public string Description => "Usage:<br>1. Install OLMOD (https://olmod.overloadmaps.com/)<br>2. Install gamemod.dll with telemetry in main game folder.<br>3. Launch \"Olmod.exe -vrmode openvr\" to start the game ('Telemetry' must appear on the upper right corner of the game's main menu)."; // No title here, the name of the plugin is added automatically.

        private Thread readThread;
        private volatile bool running = false;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private UdpClient udpClient;
        private IPEndPoint endPoint;

        private void ReadTelemetry()
        {
            try
            {
                while (running)
                {
                    if (udpClient.Available > 0)
                    {
                        byte[] data = udpClient.Receive(ref endPoint);
                        string telemetryData = Encoding.ASCII.GetString(data);
                        ProcessTelemetry(telemetryData);
                    }
                    // Thread.Sleep(20); // Reduce CPU usage - NB: Overloads sends UDP packets much too fast !!!
                }
            }
            catch (Exception ex)
            {
                // Handle or log exceptions
                Console.WriteLine("Error reading telemetry data: " + ex.Message);
            }
        }
        private void ProcessTelemetry(string telemetry)
        {
            try
            {
                string[] parts = telemetry.Split(';');
                if (parts.Length >= 14) // Make sure all parts are present
                {
                    float roll = float.Parse(parts[0]);
                    float pitch = float.Parse(parts[1]);
                    float yaw = float.Parse(parts[2]);
                    float VelocityX = float.Parse(parts[3]);
                    float VelocityY = float.Parse(parts[4]);
                    float VelocityZ = float.Parse(parts[5]);
                    float gForceX = float.Parse(parts[6]);
                    float gForceY = float.Parse(parts[7]);
                    float gForceZ = float.Parse(parts[8]);
                    float boosting = float.Parse(parts[9]);
                    float primaryFire = float.Parse(parts[10]);
                    float secondaryFire = float.Parse(parts[11]);
                    float pickedUpItem = float.Parse(parts[12]);
                    float damageTaken = float.Parse(parts[13]);

                    // Set inputs based on parsed data
                    controller.SetInput(0, yaw);
                    controller.SetInput(1, pitch);
                    controller.SetInput(2, roll);
                    // Example: Assume inputs 3, 4, 5 are set for G-forces
                    controller.SetInput(3, VelocityX);
                    controller.SetInput(4, VelocityY);
                    controller.SetInput(5, VelocityZ);
                    controller.SetInput(6, gForceX);
                    controller.SetInput(7, gForceY);
                    controller.SetInput(8, gForceZ);
                    controller.SetInput(9, boosting);
                    controller.SetInput(10, primaryFire);
                    controller.SetInput(11, secondaryFire);
                    controller.SetInput(12, pickedUpItem);
                    controller.SetInput(13, damageTaken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing telemetry data: {ex.Message}");
            }
        }

        public LedEffect DefaultLED()
        {
            return new LedEffect(EFFECT_TYPE.KNIGHT_RIDER_2, 7, new YawColor[4]
            {
                new YawColor((byte) 66, (byte) 135, (byte) 245),
                new YawColor((byte) 80, (byte) 80, (byte) 80),
                new YawColor((byte) 128, (byte) 3, (byte) 117),
                new YawColor((byte) 110, (byte) 201, (byte) 12)
            }, 25f);
        }

        public List<Profile_Component> DefaultProfile()
        {
            string defProfile = "{\"GameName\":\"Overload\",\"Name\":\"Default profile\",\"components\":[{\"Constant\":false,\"Input_index\":4,\"Output_index\":1,\"MultiplierPos\":5.0,\"MultiplierNeg\":5.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":8,\"Output_index\":1,\"MultiplierPos\":1.0,\"MultiplierNeg\":1.0,\"Offset\":0.0,\"Inverse\":true,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":3,\"Output_index\":2,\"MultiplierPos\":3.0,\"MultiplierNeg\":3.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":6,\"Output_index\":2,\"MultiplierPos\":1.0,\"MultiplierNeg\":1.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":5,\"Output_index\":0,\"MultiplierPos\":2.0,\"MultiplierNeg\":2.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":1,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":10,\"Output_index\":3,\"MultiplierPos\":40.0,\"MultiplierNeg\":40.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]},{\"Constant\":false,\"Input_index\":10,\"Output_index\":4,\"MultiplierPos\":50.0,\"MultiplierNeg\":50.0,\"Offset\":0.0,\"Inverse\":false,\"Limit\":-1.0,\"Smoothing\":1.0,\"Enabled\":true,\"Spikeflatter\":{\"Enabled\":false,\"Limit\":100.0,\"Strength\":0.5},\"Deadzone\":0.0,\"Type\":0,\"Condition\":[],\"Math\":[]}],\"effects\":{\"EffectID\":1,\"InputID\":7,\"Multiplier\":25.0,\"Colors\":[{\"R\":66,\"G\":135,\"B\":245},{\"R\":80,\"G\":80,\"B\":80},{\"R\":128,\"G\":3,\"B\":117},{\"R\":110,\"G\":201,\"B\":12}]},\"functions\":[]}";
            var MyComponentsList = new List<Profile_Component>();
            MyComponentsList = dispatcher.JsonToComponents(defProfile);
            return MyComponentsList;
        }

        public void Exit()
        {
            running = false;
            readThread.Join();
            udpClient.Close();
        }
        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return new Dictionary<string, ParameterInfo[]>(); // Return empty if no features to report
        }

        public string[] GetInputData()
        {
            return new string[] { "Yaw", "Pitch", "Roll", "RollSpeed", "PitchSpeed", "YawSpeed", "SwaySpeed", "HeaveSpeed", "SurgeSpeed", "BoostActive", "PrimaryFire", "SecondaryFire", "GotItem", "DamageTaken" }; // Text of the inputs that appear in GE's dropdown
        }

        public void Init()
        {
            if (udpClient != null)
                udpClient.Close();

            if (readThread != null && readThread.IsAlive)
                Exit();

            endPoint = new IPEndPoint(IPAddress.Any, 4123); // Use the correct port for Overload
            udpClient = new UdpClient(endPoint);

            running = true;
            readThread = new Thread(ReadTelemetry);
            readThread.Start();
        }

        public void PatchGame()
        {
            // Intentionally left blank - no patching required
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            //we need to save these references
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
    }
}
