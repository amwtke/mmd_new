using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ.MD
{
    [Serializable]
    public class MqWxRefundObject
    {
        public string appid { get; set; }
        //public int? total_fee { get; set; }
        //public int? cash_fee { get; set; }
        //public string transaction_id { get; set; }
        public string out_trade_no { get; set; }
    }
}
