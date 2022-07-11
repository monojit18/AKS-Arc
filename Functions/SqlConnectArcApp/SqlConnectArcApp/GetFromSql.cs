using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace SqlConnectArcApp
{
    public static class GetFromSql
    {

        private static string kSqlConnectionString = Environment.GetEnvironmentVariable("SQLConnectionString");

        [FunctionName("GetProducts")]
        public static async Task<IActionResult> GetProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/products")]
            HttpRequest req, ILogger log)
        {

            var productsList = new List<Products>();

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();
                    var queryString = "Select * from Products";

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {

                            while (sqlDataReader.Read())
                            {
                                var products = new Products()
                                {

                                    ProductID = sqlDataReader["ProductID"].ToString(),
                                    ProductName = sqlDataReader["ProductName"].ToString(),
                                    Price = Double.Parse(sqlDataReader["Price"].ToString()),
                                    ProductDescription = sqlDataReader["ProductDescription"].ToString(),
                                    Quantity = float.Parse(sqlDataReader["Quantity"].ToString())

                                };
                                productsList.Add(products);
                            }
                        }
                    }
                }

            }
            catch(Exception ex)
            {

                log.LogInformation(ex.Message);

            }
            var responseMessage = JsonConvert.SerializeObject(productsList);
            log.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetReturns")]
        public static async Task<IActionResult> GetReturns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/returns")]
            HttpRequest req, ILogger log)
        {

            var returnsList = new List<Returns>();

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();
                    var queryString = "Select * from Returns";

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {

                            while (sqlDataReader.Read())
                            {
                                var products = new Returns()
                                {

                                    ProductID = sqlDataReader["ProductID"].ToString(),
                                    ProductName = sqlDataReader["ProductName"].ToString(),                                    
                                    Reason = sqlDataReader["Reason"].ToString(),
                                    Quantity = float.Parse(sqlDataReader["Quantity"].ToString())

                                };
                                returnsList.Add(products);
                            }
                        }
                    }
                }

            }
            catch(Exception ex)
            {

                log.LogInformation(ex.Message);

            }
            var responseMessage = JsonConvert.SerializeObject(returnsList);
            log.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult(responseMessage);
        }
    }
}
