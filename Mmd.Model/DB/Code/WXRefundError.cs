using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.DB.Code
{
    [Serializable]
    /*退款错误码表。
        SYSTEMERROR	接口返回错误	系统超时	请用相同参数再次调用API
        USER_ACCOUNT_ABNORMAL	退款请求失败	用户帐号异常或注销	此状态代表退款申请失败，商户可自行处理退款。
        INVALID_TRANSACTIONID	无效transaction_id	请求参数未按指引进行填写	请求参数错误，检查原交易号是否存在或发起支付交易接口返回失败
        PARAM_ERROR	参数错误	请求参数未按指引进行填写	请求参数错误，请重新检查再调用退款申请
        APPID_NOT_EXIST	APPID不存在	参数中缺少APPID	请检查APPID是否正确
        MCHID_NOT_EXIST	MCHID不存在	参数中缺少MCHID	请检查MCHID是否正确
        APPID_MCHID_NOT_MATCH	appid和mch_id不匹配	appid和mch_id不匹配	请确认appid和mch_id是否匹配
        REQUIRE_POST_METHOD	请使用post方法	未使用post传递参数 	请检查请求参数是否通过post方法提交
        SIGNERROR	签名错误	参数签名结果不正确	请检查签名参数和方法是否都符合签名算法要求
        XML_FORMAT_ERROR	XML格式错误	XML格式错误	请检查XML参数格式是否正确
        */
    [Table("Code_WXRefund_Error")]
    public class CodeWXRefundError
    {
        [Key]
        public string code { get; set; }
        public string description { get; set; }
    }
}
