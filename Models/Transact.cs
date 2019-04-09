using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoLibrary;
using BrontoReference;
using BrontoTransactionalEndpoint.Controllers;

namespace BrontoTransactionalEndpoint.Models
{
    public class Transact
    {
        internal static string OrderConfirmation(Order order)
        {
            BrontoConnector.DeliveryType deliveryType = BrontoConnector.DeliveryType.transactional;

            if (order.SupplyNow == true)
            {
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("0bdb03eb0000000000000000000000106807", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else if (order.Department == "29")
            {
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("0bdb03eb00000000000000000000001068b3", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else if (order.Department == "27")
            {
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("9892cace237d4f0dc466deb63c84bce1", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else
            {
                return $"Invalid request. No email sent to {order.Email}";
            }
        }

        internal static string PasswordReset(Customer customer)
        {
            if (customer.IsPro == true)
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "0bdb03eb0000000000000000000000107043").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
            else
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "cef7902b45ddfecfc6ed14d9f4f714df").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        internal static string PasswordUpdate(Customer customer)
        {
            if (customer.IsPro == true)
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "0bdb03eb0000000000000000000000107052").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
            else
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "4fffc12ab5e0b56a7e57a0762570bda0").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        internal static string AccountElevation(Customer customer)
        {
            if (customer.IsNew == true)
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            } else if (customer.IsPro == true)
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            } else
            {
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        #region Helpers

        private static string EmailResult(writeResult brontoResult, Order order)
        {
            if (brontoResult.results[0].errorCode != 0)
            {
                string error = $"Email Failed for {order.Email}. Error Code: {brontoResult.results[0].errorCode}. Error String: {brontoResult.results[0].errorString}";
                return error;
            }
            else
            {
                string success = $"Success, Email Sent to {order.Email}";
                return success;
            }
        }
        private static string EmailResult(JObject brontoResult, Customer customer)
        {
            if ((int)brontoResult["errorCode"] != 0)
            {
                string error = $"Email Failed for {customer.Email}. Error Code: {(int)brontoResult["errorCode"]}. Error String: {(string)brontoResult["errorString"]}";
                return error;
            }
            else
            {
                string success = $"Success, Email Sent to {customer.Email}";
                return success;
            }
        }

        #endregion
    }
}
