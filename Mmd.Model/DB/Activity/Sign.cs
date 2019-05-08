using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Activity
{
    [Serializable]
    [Table("act_sign")]
    public class Sign
    {
        [Key]
        public Guid sid { get; set; }
        public Guid mid { get; set; }
        public string appid { get; set; }
        public double timeStart { get; set; }
        public double timeEnd { get; set; }
        public string awardName { get; set; }
        public string awardDescription { get; set; }
        public int awardCount { get; set; }
        public int awardQuatoCount { get; set; }
        public string awardPic { get; set; }
        public int mustSignCount { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public int status { get; set; }
        public double last_update_time { get; set; }
    }
}
