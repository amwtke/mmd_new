using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;
using MD.Lib.MQ.RPC;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.MQ.RPC;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MD.Lib.MQ
{
    /// <summary>
    /// 对任意的队列建立监听，启动新的线程执行。
    /// </summary>
    public static class MqServer
    {
        static MqServer()
        {

        }

        private static List<MqListener> _list = new List<MqListener>();

        public static void StartAListeningThread(string queueName, string ip, int port, string userName, string passWord,
            Action<object> process, bool durable = true, bool noAck = true)
        {
            var listener = new MqListener(queueName, ip, port, userName, passWord, durable, noAck, process);
            AsyncHelper.RunAsync(listener.StartListener, null);
            _list.Add(listener);
            MDLogger.LogInfoAsync(typeof(MqServer), $"_list.count:{_list.Count}");
        }

        public static void StartAListeningThreadAsync(string queueName, string ip, int port, string userName, string passWord,
    Action<object> process, bool durable = true, bool noAck = true)
        {
            var listener = new MqListener(queueName, ip, port, userName, passWord, durable, noAck, process);
            AsyncHelper.RunAsync(listener.StartListenerAsync, null);
            _list.Add(listener);
            MDLogger.LogInfoAsync(typeof(MqServer), $"_list.count:{_list.Count}");
        }

        public static void CloseAll()
        {
            foreach (var v in _list)
            {
                v.Close();
            }
        }
    }

    public class MqListener
    {
        private string qName, Ip, UserName, Password;
        private bool isDurable, isNoAck;
        private int Port;
        private Action<object> _processor;
        private IModel channel;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="durable">是不是要暂存在内存中。</param>
        /// <param name="noAck">true——表示收到后不会通知client已经收到，client不需要等待，提高队列的吞吐量。</param>
        /// <param name="process">Action的In参数模板是object类型。代表是从队列取出对象后序列化后的对象，需要在Action实体函数中对其进行as操作，以对应到客户端发送的特定class上。</param>
        public MqListener(string queueName, string ip, int port, string userName, string passWord,
            bool durable, bool noAck,
            Action<object> process)
        {
            qName = queueName;
            Ip = ip;
            Port = port;
            UserName = userName;
            Password = passWord;
            isDurable = durable;
            isNoAck = noAck;
            _processor = process;
        }

        /// <summary>
        /// 通用的创建队列监听的函数.为一个同步处理过程，会阻塞。
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="durable"></param>
        /// <param name="noAck">如果需要服务器发出处理完成的消息则设置成false，如果不需要给客户端返回消息则设置true</param>
        /// <param name="process"></param>
        /// <returns></returns>
        public void StartListener()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Ip,
                    Port = Port,
                    UserName = UserName,
                    Password = Password
                };
                using (var connection = factory.CreateConnection())
                {
                    using (channel = connection.CreateModel())
                    {
                        //如果没有创建queue，则确保queue存在 
                        channel.QueueDeclare(queue: qName,
                            durable: isDurable,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

                        //启动消费队列数据的程序
                        channel.BasicQos(0, 1, false);
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(queue: qName,
                            noAck: isNoAck,
                            consumer: consumer);

                        MDLogger.LogInfoAsync(typeof(MqServer),
                            $"computer:{CommonHelper.GetHostName()}已经启动了Mq:{qName}的监听！");

                        while (true)
                        {
                            //阻塞
                            var ea = consumer.Queue.Dequeue();

                            var body = ea.Body;
                            object obj = BinarySerializationHelper.DeserializeObject(body);

                            _processor(obj);

                            //发送回执
                            if (!isNoAck)
                                channel.BasicAck(ea.DeliveryTag, false);
                            //System.Threading.Thread.Sleep(1);
                        } //while
                    } //using
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MqServer), ex);
            }
        } //func

        /// <summary>
        /// 通用的创建队列监听的函数.为一个异步处理过程.
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="durable"></param>
        /// <param name="noAck">如果需要服务器发出处理完成的消息则设置成false，如果不需要给客户端返回消息则设置true</param>
        /// <param name="process"></param>
        /// <returns></returns>
        public void StartListenerAsync()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Ip,
                    Port = Port,
                    UserName = UserName,
                    Password = Password
                };
                using (var connection = factory.CreateConnection())
                {
                    using (channel = connection.CreateModel())
                    {
                        //如果没有创建queue，则确保queue存在 
                        channel.QueueDeclare(queue: qName,
                            durable: isDurable,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

                        //启动消费队列数据的程序
                        channel.BasicQos(0, 1, false);
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(queue: qName,
                            noAck: isNoAck,
                            consumer: consumer);

                        MDLogger.LogInfoAsync(typeof(MqServer),
                            $"computer:{CommonHelper.GetHostName()}已经启动了Mq:{qName}的监听！");

                        while (true)
                        {
                            //阻塞
                            var ea = consumer.Queue.Dequeue();

                            var body = ea.Body;
                            object obj = BinarySerializationHelper.DeserializeObject(body);

                            //_processor(obj);
                            AsyncHelper.RunAsync<object>(_processor, obj, null);
                            //发送回执
                            if (!isNoAck)
                                channel.BasicAck(ea.DeliveryTag, false);
                        } //while
                    } //using
                }
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MqServer), ex);
            }
        }

        public void Close()
        {
            if (channel.IsOpen)
                channel.Close();
        }
    }
    
    /// <summary>
    /// 可以向任意队列发送数据包。
    /// </summary>
    public class MqClient
    {
        private IConnection connection;
        private IModel channel;
        private string qName;

        public MqClient(string queueName, string ip, int port, string userName, string passWord, bool durable = true)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = ip,
                    Port = port,
                    UserName = userName,
                    Password = passWord
                };
                qName = queueName;
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                //如果queue不存在，则先行创建
                channel.QueueDeclare(queue: qName,
                    durable: durable,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(MqClient), ex);
            }

        }
        /// <summary>
        /// 向特定队列发送数据包的函数。
        /// </summary>
        /// <param name="obj">这个类型必须与MqListener构造函数中的处理函数process中的object为同一个class结构。</param>
        /// <returns></returns>
        public async Task<bool> SendMessageAsync(object obj)
        {
            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var messageBytes = BinarySerializationHelper.SerializeObject(obj);
                //channel.BasicPublish(exchange: "",
                //routingKey: qName,
                //basicProperties: properties,
                //body: messageBytes);

                await AsyncHelper.RunAsync<IModel>(delegate(IModel model)
                {
                    model.BasicPublish("", qName, properties, messageBytes);

                }, channel);
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqClient), ex);
            }
        }

        public bool SendMessage(object obj)
        {
            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var messageBytes = BinarySerializationHelper.SerializeObject(obj);
                //channel.BasicPublish(exchange: "",
                //routingKey: qName,
                //basicProperties: properties,
                //body: messageBytes);

                //await AsyncHelper.RunAsync<IModel>(delegate (IModel model)
                //{
                channel.BasicPublish("", qName, properties, messageBytes);

                //}, channel);
                return true;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MqClient), ex);
            }
        }
    }
}
