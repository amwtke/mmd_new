using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;

namespace MD.Lib.Exceptions.Pay
{
    public class PrePayDuplicatedException :Exception
    {
        public PrePayDuplicatedException(Type t, string message) : base(message)
        {
            MDLogger.LogErrorAsync(t,this);
        }

        public PrePayDuplicatedException()
        {
            
        }
    }
}
