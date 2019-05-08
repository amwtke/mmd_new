using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MD.Lib.Log;

namespace MD.Lib.Util.Mail
{
    public static class QQEMailHelper
    {
        private const string SendAddress = "865729986@qq.com";
        private const string SendPassword = "ootvqrwflvvlbdea";

        private const string SmptHost = "smtp.qq.com";
        private static SmtpClient Client = null;

        static QQEMailHelper()
        {
            Client = new SmtpClient()
            {
                UseDefaultCredentials = true,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(SendAddress, SendPassword),
                Host = "smtp.qq.com"
            };
        }

        public static void SendMail(string topic, string receiveAddress,string body)
        {
            string mailTopic = topic;//主题
            string mailBody = body;//内容
            //发件人和收件人的邮箱地址
            MailMessage mmsg = new MailMessage(new MailAddress(SendAddress), new MailAddress(receiveAddress)); 
            mmsg.Subject = mailTopic;//邮件主题
            mmsg.SubjectEncoding = Encoding.UTF8;//主题编码
            mmsg.Body = mailBody;//邮件正文
            mmsg.BodyEncoding = Encoding.UTF8;//正文编码
            mmsg.IsBodyHtml = true;//设置为HTML格式 
            mmsg.Priority = MailPriority.High;//优先级

            Client.Send(mmsg);
        }

        public static async void SendMailAsync(string topic, string receiveAddress, string body)
        {
            string mailTopic = topic;//主题
            string mailBody = body;//内容
            //发件人和收件人的邮箱地址
            MailMessage mmsg = new MailMessage(new MailAddress(SendAddress), new MailAddress(receiveAddress));
            mmsg.Subject = mailTopic;//邮件主题
            mmsg.SubjectEncoding = Encoding.UTF8;//主题编码
            mmsg.Body = mailBody;//邮件正文
            mmsg.BodyEncoding = Encoding.UTF8;//正文编码
            mmsg.IsBodyHtml = true;//设置为HTML格式 
            mmsg.Priority = MailPriority.High;//优先级

            try
            {
                await Client.SendMailAsync(mmsg);
            }
            catch (Exception ee)
            {
                MDLogger.LogErrorAsync(typeof(QQEMailHelper), ee);
            }
        }
    }
}
