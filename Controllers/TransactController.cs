using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactController : ControllerBase
    {

        // POST api/OrderConfirmation
        /// <summary>
        /// Sends an Order Confirmation Email. The template used is based on the order being SUPPLYnow(bool), Pro(Department == "29"), or D2C(Department == "27").
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="order">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("OrderConfirmation")]
        public string OrderConfirmation(Order order)
        {
            return Transact.OrderConfirmation(order);
        }

        // POST api/PasswordReset
        /// <summary>
        /// Sends Password Reset Email. The template used is based on the customer value IsPro(bool).
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">For field names and datatypes, please reference BrontoLibrary Customer Model. Customer Email and IsPro are mandatory</param>
        [HttpPost("PasswordReset")]
        public string PasswordReset(Customer customer)
        {
            return Transact.PasswordReset(customer);
        }

        // POST api/PasswordUpdate
        /// <summary>
        /// Notifies user their password has been updated. The template used is based on the customer value IsPro(bool).
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="customer">**This email does not have dynamic fields in the template, customer Email and IsPro are only mandatory fields**</param>
        [HttpPost("PasswordUpdate")]
        public string PasswordUpdate(Customer customer)
        {
            return Transact.PasswordUpdate(customer);
        }

        [HttpPost("AccountElevation")]
        public string AccountElevation(Customer customer)
        {
            return Transact.AccountElevation(customer);
        }

    }

}
