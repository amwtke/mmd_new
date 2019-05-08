using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "ComplexLocation")]
    public class ComplexLocation
    {
        [ElasticProperty(Name = "TimeStamp", Index = FieldIndexOption.NotAnalyzed)]
        public string TimeStamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed,Name = "Id")]
        public string Id { get; set; }

        [ElasticProperty(Name = "Coordinate",Type = FieldType.GeoPoint)]
        public ComplexLocationCoordinate Coordinate { get; set; }

        [ElasticProperty(Name = "IsBusiness", Index = FieldIndexOption.NotAnalyzed)]
        public int IsBusiness { get; set; }
        [ElasticProperty(Name = "Gender", Index = FieldIndexOption.NotAnalyzed)]
        public int Gender { get; set; }
        [ElasticProperty(Name = "ResearchFieldId", Index = FieldIndexOption.NotAnalyzed)]
        public long ResearchFieldId { get; set; }
    }

    public class ComplexLocationCoordinate
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
