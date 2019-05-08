using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MD.Lib.Weixin.Vector;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB;
using MD.Model.DB.Professional;

namespace Md.VectorService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = MdConfigurationManager.GetConfig<MqVectorConfig>();

            MDLogger.LogInfoAsync(typeof(Service1), $"开始启动vector监听服务........");

            MqServer.StartAListeningThreadAsync(config.QueueName, config.HostName, int.Parse(config.Port), config.UserName,
                config.Password, async delegate(object obj)
                {
                    try
                    {
                        Vector env = obj as Vector;
                        await VectorProcessorManager.Route(env);
                        //MDLogger.LogInfoAsync(typeof(Service1), $"处理了一个vector:{env.vid}");
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1),ex);
                    }
                });

            MDLogger.LogInfoAsync(typeof(Service1), $"完成Vector队列监听服务的启动！");
        }

        protected override void OnStop()
        {
            MqServer.CloseAll();
            MDLogger.LogInfoAsync(typeof(Service1), "Vector服务已关闭！");
        }
    }
}
