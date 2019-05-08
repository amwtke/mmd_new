1、这里实现的队列是P端发布到特定的Exchange通道上，C端利用订阅Exchange通道名，创建Queue来取数据；P端默认启动所有的Exchange监听，而
	C端只启动Register了处理流程的C；
2、配置文件中可以指定一个P可以启动N个C来消费；如：启动3个C来消费一个P的例子；
	TestMQ testmq = new TestMQ();
            testmq.ExchangeName = "xj";
            testmq.QueueName = "task_queue";
            testmq.HostName = "localhost";
            testmq.Port = "5677";
            testmq.Password = "123456";
            testmq.UserName = "xj";
            testmq.SpermThreshold = "5000";
            testmq.NumberOfC = "3";
            DBinitialHelper.MakeList(testmq).ForEach(c => context.BKConfigs.Add(c));
	这3个C是启动了三个Connection来消费的；其中三个Queue是按照routekey来随机路由的，即，从P端进入时就随机选取一个RouteKey来投放。
3、例子（注必须引用MD.Lib、MD.Model、MD.Configuration，EF，RabbitMQ.net等包）
==================================P端=============================================== 
static void Main(string[] args)
        {
            MQManager.Prepare_All_P_MQ();
            for (int i = 0; i < 10000; i++)
                MQManager.SendMQ<TestMQ>("hello world!");
            

            Console.ReadKey();
        }
=====================================================================================

=================================C端=================================================
static void Main(string[] args)
        {
            MQManager.RegisterConsumerProcessor<MD.Model.Configuration.MQ.TestMQ>(delegate (BasicDeliverEventArgs ar, IModel channel)
            {
                var body = ar.Body;
                var env = BinarySerializationHelper.DeserializeObject(body) as object;
                string s = env.ToString();
                Console.WriteLine(s);
                Console.WriteLine("Thread ID" + System.Threading.Thread.CurrentThread.ManagedThreadId);

                channel.BasicAck(ar.DeliveryTag, false);
                System.Threading.Thread.Sleep(1);
            });
            MQManager.Prepare_All_C_MQ();
            Console.ReadKey();
        }
=====================================================================================

注：可以在AppSetting中配置本地数据库进行测试
  <appSettings>
    <add key="LocalDBName" value="DB_C"/>
  </appSettings>