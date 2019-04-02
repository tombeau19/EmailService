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
        /// Sends an Order Confirmation Email via the Bronto Platform
        /// </summary>
        /// <remarks>returns a string with the details of the Email Send attempt</remarks>
        /// <param name="orderInput">For field names and datatypes, please reference BrontoLibrary Order Model, or the model on swagger</param>
        [HttpPost("OrderConfirmation")]
        public string OrderConfirmation(Order orderInput)
        {
            return Bronto.OrderConfirmation(orderInput);
        }

        // POST api/UpdateContact
        /// <summary>
        /// Updates a contact in Bronto
        /// </summary>
        /// <remarks>returns a string with the details of the updateContact call</remarks>
        /// <param name="contact">A Json object of the contact you wish to update. Fields that get updated: SalesRepFirstName, SalesRepLastName, SalesRepDirectLine, SalesRepImageUrlSmall, SalesRepImageUrlLarge, SalesRepEmail, SalesRepTitle, CallLogTimeStamp</param>
        [HttpPost("UpdateContact")]
        public string UpdateContact(JObject contact)
        {
            return Bronto.UpdateContact(contact);
        }

        // Get api/OrderConfirmationGet
        /// <summary>
        /// Testing purposes only
        /// </summary>
        [HttpGet("OrderConfirmationGet/{orderString}")]
        public string OrderConfirmationGet(string orderString)
        {
            //return Bronto.OrderConfirmation(orderString);
            return orderString;
        }
    }

}
