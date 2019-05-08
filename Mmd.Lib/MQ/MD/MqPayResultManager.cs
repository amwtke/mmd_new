using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;

namespace MD.Lib.MQ.MD
{
    public class MqWxPayResultManager
    {
        static MqWxPayResultManager()
        {
            try
            {
                MQManager.Prepare_P_MQ<MqWxPayCallbackConfig>();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqWxPayResultManager), ex);
            }
        }

        public static bool SendMessage(WXPayResult obj)
        {
            if (obj != null)
                return MQManager.SendMQ_TB<MqWxPayCallbackConfig>(obj);
            return false;
        }

        public static void SendMessageAsync(WXPayResult obj)
        {
            if (obj != null)
                MQManager.SendMQ<MqWxPayCallbackConfig>(obj);
        }
    }
}
