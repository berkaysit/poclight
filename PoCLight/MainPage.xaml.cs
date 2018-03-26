using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using TurtaIoTHAT;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PoCLight
{
    public class Telemetry
    {
        public double AmbientLight { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // APDS-9960 Sensor
        static APDS9960Sensor apds;

        // Sensor timer
        Timer sensorTimer;
        public MainPage()
        {
            this.InitializeComponent();
            // Initialize sensor and timer
            Initialize();
        }
        private async void Initialize()
        {
            // Initialize and configure sensor
            await InitializeAPDS9960();

            // Configure timer to 2000ms delayed start and 60000ms interval
            sensorTimer = new Timer(new TimerCallback(SensorTimerTick), null, 2000, 20000);
        }

        private async Task InitializeAPDS9960()
        {
            // Create sensor instance
            // Ambient & RGB light: enabled, proximity: disabled, gesture: disabled
            apds = new APDS9960Sensor(true, false, false);

            // Delay 1ms
            await Task.Delay(1);
        }

        private static void SensorTimerTick(object state)
        {
            // Read sensor data
            double al = apds.ReadAmbientLight();

            // Write sensor data to output / immediate window
            Debug.WriteLine("Ambient Light: " + al.ToString());
            //Debug.WriteLine("Ambient Light: " + 55);

            // Create telemetry instance to store sensor data
            Telemetry telemetry = new Telemetry();
            telemetry.AmbientLight = al;
            //telemetry.AmbientLight = 55.5555;

            // Convert telemetry JSON to string
            string telemetryJSON = JsonConvert.SerializeObject(telemetry);

            // Send data to IoT Hub
            SendDeviceToCloudMessageAsync(telemetryJSON);
        }

        static async void SendDeviceToCloudMessageAsync(string msg)
        {
            string iotHubUri = "TurtaIoTHub.azure-devices.net";
            string deviceId = "RaspiWin01";
            string deviceKey = "Sl4jGYeuSeCGYPr3fUHsL6aqMTaXOy7gxuumHSLdmnc=";

            var deviceClient = DeviceClient.Create(iotHubUri,
                AuthenticationMethodFactory.
                CreateAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey),
                TransportType.Http1);

            var message = new Message(Encoding.ASCII.GetBytes(msg));

            await deviceClient.SendEventAsync(message);
        }
    }
}
