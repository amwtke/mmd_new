using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using MD.Model.Redis;

namespace MD.Lib.DB.Redis
{
    public static class RedisCommonHelper
    {
        const string FB = "|";
        const string FB_Mask = "$`fb`";

        const string FIB = ">";
        const string FIB_Mask = "$`ib`";

        const string NIL = "NIL";//字段值为空

        #region coding

        public static string EnCoding(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            return str.Replace(FB, FB_Mask).Replace(FIB, FIB_Mask);
        }

        public static string DeCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            return str.Replace(FB_Mask, FB).Replace(FIB_Mask, FIB);
        }

        public static string ObjectToString(object obj)
        {
            if (obj == null)
                return null;
            var ps = obj.GetType().GetProperties();
            StringBuilder sb = new StringBuilder();
            foreach (var p in ps)
            {
                var v_temp = p.GetValue(obj) ?? NIL;
                sb.Append(p.Name + FIB + EnCoding(v_temp.ToString()) + FB);
            }
            return sb.ToString();
        }

        public static T StringToObject<T>(string expression) where T : new()
        {
            if (string.IsNullOrEmpty(expression))
                return new T();

            T obj = new T();
            string[] fs = expression.Split(new[] {FB}, StringSplitOptions.RemoveEmptyEntries);
            if (fs.Length > 0)
            {
                foreach (var f in fs)
                {
                    var ifs = f.Split(new[] {FIB}, StringSplitOptions.RemoveEmptyEntries);
                    if (ifs.Length == 2)
                    {
                        var f_name = ifs[0];
                        var f_value = ifs[1];

                        f_value = f_value.Equals(NIL) ? null : DeCode(f_value);

                        var ps = obj.GetType().GetProperty(f_name);

                        if(ps.ToString().ToLower().Contains("guid"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Guid.Parse(f_value));
                        }
                        else if (ps.ToString().ToLower().Contains("double"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Convert.ToDouble(f_value));
                        }
                        else if (ps.ToString().ToLower().Contains("int"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Convert.ToInt32(f_value));
                        }
                        else if (ps.ToString().ToLower().Contains("float"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Convert.ToDouble(f_value));
                        }
                        else if (ps.ToString().ToLower().Contains("decimal"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Convert.ToDecimal(f_value));
                        }
                        else if (ps.ToString().ToLower().Contains("boolean"))
                        {
                            if (f_value != null) ps?.SetValue(obj, Convert.ToBoolean(f_value));
                        }
                        else
                            ps?.SetValue(obj,f_value);
                    }
                }
            }
            return obj;
        }
        #endregion
    }
}
