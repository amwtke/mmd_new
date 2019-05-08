using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.Message.TemplateMessageObjects
{
    [Serializable]
    public class PaySuccessObject
    {
        public PaySuccessObject(MessageBase f, MessageBase r, MessageBase key1, MessageBase key2)
        {
            first = f;
            orderMoneySum = key1;
            orderProductName = key2;
            Remark = r;
        }
        public MessageBase first { get; set; }

        public MessageBase orderMoneySum { get; set; }

        public MessageBase orderProductName { get; set; }

        public MessageBase Remark { get; set; }
    }
}
