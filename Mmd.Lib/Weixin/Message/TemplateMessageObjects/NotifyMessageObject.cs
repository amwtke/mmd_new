using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.Message
{
    [Serializable]
    public class NotifyMessageObject: TemplateBase
    {
        public NotifyMessageObject(MessageBase first, MessageBase remark, MessageBase key1, MessageBase key2)
            : base(first, remark)
        {
            keynote1 = key1;
            keynote2 = key2;
        }
        public MessageBase keynote1 { get; set; }
        public MessageBase keynote2 { get; set; }
    }
}
