using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
using Type = Core.Type;

namespace Simulator
{
    internal class Simulator
    {
        private const int DefaultDelay = 5000;
        private static DeviceClient _deviceClient;
        private static DesiredDeviceTwinConfiguration _twinDesiredProperties;
        private static volatile int _messageSendDelay = DefaultDelay;

        private static readonly object Locker = new object();
        private static Status _propertyChangeStatus = Status.Accepted;

        public static void Main(string[] args)
        {
            const string configFilePath = @"../../../config/config.yaml";
            var config = configFilePath.GetIoTConfiguration();
            var testDevice = config.DeviceConfigs.First();
            var azureConfig = config.AzureIoTHubConfig;

            var cts = new CancellationTokenSource();

            _deviceClient = DeviceClient.Create(
                azureConfig.Hostname,
                new DeviceAuthenticationWithRegistrySymmetricKey(testDevice.DeviceId, testDevice.Key),
                TransportType.Mqtt);

            // execute initial connect synchronously
            Task.Run(async () =>
                    {
                        await Connect(_deviceClient);
                        await GetInitialDesiredConfiguration(_deviceClient);

                        // Add Callback for Desired Configuration changes.
                        await _deviceClient.SetDesiredPropertyUpdateCallback(OnDesiredPropertyChange, null);

                        // Add Handler to set property request 
                        _deviceClient.SetMethodHandler("AcceptDesiredProperties", OnAcceptDesiredProperty, null);
                    }
                    , cts.Token)
                .Wait(cts.Token);

            Task.Run(() => DataSend(_deviceClient, cts.Token), cts.Token);

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task DataSend(DeviceClient deviceClient, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var payload = Guid.NewGuid().ToString();

                $"{DateTime.Now:HH:mm:ss tt} - Sending Data: {payload}".LogMessage(ConsoleColor.White);

                var message = new Message(Encoding.ASCII.GetBytes(payload));
                message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Information.ToString("G"));

                await deviceClient.SendEventAsync(message);

                // Pause before next simulated device reading.
                // We'll accept dirty reads of the delay value for simplicity.
                int delay;
                lock (Locker)
                {
                    delay = _messageSendDelay;
                }
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static async Task Connect(DeviceClient deviceClient)
        {
            try
            {
                await deviceClient.OpenAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task GetInitialDesiredConfiguration(DeviceClient deviceClient)
        {
            var twin = await deviceClient.GetTwinAsync();
            _twinDesiredProperties =
                JsonConvert.DeserializeObject<DesiredDeviceTwinConfiguration>(
                    twin.Properties.Desired["deviceTwinConfig"].ToString());
        }

        private static async Task OnDesiredPropertyChange(TwinCollection desiredproperties, object usercontext)
        {
            if (desiredproperties.Contains("deviceTwinConfig"))
            {
                Message message;

                var deviceTwinConfig =
                    JsonConvert.DeserializeObject<DesiredDeviceTwinConfiguration>(
                        desiredproperties["deviceTwinConfig"].ToString());

                // ignore delay for now. Just want to see if it can be parsed.
                int delay;

                if (int.TryParse(deviceTwinConfig.MessageSendDelay, out delay))
                {
                    lock (Locker)
                    {
                        _twinDesiredProperties = deviceTwinConfig;
                        _propertyChangeStatus = Status.Pending;
                    }
                    message = new Message(
                        Encoding.ASCII.GetBytes(
                            $"Device accepted messageSendDelay value: `{deviceTwinConfig.MessageSendDelay}`.  Property change is pending call to 'acceptDesiredProperties'.")
                    );

                    message.Properties.Add(MessageProperty.Type.ToString("G"), Type.ConfigChage.ToString("G"));
                    message.Properties.Add(MessageProperty.Status.ToString("G"), Status.Pending.ToString("G"));
                    message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Critical.ToString("G"));
                }
                else
                {
                    _propertyChangeStatus = Status.Rejected;

                    ($"Device Twin Property `messageSendDelay` is set to an illegal value: `{deviceTwinConfig.MessageSendDelay}`, " +
                        $"change status is 'rejected'. Sending Critical Notification.").LogMessage(ConsoleColor.Red);

                    message = new Message(
                        Encoding.ASCII.GetBytes(
                            $"Parameter messageSendDelay value: `{deviceTwinConfig.MessageSendDelay}`, could not be converted to an Int."));

                    message.Properties.Add(MessageProperty.Type.ToString("G"), Type.ConfigChage.ToString("G"));
                    message.Properties.Add(MessageProperty.Status.ToString("G"), Status.Rejected.ToString("G"));
                    message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Critical.ToString("G"));
                }


                await _deviceClient.SendEventAsync(message);
            }
        }

        private static Task<MethodResponse> OnAcceptDesiredProperty(MethodRequest request, object context)
        {
            MethodResponse response;

            if (Monitor.TryEnter(Locker) && (_propertyChangeStatus == Status.Pending))
            {
                $"Updating message send delay from {_messageSendDelay} to {_twinDesiredProperties.MessageSendDelay}. Change will take effect immediatly."
                .LogMessage(ConsoleColor.Green);

                _messageSendDelay = int.Parse(_twinDesiredProperties.MessageSendDelay);
                _propertyChangeStatus = Status.Accepted;
                var responseMessage = new
                {
                    AcceptanceRequestDateTime = DateTime.UtcNow,
                    Status = _propertyChangeStatus,
                    Message = "'MessageSendDelay' change accepted by device"
                };
                response = new MethodResponse(
                    Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(responseMessage)),
                    (int) HttpStatusCode.OK);
            
            }
            else
            {
                // Note: Precondition failed status code should really only be used with precondition header.
                // but direct methods do not allow access to headers to check precondition assertions.

                const int httpPreconditionFailedStatusCode = 412;

                var lockedResponseMessage = new
                {
                    AcceptanceRequestDateTime = DateTime.UtcNow,
                    Status = "PreconditionFailed",
                    Message = "A precondition for acceptance of the desired configuration change, failed."
                };

                response = new MethodResponse(
                    Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(lockedResponseMessage)),
                    httpPreconditionFailedStatusCode
                );
            }
            return
                Task.FromResult(response);
        }
    }
}