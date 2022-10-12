using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Config
{
    public class ServiceBusConfig
    {
        public string ConnectionString { get; set; }
        public string TopicName { get; set; }
        public string SubscriptionName { get; set; }
    }
}
