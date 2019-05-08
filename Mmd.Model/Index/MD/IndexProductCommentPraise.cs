﻿using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "productcommentpraise")]
    public class IndexProductCommentPraise
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "pcid", Type = FieldType.String)]
        public string pcid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "uid", Type = FieldType.String)]
        public string uid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "timestamp", Type = FieldType.Double)]
        public double timestamp { get; set; }
    }
}
