using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MD.Model.Configuration.Att;

namespace MD.Model.Configuration.UI
{
    [MDConfig("UI","BackEnd")]
    public class UiBackEndConfig
    {
        [MDKey("PageSize")]
        public string PageSize { get; set; }
    }
}
