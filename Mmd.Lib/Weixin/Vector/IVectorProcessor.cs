using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Weixin.Vector
{
    public interface IVectorProcessor
    {
        bool IsVisible(Model.DB.Professional.Vector v);
        string GetVectorType();
        VectorView Parser(string expression);      //获取结果
        Task Route(Model.DB.Professional.Vector v);                        //路由到正确的地点。
        Model.DB.Professional.Vector GenVector(object obj);          //生成vector的(owner信息包含在expression中)
    }

    public class VectorView
    {
        public string Type { get; set; }
        public string Owner { get; set; }
        public string[] Objects { get; set; }
        public string[] Contents { get; set; }
        public double Timestamp { get; set; }
    }
}
