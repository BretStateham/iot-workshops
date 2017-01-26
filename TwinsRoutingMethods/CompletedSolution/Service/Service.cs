using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Type = Core.Type;

namespace Service
{
    public class Service
    {
        public static void Main(string[] args)
        {
            const string configFilePath = @"../../../config/config.yaml";
            var config = configFilePath.GetIoTConfiguration();
            var testDevice = config.DeviceConfigs.First();
            var azureConfig = config.AzureIoTHubConfig;
            var serviceBusConfig = config.AzureServiceBusConfig;

            var cts = new CancellationTokenSource();
            var registryManager = RegistryManager.CreateFromConnectionString(azureConfig.ConnectionString);

            CriticalNotificationMonitor(serviceBusConfig, azureConfig.ConnectionString, testDevice.DeviceId, cts.Token);

            var task = Task.Run(() => SendDesiredConfiguration(registryManager, testDevice.DeviceId, cts), cts.Token);

            Task.WaitAll(task);
        }

        private static async Task SendDesiredConfiguration(RegistryManager registryManager, string deviceId,
            CancellationTokenSource cts)
        {
            while (true)
            {

                Console.WriteLine("Enter a value to set device reporting frequency (in MS) or `exit` to terminate: ");
                
                var userInput = Console.ReadLine();
                var newMessageSendDelayValue = userInput.IsNullOrWhiteSpace() ? "" : userInput?.ToLowerInvariant();
                switch (newMessageSendDelayValue)
                {
                    // Allow the user to exit the application gracefully 
                    case "exit":
                        cts.Cancel();
                        return;

                    // Take all other user input and forward as change request to device
                    default:

                        var patch = new
                        {
                            properties = new
                            {
                                desired = new
                                {
                                    deviceTwinConfig =
                                    new DesiredDeviceTwinConfiguration(Guid.NewGuid().ToString(), newMessageSendDelayValue)
                                }
                            }
                        };

                        // refresh twin inside loop so that we dont end up with etag collisions.
                        var twin = await registryManager.GetTwinAsync(deviceId, cts.Token);

                        await
                            registryManager.UpdateTwinAsync(twin.DeviceId, JsonConvert.SerializeObject(patch), twin.ETag,
                                cts.Token);
                        Console.WriteLine($"Sending desired configuration change for setting `messageSendDelay` with value: {newMessageSendDelayValue} to deviceId: {deviceId}");
                        await
                            Task.Run(() => QueryReportedConfiguration(registryManager, deviceId, cts.Token), cts.Token);
                        break;
                }
            }
        }

        private static async Task QueryReportedConfiguration(RegistryManager registryManager, string deviceId,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var query = registryManager.CreateQuery($"SELECT * FROM devices WHERE deviceId = '{deviceId}'");
            var results = await query.GetNextAsTwinAsync();
            foreach (var result in results)
            {
                LogMessage(ConsoleColor.DarkYellow, $"Config report for: {result.DeviceId}");
                LogMessage(ConsoleColor.Yellow, $"Desired deviceTwinConfig: {JsonConvert.SerializeObject(result.Properties.Desired,Formatting.Indented)}");
                if (result.Properties.Reported.Contains("deviceTwinConfig"))
                {
                    LogMessage(ConsoleColor.Yellow, $"Reported deviceTwinConfig: {JsonConvert.SerializeObject(result.Properties.Reported,Formatting.Indented)}");
                }
                Console.WriteLine();
            }
        }

        private static void CriticalNotificationMonitor(
            AzureServiceBusConfig serviceBusConfig, 
            string iotHubConnectionString,
            string deviceId,
            CancellationToken token)
        {
            var client = QueueClient.CreateFromConnectionString(serviceBusConfig.ConnectionString,
                serviceBusConfig.QueueName);

            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            client.OnMessage(async message =>
            {
                var body = new StreamReader(message.GetBody<Stream>()).ReadToEnd();
                if (message.Properties.ContainsKey(MessageProperty.Type.ToString("G")) 
                    && (string)message.Properties[MessageProperty.Type.ToString("G")] == Type.ConfigChage.ToString("G"))
                {
                    var status = (Status) Enum.Parse(typeof(Status), (string)message.Properties[MessageProperty.Status.ToString("G")]);
                    switch (status)
                    {
                        case Status.Pending:
                            // Send message to accept state change.

                            LogMessage(ConsoleColor.Yellow, "");
                            var m = new CloudToDeviceMethod(DeviceMethods.AcceptDesiredProperties.ToString("G"),
                        TimeSpan.FromSeconds(30));
                            try
                            {
                                var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, m, token);
                                LogMessage(ConsoleColor.Green, $"Device responded with result code: {result.Status} and message: {result.GetPayloadAsJson()}");
                            }
                            catch (IotHubException ex)
                            {
                                Console.WriteLine(ex);
                            }
                            break;
                        case Status.Rejected:
                            // tell user value was bad. 
                            LogMessage(ConsoleColor.Red, $"Setting value was rejected by the device: '{deviceId}' with message: '{body}'. Please enter a new legal value: ");
                            break;
                        case Status.Accepted:
                            // everything worked.
                            LogMessage(ConsoleColor.White, "Setting was previously applied.");
                            break;
                        case Status.Locked:
                            LogMessage(ConsoleColor.Red, "Target device was in a `Locked` state.  Please retry your reqeust.");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        /// <summary>
        /// Logs a colerized message to the console window.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="message">The message.</param>
        private static void LogMessage(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}