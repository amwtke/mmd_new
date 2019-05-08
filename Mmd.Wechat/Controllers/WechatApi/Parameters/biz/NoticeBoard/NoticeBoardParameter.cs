using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MD.Model.DB;

namespace MD.Wechat.Controllers.WechatApi.Parameters.biz
{
    public class NoticeBoardParameter : BaseParameter
    {
        public Guid nid { get; set; }
        public int hits_count { get; set; }
        public int praise_count { get; set; }
        public int transmin_count { get; set; }
        /// <summary>
        /// 自定义取阅读过此文章用户的前几条
        /// </summary>
        public int readerSize { get; set; }
        public string comment { get; set; }
        public string extend_1 { get; set; }
        public string extend_2 { get; set; }
        public string extend_3 { get; set; }
        public int CommentPageIndex { get; set; }
        public int CommentPageSize { get; set; }
        public int praisesPageIndex { get; set; }
        public int praisesPageSize { get; set; }
        public int category { get; set; }
    }
}