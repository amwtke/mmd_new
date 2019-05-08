using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Util.MDException
{
    public class MDException : Exception
    {
        public MDException(string message,Exception ex) : base(message,ex)
        {
            
        }

        public MDException(Type t,Exception ex)
        {
            MD.Lib.Log.MDLogger.LogErrorAsync(t,ex);
        }

        public MDException(Type t,string message) : base(message)
        {
            MD.Lib.Log.MDLogger.LogErrorAsync(t, this);
        }
    }
}
