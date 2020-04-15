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
        private readonly string[] doNotRetry = { "invalid", "bounce", "suppression" };
        private readonly Polly.Retry.AsyncRetryPolicy _policy;
        public TransactController(ILogger<TransactController> logger)
        {
            _logger = logger;
            _policy = Policy
                .Handle<Exception>(e => !doNotRetry.Any(s => e.Message.ToLower().Contains(s)))
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (exception, timeSpan, retryCount) =>
                    {
                        _logger.LogError($"Email send failed. Error: {exception.Message}. Delay: {timeSpan}");
                    }
                );
        }

        #region Message IDs
        //account emails
        private readonly string NewCustomerAlbertMessageIDWithToken = "6e4e0a6403ef5f14824707a65f97d59f";
        private readonly string[] NewCustomerAlbertMessageID = { "eb06064ddafc735abd24f49de71c0c71", "8b0ac620498a2a58f0d8496b0e97c92a" };
        private readonly string[] ProCustomerAlbertMessageID = { "4a11ba0af5e44b261d708dcb62690aee", "9c2b2ea08455aad38dd03816e85bd7e1" };
        private readonly string D2CCustomerAlbertMessageID = "2fc1cd9ce17e5ccdbadec1cdfeb49778";
        private readonly string[] ProWelcomeMessageID = { "7839592063da1c9b6e93fcc1a3961741", "0c1099179ac01d55871493e073136164" };
        private readonly string ProPasswordResetMessageID = "0bdb03eb0000000000000000000000107043";
        private readonly string D2CPasswordResetMessageID = "cef7902b45ddfecfc6ed14d9f4f714df";
        private readonly string ProPasswordUpdateMessageID = "0bdb03eb0000000000000000000000107052";
        private readonly string D2CPasswordUpdateMessageID = "4fffc12ab5e0b56a7e57a0762570bda0";
        //order confirmations
        private readonly string ProOrderConfirmationMessageIDNoLeadTime = "0b39302354461c9bee7ff0b653c130a3";
        private readonly string D2COrderConfirmationMessageIDNoLeadTime = "d9d916fef652b2f4c91654e79156bc45";
        //delivery updates -- roadie emails
        private readonly string ProDeliverySuccessMessageID = "2887898c77d3e4986a4a13648dea2db3";
        private readonly string ProDeliveryFailureMessageID = "1176a19817a76c7c09f81c7fe6160eff";
        private readonly string ProDeliveryUpdateMessageID = "00edf56162abcc7ac51e0a16851981cb";
        private readonly string D2CDeliverySuccessMessageID = "e9eda5d45743d2fa6e1f6543f8cb2a2a";
        private readonly string D2CDeliveryFailureMessageID = "8d5a1f997fcb37a8b3681cc4a724841b";
        private readonly string D2CDeliveryUpdateMessageID = "e3e03f33748217cd1b3203bad8293c3f";
        //estimates
        private readonly string ProEstimateMessageID = "0bdb03eb000000000000000000000010761b";
        private readonly string OneWkToCloseMessageID = "c7fe3060fccfab3446bc9c4bc0ad95ce";
        private readonly string DayOfCloseMessageID = "6daefecf836cd65fdf3640d1598fee54";
        private readonly string OneWkPastCloseMessageID = "187b70784f747adf1ea5763d9e5b8440";
        private readonly string TwoWkToExpireMessageID = "d686d25c8ca178ecdaa95bcfb823ea4d";
        private readonly string OneWkToExpireMessageID = "832c37f898bc5f706b207f6f0cbb054a";
        private readonly string DayAfterExpireMessageID = "5aba812ac3c3433f65c0f13b306e36ec";
        private readonly string D2CEstimateMessageID = "02e304b62399fb5ecd7a6a4325bfe4af";
        //shipping
        private readonly string ProEntireOrderShipped = "bae5ff316d97b84eeb6956918209f3ce";
        private readonly string ProOneItemQtyOne = "6d6d6845555ed2af46b5f83459e10b8f";
        private readonly string ProShipping = "f3703ac72ea42b799b45cec77e8007c2";
        private readonly string D2CEntireOrderShipped = "79e3a8979188d86c4dafa26479a2f67e";
        private readonly string D2COneItemQtyOne = "3192036580580fb2a830cc4052b1bcde";
        private readonly string D2CShipping = "ed24176d6796a12b4b23514c932ec598";
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
            try
            {
                await _policy.ExecuteAsync(async () =>
                {
                    brontoResult = await BrontoConnector.SendOrderConfirmationEmail(messageId, deliveryType, order);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Order email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Order Email failed to send"
                };
                await TeamsHelper.SendError($"Order Email failed: {order.Email}", $"{ex.Message}.");
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EstimateEmail(Estimate estimate)
        {
            var messageId = "";
            if (estimate.Department == "29")
            {
                messageId = estimate.EstimateType == 1 ? OneWkToCloseMessageID :
                           estimate.EstimateType == 2 ? DayOfCloseMessageID :
                               estimate.EstimateType == 3 ? OneWkPastCloseMessageID :
                                   estimate.EstimateType == 4 ? TwoWkToExpireMessageID :
                                       estimate.EstimateType == 5 ? OneWkToExpireMessageID :
                                           estimate.EstimateType == 6 ? DayAfterExpireMessageID : ProEstimateMessageID;

            }
            else if (estimate.Department == "27" && estimate.EstimateType == 0)
            {
                messageId = D2CEstimateMessageID;
            }

            JObject brontoResult = null;
            try
            {
                await _policy.ExecuteAsync(async () =>
                {
                    brontoResult = await BrontoConnector.SendEstimateEmail(estimate, messageId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Estimate Email failed to send"
                };
                await TeamsHelper.SendError($"Estimate Email failed: {estimate.Email}", $"{ex.Message}.");
                return StatusCode(500, details);
            }

            if (WasSuccessful(brontoResult))
            {
                string subjectLine;
                try
                {
                    var messageInfo = BrontoConnector.ReadMessageInfo(messageId).Result;
                    subjectLine = (string)messageInfo["subjectLine"];
                    var responseData = new { subject = subjectLine.Replace("%%#estimate_number%%", estimate.EstimateNumber), brontoResponse = $"Success, Email Sent to {estimate.Email}" };
                    JObject responseObj = JObject.FromObject(responseData);
                    OkObjectResult success = new OkObjectResult(responseObj);
                    return Ok(success);
                }
                catch
                {
                    subjectLine = "Error Setting Subject";
                    var responseData = new { subject = subjectLine, brontoResponse = $"Success, Email Sent to {estimate.Email}" };
                    JObject responseObj = JObject.FromObject(responseData);
                    OkObjectResult success = new OkObjectResult(responseObj);
                    return Ok(success);
                }
            }
            else
            {
                _logger.LogError("Email send failed for {email}. Error code: {brontoResult}", estimate.Email, brontoResult);
                var details = new ProblemDetails
                {
                    Detail = brontoResult.ToString(),
                    Title = "Estimate Email failed to send"
                };
                await TeamsHelper.SendError($"Estimate Email failed to send", $"Failed to send to {estimate.Email}. {brontoResult.ToString()}");
                return StatusCode(500, details);
            }
        }

        /// <summary>
        /// Sends a Shipping Confirmation Email. The template used is based on the estimate being for a Pro(Department == "29") or D2C(Department == "27").
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="order">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("ShippingConfirmation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ShippingConfirmation(Order order)
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

            string messageId;
            if (order.Department == "29")
            {
                messageId = entireOrderShipped ? ProEntireOrderShipped : oneItemWithQtyOne ? ProOneItemQtyOne : ProShipping;
            }
            else
            {
                messageId = entireOrderShipped ? D2CEntireOrderShipped : oneItemWithQtyOne ? D2COneItemQtyOne : D2CShipping;
            }

            JObject brontoResult = null;
            try
            {
                await _policy.ExecuteAsync(async () =>
                {
                    brontoResult = await BrontoConnector.SendShippingConfirmationEmail(order, messageId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send shipping email");
                var details = new ProblemDetails
                {
                    Detail = ex.Message,
                    Title = "Shipping Email failed to send"
                };
                await TeamsHelper.SendError($"Shipping Email failed: {order.Email}", $"{ex.Message}.");
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
                    Title = "Shipping Email failed to send"
                };
                await TeamsHelper.SendError($"Shipping Email failed to send", $"Failed to send to {order.Email}. {brontoResult.ToString()}");
                return StatusCode(500, details);
            }
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
                await _policy.ExecuteAsync(async () =>
                {
                    brontoResult = await BrontoConnector.SendDeliveryUpdateEmail(order, messageId);
                });
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
            string result;
            try
            {
                var brontoResult = BrontoConnector.TriggerBrontoWorkflow(customer).Result;
                result = brontoResult.ToString();
            }
            catch (Exception e)
            {
                result = $"{e.Message.ToString()}";
            }
            return result;
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
            JObject brontoResult = null;
            try
            {
                await _policy.ExecuteAsync(async () =>
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

        #region helpers
        private static bool WasSuccessful(JObject result)
        {
            return !(bool)result["isError"];
        }
        private static bool WasSuccessful(writeResult result)
        {
            return !result.results[0].isError;
        }
        #endregion
    }

}
