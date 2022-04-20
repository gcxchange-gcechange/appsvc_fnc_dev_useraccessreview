using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace appsvc_fnc_dev_useraccessreview
{
    public static class scanning
    {
        [FunctionName("scanning")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //LA_user
            var userdatala=new LA_users();
            var usersdatla= await userdatala.LA_usersData(log);


            //LA_user
            var user_storage_table = new storage_table();
            var user_table = await user_storage_table.storage_table_data(log);

            //compare
            var increment = 1;
            foreach (var user in usersdatla)
            {
                increment++;
                if (user_table.Any(a => a.UPN == user.UPN))
                {
                    //compare date signin
                    log.LogInformation($"compare signin date {user.UPN}");
                    log.LogInformation("Delete item from user_table");
                }
                else
                {
                    // Define the row,
                    string sRow = increment.ToString();

                    //// Create the Entity and set the partition to signup, 
                    PersonEntity _person = new PersonEntity("signup", sRow);

                    _person.UPN = user.UPN;
                    _person.Id = user.Id;
                    _person.signinDate = user.signinDate;

                    try
                    {
                        await user_storage_table.insert_table_data(_person, log);
                    }catch (Exception ex)
                    {
                        log.LogInformation(ex.Message);
                    }
                    
                }
                log.LogInformation($"delete item from usersdata {user.UPN}");
            }

            //check what left in array table
            foreach (var item in user_table)
            {
                log.LogInformation($"delete user  {item.UPN}");
            }
                
            string responseMessage =$"Hello, this is my return for now";

            return new OkObjectResult(responseMessage);
        }

        public static async Task<string> table_getAll(CloudTableClient tableClient, PersonEntity _person, ILogger log)
        {
            CloudTable table = tableClient.GetTableReference("personitems");
            TableContinuationToken token = null;

            do
            {
                var q = new TableQuery<PersonEntity>();
                var queryResult = await table.ExecuteQuerySegmentedAsync(q, token);
                foreach (var item in queryResult.Results)
                {
                log.LogInformation($"{item.UPN}");
                    // yield return item;
                }
                token = queryResult.ContinuationToken;
            } while (token != null);
            return "ok";
        }   
    }
}

