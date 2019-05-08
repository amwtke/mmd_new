using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Antlr.Runtime.Misc;
using MD.Configuration.DB;
using MD.Lib.DB.Redis;
using MD.Lib.DB.Redis.MD;
using MD.Lib.DB.Redis.MD.ForTest;
using MD.Lib.DB.Redis.MD.WxStatistics;
using MD.Lib.DB.Repositorys;
using MD.Lib.ElasticSearch;
using MD.Lib.ElasticSearch.MD;
using MD.Lib.MQ.MD;
using MD.Lib.Util;
using MD.Lib.Util.AddressUtil;
using MD.Lib.Util.HttpUtil;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Lib.Weixin.MmdBiz;
using MD.Lib.Weixin.Services;
using MD.Lib.Weixin.Token;
using MD.Lib.Weixin.Vector;
using MD.Lib.Weixin.Vector.Vectors;
using MD.Model.Configuration.MQ.MMD;
using MD.Model.Configuration.Redis;
using MD.Model.DB;
using MD.Model.DB.Code;
using MD.Model.DB.Professional;
using MD.Model.Index.MD;
using MD.Model.Json;
using MD.Model.Redis.RedisObjects;
using MD.Model.Redis.RedisObjects.Vector;
using MD.Model.Redis.RedisObjects.WeChat.Biz;
using MD.Model.Redis.RedisObjects.WeChat.Biz.Merchant;
using MD.Model.Redis.RedisObjects.WeChat.Component;
using MD.WeChat.Filters;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs.MyExtension;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.CommonAPIs;
using Senparc.Weixin.MP.Entities.Menu;
using StackExchange.Redis;
using MD.Lib.MMBizRule.MerchantRule;
using MD.Lib.Weixin.Robot;
using MD.Model.Index;
using Nest;
using System.Threading;
using MD.Lib.Log;
using MD.Model.Configuration;
using MD.Model.Configuration.ElasticSearch.MD;
using Senparc.Weixin.MP.AdvancedAPIs;
//using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using MD.Model.MQ;
using MD.Lib.Weixin.Message;
using System.Data;
using System.Data.OleDb;
using System.IO;
using MD.Lib.Aliyun.OSS.Biz;
using MD.Model.Configuration.Aliyun;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using ThoughtWorks.QRCode.Codec;
using System.Text;
using MD.Lib.Util.Files;
using MD.Model.MQ.MD;

namespace MD.WeChat.Controllers.ApiTest
{
    [RoutePrefix("testapi")]
    [Route("{action}")]
    [AccessFilter]
    public partial class WebApiController : ApiController
    {
        /// <summary>
        /// 图片压缩(降低质量以减小文件的大小)
        /// </summary>
        /// <param name="srcBitMap">传入的Bitmap对象Bitmap srcBitMap, </param>
        /// <param name="destFile">压缩后的图片保存路径</param>
        /// <param name="level">压缩等级，0到100，0 最差质量，100 最佳</param>
        [HttpGet]
        [Route("img/getimg")]
        public string Compress()
        {

            Image img = Image.FromFile("E:\\222.png");
            //生成随机的背景图
            Bitmap backgroundimg = new Bitmap(img);
            Stream s = new FileStream("E:\\333.jpg", FileMode.Create);
            Compress(backgroundimg, s, 20);
            s.Close();
            return "aa";
        }

        private ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        /// <summary>
        /// 图片压缩(降低质量以减小文件的大小)
        /// </summary>
        /// <param name="srcBitmap">传入的Bitmap对象</param>
        /// <param name="destStream">压缩后的Stream对象</param>
        /// <param name="level">压缩等级，0到100，0 最差质量，100 最佳</param>
        public void Compress(Bitmap srcBitmap, Stream destStream, long level)
        {
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPEG file with 给定的 quality level
            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);
        }
        #region Comment
        //[HttpGet]
        //[Route("comment/addorupdate")]
        //public async Task<string> commentAdd(Guid uid, string content, string to_uid, Guid commentId, string topic_id)
        //{
        //   bool res = await EsCommentManager.AddOrUpdateAsync(uid, content, to_uid, commentId, topic_id);
        //    return res.ToString();
        //}

        [HttpGet]
        [Route("comment/getreplybyuid")]
        public async Task<List<IndexCom_Reply>> GetReplyByUid(Guid uid)
        {
            var tuple = await EsCommentManager.GetReplylistByUidAsync(uid, 3, 1, 10);
            var list = tuple.Item2;
            string uidStr = uid.ToString();
            List<IndexCom_Reply> listRes = new List<IndexCom_Reply>();
            list.ForEach((c) => { listRes.AddRange(c.Com_Replys.Where(r => r.to_uid == uidStr)); });
            return listRes.OrderByDescending(r => r.timestamp).ToList();
        }
        #endregion

        #region logistics_region省市区字典
        [HttpGet]
        [Route("addalllogisticsregion")]
        public async Task<int> addEslogistics_region()
        {
            using (var reop = new AddressRepository())
            {
                int i = 0;
                var list = await reop.GetRegionAllAsync();
                foreach (var logisticsregion in list)
                {
                    var indexlr = EsLogisticsregionManager.GenObject(logisticsregion);
                    if (indexlr != null)
                    {
                        bool b = await EsLogisticsregionManager.AddOrUpdateAsync(indexlr);
                        if (b)
                        {
                            i++;
                        }
                    }
                }
                return i;
            }
        }
        #endregion
        #region past
        [HttpGet]
        public async Task<string> test()
        {
            return "test";
        }

        public async Task<AddressFromJuHe> GetAddressInfo()
        {
            var redisConfig = MD.Configuration.MdConfigurationManager.GetConfig<WeChatRedisConfig>();
            return await AddressHelper.GetDataFromJuHe();
        }

