using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace UrlShortenerApp;

public static class Function
{
    [FunctionName("Function")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request, ILogger logger)
    {
        logger.LogInformation("[{RequestMethod}]: {NamespaceName}.{FunctionName}: HTTP trigger received a request", request.Method, nameof(Function), nameof(Run));

        string name = request.Query["name"];
        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name ??= data?.name;

        var responseMessage = string.IsNullOrEmpty(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or request body to get personalized response."
            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        logger.LogInformation("[{RequestMethod}]: {NamespaceName}.{FunctionName}: successfully processed a request and returned 200 OkObjectResult", request.Method, nameof(Function), nameof(Run));
        return new OkObjectResult(responseMessage);
    }
}