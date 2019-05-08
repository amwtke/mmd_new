using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Address
{
    [Serializable]
    [Table("Code_Province")]
    public class Province
    {
        [Key]
        public int id { get; set; }

        public string province { get; set; }

        public int code { get; set; }
    }

    [Table("Code_City")]
    public class City
    {
        [Key]
        public int id { get; set; }

        public int code { get; set; }
        public string city { get; set; }

        public int province_id { get; set; }
    }

    [Table("Code_District")]
    public class District
    {
        [Key]
        public int id { get; set; }

        public int code { get; set; }
        public string district { get; set; }

        public int city_id { get; set; }
    }
}
