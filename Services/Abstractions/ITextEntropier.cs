namespace UrlShortenerApp.Services.Abstractions;

public interface ITextEntropier
{
    public string EntropyText(string text);
}