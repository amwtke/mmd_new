using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace MD.Model.Index.MD
{
    [ElasticType(Name = "order")]
    public class IndexOrder
    {
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "Id", Type = FieldType.String)]
        public string Id { get; set; }

        //数字+字母.
        //年月日时分秒+16位随机数
        //2016 01 01--01 01 01-12312432534546.共30位。
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "o_no", Type = FieldType.String)]
        public string o_no { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "mid", Type = FieldType.String)]
        public string mid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "goid", Type = FieldType.String)]
        public string goid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "gid", Type = FieldType.String)]
        public string gid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "waytoget", Type = FieldType.Integer)]
        public int? waytoget { get; set; }//提货方式,从Group属性继承

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "paytime", Type = FieldType.Double)]
        public double? paytime { get; set; }//支付时间,可以是形成订单的时间

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "order_price", Type = FieldType.Integer)]
        public int? order_price { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "actual_pay", Type = FieldType.Integer)]
        public int? actual_pay { get; set; }//实际付款额,如果有优惠券可能会变

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "post_price", Type = FieldType.Integer)]
        public int post_price { get; set; }//邮费

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "status", Type = FieldType.Integer)]
        public int? status { get; set; }

        /// <summary>
        /// 发起者uuid
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "buyer", Type = FieldType.String)]
        public string buyer { get; set; }//发起者id

        /// <summary>
        /// 用户的取货地址列表uuid
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "upid", Type = FieldType.String)]
        public string upid { get; set; }

        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "post_company", Type = FieldType.String)]
        public string post_company { get; set; }

        /// <summary>
        /// 运单号
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "post_number", Type = FieldType.String)]
        public string post_number { get; set; }//

        /// <summary>
        /// 默认的提货点uuid。原则上是可以在所有门店提货。
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "default_writeoff_point", Type = FieldType.String)]
        public string default_writeoff_point { get; set; }//

        /// <summary>
        /// 核销员uuid
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffer", Type = FieldType.String)]
        public string writeoffer { get; set; }//核销员uuid

        /// <summary>
        /// 核销时间
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "writeoffday", Type = FieldType.Double)]
        public double? writeoffday { get; set; }//核销时间

        /// <summary>
        /// 备注信息
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "extral_info", Type = FieldType.String)]
        public string extral_info { get; set; }//备注信息

        /// <summary>
        /// 收货人姓名
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "name", Type = FieldType.String)]
        public string name { get; set; }

        /// <summary>
        /// 收货人电话
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "cellphone", Type = FieldType.String)]
        public string cellphone { get; set; }

        /// <summary>
        /// 邮寄地址
        /// </summary>
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "postaddress", Type = FieldType.String)]
        public string postaddress { get; set; }

        //发货时间
        [ElasticProperty(Index = FieldIndexOption.NotAnalyzed, Name = "shipmenttime", Type = FieldType.Double)]
        public double? shipmenttime { get; set; }

        [ElasticProperty(Index = FieldIndexOption.Analyzed, Name = "KeyWords", Type = FieldType.String, Analyzer = "ik", IndexAnalyzer = "ik", SearchAnalyzer = "ik")]
        public string KeyWords { get; set; }
    }
}
