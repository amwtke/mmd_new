using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.MQ
{
    [Serializable]
    public class NoticeMQ
    {
        public long Id { get; set; }
        public Guid ReceiverUuid { get; set; }
        public Guid RelationUuid { get; set; }
        public long RelationId { get; set; }
        public int Status { get; set; }
        public object PayLoad { get; set; }
        public NoticeType MsgType { get; set; }
        /// <summary>
        /// Unixtime
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }


    public enum NoticeType
    {
        /// <summary>
        /// 联系人请求
        /// </summary>
        ContactRequest = 100,
        /// <summary>
        /// 点赞加一
        /// </summary>
        FavoriteAdd = 200,
        /// <summary>
        /// 访客踪迹
        /// </summary>
        VisitorAdd = 300,

    }
}
