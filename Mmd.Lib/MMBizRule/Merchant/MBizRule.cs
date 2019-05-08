using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.DB.Repositorys;
using MD.Lib.Log;
using MD.Lib.Util;
using MD.Lib.Util.MDException;
using MD.Model.DB;
using MD.Model.DB.Code;

namespace MD.Lib.MMBizRule.MerchantRule
{
    public static class MBizRule
    {
        public static async Task<MBiz> BuyTaocan(Merchant mer, ECodeTaocanType taocanType, int buyCount, int money)
        {
            try
            {
                if (mer == null || mer.mid.Equals(Guid.Empty))
                    throw new Exception($"Merchant is null, value:{mer}");
                using (var bizRepo = new BizRepository())
                {
                    CodeBizTaocan tc = null;
                    CodeMerPayType payType = null;
                    CodeBizTaocanItem tcItem = null;
                    using (var codeRepos = new CodeRepository())
                    {
                        tc = await codeRepos.GetTaoCanByType(taocanType.ToString());
                        if (tc == null)
                            throw new Exception($"tc is null, value:{taocanType}");
                        var tcItemList = await codeRepos.GetTaoCanItemByType(tc.tc_type);
                        if (tcItemList == null || tcItemList.Count == 0)
                            throw new Exception($"tc is null, value:{taocanType}");
                        tcItem = tcItemList.FirstOrDefault();
                        payType = await codeRepos.GetMPayType((int)EMPayType.其他);
                        if (payType == null)
                            throw new Exception($"payType is null, value:{EMPayType.其他}");
                    }

                    //0元充值2000个订单
                    //购买一份KTJS2000套餐
                    var result = await bizRepo.MorderCreateAsync(mer, tc.tc_type, buyCount, money);
                    if (result.Item1.Equals(EMorderResponse.OK))
                    {
                        var ret = await bizRepo.MorderFinish(result.Item2.moid, payType, 0, CommonHelper.GetUnixTimeNow(), "");

                        if (ret.Equals(EMorderResponse.OK))
                        {
                            return await bizRepo.GetMbizAsync(mer.mid, tcItem.biz_type);
                        }

                        if (ret.Equals(EMorderResponse.Overflow))
                        {
                            MDLogger.LogErrorAsync(typeof(MBizRule), new Exception($"充值失败！appid:{mer.wx_appid},tc:{ECodeTaocanType.KTJS2000.ToString()},充值金额{0},充值时间{DateTime.Now}!原因：充值超限造成的！"));
                        }

                        if (ret.Equals(EMorderResponse.Error))
                        {
                            MDLogger.LogErrorAsync(typeof(MBizRule), new Exception($"充值失败！appid:{mer.wx_appid},tc:{ECodeTaocanType.KTJS2000.ToString()},充值金额{0},充值时间{DateTime.Now}.原因：发生错误！"));
                        }
                    }

                    if (result.Item1.Equals(EMorderResponse.Overflow))
                    {
                        //MDLogger.LogErrorAsync(typeof(MBizRule),new Exception($"充值失败！appid:{mer.wx_appid},tc:{ECodeTaocanType.KTJS2000.ToString()},充值金额{0},充值时间{DateTime.Now}!原因：充值超限造成的！"));
                    }
                    if (result.Item1.Equals(EMorderResponse.Error))
                    {
                        MDLogger.LogErrorAsync(typeof(MBizRule), new Exception($"充值失败！appid:{mer.wx_appid},tc:{ECodeTaocanType.KTJS2000.ToString()},充值金额{0},充值时间{DateTime.Now}.原因：发生错误！"));
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new MDException(typeof(MBizRule), ex);
            }
        }
    }
}
