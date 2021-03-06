using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BrontoLibrary.Models;
using BrontoLibrary;
using Newtonsoft.Json.Linq;
using System.Net;

namespace BrontoTransactionalEndpoint.Controllers
{
    public static class NetsuiteController
    {
        public enum MessageType
        {
            OrderConfirmation = 1,
            ShippingConfirmation,
            EstimateEmail,
            EstimateFollowUp,
            AlbertAndPRORegistration,
            PasswordReset,
            PasswordUpdate,
            ProtectionExpiration,
            HouzzReviewRequest,
            PromoRep,
            PromoCompany,
            PROWelcomeAndOnboarding
        };

        public static string CreateBrontoMessageRecord(Order order, string messageId, MessageType messageType)
        {
            string url = "https://634494.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1645&deploy=1&compid=634494&h=e1e59bf71af6447a3e9f";

            string subjectLine;
            try
            {
                var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                subjectLine = (string)messageInfo["subjectLine"];
            }
            catch
            {
                subjectLine = "Error Setting Subject";
            }

            var parameters = new
            {
                email = order.Email,
                confirmationNumber = order.OrderNumber,
                repMessage = "",
                subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber),
                sendMessage = false,
                messageType = (int)messageType,
                estimateType = false,
                source = 2,
                brontoResponse = $"Success, Email Sent to {order.Email}",
                messageContent = JObject.FromObject(order).ToString().Replace("\r\n", "")
            };

            JObject brontoMessageRecord = JObject.FromObject(parameters);
            string result = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new MyWebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                result = client.UploadString(url, brontoMessageRecord.ToString());
            }

            return string.IsNullOrEmpty(result) ? "" : result;
        }

        public static string CreateBrontoMessageRecord(Customer customer, string messageId, MessageType messageType)
        {
            string url = "https://634494.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1645&deploy=1&compid=634494&h=e1e59bf71af6447a3e9f";

            string subjectLine;
            try
            {
                var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                subjectLine = (string)messageInfo["subjectLine"];
            }
            catch
            {
                subjectLine = "Error Setting Subject";
            }

            var parameters = new
            {
                email = customer.Email,
                repMessage = "",
                subject = subjectLine.Replace("%%#first_name%%", !string.IsNullOrEmpty(customer.FirstName) ? Capitalize(customer.FirstName) : ""),
                sendMessage = false,
                confirmationNumber = false,
                messageType = (int)messageType,
                estimateType = false,
                source = 2,
                brontoResponse = $"Success, Email Sent to {customer.Email}",
                messageContent = JObject.FromObject(customer).ToString().Replace("\r\n", "")
            };

            JObject brontoMessageRecord = JObject.FromObject(parameters);
            string result = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new MyWebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                result = client.UploadString(url, brontoMessageRecord.ToString());
            }

            return string.IsNullOrEmpty(result) ? "" : result;
        }

        private static string Capitalize(string name)
        {
            var trimmedName = name.Trim();
            var words = trimmedName.ToLower().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }
    }

    class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 900000;
            return w;
        }
    }
}