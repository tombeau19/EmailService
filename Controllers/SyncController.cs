using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoLibrary;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : Controller
    {
        /// <summary>
        /// Updates a contact in Bronto
        /// </summary>
        /// <remarks>returns a string with the details of the updateContact call</remarks>
        /// <param name="contact">A Json object of the contact you wish to update. Json object field names to send: SalesRepFirstName, SalesRepLastName, SalesRepDirectLine, SalesRepImageUrlSmall, SalesRepImageUrlLarge, SalesRepEmail, SalesRepTitle, CallLogTimeStamp</param>
        [HttpPost("UpdateContact")]
        public string UpdateContact(JObject contact)
        {
            return Sync.UpdateContact(contact);
        }
        
        /// <summary>
        /// When Changes are made to Sales Rep, this saves the rep to the BRONTO.MarketingSalesRepSyncLog to be udpated by the Sync project
        /// </summary>
        /// <remarks>returns a 1 when success and 0 when failed</remarks>
        /// <param name="repData"></param>
        [HttpPost("UpdateSalesRep")]
        public int UpdateSalesRep(JObject repData)
        {
            return Sync.UpdateSalesRep(repData);
        }
        
        /// <summary>
        /// Endpoint called from NetSuite on creation of an Albert Task. This saves the URL the customer used to fill out the Albert to Bronto where we use it for marketing
        /// </summary>
        /// <remarks>returns true or false based on success</remarks>
        /// <param name="customer"></param>
        [HttpPost("SaveFieldsToBronto")]
        public bool SaveFieldsToBronto(Customer customer)
        {
            return BrontoConnector.SyncFieldsToBronto(customer).Result;
        }
    }
}