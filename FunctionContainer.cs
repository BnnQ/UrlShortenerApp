#nullable enable
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Azure;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using UrlShortenerApp.Models.Entities;
using UrlShortenerApp.Services;
using UrlShortenerApp.Services.Abstractions;
using UrlShortenerApp.Utils.Extensions;

namespace UrlShortenerApp;

public class FunctionContainer
{
    private readonly IUrlRepository urlRepository;
    private readonly UrlShortener urlShortener;
    private readonly ILogger<FunctionContainer> logger;

    public FunctionContainer(IUrlRepository urlRepository, UrlShortener urlShortener, ILoggerFactory loggerFactory)
    {
        this.urlRepository = urlRepository;
        this.urlShortener = urlShortener;
        logger = loggerFactory.CreateLogger<FunctionContainer>();
    }

    [FunctionName("ShortenUrl")]
    public async Task<IActionResult> ShortenUrl(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "shorten/")]
        HttpRequest request, [Queue(queueName: "olds")] QueueClient oldUrlQueue)
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

        var url = await urlRepository.GetUrlByFullUrlIfExistsAsync(urlToShortening);
        if (url is not null)
        {
            var shortenedUrl = urlShortener.GetShortenedUrlFromShortcut(url.ShortcutCode);
            logger.LogInformation(
                "{BaseLogMessage}: successfully processed a request but the given URL '{UrlToShortening}' was already shortened before, returning 200 OK with already existing short URL ({ShortenedUrl})",
                 GetBaseLogMessage(nameof(ShortenUrl), request), urlToShortening, shortenedUrl);
        
            return new ObjectResult(shortenedUrl);
        }
        else
        {
            var currentIdentifier = await urlRepository.GetCurrentIdentifierAsync();
            var firstThreeLettersOfHost = new Uri(urlToShortening).GetHost()
                .GetFirstThreeLettersOfHost();
            
            var shortcut =
                urlShortener.GetShortcutCode(length: 5, baseShortcutCode: firstThreeLettersOfHost, seed: currentIdentifier); 
            url = new Url { ShortcutCode = shortcut, FullUrl = urlToShortening };
        
            try
            {
                await urlRepository.AddUrlAsync(url);
                await urlRepository.UpdateIdentityAsync(++currentIdentifier);
                await oldUrlQueue.SendMessageAsync(messageText: url.ShortcutCode, visibilityTimeout: TimeSpan.FromDays(31));
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "go/{shortcutCode}")]
        HttpRequest request, string shortcutCode, [Queue(queueName: "redirects")] QueueClient redirectCountingQueue)
    {
        logger.LogInformation(
            "{BaseLogMessage}: HTTP trigger received a request to redirect to full URL by '{ShortcutCode}' shortcut code",
            GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode);

        var url = await urlRepository.GetUrlByShortcutCodeIfExistsAsync(shortcutCode);
        if (url is null)
        {
            logger.LogWarning(
                "{BaseLogMessage}: URL with shortcut '{ShortcutCode}' not found, returning 404 Not Found",
                GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode);
    
            return new NotFoundResult();
        }

        await redirectCountingQueue.SendMessageAsync(shortcutCode);
        
        logger.LogInformation(
            "{BaseLogMessage}: successfully processed a request to redirect to full URL by '{ShortcutCode}' shortcut code, returning 302 Temporary Redirect to full URL ({FullUrl})",
            GetBaseLogMessage(nameof(ShortenUrl), request), shortcutCode, url.FullUrl);

        return new RedirectResult(url.FullUrl, permanent: false);
    }

    [FunctionName("CountRedirect")]
    public async Task CountRedirect([QueueTrigger(queueName: "redirects")] string shortcutCode)
    {
        logger.LogInformation("{BaseLogMessage}: received '{ShortcutCode}' shortcut code from queue",
            GetBaseLogMessage(nameof(CountRedirect)), shortcutCode);
        
        var url = await urlRepository.GetUrlByShortcutCodeIfExistsAsync(shortcutCode);
        if (url is null)
        {
            logger.LogWarning("{BaseLogMessage}: URL with shortcut code '{ShorcutCode}' not found",
                GetBaseLogMessage(nameof(CountRedirect)), shortcutCode);
    
            return;
        }
    
        url.Count += 1;
        await urlRepository.UpdateUrlAsync(url);
        
        logger.LogInformation("{BaseLogMessage}: successfully counted redirects number of URL with shortcut code '{ShortcutCode}', currently its {RedirectCount}",
            GetBaseLogMessage(nameof(CountRedirect)), shortcutCode, url.Count);
    }
    
    [FunctionName("ClearOldUrl")]
    public async Task ClearOldUrl([QueueTrigger(queueName: "olds")] string shortcutCode)
    {
        logger.LogInformation("{BaseLogMessage}: received '{ShortcutCode}' shortcut code from queue",
            GetBaseLogMessage(nameof(ClearOldUrl)), shortcutCode);
        
        var url = await urlRepository.GetUrlByShortcutCodeIfExistsAsync(shortcutCode);
        if (url is null)
        {
            logger.LogWarning("{BaseLogMessage}: URL with shortcut code '{ShorcutCode}' not found",
                GetBaseLogMessage(nameof(ClearOldUrl)), shortcutCode);
            
            return;
        }

        await urlRepository.RemoveUrlAsync(url);

        var currentIdentifier = await urlRepository.GetCurrentIdentifierAsync();
        await urlRepository.UpdateIdentityAsync(--currentIdentifier);
        
        logger.LogInformation("{BaseLogMessage}: successfully removed old URL '{FullUrl}' with shortcut code '{ShortcutCode}'",
            GetBaseLogMessage(nameof(ClearOldUrl)), url.FullUrl, shortcutCode);
    }

    #region Utils
    
    private static string GetBaseLogMessage(string methodName, HttpRequest? request = null)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.Append('[')
            .Append(request is not null ? request.Method : "UTILITY")
            .Append($"] {nameof(FunctionContainer)}.{methodName}");
        
        return messageBuilder.ToString();
    }
    
    #endregion
    
}