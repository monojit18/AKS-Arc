using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ZipImagesArcApp
{
    public static class SendZipEvent
    {

        private static async Task<HttpResponseMessage> SendEventAsync(string zipFileNameString,
                                                                      ILogger logger)
        {

            var cl = new HttpClient();
            var zipEventModel = new ZipEventModel()
            {

                SpecVersion = "1.0",
                Type = "zipCreated",
                Source = "arctest/zip",
                Id = "eventId-n",
                Time = "2022-01-20T11:13:08+05:30",
                Subject = "images/zip",
                DataSchema = "1.0",
                ZipModel = new ZipModel()
                {

                    Zip = zipFileNameString

                }
            };
            var zipEventModelsList = new List<ZipEventModel>()
            {
                zipEventModel
            };

            var zipEventURL = Environment.GetEnvironmentVariable("ZIP_EVENT_URL");
            var zipEventKey = Environment.GetEnvironmentVariable("ZIP_EVENT_KEY");

            var zipEventModelString = JsonConvert.SerializeObject(zipEventModelsList);
            var content = new StringContent(zipEventModelString, Encoding.UTF8,
                                            "application/cloudevents-batch+json");
            try
            {

                cl.DefaultRequestHeaders.Add("aeg-sas-key", zipEventKey);                

            }
            catch(Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            

            var response = await cl.PostAsync(zipEventURL, content);
            logger.LogInformation(response.Content.ToString());
            return response;

        }

        [FunctionName("send")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "event/{zip}")]
            HttpRequest request, string zip, ILogger logger)
        {
            
            var resp = await SendEventAsync(zip, logger);
            return new OkObjectResult(resp);

        }
    }
}
