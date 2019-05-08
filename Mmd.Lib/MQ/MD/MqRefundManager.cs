using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.MQ;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB.Code;
using MD.Model.MQ;
using MD.Model.MQ.MD;

namespace MD.Lib.MQ.MD
{
    public static class MqRefundManager
    {
        static MqRefundManager()
        {
            try
            {
                MQManager.Prepare_P_MQ<MqWxRefundConfig>();
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqRefundManager),ex);
            }
        }

        public static bool SendMessage(MqWxRefundObject obj)
        {
            //订单状态到——退款中
            using (var repo = new BizRepository())
            {
                if (string.IsNullOrEmpty(obj?.out_trade_no))
                    return false;

                var order = repo.OrderGetByOutTradeNo(obj.out_trade_no);
                if (order == null)
                    return false;
                if (order.status != (int)EOrderStatus.退款中)
                {
                    order.status = (int)EOrderStatus.退款中;
                    repo.OrderUpDate(order);
                }

                if (obj != null)
                    MQManager.SendMQ<MqWxRefundConfig>(obj);

                return true;
            }
        }

        public static async void SendMessageAsync(MqWxRefundObject obj)
        {
            //订单状态到——退款中
            using (var repo = new BizRepository())
            {
                if (string.IsNullOrEmpty(obj?.out_trade_no))
                    return;

                var order = await repo.OrderGetByOutTradeNoAsync(obj.out_trade_no);
                if (order == null)
                    return;
                if (order.status != (int) EOrderStatus.退款中)
                {
                    order.status = (int) EOrderStatus.退款中;
                    await repo.OrderUpDateAsync(order);
                }

                if (obj != null)
                    MQManager.SendMQ<MqWxRefundConfig>(obj);
            }
        }
    }
}
