using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.Util;
using MD.Lib.Weixin.Services;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;
using RabbitMQ.Client;

namespace Md.WxPayCallBackProcess
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = MdConfigurationManager.GetConfig<MqWxPayCallbackConfig>();
            int numberOfQs = int.Parse(ConfigurationManager.AppSettings["NumberOfQueue"]);
            
            MDLogger.LogInfoAsync(typeof(Service1),$"开始启动{numberOfQs}个支付回调队列监听........");

            for (int i = 0; i < numberOfQs; i++)
            {
                string queueName = WxServiceHelper.GetWxPayQueueName(config.QueueName, i);
                WatchStopper ws = new WatchStopper(typeof(Service1), $"watch stop from queue:{queueName}");

                MqServer.StartAListeningThread(queueName, config.HostName, int.Parse(config.Port), config.UserName,
                    config.Password,
                    delegate(object obj)
                    {
                        var env = obj as WXPayResult;
                        ws.Restart(queueName+$"开始处理otn:{env.out_trade_no}。。。");
                        WxServiceHelper.Md_PrcessWxPayCallBack(env);
                        ws.Stop();
                        //MDLogger.LogInfoAsync(typeof(Service1), $"由queue:{queueName},处理了otn:{env.out_trade_no}完成！");
                    });
            }

            MDLogger.LogInfoAsync(typeof(Service1), $"完成启动{numberOfQs}个支付回调队列监听！");
        }

        protected override void OnStop()
        {
            MqServer.CloseAll();
        }
    }
}
