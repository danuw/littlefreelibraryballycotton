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

namespace api
{
    public static class UploadPhotoToBlobStorage
    {
        [FunctionName("FileUpload")]
        public static async Task < IActionResult > Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log) {
                System.Console.WriteLine("File Upload start");
            string Connection = Environment.GetEnvironmentVariable("StorageConnection");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            Stream myBlob = new MemoryStream();
            System.Console.WriteLine("getting file");
            var file = req.Form.Files["fileToUpload"];
            System.Console.WriteLine("file found");
            myBlob = file.OpenReadStream();
            System.Console.WriteLine("Blob upload");
            var blobClient = new BlobContainerClient(Connection, containerName);
            var newFileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss-")}{file.FileName}";
            System.Console.WriteLine($"Uploading to {newFileName}");
            var blob = blobClient.GetBlobClient(newFileName);
            await blob.UploadAsync(myBlob);
            System.Console.WriteLine($"Blob uploaded {newFileName}");
            return new OkObjectResult($"{{\"message\" : \"file uploaded successfully to {newFileName}\"}}");
        }
    }
}
