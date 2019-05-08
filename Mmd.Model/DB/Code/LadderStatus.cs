using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    public class LadderStatus
    {
       
    }
    public enum ELadderGroupStatus
    {
        已发布 = 0,
        已删除 = 1,
        待发布 = 2,
        已过期 = 3,
        已结束 = 4,
    }
    public enum ELadderGroupOrderStatus
    {
        拼团进行中 = 0,
        拼团成功 = 1,
    }
    public enum ELadderOrderStatus
    {
        已支付 = 2,
        已成团未提货 = 5,
        拼团成功 = 6,
    }
}
