using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Helper;

namespace BrontoTransactionalEndpoint.Controllers
{
    public static class TeamsHelper
    {
        public static async Task SendError(string title, string text)
        {
            string webhookUrl = "https://outlook.office.com/webhook/2e3cbfd5-55cb-4a1b-a2fd-c683dbffd345@3c2f8435-994c-4552-8fe8-2aec2d0822e4/IncomingWebhook/1e1761ecf1c546d59bdc40ba7b8442b9/35aa24e2-d8c8-48b7-8dbe-c577c684ca90";

            string cardJson = @"{
                ""@context"":""https://schema.org/extensions"",
                ""@type"":""MessageCard"",
                ""themeColor"":""FF0000"",
                ""title"":'"+title+@"',
                ""text"":'"+text+@"' 
            }";

            await PostCardAsync(webhookUrl, cardJson);
        }

        private static async Task PostCardAsync(string webhook, string cardJson)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var content = new StringContent(cardJson, System.Text.Encoding.UTF8, "application/json");
            using (var response = await client.PostAsync(webhook, content))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Helper.Email.SendEmail("Failed error post to Teams", "BRONTO: Error Failed to Post to Teams", "t.beauregard@hmwallace.com");
                }
            }
        }
        
    }
}
