using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;

namespace BrontoTransactionalEndpoint.Models
{
    public class Bronto
    {
        internal static string OrderConfirmation(string orderString)
        {
            JObject orderJson = JObject.Parse(orderString);
            Order order = new Order(orderJson);

            if (order.SupplyNow == true)
            {
                var brontoResult = BrontoLibrary.BrontoConnector.SendOrderConfirmationEmail("0bdb03eb0000000000000000000000106807", "test", order).Result;

                if (brontoResult.results[0].errorCode != 0)
                {
                    string error = $"Email Failed. Error Code: {brontoResult.results[0].errorCode}. Error String: {brontoResult.results[0].errorString}";
                    return error;
                }
                else
                {
                    string success = $"Success, Email Sent to {order.Email}";
                    return success;
                }
            }
            else if (order.Department == "29")
            {
                return "Pro email not set up yet";
            }
            else if (order.Department == "27")
            {
                return "D2C email not set up yet";
            }
            else
            {
                return "invalid values";
            }
        }
    }
}
