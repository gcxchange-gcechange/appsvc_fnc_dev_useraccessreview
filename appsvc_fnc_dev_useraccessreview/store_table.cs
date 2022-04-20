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
    public static class store_table
    {
        [FunctionName("store_table")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string email = req.Query["email"];
            string UPN = req.Query["UPN"];
            string userID = req.Query["userID"];
            string signinDate = req.Query["signinDate"];
            string table_action = req.Query["table_action"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            email = email ?? data?.email;
            UPN = UPN ?? data?.UPN;
            userID = userID ?? data?.userID;
            signinDate = signinDate ?? data?.signinDate;
            table_action = table_action ?? data.table_action;


            // Define the row,
            string sRow = email + UPN;

            // Create the Entity and set the partition to signup, 
            PersonEntity _person = new PersonEntity("signup", sRow);

            _person.UPN = UPN;
            _person.Id = userID;
            _person.signinDate = signinDate;
            _person.Email = email;
            IConfiguration config = new ConfigurationBuilder()

                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .Build();
            // Connect to the Storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
           


            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            switch (table_action)
            {
                case "insert":
                    var insert = await table_insert(tableClient, _person, log);
                    break;
                case "update":
                    var update = await table_update(tableClient, UPN, log);
                    break;
                case "delete":
                    var delete = await table_delete(tableClient, _person, log);
                    break;
                case "all":
                    var getAll = await table_getAll(tableClient, _person, log);
                    break;
            }    
                
            string responseMessage = string.IsNullOrEmpty(UPN)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {UPN}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        public static async Task<string> table_insert(CloudTableClient tableClient, PersonEntity _person, ILogger log)
        {
            // Get user that never sign in
            CloudTable table = tableClient.GetTableReference("userAccessReview");

            await table.CreateIfNotExistsAsync();

            TableOperation insertOperation = TableOperation.Insert(_person);

            await table.ExecuteAsync(insertOperation);
            return "ok";
        }

        public static async Task<string> table_update(CloudTableClient tableClient, string UPN, ILogger log)
        {
            log.LogInformation("In update");
            // Get user that never sign in
            CloudTable table = tableClient.GetTableReference("personitems");

            await table.CreateIfNotExistsAsync();

            // Create a retrieve operation that takes an item entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<PersonEntity>("signup", "stephanie@tbs.gc.calefebvre");

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to a Item object.
            PersonEntity updateEntity = (PersonEntity)retrievedResult.Result;
            log.LogInformation($"{updateEntity}");

            //if (updateEntity != null)
            //{
                //Change the description
                updateEntity.UPN = UPN;

                // Create the InsertOrReplace TableOperation
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);

                // Execute the operation.
               await table.ExecuteAsync(insertOrReplaceOperation);
                log.LogInformation("Entity was updated.");
        //}
            return "ok";
        }

        public static async Task<string> table_delete(CloudTableClient tableClient, PersonEntity _person, ILogger log)
        {
            // Get user that never sign in
            CloudTable table = tableClient.GetTableReference("personitems");

            try
            {
                // Create a retrieve operation that takes an item entity.
                TableOperation retrieveOperation = TableOperation.Retrieve<PersonEntity>("signup", "stephanie@tbs.gc.calefebvre");

                // Execute the operation.
                TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
                PersonEntity deleteEntity = (PersonEntity)retrievedResult.Result;

                TableOperation delteOperation = TableOperation.Delete(deleteEntity);
                await table.ExecuteAsync(delteOperation);
            }
            catch (Exception ex)
            {
                log.LogInformation($"{ex}");
                throw;
            }
            return "ok";
        }

        public static async Task<string> table_getAll(CloudTableClient tableClient, PersonEntity _person, ILogger log)
        {
            // Get user that never sign in
            CloudTable table = tableClient.GetTableReference("personitems");

        
                //var table = this.GetCloudTable("personitems");
                TableContinuationToken token = null;
                do
                {
                    var q = new TableQuery<PersonEntity>();
                    var queryResult = await table.ExecuteQuerySegmentedAsync(q, token);
                    foreach (var item in queryResult.Results)
                    {
                    log.LogInformation($"{item.Email}");
                       // yield return item;
                    }
                    token = queryResult.ContinuationToken;
                } while (token != null);
                return "ok";
            }
   
            
        }

    }

