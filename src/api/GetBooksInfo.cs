using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace api
{
    public static class GetBooksInfo
    {
        [FunctionName("GetBooksInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("StorageConnection");
        string containerName = Environment.GetEnvironmentVariable("ContainerName");

        // Get latest blob with extension ".json"
        var blobClient = new BlobContainerClient(connectionString, containerName);
        BlobItem latestBlob = null;
        await foreach (var blob in blobClient.GetBlobsAsync())
        {
            if (blob.Name.EndsWith(".json") && (latestBlob == null || blob.Properties.CreatedOn > latestBlob.Properties.CreatedOn))
            {
                latestBlob = blob;
            }
        }

        if (latestBlob != null)
        {
            System.Console.WriteLine($"Found latest blob: {latestBlob.Name}");
            var blob2 = blobClient.GetBlobClient(latestBlob.Name);
            var blobDownloadInfo = await blob2.DownloadAsync();
            var jsonResponse = await new StreamReader(blobDownloadInfo.Value.Content).ReadToEndAsync();
            
            var parsedJson = JsonConvert.DeserializeObject(jsonResponse);
            string formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

            return new OkObjectResult(formattedJson);
        }
        else
        {
            return new NotFoundResult();
        }
        }
    }
}
