using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using ValidateOCRArcApp.Models;

namespace ValidateOCRArcApp
{
    public static class ProcessOCRQueue
    {
        [FunctionName("processOCRQueue")]
        public static async Task Run([QueueTrigger("ocrinfoqueue", Connection = "OCR_INFO_STORAGE")]
                                     CloudQueueMessage cloudQueueMessage,
                                     [DurableClient] IDurableOrchestrationClient client,
                                     ILogger logger)
        {

            var queueMessageString = cloudQueueMessage.AsString;
            logger.LogInformation($"Queue Message:{queueMessageString}");

            var approvalModel = JsonConvert.DeserializeObject<ApprovalModel>(queueMessageString);
            var languageString = approvalModel.Language;

            //shouldApprove
            bool shouldApprove = (string.Compare(languageString, "unk",
                                  StringComparison.CurrentCultureIgnoreCase) != 0);

            await client.RaiseEventAsync(approvalModel.InstanceId, "Approval", shouldApprove);
        }
    }
}
