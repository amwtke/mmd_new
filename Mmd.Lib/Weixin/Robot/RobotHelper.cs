using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Pay;
using MD.Model.DB;
using MD.Model.DB.Code;
using Senparc.Weixin.MP.TenPayLibV3;

namespace MD.Lib.Weixin.Robot
{
    public static class RobotHelper
    {
        public static readonly Guid RobotMid=new Guid("11111111-1111-1111-1111-111111111111");
        public static readonly Guid RobotWoid = new Guid("11111111-1111-1111-1111-111111111111");

        public static readonly string RobotAppid = "Robot";
        public static readonly string RobotOpenidPrefix = "Robot";
        public static readonly string RobotMchId = "Robot";

        public static ConcurrentDictionary<Guid,string> _dic = new ConcurrentDictionary<Guid, string>();
        static object lock_object = new object();

        public static bool IsRobot(string openid)
        {
            return openid.StartsWith(RobotOpenidPrefix);
        }

        public static bool IsRobot(Guid uid)
        {
            return _dic.ContainsKey(uid);
        }

        static RobotHelper()
        {
            if (_dic.Count == 0)
            {
                lock (lock_object)
                {
                    if (_dic.Count == 0)
                    {
                        using (var repo = new BizRepository())
                        {
                            var list = repo.UserGetAllRobots();
                            foreach (var u in list)
                            {
                                _dic[u.uid] = u.openid;
                            }
                        }
                    }
                }
            }   
        }

        public static void ReloadRobots()
        {
            using (var repo = new BizRepository())
            {
                var list = repo.UserGetAllRobots();
                foreach (var u in list)
                {
                    _dic[u.uid] = u.openid;
                }
            }
        }

        public static async Task CompleteAGo(GroupOrder go)
        {
            try
            {
                if (go != null && go.status.Equals((int)EGroupOrderStatus.拼团进行中) && go.user_left > 0)
                {
                    int left = go.user_left.Value;
                    var robot_list = getRobotsOpenids(left);
                    using (var repo = new BizRepository())
                    {
                        var group = await repo.GroupGetGroupById(go.gid);
                        if (group != null)
                        {
                            foreach (var r in robot_list)
                            {
                                //判断库存数，当参团人数大于库存时， 就不再投放机器人。
                                if (group.person_quota > group.product_quota)
                                    continue;
                                var ctResult = await MdOrderBizHelper.CtProcess_robotAsync(go, group, r, RobotWoid, 0);
                                if (ctResult != null)
                                    await MdWxPayUtil.PayCallbackProcess_robot(genWXPayResult(r, ctResult.o_no));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MDLogger.LogErrorAsync(typeof(RobotHelper),ex);
            }
        }

        public static WXPayResult genWXPayResult(string openid,string otn)
        {
            WXPayResult result = new WXPayResult();
            result.id = Guid.Empty;
            result.appid = "Robot";
            result.mch_id = "Robot";
            result.result_code = "SUCCESS";
            result.out_trade_no = otn;
            result.openid = openid;
            result.trade_type = "JSAPI";
            result.total_fee = 0;
            result.cash_fee = 0;
            result.transaction_id = "Robot";
            result.time_end = "Robot";
            result.return_code = "SUCCESS";
            result.return_msg = "";
            result.notify_xml = "Robot";
            result.timestamp = CommonHelper.GetUnixTimeNow();
            return result;
        }

        public static List<string> getRobotsOpenids(int number)
        {
            if (_dic.Count < number)
            {
                throw new MDException(typeof(RobotHelper), new Exception($"robot超限！bot总数{_dic.Count},bot需求{number}"));
            }

            if (_dic.Count == number)
                return _dic.Values.ToList();

            List<string> _ret = new List<string>();

            var list = getRandomList(0, _dic.Count-1, number);
            for (int i = 0; i < number; i++)
            {
                _ret.Add(_dic.Values.ToArray()[list[i]]);
            }
            return _ret;
        }

        public static int GetRobotNubmer()
        {
            return _dic.Count;
        }

        public static ConcurrentDictionary<Guid, string> GetAll()
        {
            return _dic;
        }

        private static int getRandomNumber(int from, int to)
        {
            return new Random().Next(from, to);
        }

        private static List<int> getRandomList(int from, int to, int number)
        {
            if (number > 0)
            {
                var list = new List<int>();
                while (list.Count < number)
                {
                    var temp = getRandomNumber(from, to);
                    if (!list.Contains(temp))
                        list.Add(temp);
                }
                return list;
            }
            return null;
        }
    }
}
