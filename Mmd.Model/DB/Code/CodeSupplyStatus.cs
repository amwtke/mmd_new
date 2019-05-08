using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    public class CodeSupplyStatus
    {
     
    }
    public enum ESupplyStatus
    {
        已下线 = 0,
        已上线 = 1,
        已删除=2
    }
}
