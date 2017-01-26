using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Model of yaml configuration file.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets the azure iot hub configuration.
        /// </summary>
        /// <value>
        /// The azure io t hub configuration.
        /// </value>
        public AzureIoTHubConfig AzureIoTHubConfig { get; set; }

        /// <summary>
        /// Gets or sets the list of device configurations.
        /// </summary>
        /// <value>
        /// The device configurations.
        /// </value>
        public List<DeviceConfig> DeviceConfigs { get; set; }

        /// <summary>
        /// Gets or sets the azure service bus configuration.
        /// </summary>
        /// <value>
        /// The azure service bus configuration.
        /// </value>
        public AzureServiceBusConfig AzureServiceBusConfig { get; set; }
    }

    /// <summary>
    /// Model of settings for an Azure Service Bus
    /// </summary>
    public class AzureServiceBusConfig
    {
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the name of the queue.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        public string QueueName { get; set; }
    }

    /// <summary>
    /// Model of settings for an Azure IoT Hub.
    /// </summary>
    public class AzureIoTHubConfig
    {
        /// <summary>
        /// Gets or sets the iot hub URI - aka azure portal iot hub host name.
        /// </summary>
        /// <value>
        /// The iot hub URI - aka azure portal iot hub host name.
        /// </value>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets or sets the connection string to the iot hub.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Model of a single device configuration.
    /// </summary>
    public class DeviceConfig
    {

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>
        /// The device identifier.
        /// </value>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device key.
        /// </summary>
        /// <value>
        /// The device key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the device generation.
        /// </summary>
        /// <value>
        /// The generation.
        /// </value>
        public string Status { get; set; }
    }
}
