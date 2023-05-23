#nullable enable
using System;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class RandomLetterUrlShortener : IUrlShortener
{
    private readonly ILetterGenerator letterGenerator;
    private readonly ITextEntropier textEntropier;

    public RandomLetterUrlShortener(ILetterGenerator letterGenerator, ITextEntropier textEntropier)
    {
        this.letterGenerator = letterGenerator;
        this.textEntropier = textEntropier;
    }

    public string GetShortcutCode(byte length, string urlToShortening, string? baseShortcutCode = "")
    {
        var shortCutCodeBuilder = new StringBuilder(baseShortcutCode)
            .Append(letterGenerator.GenerateLetters(length));

        return textEntropier.EntropyText(shortCutCodeBuilder.ToString());
    }

    public string GetShortenedUrlFromShortcut(string shortcutCode)
    {
        var shortenedUrlBuilder = new StringBuilder("https://");
        var hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "localhost";
        shortenedUrlBuilder.Append(hostName);
        if (hostName.Equals("localhost"))
        {
            shortenedUrlBuilder.Append(":7071");
        }
        else
        {
            var port = Environment.GetEnvironmentVariable("WEBSITE_PORT");
            if (!string.IsNullOrWhiteSpace(port))
                shortenedUrlBuilder.Append($":{port}");
        }

        return shortenedUrlBuilder.Append("/go/").Append(shortcutCode).ToString();
    }
    
}