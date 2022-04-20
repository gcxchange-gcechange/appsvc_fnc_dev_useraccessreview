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
        public async Task<List<userTable>> storage_table_data (ILogger log)
        {
            log.LogInformation("C# HTTP trigger fun ction processed a request.");

            IConfiguration config = new ConfigurationBuilder()

                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .Build();
            // Connect to the Storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);       
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            var getAll = await table_getAll(tableClient, log);

            return getAll;
        }

        public static async Task<List<userTable>> table_getAll(CloudTableClient tableClient, ILogger log)
        {
            // Get user that never sign in
            CloudTable table = tableClient.GetTableReference("userAccessReview");
            List<userTable> userTables = new List<userTable>();
        
                //var table = this.GetCloudTable("personitems");
                TableContinuationToken token = null;
                do
                {
                    var q = new TableQuery<PersonEntity>();
                    var queryResult = await table.ExecuteQuerySegmentedAsync(q, token);
                    foreach (var item in queryResult.Results)
                    {
                    log.LogInformation($"{item.Email}");
                    userTables.Add(new userTable { Id = item.Id, UPN = item.UPN, LastCall = "" });
                    // yield return item;
                }
                    token = queryResult.ContinuationToken;
                } while (token != null);
                return userTables;
            }
   
            
        }

    }

