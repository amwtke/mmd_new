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
    [Table("AdvertiseArtide")]
    public class AdvertiseArtide//软文
    {
        [Key]
        public Guid aaid { get; set; }//软文编号
        public string content { get; set; }//软文内容
        public Guid last_update_user { get; set; }//最近编辑内容的用户
        public double timestamp { get; set; }//时间戳
    }
}
