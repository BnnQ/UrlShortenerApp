#nullable enable
namespace UrlShortenerApp.Services.Abstractions;

public interface IUrlShortener
{
    public string GetShortcutCode(byte length, string urlToShortening, string? baseShortcutCode = "");

    public string GetShortenedUrlFromShortcut(string shortcutCode);
}