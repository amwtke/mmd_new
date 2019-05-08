using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.Util;
using MD.Lib.Weixin.Services;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.MQ.MD;
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
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Md.RemindMessageService
{
    /*
     * 组团结束前发送模板消息提醒服务
     * 未启用
     * 已合并到Md.Messaging服务中
     */
    #region 组团结束9小时前发送模板消息提醒服务
    public partial class Service1 : ServiceBase
    {
        static bool CloseService = false;

        int timeEndHours = 9;  //团结束时间，单位：小时
        int timeInterval = 20; //扫描时间间隔，单位：分钟

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MDLogger.LogInfo(typeof(Service1), "MD.RemindMessageService服务开始启动.......");
            //System.IO.File.AppendAllText(@"d:\log.txt", "RemindMessageService start" + ",time:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + Environment.NewLine);
            Init();
            MDLogger.LogInfo(typeof(Service1), "MD.RemindMessageService服务已经启动！");
        }

        private void Init()
        {
            AsyncHelper.RunAsync(delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始处理将要结束的GroupOrder.......");

                        ProcessData();

                        MDLogger.LogInfoAsync(typeof(Service1), "本次GroupOrder提醒模板消息处理结束！");

                        //休息一下
                        Thread.Sleep(TimeSpan.FromMinutes(20));
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), ex);
                        System.IO.File.AppendAllText(@"d:\log.txt", "RemindMessageService ex" + ex.Message + ex.StackTrace + ",time:" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + Environment.NewLine);
                    }
                }
            }, null);
            Thread.Sleep(1000);

        }

        private void ProcessData()
        {
            WxServiceHelper.Md_GroupRemindProcess(timeEndHours, timeInterval);

        }

        protected override void OnStop()
        {
            MDLogger.LogInfo(typeof(Service1), "MD.RemindMessageService服务开始关闭.......");
            Onclose();
            MDLogger.LogInfo(typeof(Service1), "MD.RemindMessageService服务已经关闭！");
        }

        private void Onclose()
        {
            CloseService = true;
            //MQManager.CloseAll();
            //Thread.Sleep(1000);
        }
    }
    #endregion

}
