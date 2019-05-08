using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;
using MD.Model.Index;
using MD.Model.MQ;
using MD.Model.MQ.MD;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MD.Lib.Util;
using System.Threading;
using MD.Lib.Weixin.Services;

namespace MD.LotteryService
{
    /// <summary>
    /// 后台抽奖服务
    /// </summary>
    public partial class Service1 : ServiceBase
    {
        static bool CloseService = false;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MDLogger.LogInfo(typeof(Service1), "MD.LotteryService服务开始启动.......");
            Init();
            MDLogger.LogInfo(typeof(Service1), "MD.LotteryService服务已经启动！");
        }

        private void Init()
        {
            AsyncHelper.RunAsync(delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始处理到开奖时间的Group.......");

                        ProcessData();

                        MDLogger.LogInfoAsync(typeof(Service1), "本次抽奖处理结束！");

                        //休息一下
                        Thread.Sleep(TimeSpan.FromMinutes(5));
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), ex);
                    }
                }
            }, null);
            Thread.Sleep(1000);
        }

        private void ProcessData()
        {
            WxServiceHelper.Md_GroupLottery_Process();
        }

        protected override void OnStop()
        {
            CloseService = true;
        }

        private void Onclose()
        {
            CloseService = true;
            //MQManager.CloseAll();
            //Thread.Sleep(1000);
        }
    }
}
