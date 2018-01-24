using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;

using FuyukaiLib.Crypto;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace FuyukaiMiningClient.Classes.HTTP
{
    class Request
    {
        private static String PUB_KEY = "3082020A0282020100C98B6C694A" +
            "BD034EE2CDB626D96D8B25F66D6BFE9C0CA9A476D2EC3917FA9782FE" +
            "A074B75BC153A1BC06393F5DBC4069851954D1EEA3A5C7C3AD9DB7C2" +
            "CC9933C8FD45FB96572726F1AF62012DB08B21F91A203629301AA50B" +
            "BB8F53BEA94E19DF3AFD6A11797634D0488ADE8079A29AA49C5BD405" +
            "39CC87DC395AE0954186F6FBAA26CAC3A1842A0643D6508440F3A388" +
            "C5B53692ED740A7B7489D796380168BE0E8CE96E2978B6C2AC9174FA" +
            "9FC02F06DBD01FB66CB9CAE023D450FBC9943E5E775930D1F70D8575" +
            "95D1882F5A44AE3383DAFE4950491B8E6A07E99DAE75FE6DC6737AC3" +
            "448FD3BE93F0B286B7991CF43DFD9C4B30BC068FAF06C055612CED56" +
            "8E249007FCE11F7DD4DF079861FB5A15BCC46422B90219871BA3FADC" +
            "8A04BFCC389F4DD77273169041FEB4A9CCE345E4951D2BD1B3E6A47D" +
            "E9602A770D77062BCC76F1A6020F698736B16CE376682FC144D61F07" +
            "0A09E7B91F380D77FE034E5B2F28F8368DF59EED8F97B468611CB319" +
            "CA3A584EA499E9E95CC22BA53FC43B0D95F662CE285EA9747FD62905" + 
            "157D8849F7D4B156A712EB6A3DA72B45C997E142AED2DCBC184F5A3F" +
            "60C4468B057A145D0E528201B0A02A17B418605A78F2EE05E4667EA2" +
            "9B1CE9FC0739214B449D342303651D4493904BC9B66F8BF101BB1116" +
            "D8F01CCFC93303019C24C89E196BA68FD437A0EE0F4314C6EFA0E154" +
            "D63FAB0203010001";

        private string serverAddress;
        private static readonly HttpClient client = new HttpClient();
        private Telemetry del;

        private const string FUYUKAI_REQUEST = "763b8d6505789c7a9f3c9d0dec047ba8";
        private const string SERVER_PRIVATE_KEY = "8hCBriYA29v6ha67XRqM63rAn2kGahJK4";

        public Request(string serverAddress, Telemetry del)
        {
            ServicePointManager.ServerCertificateValidationCallback = Request.PinPublicKey;
            this.del = del;
            this.serverAddress = serverAddress;

            client.DefaultRequestHeaders
              .Accept
              .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            client.DefaultRequestHeaders.Add("User-Agent", "Fuyukai Telemetry Client (" + version + ")");
            client.DefaultRequestHeaders.Add("fuyukai-request", Request.FUYUKAI_REQUEST);
        }

        public async void SendData(string jsonData)
        {
            if (this.serverAddress != null && this.serverAddress.Length > 0)
            {
                string dateTimeString = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
                string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                // create post dict
                Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "payload", jsonData },
                { "version", version },
                { "datetime",  dateTimeString },
                { "signature", this.GenerateSignature(jsonData, dateTimeString) }
            };

                string responseString = "";
                HttpContent content = new FormUrlEncodedContent(data);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(this.serverAddress, content);
                    responseString = await response.Content.ReadAsStringAsync();
                }
                catch (Exception e) {
                    del.SendingError(this, e);
                    return;
                }

                del.SendingDone(this, responseString);
            }
            else {
                Program.WriteLine("WARNING: INVLAID SERVER URL, NO DATA SEND");
            }
        }

        private string GenerateSignature(string payload, string datetime)
        {
            return MD5Hash.Create(string.Concat(payload, datetime, Request.SERVER_PRIVATE_KEY));
        }

        public static bool PinPublicKey(object sender, X509Certificate certificate, X509Chain chain,
                                SslPolicyErrors sslPolicyErrors)
        {
            if (null == certificate)
                return false;

            String pk = certificate.GetPublicKeyString();
            if (pk.Equals(PUB_KEY))
                return true;

            return false;
        }
    }
}
