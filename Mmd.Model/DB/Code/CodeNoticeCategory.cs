using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MD.Model.DB.Code
{
    [Serializable]
    [Table("code_notice_category")]
    public class CodeNoticeCategory
    {
        [Key]
        public int code { get; set; }
        public string name { get; set; }
        public int sortid { get; set; }
    }
}
