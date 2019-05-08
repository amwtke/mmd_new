using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
using MD.Lib.ElasticSearch;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.Mail;
using MD.Model.Configuration.MQ;
using MD.Model.Index;
using MD.Model.MQ;
using MD.Model.MQ.MD;

namespace BK.LogService
{
    public partial class Service1 : ServiceBase
    {
        private static int emailFailSleepTimes = 0;
        private static double smtpFailTime = 0;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MQManager.RegisterConsumerProcessor<LogMQConfig>(delegate (BasicDeliverEventArgs ar, IModel channel)
            {
                var body = ar.Body;
                object obj = BinarySerializationHelper.DeserializeObject(body);

                if (obj != null)
                {
                    if (obj is BizMQ)
                    {
                        var env = obj as BizMQ;
                        if (env != null && !string.IsNullOrEmpty(env.Id) && !Guid.Parse(env.Id).Equals(Guid.Empty))
                        {
                            BizIndex index = LogESManager.CopyFromBizMQ(env);
                            LogESManager.AddOrUpdateBiz(index);
                        }
                    }
                    if (obj is LogEventMQ)
                    {
                        var env = obj as LogEventMQ;
                        if (env != null && !string.IsNullOrEmpty(env.Id) && !Guid.Parse(env.Id).Equals(Guid.Empty))
                        {
                            var index = LogESManager.CopyFromLogEventMQ(env);
                            LogESManager.AddOrUpdateLogEvent(index);
                        }

                        //发送email
                        if (env.Level.Equals(LogLevel.Error.ToString()))
                        {
                            MqEmailManager.SendMessage(new MqEmailObject()
                            {
                                TagetAddress= "865729986@qq.com",
                                Body = env.Exception,
                                Topic = $"MMD服务器错误！模块：{env.Domain};类:{env.LoggerName};时间：{env.TimeStamp}"
                            });
                        }
                    }

                }
                //channel.BasicAck(ar.DeliveryTag, false);
                //System.Threading.Thread.Sleep(10);
            });
            MQManager.Prepare_C_MQ<LogMQConfig>();

            MQManager.RegisterConsumerProcessor<EmailMQConfig>(delegate(BasicDeliverEventArgs ar, IModel channel)
            {
                var body = ar.Body;
                object obj = BinarySerializationHelper.DeserializeObject(body);

                if (!(obj is MqEmailObject)) return;
                try
                {
                    var env = obj as MqEmailObject;

                    //错误次数小于20可以重复发送。
                    if (emailFailSleepTimes < 20)
                    {
                        QQEMailHelper.SendMail(env.Topic, env.TagetAddress, env.Body);
                        //只要不报错就清空计数。
                        emailFailSleepTimes = 0;
                        smtpFailTime = 0;
                    }
                    else if (CommonHelper.GetUnixTimeNow() > smtpFailTime) //大于20次错误，就需要emailFailSleepTimes*60000 ms再试!
                    {
                        QQEMailHelper.SendMail(env.Topic, env.TagetAddress, env.Body);
                        //只要不报错就清空计数。
                        emailFailSleepTimes = 0;
                        smtpFailTime = 0;
                    }
                }
                catch (Exception ex)
                {
                    emailFailSleepTimes++;
                    if (emailFailSleepTimes < 20)
                    {
                        Thread.Sleep(emailFailSleepTimes * 5000);
                        MDLogger.LogInfo(typeof(Service1), $"sleeptime:{emailFailSleepTimes * 5000} ms!---" + ex.ToString());
                    }
                    else
                    {
                        //大于20次就是服务器错误，需要记录服务器不服务的时间。
                        smtpFailTime = CommonHelper.GetUnixTimeNow() + emailFailSleepTimes*60000;
                    }
                    //大于20次错误就不提示错误了！
                }
            });

            MQManager.Prepare_C_MQ_TB<EmailMQConfig>();
        }

        protected override void OnStop()
        {
            MQManager.CloseAll();
        }
    }
}
