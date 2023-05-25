#nullable enable
using System;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class UrlShortener
{
    private readonly ILetterGenerator letterGenerator;
    private readonly ITextEntropier textEntropier;

    public UrlShortener(ILetterGenerator letterGenerator, ITextEntropier textEntropier)
    {
        this.letterGenerator = letterGenerator;
        this.textEntropier = textEntropier;
    }

    public string GetShortcutCode(byte length, string? baseShortcutCode = "", int seed = 1)
    {
        var shortCutCodeBuilder = new StringBuilder(textEntropier.EntropyText(baseShortcutCode))
            .Append(letterGenerator.GenerateLetters(length, seed));

        return shortCutCodeBuilder.ToString();
    }
    
    public string GetShortenedUrlFromShortcut(string shortcutCode)
    {
        var shortenedUrlBuilder = new StringBuilder("http://");
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

        return shortenedUrlBuilder.Append("/api/go/").Append(shortcutCode).ToString();
    }
    
}