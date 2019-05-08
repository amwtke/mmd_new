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
    [Table("WriteOffer")]
    public class WriteOffer //核销员
    {
        [Key]
        public int? id { get; set; }

        public Guid uid { get; set; }
        public Guid woid { get; set; }

        public Guid mid { get; set; }
        public string openid { get; set; }
        public bool is_valid { get; set; }

        public double? timestamp { get; set; }
        public string realname { get; set; }
        public string phone { get; set; }
        public int commission { get; set; }
    }

    public class WriteOfferView //核销员
    {
        public int? id { get; set; }

        public Guid uid { get; set; }
        public Guid woid { get; set; }
        public string woname { get; set; }
        public Guid mid { get; set; }
        public string openid { get; set; }
        public bool? is_valid { get; set; }

        public double? timestamp { get; set; }
        public string realname { get; set; }
        public string phone { get; set; }
        public string nickName { get; set; }
        public int commission { get; set; }
        public int? skin { get; set; }
        public int? age { get; set; }
    }
}
