using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("act_boxtreasure")]
    public class BoxTreasure
    {
        [Key]
        public Guid btid { get; set; }
        public Guid bid { get; set; }
        public string name { get; set; }
        public int count { get; set; }
        public int quota_count { get; set; }
        public string description { get; set; }
        public string pic { get; set; }
    }
}
