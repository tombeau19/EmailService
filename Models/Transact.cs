using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoLibrary;
using BrontoReference;
using Serilog;
using BrontoTransactionalEndpoint.Controllers;

namespace BrontoTransactionalEndpoint.Models
{
    //Templat IDs have their corresponding names in the Bronto Platform commented above them
    public class Transact
    {
        internal static string EstimateEmail(Estimate estimate)
        {
            //Bronto Templates
            string ProEstimateMessageID = "0bdb03eb000000000000000000000010761b";
            string OneWkToCloseMessageID = "c7fe3060fccfab3446bc9c4bc0ad95ce";
            string DayOfCloseMessageID = "6daefecf836cd65fdf3640d1598fee54";
            string OneWkPastCloseMessageID = "187b70784f747adf1ea5763d9e5b8440";
            string TwoWkToExpireMessageID = "d686d25c8ca178ecdaa95bcfb823ea4d";
            string OneWkToExpireMessageID = "832c37f898bc5f706b207f6f0cbb054a";
            string DayAfterExpireMessageID = "5aba812ac3c3433f65c0f13b306e36ec";

            //Email Subject Lines - Set in NETSUITE BrontoEstFollowUpTriggerWFA.js
            string ProEstimateSubject = "SUPPLY.com Estimate";
            string OneWkToCloseSubject = "Status of";
            string DayOfCloseSubject = "Checking in on your estimate";
            string OneWkPastCloseSubject = "Estimate status for";
            string TwoWkToExpireSubject = "is expiring soon!";
            string OneWkToExpireSubject = "You have an expiring estimate";
            string DayAfterExpireSubject = "Requesting Feedback";

            if (estimate.Department == "29")
            {
                var messageType = estimate.Subject.Contains(ProEstimateSubject) ? ProEstimateMessageID :
                    estimate.Subject.Contains(OneWkToCloseSubject) ? OneWkToCloseMessageID :
                        estimate.Subject.Contains(DayOfCloseSubject) ? DayOfCloseMessageID :
                            estimate.Subject.Contains(OneWkPastCloseSubject) ? OneWkPastCloseMessageID :
                                estimate.Subject.Contains(TwoWkToExpireSubject) ? TwoWkToExpireMessageID :
                                    estimate.Subject.Contains(OneWkToExpireSubject) ? OneWkToExpireMessageID :
                                        estimate.Subject.Contains(DayAfterExpireSubject) ? DayAfterExpireMessageID : "";

                if (messageType == "")
                {
                    //TODO throw error
                    return $"Estimate Email Failed to send {estimate.Email}";
                }
                else
                {
                    var brontoResult = BrontoConnector.SendEstimateEmail(estimate, messageType).Result;
                    var result = EstimateEmailResult(brontoResult, estimate);
                    return result;
                }
            }
            else if (estimate.Department == "27" && estimate.Subject.Contains("SUPPLY.com"))
            {
                //Estimate - D2C
                var brontoResult = BrontoConnector.SendEstimateEmail(estimate, "02e304b62399fb5ecd7a6a4325bfe4af").Result;
                var result = EstimateEmailResult(brontoResult, estimate);
                return result;
            }
            else
            {
                var result = "D2C follow ups are turned off";
                return result;
            }
        }

        internal static string ShippingConfirmation(Order order)
        {
            if (order.Department == "29")
            {
                //SUPPLY.com Shipping Confirmation - PRO
                var brontoResult = BrontoConnector.SendShippingConfirmationEmail(order, "f3703ac72ea42b799b45cec77e8007c2").Result;
                var result = ShippingEmailResult(brontoResult, order);
                return result;
            }
            else
            {
                //SUPPLY.com Shipping Confirmation - PRO
                var brontoResult = BrontoConnector.SendShippingConfirmationEmail(order, "ed24176d6796a12b4b23514c932ec598").Result;
                var result = ShippingEmailResult(brontoResult, order);
                return result;
            }
        }

        internal static string Promo(Customer customer)
        {
            if (customer.IsNew == true)
            {
                //PAM - Albert - Net New PRO
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "72911a76cfa01d1225044d0d400053da").Result;
                var result = EmailResult(brontoResult, customer); 
                return result;
            }
            else if (customer.IsPro == true)
            {
                //PAM - Albert - Existing PRO
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "028db216dfc93bd8901a626081a8f6f5").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
            else
            {
                //PAM - Albert - Existing D2C
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "2e6670754405f6456860e1a45a0fa79f").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        internal static string TriggerBrontoWorkflow(Customer customer)
        {
            var brontoResult = BrontoConnector.TriggerBrontoWorkflow(customer).Result;
            return brontoResult.ToString();
        }

        #region Helpers

        private static string EmailResult(JObject brontoResult, Estimate estimate)
        {
            if ((int)brontoResult["errorCode"] != 0)
            {
                string error = $"Email Failed for {estimate.Email}. Error Code: {(int)brontoResult["errorCode"]}. Error String: {(string)brontoResult["errorString"]}";
                return error;
            }
            else
            {
                string success = $"Success, Email Sent to {estimate.Email}";
                return success;
            }
        }
        private static string ShippingEmailResult(JObject brontoResult, Order order)
        {
            if ((int)brontoResult["errorCode"] != 0)
            {
                string error = $"Email Failed for {order.Email}. Error Code: {(int)brontoResult["errorCode"]}. Error String: {(string)brontoResult["errorString"]}";
                return error;
            }
            else
            {
                string success = $"Success, Email Sent to {order.Email}";
                return success;
            }
        }
        private static string EstimateEmailResult(JObject brontoResult, Estimate estimate)
        {
            if ((int)brontoResult["errorCode"] != 0)
            {
                string error = $"Email Failed for {estimate.Email}. Error Code: {(int)brontoResult["errorCode"]}. Error String: {(string)brontoResult["errorString"]}";
                return error;
            }
            else
            {
                string success = $"Success, Email Sent to {estimate.Email}";
                return success;
            }
        }

        #endregion
    }
}
