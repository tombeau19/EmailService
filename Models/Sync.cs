using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
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

        internal static int UpdateSalesRep(JObject repData)
        {
            DateTime date = DateTime.Now;
            var updateDate = date.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var repId = repData["repId"].ToString();
            var repFirstName = repData["newRepFN"].ToString();
            var repLastName = repData["newRepLN"].ToString();
            var repEmail = repData["newRepEmail"].ToString();
            var repDirectLine = repData["newRepDL"].ToString();
            var repTitle = repData["newRepTitle"].ToString();
            var repImageURL_small = repData["newRepIURL"].ToString();
            var repImageURL_large = repData["newRepIURL2"].ToString();
            int updatedInBronto = 0;
            int CountOfCustomers = 0;
            int CustomersUpdated = 0;

            SqlConnection repConnection = new SqlConnection("Data Source=srv-pro-sqls-02;Initial Catalog=BRONTO;Integrated Security=True");
            repConnection.Open();
            try
            {
                SqlCommand addRepsToTable = new SqlCommand("INSERT INTO dbo.MarketingSalesRepSyncLog (UpdateDate, RepId, RepFirstName, RepLastName, RepEmail, RepDirectLine, RepTitle, RepImageURL_small, RepImageURL_large, UpdatedInBronto, CountOfCustomers, CustomersUpdated) " +
                        $"VALUES ('{updateDate}', '{repId}', '{repFirstName}', '{repLastName}', '{repEmail}', '{repDirectLine}', '{repTitle}', '{repImageURL_small}', '{repImageURL_large}', {updatedInBronto}, {CountOfCustomers}, {CustomersUpdated});", repConnection);
                var insertCall = addRepsToTable.ExecuteNonQuery();
                return insertCall;
            }
            catch(Exception ex)
            {
                return 0;
            }
        }
    }
}
