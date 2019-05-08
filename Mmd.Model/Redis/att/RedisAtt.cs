using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis
{
    public enum RedisSpecialStrings
    {
        EVERY_KEY,
    }
    public enum ListPush
    {
        Left,
        Right,
    }
    public class RedisBaseAttribute : Attribute
    {
        public string Name { get; set; }
        public RedisBaseAttribute(string name)
        {
            Name = name;
        }
        public RedisBaseAttribute() { }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class RedisHashAttribute : RedisBaseAttribute
    {
        public RedisHashAttribute(string name) : base(name)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RedisDBNumberAttribute : Attribute
    {
        public RedisDBNumberAttribute(string no)
        {
            DBNumber = no;
        }
        public string DBNumber { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RedisSetAttribute : RedisBaseAttribute
    {
        public RedisSetAttribute(string name) : base(name)
        {

        }
    }

    /// <summary>
    /// scoreFieldName字段为对象中为此key赋值score的字段名称。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisZSetAttribute : RedisBaseAttribute
    {
        public RedisZSetAttribute(string name, string scoreFieldName) : base(name)
        {
            ScoreFieldName = scoreFieldName;
        }
        public string ScoreFieldName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RedisListAttribute : RedisBaseAttribute
    {
        public RedisListAttribute(string name, ListPush push) : base(name)
        {
            Push = push;
        }
        public ListPush Push { get; set; }

    }

    public class RedisHashEntryAttribute : RedisBaseAttribute
    {
        public RedisHashEntryAttribute(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// Key是不会存到Redis-Hash里的。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RedisStringAttribute : RedisBaseAttribute
    {
        public RedisStringAttribute(string name) : base(name)
        {

        }
    }
}
