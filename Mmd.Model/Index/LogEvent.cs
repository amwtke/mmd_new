using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Index
{
    public enum LogLevel
    {
        Info,
        Error,
        Warning,
        Debug
    }
    [ElasticType(Name = "event")]
    public class LogEvent
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "TimeStamp", Type = FieldType.Date)]
        public string TimeStamp { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Message", Analyzer = "ik", Type = FieldType.String)]
        public string Message { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "MessageObject", Analyzer = "ik", Type = FieldType.String)]
        public string MessageObject { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "Exception", Analyzer = "ik", Type = FieldType.String)]
        public String Exception { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "LoggerName", Type = FieldType.String)]
        public string LoggerName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Domain", Type = FieldType.String)]
        public string Domain { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Level", Type = FieldType.String)]
        public string Level { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "ClassName", Type = FieldType.String)]
        public string ClassName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "FileName", Type = FieldType.String)]
        public string FileName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Name", Type = FieldType.String)]
        public string Name { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "FullInfo", Type = FieldType.String)]
        public string FullInfo { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "MethodName", Type = FieldType.String)]
        public string MethodName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Os", Type = FieldType.String)]
        public string Os { get; set; }
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Properties", Type = FieldType.String)]
        public string Properties { get; set; }
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "UserName", Type = FieldType.String)]
        public string UserName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "ThreadName", Type = FieldType.String)]
        public string ThreadName { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "HostName", Type = FieldType.String)]
        public string HostName { get; set; }
    }
}
