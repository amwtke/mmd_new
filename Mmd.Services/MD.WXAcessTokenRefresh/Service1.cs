using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;
using MD.Lib.MQ.RPC;
using MD.Lib.Util;
using MD.Model.Configuration.MQ.RPC;
using MD.WXAcessTokenRefresh.ComponentAt;

namespace MD.WXAcessTokenRefresh
{
    public partial class MD_ATRefresh : ServiceBase
    {
        
        public MD_ATRefresh()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MDLogger.LogInfoAsync(typeof(MD_ATRefresh),"刷新AT的服务开始启动！");
            RpcServerFactory<RpcGetComponentAtConfig>.StartRpcServer(RpcWrapper.GetComponentAtWrapper);
            RpcServerFactory<RpcGetComponentAtConfig>.StartRpcServer(RpcWrapper.GetComponentAtWrapper);
            RpcServerFactory<RpcGetAuthorizerAtConfig>.StartRpcServer(RpcWrapper.GetAuthorizerAtWrapper);
        }

        protected override void OnStop()
        {
            MDLogger.LogInfoAsync(typeof(MD_ATRefresh), "刷新AT的服务已关闭！");
        }
    }
}
