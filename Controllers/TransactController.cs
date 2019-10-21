using Microsoft.AspNetCore.Mvc;
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
        private readonly string[] NewCustomerAlbertMessageIDWithToken = { "6e4e0a6403ef5f14824707a65f97d59f", "8120e19feb130544962b56a019cd3a58" };
        private readonly string[] NewCustomerAlbertMessageID = { "eb06064ddafc735abd24f49de71c0c71", "8b0ac620498a2a58f0d8496b0e97c92a" };
        private readonly string ProCustomerAlbertMessageID = "4a11ba0af5e44b261d708dcb62690aee";
        private readonly string D2CCustomerAlbertMessageID = "2fc1cd9ce17e5ccdbadec1cdfeb49778";
        private readonly string ProWelcomeMessageID = "59df810343334dde290123cc9a477f0b";
        private readonly string ProPasswordResetMessageID = "0bdb03eb0000000000000000000000107043";
        private readonly string D2CPasswordResetMessageID = "cef7902b45ddfecfc6ed14d9f4f714df";
        private readonly string ProPasswordUpdateMessageID = "0bdb03eb0000000000000000000000107052";
        private readonly string D2CPasswordUpdateMessageID = "4fffc12ab5e0b56a7e57a0762570bda0";
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
            var messageId = order.SupplyNow ? SUPPLYnowOrderConfirmationMessageID : order.Department == "29" ? ProOrderConfirmationMessageIDNoLeadTime : D2COrderConfirmationMessageIDNoLeadTime;

            writeResult brontoResult = new writeResult();

            try
            {
                brontoResult = await BrontoConnector.SendOrderConfirmationEmail(messageId, deliveryType, order);
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
                if (order.OrderNumber.Contains("SO"))
                {
                    string subjectLine;
                    try
                    {
                        var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                        subjectLine = (string)messageInfo["subjectLine"];
                        var responseData = new { subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber), brontoRespone = $"Success, Email Sent to {order.Email}" };
                        JObject responseObj = JObject.FromObject(responseData);
                        OkObjectResult success = new OkObjectResult(responseObj);
                        return Ok(success);
                    }
                    catch
                    {
                        subjectLine = "Error Setting Subject";
                        var responseData = new { subject = subjectLine, brontoRespone = $"Success, Email Sent to {order.Email}" };
                        JObject responseObj = JObject.FromObject(responseData);
                        OkObjectResult success = new OkObjectResult(responseObj);
                        return Ok(success);
                    }
                }
                else
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var createBMR = NetsuiteController.CreateBrontoMessageRecord(order, messageId, NetsuiteController.MessageType.OrderConfirmation);
                            if (string.IsNullOrEmpty(createBMR))
                            {
                                await TeamsHelper.SendError($"Error Creating BMR for: {order.Email}", $"{order.OrderNumber}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await TeamsHelper.SendError($"Error Creating BMR for: {order.Email}, {order.OrderNumber}", $"{ex.Message}");
                        }
                    }).ConfigureAwait(false);


                    return Ok();
                }

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
            var messageId = customer.IsPro ? ProPasswordResetMessageID : D2CPasswordResetMessageID;

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.PasswordReset);
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
            var messageId = customer.IsPro ? ProPasswordUpdateMessageID : D2CPasswordUpdateMessageID;

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.PasswordUpdate);
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

            var messageId = customer.IsNew ? NewCustomerAlbertMessageID[rand.Next(NewCustomerAlbertMessageID.Length)] : customer.IsPro ? ProCustomerAlbertMessageID : D2CCustomerAlbertMessageID;

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.AlbertAndPRORegistration);
        }

        /// <summary>
        /// Sends an Account Elevation Email with a Token instead of temp password.
        /// </summary>
        /// <remarks>returns a http response with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and IsNew are mandatory fields. TempPassword is required if IsNew == true, meaning a Net New Pro</param>
        [HttpPost("AccountElevationWithToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> AccountElevationWithToken(Customer customer)
        {
            Random rand = new Random();
            var messageId = NewCustomerAlbertMessageIDWithToken[rand.Next(NewCustomerAlbertMessageIDWithToken.Length)];

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.AlbertAndPRORegistration);
        }

        /// <summary>
        /// Sends a Specific Transactional Email based on current Promotion.
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">Customer Email, IsPro, and IsNew are mandatory fields. TempPassword is required if IsNew == true, meaning a Net New Pro</param>
        [HttpPost("Promo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Promo(Customer customer)
        {
            var details = new ProblemDetails()
            {
                Title = "Promo Endpoint is OFF"
            };
            return StatusCode(500, details);
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
            var messageId = ProWelcomeMessageID;

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.WelcomeEmail);
        }

        private async Task<IActionResult> SendAccountEmail(Customer customer, string messageId, NetsuiteController.MessageType messageType)
        {
            JObject brontoResult = null;
            try
            {
                brontoResult = await BrontoConnector.SendAccountEmail(customer, messageId);
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
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var createBMR = NetsuiteController.CreateBrontoMessageRecord(customer, messageId, messageType);
                        if (string.IsNullOrEmpty(createBMR))
                        {
                            await TeamsHelper.SendError($"Error Creating BMR for: {customer.Email}", $"Message Type: {messageType}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await TeamsHelper.SendError($"Error Creating BMR for: {customer.Email}, Message Type: {messageType}", $"{ex.Message}");
                    }
                }).ConfigureAwait(false);
                
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
