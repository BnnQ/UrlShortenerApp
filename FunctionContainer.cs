#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using UrlShortenerApp.Models.Entities;
using UrlShortenerApp.Services.Abstractions;
using UrlShortenerApp.Utils.Extensions;

namespace UrlShortenerApp;

public class FunctionContainer
{
    private readonly TableClient tableClient;
    private readonly IUrlShortener urlShortener;
    private readonly QueueClient queueClient;
    private readonly ILogger<FunctionContainer> logger;

    public FunctionContainer(TableClient tableClient, IUrlShortener urlShortener, QueueClient queueClient, ILoggerFactory loggerFactory)
    {
        this.tableClient = tableClient;
        this.urlShortener = urlShortener;
        this.queueClient = queueClient;
        logger = loggerFactory.CreateLogger<FunctionContainer>();
    }

    [FunctionName("ShortenUrl")]
    public async Task<IActionResult> ShortenUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "shorten/")]
        HttpRequest request)
    {
        const string ParameterName = "urlToShortening";
        var urlToShortening = await request.GetParameterValueOrDefaultAsync(ParameterName);
        if (string.IsNullOrWhiteSpace(urlToShortening))
        {
            logger.LogWarning(
                "{BaseLogMessage}: HTTP trigger received a request URL, but required '{ParameterName}' parameter is not provided, returning 400 Bad Reuqest",
                GetBaseLogMessage(nameof(ShortenUrl), request), ParameterName);
            
            return new BadRequestObjectResult($"Parameter '{ParameterName}' is not provided");
        }

        logger.LogInformation(
            "{BaseLogMessage}: HTTP trigger received a request to shorten '{UrlToShortening}' URL",
            GetBaseLogMessage(nameof(ShortenUrl), request), urlToShortening);

        var firstThreeLettersOfHost = new Uri(urlToShortening).GetHost().GetFirstThreeLettersOfHost();

        var partitionKeyFilter =
            TableQuery.GenerateFilterCondition(nameof(Url.PartitionKey), QueryComparisons.Equal, firstThreeLettersOfHost);
        var fullUrlFilter = TableQuery.GenerateFilterCondition(nameof(Url.FullUrl), QueryComparisons.Equal, urlToShortening);
        var combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, fullUrlFilter);

        var urlResponse = tableClient.Query<Url>(combinedFilter);
        if (urlResponse.Any())
        {
            var url = urlResponse.Single();
            var shortenedUrl = urlShortener.GetShortenedUrlFromShortcut(url.RowKey);
            logger.LogInformation(
                "{BaseLogMessage}: successfully processed a request but the given URL '{UrlToShortening}' was already shortened before, returning 200 OK with already existing short URL ({ShortenedUrl})",
                 GetBaseLogMessage(nameof(ShortenUrl), request), urlToShortening, shortenedUrl);

            return new ObjectResult(shortenedUrl);
        }
        else
        {
            var shortcut =
                urlShortener.GetShortcutCode(length: 6, urlToShortening: urlToShortening, baseShortcutCode: firstThreeLettersOfHost);
            var url = new Url { RowKey = shortcut, FullUrl = urlToShortening, PartitionKey = firstThreeLettersOfHost };

            try
            {
                await tableClient.AddEntityAsync(url);
            }
            catch (RequestFailedException exception)
            {
                logger.LogError(
                    "{BaseLogMessage}: fail when trying to save shortened URL (full URL: {FullUrl}, shortcut: {Shortcut}, details: {ErrorDetails}), returning 500 Internal Server Error",
                    GetBaseLogMessage(nameof(ShortenUrl), request), urlToShortening, shortcut, exception.Message);

                return new InternalServerErrorResult();
            }

            var shortenedUrl = urlShortener.GetShortenedUrlFromShortcut(shortcutCode: shortcut);
            logger.LogInformation(
                "{BaseLogMessage}: successfully processed a request to shorten URL '{UrlToShortening}', returning 200 OK with shortened URL ({ShortenedUrl})",
                GetBaseLogMessage(nameof(ShortenUrl), request), urlToShortening, shortenedUrl);

            return new OkObjectResult(shortenedUrl);
        }
    }
    
    [FunctionName("GoToShortUrl")]
    public async Task<IActionResult> GoToShortUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "go/{shortcutCode:alpha}")]
        HttpRequest request, string shortcutCode)
    {
        logger.LogInformation(
            "{BaseLogMessage}: HTTP trigger received a request to redirect to full URL by '{ShortcutCode}' shortcut code",
            GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode);

        var partitionKey = shortcutCode.GetFirstThreeLettersOfHost();
        var urlEntityResponse = await tableClient.GetEntityAsync<Url>(partitionKey, shortcutCode);
        if (!urlEntityResponse.HasValue)
        {
            logger.LogWarning(
                "{BaseLogMessage}: URL with shortcut '{ShortcutCode}' not found, returning 404 Not Found",
                GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode);

            return new NotFoundResult();
        }
        var urlEntity = urlEntityResponse.Value;
        
        await queueClient.SendMessageAsync(shortcutCode);
        
        logger.LogInformation(
            "{BaseLogMessage}: successfully processed a request to redirect to full URL by '{ShortcutCode}' shortcut code, returning 301 Permanent Redirect to full URL ({FullUrl})",
            GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode, urlEntity.FullUrl);
        
        return new RedirectResult(urlEntity.FullUrl, permanent: true);
    }

    #region Utils
    
    private static string GetBaseLogMessage(string methodName, HttpRequest request)
    {
        var resultLogMessage = $"[{request.Method}] {nameof(FunctionContainer)}.{methodName}";
        return resultLogMessage;
    }
    
    #endregion
    
}