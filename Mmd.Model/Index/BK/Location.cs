using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "Location")]
    public class Location
    {
        [ElasticProperty(Name = "TimeStamp", Index = FieldIndexOption.NotAnalyzed)]
        public string TimeStamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed,Name = "Id")]
        public string Id { get; set; }

        [ElasticProperty(Name = "Coordinate",Type = FieldType.GeoPoint)]
        public Coordinate Coordinate { get; set; }
    }

    [Serializable]
    public class Coordinate
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
