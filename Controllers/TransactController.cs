using Microsoft.AspNetCore.Mvc;
using BrontoTransactionalEndpoint.Models;
using System.Collections.Generic;

namespace BrontoTransactionalEndpoint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactController : ControllerBase
    {

        // POST api/OrderConfirmation
        [HttpPost("OrderConfirmation")]
        public Order OrderConfirmation(Order input)
        {
            //return Bronto.OrderConfirmation(input);
            return input;
        }

        [HttpGet("OrderConfirmationGet/{orderString}")]
        public string OrderConfirmationGet(string orderString)
        {
            //return Bronto.OrderConfirmation(orderString);
            return orderString;
        }
    }

    public class Order
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string ShipAttn { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipAddress2 { get; set; }
        public string ShipCity { get; set; }
        public string ShipState { get; set; }
        public string ShipZip { get; set; }
        public string ShipCountry { get; set; }
        public string BillName { get; set; }
        public string BillAddress { get; set; }
        public string BillAddress2 { get; set; }
        public string BillCity { get; set; }
        public string BillState { get; set; }
        public string BillZip { get; set; }
        public string BillCountry { get; set; }
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public string Total { get; set; }
        public string SubTotal { get; set; }
        public string Tax { get; set; }
        public string Shipping { get; set; }
        public string OnlineSummaryLink { get; set; }
        public List<LineItem> LineItems { get; set; }
    }
    public class LineItem
    {
        public string Description { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string ShippingMessage { get; set; }
        public string Note { get; set; }
        public string ImageUrl { get; set; }
        public string Discount { get; set; }
        public bool BackOrdered { get; set; }
        public bool ListSection { get; set; }
    }
}
