using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using BrontoTransactionalEndpoint.Helpers;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using BrontoLibrary;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoReference;
using Microsoft.Extensions.Logging;
using System.Linq;
using BrontoTransactionalEndpoint.Controllers;
using Polly;

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

        private readonly int retryCount = 3;
        private readonly TimeSpan delay = TimeSpan.FromSeconds(2);

        #region Message IDs
        private readonly string NewCustomerAlbertMessageIDWithToken = "6e4e0a6403ef5f14824707a65f97d59f";
        private readonly string[] NewCustomerAlbertMessageID = { "eb06064ddafc735abd24f49de71c0c71", "8b0ac620498a2a58f0d8496b0e97c92a" };
        private readonly string[] ProCustomerAlbertMessageID = { "4a11ba0af5e44b261d708dcb62690aee", "9c2b2ea08455aad38dd03816e85bd7e1" };
        private readonly string D2CCustomerAlbertMessageID = "2fc1cd9ce17e5ccdbadec1cdfeb49778";
        private readonly string[] ProWelcomeMessageID = { "7839592063da1c9b6e93fcc1a3961741", "0c1099179ac01d55871493e073136164" };
        private readonly string ProPasswordResetMessageID = "0bdb03eb0000000000000000000000107043";
        private readonly string D2CPasswordResetMessageID = "cef7902b45ddfecfc6ed14d9f4f714df";
        private readonly string ProPasswordUpdateMessageID = "0bdb03eb0000000000000000000000107052";
        private readonly string D2CPasswordUpdateMessageID = "4fffc12ab5e0b56a7e57a0762570bda0";
        private readonly string ProOrderConfirmationMessageIDNoLeadTime = "0b39302354461c9bee7ff0b653c130a3";
        private readonly string D2COrderConfirmationMessageIDNoLeadTime = "d9d916fef652b2f4c91654e79156bc45";
        private readonly string ProDeliverySuccessMessageID = "2887898c77d3e4986a4a13648dea2db3";
        private readonly string ProDeliveryFailureMessageID = "1176a19817a76c7c09f81c7fe6160eff";
        private readonly string ProDeliveryUpdateMessageID = "00edf56162abcc7ac51e0a16851981cb";
        private readonly string D2CDeliverySuccessMessageID = "e9eda5d45743d2fa6e1f6543f8cb2a2a";
        private readonly string D2CDeliveryFailureMessageID = "8d5a1f997fcb37a8b3681cc4a724841b";
        private readonly string D2CDeliveryUpdateMessageID = "e3e03f33748217cd1b3203bad8293c3f";
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
            var messageId = order.Department == "29" ? ProOrderConfirmationMessageIDNoLeadTime : D2COrderConfirmationMessageIDNoLeadTime;

            writeResult brontoResult = new writeResult();
            int currentRetry = 0;

            for(;;)
            {
                try
                {
                    brontoResult = await BrontoConnector.SendOrderConfirmationEmail(messageId, deliveryType, order);
                    break;
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    _logger.LogError(ex, $"order confirmation email failed. attempt: {currentRetry}");
                    bool doNotRetry = ex.Message.ToLower().Contains("suppression") || ex.Message.ToLower().Contains("bounce")
                                        || ex.Message.ToLower().Contains("invalid") ? true : false;
                    if (currentRetry >= this.retryCount || doNotRetry)
                    {
                        var details = new ProblemDetails
                        {
                            Detail = ex.Message,
                            Title = "Email failed to send"
                        };
                        await TeamsHelper.SendError($"Order Confirmation Email failed: {order.Email}", $"{ex.Message}");
                        return StatusCode(500, details);
                    }
                }
                await Task.Delay(delay);
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
                        var responseData = new { subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber), brontoResponse = $"Success, Email Sent to {order.Email}" };
                        JObject responseObj = JObject.FromObject(responseData);
                        OkObjectResult success = new OkObjectResult(responseObj);
                        return Ok(success);
                    }
                    catch
                    {
                        subjectLine = "Error Setting Subject";
                        var responseData = new { subject = subjectLine, brontoResponse = $"Success, Email Sent to {order.Email}" };
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
        /// Sends a Delivery Update Email for Roadie Updates. Currently only Pro(Department == "29") will send an email.
        /// </summary>
        /// <remarks>returns a 200 or 500 code with details of success/failure</remarks>
        /// <param name="order">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("DeliveryUpdate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeliveryUpdate(Order order)
        {
            JObject brontoResult = null;
            string messageId;
            if (order.Department == "29")
            {
                messageId = order.DeliveryUpdate == "success" ? ProDeliverySuccessMessageID : order.DeliveryUpdate == "failure" ? ProDeliveryFailureMessageID : ProDeliveryUpdateMessageID;
            }
            else
            {
                messageId = order.DeliveryUpdate == "success" ? D2CDeliverySuccessMessageID : order.DeliveryUpdate == "failure" ? D2CDeliveryFailureMessageID : D2CDeliveryUpdateMessageID;
            }

            try
            {
                brontoResult = await BrontoConnector.SendDeliveryUpdateEmail(order, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send delivery update email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Email failed to send"
                };
                await TeamsHelper.SendError($"Delivery Update Email failed: {order.Email}", $"{ex.Message}");
                return StatusCode(500, details);
            }

            if (WasSuccessful(brontoResult))
            {
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#order_number%%", order.OrderNumber), brontoResponse = $"Success, Email Sent to {order.Email}" };
                    JObject responseObj = JObject.FromObject(responseData);
                    OkObjectResult success = new OkObjectResult(responseObj);
                    return Ok(success);
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = $"Success, Email Sent to {order.Email}" };
                    JObject responseObj = JObject.FromObject(responseData);
                    OkObjectResult success = new OkObjectResult(responseObj);
                    return Ok(success);
                }
            }
            else
            {
                _logger.LogError("Email send failed for {email}. Error code: {brontoResult}", order.Email, brontoResult);
                var details = new ProblemDetails
                {
                    Detail = brontoResult.ToString(),
                    Title = "Delivery Update Email failed to send"
                };

                await TeamsHelper.SendError($"Delivery Update failed to send", $"Failed to send to {order.Email}. {brontoResult.ToString()}");
                return StatusCode(500, details);
            }
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

            var messageId = customer.IsNew ? NewCustomerAlbertMessageID[rand.Next(NewCustomerAlbertMessageID.Length)] : customer.IsPro ? ProCustomerAlbertMessageID[rand.Next(ProCustomerAlbertMessageID.Length)] : D2CCustomerAlbertMessageID;

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
            var messageId = NewCustomerAlbertMessageIDWithToken;

            return SendAccountEmail(customer, messageId, NetsuiteController.MessageType.AlbertAndPRORegistration);
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
            Random rand = new Random();
            return SendAccountEmail(customer, ProWelcomeMessageID[rand.Next(ProWelcomeMessageID.Length)], NetsuiteController.MessageType.PROWelcomeAndOnboarding);
        }

        private async Task<IActionResult> SendAccountEmail(Customer customer, string messageId, NetsuiteController.MessageType messageType)
        {
            string[] doNotRetry = { "invalid", "bounce", "suppression" };
            var policy = Policy
                .Handle<Exception>(e => !doNotRetry.Any(s => e.Message.ToLower().Contains(s)))
                //.OrResult<JObject>(r => r["errorString"].Contains("101"))
                .WaitAndRetryAsync(
                    retryCount, 
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (exception, attempt) =>
                    {
                        _logger.LogError($"Email send failed: {customer.Email}. Error: {exception.Message}. Attempt: {attempt}");
                    }
                );

            JObject brontoResult = null;

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    brontoResult = await BrontoConnector.SendAccountEmail(customer, messageId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Account Email failed to send"
                };

                await TeamsHelper.SendError($"Account Email failed: {customer.Email}", $"{ex.Message}. Message Type: {messageType}");
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

                await TeamsHelper.SendError($"Account Email failed to send", $"Failed to send to {customer.Email}. {brontoResult.ToString()}. Message Type: {messageType}");
                return StatusCode(500, details);
            }
        }

        private static bool WasSuccessful(JObject result)
        {
            return !(bool)result["isError"];
        }

        private static bool WasSuccessful(writeResult result)
        {
            return !result.results[0].isError;
        }

    }

}
