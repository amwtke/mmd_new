using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration.DB;
using MD.Model.Configuration;
using MD.Model.Configuration.Att;
using MD.Model.Configuration.ElasticSearch;
using MD.Model.Configuration.MQ;
using MD.Model.Configuration.PaaS;
using MD.Model.Configuration.Redis;
using MD.Model.Configuration.User;
using MD.Model.DB;

namespace MD.Configuration.DB
{
    public class ConfigDbContext : DbContext
    {
        public ConfigDbContext() : base("name=MDConfigContext")
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Database.Initialize(false);
            //Database.SetInitializer<ConfigDbContext>(new MdConfigInit<ConfigDbContext>());
        }

        public ConfigDbContext(string conString) : base(conString)
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Database.Initialize(false);
            //Database.SetInitializer<ConfigDbContext>(new MdConfigInit<ConfigDbContext>());
        }

        public DbSet<MDConfigItem> MDConfigs { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }

    public class MdConfigInit<T> : CreateDatabaseIfNotExists<ConfigDbContext>
    {
        protected override void Seed(ConfigDbContext context)
        {
            base.Seed(context);
            string appDomain = ConfigurationManager.AppSettings["ConfigDomain"];

            WeixinConfig weixinobject = new WeixinConfig();
            weixinobject.WeixinAppId = ConfigurationManager.AppSettings["WeixinAppId"];
            weixinobject.WeixinAppSecret = ConfigurationManager.AppSettings["WeixinAppSecret"];
            weixinobject.WeixinEncodingAESKey = ConfigurationManager.AppSettings["WeixinEncodingAESKey"];
            weixinobject.WeixinToken = ConfigurationManager.AppSettings["WeixinToken"];


            LogConfig log = new LogConfig();
            log.TraceResponse = "false";
            log.RemotePort = "9200";
            log.RemoteAddress = "10.24.38.224";

            #region weixin
            MDConfigAttribute wxMf = typeof(WeixinConfig).GetCustomAttributes(false)[0] as MDConfigAttribute;

            List<MDConfigItem> weixinItems = new List<MDConfigItem>();
            weixinItems.Add(new MDConfigItem { Domain = appDomain, Module = wxMf.Module, Function = wxMf.Function, Key = "WeixinAppId", Value = weixinobject.WeixinAppId, TimeStamp = Helper.ToUnixTime(DateTime.Now) });

            weixinItems.Add(new MDConfigItem { Domain = appDomain, Module = wxMf.Module, Function = wxMf.Function, Key = "WeixinAppSecret", Value = weixinobject.WeixinAppSecret, TimeStamp = Helper.ToUnixTime( DateTime.Now )});

            weixinItems.Add(new MDConfigItem { Domain = appDomain, Module = wxMf.Module, Function = wxMf.Function, Key = "WeixinEncodingAESKey", Value = weixinobject.WeixinEncodingAESKey, TimeStamp = Helper.ToUnixTime(DateTime.Now) });

            weixinItems.Add(new MDConfigItem { Domain = appDomain, Module = wxMf.Module, Function = wxMf.Function, Key = "WeixinToken", Value = weixinobject.WeixinToken, TimeStamp = Helper.ToUnixTime(DateTime.Now) });
            weixinItems.ForEach(a => context.MDConfigs.Add(a));
            #endregion


            #region log
            MDConfigAttribute logMf = typeof(LogConfig).GetCustomAttributes(false)[0] as MDConfigAttribute;
            List<MDConfigItem> logItems = new List<MDConfigItem>();

            logItems.Add(new MDConfigItem { Domain = appDomain, Module = logMf.Module, Function = logMf.Function, Key = "TraceResponse", Value = log.TraceResponse, TimeStamp = Helper.ToUnixTime(DateTime.Now) });

            logItems.Add(new MDConfigItem { Domain = appDomain, Module = logMf.Module, Function = logMf.Function, Key = "RemotePort", Value = log.RemotePort, TimeStamp = Helper.ToUnixTime(DateTime.Now) });

            logItems.Add(new MDConfigItem { Domain = appDomain, Module = logMf.Module, Function = logMf.Function, Key = "RemoteAddress", Value = log.RemoteAddress, TimeStamp = Helper.ToUnixTime(DateTime.Now) });
            logItems.ForEach(b => context.MDConfigs.Add(b));



            #endregion


            #region MQ

            WeChatMessageMQConfig wcMq = new WeChatMessageMQConfig();
            wcMq.ExchangeName = "WeChatMessage_Exchange";
            wcMq.QueueName = "WeChatMessage_queue";
            wcMq.HostName = "10.24.38.224";
            wcMq.Port = "5672";
            wcMq.Password = "rabbit@46355";
            wcMq.UserName = "xj";
            wcMq.SpermThreshold = "5000";
            wcMq.NumberOfC = "1";
            DBinitialHelper.MakeList(wcMq, appDomain).ForEach(c => context.MDConfigs.Add(c));

            EmailMQConfig emailMq = new EmailMQConfig();
            emailMq.ExchangeName = "Email_Exchange";
            emailMq.QueueName = "Email_queue";
            emailMq.HostName = "10.24.38.224";
            emailMq.Port = "5672";
            emailMq.Password = "rabbit@46355";
            emailMq.UserName = "xj";
            emailMq.SpermThreshold = "5000";
            emailMq.NumberOfC = "1";
            DBinitialHelper.MakeList(emailMq, appDomain).ForEach(c => context.MDConfigs.Add(c));

            SMMQConfig smmq = new SMMQConfig();
            smmq.ExchangeName = "sm_Exchange";
            smmq.QueueName = "sm_queue";
            smmq.HostName = "10.24.38.224";
            smmq.Port = "5672";
            smmq.Password = "rabbit@46355";
            smmq.UserName = "xj";
            smmq.SpermThreshold = "5000";
            smmq.NumberOfC = "1";
            DBinitialHelper.MakeList(smmq, appDomain).ForEach(c => context.MDConfigs.Add(c));

            LogMQConfig logmq = new LogMQConfig();
            logmq.ExchangeName = "log_Exchange";
            logmq.QueueName = "log_queue";
            logmq.HostName = "10.24.38.224";
            logmq.Port = "5672";
            logmq.Password = "rabbit@46355";
            logmq.UserName = "xj";
            logmq.SpermThreshold = "5000";
            logmq.NumberOfC = "1";
            DBinitialHelper.MakeList(logmq, appDomain).ForEach(c => context.MDConfigs.Add(c));


            //TestMQ testmq = new TestMQ();
            //testmq.ExchangeName = "xj";
            //testmq.QueueName = "task_queue";
            //testmq.HostName = "10.24.38.224";
            //testmq.Port = "5672";
            //testmq.Password = "rabbit@46355";
            //testmq.UserName = "xj";
            //testmq.SpermThreshold = "5000";
            //testmq.NumberOfC = "3";
            //DBinitialHelper.MakeList(testmq, appDomain).ForEach(c => context.MDConfigs.Add(c));

            WeChatRedisConfig wcRedisconfig = new WeChatRedisConfig();
            wcRedisconfig.MasterHostAndPort = "10.24.38.224:6379";
            wcRedisconfig.Password = "redis@8d756";
            wcRedisconfig.SlaveHostsAndPorts = "10.24.38.224:6380,";
            wcRedisconfig.StringSeperator = ",";
            DBinitialHelper.MakeList(wcRedisconfig, appDomain).ForEach(c => context.MDConfigs.Add(c));

            #endregion

            #region index

            LocationConfig location = new LocationConfig();
            location.IndexName = "userlocation";
            location.NumberOfReplica = "1";
            location.NumberOfShards = "2";
            location.RemoteAddress = "10.24.38.224";
            location.RemotePort = "9200";
            DBinitialHelper.MakeList(location, appDomain).ForEach(c => context.MDConfigs.Add(c));

            //ComplexLocationConfig complexLocation = new ComplexLocationConfig();
            //location.IndexName = "complexlocation";
            //location.NumberOfReplica = "1";
            //location.NumberOfShards = "2";
            //location.RemoteAddress = "10.24.38.224";
            //location.RemotePort = "9200";
            //DBinitialHelper.MakeList(complexLocation, appDomain).ForEach(c => context.MDConfigs.Add(c));
            #endregion

            #region userbehavior

            UserBehaviorConfig userBehaviorConfig = new UserBehaviorConfig();
            userBehaviorConfig.LoginTimeSpanMin = "10";
            userBehaviorConfig.GetMessageCount = "10";
            DBinitialHelper.MakeList(userBehaviorConfig, appDomain).ForEach(c => context.MDConfigs.Add(c));

            #endregion

            //#region PaaS

            //QiniuConfig qiniu = new QiniuConfig();
            //qiniu.ACCESS_KEY = "r3_0PzYByEpjj1nFIFkmo1wIqHGuRzdgckeBplji";
            //qiniu.SECRET_KEY = "YKWYVUm-DqE4xFWfgLbWWke5hvsbEe5G-iw-QJfL";
            //qiniu.ImgBUCKET = "eksns-img";
            //qiniu.ImgDOMAIN = "img.51science.cn";
            //qiniu.AttBUCKET = "eksns-att";
            //qiniu.AttDOMAIN = "7xme9o.dl1.z0.glb.clouddn.com";
            //qiniu.HDPBUCKET = "bk-headpic";
            //qiniu.HDPDOMAIN = "headpic.51science.cn";
            //qiniu.EKABUCKET = "bk-ektodaypic";
            //qiniu.EKADOMAIN = "wechatektodaypic.51science.cn";
            //DBinitialHelper.MakeList(qiniu, appDomain).ForEach(c => context.MDConfigs.Add(c));


            //SendCloudConfig sc = new SendCloudConfig();
            //sc.EDM_API_User = "51science_Push";
            //sc.SVR_API_User = "51science_Service";
            //sc.API_Key = "jjomHhfTTntIHW4u";
            //sc.SMS_API_User = "51science_SMS_Push";
            //sc.SMS_API_Key = "LFd0nUg0CCg3Ma5sla7yc2HaVx2nNqOH";
            //sc.RegisterValidation_TempleteId = "298";
            //sc.ResetPasswordValidation_EmailTempleteId = "bk_ResetPasswordValidation";
            //DBinitialHelper.MakeList(sc, appDomain).ForEach(c => context.MDConfigs.Add(c));
            //#endregion
            context.SaveChanges();
        }
    }

    internal static class DBinitialHelper
    {
        public static List<MDConfigItem> MakeList(Object o, string domain)
        {
            if (o == null || o.GetType().GetCustomAttributes(typeof(MDConfigAttribute), false).Length != 1)
                return null;

            List<MDConfigItem> list = new List<MDConfigItem>();

            MDConfigAttribute bkatt = o.GetType().GetCustomAttributes(typeof(MDConfigAttribute), false)[0] as MDConfigAttribute;
            string modulName = bkatt.Module;
            string funcName = bkatt.Function;

            o.GetType().GetProperties().ToList().ForEach(delegate (PropertyInfo property) {
                MDKeyAttribute key = property.GetCustomAttribute(typeof(MDKeyAttribute)) as MDKeyAttribute;
                string keyStr = key.Key;
                if (o.GetType().GetProperty(keyStr) == null)
                {
                    return;
                }
                string value = o.GetType().GetProperty(keyStr).GetValue(o).ToString();
                list.Add(new MDConfigItem { Domain = domain, Module = modulName, Function = funcName, Key = keyStr, Value = value, TimeStamp = Helper.ToUnixTime(DateTime.Now) });
            });
            return list;
        }
    }
}
