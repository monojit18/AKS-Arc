using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ZipImagesArcApp
{
    public static class ZipImagesArcApp
    {

        private static CloudBlockBlob GetBlobReference(string containerNameString,
                                                        string imageNameString)
        {

            CloudStorageAccount cloudStorageAccount = null;
            var connectionString = Environment.GetEnvironmentVariable("BIG_IMAGE_STORAGE");
            var couldParse = CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount);
            if (couldParse == false)
                return null;

            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var blobContainerReference = cloudBlobClient.GetContainerReference(containerNameString);
            var blobReference = blobContainerReference.GetBlockBlobReference(imageNameString);
            return blobReference;

        }

        private static async Task<byte[]> DownloadImageFromBlobAsync(string imageNameString,
                                                                     ILogger logger)
        {

            var containerNameString = Environment.GetEnvironmentVariable("BIG_IMAGE_BLOB_NAME");
            var blobReference = GetBlobReference(containerNameString, imageNameString);

            var ms = new MemoryStream();
            await blobReference.DownloadToStreamAsync(ms);
            return ms.ToArray();

        }

        private static async Task ProcessZipAsync(string zipFileNameString, ILogger logger)
        {

            var cl = new HttpClient();
            var zm = new ZipModel()
            {

                Zip = zipFileNameString

            };

            var zipWorkflowURL = Environment.GetEnvironmentVariable("ZIP_WORKFLOW_URL");
            var zms = JsonConvert.SerializeObject(zm);
            var content = new StringContent(zms, Encoding.UTF8, "application/json");
            var response = await cl.PostAsync(zipWorkflowURL, content);
            logger.LogInformation(response.Content.ToString());

        }

        private static async Task UploadImageToBlobAsync(byte[] uploadBytesArray, ILogger logger)
        {

            var containerNameString = Environment.GetEnvironmentVariable("ZIP_IMAGE_BLOB_NAME");
            var timeString = DateTime.Now.Ticks.ToString();
            var zipImagePrefix = Environment.GetEnvironmentVariable("ZIP_IMAGE_PREFIX");
            var uploadFileNameString = $"{zipImagePrefix}{timeString}.zip";

            var blobReference = GetBlobReference(containerNameString, uploadFileNameString);

            await blobReference.UploadFromByteArrayAsync(uploadBytesArray, 0,
                                                            uploadBytesArray.Length);
            await ProcessZipAsync(uploadFileNameString, logger);

        }

        [FunctionName("zipImages")]        
        public static async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "post",
                                     Route = "zip")] HttpRequestMessage request, ILogger logger)
        {

            var contentString = await request.Content.ReadAsStringAsync();
            var imageNamesList = JsonConvert.DeserializeObject<List<string>>(contentString);

            using (var ms = new MemoryStream())
            {

                using (var zp = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {

                    foreach (var imageNameString in imageNamesList)
                    {

                        var bts = await DownloadImageFromBlobAsync(imageNameString, logger);
                        var ze = zp.CreateEntry(imageNameString);
                        using (var es = ze.Open())
                        {

                            using (var bw = new BinaryWriter(es))
                            {

                                bw.Write(bts);


                            }

                        }

                    }

                }

                await UploadImageToBlobAsync(ms.ToArray(), logger);

            }

        }
    }    
}
