using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BrontoLibrary.Models;
using BrontoLibrary;
using BrontoReference;
using BrontoTransactionalEndpoint.Controllers;

namespace BrontoTransactionalEndpoint.Models
{
    public class Sync
    {
        internal static string UpdateContact(JObject contact)
        {
            JObject brontoResult = BrontoConnector.UpdateContact(contact).Result;

            if ((int)brontoResult["errorCode"] != 0)
            {
                return $"UpdateContact failed for {(string)contact["Email"]}. ErrorCode: {(int)brontoResult["errorCode"]}. ErrorString: {(string)brontoResult["errorString"]}.";
            }
            else
            {
                return $"{(string)contact["Email"]} successfully updated";
            }
        }

        internal static string UpdateSalesRep(JObject repWithCustomers)
        {
            //TODO figure out error handling with this bad boy
            return  BrontoConnector.UpdateSalesRep(repWithCustomers).Result;
        }
    }
}
