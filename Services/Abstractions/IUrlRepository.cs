#nullable enable
using System.Threading.Tasks;
using UrlShortenerApp.Models.Entities;

namespace UrlShortenerApp.Services.Abstractions;

public interface IUrlRepository
{
    public Task<Url?> GetUrlByFullUrlIfExistsAsync(string fullUrl);
    public Task<Url?> GetUrlByShortcutCodeIfExistsAsync(string shortcutCode);
    public Task AddUrlAsync(Url url);
    public Task UpdateUrlAsync(Url url);
    public Task RemoveUrlAsync(Url url);
    public Task<int> GetCurrentIdentifierAsync();
    public Task UpdateIdentityAsync(int newIdentifier);
}