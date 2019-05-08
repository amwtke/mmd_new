using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ
{
    [Serializable]
    public class ChatMessageMQ:MessageBaseMQ
    {
        public string SessionId { get; set; }
    }
}
