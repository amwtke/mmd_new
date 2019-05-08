using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("act_usersign")]
    public class UserSign
    {
        [Key]
        public Guid usid { get; set; }
        public Guid uid { get; set; }
        public string openid { get; set; }
        public Guid sid { get; set; }
        public Guid mid { get; set; } 
        public int status { get; set; }
        public double signTime { get; set; }
        public double writeoffTime { get; set; }
        public Guid writeoffer { get; set; }
    }
}
