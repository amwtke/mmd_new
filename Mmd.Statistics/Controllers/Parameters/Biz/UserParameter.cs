using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mmd.Statistics.Controllers.Parameters.Biz
{
    public class UserParameter:BaseParameter
    {
        public string loginname { get; set; }
        public string pwd { get; set; }

        public string nickname { get; set; }

        public string tel { get; set; }
    }
}