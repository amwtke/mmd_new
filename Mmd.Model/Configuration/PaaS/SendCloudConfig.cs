using MD.Model.Configuration.Att;
namespace MD.Model.Configuration.PaaS
{
    [MDConfig("PaaS", "SendCloud")]
    public class SendCloudConfig : IConfigModel
    {
        [MDKey("SMS_API_User")]
        public string SMS_API_User { get; set; }

        [MDKey("SMS_API_Key")]
        public string SMS_API_Key { get; set; }

        [MDKey("EDM_API_User")]
        public string EDM_API_User { get; set; }

        [MDKey("SVR_API_User")]
        public string SVR_API_User { get; set; }

        [MDKey("API_Key")]
        public string API_Key { get; set; }



        [MDKey("ResetPasswordValidation_EmailTempleteId")]
        public string ResetPasswordValidation_EmailTempleteId { get; set; }


        [MDKey("RegisterValidation_TempleteId")]
        public string RegisterValidation_TempleteId { get; set; }

        public void Init()
        {
        }
    }
}
