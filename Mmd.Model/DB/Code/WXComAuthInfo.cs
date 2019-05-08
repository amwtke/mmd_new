using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    /*商铺微信授权列表。*/
    [Table("Code_WXComponent_AuthorizInfo")]
    public class CodeWXComAuthInfo
    {
        [Key]
        public int id { get; set; }
        public int code { get; set; }
        public string description { get; set; }
    }
}
