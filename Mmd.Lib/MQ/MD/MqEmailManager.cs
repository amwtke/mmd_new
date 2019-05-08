using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.MQ;
using MD.Model.MQ.MD;

namespace MD.Lib.MQ.MD
{
    public static class MqEmailManager
    {
        static MqEmailManager()
        {
            try
            {
                MQManager.Prepare_P_MQ<EmailMQConfig>();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqEmailManager), ex);
            }
        }

        public static bool SendMessage(MqEmailObject obj)
        {
            if (obj != null)
                return MQManager.SendMQ_TB<EmailMQConfig>(obj);
            return false;
        }

        public static async Task<bool> SendMessageAsync(MqEmailObject obj)
        {
            if (obj != null)
                return await MQManager.SendMQ<EmailMQConfig>(obj);
            return false;
        }
    }
}
