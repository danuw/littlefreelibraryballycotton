using System;
using System.Net;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

using System.Text;
using System.Threading;
using System.Text.Json;

using Azure.AI.OpenAI;
using System.Text.Json.Serialization;
using System.Net.Http;


using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;


namespace api
{
    public static class ProcessImageUpload
    {
        private static readonly string OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

        // optional - setting OPEN_API_URL is not required and will use default api URL
        private static readonly string OPENAI_API_URL = Environment.GetEnvironmentVariable("OPENAI_API_URL") ?? "https://api.openai.com";



        // source : https://learn.microsoft.com/en-us/azure/storage/blobs/blob-upload-function-trigger?tabs=azure-portal

        // Azure Function name and output Binding to Table Storage
        [FunctionName("ProcessImageUpload")]
        [return: Table("ImageText", Connection = "StorageConnection")]
        // Trigger binding runs when an image is uploaded to the blob container below
        public static async Task<ImageContent> Run([BlobTrigger("imageanalysis/{name}", Connection = "StorageConnection")]Stream myBlob, string name, ILogger log)
        {
            System.Console.WriteLine("ProcessImageUpload start");
            try {
                // Get image Text
                string textContext = await GetImageText(name, log);
            
                System.Console.WriteLine("GetBookSummaries start");
                // Get Summaries from OpenAI
                string summaries = await GetBookSummaries(textContext, log);
                
                return new ImageContent
                {
                    PartitionKey = "Images",
                    RowKey = Guid.NewGuid().ToString(),
                    Text = textContext
                };
            }
            catch(Exception ex){
                System.Console.WriteLine(ex.Message); 
            }
            return null;
        }

        internal static async Task<string> GetBookSummaries(string prompt, ILogger log)
        {
            //try{
                var completion = await OpenAICreateCompletion("text-davinci-003", GeneratePrompt(prompt), 0.1m, 2000, log);
                System.Console.WriteLine(JsonSerializer.Serialize(completion));
                if (completion.error!=null){
                    var message = JsonSerializer.Serialize(completion.error);//billing_not_active
                    System.Console.WriteLine(message);
                    throw new Exception(message);
                }

                var choice = completion.choices[0];
                log.LogInformation($"Completions result: {choice}");

               var responseMessage = System.Text.Json.JsonSerializer.Serialize(choice.text);
               if (responseMessage.IndexOf("{") > -1 && responseMessage.LastIndexOf("}") > responseMessage.IndexOf("{") ){
                    // substring from the {
                    responseMessage = responseMessage.Substring(responseMessage.IndexOf("{"));

                    // substring to remove last index of }
                    responseMessage = responseMessage.Substring(0, responseMessage.LastIndexOf("}")+1);
                    responseMessage = responseMessage.Replace("\\n", "");
                    responseMessage = System.Text.RegularExpressions.Regex.Unescape(responseMessage);
               }
               System.Console.WriteLine($"Response Message: {responseMessage}");

               
            // }   
            // catch (Exception ex)
            // {
            //     var message = $"Exception thrown: {ex.ToString()}";
            //     response = new ObjectResult(message)
            //         {
            //             StatusCode = (int)HttpStatusCode.InternalServerError
            //         };
            //         return response;
            // }
            return responseMessage;
        }

        internal static async Task<string> GetImageText(string name, ILogger log)
        {
            // Get connection configurations
            string subscriptionKey = Environment.GetEnvironmentVariable("ComputerVisionKey");
            string endpoint = Environment.GetEnvironmentVariable("ComputerVisionEndpoint");
            //https://littlelibballycotton.blob.core.windows.net/imageanalysis?sv=2021-04-10&st=2023-09-05T10%3A50%3A05Z&se=2023-09-06T10%3A50%3A05Z&sr=c&sp=rl&sig=mXK0FLKI6ZaHqejAEpSNVPPx%2BweXV5gF1IBxNCKMeU8%3D
            string imgUrl = "";//$"https://{Environment.GetEnvironmentVariable("StorageAccountName")}.blob.core.windows.net/imageanalysis/{name}?sv=2021-04-10&st=2023-09-05T10%3A50%3A05Z&se=2023-09-06T10%3A50%3A05Z&sr=c&sp=rl&sig=mXK0FLKI6ZaHqejAEpSNVPPx%2BweXV5gF1IBxNCKMeU8%3D";
            //imgUrl = $"https://127.0.0.1:10000/devstoreaccount1/imageanalysis/{name}";
            string connectionString = Environment.GetEnvironmentVariable("StorageConnection");

             BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            string accountKey = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString }["AccountKey"].ToString();
            // get the full link for the image
            imgUrl = GenerateSasToken(blobServiceClient, "imageanalysis", name, accountKey);
            System.Console.WriteLine($"Image URL: {imgUrl}");
            System.Console.WriteLine($"Computer Vision Key: {subscriptionKey}");
            System.Console.WriteLine($"Computer Vision Endpoint: {endpoint}");
            System.Console.WriteLine($"Computer Vision client creating...");
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint };

