using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Redis.Objects.UserBehavior
{
    [RedisDBNumber("0")]
    public class NameCardRedis
    {
        [UnivZset("namecard.univ.zset","")]
        [DeptZset("namecard.dept.zset","")]
        [ResearchFieldZset("namecard.researchfield.zset","")]

        [NameCardCountZset("namecard.accesscount.zset","")]
        [NameCardPCountZset("namecard.p.count.zset", "")]
        [NameCardSCountZset("namecard.s.count.zset", "")]

        [UnivProfessorZset("namecard.univ.p.zset","")]
        [DeptProfessorZset("namecard.dep.p.zset","")]
        [ResearchFieldProfessorZset("namecard.rf.p.zset","")]
        [UnivStudentZset("namecard.univ.s.zset","")]
        [DeptStudentZset("namecard.dep.s.zset","")]
        [ResearchFieldStudentZset("namecard.rf.s.zset","")]

        [RedisKey]
        public string Useruuid { get; set; }
    }

    public class NameCardCountZsetAttribute:RedisZSetAttribute
    {
        public NameCardCountZsetAttribute(string name,string scoreFieldName) : base(name,scoreFieldName) { }
    }

    public class NameCardPCountZsetAttribute : RedisZSetAttribute
    {
        public NameCardPCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class NameCardSCountZsetAttribute : RedisZSetAttribute
    {
        public NameCardSCountZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class UnivZsetAttribute : RedisZSetAttribute
    {
        public UnivZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class DeptZsetAttribute : RedisZSetAttribute
    {
        public DeptZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
    public class ResearchFieldZsetAttribute : RedisZSetAttribute
    {
        public ResearchFieldZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class UnivProfessorZsetAttribute : RedisZSetAttribute
    {
        public UnivProfessorZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class DeptProfessorZsetAttribute : RedisZSetAttribute
    {
        public DeptProfessorZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
    public class ResearchFieldProfessorZsetAttribute : RedisZSetAttribute
    {
        public ResearchFieldProfessorZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class UnivStudentZsetAttribute : RedisZSetAttribute
    {
        public UnivStudentZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }

    public class DeptStudentZsetAttribute : RedisZSetAttribute
    {
        public DeptStudentZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
    public class ResearchFieldStudentZsetAttribute : RedisZSetAttribute
    {
        public ResearchFieldStudentZsetAttribute(string name, string scoreFieldName) : base(name, scoreFieldName) { }
    }
}
