using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoLibrary;
using BrontoTransactionalEndpoint.Controllers;

namespace BrontoTransactionalEndpoint.Models
{
    public class Bronto
    {
        internal static string OrderConfirmation(Order order)
        {
            BrontoConnector.DeliveryType deliveryType = BrontoConnector.DeliveryType.transactional;

            if (order.SupplyNow == true)
            {
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("0bdb03eb0000000000000000000000106807", deliveryType, order).Result;

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
                return $"Invalid request. No email sent to {order.Email}";
            }
        }

        internal static string UpdateContact(JObject contact)
        {
            JObject brontoResult = BrontoConnector.UpdateContact(contact).Result;

            if ((bool)brontoResult["isError"] == true)
            {
                return $"UpdateContact failed for {(string)contact["Email"]}. ErrorCode: {(int)brontoResult["errorCode"]}. ErrorString: {(string)brontoResult["errorString"]}.";
            }
            else
            {
                return $"{(string)contact["Email"]} successfully updated";
            }
        }
    }
}
