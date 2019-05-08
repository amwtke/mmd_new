using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("act_usertreasure")]
    public class UserTreasure
    {
        [Key]
        public Guid utid { get; set; }
        public Guid uid { get; set; }
        public Guid mid { get; set; }
        public string openid { get; set; }
        public Guid btid { get; set; }
        public Guid bid { get; set; }
        public int status { get; set; }
        public double open_time { get; set; }
        public double writeofftime { get; set; }
        public Guid writeoffer { get; set; }
    }
}
