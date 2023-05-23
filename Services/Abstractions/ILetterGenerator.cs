namespace UrlShortenerApp.Services.Abstractions;

public interface ILetterGenerator
{
    public string GenerateLetters(ushort length);
}