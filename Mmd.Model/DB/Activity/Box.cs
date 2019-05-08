using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("act_box")]
    public class Box
    {
        [Key]
        public Guid bid { get; set; }
        public Guid mid { get; set; }
        public string appid { get; set; }
        public string title { get; set; }
        public string pic { get; set; }
        public string description { get; set; }
        public double time_start { get; set; }
        public double time_end { get; set; }
        public int status { get; set; }
        public double last_update_time { get; set; }
        [NotMapped]
        public List<BoxTreasure> BoxTreasureList { get; set; }
    }
}
