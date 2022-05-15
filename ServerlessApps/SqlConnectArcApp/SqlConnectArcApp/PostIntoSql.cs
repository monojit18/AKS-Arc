using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace SqlConnectArcApp
{
    public static class PostIntoSql
    {

        private static string kSqlConnectionString = Environment.GetEnvironmentVariable("SQLConnectionString");
        private static HttpClient httpClient = new HttpClient();

        private static Returns CheckReturns(Products products)
        {
            
            bool shouldBeReturned = (products.Quantity > 500 || products.Quantity < 50);
            if (shouldBeReturned == false)
                return null;

            string reasonForReturn = string.Empty;
            if (products.Quantity > 500)
                reasonForReturn = "Quantity is too high";
            else if (products.Quantity < 50)
                reasonForReturn = "Quantity is too Low";

            var returns = new Returns()
            {

                ProductID = Guid.NewGuid().ToString(),
                ProductName = products.ProductName,
                Quantity = products.Quantity,
                Reason = reasonForReturn

            };

            return returns;
        }

        private static async Task<string> sendEmailAsync(Returns returns, ILogger log)
        {

            var logicAppCallbackUri = Environment.GetEnvironmentVariable("LOGICAPP_CALLBACK_URL");
            var logicAppPostUri = Environment.GetEnvironmentVariable("LOGICAPP_POST_URL");

            var responseMessage = await httpClient.PostAsync(logicAppCallbackUri, null);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            log.LogInformation($"Callback:{responseContent}");

            var callbackModel = JsonConvert.DeserializeObject<CallbackModel>(responseContent);
            var signature = callbackModel.Queries.Signature;
            logicAppPostUri = string.Format(logicAppPostUri, signature);
            log.LogInformation($"PostUri:{logicAppPostUri}");

            var body = JsonConvert.SerializeObject(returns);
            log.LogInformation($"Body:{body}");
            
            var bodyContent = new StringContent(body);
            bodyContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            responseMessage = await httpClient.PostAsync(logicAppPostUri, bodyContent);
            responseContent = await responseMessage.Content.ReadAsStringAsync();
            log.LogInformation($"ResponseContent:{responseContent}");
            return responseContent;
        }

        [FunctionName("PostIntoSql")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")]
            HttpRequest req, ILogger log)
        {

            var messageString = "OK";
            var reader = new StreamReader(req.Body);            
            var body = await reader.ReadToEndAsync();
            var products = JsonConvert.DeserializeObject<Products>(body);
            var returns = CheckReturns(products);

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();                    
                    var productInsertString = $"Insert into Products (ProductID, ProductName, Price, ProductDescription, Quantity) Values (@param1, @param2, @param3, @param4, @param5)";
                    var returnsInsertString = $"Insert into Returns (ProductID, ProductName, Quantity, Reason) Values (@param1, @param2, @param3, @param4)";
                    var queryString = (returns != null) ? returnsInsertString : productInsertString;

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        if (returns != null)
                        {
                            sqlCommand.Parameters.Add("@param1", SqlDbType.NVarChar).Value = returns.ProductID;
                            sqlCommand.Parameters.Add("@param2", SqlDbType.VarChar).Value = returns.ProductName;
                            sqlCommand.Parameters.Add("@param3", SqlDbType.Float).Value = returns.Quantity;
                            sqlCommand.Parameters.Add("@param4", SqlDbType.VarChar).Value = returns.Reason;                                                    
                        }
                        else
                        {                            
                            sqlCommand.Parameters.Add("@param1", SqlDbType.NVarChar).Value = Guid.NewGuid().ToString();
                            sqlCommand.Parameters.Add("@param2", SqlDbType.VarChar).Value = products.ProductName;
                            sqlCommand.Parameters.Add("@param3", SqlDbType.Money).Value = products.Price;
                            sqlCommand.Parameters.Add("@param4", SqlDbType.VarChar).Value = products.ProductDescription;
                            sqlCommand.Parameters.Add("@param5", SqlDbType.Float).Value = products.Quantity;
                        }
                        sqlCommand.CommandType = CommandType.Text;
                        await sqlCommand.ExecuteNonQueryAsync();

                        if (returns != null)
                        {                            
                            messageString = await sendEmailAsync(returns, log);
                        }                            
                    }
                }
            }
            catch(Exception ex)
            {

                log.LogInformation(ex.Message);
                messageString = ex.Message;

            }
            
            log.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult(messageString);
        }
    }
}
