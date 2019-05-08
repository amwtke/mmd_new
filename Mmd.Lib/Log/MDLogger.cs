using MD.Lib.MQ;
using MD.Lib.Util;
using MD.Model.Index;
using MD.Model.MQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Log
{
    public static class MDLogger
    {
        public static void LogInfoAsync(Type t, object message)
        {
            var obj = LogMQOP.GenLogEvent(t, message.ToString());
            LogMQOP.SendMessage(obj);
        }

        public static void LogInfo(Type t, object message)
        {
            var obj = LogMQOP.GenLogEvent(t, message.ToString());
            LogMQOP.SendMessage(obj);
        }

        public static void LogBizAsync(Type t,BizMQ bo)
        {
            bo.TimeStamp = CommonHelper.GetLoggerDateTime(DateTime.Now);
            bo.LoggerName = CommonHelper.GetLoggerName(t);
            bo.HostName = CommonHelper.GetHostName();
            LogMQOP.SendMessage(bo);
        }

        public static void LogBiz(Type t,BizMQ bo)
        {
            bo.TimeStamp = CommonHelper.GetLoggerDateTime(DateTime.Now);
            bo.LoggerName = CommonHelper.GetLoggerName(t);
            bo.HostName = CommonHelper.GetHostName();
            LogMQOP.SendMessage(bo);
        }

        public static void LogErrorAsync(Type t, Exception ex)
        {
            var obj = LogMQOP.GenLogEvent(t,"ERROR",ex);
            LogMQOP.SendMessage(obj);
        }

        public static async Task<bool> LogErrorAsync2(Type t, Exception ex)
        {
            var obj = LogMQOP.GenLogEvent(t, "ERROR", ex);
            await LogMQOP.SendMessageAsync(obj);
            return true;
        }

        public static void LogErrorTestAsync(Type t, Exception ex)
        {
            var obj = LogMQOP.GenLogEvent(t, "ERROR", ex);
            LogMQOP.SendMessageTestAsync(obj);
        }

        public static void LogError(Type t, Exception ex)
        {
            var obj = LogMQOP.GenLogEvent(t, "ERROR", ex);
            LogMQOP.SendMessage(obj);
        }
    }
}
