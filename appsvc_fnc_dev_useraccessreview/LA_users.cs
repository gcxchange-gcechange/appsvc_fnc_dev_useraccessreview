using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Graph;
using Azure.Monitor.Query.Models;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure;

namespace appsvc_fnc_dev_useraccessreview
{
    class LA_users
    {
        public async Task<List<userTable>> LA_usersData (ILogger log)
        {
            IConfiguration config = new ConfigurationBuilder()

            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            string clientSecret = config["clientSecret"];
            string clientId = config["clientId"];
            string tenantid = config["tenantid"];
            string workspaceId = config["workspaceId"];

            Auth auth = new Auth();
            var graphAPIAuth = auth.graphAuth(log);

            var PendingAcceptance = await getUsersPendingAcceptance(graphAPIAuth, log);
            var OldLogs = await getUsersOldLogs(tenantid, clientId, clientSecret, workspaceId, log);

            foreach (var item in PendingAcceptance)
            {
                OldLogs.Add(new userTable { Id = item.Id, UPN = item.UserPrincipalName, signinDate ="N/A" });
            }
            return OldLogs;
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
                    listUser.Add(new userTable()
                    {
                        Id = row["UserId"].ToString(),
                        UPN = row["UserDisplayName"].ToString(),
                        signinDate = row["LastCall"].ToString()
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
