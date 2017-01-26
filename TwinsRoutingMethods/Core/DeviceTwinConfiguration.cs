using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class DesiredDeviceTwinConfiguration
    {
        public string ConfigId { get; set; }
        public string MessageSendDelay { get; set; }

        public DesiredDeviceTwinConfiguration(string configId, string messageSendDelay)
        {
            ConfigId = configId;
            MessageSendDelay = messageSendDelay;
        }
    }

    public class ReportedDeviceTwinConfiguration
    {
        public string ConfigId { get; set; }
        public DateTime PropertyLastUpdateReceived { get; set; }
        public string DesiredMessageSendDelay { get; set; }
        public ReportedDeviceTwinConfiguration(string configId, string desiredMessageSendDelay, DateTime propertyLastUpdateReceived)
        {
            ConfigId = configId;
            desiredMessageSendDelay = DesiredMessageSendDelay;
            PropertyLastUpdateReceived = propertyLastUpdateReceived;
        }
    }
}