        [System.Web.Http.HttpGet]
        public async Task<bool> SaveAddress()
        {
            var redisConfig = MD.Configuration.MdConfigurationManager.GetConfig<WeChatRedisConfig>();
            var obj = await AddressHelper.GetDataFromJuHe();
            if (obj.reason == "successed")
            {
                foreach (var province in obj.result)
                {
                    if (province.id > 0)
                    {
                        using (AddressRepository repo = new AddressRepository())
                        {
                            await repo.AddProvince(province);
                        }
                        if (province.city.Count > 0)
                        {
                            foreach (var city in province.city)
                            {
                                using (AddressRepository repo = new AddressRepository())
                                {
                                    await repo.AddCity(city, province.id);
                                }

                                if (city.district.Count > 0)
                                {
                                    foreach (var dis in city.district)
                                    {
                                        using (AddressRepository repo = new AddressRepository())
                                        {
                                            await repo.AddDistrict(dis, city.id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            else
                return false;
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("setsession/{key}-{value}")]
        public bool SetSession(string key, string value)
        {
            var context = HttpContext.Current;
            context.Session[key] = value;
            return true;

        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("ss")]
        //http://localhost:2895/api/ss?key=xiao&value=jin
        public bool SetSession2(string key, string value)
        {
            var context = HttpContext.Current;
            context.Session[key] = value;
            return true;

        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("getfromsession/{key}")]
        public string GetFromSession(string key)
        {
            var ses = HttpContext.Current.Session[key];
            return ses.ToString();
        }

        [HttpGet]
        [System.Web.Http.Route("log/{content}")]
        public string TestLog(string content)
        {
            MD.Lib.Log.MDLogger.LogInfoAsync(typeof(WebApiController), content);
            return "ok";
        }
        [HttpGet]
        [Route("test/redis/string/setvalue/{value}")]
        public async Task<bool> TestRedisSet(string value)
        {
            return await new RedisManager2<WeChatRedisConfig>().StringSetAsync<RedisTest, TestStringAttribute>(value);
        }

        [HttpGet]
        [Route("test/redis/string/getvalue")]
        public async Task<String> TestRedisGet()
        {
            string s = await new RedisManager2<WeChatRedisConfig>().StringGetAsync<RedisTest, TestStringAttribute>();
            return s;
        }

        [HttpGet]
        [Route("config/getall")]
        public async Task<List<MDConfigItem>> GetAllConfig()
        {
            using (var configRepo = new ConfigRepository())
            {
                return await configRepo.GetAll();
            }
        }

        [HttpGet]
        [Route("conponent/getprecode")]
        public async Task<string> GetPrecode()
        {
            return await WXComponentHelper.GetComponentPreCodeForceAsync();
        }

        [HttpGet]
        [Route("conponent/geturl")]
        public string GetUrl()
        {
            return WXComponentHelper.GetAuthUrl();
        }

        [HttpGet]
        [Route("md/at")]
        public string getMdAt()
        {
            return WXTokenHelper.GetSiteAccessTokenFromRedis();
        }

        [HttpGet]
        [Route("conponent/getauthat/{appid}")]
        public async Task<string> GetAuthat(string appid)
        {
            return await WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppIdAsync(appid);
        }

        [HttpGet]
        [Route("conponent/refresh/{appid}/{token}")]
        public async Task<AuthorizerInfoRedis> Refresh(string appid, string token)
        {
            return await WXComponentHelper.RefreshAuthorizerAccessTokenAsnyc(appid, token);
        }

        [HttpGet]
        [Route("conponent/getmpinfo/{appid}")]
        public async Task<AuthorizerInfoRedis> GetMpInfo(string appid)
        {
            return await WXComponentHelper.GetAuthorizerInfoAsync(appid);
        }

        [HttpGet]
        [Route("conponent/getuserat/{appid}")]
        public string GetUserAT(string appid)
        {
            return WXComponentUserHelper.GetUserAccessToken(appid);
        }

        [HttpGet]
        [Route("test/exception")]
        public string TestMdException()
        {
            throw new MDException(typeof(WebApiController), "测试MD的Exception！");
        }

        [HttpGet]
        [Route("component/at")]
        public string GetComponentAt()
        {
            return WXComponentHelper.GetComponentAccessToken();
        }

        [HttpGet]
        [Route("component/att")]
        public async Task<string> GetComponentAtt()
        {
            return await WXComponentHelper.GetComponentAccessTokenAsync();
        }

        [HttpGet]
        [Route("test/redis/hash/delentry")]
        public async Task<bool> DeleteHashEntry(string k, string e)
        {
            return await new RedisManager2<WeChatRedisConfig>().DeleteHashItemAsync<UserInfoRedis>(k, e);
        }

        [HttpGet]
        [Route("test/redis/hash/delete")]
        public async Task<int> DeleteHashEntry(string k)
        {
            return await new RedisManager2<WeChatRedisConfig>().DeleteHashObjectAsync<UserInfoRedis>(k);
        }


        #endregion

        #region test db es
        #region es

        [HttpGet]
        [Route("test/es/getorderbyid")]
        public async Task<IndexOrder> getEsorderbyid(Guid oid)
        {
            return await EsOrderManager.GetByIdAsync(oid);
        }

        [HttpGet]
        [Route("test/es/getorderbyotn")]
        public async Task<IndexOrder> getorderbyotn(string otn)
        {
            return await EsOrderManager.GetByOtnAsync(otn);
        }

        [HttpGet]
        [Route("test/es/getgobyid")]
        public async Task<IndexGroupOrder> getEsGoById(Guid goid)
        {
            return await EsGroupOrderManager.GetByIdAsync(goid);
        }

        [HttpGet]
        [Route("test/es/getwop")]
        public async Task<IndexWriteOffPoint> getwop(Guid id)
        {
            return await EsWriteOffPointManager.GetByIdAsync(id);
        }

        [HttpGet]
        [Route("test/es/getproductbyid")]
        public async Task<IndexProduct> getproductbyid(Guid pid)
        {
            return await EsProductManager.GetByPidAsync(pid);
        }

        [HttpGet]
        [Route("test/es/syncallpropuct")]
        public async Task<bool> syncallpropuct()
        {
            using (var repo = new BizRepository())
            {
                foreach (var g in repo.Context.Products.AsEnumerable())
                {
                    var index = await EsProductManager.GenObject(g.pid);
                    if (index != null)
                    {
                        await EsProductManager.AddOrUpdateAsync(index);
                    }
                }
                return true;
            }
        }

        [HttpGet]
        [Route("test/es/syncallgo")]
        public async Task<bool> syncallgo()
        {
            using (var repo = new BizRepository())
            {
                foreach (var go in repo.Context.GroupOrders.AsEnumerable())
                {
                    var index = await EsGroupOrderManager.GenObjectAsync(go);
                    if (index != null)
                    {
                        await EsGroupOrderManager.AddOrUpdateAsync(index);
                    }
                }
                return true;
            }
        }

        [HttpGet]
        [Route("test/es/syncallorder")]
        public async Task<bool> syncallorder()
        {
            using (var repo = new BizRepository())
            {
                foreach (var or in repo.Context.Orders.AsEnumerable())
                {
                    var index = await EsOrderManager.GenObjectAsync(or);
                    {
                        await EsOrderManager.AddOrUpdateAsync(index);
                    }
                }
                return true;
            }
        }

        [HttpGet]
        [Route("test/es/syncalluser")]
        public async Task<bool> syncalluser()
        {
            using (var repo = new BizRepository())
            {
                foreach (var u in repo.Context.Users.AsEnumerable())
                {
                    var index = EsUserManager.GenObject(u);
                    {
                        await EsUserManager.AddOrUpdateAsync(index);
                    }
                }
                return true;
            }
        }

        [HttpGet]
        [Route("test/es/getorders")]
        public async Task<Tuple<int, List<IndexOrder>>> esGetorders(Guid mid, Guid uid)
        {
            var orders =
                await
                    EsOrderManager.SearchAsnyc2("", mid, uid, null, null, 1,
                        10);
            return orders;
        }
        #endregion 
        #endregion

        #region order
        [HttpGet]
        [Route("test/db/getorderrefund")]
        public async Task<List<MD.Model.DB.Order>> getRefundOrders()
        {
            using (var repo = new BizRepository())
            {
                var ret = await repo.OrderGetNeedRefundAsync();
                return ret;
            }
        }
        [HttpGet]
        [Route("order/verifygid")]
        public async Task<bool> verifygid()
        {
            using (var repo = new BizRepository())
            {
                var list = await repo.Context.Orders.Where(oo => !oo.oid.Equals(Guid.Empty)).ToListAsync();
                foreach (var o in list)
                {
                    if (!o.goid.Equals(Guid.Empty))
                    {
                        var go = await repo.GroupOrderGet(o.goid);
                        if (go != null)
                        {
                            var group = await repo.GroupGetGroupById(go.gid);
                            if (group != null)
                            {
                                o.gid = group.gid;
                                await repo.OrderUpDateAsync(o);
                            }
                        }
                    }
                }
                return true;
            }
        }
        [HttpGet]
        [Route("es/order/get")]
        public async Task<IndexOrder> orderesGet(Guid oid)
        {
            return await EsOrderManager.GetByIdAsync(oid);
        }

        /// <summary>
        /// 把后台手动退款的订单该为退款成功（已退款）
        /// </summary>
        /// <param name="o_no"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("order/udateorderstatus")]
        public async Task<bool> UpdateOrderStatus(Guid oid, int status)
        {
            // EOrderStatus.已退款 = 1;
            // EOrderStatus.退款失败 = 8;
            using (var repo = new BizRepository())
            {
                var indexorder = await EsOrderManager.GetByIdAsync(oid);
                var dborder = await repo.OrderGetByOid(oid);
                if (indexorder == null || dborder == null)
                    return false;
                bool flag = await repo.UpdateOrderStatusByOid(oid, status);//更新数据库和ES
                return flag;
            }
        }
        [HttpGet]
        [Route("order/udategrouporderstatus")]
        public async Task<bool> UpdateGroupOrderStatus(Guid goid)
        {
            using (var repo = new BizRepository())
            {
                var go = await EsGroupOrderManager.GetByIdAsync(goid);
                var dbgo = await repo.GroupOrderGet(goid);
                if (go == null || dbgo == null)
                    return false;
                dbgo.status = (int)EGroupOrderStatus.拼团成功;
                //dbgo.user_left = 0;
                bool flag = await repo.GroupOrderUpdateAsync(dbgo);//更新数据库和ES
                return flag;
            }
        }
        [HttpGet]
        [Route("order/GroupOrderVerifyOrderStatusAfterGoSuccess")]
        public async Task<bool> GroupOrderVerifyOrderStatusAfterGoSuccess()
        {
            using (var repo = new BizRepository())
            {
                var go = await repo.GroupOrderGet(Guid.Parse("aa2bf036-8be5-4253-a3f7-a0aad5cec61a"));
                var group = await repo.GroupGetGroupById(go.gid);
                repo.GroupOrderVerifyOrderStatusAfterGoSuccess(go, group);
                return true;
            }
        }

        [HttpGet]
        [Route("order/GetOrderListDeleted")]
        public async Task<List<string>> GetOrderListDel()
        {
            double timeStart = CommonHelper.ToUnixTime(Convert.ToDateTime("2016-11-25"));
            double timeEnd = CommonHelper.ToUnixTime(Convert.ToDateTime("2016-12-04"));
            var tuple = await EsOrderManager.SearchAsnyc("2016-11-25 - 2016-12-04", "", Guid.Parse("c5bef86f-fec9-4ce9-a0dc-a3e7ee46f3ca"), null, null, 1, 10000);
            List<IndexOrder> list = tuple.Item2;
            List<string> listRes = new List<string>();
            var tupleBiz = EsBizLogStatistics.SearchBizByModelNameView("支付回调", Guid.Empty, Guid.Empty, timeStart, timeEnd, 1, 10000);
            var bizList = tupleBiz.Item2;
            using (var repo = new BizRepository())
            {
                foreach (var item in bizList)
                {
                    string mes = item.Message;
                    string[] m = mes.Split(',');
                    string mchid = m[1].Replace("mch_id:", "");
                    if (mchid == "1226155602")
                    {
                        string orderNum = m[8].Replace("out_trade_no:", "");
                        bool isExist = list.Exists(o => o.o_no == orderNum);
                        if (!isExist)
                            listRes.Add(mes);
                    }
                }
            }

            return listRes;
        }

        #endregion

        [HttpGet]
        [Route("redis/mer/sync")]
        public async Task<MerchantRedis> merSync(Guid mid)
        {
            return await RedisMerchantOp.UpdateFromDbAsync(mid);
        }


        [HttpGet]
        [Route("redis/mer/get")]
        public async Task<MerchantRedis> merGet(Guid mid)
        {
            return await RedisMerchantOp.GetByMidAsync(mid);
        }

        #region wx
        [HttpGet]
        [Route("test/url/entrance")]
        public string getEntrance(string appid)
        {
            return MdWxSettingUpHelper.GenEntranceUrl(appid);
        }

        [HttpGet]
        [Route("test/wx/getmenu")]
        public async Task<Dictionary<string, object>> GetMenu(string appid)
        {
            //string appid = "wxa2f78f4dfc3b8ab6";
            string at = await WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppIdAsync(appid);
            var obj = CommonApi.GetMenu(at);
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("Data", obj.menu);
            return dic;
        }

        [HttpGet]
        [Route("test/wx/setmenu")]
        public async Task<WxJsonResult> setManu()
        {
            string appid = "wxa2f78f4dfc3b8ab6";
            string at = await WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppIdAsync(appid);
            string url = MdWxSettingUpHelper.GenEntranceUrl(appid);
            ButtonGroup bg = new ButtonGroup();

            //单击
            bg.button.Add(new SingleViewButton()
            {
                name = "MM拼团",
                url = url,
                type = "view",
            });
            //List<object> button = new List<object>();
            //button.Add(new
            //{
            //    type = "view",
            //    name = "MM拼团",
            //    url = url
            //});
            return await CommonApi.CreateMenuAsync(at, bg);
        }

        #endregion


        #region tempmsg
        [HttpGet]
        [Route("test/testLottery")]
        public List<string> testLottery()
        {
            WxServiceHelper.Md_GroupLottery_Process();
            List<string> listRes = new List<string>();
            listRes.Add("ss");
            return listRes;
            //Guid gid = Guid.Parse("632cfc11-601b-4d85-b6a8-db6093da49a7");
            //using (var repo = new BizRepository())
            //{
            //    MerchantRedis mer = RedisMerchantOp.GetByMid(Guid.Parse("c5bef86f-fec9-4ce9-a0dc-a3e7ee46f3ca"));
            //    List<GroupOrder> listGroupFail = repo.GroupOrderGetByGidAsync(gid, EGroupOrderStatus.拼团失败).Result;
            //    List<string> list = new List<string>();
            //    foreach (var go in listGroupFail)
            //    {
            //        //拼团失败的订单发送到退款队列
            //        //更新团订单状态到——失败！
            //        //go.status = (int)EGroupOrderStatus.拼团失败;
            //        var orderList = repo.OrderGetByGoid2(go.goid, EOrderStatus.已支付);
            //        if (orderList == null || orderList.Count <= 0) continue;
            //        foreach (var o in orderList)
            //        {


            //            list.Add(o.o_no);
            //            //发送到refund队列处理
            //            MqRefundManager.SendMessageAsync(new MqWxRefundObject()
            //            {
            //                appid = mer.wx_appid,
            //                out_trade_no = o.o_no
            //            });
            //        }
            //    }
            //    return list;
            //}
        }


        [HttpGet]
        [Route("temp/sendeMessage")]
        public bool sendeMessage()
        {
            var obj = MqWxTempMsgManager.GenNewsObject("wx6c2806b9adbc00b7", "otdlkt4GncPO14I8PLAXa4DnZQF8", "kt,11ce11c6-0ed2-48eb-8baa-f648384461c6");
            //var obj = MqWxTempMsgManager.GenNewsObject("wxa2f78f4dfc3b8ab6", "otuH9sjtu4yCQZ43oFua3qCVg7l4", "kt,ab2b3d09-c73e-4ac1-b575-cef5d998b9c9");
            return MqWxTempMsgManager.SendMessage(obj);
        }

        [HttpGet]
        [Route("temp/sucess")]
        public async Task<bool> tempsuccess(Guid oid)
        {
            //using (var repo = new BizRepository())
            //{
            //    var order = await repo.OrderGetByOid(oid);
            //    var obj = await MqWxTempMsgManager.GenFromPtSucess(order);
            //    return await MqWxTempMsgManager.SendMessageAsync(obj);
            //}
            using (var acti = new ActivityRepository())
            {
                var order = await acti.GetOrderByOidAsync(oid);
                var obj = await MqWxTempMsgManager.GenFromLadderGroupPtSucess(order);
                return await MqWxTempMsgManager.SendMessageAsync(obj);
            }
        }

        [HttpGet]
        [Route("temp/fail")]
        public async Task<bool> tempfail(Guid oid)
        {
            using (var repo = new BizRepository())
            {
                var order = await repo.OrderGetByOid(oid);
                var obj = await MqWxTempMsgManager.GenFromPtFail(order);
                return await MqWxTempMsgManager.SendMessageAsync(obj);
            }
        }

        [HttpGet]
        [Route("temp/paysuccess")]
        public async Task<Dictionary<string, object>> tempPaySuccess(string out_trade_no, string appid, string openid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                using (var repo = new BizRepository())
                {
                    var order = repo.OrderGetByOutTradeNo(out_trade_no);
                    var obj = MqWxTempMsgManager.GenFromPaySuccess(order, appid, openid);
                    bool flag = await MqWxTempMsgManager.SendMessageAsync(obj);
                    dic.Add("Result", flag);
                }
            }
            catch (Exception ex)
            {
                dic.Add("Result", false);
                dic.Add("Message", ex.Message);
            }
            return dic;
        }

        [HttpGet]
        [Route("temp/groupremind")]
        public async Task<Dictionary<string, object>> tempGroupRemind(string appid, string openid, Guid goid, string productName, int timeEndHours, int userLeft)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                using (var repo = new BizRepository())
                {
                    var obj = MqWxTempMsgManager.GenGroupRemind(appid, openid, goid, productName, timeEndHours, userLeft);
                    bool flag = await MqWxTempMsgManager.SendMessageAsync(obj);
                    dic.Add("Result", flag);
                }
            }
            catch (Exception ex)
            {
                dic.Add("Result", false);
                dic.Add("Message", ex.Message);
            }
            return dic;
        }

        [HttpGet]
        [Route("temp/grouporderlist")]
        public string tempGroupOrderList(int timeEndHours, int timeInterval)
        {
            //Dictionary<string, object> dic = new Dictionary<string, object>();
            //var list = CommonHelper.GetRandomList(0, timeEndHours, timeInterval);
            //return Newtonsoft.Json.JsonConvert.SerializeObject(list);

            //WxServiceHelper.Md_GroupRemindProcess(timeEndHours, timeInterval);
            //return "";
            try
            {
                using (var repo = new BizRepository())
                {
                    List<GroupOrder> list = repo.GroupOrdersGetByTime(timeEndHours, timeInterval);
                    return Newtonsoft.Json.JsonConvert.SerializeObject(list);
                }
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
        }

        [HttpGet]
        [Route("temp/addtemp")]
        public async Task<AddTemplateMessageResult> addtemp(string appid, string tempInShort)
        {
            string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
            return await MyTemplateApi.AddemplateMessageAsync(at, tempInShort);
        }


        [HttpGet]
        [Route("temp/lottery")]
        public string getLotteryLucky()
        {
            List<Model.DB.Order> listAll = new List<Model.DB.Order>();
            List<Model.DB.Order> listLucky = new List<Model.DB.Order>();
            using (var repo = new BizRepository())
            {
                var attrepo = new AttRepository();
                //1.获取已经结束且未处理的Group集合,取中奖人数
                List<Group> listGroup = repo.luckyGroupGetAsync().Result;
                Random ran = new Random();
                if (listGroup != null && listGroup.Count > 0)
                {
                    for (int i = 0; i < listGroup.Count; i++)
                    {
                        Guid gid = listGroup[i].gid;
                        Guid pid = listGroup[i].pid;
                        string groupName = listGroup[i].title;
                        var lucky_count = AttHelper.GetValue(gid, EAttTables.Group.ToString(), EGroupAtt.lucky_count.ToString());
                        //该团活动的中奖人数
                        int lotteryUserCount = Convert.ToInt32(lucky_count);
                        //1.1获取属于这个抽奖团且组团成功的GroupOrder集合
                        List<Guid> listGuid = repo.GroupOrderGetByGidRetunGuidAsync(gid, EGroupOrderStatus.拼团成功).Result;
                        if (listGuid.Count <= 0)
                        {
                            attrepo.lucky_statusAddOrUpdateAsync(gid, (int)EGroupLuckyStatus.已开奖);
                            continue;
                        }
                        List<Model.DB.Order> listOrder = repo.OrderGetByGoidAsync2(listGuid, EOrderStatus.已成团未提货).Result;
                        //参团的总人数
                        int joinUserCount = listOrder.Count;
                        bool isAllLucky = false;
                        List<int> listLuckyNumber = new List<int>();
                        if (lotteryUserCount > joinUserCount)
                        {
                            isAllLucky = true;
                        }
                        else
                        {
                            int count = 0;
                            while (count < lotteryUserCount)
                            {
                                int r = ran.Next(joinUserCount);
                                if (!listLuckyNumber.Contains(r))
                                {
                                    listLuckyNumber.Add(r);
                                    count++;
                                }
                            }
                        }
                        for (int j = 0; j < joinUserCount; j++)
                        {
                            Model.DB.Order o = listOrder[j];
                            MerchantRedis mer = RedisMerchantOp.GetByMid(o.mid);
                            string openid = EsUserManager.GetById(o.buyer)?.openid;
                            string productName = EsProductManager.GetByPid(pid)?.name;
                            bool isLucky = false;
                            if (isAllLucky || listLuckyNumber.Contains(j))    //中奖订单修改状态为中奖，发到模板消息队列
                            {
                                listAll.Add(listOrder[j]);
                                listLucky.Add(listOrder[j]);
                                isLucky = true;
                                attrepo.order_luckyStatusAddOrUpdateAsync(o.oid, (int)EOrderLuckyStatus.已中奖);
                            }
                            else                                //未中奖订单修改状态为未中奖，发到退款订单集合
                            {
                                listAll.Add(listOrder[j]);
                                attrepo.order_luckyStatusAddOrUpdateAsync(o.oid, (int)EOrderLuckyStatus.未中奖);
                                //退款
                                //MqRefundManager.SendMessageAsync(new MqWxRefundObject() { appid = mer.wx_appid, out_trade_no = o.o_no });
                            }

                            var obj = MqWxTempMsgManager.GenLotteryResult(mer?.wx_appid, openid, o.oid, groupName, productName, isLucky);
                            MqWxTempMsgManager.SendMessageAsync(obj);
                        }
                        //3修改团活动为已开奖
                        attrepo.lucky_statusAddOrUpdateAsync(gid, (int)EGroupLuckyStatus.已开奖);
                    }
                }
                else
                {
                    return "no data";
                }
            }
            return "listAll:" + Newtonsoft.Json.JsonConvert.SerializeObject(listAll) + ",listLucky:" + Newtonsoft.Json.JsonConvert.SerializeObject(listLucky);
        }

        [HttpGet]
        [Route("temp/lotteryres")]
        public async Task<Dictionary<string, object>> getLotteryRes(string appid, string openid, Guid goid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                using (var repo = new BizRepository())
                {
                    var obj = MqWxTempMsgManager.GenLotteryResult(appid, openid, goid, "活动名称", "商品名称", true);
                    bool flag = await MqWxTempMsgManager.SendMessageAsync(obj);
                    dic.Add("Result", flag);
                }
            }
            catch (Exception ex)
            {
                dic.Add("Result", false);
                dic.Add("Message", ex.Message);
            }
            return dic;
        }

        [HttpGet]
        [Route("temp/GetGroupList")]
        public async Task<List<Group>> GetGroupList()
        {
            using (var repo = new BizRepository())
            {
                List<Group> list = repo.luckyGroupGet();
                foreach (var item in list)
                {
                    item.lucky_endTime = CommonHelper.FromUnixTime(Convert.ToDouble(item.lucky_endTime)).ToString("yyyy年M月d日 hh:mm");
                }
                return list;
            }

        }

        [HttpGet]
        [Route("temp/getopenidlist")]
        public async Task<Dictionary<string, object>> GetOpenIdList(string appid, string nextopenid)
        {

            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                //string appid = "wxa2f78f4dfc3b8ab6";
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                string urlFormat = $"https://api.weixin.qq.com/cgi-bin/user/get?access_token={at}&next_openid={nextopenid}";
                string res = await RequestUtility.HttpGetAsync(urlFormat, null, null, 3000);
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Senparc.Weixin.MP.AdvancedAPIs.User.OpenIdResultJson>(res);
                if (obj.errcode == 0 && obj.data != null)
                {
                    List<string> listOpenid = obj.data.openid;
                    foreach (var openid in listOpenid)
                    {
                        //todo:记录关注用户的openid
                    }
                }
                dic.Add("Data", obj);
            }
            catch (Exception ex)
            {
                dic.Add("Result", false);
                dic.Add("Message", ex.Message);
            }
            return dic;
        }

        [HttpGet]
        [Route("test/redis/tmpid/add")]
        public async Task<Dictionary<string, object>> AddTmpId(string openid, string goid, string type)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                bool flag = RedisUserOp.SaveTmpId(openid, goid, type);
                dic.Add("res", flag);
            }
            catch (Exception ex)
            {
                dic.Add("message", ex);

            }
            return dic;
        }

        [HttpGet]
        [Route("test/redis/tmpid/get")]
        public async Task<Dictionary<string, object>> GetTmpId(string openid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                string value = RedisUserOp.GetTmpId(openid);
                dic.Add("res", value);
            }
            catch (Exception ex)
            {
                dic.Add("message", ex);

            }
            return dic;
        }

        [HttpGet]
        [Route("test/redis/hash/add")]
        public async Task<Dictionary<string, object>> AddUserSub(string openid, string appid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                UserSubMapRedis user = new UserSubMapRedis() { OpenId = openid, Appid = appid };
                bool flag = await RedisUserOp.SaveOpenidAsync(user);
                //using (BizRepository rep = new BizRepository())
                //{
                //    Subscribe_User sub_user = new Subscribe_User();
                //    sub_user.openid = openid;
                //    sub_user.subscribe = 1;
                //    sub_user.subscribe_time = Convert.ToInt64(CommonHelper.GetUnixTimeNow());
                //    bool flagAdd = await rep.AddSub_userAsync(sub_user);
                //}
                dic.Add("res", flag);
            }
            catch (Exception ex)
            {
                dic.Add("message", ex);

            }
            return dic;
        }

        [HttpGet]
        [Route("test/redis/hash/batchadd")]
        public async Task<Dictionary<string, object>> BatchAddUserSub(string appid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                System.Diagnostics.Stopwatch ws = new System.Diagnostics.Stopwatch();
                ws.Start();
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                await batchAdd(at, "", appid);
                ws.Stop();
                dic.Add("res", true);
                dic.Add("time", ws.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                dic.Add("message", ex);

            }
            return dic;
        }


        [HttpGet]
        [Route("test/redis/hash/autobatchadd")]
        public async Task<Dictionary<string, object>> AutoBatchAddUserSub()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                using (BizRepository repo = new BizRepository())
                {
                    var tup = await repo.MerchantSearchByNameAsync("", 1, 200);
                    List<Merchant> list = tup.Item2;
                    foreach (var m in list)
                    {
                        string appid = m.wx_appid;
                        string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                        await batchAdd(at, "", appid);
                    }
                }
                //System.Diagnostics.Stopwatch ws = new System.Diagnostics.Stopwatch();
                //ws.Start();

                //ws.Stop();
                dic.Add("res", true);
                //dic.Add("time", ws.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                dic.Add("message", ex);
                System.IO.File.AppendAllText(@"d:\log.txt", ex.ToString());
            }
            return dic;
        }

        private async Task<bool> batchAdd(string at, string nextopenid, string appid)
        {
            string urlFormat = $"https://api.weixin.qq.com/cgi-bin/user/get?access_token={at}&next_openid={nextopenid}";
            string res = await RequestUtility.HttpGetAsync(urlFormat, null, null, 3000);
            var obj = await Task.Factory.StartNew(() => Newtonsoft.Json.JsonConvert.DeserializeObject<Senparc.Weixin.MP.AdvancedAPIs.User.OpenIdResultJson>(res));
            //using (BizRepository rep = new BizRepository())
            //{
            if (obj.errcode == 0 && obj.data != null)
            {
                List<HashEntry> list = obj.data.openid.Select(o => new HashEntry(o + "_Appid", appid)).ToList();
                //批量导入    
                await RedisUserOp.SaveOpenidListAsync(list);

                //foreach (var openid in listOpenid)
                //{
                //    //todo:记录关注用户的openid
                //    UserSubMapRedis user = new UserSubMapRedis() { OpenId = openid, Appid = appid };
                //    bool flag = await RedisUserOp.SaveOpenidAsnyc(user);

                //    //Subscribe_User sub_user = new Subscribe_User();
                //    //sub_user.openid = openid;
                //    //sub_user.subscribe = 1;
                //    //sub_user.subscribe_time = Convert.ToInt64(CommonHelper.GetUnixTimeNow());
                //    //bool flagAdd = await rep.AddSub_userAsync(sub_user);
                //    //list.Add(sub_user);
                //}
                await batchAdd(at, obj.next_openid, appid);
            }
            //}
            return true;
        }

        [HttpGet]
        [Route("test/redis/hash/isexist")]
        public async Task<Dictionary<string, object>> IsExistOpenid(string openid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            bool f = await RedisUserOp.IsExistOpenidAsync(openid);
            dic.Add("res", f);
            return dic;
        }

        [HttpGet]
        [Route("test/custom/sendnews")]
        public async Task<Dictionary<string, object>> SendNews(string openid, string id)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                string appid = "wxa2f78f4dfc3b8ab6";
                string token = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);

                var obj = MqWxTempMsgManager.GenNewsObject(appid, openid, id);
                //var obj = MqWxTempMsgManager.GenNewsObject(appid, openid, "kt,1ccd2184-3367-40ea-b232-d65e41a324ba");
                MqWxTempMsgManager.SendMessage(obj);
                //List<Article> list = new List<Article>();
                //Article art = new Article();
                //art.Title = "测试图文消息";
                //art.Description = "图文消息描述";
                //art.Url = "http://wxa2f78f4dfc3b8ab6.wx.mmpintuan.com/mmpt/app/#/app/cosmetics/cosmetics/page/054dc31f-ec5d-4e80-b49b-fac41df53224&otuH9sjtu4yCQZ43oFua3qCVg7l4";
                ////art.PicUrl = "http://mmbiz.qpic.cn/mmbiz_jpg/Ylj4GjGwyrAGic72xeSYqGNK1mY7rHbMQ9SJCzLNEw9QiaWfwRLnMyuGH383lA44wL4dowFpxpzW1nlYicB5esiaQQ/0";
                //art.PicUrl = "http://picscdn.mmpintuan.com/g/4942e6a47a3c4f5a9c2ff391d7e65cac/a/1474872751.34855@359h_640w_2e";
                //list.Add(art);
                //var res = CustomApi.SendNews(token, openid, list);
                //string title = "测试图文消息";
                //string description = "图文消息描述";
                //string url = "http://wxa2f78f4dfc3b8ab6.wx.mmpintuan.com/mmpt/app/#/app/cosmetics/cosmetics/page/054dc31f-ec5d-4e80-b49b-fac41df53224&otuH9sjtu4yCQZ43oFua3qCVg7l4";
                //string picurl = "http://picscdn.mmpintuan.com/g/4942e6a47a3c4f5a9c2ff391d7e65cac/a/1474872751.34855@359h_640w_2e";
                //var res = await MyTemplateApi.SendNewsAsync(token, openid, new { title, description, url, picurl });
                dic.Add("res", true);
            }
            catch (Exception ex)
            {
                dic.Add("mes", ex);
            }
            return dic;
        }

        [HttpGet]
        [Route("test/CheckUserSub")]
        public async Task<Dictionary<string, object>> CheckUserSub(string appid, string openid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                string at = WXComponentHelper.GetAuthorizerAccessTokenByAuthorizerAppId(appid);
                UserInfoJson user = await UserApi.InfoAsync(at, openid);
                bool IsUserSub = user?.subscribe == 1;
                dic.Add("User", user);
                dic.Add("res", IsUserSub);
                return dic;
            }
            catch (Exception ex)
            {
                dic.Add("res", false);
                dic.Add("message", ex);
                return dic;
            }

        }

        [HttpGet]
        [Route("test/sendlocation")]
        public async Task<bool> sendLocation()
        {
            string gid = "ab2b3d09-c73e-4ac1-b575-cef5d998b9c9";
            string openid = "otuH9sjtu4yCQZ43oFua3qCVg7l4";
            Guid uid = Guid.Parse("47a31631-0f51-4b8a-ab07-afb4baf0b7db");
            Guid mid = Guid.Parse("37d87b84-7a58-4616-a6b7-1c12a5817d0b");
            // var biz = new BizMQ(ELogBizModuleType.PayView.ToString(), openid, uid, mid.ToString(), gid, null, null, new Coordinate { Lon = 114.326276, Lat = 30.52647 });
            //var index = LogESManager.CopyFromBizMQ(biz);
            // BizIndex ret = new BizIndex();
            // foreach (System.Reflection.PropertyInfo pi in ret.GetType().GetProperties())
            // {
            //     object value = index.GetType().GetProperty(pi.Name).GetValue(index);
            //     if (value != null)
            //         pi.SetValue(ret, value);
            // }
            EsBizLogStatistics.AddPayBizViewLog(gid, openid, uid, mid, 114.326276, 30.52647);
            return true;
        }

        [HttpGet]
        [Route("test/getdataall")]
        public async Task<List<object>> getData()
        {
            double to = CommonHelper.GetUnixTimeNow();
            double f = CommonHelper.ToUnixTime(Convert.ToDateTime("2016-10-20"));
            var tuple = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.PayView, Guid.Empty, Guid.Empty, null, null, 1, 10000);
            List<BizIndex> list = tuple.Item2;
            //List<geometry> listGeometry = new List<geometry>();
            List<object> listObj = new List<object>();
            foreach (var item in list)
            {
                //if (item.Location != null && item.Location.Lon != 0)
                //{
                //    double[] coor = { item.Location.Lon, item.Location.Lat };
                //    geometry obj = new geometry { type = "Point", coordinates = coor };
                //    //listGeometry.Add(obj);
                //    listObj.Add(new { geometry = obj });
                //}
            }
            return listObj;
        }
        public class geometry
        {
            public string type { get; set; }
            public double[] coordinates { get; set; }
        }

        [HttpGet]
        [Route("test/testtemp")]
        public async Task<string> testTemp()
        {
            string res = await TemplateMsgHelper.GetTempId("wx993409c7e264fd3c", "OPENTM206854010");
            return res;
        }

        [HttpGet]
        [Route("test/getfee")]
        public async Task<int> GetFee(Guid ltid, string code)
        {
            int res = await EsLogisticsTemplateManager.GetFeeByCode(ltid, code);
            return res;
        }

        [HttpGet]
        [Route("test/getusersnear")]
        public async Task<object> GetUsersNearby(Guid mid, float lat, float lon)
        {
            string gid = "ab2b3d09-c73e-4ac1-b575-cef5d998b9c9";
            string openid = "otuH9sjtu4yCQZ43oFua3qCVg7l4";
            Guid uid = Guid.Parse("47a31631-0f51-4b8a-ab07-afb4baf0b7db");
            //Guid mid = Guid.Parse("37d87b84-7a58-4616-a6b7-1c12a5817d0b");
            //float lat = 30.52647f;
            //float lon = 114.326276f;
            //EsBizLogStatistics.AddPayBizViewLog(gid, openid, uid, mid, 114.326276, 30.52647);
            //BizIndex index = new BizIndex();
            //index.Id = Guid.NewGuid().ToString();
            //index.Location = "";
            //LogESManager.AddOrUpdateBiz(index);
            var tuple = await EsBizLogStatistics.GetUsersNear(mid, lat, lon);
            var list = tuple.Item2;
            Dictionary<string, Coordinate> dic = new Dictionary<string, Coordinate>();
            List<object> listRes = new List<object>();
            foreach (var item in list)
            {
                dic[item.UserUuid] = item.Location;
            }
            foreach (var item in dic.Keys)
            {
                var user = await RedisUserOp.GetByUidAsnyc(Guid.Parse(item));
                if (user != null)
                {
                    Coordinate cor = dic[item];
                    UserNearby un = new UserNearby();
                    un.uid = item;
                    un.name = user.NickName;
                    un.distance = GeoHelper.GetDistance(cor.Lat, cor.Lon, lat, lon);
                    listRes.Add(un);
                }
            }
            return new { data = listRes };
        }
        public class UserNearby
        {
            public string uid { get; set; }
            public string name { get; set; }
            public double distance { get; set; }
        }
        private double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double EARTH_RADIUS = 6378137;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return Math.Round(result, 2);
        }

        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private double Rad(double d)
        {
            return d * Math.PI / 180d;
        }

        [HttpGet]
        [Route("test/updateflag")]
        public async Task<object> UpdateFlag()
        {
            var tuple = await EsCommunityManager.GetListAsync(Guid.Empty, Guid.Empty, (int)ECommunityTopicType.MMSQ, 1, 1000, (int)ECommunityStatus.已发布, "");
            var list = tuple.Item2;
            foreach (var item in list)
            {
                item.flag = (int)ECommunityFlag.嗮好货;
                await EsCommunityManager.AddOrUpdateAsync(item);
            }
            return new { data = "success" };
        }

        [HttpGet]
        [Route("test/TestCount")]
        public async Task<object> TestCount()
        {
            Dictionary<int, bool> dic = new Dictionary<int, bool>();
            for (int i = 1; i <= 2015; i++)
            {
                dic.Add(i, false);
            }
            for (int m = 1; m <= 2015; m++)
            {
                for (int n = 1; n <= 2015; n++)
                {
                    if (n % m == 0)
                        dic[n] = !dic[n];
                }
            }
            var dicRes = dic.Where(t => t.Value == true).ToList();
            return new { data = dicRes };
        }
        #endregion

        #region wx login

        [HttpGet]
        [Route("wxlogincount/get")]
        public async Task<int> getwxlogincount()
        {
            return await WxStatisticsOp.GetTotalLoginCount();
        }

        [HttpGet]
        [Route("wxlogincount/add")]
        public async Task<bool> addwxlogincount()
        {
            return await WxStatisticsOp.AddTotalLoginCount();
        }

        [HttpGet]
        [Route("wxexlogincount/get")]
        public async Task<int> getexwxlogincount()
        {
            return await WxStatisticsOp.GetExceptionCountAsync();
        }

        [HttpGet]
        [Route("wxexlogincount/add")]
        public async Task<bool> addexwxlogincount()
        {
            return await WxStatisticsOp.AddExceptionCount();
        }

        #endregion

        #region verify or get merchant statistics

        [HttpGet]
        [Route("mer/s/get2")]
        public async Task<MerchantStatisticsRedis> getMerStatistics2(string appid)
        {
            var mer = await RedisMerchantOp.GetByAppidAsync(appid);
            if (!string.IsNullOrEmpty(mer?.wx_appid) && !mer.mid.Equals(Guid.Empty))
            {
                if (await merVerify(appid))
                {
                    var redisStatistics = await RedisMerchantStatisticsOp.GetObjectAsync(mer.mid);
                    return redisStatistics;
                }
            }
            return null;
        }

        [HttpGet]
        [Route("mer/s/get")]
        public async Task<MerchantStatisticsRedis> getMerStatistics(string appid)
        {
            var mer = await RedisMerchantOp.GetByAppidAsync(appid);
            if (!string.IsNullOrEmpty(mer?.wx_appid) && !mer.mid.Equals(Guid.Empty))
            {
                var redisStatistics = await RedisMerchantStatisticsOp.GetObjectAsync(mer.mid);
                return redisStatistics;
            }
            return null;
        }

        [HttpGet]
        [Route("mer/s/verify")]
        public async Task<bool> merVerify(string appid)
        {
            var mer = await RedisMerchantOp.GetByAppidAsync(appid);
            if (!string.IsNullOrEmpty(mer?.wx_appid) && !mer.mid.Equals(Guid.Empty))
            {
                var redisStatistics = await RedisMerchantStatisticsOp.GetObjectAsync(mer.mid);

                //开团总数
                var groupTuple = await EsGroupManager.GetByMidAsync(Guid.Parse(mer.mid), new List<int>() { (int)EGroupStatus.已发布, (int)EGroupStatus.已结束, (int)EGroupStatus.已过期 }, 1, 10000);
                if (groupTuple.Item1 > 0)
                {
                    int groupCount = 0;
                    foreach (var group in groupTuple.Item2)
                    {
                        var count = await EsGroupOrderManager.GetByGidAsync2(Guid.Parse(group.Id), new List<int>() { (int)EGroupOrderStatus.拼团成功, (int)EGroupOrderStatus.开团中, (int)EGroupOrderStatus.拼团失败, (int)EGroupOrderStatus.拼团进行中 }, 1, 10000);
                        groupCount = groupCount + count.Item1;
                    }

                    if (int.Parse(redisStatistics.GroupOrderTotal) != groupCount)
                    {
                        redisStatistics.GroupOrderTotal = groupCount.ToString();
                    }
                }

                //成功开团数
                if (groupTuple.Item1 > 0)
                {
                    int groupOKCount = 0;
                    foreach (var group in groupTuple.Item2)
                    {
                        var count = await EsGroupOrderManager.GetByGidAsync2(Guid.Parse(group.Id), new List<int>() { (int)EGroupOrderStatus.拼团成功 }, 1, 10000);
                        groupOKCount = groupOKCount + count.Item1;
                    }

                    if (int.Parse(redisStatistics.GroupOrderOkTotal) != groupOKCount)
                    {
                        redisStatistics.GroupOrderOkTotal = groupOKCount.ToString();
                    }
                }
                string qdate = "2014-01-01 - 2055-01-01";
                //生成的订单总数
                var orderTotal =
                    await
                        EsOrderManager.SearchAsnyc(qdate, "", Guid.Parse(mer.mid), null,
                            new List<int>()
                            {
                                (int) EOrderStatus.拼团成功,
                                (int) EOrderStatus.已成团未提货,
                                (int) EOrderStatus.已支付,
                                (int) EOrderStatus.已退款
                            }, 1, 10000);
                if (int.Parse(redisStatistics.OrderTotal) != orderTotal.Item1)
                {
                    redisStatistics.OrderTotal = orderTotal.Item1.ToString();
                }

                //成功拼团的订单数
                var orderOkTotal =
           await
               EsOrderManager.SearchAsnyc(qdate, "", Guid.Parse(mer.mid), null,
                   new List<int>()
                   {
                                (int) EOrderStatus.拼团成功,
                                (int) EOrderStatus.已成团未提货
                   }, 1, 10000);
                if (int.Parse(redisStatistics.OrderOkTotal) != orderOkTotal.Item1)
                {
                    redisStatistics.OrderOkTotal = orderOkTotal.Item1.ToString();
                }

                //总成交额
                var orderTotalIncome =
           await
               EsOrderManager.SearchAsnyc(qdate, "", Guid.Parse(mer.mid), null,
                   new List<int>()
                   {
                                (int) EOrderStatus.拼团成功,
                                (int) EOrderStatus.已成团未提货,
                                (int)EOrderStatus.已支付
                   }, 1, 10000);


                int totalMoney = 0;
                foreach (var o in orderTotalIncome.Item2)
                {
                    totalMoney += o.actual_pay.Value;
                }
                //如果不相同则跟数据库中的比较
                if (int.Parse(redisStatistics.InComeTotal) != totalMoney)
                {
                    using (var repo = new BizRepository())
                    {
                        var orders = await repo.OrderGetByMidAsync(Guid.Parse(mer.mid), new List<int>()
                        {
                            (int) EOrderStatus.拼团成功,
                            (int) EOrderStatus.已成团未提货,
                            (int) EOrderStatus.已支付
                        });
                        int dbTotal = 0;
                        foreach (var o in orders)
                        {
                            dbTotal = dbTotal + o.actual_pay.Value;
                        }
                        //如果数据库跟es中相等,到店与总额是一样的。
                        //if (dbTotal == totalMoney)
                        //{
                        redisStatistics.InComeTotal = dbTotal.ToString();
                        redisStatistics.DaoDianInComeTotal = dbTotal.ToString();
                        //}
                    }
                }
                //更新redis
                return await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(redisStatistics);
            }
            return false;
        }
        #endregion


        #region att
        #region AttName

        [HttpGet]
        [Route("test/addAttName")]
        public async Task<bool> addAttName(string table_name, string att_name, string unit, string description)
        {
            using (var attrepo = new AttRepository())
            {
                return await attrepo.AddAttNameAsync(att_name, table_name, unit, description);
            }
        }

        [HttpGet]
        [Route("test/getAttName")]
        public async Task<AttName> getAttName(Guid attid)
        {
            using (var attrepo = new AttRepository())
            {
                return await attrepo.AttNameGetAsync(attid);
            }
        }
        [HttpGet]
        [Route("test/delAttName")]
        public async Task<bool> delAttName(Guid attid)
        {
            using (var attrepo = new AttRepository())
            {
                return await attrepo.DelAttNameAsync(attid);
            }
        }

        #endregion

        /// <summary>
        /// 批量从attvalue中更新group_type
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("group/updategrouptype")]
        public async Task<int> UpdateGroupType()
        {
            int i = 0;
            using (var attrepo = new AttRepository())
            {
                using (var repo = new BizRepository())
                {
                    var attname = await attrepo.AttNameGetAsync(EAttTables.Group.ToString(), EGroupAtt.group_type.ToString());//获取抽奖团的attid
                    if (attname == null || attname.attid.Equals(Guid.Empty))
                        return 0;

                    var grouplist = repo.GetAllGroupList();

                    foreach (var group in grouplist)
                    {
                        var attvalue = attrepo.AttValueGet(group.gid, attname.attid);
                        if (attvalue != null && attvalue.value == "1")
                        {
                            bool b = await repo.UpdateGroupGroup_type(group.gid, int.Parse(attvalue.value));
                            if (b)
                            {
                                var indexgroup = await EsGroupManager.GenObject(group.gid);
                                await EsGroupManager.AddOrUpdateAsync(indexgroup);
                                i++;
                            }
                        }
                    }
                }
            }
            return i;
        }

        [HttpGet]
        [Route("att/get")]
        public async Task<string> GetAttvalue(Guid o, string t, string a)
        {
            return await AttHelper.GetValueAsync(o, t, a);
        }

        [HttpGet]
        [Route("att/updateAttValue")]
        public async Task<bool> updateAttValue(Guid gid)
        {
            using (var repo = new AttRepository())
            {
                var group = await EsGroupManager.GetByGidAsync(gid);
                if (group == null || group.group_type != (int)EGroupTypes.抽奖团)
                    return false;
                var b = await repo.lucky_statusAddOrUpdateAsync(gid, 1);
                return b;
            }
        }

        [HttpGet]
        [Route("att/patch/redisbug")]
        public async Task<bool> patchGroupRedisBug()
        {
            using (var repo = new AttRepository())
            {
                Guid attid = Guid.Parse("901fc6c6-a7b7-4cac-9502-ef39bf06d116");
                var list =
                    await
                        repo.Context.AttValues.Where(
                            w => w.attid.Equals(attid) && w.value.Equals("1"))
                            .ToListAsync();
                if (list != null && list.Count > 0)
                {
                    foreach (var v in list)
                    {
                        var o_v = await AttHelper.GetValueAsync(v.owner, EAttTables.Group.ToString(),
                            EGroupAtt.userobot.ToString());
                        if (o_v.Equals(v.value))
                            continue;

                        await AttHelper.UpdateRedisValueAsync(v.owner, EAttTables.Group.ToString(),
                            EGroupAtt.userobot.ToString(), v.value);
                    }
                }
            }
            return true;
        }

        //public async Task<List<AttValue>> getallzjattvalue()
        //{
        //    using (var repo=new AttRepository())
        //    {
        //        repo.AttValueGetByAttid(Guid.Parse("3b973b85-b9e7-4b59-9e9d-d75d6b6cd616"), "1");
        //    }
        //}

        #endregion

        #region vector

        #region PTCG
        [HttpGet]
        [Route("vector/ptcg/test")]
        public async Task vectorTest(Guid goid)
        {
            PtSuccessVectorProcessor p = new PtSuccessVectorProcessor();
            Vector v = p.GenVector(goid.ToString());
            await VectorProcessorManager.Route(v);
        }

        [HttpGet]
        [Route("vector/ptcg/send")]
        public bool vectorTestSend(Guid goid)
        {
            //PtSuccessVectorProcessor p = new PtSuccessVectorProcessor();
            //Vector v = p.GenVector(goid.ToString());
            MqVectorManager.Send<PtSuccessVectorProcessor>(goid);
            return true;
        }

        [HttpGet]
        [Route("vector/ptcg/resetall")]
        public async Task<bool> vectorResetall()
        {
            try
            {
                var _redis = new RedisManager2<WeChatRedisConfig>();

                var zsetNameEveryKey = _redis.GetZsetNameEveryKey<VectorQMRedis, VectorUserQMZsetAttribute>("7ffea92b-89d2-4ca2-bd33-1809e2e5713d");


                var qwe = _redis.GetZsetName<VectorQMRedis, VectorUserQMZsetAttribute>(zsetNameEveryKey);
                var ssssss = _redis.GetZsetName<VectorQMRedis, VectorUserQMZsetAttribute>("7ffea92b-89d2-4ca2-bd33-1809e2e5713d");

                //var tuple = await EsGroupOrderManager.SearchAsnyc("", (int)EGroupOrderStatus.拼团成功, 1, 1000000);
                //if (tuple.Item1 > 0)
                //{
                //    //清空所有zset
                //    using (var repo = new BizRepository())
                //    {
                //        foreach (var u in repo.Context.Users.AsEnumerable())
                //        {
                //            var zsetNameEveryKey = _redis.GetZsetNameEveryKey<VectorQMRedis, VectorUserQMZsetAttribute>(u.uid.ToString());
                //            if (!string.IsNullOrEmpty(zsetNameEveryKey))
                //            {
                //                _redis.DeleteRedisKey<VectorQMRedis>(zsetNameEveryKey);
                //            }
                //        }
                //    }

                //    //填充zset
                //    foreach (var goid in tuple.Item2)
                //    {
                //        MqVectorManager.Send<PtSuccessVectorProcessor>(goid);
                //    }
                //}

                return true;
            }
            catch (Exception ex)
            {

                throw new MDException(typeof(WebApiController), ex);
            }
        }

        [HttpGet]
        [Route("vector/ptcg/get")]
        public async Task<List<KeyValuePair<string, double>>> vectorgetbyuid(Guid uid, int p)
        {
            var retsult = await RedisVectorOp.GetPTCGTop2Asnyc(uid, p, 10);
            if (retsult.Item2.Count > 0)
            {
                var redis = new RedisManager2<WeChatRedisConfig>();
                List<KeyValuePair<string, double>> _list = new List<KeyValuePair<string, double>>();
                foreach (var v in retsult.Item2)
                {
                    var redisValue = await redis.GetObjectFromRedisHash<UserInfoRedis>(v.Key);
                    _list.Add(new KeyValuePair<string, double>(redisValue.HeadImgUrl, v.Value));
                }
                return _list;
            }
            return null;
        }

        [HttpGet]
        [Route("vector/tl/send")]
        public async Task<bool> vectorTlSend(int t, Guid u, Guid b)
        {
            TimeLineVectorProcessor.TimeLineVectorParameter p = new TimeLineVectorProcessor.TimeLineVectorParameter();
            ETimelineType type;

            if (t == 0)
            {
                type = ETimelineType.CJPT;
            }
            else
            {
                type = ETimelineType.YDWZ;
            }
            p = TimeLineVectorHelper.GenParameter(type, u, new List<Guid>() { b }, null);
            if (p != null)
            {

                TimeLineVectorProcessor processor = new TimeLineVectorProcessor();
                Vector v = processor.GenVector(p);
                await VectorProcessorManager.Route(v);
                //MqVectorManager.Send<TimeLineVectorProcessor>(p);
                return true;
            }
            return false;
        }

        [HttpGet]
        [Route("vector/tl/get")]
        public async Task<List<object>> vectorTlGet(Guid u, int p)
        {
            var tuple = await TimeLineVectorHelper.GetTopAsnyc(u, p);
            if (tuple.Item2?.Count > 0)
            {
                List<object> _list = new List<object>();

                var redis = new RedisManager2<WeChatRedisConfig>();
                foreach (var v in tuple.Item2)
                {
                    //从redis中取出vector
                    var vectorRedis = await redis.GetObjectFromRedisHash<VectorRedis>(v.Key);
                    if (string.IsNullOrEmpty(vectorRedis.value))
                        continue;
                    //序列化成vector db对象
                    Vector vv = RedisCommonHelper.StringToObject<Vector>(vectorRedis.value);//vectorRedis.GetObject();
                    var view = VectorProcessorManager.Parse(vv);
                    if (view == null)
                        continue;
                    //user头像
                    var userRedis = await RedisUserOp.GetByUidAsnyc(Guid.Parse(view.Owner));

                    _list.Add(new
                    {
                        type = view.Type,
                        head_pic = userRedis.HeadImgUrl,
                        comment = view.Contents[0],
                        bizid = view.Objects[0],
                        timestamp = CommonHelper.FromUnixTime(view.Timestamp).ToString()
                    });
                }



                return _list;
            }

            return null;
        }

        #endregion

        #endregion

        #region MOrder
        [HttpGet]
        [Route("morder/KTJS2000")]
        /// <summary>
        /// 开团就送2000测试
        /// </summary>
        /// <returns></returns>
        public async Task<string> KTJS2000()
        {
            using (var repo = new BizRepository())
            {
                Merchant mer = await repo.GetMerchantByMidAsync(Guid.Parse("7bd31f1c-3850-405a-8da3-03e77b79969e"));
                await MBizRule.BuyTaocan(mer, ECodeTaocanType.KTJS10, 1, 0);
                var quota = await repo.MerchantGetBizQuota(mer.mid, EBizType.DD.ToString());
            }
            return "1";
        }

        [HttpGet]
        [Route("morder/ktjs10")]
        /// <summary>
        /// 开团就送2000测试
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ktjs10()
        {
            using (var repo = new BizRepository())
            {
                foreach (var mer in repo.Context.Merchants.AsEnumerable())
                {
                    await MBizRule.BuyTaocan(mer, ECodeTaocanType.KTJS10, 1, 0);
                }
            }
            return true;
        }
        #endregion

        #region User
        [HttpPost]
        [Route("user/AddUpdateRobot")]
        public async Task<string> AddUpdateRobot(UserParameter parameter)
        {
            if (string.IsNullOrEmpty(parameter.nickname) || string.IsNullOrEmpty(parameter.sex.ToString()) || string.IsNullOrEmpty(parameter.country)
                || string.IsNullOrEmpty(parameter.city) || string.IsNullOrEmpty(parameter.province) || string.IsNullOrEmpty(parameter.headimgurl))
            {
                return "参数错误";
            }
            using (var repo = new BizRepository())
            {
                bool flag = false;
                Model.DB.User user = await repo.UserGetByUidAsync(parameter.uid);
                if (user == null || parameter.uid.Equals(Guid.Empty))//新增
                {
                    user = new Model.DB.User();
                    user.openid = "Robot" + Guid.NewGuid().ToString().Replace("-", "");
                    user.wx_appid = "Robot";
                    user.mid = new Guid("11111111-1111-1111-1111-111111111111");
                    user.name = EmojiFilter.FilterEmoji(parameter.nickname);
                    user.sex = parameter.sex;
                    flag = await repo.SaveOrUpdateUserAsnyc(user);//此方法新增时不会更新ES,修改时才更新ES
                    user = await repo.UserGetByOpenIdAsync(user.openid);//重新取出完整的User
                    var indexUser = EsUserManager.GenObject(user);
                    flag = await EsUserManager.AddOrUpdateAsync(indexUser);
                }
                else
                {
                    flag = await repo.SaveOrUpdateUserAsnyc(user);//此方法新增时不会更新ES,修改时才更新ES
                }

                if (flag)
                {
                    //把头像保存到radis
                    UserInfoRedis u = new UserInfoRedis();
                    u.Uid = user.uid.ToString();
                    u.Openid = user.openid;
                    u.City = parameter.city;
                    u.Country = parameter.country;
                    u.HeadImgUrl = parameter.headimgurl;
                    u.NickName = user.name;
                    u.Sex = user.sex.ToString();
                    u.Privilege = null;
                    u.Unionid = null;
                    u.Province = parameter.province;
                    u.ExpireIn = (CommonHelper.GetUnixTimeNow() + TimeSpan.FromDays(7).TotalSeconds).ToString();
                    //保存
                    bool flag2 = await new RedisManager2<WeChatRedisConfig>().SaveObjectAsync(u);
                    return flag2 ? "保存成功" : "保存失败";
                }
            }
            return "保存失败了";
        }

        [HttpGet]
        [Route("user/getESuser")]
        public async Task<HttpResponseMessage> getESuser(Guid uid)
        {
            var user = await EsUserManager.GetByIdAsync(uid);
            return JsonResponseHelper.HttpRMtoJson(user, HttpStatusCode.OK, ECustomStatus.Success);
        }

        [HttpGet]
        [Route("user/getRadisuser")]
        public async Task<HttpResponseMessage> getRadisuser(string openid)
        {
            UserInfoRedis userRedis =
            await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openid);
            return JsonResponseHelper.HttpRMtoJson(userRedis, HttpStatusCode.OK, ECustomStatus.Success);
        }
        public class UserParameter
        {
            public Guid uid { get; set; }
            public string nickname { get; set; }
            public int sex { get; set; }
            public string headimgurl { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string province { get; set; }
        }

        /// <summary>
        /// 解决存user到ES报错！转换成index错误3！报错问题
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="appid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("user/saveUserToEs")]
        public async Task<HttpResponseMessage> saveUserToEs(string openid, string appid)
        {
            //实际上是模仿MdUserHelper.SaveUserFromCallback2方法

            UserInfoRedis u = await new RedisManager2<WeChatRedisConfig>().GetObjectFromRedisHash<UserInfoRedis>(openid);
            if (u == null)
                return JsonResponseHelper.HttpRMtoJson("oRedis里没有user", HttpStatusCode.OK, ECustomStatus.Fail);

            //存入db redis es
            using (var repo = new BizRepository())
            {
                var mer = await repo.GetMerchantByAppidAsync(appid);
                if (mer == null || !mer.wx_appid.Equals(appid))
                {
                    MDLogger.LogErrorAsync(typeof(MdUserHelper),
                        new Exception($"appid:{appid}与mer:appid:{appid}冲突，或者mer为null!"));
                    return null;
                }

                var DBuser = await repo.UserGetByOpenIdAsync(openid);

                //更新es
                var indexUser = EsUserManager.GetById(DBuser.uid);
                int times = 0;
                while (indexUser == null && times <= 3)
                {
                    times++;
                    Thread.Sleep(times * 100);
                    indexUser = EsUserManager.GetById(DBuser.uid);
                }
                if (indexUser == null)
                {
                    //将user存到ES
                    if (await EsUserManager.AddOrUpdateAsync(EsUserManager.GenObject(DBuser)))
                        return JsonResponseHelper.HttpRMtoJson("存user到ES成功", HttpStatusCode.OK, ECustomStatus.Success);
                    //throw new MDException(typeof(MdUserHelper), $"存user到ES报错！转换成index错误3！openid:{user.openid},uid:{user.uid},times:{times}");
                }

                return JsonResponseHelper.HttpRMtoJson("存user到ES失败", HttpStatusCode.OK, ECustomStatus.Fail);
            }
        }
        #endregion

        #region rebot
        [HttpGet]
        [Route("robot/getall")]
        public ConcurrentDictionary<Guid, string> robotGetall()
        {
            return RobotHelper.GetAll();
        }

        [HttpGet]
        [Route("robot/reload")]
        public ConcurrentDictionary<Guid, string> robotReload()
        {
            RobotHelper.ReloadRobots();
            return RobotHelper.GetAll();
        }

        [HttpGet]
        [Route("robot/send")]
        public async Task<bool> send(Guid goid)
        {
            using (var repo = new BizRepository())
            {
                var go = await repo.GroupOrderGet(goid);
                if (go != null)
                {
                    await RobotHelper.CompleteAGo(go);
                    return true;
                }
                return false;
            }
        }

        [HttpGet]
        [Route("robot/getgo")]
        public async Task<List<GroupOrder>> getgos()
        {
            using (var repo = new BizRepository())
            {
                var gos = await repo.GroupOrderGetbyExpiretimeAsync(30);
                return gos;
            }
        }
        #endregion

        #region 登录统计

        [HttpGet]
        [Route("tj")]
        public async Task<Tuple<int, List<BizIndex>>> tjMid(string t, Guid o, double f)
        {
            double to = CommonHelper.GetUnixTimeNow();
            var type = t.Equals("m") ? ELogBizModuleType.MidView : ELogBizModuleType.GidView;
            return await EsBizLogStatistics.SearchBizViewAsnyc(type, o, Guid.Empty, f, to);
        }

        [HttpGet]
        [Route("tj/all")]
        public async Task<Tuple<int, List<BizIndex>>> tjall(string t, double f)
        {
            double to = CommonHelper.GetUnixTimeNow();
            var type = t.Equals("m") ? ELogBizModuleType.MidView : ELogBizModuleType.GidView;
            return await EsBizLogStatistics.SearchBizViewAsnyc(type, Guid.Empty, Guid.Empty, f, to);
        }

        [HttpGet]
        [Route("tj/m/count")]
        public async Task<Dictionary<string, long>> tjcount()
        {
            var _ret = await EsBizLogStatistics.GetMTopN(20);
            if (_ret.Items.Count > 0)
            {
                Dictionary<string, long> ret = new Dictionary<string, long>();
                foreach (var v in _ret.Items)
                {
                    Guid mid = Guid.Parse(v.Key);
                    var temp = await RedisMerchantOp.GetByMidAsync(mid);
                    ret[temp.name] = v.DocCount;
                }
                return ret;
            }
            return null;
        }
        [HttpGet]
        [Route("tj/g/count")]
        public async Task<Dictionary<string, long>> tjgcount()
        {
            var _ret = await EsBizLogStatistics.GetGAccessTopN(20);
            if (_ret.Items.Count > 0)
            {
                Dictionary<string, long> ret = new Dictionary<string, long>();
                foreach (var v in _ret.Items)
                {
                    Guid gid = Guid.Parse(v.Key);
                    var temp = await EsGroupManager.GetByGidAsync(gid);
                    ret[temp.title] = v.DocCount;
                }
                return ret;
            }
            return null;
        }

        [HttpGet]
        [Route("tj/all/count")]
        public async Task<Dictionary<string, long>> allcount()
        {
            var mTotal = await EsBizLogStatistics.GetTotalCount(ELogBizModuleType.MidView);
            var gTotal = await EsBizLogStatistics.GetTotalCount(ELogBizModuleType.GidView);
            Dictionary<string, long> _dic = new Dictionary<string, long>();
            _dic["gid"] = gTotal;
            _dic["mid"] = mTotal;
            return _dic;
        }

        [HttpGet]
        [Route("tj/all/au")]
        public async Task<long> tjaucount()
        {
            double f = CommonHelper.ToUnixTime(DateTime.Now.Date);
            double t = CommonHelper.ToUnixTime(DateTime.Now.Date + TimeSpan.FromDays(1));
            return await EsBizLogStatistics.GetActiveUserCount(f, t);
        }

        [HttpGet]
        [Route("tj/bizall")]
        public async Task<Dictionary<string, object>> GetDateHistogram()
        {
            DateTime timeEnd = DateTime.Now;
            DateTime timeStart = timeEnd.AddMonths(-1);
            Guid mid = Guid.Parse("d882482f-1975-483b-b075-caa5133763c9");
            Dictionary<string, object> dic = new Dictionary<string, object>();
            var res = await EsBizLogStatistics.GetDateHistogram(timeStart, timeEnd, mid, ELogBizModuleType.MidView.ToString());
            dic.Add("res", res);
            return dic;
        }
        [HttpGet]
        [Route("tj/bizbyhour")]
        public async Task<Dictionary<string, object>> GetHourHistogram()
        {
            DateTime timeEnd = DateTime.Now;
            //DateTime timeStart =Convert.ToDateTime(timeEnd.ToString("yyyy-MM-dd 00:00:00"));
            DateTime timeStart = DateTime.Today;
            Guid mid = Guid.Parse("d882482f-1975-483b-b075-caa5133763c9");
            Dictionary<string, object> dic = new Dictionary<string, object>();
            var res = await EsBizLogStatistics.GetHourHistogram(timeStart, timeEnd, mid);
            dic.Add("res", res);
            return dic;
        }

        [HttpGet]
        [Route("update/bizmid")]
        public async Task<Dictionary<string, object>> UpdateBiz()
        {
            var tuple = await EsBizLogStatistics.SearchBizViewAsnyc(ELogBizModuleType.PayView, Guid.Empty, Guid.Empty, Guid.Empty, null, null, 1, 10000);
            List<BizIndex> list = tuple.Item2;
            Dictionary<string, object> dic = new Dictionary<string, object>();
            foreach (var item in list)
            {
                IndexUser u = await EsUserManager.GetByIdAsync(Guid.Parse(item.UserUuid));
                if (u != null)
                {
                    item.UnUsed2 = u.mid;
                    LogESManager.UpdateBiz(item);
                }
            }
            dic.Add("res", true);
            return dic;
        }

        [HttpGet]
        [Route("test/getguid")]
        public async Task<Dictionary<string, object>> getguid()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            List<string> list = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(GuidHelper.GuidTo16String2());
            }
            dic.Add("res", list);
            return dic;
        }
        #endregion

        #region WriteOffer
        [HttpGet]
        [Route("writeoffer/getwriteofferurl")]
        public string getwriteofferurl(string appid, Guid oid)
        {
            string url = MdWxSettingUpHelper.GenWriteOffOrderUrl(appid, oid);
            return url;
        }

        [HttpGet]
        [Route("writeoffer/writeofferUpdate")]
        public async Task<string> writeofferUpdate(Guid uid, int amount)
        {
            using (var repo = new BizRepository())
            {
                int i = 0;
                var list = await repo.getAllWriteoffer();
                foreach (var writeoffer in list)
                {
                    var indexWriteOffer = await EsWriteOfferManager.GetByUidAsync(writeoffer.uid);
                    if (indexWriteOffer == null)
                    {
                        var tempIndex = EsWriteOfferManager.GenObject(writeoffer);
                        if (tempIndex != null)
                        {
                            var flag = await EsWriteOfferManager.AddOrUpdateAsync(tempIndex);
                            if (flag)
                                i++;
                        }
                    }
                }
                return $"数据库数据:{list.Count}条，新增了{i}条";
            }
        }
        #endregion

        [HttpGet]
        [Route("sta/writeoff")]
        public async Task<HttpResponseMessage> GetWriteOffPointSta(string q, int pageIndex, int pageSize)
        {
            List<object> listRes = new List<object>();
            double timeStart = CommonHelper.ToUnixTime(Convert.ToDateTime("2016-01-01"));
            double timeEnd = CommonHelper.ToUnixTime(Convert.ToDateTime("2016-08-31"));
            using (var repo = new BizRepository())
            {
                var tuple = await repo.MerchantSearchByNameAsync(q, pageIndex, pageSize);
                if (tuple.Item1 > 0 && tuple.Item2 != null && tuple.Item2.Count > 0)
                {
                    List<Merchant> list = tuple.Item2.ToList();
                    foreach (Merchant m in list)
                    {
                        //var MerchantSta = await RedisMerchantStatisticsOp.GetObjectAsync(m.mid.ToString());
                        object obj = new
                        {
                            mid = m.mid,
                            name = m.name,
                            pointCount = await EsWriteOffPointManager.GetCountByMidAsync(m.mid),
                            regDate = CommonHelper.FromUnixTime((double)m.register_date).ToString("yyyy-MM-dd HH:mm"),
                            productCount = await EsProductManager.GetCountByMidAsync(m.mid, timeStart, timeEnd),
                            groupCountAll = await EsGroupManager.GetCountByMidAsync(m.mid, new List<int>() { (int)EGroupStatus.已发布 }, timeStart, timeEnd),
                            groupCountK = await EsGroupManager.GetCountByMidAsync(m.mid, new List<int>() { (int)EGroupOrderStatus.拼团成功, (int)EGroupOrderStatus.拼团失败, (int)EGroupOrderStatus.拼团进行中 }, timeStart, timeEnd),
                            groupCountS = await EsGroupManager.GetCountByMidAsync(m.mid, new List<int>() { (int)EGroupOrderStatus.拼团成功 }, timeStart, timeEnd),
                            orderCount = await EsOrderManager.GetOrderCountAsync(m.mid, new List<int>() { (int)EOrderStatus.拼团成功 }, timeStart, timeEnd),
                            orderAmount = 0,
                            orderH = 0,
                            viewCount = 0
                        };
                        listRes.Add(obj);
                    }

                }
                return JsonResponseHelper.HttpRMtoJson(listRes, HttpStatusCode.OK, ECustomStatus.Success);
            }

        }

        #region for test

        [HttpGet]
        [Route("ft/addappid")]
        public async Task<bool> fortestadd(string appid)
        {
            return await RedisForTestOp.AddTestAppidAsync(appid);
        }

        [HttpGet]
        [Route("ft/rmappid")]
        public async Task<bool> ftrmappid(string appid)
        {
            return await RedisForTestOp.RemoveTestAppidAsnyc(appid);
        }

        [HttpGet]
        [Route("ft/containappid")]
        public async Task<bool> containappid(string appid)
        {
            return await RedisForTestOp.ContainAppidAsync(appid);
        }

        [HttpGet]
        [Route("ft/allappid")]
        public async Task<List<string>> allappid(string appid)
        {
            return await RedisForTestOp.GetAllAppidsAsnyc(appid);
        }

        #endregion

        #region group
        [HttpGet]
        [Route("group/getesgroup")]
        public async Task<HttpResponseMessage> getEsGroup(Guid mid)
        {
            var result = await EsGroupManager.GetByMidAsync(mid, new List<int>() { (int)EGroupStatus.已发布 }, 1, 10);
            return JsonResponseHelper.HttpRMtoJson(new { total = result.Item1, list = result.Item2 }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        /// <summary>
        /// 使一个下线的团重新上线
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("group/grouponline")]
        public async Task<HttpResponseMessage> groupOnline(Guid gid)
        {
            if (gid.Equals(Guid.Empty))
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                if (await repo.GroupOnline(gid))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return JsonResponseHelper.HttpRMtoJson($"团成功上线!", HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson($"团上线失败!", HttpStatusCode.OK, ECustomStatus.Fail);
        }

        /// <summary>
        /// 修改set_product_setting_count，校正已售数量
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="product_setting_count"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("group/set_product_setting_count")]
        public async Task<HttpResponseMessage> groupUpdateproduct_setting_count(Guid gid, int product_setting_count)
        {
            if (gid.Equals(Guid.Empty) || product_setting_count <= 0)
                return JsonResponseHelper.HttpRMtoJson($"parameter error!", HttpStatusCode.OK, ECustomStatus.Fail);
            using (var repo = new BizRepository())
            {
                if (await repo.UpdateGroupSetting_Count(gid, product_setting_count))
                {
                    if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
                    {
                        return JsonResponseHelper.HttpRMtoJson($"更新product_setting_count成功!", HttpStatusCode.OK, ECustomStatus.Success);
                    }
                }
            }
            return JsonResponseHelper.HttpRMtoJson($"更新product_setting_count失败!", HttpStatusCode.OK, ECustomStatus.Fail);
        }

        /// <summary>
        /// 单独把一个group同步到ES
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("group/syncallgroupBygid")]
        public async Task<HttpResponseMessage> syncallgroupBygid(Guid gid)
        {
            if (await EsGroupManager.AddOrUpdateAsync(await EsGroupManager.GenObject(gid)))
            {
                return JsonResponseHelper.HttpRMtoJson($"更新group到ES成功!", HttpStatusCode.OK, ECustomStatus.Success);
            }
            return JsonResponseHelper.HttpRMtoJson($"更新group到ES失败!", HttpStatusCode.OK, ECustomStatus.Success);
        }
        /// <summary>
        /// 从ES中查询Group信息
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("test/es/getgroupbyid")]
        public async Task<IndexGroup> getEsGroupbyid(Guid gid)
        {
            return await EsGroupManager.GetByGidAsync(gid);
        }
        [HttpGet]
        [Route("group/getgrouplucky")]
        public async Task<HttpResponseMessage> getgrouplucky()
        {
            using (var repo = new BizRepository())
            {
                //var list = await repo.luckyGroupGetAsync();
                var list = repo.luckyGroupGet();
                return JsonResponseHelper.HttpRMtoJson(list, HttpStatusCode.OK, ECustomStatus.Success);
            }
        }
        [HttpGet]
        [Route("group/testGroupInventory")]
        public HttpResponseMessage testGroupInventory(int flag)
        {
            List<object> retobj = new List<object>();
            for (int i = 0; i < 10000; i++)
            {
                retobj.Add(new
                {
                    Age = i + 1,
                    Name = "Name" + i,
                    Sex = i % 2 == 0 ? "男" : "女",
                    IsMarry = i % 2 > 0 ? true : false
                });
            }
            return JsonResponseHelper.HttpRMtoJson(new { total = 3, retobj = retobj }, HttpStatusCode.OK, ECustomStatus.Success);
        }
        #endregion

        #region transfer index

        [HttpGet]
        [Route("transfer/log")]
        public async Task<int> TransferLog()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            LogESManager.MappingAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<LogConfig, BizIndex>("mdlog", delegate (BizIndex obj)
            {
                LogESManager.AddOrUpdateBiz(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/order")]
        public async Task<int> TransferOrder()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsOrderManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsOrderConfig, IndexOrder>("order", delegate (IndexOrder obj)
            {
                EsOrderManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/go")]
        public async Task<int> TransferGo()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsGroupOrderManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsGroupOrderConfig, IndexGroupOrder>("grouporder", delegate (IndexGroupOrder obj)
            {
                EsGroupOrderManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/group")]
        public async Task<int> Transfergroup()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsGroupManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsGroupConfig, IndexGroup>("group", delegate (IndexGroup obj)
            {
                EsGroupManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/product")]
        public async Task<int> Transferproduct()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsProductManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsProductConfig, IndexProduct>("product", delegate (IndexProduct obj)
            {
                EsProductManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/user")]
        public async Task<int> Transferuser()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsUserManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsUserConfig, IndexUser>("user", delegate (IndexUser obj)
            {
                EsUserManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/notice")]
        public async Task<int> TransferNotice()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsNoticeBoardManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsNoticeBoardConfig, IndexNoticeBoard>("noticeboard", delegate (IndexNoticeBoard obj)
            {
                EsNoticeBoardManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }

        [HttpGet]
        [Route("transfer/wo")]
        public async Task<int> Transferwo()
        {
            var Newclient = ESHeper.GetClient("10.26.250.100", "9200");
            EsWriteOffPointManager.MapAClient(Newclient);

            int count = await ScrollHelper.ProcessScrollAsync<EsWriteOffPointConfig, IndexWriteOffPoint>("writeoffpoint", delegate (IndexWriteOffPoint obj)
            {
                EsWriteOffPointManager.AddOrUpdate(Newclient, obj);
            });
            return count;
        }
        #endregion

        #region Md_GroupLottery_Process
        [HttpGet]
        [Route("Lottery/Md_GroupLottery_Process")]
        public async Task<bool> lotteryTest()
        {
            WxServiceHelper.Md_GroupLottery_Process();
            return true;
        }
        #endregion

        [HttpGet]
        [Route("sta/GetExcel")]
        public async Task<HttpResponseMessage> GetExcel()
        {
            string UserPath = @"D:\EEEE.txt";
            List<string> listUser = new List<string>();
            using (StreamReader sr = File.OpenText(UserPath))
            {
                string users = "";
                while (true)
                {
                    users = sr.ReadLine();
                    if (users != null)
                    {
                        listUser.Add(users);
                    }
                    else
                    {
                        break;
                    }
                }
                List<string> retobj = new List<string>();
                using (var reop = new BizRepository())
                {
                    Guid mid = Guid.Parse("d882482f-1975-483b-b075-caa5133763c9");
                    string woid = "7866dd33-0ed1-425a-a504-b2bce595a16f";
                    var orders = await reop.GetListOrder(mid, woid, 1472659200, 1475251200);
                    //foreach (var o_no in listUser)
                    //{
                    //   var dds = o_no.ToLower();
                    //    var order = orders.Where(p => p.o_no.Contains(dds)).FirstOrDefault();
                    //    if (order == null)
                    //    {
                    //        retobj.Add(o_no);
                    //    }
                    //}
                    foreach (var item in orders)
                    {
                        var dds = item.o_no.ToLower();
                        var dds2 = dds.Substring(dds.Length - 6, 6);
                        if (!listUser.Contains(dds2))
                        {
                            retobj.Add(dds);
                        }
                    }
                    return JsonResponseHelper.HttpRMtoJson(new { retobj, count = retobj.Count }, HttpStatusCode.OK, ECustomStatus.Success);
                }

            }
        }

        #region clear test data
        [HttpGet]
        [Route("del/DelLadderGroupByGid")]
        public async Task<Dictionary<string, object>> DelLadderGroupByGid(Guid gid)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            try
            {
                using (var repo = new ActivityRepository())
                {
                    var group = await repo.GetGroupByIdAsync(gid);
                    if (group != null)
                    {
                        //var s = "http://picscdn.mmpintuan.com/";
                        //int index = group.pic.LastIndexOf("/");
                        //string filePath = group.pic.Substring(index + 1);

                        //string sPath = "a/0e7cd2fbc96540f89ea06f823d5d17f2/a/1477636513.10489";
                        //Stream st = OssPicPathManager<OssPicBucketConfig>.DownloadPic(sPath);
                        //Image img = Image.FromStream(st);
                        //img.Save(@"d:\img.jpg");
                        //string name = CommonHelper.GetUnixTimeNow().ToString();
                        //string ss = OssPicPathManager<OssPicBucketConfig>.GetActivityPath(gid);

                        //OssPicPathManager<OssPicBucketConfig>.DeletePic(filePath);

                        bool res = await EsLadderGroupManager.DeleteAsync(gid.ToString());
                        dic.Add("res", true);
                    }
                }
            }
            catch (Exception ex)
            {
                dic.Add("res", "error");
                dic.Add("message", ex);
            }
            return dic;
        }


        #endregion

        [HttpGet]
        [Route("ladder/sucessLaddergroupOrder")]
        public bool sucessLaddergroupOrder()
        {
            using (var acti = new ActivityRepository())
            {
                //过期的laddergrouporder,全部改为拼团成功
                var list = acti.GroupOrderGetFailsByTimeLimits();
                foreach (var go in list)
                {
                    MdInventoryHelper.SuccessLadderGroupOrder(go);
                }
            }
            return true;
        }
        [HttpGet]
        [Route("ladder/updateLaddergroupOrder")]
        public async Task<bool> updateLaddergroupOrder(Guid goid, int expire_date)
        {
            using (var acti = new ActivityRepository())
            {
                var go = await acti.GroupOrderGetAsync(goid);
                go.expire_date = expire_date;
                await acti.GroupOrderUpdateAsync(go);
            }
            return true;
        }

        #region noticeboard
        [HttpGet]
        [Route("noticeboard/updateesnoticeboard")]
        public async Task<string> updateesnoticeboard()
        {
            using (var repo=new BizRepository())
            {
                int i = 0;
                var list = await EsNoticeBoardManager.GetAllNoticeBoardAsync();
                foreach (var n in list)
                {
                    n.mid = "11111111-1111-1111-1111-111111111111";
                    var flag = await EsNoticeBoardManager.AddOrUpdateAsync(n);
                    if (flag)
                        i++;
                }
                return $"总记录{list.Count}条,更新成功记录{i}条";
            }
        }
        #endregion
    }
}

