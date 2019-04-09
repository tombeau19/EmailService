using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : Controller
    {
        // POST api/UpdateContact
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

        // POST api/UpdateSalesRep
        /// <summary>
        /// When Changes are made to Sales Rep(ie. name change, directline change, etc.). This method updates all the Rep's customers in Bronto
        /// </summary>
        /// <remarks>returns a string with the number of successful updates and number of failed updates</remarks>
        /// <param name="repWithCustomers">A Json object with the new sales rep info and a string array of all customers beloning to said rep. Field names to send: SalesRepFirstName, SalesRepLastName, SalesRepDirectLine, SalesRepImageUrlSmall, SalesRepImageUrlLarge, SalesRepEmail, SalesRepTitle</param>
        [HttpPost("UpdateSalesRep")]
        public string UpdateSalesRep(JObject repWithCustomers)
        {
            return Sync.UpdateSalesRep(repWithCustomers);
        }
    }
}