using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Configuration;
using MD.Lib.Util.MDException;
using MD.Lib.Weixin.Component;
using MD.Model.Configuration.UI;

namespace MD.Lib.Weixin.MmdBiz
{
    public static class MdWxSettingUpHelper
    {
        /// <summary>
        /// 为每个商家的展示入口生成微信回调页面
        /// </summary>
        /// <param name="appid"></param>
        /// <returns></returns>
        public static string GenEntranceUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "1-xj", "state", "group", "entrance");
            return url;
        }

        public static string GenCommuintryEntranceUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "1-xj", "state", "group", "communitryentrance");
            return url;
        }

        public static string GenWriteOfferUrl(string appid, Guid woid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, woid.ToString(), "state", "WriteOff", "index");
            return url;
        }

        public static string GenWriteOffOrderUrl(string appid, Guid oid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, oid.ToString(), "state", "WriteOff", "WriteOffOrder");
            return url;
        }

        public static string GenGoDetailUrl(string appid, Guid goid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, goid.ToString(), "state", "Group", "godetail");
            return url;
        }
        public static string GenGoDetailUrl_fx(string appid, Guid goid, string shareopenid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl_fx(appid, goid.ToString(), shareopenid, "state", "Group", "godetail_fx");
            return url;
        }

        public static string GenProductListUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "", "state", "Group", "productlist");
            return url;
        }
        public static string GenProductDetailUrl(string appid, Guid pid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, pid.ToString(), "state", "Group", "productdetail");
            return url;
        }

        public static string GenOrderDetailUrl(string appid, Guid oid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, oid.ToString(), "state", "Group", "orderdetail");
            return url;
        }
        public static string GenGroupDetailUrl(string appid, Guid gid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, gid.ToString(), "state", "Group", "groupdetail");
            return url;
        }

        public static string GenGroupDetailUrl_fx(string appid, Guid gid, string shareopenid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl_fx(appid, gid.ToString(), shareopenid, "state", "Group", "groupdetail_fx");
            return url;
        }
        //生成分享文章的微信回调地址
        public static string GenNoticeDetailUrl(string appid, Guid nid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, nid.ToString(), "state", "Group", "noticedetail");
            return url;
        }
        public static string GenGroupPreviewUrl(string appid, Guid gid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, gid.ToString(), "state", "Group", "grouppreview");
            return url;
        }
        #region 寻宝分享与核销
        /// <summary>
        /// 寻宝分享的URL
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="mid"></param>
        /// <returns></returns>
        public static string GenFindBoxUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "", "state", "Group", "gotoolbox");
            return url;
        }
        /// <summary>
        /// 生成宝箱核销的URL
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static string GenWriteOffFindBoxUrl(string appid, string utid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, utid, "state", "WriteOff", "WriteOffFindBox");
            return url;
        }
        #endregion
        #region 签到分享与核销
        public static string GenSignUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "", "state", "Group", "gotoolboxsign");
            return url;
        }
        public static string GenWriteOffSignUrl(string appid, string usid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, usid, "state", "WriteOff", "WriteOffSign");
            return url;
        }
        #endregion
        #region 阶梯团分享与核销
        public static string GenLadderGroupListUrl(string appid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, "", "state", "Group", "laddergrouplist");
            return url;
        }
        public static string GenLadderGroupDetailUrl(string appid, Guid gid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, gid.ToString(), "state", "Group", "laddergroupdetail");
            return url;
        }
        public static string GenLadderGoDetailUrl(string appid, Guid goid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, goid.ToString(), "state", "Group", "laddergrouporderdetail");
            return url;
        }
        public static string GenLadderWriteOffUrl(string appid, Guid oid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, oid.ToString(), "state", "Group", "laddergrouporderwriteoff");
            return url;
        }
        #endregion

        public static string GenCommunityMyCenterUrl(string appid, Guid to_uid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, to_uid.ToString(), "state", "Group", "communityMyCenter");
            return url;
        }

        public static string GenCommunityDetailUrl(string appid, Guid cid)
        {
            var url = WXComponentUserHelper.GenComponentCallBackUrl(appid, cid.ToString(), "state", "Group", "communitydetail");
            return url;
        }

        public static string GenSupplyDetailUrl(Guid sid)
        {
            var url = WXComponentUserHelper.GenCallBackUrl(sid.ToString(), "Group", "supplydetail");
            return url;
        }

        public static int GetPageSize()
        {
            var config = MdConfigurationManager.GetConfig<UiBackEndConfig>();
            if (config == null)
                throw new MDException(typeof(MdWxSettingUpHelper), new Exception("GetPagesize config is null!"));
            return int.Parse(config.PageSize);
        }

        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <param name="totalCount">总行数</param>
        /// <returns>总页数</returns>
        public static int GetTotalPages(int totalCount)
        {
            if (totalCount <= 0)
                return 0;
            int size = GetPageSize();
            if (totalCount % size == 0)
                return totalCount / size;
            return totalCount / size + 1;
        }
        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <param name="totalCount">总行数</param>
        /// <param name="pageSize">分页的行数,默认10条</param>
        /// <returns></returns>
        public static int GetTotalPages(int totalCount, int pageSize)
        {
            if (totalCount <= 0)
                return 0;
            if (totalCount % pageSize == 0)
                return totalCount / pageSize;
            return totalCount / pageSize + 1;
        }
    }
}
