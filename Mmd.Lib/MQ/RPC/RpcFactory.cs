using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.Configuration.MQ.RPC;
using MD.Model.MQ.RPC;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MD.Lib.MQ.RPC
{
    public static class RpcServerFactory<Config> where Config:RpcConfigBase,new()
    {
        private static ConcurrentDictionary<Type,List<MQRpcServer<Config>>> _dic = new ConcurrentDictionary<Type, List<MQRpcServer<Config>>>();

        public static void StartRpcServer(Func<RpcArgs, RpcResults> func)
        {
            var config = MdConfigurationManager.GetConfig<Config>();
            if(config==null || func==null)
                throw new MDException(typeof(RpcServerFactory<Config>), $"RpcServerFactory取不到配置或者func没有指定！config:{typeof(Config)}");
            Type t = typeof (Config);
            var server = new MQRpcServer<Config>(func);

            //启动线程
            AsyncHelper.RunAsync(server.Start,null);

            if(!_dic.ContainsKey(t))
                _dic[t] = new List<MQRpcServer<Config>>();
            _dic[t].Add(server);
        }
    }
    public class MQRpcServer<Config> where Config:RpcConfigBase,new()
    {
        private string _ServerName;
        private string _ServerQueue;
        private string _ServerMqIp;
        private int _ServerMqPort;
        private Func<RpcArgs, RpcResults> _rpcFunc;
        private string _ComputerName;
        private RpcConfigBase config;
        public MQRpcServer(Func<RpcArgs, RpcResults> func)
        {
            config = MdConfigurationManager.GetConfig<Config>();
            if(config==null)
                throw new MDException(typeof(MQRpcServer<Config>),$"RpcServer取不到配置！config:{typeof(Config)}");
            _ServerName = config.ServerName;
            _ServerQueue = config.QueueName;
            _ServerMqIp = config.Host;
            _ServerMqPort = int.Parse(config.Port);
            _rpcFunc = func;
            _ComputerName = CommonHelper.GetHostName();
        }

        /// <summary>
        /// 会阻塞
        /// </summary>
        public void Start()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _ServerMqIp,
                Port = _ServerMqPort,
                UserName = config.UserName,
                Password = config.PassWord
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _ServerQueue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                channel.BasicQos(0, 1, false);
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(queue: _ServerQueue,
                    noAck: false,
                    consumer: consumer);
                MDLogger.LogInfoAsync(typeof(MQRpcServer<Config>), $"computer:{_ComputerName}已经启动了RPC:{_ServerName}");
                while (true)
                {
                    //阻塞
                    var ea = consumer.Queue.Dequeue();

                    var body = ea.Body;
                    object obj = BinarySerializationHelper.DeserializeObject(body);
                    if (obj != null && obj is RpcArgs)
                    {
                        AsyncHelper.RunAsync(delegate()
                        {
                            RpcArgs args = obj as RpcArgs;

                            var props = ea.BasicProperties;
                            var replyProps = channel.CreateBasicProperties();
                            replyProps.CorrelationId = props.CorrelationId;
                            try
                            {
                                var results = _rpcFunc(args);
                                if (results != null)
                                {
                                    var responseBytes = BinarySerializationHelper.SerializeObject(results);
                                    channel.BasicPublish(exchange: "",
                                        routingKey: props.ReplyTo,
                                        basicProperties: replyProps,
                                        body: responseBytes);
                                }
                            }
                            catch (Exception e)
                            {
                                MDLogger.LogErrorAsync(typeof (MQRpcServer<Config>), e);
                            }
                            finally
                            {
                                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            }
                        }, null);
                    }
                }//while
            }//using

        } //init
    }


    public class MQRpcClient<Config> where Config : RpcConfigBase,new()
    {
        private bool _isStarted;
        string _ClientMqHost;
        int _ClientMqPort;
        string _ServerQueue;

        private IConnection connection;
        private IModel channel;
        private string replyQueueName;
        private QueueingBasicConsumer consumer;
        private static readonly ConcurrentDictionary<string,RpcResults> _resultsDic = new ConcurrentDictionary<string, RpcResults>();
        //Stopwatch ws = new Stopwatch();
        private readonly RpcConfigBase config;
        private readonly RpcClinetConfig clientConfig;

        private int retryTimes;
        private int retryDelta;

        public bool IsStarted => _isStarted;
        public MQRpcClient()
        {
            _isStarted = false;

            config =  MdConfigurationManager.GetConfig<Config>();
            clientConfig = MdConfigurationManager.GetConfig<RpcClinetConfig>();
            if (config == null || clientConfig==null)
            {
                throw new MDException(typeof(MQRpcClient<Config>),$"Rpc client找不到配置！Config:{typeof(Config)}");
            }
            _ClientMqHost = config.Host;
            _ClientMqPort = int.Parse(config.Port);
            _ServerQueue = config.QueueName;

            retryDelta = int.Parse(clientConfig.RetryDelta);
            retryTimes = int.Parse(clientConfig.ClientRetryTimes);
        }

        public void Start()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _ClientMqHost,
                Port = _ClientMqPort,
                UserName = config.UserName,
                Password = config.PassWord
            };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            //匿名的replyqueue
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(queue: replyQueueName,
                                 noAck: true,
                                 consumer: consumer);

            //开始处理
            AsyncHelper.RunAsync(delegate()
            {
                while (true)
                {
                    var ea = consumer.Queue.Dequeue();
                    var body = ea.Body;
                    object obj = BinarySerializationHelper.DeserializeObject(body);
                    if (obj is RpcResults)
                        _resultsDic[ea.BasicProperties.CorrelationId] = obj as RpcResults;
                }
            }, null);

            _isStarted = true;
        }

        public RpcResults Call(RpcArgs args)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            props.CorrelationId = corrId;

            var messageBytes = BinarySerializationHelper.SerializeObject(args);
            //发射
            channel.BasicPublish(exchange: "",
                routingKey: _ServerQueue,
                basicProperties: props,
                body: messageBytes);

            //等待响应
            RpcResults ret = null;
            //ws.Reset();
            //ws.Start();
            //4次重试
            for (int i = 0; i <= retryTimes; i++)
            {
                Thread.Sleep(5);
                if (_resultsDic.TryRemove(corrId, out ret))
                {
                    //ws.Stop();
                    //MDLogger.LogInfoAsync(typeof(MQRpcClient<Config>),$"{_ServerQueue}RPC第{i}次取到值！");
                    //MDLogger.LogInfoAsync(typeof(MQRpcClient<Config>), $"{_ServerQueue}RPC耗时:{ws.ElapsedMilliseconds}");

                    return ret;
                }
                    
                Thread.Sleep(10+(i* retryDelta));
            }
            //ws.Stop();
            return ret;
        }

        public void Close()
        {
            connection?.Close();
            _isStarted = false;
        }
    }
}

