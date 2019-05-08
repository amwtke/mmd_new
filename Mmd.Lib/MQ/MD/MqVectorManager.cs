using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Vector;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.DB.Professional;

namespace MD.Lib.MQ.MD
{
    public static class MqVectorManager
    {
        private static MqClient _client = null;
        private static MqVectorConfig _config = MdConfigurationManager.GetConfig<MqVectorConfig>();
        static MqVectorManager()
        {
            try
            {
                _client = new MqClient(_config.QueueName, _config.HostName, int.Parse(_config.Port), _config.UserName, _config.Password);
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqVectorManager), ex);
            }
        }

        public static async void SendAsync(Vector v)
        {
            await _client.SendMessageAsync(v);
        }

        public static void Send(Vector v)
        {
            _client.SendMessage(v);
        }
        public static void Send<T>(object o) where T :IVectorProcessor,new()
        {
            IVectorProcessor p = new T();
            var v = p?.GenVector(o);
            if (v != null)
            {
                Send(v);
            }
        }
    }
}
