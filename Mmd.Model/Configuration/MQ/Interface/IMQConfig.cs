using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.MQ
{
    public interface IMQConfig
    {
        string HostName { get; set; }

        string Port { get; set; }

        string UserName { get; set; }

        string Password { get; set; }

        string ExchangeName { get; set; }

        string QueueName { get; set; }

        string SpermThreshold { get; set; }

        string NumberOfC { get; set; }
    }
}
