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
}
