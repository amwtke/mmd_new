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
    [Table("WriteOffPoint")]
    public class WriteOffPoint
    {
        [Key]
        public Guid woid { get; set; }
        public Guid mid { get; set; }

        [Required(ErrorMessage = "地址必填！")]
        public string address { get; set; }

        [Required(ErrorMessage = "联系方式必填！")]
        public string tel { get; set; }
        public long? cell_phone { get; set; }
        public string contact_person { get; set; }
        /// <summary>
        /// 空代表是总店
        /// </summary>
        public Guid parent { get; set; }//空代表总店
        /// <summary>
        /// 店铺的名称
        /// </summary>
        [Required(ErrorMessage = "店名必填！")]
        public string name { get; set; }

        public bool? is_valid { get; set; }
        //经度
        public double longitude { get; set; }
        //纬度
        public double latitude { get; set; }
        public double? timestamp { get; set; }
        [NotMapped]
        public List<WriteOfferView> listWriteOffer { get; set; }
    }
}
