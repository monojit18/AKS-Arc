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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "student")]
            HttpRequest req, ILogger log)
        {

            var messageString = "OK";
            var reader = new StreamReader(req.Body);            
            var body = await reader.ReadToEndAsync();
            var student = JsonConvert.DeserializeObject<Student>(body);

            try
            {

                using (var sqlConnection = new SqlConnection(kSqlConnectionString))
                {

                    await sqlConnection.OpenAsync();
                    var queryString = $"Insert into Students (Id, Name, Age) Values (@param1, @param2, @param3)";                    

                    using (var sqlCommand = new SqlCommand(queryString, sqlConnection))
                    {

                        sqlCommand.Parameters.Add("@param1", SqlDbType.VarChar).Value = student.Id;
                        sqlCommand.Parameters.Add("@param2", SqlDbType.VarChar).Value = student.Name;
                        sqlCommand.Parameters.Add("@param3", SqlDbType.Int).Value = student.Age;
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
