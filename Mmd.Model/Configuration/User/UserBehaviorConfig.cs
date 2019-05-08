using MD.Model.Configuration.Att;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Configuration.User
{
    [MDConfig("User", "Behavior")]
    public class UserBehaviorConfig
    {
        [MDKey("LoginTimeSpanMin")]
        public string LoginTimeSpanMin { get; set; }

        [MDKey("GetMessageCount")]
        public string GetMessageCount { get; set; }
    }
}