            System.Console.WriteLine($"Computer Vision client created.");
            // Get the analyzed image contents
            var textContext = await AnalyzeImageContent(client, imgUrl);
            System.Console.WriteLine("Analysis came back successfully with ");
            return textContext;
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
        internal static string GeneratePrompt(string prompt)
        {
            // Freeform question
            //var promptTemplate = prompt; 

            // Chat
            //var promptTemplate = `The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, and very friendly.\n\nHuman: Hello, who are you?\nAI: I am an AI created by OpenAI. How can I help you today?\nHuman: ${capitalizedPrompt}` 

            // Classification
            //var promptTemplate = `The following is a list of companies and the categories they fall into:\n\nApple, Facebook, Fedex\n\nApple\nCategory: ` 

            // Natural language to Python
            //var promptTemplate = '\"\"\"\n1. Create a list of first names\n2. Create a list of last names\n3. Combine them randomly into a list of 100 full names\n\"\"\"'

            var promptTemplate = $@"Perform the following actions: 
1 - In the following text delimited by triple \
backticks, identify names of books and their authors in an array of books respectively in 'name' and 'author' fields
2 - Summarise the books that were recognised
3 - Output a json object that contains the following \
keys: name, author, tags, summary.

The final output should have a valid JSON format as follows:
```
{{
    ""books"": [
        {{
            ""name"": ""The Lord of the Rings"",
            ""author"": """",
            ""tags"": """",
            ""summary"": """"}},
    ]

}}
```

Text:
```
{prompt}
```
";

            return promptTemplate;
        }


        public class ImageContent
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string Text { get; set; }
        }
        internal static async Task<string> AnalyzeImageContent(ComputerVisionClient client, string urlFile)
        {
            System.Console.WriteLine($"Analyzing {urlFile}...");
            // Analyze the file using Computer Vision Client
            var textHeaders = await client.ReadAsync(urlFile);
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            System.Console.WriteLine($"Operation Location: {operationLocation}");
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Read back the results from the analysis request
            ReadOperationResult results;
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            var textUrlFileResults = results.AnalyzeResult.ReadResults;

            // Assemble into readable string
            StringBuilder text = new StringBuilder();
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    text.AppendLine(line.Text);
                }
            }
            System.Console.WriteLine($"Text: {text.ToString()}");
            return text.ToString();
        }

        internal static async Task<CompletionResponse> OpenAICreateCompletion(string model, string prompt, decimal temperature, int max_tokens, ILogger logger)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            // Adding app id as part of the header
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OPENAI_API_KEY}");

            var completion = CompletionRequest.CreateDefaultCompletionRequest(model, prompt, temperature, max_tokens);
            logger.LogInformation($"Completion Request Body: {completion}");
           
            var completionJson = System.Text.Json.JsonSerializer.Serialize<CompletionRequest>(completion);
            logger.LogInformation($"Completion Request Body Json: {completionJson}");

            var content = new StringContent(completionJson, Encoding.UTF8, "application/json");

            var baseUrl = $"{OPENAI_API_URL}/v1/completions";
            var response = await client.PostAsync(baseUrl, content);
            logger.LogInformation($"POST to REST API at: {baseUrl}");

            logger.LogInformation("Response code: \n" + response.StatusCode);       
            var completionResponse = System.Text.Json.JsonSerializer.Deserialize<CompletionResponse>(response.Content.ReadAsStream());

            return completionResponse;     
        }


    }

}