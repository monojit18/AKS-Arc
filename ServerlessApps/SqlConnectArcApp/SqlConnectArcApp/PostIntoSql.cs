using System;
using System.IO;
using System.Net.Http;
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

        [FunctionName("PostIntoSql")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")]
            HttpRequest req, ILogger log)
        {

            var messageString = "OK";
            var reader = new StreamReader(req.Body);            
            var body = await reader.ReadToEndAsync();
            var products = JsonConvert.DeserializeObject<Products>(body);

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();
                    var queryString = $"Insert into Products (ProductID, ProductName, Price, ProductDescription) Values (@param1, @param2, @param3, @param4)";                    

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        sqlCommand.Parameters.Add("@param1", SqlDbType.Int).Value = products.ProductID;
                        sqlCommand.Parameters.Add("@param2", SqlDbType.VarChar).Value = products.ProductName;
                        sqlCommand.Parameters.Add("@param3", SqlDbType.Money).Value = products.Price;
                        sqlCommand.Parameters.Add("@param4", SqlDbType.VarChar).Value = products.ProductDescription;
                        sqlCommand.CommandType = CommandType.Text;

                        await sqlCommand.ExecuteNonQueryAsync();

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
