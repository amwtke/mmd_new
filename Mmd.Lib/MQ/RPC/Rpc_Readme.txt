创建一个RPC服务的服务器与客户端大体准备这么几个类：
1、对RpcArgs与RpcResult的封装与解析（Lib.MQ.RPC.ArgsAndRets空间下）;
2、创建对服务方法的wrapper，包装成符合Func<RpcArgs,RpcResults>的形式；（写在哪都行）
3、为服务编写配置对象，在MD.Model.Configuration.MQ.RPC空间下，必须继承RpcConfigBase类；
4、根据配置与wrapper启动服务：RpcServerFactory<RpcGetComponentAtConfig>.StartRpcServer(RpcWrapper.GetComponentAtWrapper);
5、根据配置启动Client：
MQRpcClient<RpcGetAuthorizerAtConfig> _AuthorizerAtClient = new MQRpcClient<RpcGetAuthorizerAtConfig>();
 _AuthorizerAtClient.Start();

 6、调用服务
 var ret = _AuthorizerAtClient.Call(GetAuthorizerAtFunc.GenArgs(authorizerAppid));
 7、解出返回值
 atString = GetAuthorizerAtFunc.GetAtFromRpcResult(ret);




1、RpcFactory.cs是启动服务于客户端的文件，内部包含三个类
	1.1 RpcServerFactory——用于启动服务,管理服务；
	1.2 MQRpcServer——用于启动一个服务器
	1.3	MQRpcClient——用于启动一个客户端

	1.4结构
	func->funcWrapper->factory(server client)
	所有要产生的新服务，必须被包装成Func<RpcArgs,RpcResult>的形式才能被启动。
	而每个服务必须提供通过RpcArgs获取调用参数与通过RpcResult取得结果的方法，这个方法写在
	mmd.Lib.MQ.Rpc.ArgsAndRets命名空间下。

2、写一个新的rpc服务的过程
	2.1 在MD.Lib.MQ.RPC.ArgsAndRets命名空间下为RpcArgs与RpcResult写包装函数，如：
	public static class GetAuthorizerAtFunc(Wrapper)
    {
		//通过参数产生RpcArgs对象的函数（client调用server时的call函数需要RpcArgs对象）
        public static RpcArgs GenArgs(string appid)
        {
            RpcArgs ret = new RpcArgs { ["AppId"] = appid };
            return ret;
        }

		//通过封装对象获取具体的参数值。server的wrapper函数需要用它来解出具体的参数值。
        public static string GetAppidFromRpcArgs(RpcArgs ra)
        {
            return ra["AppId"].ToString();
        }
		//server的wrapper函数通过它来产生返回值
        public static RpcResults GenResults(string at)
        {
            RpcResults ret = new RpcResults { ["at"] = at };
            return ret;
        }

		//客户端调用者通过这个函数来解出具体的返回值。
        public static string GetAtFromRpcResult(RpcResults rr)
        {
            return rr["at"].ToString();
        }
    }

	2.2 编写server的wrapper函数
		2.2.1 接收一个RpcArgs参数，并通过上面get的方法（GetAppidFromRpcArgs）获取到相关的调用参数；
		2.2.2 调用原生函数
		2.2.3 通过原生函数返回的值，包装一个RpcResult对象

	public static RpcResults GetAuthorizerAtWrapper(RpcArgs args)
        {
			//通过RpcArgs对象解出具体的参数
            string appid = GetAuthorizerAtFunc.GetAppidFromRpcArgs(args);
            if (!string.IsNullOrEmpty(appid))
            {
				//用解出的参数，调用原生函数
                var at = Helper.GetAuthorizerAtByAppId(appid);

				//返回的结果包装成一个RpcResult对象，并返回。
                var ret = GetAuthorizerAtFunc.GenResults(at);
                return ret;
            }
            return null;
        }

	2.3 启动服务
	1、获取一个rpc的配置对象，如：RpcGetComponentAtConfig；2、使用wrapper方法，调用RpcServerFactory，启动服务。
	RpcServerFactory<RpcGetComponentAtConfig>.StartRpcServer(RpcWrapper.GetComponentAtWrapper);


	2.4 启动客户端
	private static MQRpcClient<RpcGetAuthorizerAtConfig> _AuthorizerAtClient = new MQRpcClient<RpcGetAuthorizerAtConfig>();
	 _AuthorizerAtClient.Start();

	调用：
	var ret = _AuthorizerAtClient.Call(GetAuthorizerAtFunc.GenArgs(authorizerAppid));
                if (ret != null)
                {
				//通过Get方法解出相关的返回值
                    atString = GetAuthorizerAtFunc.GetAtFromRpcResult(ret);
                }
                return atString;
	停止客户端：注意客户端的资源需要Close掉。
	_AuthorizerAtClient.Close();