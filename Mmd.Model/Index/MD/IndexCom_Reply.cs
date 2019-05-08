using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    public class IndexCom_Reply
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 回复类型(因为回复可以是针对评论的回复(comment)，也可以是针对回复的回复(reply)， 通过这个字段来区分两种情景)
        /// </summary>
        public string reply_type { get; set; }

        /// <summary>
        /// 回复目标id(如果reply_type是comment的话，那么reply_id＝commit_id，如果reply_type是reply的话，这表示这条回复的父回复)
        /// </summary>
        public string reply_id { get; set; }

        /// <summary>
        /// 回复内容
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// 回复用户id
        /// </summary>
        public string from_uid { get; set; }

        /// <summary>
        /// 目标用户id
        /// </summary>
        public string to_uid { get; set; }

        /// <summary>
        /// 回复时间
        /// </summary>
        
        public int timestamp { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
       
        public int status { get; set; }
    }

    public enum EReplyType
    {
        /// <summary>
        /// 针对评论的回复
        /// </summary>
        comment,
        /// <summary>
        /// 针对回复的回复
        /// </summary>
        reply
    }
}
