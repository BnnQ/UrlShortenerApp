namespace UrlShortenerApp.Models.Entities;

public class Url
{
    public string ShortcutCode { get; set; }
    public string FullUrl { get; set; }
    public int Count { get; set; }
}