using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Lib.Weixin.Robot;
using MD.Lib.Weixin.Services;
using MD.Model.Configuration.MQ;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;
using MD.Model.Index;
using MD.Model.MQ;
using MD.Model.MQ.MD;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Md.RefundSevice
{
    public partial class Service1 : ServiceBase
    {
        static bool CloseService = false;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MDLogger.LogInfo(typeof(Service1), "MD.Refund服务开始启动.......");
            Init();
            MDLogger.LogInfo(typeof(Service1), "MD.Refund服务已经启动！");
        }

        private void Init()
        {
            initRefund();
            AsyncHelper.RunAsync(delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始处理过期的GroupOrder.......");
                        ProcessData();
                        MDLogger.LogInfoAsync(typeof(Service1), "本次GroupOder处理结束！");

                        //休息一下
                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(20));
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), ex);
                    }
                }
            }, null);
            AsyncHelper.RunAsync(delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始处理退款失败的wxrefund.......");
                        ProcessDate_refundFail();
                        MDLogger.LogInfoAsync(typeof(Service1), "本次处理退款失败的wxrefund处理结束！");
                        //休息一下
                        System.Threading.Thread.Sleep(TimeSpan.FromDays(1));
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), new Exception("处理重新退款ProcessDate_refundFail失败" + ex.Message));
                    }
                }
            }, null);
            Thread.Sleep(1000);
            //initWxPayCallbackQueue();
        }

        private void ProcessData()
        {
            WxServiceHelper.Md_GoExpire_RefundingOrderProcess();

        }
        /// <summary>
        /// 重新处理退款失败的订单（再重新退款）
        /// </summary>
        private void ProcessDate_refundFail()
        {
            WxServiceHelper.Md_refundFailProcess();
        }
        private void initRefund()
        {
            MDLogger.LogInfoAsync(typeof(Service1), "开始启动refund队列服务器.....");

            MQManager.RegisterConsumerProcessor<MqWxRefundConfig>(async delegate (BasicDeliverEventArgs ar, IModel channel)
            {
                try
                {
                    var body = ar.Body;
                    object obj = BinarySerializationHelper.DeserializeObject(body);

                    var env = obj as MqWxRefundObject;
                    if (!string.IsNullOrEmpty(env?.appid) && !string.IsNullOrEmpty(env.out_trade_no) && !env.appid.Equals(RobotHelper.RobotAppid))
                    {
                        if (await MdWxPayUtil.RefundAsync(env.appid, env.out_trade_no))
                        {
                            MDLogger.LogInfoAsync(typeof(Service1), $"退款成功servicLog:appid{env.appid},out trade no:{env.out_trade_no}");
                            //更新统计信息
                            RedisMerchantStatisticsOp.AfterRefundAsync(env.appid, env.out_trade_no);
                        }
                        else
                        {
                            MDLogger.LogInfoAsync(typeof(Service1), $"退款失败！servicLog:appid{env.appid},out trade no:{env.out_trade_no}");
                        }
                    }
                    else
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), new Exception($"退款失败！反序列化问题2！appid或者otn为空！servicLog:appid{env?.appid},out trade no:{env?.out_trade_no},是否包含：{env.appid.Equals(RobotHelper.RobotAppid)}"));
                    }
                }
                catch (Exception ex)
                {
                    MDLogger.LogErrorAsync(typeof(Service1), ex);
                }
            });

            MQManager.Prepare_C_MQ<MqWxRefundConfig>();
            MDLogger.LogInfoAsync(typeof(Service1), "启动refund队列服务器完成!");
        }

        private void initWxPayCallbackQueue()
        {
            MDLogger.LogInfoAsync(typeof(Service1), "开始启动WxPayCallback队列服务器.....");

            MQManager.RegisterConsumerProcessor<MqWxPayCallbackConfig>(delegate (BasicDeliverEventArgs ar, IModel channel)
            {
                var body = ar.Body;
                object obj = BinarySerializationHelper.DeserializeObject(body);

                var env = obj as WXPayResult;
                WxServiceHelper.Md_PrcessWxPayCallBack(env);
            });
            //MQManager.Prepare_All_C_MQ();
            MQManager.Prepare_C_MQ_TB<MqWxPayCallbackConfig>();
            MDLogger.LogInfoAsync(typeof(Service1), "启动WxPayCallback队列服务器完毕！");
        }

        protected override void OnStop()
        {
            MDLogger.LogInfo(typeof(Service1), "MD.Refund服务开始关闭.......");
            Onclose();
            MDLogger.LogInfo(typeof(Service1), "MD.Refund服务已经关闭！");
        }

        private void Onclose()
        {
            CloseService = true;
            //MQManager.CloseAll();
            //Thread.Sleep(1000);
        }
    }
}
