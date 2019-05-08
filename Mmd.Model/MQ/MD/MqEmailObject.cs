using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ.MD
{
    [Serializable]
    public class MqEmailObject
    {
        public string TagetAddress { get; set; }
        public string Topic { get; set; }
        public string Body { get; set; }
    }
}
