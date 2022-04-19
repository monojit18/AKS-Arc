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

        [FunctionName("GetFromSql")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "student")]
            HttpRequest req, ILogger log)
        {

            var studensList = new List<Student>();

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();
                    var queryString = "Select * from Student";

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {

                            while (sqlDataReader.Read())
                            {

                                var student = new Student()
                                {

                                    Name = sqlDataReader["Name"].ToString(),
                                    Age = Int32.Parse(sqlDataReader["Age"].ToString())

                                };

                                studensList.Add(student);


                            }
                        }
                    }
                }

            }
            catch(Exception ex)
            {

                log.LogInformation(ex.Message);

            }
            

            var responseMessage = JsonConvert.SerializeObject(studensList);
            log.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult(responseMessage);

        }
    }
}
