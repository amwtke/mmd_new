using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB
{
    [Serializable]
    [Table("M_WX_Authorization")]
    public class MerWXAuth//商铺微信授权列表
    {
        [Key]
        public Guid mid { get; set; }
        public string auth_list { get; set; }//逗号分隔的权限列表,为数字
        public bool is_valide { get; set; }
        public double last_update_time { get; set; }
    }
}
