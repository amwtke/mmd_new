using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.MQ
{
    [MDConfig("MQ", "Email")]
    public class EmailMQConfig : IConfigModel,IMQConfig
    {
        [MDKey("HostName")]
        public string HostName { get; set; }

        [MDKey("Port")]
        public string Port { get; set; }

        [MDKey("UserName")]
        public string UserName { get; set; }

        [MDKey("Password")]
        public string Password { get; set; }

        [MDKey("ExchangeName")]
        public string ExchangeName { get; set; }

        [MDKey("QueueName")]
        public string QueueName { get; set; }

        [MDKey("SpermThreshold")]
        public string SpermThreshold { get; set; }

        [MDKey("NumberOfC")]
        public string NumberOfC { get; set; }

        public void Init()
        {

        }
    }
}
