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
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public string DateTime { get; set; }
        public double UVIndex { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // APDS-9960 Sensor
        static APDS9960Sensor apds;
        // BME280Sensor
        static BME280Sensor bme;
        // VEML6075 Sensor
        static VEML6075Sensor veml;

        static int count = 0;

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
            // Initialize and configure sensor: APDS9960
            await InitializeSensors();
            // Initialize and configure sensor: BME280Sensor
            //await InitializeBME280Sensor();

            // Configure timer to 2000ms delayed start and 60000ms interval
            sensorTimer = new Timer(new TimerCallback(SensorTimerTick), null, 2000, 60000);
        }

        private async Task InitializeSensors()
        {
            // Create sensor instance: APDS9960
            // Ambient & RGB light: enabled, proximity: disabled, gesture: disabled
            apds = new APDS9960Sensor(true, false, false);
            
            // Create sensor instance: BME280
            bme = new BME280Sensor();

            // Delay 1ms
            await Task.Delay(1);

            // Create sensor instance: VEML6075Sensor
            veml = new VEML6075Sensor();

            // Advanced sensor configuration: VEML6075Sensor
            await veml.Config(
                VEML6075Sensor.IntegrationTime.IT_800ms,
                VEML6075Sensor.DynamicSetting.High,
                VEML6075Sensor.Trigger.NoActiveForceTrigger,
                VEML6075Sensor.ActiveForceMode.NormalMode,
                VEML6075Sensor.PowerMode.PowerOn
                );
        }

        private static void SensorTimerTick(object state)
        {
            // Read sensor data: al
            double al = apds.ReadAmbientLight();
            
            // Read sensor data: bme
            double derece = bme.ReadTemperature();
            double nem = bme.ReadHumidity();
            double basinc = bme.ReadPressure();
            double UVIndex = veml.Calculate_Average_UV_Index();

            // Create telemetry instance to store sensor data
            Telemetry telemetry = new Telemetry();
            
            telemetry.AmbientLight = Math.Round(al, 2);
            telemetry.Temperature = Math.Round(derece, 2);
            telemetry.Humidity = Math.Round(nem, 2);
            telemetry.Pressure = Math.Round(basinc, 2);
            telemetry.UVIndex = Math.Round(UVIndex, 2);

            // Set Measurement Time
            DateTime localDate = DateTime.Now;

            //string utcFormat = localDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            telemetry.DateTime = TimeZoneInfo.ConvertTime(localDate.ToUniversalTime(), TimeZoneInfo.Local).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

            // Ex: Get GTB Standard Time zone - (GMT+02:00) Athens, Istanbul, Minsk
            //TimeZoneInfo SystemTimeZoneId = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            //DateTime LocalTime = TimeZoneInfo.ConvertTime(localDate, TimeZoneInfo.Local, SystemTimeZoneId);
            //Debug.WriteLine("LocalTime: " + LocalTime);

            // Write sensor data to output / immediate window
            Debug.WriteLine("Date Time: " + telemetry.DateTime);
            Debug.WriteLine("Ambient Light: " + telemetry.AmbientLight.ToString());
            Debug.WriteLine("Temperature: " + telemetry.Temperature.ToString());
            Debug.WriteLine("Humidity: " + telemetry.Humidity.ToString());
            Debug.WriteLine("Pressure: " + telemetry.Pressure.ToString());

            Debug.WriteLine("- - - - - - - - - - - - -");

            // Write sensor data to output / immediate window
            Debug.WriteLine("UVA........: " + veml.Read_RAW_UVA().ToString());
            Debug.WriteLine("UVB........: " + veml.Read_RAW_UVB().ToString());
            Debug.WriteLine("UVA Index..: " + veml.Calculate_UV_Index_A().ToString());
            Debug.WriteLine("UVB Index..: " + veml.Calculate_UV_Index_B().ToString());
            Debug.WriteLine("UV Index...: " + telemetry.UVIndex.ToString());
            count = count + 1;
            Debug.WriteLine("Sıra: " + count);
            Debug.WriteLine("--------------------------");

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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
