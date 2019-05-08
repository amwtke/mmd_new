using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ
{
    [Serializable]
    public class EkCommentMQObject
    {
        public string Uuid { get; set; }
        public string From { get; set; }
        public long To { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    [Serializable]
    public class PCommentMQObject
    {
        public string Uuid { get; set; }
        public string From { get; set; }
        public long To { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
