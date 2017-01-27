using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Core;


namespace CreateDeviceIdentity
{
    /// <summary>
    /// Sample program to create a device identity in Azure IoT Hub.
    /// </summary>
    public class Program
    {
        private static RegistryManager _registryManager;

        /// <summary>
        /// Adds the device asynchronously.
        /// </summary>
        /// <param name="deviceConfig">The device configuration.</param>
        /// <returns></returns>
        private static async Task<string> AddDeviceAsync(DeviceConfig deviceConfig)
        {
            Device device;
            try
            {
                DeviceStatus status;
                if (!Enum.TryParse(deviceConfig.Status, true, out status)) 
                    status = DeviceStatus.Disabled;

                var d = new Device(deviceConfig.DeviceId)
                {
                    Status = status
                };
                device = await _registryManager.AddDeviceAsync(d);
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await _registryManager.GetDeviceAsync(deviceConfig.DeviceId);
            }
            return device.Authentication.SymmetricKey.PrimaryKey;
        }

        public static void Main(string[] args)
        {
            const string configFilePath = @"../../../config/config.yaml";
            var config = configFilePath.GetIoTConfiguration();
            var testDevice = config.DeviceConfigs.First();
            var azureConfig = config.AzureIoTHubConfig;

            _registryManager = RegistryManager.CreateFromConnectionString(azureConfig.ConnectionString);
            var task = AddDeviceAsync(testDevice);
            task.Wait();

            testDevice.Key = task.Result;

            Console.WriteLine(configFilePath.UpdateIoTConfiguration(config).Item1
                ? $"DeviceId: {testDevice.DeviceId} has DeviceKey: {testDevice.Key} \r\nConfig file: {configFilePath} has been updated accordingly."
                : $"Error writing DeviceKey: {testDevice.Key} for DeviceId: {testDevice.DeviceId} to config file: {configFilePath} ");

            Console.ReadLine();
        }
    }
}
