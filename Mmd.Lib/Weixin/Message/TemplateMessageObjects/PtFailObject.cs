using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.Message.TemplateMessageObjects
{
    [Serializable]
    public class PtFailObject : TemplateBase
    {
        public PtFailObject(MessageBase first, MessageBase remark, MessageBase key1, MessageBase key2, MessageBase key3)
            : base(first, remark)
        {
            keyword1 = key1;
            keyword2 = key2;
            keyword3 = key3;
        }
        public MessageBase keyword1 { get; set; }

        public MessageBase keyword2 { get; set; }

        public MessageBase keyword3 { get; set; }
    }
}
