﻿using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using BrontoLibrary;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoReference;
using Microsoft.Extensions.Logging;
using BrontoTransactionalEndpoint.Controllers;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactController : ControllerBase
    {
        private readonly ILogger<TransactController> _logger;
        public TransactController(ILogger<TransactController> logger)
        {
            _logger = logger;
        }

        #region Message IDs
        private readonly string[] NewCustomerAlbertMessageID = { 
            /*Albert Account Elevation for Net New PRO Accounts*/ "0bdb03eb0000000000000000000000106e3c", 
            /*Albert Net New PRO - Subject Line Name Test Email*/ "7fdb4a27155056afda4e539dd26aced1",
            /*Albert New PRO - Reduced Content NO Name Subject Line*/ "07a8b006116f0ae96a54042e936cba1f",
            /*Albert New PRO - Reduced Content + Name Subject Line*/ "eb06064ddafc735abd24f49de71c0c71"
        };
        private readonly string[] ProCustomerAlbertMessageID = { 
            /*Albert Account Elevation for Already Existing PRO*/ "f78a836d0e658f688778c0dfd08a7f19", 
            /*Albert Existing PRO - Subject Line Name Test Email*/ "98818ea203596ba32372b32ba6a5f87e",
            /*Albert Existing PRO - Reduced Content + Name Subject Line*/ "4a11ba0af5e44b261d708dcb62690aee",
            /*Albert Existing PRO - Reduced Content NO Name Subject Line*/ "e9341e16adb3079ca04f27772b88ea5b"
        };
        private readonly string[] D2CCustomerAlbertMessageID = { 
            /*Albert Account Elevation for Already Existing D2C*/ "b904aa97f0a394372c697288bd30cef4", 
            /*Albert Existing D2C - Subject Line Name Test Email*/ "9a173e6082c847544e552e9de3deb5c6",
            /*Albert Existing D2C - Reduced Content NO Name Subject Line*/ "fd39e615680927a6f3e18e9fc27706d4",
            /*Albert Existing D2C - Reduced Content + Name Subject Line*/ "2fc1cd9ce17e5ccdbadec1cdfeb49778"
        };
        private readonly string ProWelcomeMessageID = "59df810343334dde290123cc9a477f0b";
        private readonly string ProPasswordResetMessageID = "0bdb03eb0000000000000000000000107043";
        private readonly string D2CPasswordResetMessageID = "cef7902b45ddfecfc6ed14d9f4f714df";
        private readonly string ProPasswordUpdateMessageID = "0bdb03eb0000000000000000000000107052";
        private readonly string D2CPasswordUpdateMessageID = "4fffc12ab5e0b56a7e57a0762570bda0";
        private readonly string ProOrderConfirmationMessageID = "0bdb03eb00000000000000000000001068b3";
        private readonly string D2COrderConfirmationMessageID = "9892cace237d4f0dc466deb63c84bce1";
        private readonly string ProOrderConfirmationMessageIDNoLeadTime = "0b39302354461c9bee7ff0b653c130a3";
        private readonly string D2COrderConfirmationMessageIDNoLeadTime = "d9d916fef652b2f4c91654e79156bc45";
        private readonly string SUPPLYnowOrderConfirmationMessageID = "0bdb03eb0000000000000000000000106807";
        #endregion

        /// <summary>
        /// Sends an Order Confirmation Email. The template used is based on the order being SUPPLYnow(bool), Pro(Department == "29"), or D2C(Department == "27").
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="order">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("OrderConfirmation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> OrderConfirmation(Order order)
        {
            BrontoConnector.DeliveryType deliveryType = BrontoConnector.DeliveryType.transactional;
            var messageType = order.SupplyNow ? SUPPLYnowOrderConfirmationMessageID : order.Department == "29" ? ProOrderConfirmationMessageIDNoLeadTime : D2COrderConfirmationMessageIDNoLeadTime;

            writeResult brontoResult = new writeResult();

            try
            {
                brontoResult = await BrontoConnector.SendOrderConfirmationEmail(messageType, deliveryType, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Email failed to send"
                };
                await TeamsHelper.SendError($"Order Confirmation Email failed: {order.Email}", $"{ex.Message}");
                return StatusCode(500, details);
            }

            if (WasSuccessful(brontoResult))
            {
                OkObjectResult success = new OkObjectResult($"Success, Email Sent to {order.Email}");

                return Ok(success);
            }
            else
            {
                _logger.LogError("Email send failed for {email}. Error code: {brontoResult}", order.Email, brontoResult.results[0].errorString);
                var details = new ProblemDetails
                {
                    Detail = brontoResult.results[0].errorString,
                    Title = "Email failed to send"
                };
                await TeamsHelper.SendError($"Email failed to send to {order.Email}", $"{brontoResult.results[0].errorString}");
                return StatusCode(500, details);
            }

        }

        /// <summary>
        /// Sends an Estimate Email. The template used is based on the estimate being for a Pro(Department == "29") or D2C(Department == "27").
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="estimate">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("EstimateEmail")]
        public string EstimateEmail(Estimate estimate)
        {
            return Transact.EstimateEmail(estimate);
        }

        /// <summary>
        /// Sends a Shipping Confirmation Email. The template used is based on the estimate being for a Pro(Department == "29") or D2C(Department == "27").
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="order">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("ShippingConfirmation")]
        public string ShippingConfirmation(Order order)
        {
            return Transact.ShippingConfirmation(order);
        }

        /// <summary>
        /// Sends Password Reset Email. The template used is based on the customer value IsPro(bool).
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and Token are mandatory</param>
        [HttpPost("PasswordReset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> PasswordReset(Customer customer)
        {
            var messageType = customer.IsPro ? ProPasswordResetMessageID : D2CPasswordResetMessageID;

            return SendAccountEmail(customer, messageType);
        }
        
        /// <summary>
        /// Notifies user their password has been updated. The template used is based on the customer value IsPro(bool).
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">**This email does not have dynamic fields in the template, Customer Email and IsPro are only mandatory fields**</param>
        [HttpPost("PasswordUpdate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> PasswordUpdate(Customer customer)
        {
            var messageType = customer.IsPro ? ProPasswordUpdateMessageID : D2CPasswordUpdateMessageID;

            return SendAccountEmail(customer, messageType);
        }

        /// <summary>
        /// Sends an Account Elevation Email.
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and IsNew are mandatory fields. TempPassword is required if IsNew == true, meaning a Net New Pro</param>
        [HttpPost("AccountElevation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> AccountElevation(Customer customer)
        {
            Random rand = new Random();

            var messageType = customer.IsNew ? NewCustomerAlbertMessageID[rand.Next(NewCustomerAlbertMessageID.Length)] :
                customer.IsPro ? ProCustomerAlbertMessageID[rand.Next(ProCustomerAlbertMessageID.Length)] : D2CCustomerAlbertMessageID[rand.Next(D2CCustomerAlbertMessageID.Length)];

            return SendAccountEmail(customer, messageType);
        }

        /// <summary>
        /// Sends a Specific Transactional Email based on current Promotion.
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and IsNew are mandatory fields. TempPassword is required if IsNew == true, meaning a Net New Pro</param>
        [HttpPost("Promo")]
        public string Promo(Customer customer)
        {
            return Transact.Promo(customer);
        }

        /// <summary>
        /// Sends a keyword to trigger a Bronto workflow via API.
        /// </summary>
        /// <remarks>returns a string indicating whether or not the workflow was triggered</remarks>
        /// <param name="customer">Customer Email with the customer Keyword are required to trigger the workflow</param>
        [HttpPost("TriggerBrontoWorkflow")]
        public string TriggerBrontoWorkflow(Customer customer)
        {
            return Transact.TriggerBrontoWorkflow(customer);
        }

        /// <summary>
        /// Sends a Pro Welcome Email.
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and IsNew are mandatory fields. TempPassword is required if IsNew == true, meaning a Net New Pro</param>
        [HttpPost("WelcomeEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> WelcomeEmail(Customer customer)
        {
            var messageType = ProWelcomeMessageID;

            return SendAccountEmail(customer, messageType);
        }

        private async Task<IActionResult> SendAccountEmail(Customer customer, string messageType)
        {
            JObject brontoResult = null;
            try
            {
                brontoResult = await BrontoConnector.SendAccountEmail(customer, messageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Account Email failed to send"
                };

                await TeamsHelper.SendError($"Account Email failed: {customer.Email}", $"{ex.Message}");
                return StatusCode(500, details);
            }

            if (WasSuccessful(brontoResult))
            {
                return Ok();
            }
            else
            {
                _logger.LogError("Email send failed for {email}. Error code: {brontoResult}", customer.Email, brontoResult);
                var details = new ProblemDetails
                {
                    Detail = brontoResult.ToString(),
                    Title = "Account Email failed to send"
                };

                await TeamsHelper.SendError($"Account Email failed to send", $"Failed to send to {customer.Email}. {brontoResult.ToString()}");
                return StatusCode(500, details);
            }
        }

        private static bool WasSuccessful(JObject result)
        {
            return !(bool)result["isError"];
        }

        private static bool WasSuccessful(writeResult result)
        {
            return !(bool)result.results[0].isError;
        }

    }

}
