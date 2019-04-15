﻿using System;
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
    //Templat IDs have their corresponding names in the Bronto Platform commented above them
    public class Transact
    {
        internal static string OrderConfirmation(Order order)
        {
            BrontoConnector.DeliveryType deliveryType = BrontoConnector.DeliveryType.transactional;

            if (order.SupplyNow == true)
            {
                //SUPPLYnow Order Confirmation
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("0bdb03eb0000000000000000000000106807", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else if (order.Department == "29")
            {
                //SUPPLY.com Order Confirmation - PRO
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("0bdb03eb00000000000000000000001068b3", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else if (order.Department == "27")
            {
                //SUPPLY.com Order Confirmation - D2C
                var brontoResult = BrontoConnector.SendOrderConfirmationEmail("9892cace237d4f0dc466deb63c84bce1", deliveryType, order).Result;
                var result = EmailResult(brontoResult, order);
                return result;
            }
            else
            {
                return $"Invalid request. Department likely not set. No email sent to {order.Email}";
            }
        }

        internal static string EstimateEmail(Estimate estimate)
        {
            if (estimate.Department == "29")
            {
                //Estimate - PRO
                var brontoResult = BrontoConnector.SendEstimateEmail(estimate, "0bdb03eb000000000000000000000010761b").Result;
                var result = EmailResult(brontoResult, estimate);
                return result;
            }
            else if (estimate.Department == "27")
            {
                //Estimate - D2C
                var brontoResult = BrontoConnector.SendEstimateEmail(estimate, "02e304b62399fb5ecd7a6a4325bfe4af").Result;
                var result = EmailResult(brontoResult, estimate);
                return result;
            }
            else
            {
                return $"Invalid request. Department likely not set. No email sent to {estimate.Email}";
            }
        }

        internal static string PasswordReset(Customer customer)
        {
            if (customer.IsPro == true)
            {
                //Password Reset - PRO
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "0bdb03eb0000000000000000000000107043").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
            else
            {
                //Password Reset - D2C
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "cef7902b45ddfecfc6ed14d9f4f714df").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        internal static string PasswordUpdate(Customer customer)
        {
            if (customer.IsPro == true)
            {
                //Password Update - PRO
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "0bdb03eb0000000000000000000000107052").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
            else
            {
                //Password Update - D2C
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "4fffc12ab5e0b56a7e57a0762570bda0").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            }
        }

        internal static string AccountElevation(Customer customer)
        {
            if (customer.IsNew == true)
            {
                //Account Elevation for Net New PRO Accounts
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "0bdb03eb0000000000000000000000106e3c").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            } else if (customer.IsPro == true)
            {
                //Account Elevation for Already Existing PRO
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "f78a836d0e658f688778c0dfd08a7f19").Result;
                var result = EmailResult(brontoResult, customer);
                return result;
            } else
            {
                //Account Eleavation for Already Existing D2C
                var brontoResult = BrontoConnector.SendAccountEmail(customer, "b904aa97f0a394372c697288bd30cef4").Result;
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

        #endregion
    }
}