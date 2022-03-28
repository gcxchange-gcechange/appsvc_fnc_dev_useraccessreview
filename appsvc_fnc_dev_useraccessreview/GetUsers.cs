using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Graph;
using Azure.Identity;
//using Azure.Monitor.Query;
//using Azure.Monitor.Query.Models;
using Azure;
using Microsoft.Extensions.Configuration;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System.Collections.Generic;

namespace appsvc_fnc_dev_useraccessreview
{
    public static class GetUsers
    {
        [FunctionName("GetUsers")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            IConfiguration config = new ConfigurationBuilder()

            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            string clientSecret = config["clientSecret"];
            string clientId = config["clientId"];
            string tenantid = config["tenantid"];
            string workspaceId = config["workspaceId"];

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            Auth auth = new Auth();
            var graphAPIAuth = auth.graphAuth(log);

            var PendingAcceptance = await getUsersPendingAcceptance(graphAPIAuth, log);
            var OldLogs = await getUsersOldLogs(tenantid, clientId, clientSecret, workspaceId, log);


            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(OldLogs);
        }

        public static async Task<IGraphServiceUsersCollectionPage> getUsersPendingAcceptance(GraphServiceClient graphServiceClient, ILogger log)
        {
            // Get user that never sign in
            var users = await graphServiceClient.Users
            .Request()
            .Select("userPrincipalName,externalUserState")
            .Filter("externalUserState eq 'PendingAcceptance'")
            .GetAsync();
            return users;
        }

        public static async Task<List<userTable>> getUsersOldLogs(string tenantid, string clientId, string clientSecret, string workspaceId, ILogger log)
        {
            ClientSecretCredential cred = new ClientSecretCredential(tenantid, clientId, clientSecret);
            var client = new LogsQueryClient(cred);
            List<userTable> listUser = new List<userTable>();
            log.LogInformation("inside");
            try
            {
                
                //Cipher timegenerated = 30 last call = 2
                //Prod timegenerated = 180 Last call = 60 days
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    workspaceId,
                    "SigninLogs | where TimeGenerated > ago(30d) | where UserPrincipalName != UserId | summarize LastCall = max(TimeGenerated) by UserDisplayName, UserId | where LastCall < ago(2d) | order by LastCall asc",
                    new QueryTimeRange(TimeSpan.FromDays(30)));

                LogsTable table = response.Value.Table;

                foreach (var row in table.Rows)
                {
                    log.LogInformation(row["UserDisplayName"] + " " + row["LastCall"]);
                    listUser.Add(new userTable()
                    {
                        Id = row["UserId"].ToString(),
                        UPN = row["UserDisplayName"].ToString(),
                        LastCall = row["LastCall"].ToString()
                    });

                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);

            }
            return listUser;
        }
    }
}
