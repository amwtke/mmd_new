using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ
{
    [Serializable]
    public class MessageBaseMQ
    {
        public string Uuid { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public object PayLoad { get; set; }
        public MsgTypeMQ MsgType { get; set; }
        /// <summary>
        /// Unixtime
        /// </summary>
        public double TimeStamp { get; set; }
    }
}
