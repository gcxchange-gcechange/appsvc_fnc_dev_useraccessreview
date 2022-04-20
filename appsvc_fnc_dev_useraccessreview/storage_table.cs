using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace appsvc_fnc_dev_useraccessreview
{
    class storage_table
    {
        public class Globals
        {
            //Global class so other class can access variables
            static IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            // Connect to the Storage account.
            private static readonly CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
            private static readonly CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            public static CloudTableClient Get_tableClient()
            {
                return tableClient;
            }
        }
        public async Task<List<userTable>> storage_table_data (ILogger log)
        {
            // Get user that never sign in
            CloudTable table = Globals.Get_tableClient().GetTableReference("userAccessReview");
            List<userTable> userTables = new List<userTable>();
        
                //var table = this.GetCloudTable("personitems");
                TableContinuationToken token = null;
                do
                {
                    var q = new TableQuery<PersonEntity>();
                    var queryResult = await table.ExecuteQuerySegmentedAsync(q, token);
                    foreach (var item in queryResult.Results)
                    {
                    userTables.Add(new userTable { Id = item.Id, UPN = item.UPN, signinDate = item.signinDate });
                    // yield return item;
                }
                    token = queryResult.ContinuationToken;
                } while (token != null);
            return userTables;
        }

        public async Task<string> insert_table_data(PersonEntity _person, ILogger log)
        {
            var result = "";

            // Get user that never sign in
            CloudTable table = Globals.Get_tableClient().GetTableReference("userAccessReview");

            await table.CreateIfNotExistsAsync();

            TableOperation insertOperation = TableOperation.Insert(_person);
            try
            {
                await table.ExecuteAsync(insertOperation);
                result = "Success";
            }catch (Exception ex)
            {
                result = ex.Message;
                log.LogInformation(ex.Message);
            }
            return result;
        }
    }
}

