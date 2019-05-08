using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz.Community
{
    public class CommunityParameter:BaseParameter
    {
        public Guid cid { get; set; }
        public int topictype { get; set; }
        public int flag { get; set; }
        public string content { get; set; }
        public string[] imgs { get; set; }
        public Guid to_uid { get; set; }
        public Guid commentId { get; set; }

        /// <summary>
        /// 评论列表的页数
        /// </summary>
        public int CommentPageIndex { get; set; }
        /// <summary>
        /// 评论列表的行数
        /// </summary>
        public int CommentPageSize { get; set; }
        /// <summary>
        /// 点赞人列表的页数
        /// </summary>
        public int praisesPageIndex { get; set; }
        /// <summary>
        /// 点赞人列表的行数
        /// </summary>
        public int praisesPageSize { get; set; }

        public int bizType { get; set; }

        /// <summary>
        /// 背景图
        /// </summary>
        public string backImg { get; set; }

        /// <summary>
        /// 排序规则
        /// </summary>
        public string strSort { get; set; }
    }
}