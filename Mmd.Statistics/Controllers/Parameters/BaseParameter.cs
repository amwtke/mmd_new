using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mmd.Statistics.Controllers.Parameters
{
    public class BaseParameter
    {
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        /// <summary>
        /// 查询字符串
        /// </summary>
        public string queryStr { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime from { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime to { get; set; }

        public string orderBy { get; set; }

        public bool isAsc { get; set; }
    }
}