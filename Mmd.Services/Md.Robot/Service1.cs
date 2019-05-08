using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.Robot;
using MD.Model.DB;

namespace Md.Robot
{
    public partial class Service1 : ServiceBase
    {
        private static bool CloseService = false;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            AsyncHelper.RunAsync(async delegate ()
            {
                while (!CloseService)
                {
                    try
                    {
                        MDLogger.LogInfoAsync(typeof(Service1), "开始投放机器人服务.......");

                        await ProcessData();

                        MDLogger.LogInfoAsync(typeof(Service1), "本次机器人投放轮询结束！");

                    }
                    catch (Exception ex)
                    {
                        MDLogger.LogErrorAsync(typeof(Service1), ex);
                    }

                    //休息一下
                    System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
                }
                MDLogger.LogInfo(typeof(Service1), "md.robot服务已经停止！");
            }, null);

            MDLogger.LogInfo(typeof(Service1), "md.robot服务已经启动！");
        }

        private static async Task ProcessData()
        {
            //筛选出离团活动还剩30分钟的团，并且同意投放的团进行成团投放处理
            using (var repo = new BizRepository())
            {
                //筛选出距离结束还有30分钟的go
                var golist = await repo.GroupOrderGetbyExpiretimeAsync(30);
                if (golist != null && golist.Count > 0)
                {
                    foreach (var go in golist)
                    {
                        var userRobot = await AttHelper.GetValueAsync(go.gid, EAttTables.Group.ToString(), EGroupAtt.userobot.ToString());

                        //不用robot
                        if(userRobot==null || userRobot=="0")
                            continue;

                        await RobotHelper.CompleteAGo(go);
                        MDLogger.LogInfoAsync(typeof(Service1),$"对go:{go.goid}使用了机器人。");
                    }
                }
            }
        }

        protected override void OnStop()
        {
            CloseService = true;
        }
    }
}
