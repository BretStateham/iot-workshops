using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Core
{
    public static class StringExtensions
    {
        /// <summary>
        /// Gets the IoT configuration.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <returns></returns>
        public static Configuration GetIoTConfiguration(this string configFilePath)
        {
            var deserializer = new Deserializer();
            using (var reader = File.OpenText(configFilePath))
            {
                return deserializer.Deserialize<Configuration>(reader);
            }
        }

        /// <summary>
        /// Updates the IoT configuration.
        /// </summary>
        /// <param name="configFilePath">The configuration file path.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public static Tuple<bool, Exception> UpdateIoTConfiguration(this string configFilePath, Configuration config)
        {
            try
            {
                var serializer = new Serializer();
                using (var writer = File.CreateText(configFilePath))
                {
                    serializer.Serialize(writer, config);
                }
                return new Tuple<bool, Exception>(true, null);

            } catch (Exception e)
            {
                return new Tuple<bool, Exception>(false, e);
            }
        }

        /// <summary>
        /// Logs a colerized message to the console window.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="message">The message.</param>
        public static void LogMessage(this string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
