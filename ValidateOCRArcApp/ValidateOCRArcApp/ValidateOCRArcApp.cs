using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using ValidateOCRArcApp.Models;

namespace ValidateOCRArcApp
{
    public static class ValidateOCRArcApp
    {

        private static CloudBlockBlob GetBlobReference(string containerNameString,
                                                       string imageNameString)
        {

            CloudStorageAccount cloudStorageAccount = null;            
            var connectionString = Environment.GetEnvironmentVariable("OCR_INFO_STORAGE");
            var couldParse = CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount);
            if (couldParse == false)
                return null;

            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var blobContainerReference = cloudBlobClient.GetContainerReference(containerNameString);
            var blobReference = blobContainerReference.GetBlockBlobReference(imageNameString);
            return blobReference;

        }

        private static async Task UploadImageToBlobAsync(byte[] uploadBytesArray,
                                                         string imageNameString)
        {

            var containerNameString = Environment.GetEnvironmentVariable("APPROVED_BLOB_NAME");
            var blobReference = GetBlobReference(containerNameString, imageNameString);
            await blobReference.UploadFromByteArrayAsync(uploadBytesArray, 0,
                                                         uploadBytesArray.Length);

        }

        [FunctionName("blobInfo")]
        public static async Task<BlobInfoModel> GetBlobInfoAsync([ActivityTrigger]                                                                   
                                                                 string imageNameString)
        {

            var containerNameString = Environment.GetEnvironmentVariable("VALIDATE_BLOB_NAME");
            var blobReference = GetBlobReference(containerNameString, imageNameString);
            MemoryStream blobStream = new MemoryStream();

            await blobReference.DownloadToStreamAsync(blobStream);
            var blobContents = blobStream.ToArray();

            var blobInfoModel = new BlobInfoModel()
            {

                BlobContents = blobContents,
                ImageName = imageNameString                

            };

            return blobInfoModel;

        }

        [FunctionName("parseOCR")]
        public static async Task<string> ParseOCRAsync([ActivityTrigger]
                                                        BlobInfoModel blobInfoModel,
                                                        ILogger logger)
        {

            var client = new HttpClient();
            var apiKeyString = Environment.GetEnvironmentVariable("OCR_API_KEY");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKeyString);

            var content = new ByteArrayContent(blobInfoModel?.BlobContents);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var ocrResponse = await client.PostAsync(Environment.GetEnvironmentVariable("OCR_URL"),
                                                     content);
            var parsedOCR = await ocrResponse.Content.ReadAsStringAsync();
            logger.LogInformation($"OCR = {parsedOCR}");
            return parsedOCR;

        }

        [FunctionName("sendForApproval")]
        public static async Task UploadBlobAsync([ActivityTrigger] ApprovalModel approvalModel,
                                                [Queue("ocrinfoqueue",
                                                 Connection = "OCR_INFO_STORAGE")]
                                                IAsyncCollector<CloudQueueMessage>
                                                cloudQueueMessageCollector,
                                                ILogger logger)
        {

            var approvalModelString = JsonConvert.SerializeObject(approvalModel);
            logger.LogInformation($"Approval Model = {approvalModelString}");
            var cloudQueueMessage = new CloudQueueMessage(approvalModelString);

            await cloudQueueMessageCollector.AddAsync(cloudQueueMessage);

        }

        [FunctionName("postApproval")]
        public static async Task PostApprovalAsync([ActivityTrigger]
                                                    UploadImageModel uploadImageModel,
                                                    ILogger logger)
        {

            var isApproved = uploadImageModel.IsApproved;
            logger.LogInformation($"Approved:{isApproved}");

            if (isApproved == true)
                await UploadImageToBlobAsync(uploadImageModel.BlobContents,
                                             uploadImageModel.ImageName);

        }

        [FunctionName("processBlob")]
        public static async Task ProcessBlobContents([OrchestrationTrigger]
                                                     IDurableOrchestrationContext context)
        {

            var blobInfoModelsList = context.GetInput<List<BlobInfoModel>>();
            var tasks = blobInfoModelsList.Select(async (BlobInfoModel blobInfoModel) =>
            {

                var imageNameString = blobInfoModel?.ImageName;
                blobInfoModel = await context.CallActivityAsync<BlobInfoModel>("blobInfo",
                                                                               imageNameString);

                var parsedOCRString = await context.CallActivityAsync<string>("parseOCR",
                                                                              blobInfoModel);

                var ocrInfoModel = JsonConvert.DeserializeObject<OCRInfoModel>(parsedOCRString);
                var approvalModel = new ApprovalModel()
                {

                    InstanceId = context.InstanceId,
                    Language = ocrInfoModel.Language

                };

                await context.CallActivityAsync("sendForApproval", approvalModel);

                using (var cts = new CancellationTokenSource())
                {

                    var dueTime = context.CurrentUtcDateTime.AddMinutes(3);
                    var timerTask = context.CreateTimer(dueTime, cts.Token);
                    var approvalTask = context.WaitForExternalEvent<bool>("Approval");
                    var completedTask = await Task.WhenAny(approvalTask, timerTask);

                    var isApproved = approvalTask.Result;
                    var uploadImageModel = new UploadImageModel()
                    {

                        IsApproved = isApproved,
                        BlobContents = blobInfoModel?.BlobContents,
                        ImageName = blobInfoModel.ImageName

                    };

                    await context.CallActivityAsync("postApproval", uploadImageModel);

                }

            }).ToArray();

            await Task.WhenAll(tasks);           

        }

        [FunctionName("blob")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "post",
                                        Route = null)] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient starter, ILogger logger)
        {

            var contentString = await request.Content.ReadAsStringAsync();
            var imageNamesList = JsonConvert.DeserializeObject<List<string>>(contentString);
            var blobInfoModelsList = new List<BlobInfoModel>();

            foreach (var imageName in imageNamesList)
            {

                var blobInfoModel = new BlobInfoModel()
                {

                    ImageName = imageName

                };

                blobInfoModelsList.Add(blobInfoModel);

            }
            

            string instanceId = await starter.StartNewAsync("processBlob", blobInfoModelsList);
            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        }        
    }
}
