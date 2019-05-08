using MD.Lib.Util;
using MD.Model.Configuration.MQ;
using MD.Model.Index;
using MD.Model.MQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.MQ
{
    public static class LogMQOP
    {
        static LogMQOP()
        {
            try
            {
                MQManager.Prepare_All_P_MQ();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private static LogMQObject GenObject(object obj)
        {
            LogMQObject ret = null;
            if(obj is LogEventMQ)
            {
                ret = new LogMQObject();
                ret.Type = LogType.Log;
                ret.Event = obj as LogEventMQ;
            }
            else if(obj is BizMQ)
            {
                ret = new LogMQObject();
                ret.Type = LogType.Biz;
                ret.BizObject = obj as BizMQ;
            }
            return ret;
        }

        public static LogEventMQ GenLogEvent(Type t,string message,Exception ex=null,LogLevel level = LogLevel.Info)
        {
            LogEventMQ ret = new LogEventMQ();
            ret.HostName = CommonHelper.GetHostName();
            ret.Level = level.ToString();
            ret.ThreadName = CommonHelper.GetThreadId();
            ret.Domain = CommonHelper.GetDomain();
            ret.Os = CommonHelper.GetOSName();
            ret.LoggerName = CommonHelper.GetLoggerName(t);
            ret.TimeStamp = CommonHelper.GetLoggerDateTime(DateTime.Now);
            if (ex != null)
            {
                ret.Exception = ex.ToString() + ex.StackTrace;
                ret.Level = LogLevel.Error.ToString();
            }
            ret.Message = message;
            return ret;
        }

        public static BizMQ GenBizIndex(Type t,string modelname, string openid, Guid useruuid, string message)
        {
            BizMQ index = new BizMQ(modelname, openid, useruuid, message);
            index.LoggerName = CommonHelper.GetLoggerName(t);
            index.TimeStamp = CommonHelper.GetLoggerDateTime(DateTime.Now);
            index.UserIp = CommonHelper.GetHostName();
            return index;
        }

        public static bool SendMessage(object obj)
        {
            LogMQObject log = GenObject(obj);
            if(log!=null)
                return MQManager.SendMQ_TB<LogMQConfig>(obj);
            return false;
        }

        public static async Task<bool> SendMessageAsync(object obj)
        {
            LogMQObject log = GenObject(obj);
            if (log != null)
                await MQManager.SendMQ<LogMQConfig>(obj);
            return true;
        }
        public static void SendMessageTestAsync(object obj)
        {
            LogMQObject log = GenObject(obj);
            if (log != null)
                 MQManager.SendMQ<LogMQConfig>(obj);
        }
    }
}
