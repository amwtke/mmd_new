using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Configuration.DB
{
    public static class Helper
    {
        public static double ToUnixTime(System.DateTime time)
        {
            double intResult = 0;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            intResult = (time - startTime).TotalSeconds;
            return intResult;
        }

        public static double GetUnixTimeNow()
        {
            return ToUnixTime(DateTime.Now);
        }
    }
}
