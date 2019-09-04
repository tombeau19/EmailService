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

            if (estimate.Department == "29")
            {
                var messageType = estimate.EstimateType == 1 ? OneWkToCloseMessageID :
                        estimate.EstimateType == 2 ? DayOfCloseMessageID :
                            estimate.EstimateType == 3 ? OneWkPastCloseMessageID :
                                estimate.EstimateType == 4 ? TwoWkToExpireMessageID :
                                    estimate.EstimateType == 5 ? OneWkToExpireMessageID :
                                        estimate.EstimateType == 6 ? DayAfterExpireMessageID : ProEstimateMessageID;

                var brontoResult = BrontoConnector.SendEstimateEmail(estimate, messageType).Result;
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo(messageType).Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#estimate_number%%", estimate.EstimateNumber), brontoResponse = ShippingEmailResult(brontoResult, estimate) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = ShippingEmailResult(brontoResult, estimate) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
            }
            else if (estimate.Department == "27" && estimate.EstimateType == 0)
            {
                //Estimate - D2C
                var brontoResult = BrontoConnector.SendEstimateEmail(estimate, "02e304b62399fb5ecd7a6a4325bfe4af").Result;
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo("02e304b62399fb5ecd7a6a4325bfe4af").Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#estimate_number%%", estimate.EstimateNumber), brontoResponse = ShippingEmailResult(brontoResult, estimate) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = ShippingEmailResult(brontoResult, estimate) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
            }
            else
            {
                var result = "D2C follow ups are turned off";
                return result;
            }
        }

        internal static string ShippingConfirmation(Order order)
        {
            var itemsLeftToShip = 0;
            var oneItemWithQtyOne = false;
            if (order.LineItems.Count() > 1 || order.LineItems[0].Quantity > 1)
            {
                foreach (var item in order.LineItems)
                {
                    if (item.Shipped == false && item.Quantity > 0 && item.ListSection == false)
                    {
                        itemsLeftToShip += 1;
                    }
                }
            }
            else
            {
                oneItemWithQtyOne = true;
                itemsLeftToShip = 1;
            }

            var entireOrderShipped = itemsLeftToShip == 0;

            if (order.Department == "29")
            {
                //2019.08| Shipping Confirmation | SUPPLY.com Post-Purchase PRO | Your Entire Order has Shipped!
                //SUPPLY.com Shipping Confirmation - PRO
                var messageId = entireOrderShipped ? "bae5ff316d97b84eeb6956918209f3ce" : oneItemWithQtyOne ? "6d6d6845555ed2af46b5f83459e10b8f" : "f3703ac72ea42b799b45cec77e8007c2";
                var brontoResult = BrontoConnector.SendShippingConfirmationEmail(order, messageId).Result;
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber), brontoResponse = ShippingEmailResult(brontoResult, order) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = ShippingEmailResult(brontoResult, order) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
            }
            else
            {
                //2019.08| Shipping Confirmation | SUPPLY.com Post-Purchase D2C | Your Entire Order has Shipped!
                //SUPPLY.com Shipping Confirmation - D2C
                var messageId = entireOrderShipped ? "79e3a8979188d86c4dafa26479a2f67e" : oneItemWithQtyOne ? "3192036580580fb2a830cc4052b1bcde" : "ed24176d6796a12b4b23514c932ec598";
                var brontoResult = BrontoConnector.SendShippingConfirmationEmail(order, messageId).Result;
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber), brontoResponse = ShippingEmailResult(brontoResult, order) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = ShippingEmailResult(brontoResult, order) };
                    JObject responseObj = JObject.FromObject(responseData);
                    return responseObj.ToString();
                }
            }
        }

        internal static string TriggerBrontoWorkflow(Customer customer)
        {
            var brontoResult = BrontoConnector.TriggerBrontoWorkflow(customer).Result;
            return brontoResult.ToString();
        }

        #region Helpers

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
