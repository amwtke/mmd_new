using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.MQ.RPC
{
    
    public class RpcConfigBase
    {
        [MDKey("ServerName")]
        public string ServerName { get; set; }

        /// <summary>
        /// server监听的rpc队列名称
        /// </summary>
        [MDKey("QueueName")]
        public string QueueName { get; set; }

        [MDKey("Host")]
        public string Host { get; set; }

        [MDKey("Port")]
        public string Port { get; set; }

        /// <summary>
        /// 暂时不用
        /// </summary>
        [MDKey("NumberOfQueue")]
        public string NumberOfQueue { get; set; }

        [MDKey("UserName")]
        public string UserName { get; set; }

        [MDKey("PassWord")]
        public string PassWord { get; set; }
    }
}
