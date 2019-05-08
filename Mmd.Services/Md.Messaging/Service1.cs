using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.ElasticSearch;
using MD.Lib.Log;
using MD.Lib.MQ;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Weixin.Message;
using MD.Lib.Weixin.Message.TemplateMessageObjects;
using MD.Model.Configuration.MQ;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.Index;
using MD.Model.MQ;
using MD.Model.MQ.MD;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using MD.Lib.Weixin.Services;
using System.Configuration;
using Senparc.Weixin.MP.AdvancedAPIs.MyExtension;

namespace Md.Messaging
{
    public partial class Service1 : ServiceBase
    {
        static bool CloseService = false;
        int timeEndHours = Convert.ToInt32(ConfigurationManager.AppSettings["timeEndHours"]);  //团结束时间，单位：小时
        int timeEndHours2 = Convert.ToInt32(ConfigurationManager.AppSettings["timeEndHours2"]);
        int timeInterval = Convert.ToInt32(ConfigurationManager.AppSettings["timeInterval"]); //扫描时间间隔，单位：分钟

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MDLogger.LogInfoAsync(typeof(Service1),"开始启动Md.Messaging服务....");
            MQManager.RegisterConsumerProcessor<MqWxTempMsgConfig>(async delegate (BasicDeliverEventArgs ar, IModel channel)
            {
                try
                {
                    
                    var body = ar.Body;
                    object obj = BinarySerializationHelper.DeserializeObject(body);

                    var env = obj as MqWxTempMsgObject;

                    if (env.ShortId == TemplateType.CustomerNews.ToString())    //发送图文消息，不是模板消息
                    {
                        await MyTemplateApi.SendNewsAsync(env.At, env.ToOpenId, env.Data);
                        return;
                    }


                    if (!string.IsNullOrEmpty(env?.At) && !string.IsNullOrEmpty(env.ToOpenId) && !string.IsNullOrEmpty(env.ShortId) && !string.IsNullOrEmpty(env.Url) && env.Data != null &&!string.IsNullOrEmpty(env.Appid))
                    {
                        //MDLogger.LogInfoAsync(typeof(Service1), $"进来一个新的模板消息请求!");
                        string tempMsgId = await TemplateMsgHelper.GetTempId(env.Appid, env.ShortId);

                        if (string.IsNullOrEmpty(tempMsgId))
                        {
                            MDLogger.LogErrorAsync(typeof(Service1),
                                new Exception($"获取魔板消息id错误!appid:{env.Appid},shortId:{env.ShortId}"));

                            return;
                        }
                            

                        string defaltColor = string.IsNullOrEmpty(env.TopColor)
                            ? TemplateMsgHelper.DefaultColor
                            : env.TopColor;

                        await TemplateMsgHelper.SendAsync(env.At,env.ToOpenId, tempMsgId, env.Data,env.Url, defaltColor);
                    }
                }
                catch (Exception ex)
                {
                    MDLogger.LogInfoAsync(typeof(Service1),$"发送模板消息异常！"+ex);
                }
            });
            MQManager.Prepare_C_MQ<MqWxTempMsgConfig>();
            MDLogger.LogInfoAsync(typeof(Service1), "Md.Messaging服务启动成功！");
            InitRemind();
        }

        /// <summary>
        /// 轮询查询将要结束的GroupOrder
        /// </summary>
        private void InitRemind()
        {
            AsyncHelper.RunAsync(delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始处理将要结束的GroupOrder模板消息.......");
                        WxServiceHelper.Md_GroupRemindProcess(timeEndHours, timeInterval);
                        WxServiceHelper.Md_GroupRemindProcess(timeEndHours2, timeInterval);
                        MDLogger.LogInfoAsync(typeof(Service1), "本次GroupOrder提醒模板消息处理结束！");

                        //休息一下
                        Thread.Sleep(TimeSpan.FromMinutes(timeInterval));
                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), ex);
                    }
                }
            }, null);
        }

        protected override void OnStop()
        {
            CloseService = true;
            MQManager.CloseAll();
        }
    }
}

