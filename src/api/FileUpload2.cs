using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;

using Azure.Storage;
using Azure.Storage.Sas;

namespace api
{
    public static class UploadPhotoToBlobStorage2
    {
        [FunctionName("FileUpload2")]
        public static async Task < IActionResult > Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log) {
                System.Console.WriteLine("File Upload start");
            string connectionString = Environment.GetEnvironmentVariable("StorageConnection");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            Stream myBlob = new MemoryStream();
            System.Console.WriteLine("getting file");
            var file = req.Form.Files["fileToUpload"];
            System.Console.WriteLine("file found");
            myBlob = file.OpenReadStream();
            System.Console.WriteLine("Blob upload");
            var blobClient = new BlobContainerClient(connectionString, containerName);
            var newFileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss-")}{file.FileName}";
            
            // Upload
            System.Console.WriteLine($"Uploading to {newFileName}");
            var blob = blobClient.GetBlobClient(newFileName);
            await blob.UploadAsync(myBlob);
            System.Console.WriteLine($"Blob uploaded {newFileName}");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            string accountKey = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString }["AccountKey"].ToString();
            // get the full link for the image
            //var imgUrl = GenerateSasToken(blobServiceClient, containerName, newFileName, accountKey);


            System.Console.WriteLine("ProcessImageUpload start");
            try {
                // Get image Text
                string textContext = await ProcessImageUpload.GetImageText(newFileName, log);
            
                System.Console.WriteLine("GetBookSummaries start");
                // Get Summaries from OpenAI
                string summaries = await ProcessImageUpload.GetBookSummaries(textContext, log);
                
                // upload "textContent" to blob again
                var blobClient2 = new BlobContainerClient(connectionString, containerName);
                var newFileName2 = $"{DateTime.Now.ToString("yyyyMMddHHmmss-")}{file.FileName}.json";
                var blob2 = blobClient2.GetBlobClient(newFileName2);
                await blob2.UploadAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(summaries)));
                System.Console.WriteLine($"Blob uploaded {newFileName2}");


                return new OkObjectResult($"{{\"message\" : \"files uploaded successfully to {newFileName2}\"}}");

                // return new ImageContent
                // {
                //     PartitionKey = "Images",
                //     RowKey = Guid.NewGuid().ToString(),
                //     Text = textContext
                // };
            }
            catch(Exception ex){
                System.Console.WriteLine(ex.Message); 
            }

            return new OkObjectResult($"{{\"message\" : \"files uploaded successfully to {newFileName} \"}}");
        }

    public static string GenerateSasToken(BlobServiceClient blobServiceClient, string containerName, string blobName, string accountKey)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        BlobSasBuilder sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            IPRange = new SasIPRange(IPAddress.None, IPAddress.None),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, accountKey);

        string sasToken = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();

        return blobClient.Uri + "?" + sasToken;
    }
    }
}
