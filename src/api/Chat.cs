using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;


using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using Azure.AI.OpenAI;
using Newtonsoft.Json;
using System.Net.Http;
using Azure;
using Microsoft.Azure.Documents;

namespace api
{
    public static class Chat
    {
                // must export and set OPEN_API_KEY using https://platform.openai.com/account/api-keys
        private static readonly string OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

        // optional - setting OPEN_API_URL is not required and will use default api URL
        private static readonly string OPENAI_API_URL = Environment.GetEnvironmentVariable("OPENAI_API_URL") ?? "https://api.openai.com";


        [FunctionName("Chat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            IActionResult response;
            string responseMessage= string.Empty;
            System.Console.WriteLine(OPENAI_API_KEY);
            System.Console.WriteLine(OPENAI_API_URL);

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;

            if (string.IsNullOrWhiteSpace(OPENAI_API_KEY)) {
                    log.LogError("Missing env var OPENAI_API_KEY must be set.");
                    var errorMessage = "Missing env var OPENAI_API_KEY must be set.";
                    response = new ObjectResult(errorMessage)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                    return response;
            }

            try {
                System.Console.WriteLine("Request:");
                var requestBodyJson = await new StreamReader(req.Body).ReadToEndAsync();
                System.Console.WriteLine(requestBodyJson);
                System.Console.WriteLine("-----");
                var requestBody = System.Text.Json.JsonSerializer.Deserialize<PromptRequestBody>(requestBodyJson);

                string prompt = ""; 

                if ((requestBody == null)||(requestBody.prompt == null)) {
                    var message = "Missing value for prompt in request body.";
                    log.LogError(message);
                    response = new ObjectResult(message)
                    {
                        StatusCode = (int)HttpStatusCode.NotAcceptable
                    };

                    return response;
                } else {
                    prompt = requestBody.prompt;
                }
                System.Console.WriteLine(prompt);
                // call OpenAI ChatGPT API here with desired chat prompt
                var completion = await OpenAICreateCompletion("text-davinci-003", GeneratePrompt(prompt), 0.1m, 250, log);
                System.Console.WriteLine(JsonConvert.SerializeObject(completion));
                if (completion.error!=null){
                    var message = JsonConvert.SerializeObject(completion.error);//billing_not_active
                    System.Console.WriteLine(message);
                    throw new Exception(message);
                }

                var choice = completion.choices[0];
                log.LogInformation($"Completions result: {choice}");

                responseMessage = System.Text.Json.JsonSerializer.Serialize<Choice>(choice);
            }   
            catch (Exception ex)
            {
                var message = $"Exception thrown: {ex.ToString()}";
                response = new ObjectResult(message)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                    return response;
            }

            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //     : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        
        // [FunctionName("chat")]
        // public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        //     FunctionContext executionContext)
        // {
        //     var logger = executionContext.GetLogger("chat_function");
        //     logger.LogInformation("message logged");

        //     HttpResponseData response;

        //     if (OPENAI_API_KEY == "") {
        //             logger.LogError("Missing env var OPENAI_API_KEY must be set.");
        //             response = req.CreateResponse(HttpStatusCode.InternalServerError);
        //             await response.WriteStringAsync("Missing env var OPENAI_API_KEY must be set."); 
        //             return response;
        //     }

        //     try {
        //         var requestBodyJson = await new StreamReader(req.Body).ReadToEndAsync();

        //         var requestBody = JsonSerializer.Deserialize<PromptRequestBody>(requestBodyJson);

        //         string prompt = ""; 

        //         if ((requestBody == null)||(requestBody.prompt == null)) {
        //             logger.LogError("Missing value for prompt in request body.");
        //             response = req.CreateResponse(HttpStatusCode.NotAcceptable);
        //             await response.WriteStringAsync("Missing value for prompt in request body.");

        //             return response;
        //         } else {
        //             prompt = requestBody.prompt;
        //         }
            
        //         // call OpenAI ChatGPT API here with desired chat prompt
        //         var completion = await OpenAICreateCompletion("text-davinci-003", GeneratePrompt(prompt), 0.9m, 64, logger);

        //         var choice = completion.choices[0];
        //         logger.LogInformation($"Completions result: {choice}");

        //         response = req.CreateResponse(HttpStatusCode.OK);
        //         await response.WriteAsJsonAsync<Choice>(choice);
        //     }   
        //     catch (Exception ex)
        //     {
        //         logger.LogError($"Exception thrown: {ex.ToString()}");
        //         response = req.CreateResponse(HttpStatusCode.InternalServerError);
        //     }

        //     return response;
        // }

        // Generates prompts from templates -- which is important to set some context and training 
        // up front in addition to user driven input
        static string GeneratePrompt(string prompt)
        {
            // Freeform question
            var promptTemplate = prompt; 

            // Chat
            //var promptTemplate = `The following is a conversation with an AI assistant. The assistant is helpful, creative, clever, and very friendly.\n\nHuman: Hello, who are you?\nAI: I am an AI created by OpenAI. How can I help you today?\nHuman: ${capitalizedPrompt}` 

            // Classification
            //var promptTemplate = `The following is a list of companies and the categories they fall into:\n\nApple, Facebook, Fedex\n\nApple\nCategory: ` 

            // Natural language to Python
            //var promptTemplate = '\"\"\"\n1. Create a list of first names\n2. Create a list of last names\n3. Combine them randomly into a list of 100 full names\n\"\"\"'

            return promptTemplate;
        }


        static async Task<CompletionResponse> OpenAICreateCompletion(string model, string prompt, decimal temperature, int max_tokens, ILogger logger)
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

    public record PromptRequestBody(string prompt);

    public record CompletionRequest(string model, 
                                    string prompt,
                                    decimal temperature,
                                    int max_tokens,
                                    decimal top_p,
                                    decimal frequency_penalty,
                                    decimal presence_penalty
                                    )
    {

        public static CompletionRequest CreateDefaultCompletionRequest(string model, string prompt, decimal temperature, int max_tokens) {
            return new CompletionRequest(model, prompt, temperature, max_tokens, 1.0m, 0.0m, 0.0m);
        }

        public static CompletionRequest CreateDefaultCompletionRequest(string prompt) {
            return new CompletionRequest("text-davinci-003", prompt, 0.9m, 64, 1.0m, 0.0m, 0.0m);
        }

        public static CompletionRequest CreateDefaultCompletionRequest() {
            return CompletionRequest.CreateDefaultCompletionRequest(prompt: "");
        }
    }

    public record CompletionResponse(string id, 
                                    [property: JsonPropertyName("object")] string _object,
                                    int created,    
                                    string model,
                                    Choice[] choices,
                                    Usage usage,
                                    Error error
                                    );

    public record Choice(string text, int index, string logprobs, string finish_reason);

    public record Error(string message, string type, string param, string code);

    public record Usage(int prompt_tokens, int completion_tokens, int total_tokens);


// curl https://api.openai.com/v1/completions \
//   -H "Content-Type: application/json" \
//   -H "Authorization: Bearer $OPENAI_API_KEY" \
//   -d '{
//   "model": "text-davinci-003",
//   "prompt": "Summarize this for a second-grade student:\n\nJupiter is the fifth planet from the Sun and the largest in the Solar System. It is a gas giant with a mass one-thousandth that of the Sun, but two-and-a-half times that of all the other planets in the Solar System combined. Jupiter is one of the brightest objects visible to the naked eye in the night sky, and has been known to ancient civilizations since before recorded history. It is named after the Roman god Jupiter.[19] When viewed from Earth, Jupiter can be bright enough for its reflected light to cast visible shadows,[20] and is on average the third-brightest natural object in the night sky after the Moon and Venus.",
//   "temperature": 0.7,
//   "max_tokens": 64,
//   "top_p": 1.0,
//   "frequency_penalty": 0.0,
//   "presence_penalty": 0.0
// }'
//
//{"id":"cmpl-6nORpx9l54RDOD25zbo9b0fTmi2Ri","object":"text_completion","created":1677230069,"model":"text-davinci-003","choices":[{"text":"\n\nJupiter is the fifth planet from the Sun and the biggest in our Solar System. It is very bright and can be seen in the night sky. It is named after the Roman god Jupiter. It is the third brightest object in the night sky after the Moon and Venus.","index":0,"logprobs":null,"finish_reason":"stop"}],"usage":{"prompt_tokens":151,"completion_tokens":57,"total_tokens":208}}

}