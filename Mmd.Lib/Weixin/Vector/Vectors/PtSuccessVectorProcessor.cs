using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Weixin.Robot;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.Redis.RedisObjects.Vector;

namespace MD.Lib.Weixin.Vector.Vectors
{
    /// <summary>
    /// 拼团成功后触发的vector
    /// </summary>
    public class PtSuccessVectorProcessor : IVectorProcessor
    {
        public bool IsVisible(Model.DB.Professional.Vector v)
        {
            return v.visible;
        }

        public string GetVectorType()
        {
            return EVectorType.PTCG.ToString();
        }

        public VectorView Parser(string expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 共同拼团的人之间的请密度都加1
        /// </summary>
        /// <param name="v"></param>
        public async Task Route(Model.DB.Professional.Vector v)
        {
            if (string.IsNullOrEmpty(v?.expression))
                return;
            Guid goid;
            string goidstr = v.expression.Split(new char[] { ':' })[1];
            if (Guid.TryParse(goidstr, out goid))
            {
                //存储vector
                using (BizRepository repo = new BizRepository())
                {
                    await repo.VectorAddAsnyc(v);
                }

                //redis
                VectorRedis r = new VectorRedis();
                r.vid = v.vid.ToString();
                r.value = RedisCommonHelper.ObjectToString(v);//r.GenValue(v);
                await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(r);

                //var rr = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<VectorRedis>(v.vid.ToString());
                //var vv = rr.GetObject();
                //具体逻辑
                using (var repo = new BizRepository())
                {

                    /*
                     * 两个逻辑处理：
                     * 1、团内成员亲密度两两+1；
                     * 2、商家订单配额减少相应的订单数。
                     */
                    var go = await repo.GroupOrderGet(goid);
                    var g = await repo.GroupGetGroupById(go.gid);

                    if (go != null && g != null)
                    {
                        var list = await repo.OrderGetByGoidAsync2(go.goid, new List<int>() { (int)EOrderStatus.已成团未提货, (int)EOrderStatus.已成团未发货 });

                        if (list.Count > 0 && go.status.Equals((int)EGroupOrderStatus.拼团成功))
                        {
                            int mbizCount = list.Count;//除去机器人的订单数

                            //亲密度增加
                            foreach (var o in list)
                            {
                                //去掉机器人，机器人不能有闺蜜圈
                                Guid buyer = o.buyer;
                                if (RobotHelper.IsRobot(buyer))
                                {
                                    mbizCount = mbizCount - 1;
                                    continue;
                                }

                                foreach (var oo in list)
                                {
                                    //人类的闺蜜圈中不能有机器人
                                    Guid oBuyer = oo.buyer;
                                    if (RobotHelper.IsRobot(oBuyer))
                                        continue;

                                    if (oo.oid.Equals(o.oid))
                                        continue;

                                    //在o.buyer的zset中插入一个oo.buyer的记录，并加1
                                    await
                                        new RedisManager2<WeChatRedisConfig>()
                                            .AddScoreEveryKeyAsync<VectorQMRedis, VectorUserQMZsetAttribute>(
                                                o.buyer.ToString(),
                                                oo.buyer.ToString(), 1);
                                }
                            }
                            if (g.group_type == (int)EGroupTypes.普通团)
                            {
                                //只有普通团做订单配额扣减
                                await repo.MerchantOrderConsumeAsnyc(g.mid, EBizType.DD.ToString(), mbizCount, go.goid);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 产生vector
        /// </summary>
        /// <param name="owner">goid</param>
        /// <returns></returns>
        public Model.DB.Professional.Vector GenVector(object obj)
        {
            string owner = obj.ToString();
            Model.DB.Professional.Vector v = new Model.DB.Professional.Vector()
            {
                vid = Guid.NewGuid(),
                type = EVectorType.PTCG.ToString(),
                timestamp = CommonHelper.GetUnixTimeNow(),
                expression = $"goid:{owner}",
                visible = false,
                owner = Guid.Parse(owner)
            };
            return v;
        }
    }
}
