using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ.MD
{
    [Serializable]
    public class MqWxTempMsgObject
    {
        public string Appid { get; set; }
        public string At { get; set; }
        public string ToOpenId { get; set; }
        public string ShortId { get; set; }
        public object Data { get; set; }
        public string Url { get; set; }
        public string TopColor { get; set; }
    }
}
