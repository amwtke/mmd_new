using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    [ElasticType(Name = "biz")]
    public class BizIndex
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "TimeStamp", Type = FieldType.Date)]
        public string TimeStamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UserName", Type = FieldType.String)]
        public string UserName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "LoggerName", Type = FieldType.String)]
        public string LoggerName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UserEmail", Type = FieldType.String)]
        public String UserEmail { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "FromUrl", Type = FieldType.String)]
        public string FromUrl { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "NowUrl", Type = FieldType.String)]
        public string NowUrl { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UserIp", Type = FieldType.String)]
        public string UserIp { get; set; }

        /// <summary>
        /// 功能模块的名称
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "ModelName", Type = FieldType.String)]
        public string ModelName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "SessionId", Type = FieldType.String)]
        public string SessionId { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "OpenId", Type = FieldType.String)]
        public string OpenId { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Message", Analyzer = "ik", Type = FieldType.String)]
        public string Message { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UserUuid", Type = FieldType.String)]
        public string UserUuid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Platform", Type = FieldType.String)]
        public string Platform { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UnUsed1", Type = FieldType.String)]
        public string UnUsed1 { get; set; }
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UnUsed2", Type = FieldType.String)]
        public string UnUsed2 { get; set; }
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UnUsed3", Type = FieldType.String)]
        public string UnUsed3 { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "HostName", Type = FieldType.String)]
        public string HostName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Location", Type = FieldType.GeoPoint)]
        public Coordinate Location { get; set; }
    }
}
