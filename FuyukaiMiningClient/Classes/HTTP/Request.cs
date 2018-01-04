using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;

using FuyukaiMiningClient.Classes.Crypto;

namespace FuyukaiMiningClient.Classes.HTTP
{
    class Request
    {
        private string serverAddress;
        private static readonly HttpClient client = new HttpClient();

        private const string FUYUKAI_REQUEST = "763b8d6505789c7a9f3c9d0dec047ba8";
        private const string SERVER_PRIVATE_KEY = "8hCBriYA29v6ha67XRqM63rAn2kGahJK4";

        public Request(string serverAddress)
        {
            this.serverAddress = serverAddress;

            client.DefaultRequestHeaders
              .Accept
              .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("fuyukai-request", Request.FUYUKAI_REQUEST);
        }

        public async void SendData(string jsonData)
        {
            if (this.serverAddress != null && this.serverAddress.Length > 0)
            {
                string dateTimeString = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");

                // create post dict
                Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "payload", jsonData },
                { "datetime",  dateTimeString },
                { "signature", this.GenerateSignature(jsonData, dateTimeString) }
            };

                HttpContent content = new FormUrlEncodedContent(data);
                HttpResponseMessage response = await client.PostAsync(this.serverAddress, content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            else {
                Console.WriteLine(">>> WARNING: INVLAID SERVER URL, NO DATA SEND");
            }
        }

        private string GenerateSignature(string payload, string datetime)
        {
            return MD5Hash.Create(string.Concat(payload, datetime, Request.SERVER_PRIVATE_KEY));
        }
    }
}
