using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

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

            var consoleReadTask = Task.Run(async () => {
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
                            await SendDesiredConfiguration(registryManager, testDevice.DeviceId, cts.Token, newMessageSendDelayValue);
                            break;
                    }
                }
            }, cts.Token);

            Task.WaitAll(consoleReadTask);
        }

        private static async Task SendDesiredConfiguration(RegistryManager registryManager, string deviceId,
            CancellationToken cancellationToken, string newMessageSendDelayValue)
        {
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        deviceTwinConfig =
                        new DesiredDeviceTwinConfiguration(Guid.NewGuid().ToString(),
                            newMessageSendDelayValue)
                    }
                }
            };

            // Get the latest Device Twin State
            var twin = await registryManager.GetTwinAsync(deviceId, cancellationToken);

            await
                registryManager.UpdateTwinAsync(twin.DeviceId, JsonConvert.SerializeObject(patch), twin.ETag,
                    cancellationToken);
            ($"Sending desired configuration change for setting `messageSendDelay` with value: " + 
                "{newMessageSendDelayValue} to deviceId: {deviceId}").LogMessage(ConsoleColor.Green);

            await
                Task.Run(() => QueryTwinConfiguration(registryManager, deviceId), cancellationToken);
        }

        private static async Task QueryTwinConfiguration(RegistryManager registryManager, string deviceId)
        {
            var query = registryManager.CreateQuery($"SELECT * FROM devices WHERE deviceId = '{deviceId}'");
            var results = await query.GetNextAsTwinAsync();
            foreach (var result in results)
            {
                $"Config report for: {result.DeviceId}".LogMessage(ConsoleColor.DarkYellow);
                $"Desired deviceTwinConfig: {JsonConvert.SerializeObject(result.Properties.Desired, Formatting.Indented)}"
                    .LogMessage(ConsoleColor.Yellow);
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
                var status = (Status) Enum.Parse(
                    typeof(Status),
                    (string) message.Properties[MessageProperty.Status.ToString("G")]);

                switch (status)
                {
                    case Status.Pending:
                        // Send message to accept state change.
                        Console.WriteLine();
                        var m = new CloudToDeviceMethod(DeviceMethods.AcceptDesiredProperties.ToString("G"),
                            TimeSpan.FromSeconds(30));
                        var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, m, token);
                        $"Device responded with result code: {result.Status} and message: {result.GetPayloadAsJson()}"
                            .LogMessage(ConsoleColor.Green);

                        break;
                    case Status.Rejected:
                        var body = new StreamReader(message.GetBody<Stream>()).ReadToEnd();
                        $"Setting value was rejected by the device: '{deviceId}' with message: '{body}'. Please enter a new legal value: "
                            .LogMessage(ConsoleColor.Red);
                        break;
                    case Status.Accepted:
                    case Status.PreconditionFailed:
                    default:
                        throw new ArgumentException();
                }
            });
        }
    }
}