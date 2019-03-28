using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactController : ControllerBase
    {

        // POST api/values
        [HttpPost]
        public string OrderConfirmation(string orderString)
        {
            return Bronto.OrderConfirmation(orderString);
        }

    }
}
