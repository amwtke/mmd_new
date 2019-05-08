using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;

namespace MD.Lib.Util
{
    public class WatchStopper
    {
        string name;
        string sign;
        private Stopwatch Watch;
        private Type type;
        public WatchStopper(Type t,string swname = "WatchStopper")
        {
            name = swname;
            sign = Guid.NewGuid().ToString();
            type = t;
            Watch = new Stopwatch();
        }

        public void Start(string Newname="")
        {
            if (!string.IsNullOrEmpty(Newname))
                name = Newname;
            Watch.Start();
        }

        public void Stop()
        {
            Watch.Stop();
            MDLogger.LogInfoAsync(typeof(Type), $"！sign:{sign}，{name}耗时：{Watch.Elapsed.TotalSeconds} 秒！");
        }

        public void Restart(string Newname = "")
        {
            if (!string.IsNullOrEmpty(Newname))
                name = Newname;

            Watch.Restart();
        }
    }
}
