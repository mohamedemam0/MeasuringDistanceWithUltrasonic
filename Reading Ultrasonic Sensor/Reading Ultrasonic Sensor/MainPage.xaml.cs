using Amqp;
using Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Reading_Ultrasonic_Sensor
{
  
    public sealed partial class MainPage : Page
    {
        private const int triggerPinNumber = 18;
        private GpioPin TriggerPin;
        private const int echoPinNumber = 23;
        private GpioPin EchoPin;
        private DispatcherTimer timer;
        Stopwatch stopWatch = new Stopwatch();
        TimeSpan elapsedTime;

        const string sbNamespace = "https://iacaddemo.servicebus.windows.net/";
        const string keyName = "Send";
        const string keyValue = "+k5+YyNNhlYWxW3mwuY7ANMO4inkwo07ECB3JB03si8=";
        const string entity = "iacaddemo";

        private Geolocator _geolocator = null;
        double longitude;
        double lattitude;

        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            if (TriggerPin != null && EchoPin != null)
            {
                timer.Start();
            }
            _geolocator = new Geolocator();
            GetSASToken(sbNamespace, keyName, keyValue);
        }

        private string GetSASToken(string baseAddress, string SASKeyName, string SASKeyValue)
        {
            TimeSpan fromEpochStart = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string expiry = Convert.ToString((int)fromEpochStart.TotalSeconds + 3600);
            string stringToSign = WebUtility.UrlEncode(baseAddress) + "\n" + expiry;
            string hmac = GetSHA256Key(SASKeyValue, stringToSign);
            string hash = HmacSha256(SASKeyValue, stringToSign);
            string sasToken = String.Format(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}&skn={3}",
                WebUtility.UrlEncode(baseAddress), WebUtility.UrlEncode(hash), expiry, SASKeyName);
            return sasToken;
        }
        public string GetSHA256Key(string hashKey, string stringToSign)
        {
            MacAlgorithmProvider macAlgorithmProvider = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA256");
            BinaryStringEncoding encoding = BinaryStringEncoding.Utf8;
            var messageBuffer = CryptographicBuffer.ConvertStringToBinary(stringToSign, encoding);
            IBuffer keyBuffer = CryptographicBuffer.ConvertStringToBinary(hashKey, encoding);
            CryptographicKey hmacKey = macAlgorithmProvider.CreateKey(keyBuffer);
            IBuffer signedMessage = CryptographicEngine.Sign(hmacKey, messageBuffer);
            return CryptographicBuffer.EncodeToBase64String(signedMessage);
        }
        public string HmacSha256(string secretKey, string value)
        {
            // Move strings to buffers.
            var key = CryptographicBuffer.ConvertStringToBinary(secretKey, BinaryStringEncoding.Utf8);
            var msg = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);

            // Create HMAC.
            var objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            var hash = objMacProv.CreateHash(key);
            hash.Append(msg);
            return CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset());
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                TriggerPin = null;
                EchoPin = null;
                return;
            }
            TriggerPin = gpio.OpenPin(triggerPinNumber);
            TriggerPin.SetDriveMode(GpioPinDriveMode.Output);
            EchoPin = gpio.OpenPin(echoPinNumber);
            EchoPin.SetDriveMode(GpioPinDriveMode.Input);
        }

        private void WaitForEcho(GpioPinValue value, int timeout)
        {
            int count = timeout;
            while (EchoPin.Read() == value && --count > 0) { }
        }
       
        private double GetDistance()
        {
            stopWatch.Reset();
            TriggerPin.Write(GpioPinValue.High);
            Task.Delay(TimeSpan.FromMilliseconds(0.01)).Wait();
            TriggerPin.Write(GpioPinValue.Low);
            WaitForEcho(GpioPinValue.Low, 10000);
            stopWatch.Start();
            WaitForEcho(GpioPinValue.High, 10000);
            stopWatch.Stop();
            elapsedTime = stopWatch.Elapsed;
            double _distance =  elapsedTime.TotalMilliseconds * 34.3 / 2.0;
            distanceText.Text = "Distance: " + _distance.ToString();
            return _distance;
        }

        private void Timer_Tick(object sender, object e)
        {
           var distance = GetDistance();
            try
            {
                Send(distance);
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
            }
        }


        private async void GetLocation()
        {
            _geolocator.DesiredAccuracy = PositionAccuracy.High;
            Geoposition pos = await _geolocator.GetGeopositionAsync();

            longitude = pos.Coordinate.Point.Position.Longitude;
            lattitude = pos.Coordinate.Point.Position.Latitude;
            //Send(2.2, longitude, lattitude);
        }

        async void Send(double dist)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("SharedAccessSignature",GetSASToken(sbNamespace, keyName, keyValue));
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(dist.ToString(), Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                HttpBufferContent BufferContent = new HttpBufferContent(buffer);
                BufferContent.Headers.Add("Content-Type", "application/atom+xml;type=entry;charset=utf-8");
                var res = await client.PostAsync(new Uri("https://iacaddemo.servicebus.windows.net/iacaddemo/partitions/1/messages"), BufferContent);
            }

        }
    }
}
