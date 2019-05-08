using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    public enum EBoxStatus
    {
        待发布 = 0,
        已上线 = 1,
        已下线 = 2
    }

    public enum EUserTreasureStatus
    {
        未核销 = 0,
        已核销 = 1
    }

    public enum ESignStatus
    {
        待发布 = 0,
        已上线 = 1,
        已下线 = 2
    }

    public enum EUserSignStatus
    {
        已签到未领取 = 0,
        已领取 = 1
    }
}
