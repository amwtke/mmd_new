using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.Vector
{
    public static class VectorCommonHelper
    {
        public const string PS = "%";
        public const string PS_Mask = "#ps#";

        public const string IPS = ":";
        public const string IPS_Mask = "#ips#";

        public const string Empty = "Null";

        public static string Encode(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return null;
            return expression.Replace(PS, PS_Mask).Replace(IPS, IPS_Mask);
        }

        public static string Decode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            return str.Replace(PS_Mask, PS).Replace(IPS_Mask, IPS);
        }
    }
}
