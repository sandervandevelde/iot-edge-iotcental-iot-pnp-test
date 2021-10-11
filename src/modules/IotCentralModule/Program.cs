namespace IotCentralModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Newtonsoft.Json;

    class Program
    {
        private static ModuleClient ioTHubModuleClient;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();

            await ioTHubModuleClient.SetMethodHandlerAsync(
                "getCount",
                getCountMethodCallBack,
                ioTHubModuleClient);

            await ioTHubModuleClient.SetMethodHandlerAsync(
                "getError",
                getErrorMethodCallBack,
                ioTHubModuleClient);

            Console.WriteLine("IoT Hub module client initialized: getCount; getError");
        }

        static async Task<MethodResponse> getCountMethodCallBack(MethodRequest methodRequest, object userContext)        
        {
            //// Send a message

            var telemetryMessage = new TelemetryMessage {
                id = 42,
                value = "telemetry",
            };

            var jsonMessage = JsonConvert.SerializeObject(telemetryMessage);

            using (var message = new Message(Encoding.UTF8.GetBytes(jsonMessage)))
            { 
                message.ContentEncoding = "utf-8";
                message.ContentType = "application/json";

                await ioTHubModuleClient.SendEventAsync("output1", message);
            }

            System.Console.WriteLine("Telemetry after 'getCount' sent.");

            //// Return a COUNT response

            var getCountResponse = new GetCountResponse {
                count = 42,            
            };

            var json = JsonConvert.SerializeObject(getCountResponse);
            var response = new MethodResponse(Encoding.UTF8.GetBytes(json), 200);

            await Task.Delay(TimeSpan.FromSeconds(0));

            return response;
        }

        static async Task<MethodResponse> getErrorMethodCallBack(MethodRequest methodRequest, object userContext)        
        {
            //// Send a message

            var errorMessage = new ErrorMessage {
                code = 1234,
                message = "this is an error",
            };

            var jsonMessage = JsonConvert.SerializeObject(errorMessage);

            using (var message = new Message(Encoding.UTF8.GetBytes(jsonMessage)))
            { 
                message.ContentEncoding = "utf-8";
                message.ContentType = "application/json";

                await ioTHubModuleClient.SendEventAsync("output2", message);
            }

            System.Console.WriteLine("Error after 'getError' sent.");

            //// Return an ERROR response

            var getErrorResponse = new GetErrorResponse {
                code = 1234,            
            };

            var json = JsonConvert.SerializeObject(getErrorResponse);
            var response = new MethodResponse(Encoding.UTF8.GetBytes(json), 200);

            await Task.Delay(TimeSpan.FromSeconds(0));

            return response;
        }
    }

    public class GetErrorResponse 
    {
        public int code { get; set; }
    }

    public class GetCountResponse 
    {
        public int count { get; set; }
    }

    public class TelemetryMessage
    {
        public int id { get; set; }
        public string value { get; set; }
    }

    public class ErrorMessage
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
