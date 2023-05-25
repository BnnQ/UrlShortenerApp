#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using UrlShortenerApp.Models.Entities;
using UrlShortenerApp.Services.Abstractions;
using UrlShortenerApp.Utils.Extensions;

namespace UrlShortenerApp.Services;

public class AzureTableUrlRepository : IUrlRepository
{
    private readonly TableClient tableClient;
    private readonly IMapper mapper;

    public AzureTableUrlRepository(TableClient tableClient, IMapper mapper)
    {
        this.tableClient = tableClient;
        this.mapper = mapper;
    }

    public Task<Url?> GetUrlByFullUrlIfExistsAsync(string fullUrl)
    {
        var firstThreeLettersOfHost = new Uri(fullUrl).GetHost()
            .GetFirstThreeLettersOfHost();

        var partitionKeyFilter =
            TableQuery.GenerateFilterCondition(nameof(UrlTableEntity.PartitionKey), QueryComparisons.Equal, firstThreeLettersOfHost);
        var fullUrlFilter = TableQuery.GenerateFilterCondition(nameof(Url.FullUrl), QueryComparisons.Equal, fullUrl);
        var combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, fullUrlFilter);

        var urlResponse = tableClient.Query<UrlTableEntity>(combinedFilter);
        var url = urlResponse.SingleOrDefault();
        return Task.FromResult(url is null ? null : mapper.Map<Url>(url));
    }

    public async Task<Url?> GetUrlByShortcutCodeIfExistsAsync(string shortcutCode)
    {
        var partitionKey = shortcutCode.GetFirstThreeLettersOfHost();
        var urlEntityResponse = await tableClient.GetEntityIfExistsAsync<UrlTableEntity>(partitionKey, shortcutCode);

        return urlEntityResponse.HasValue ? mapper.Map<Url>(urlEntityResponse.Value) : null;
    }

    public async Task AddUrlAsync(Url url)
    {
        var urlTableEntity = mapper.Map<UrlTableEntity>(url);
        await tableClient.AddEntityAsync(urlTableEntity);
    }

    public async Task UpdateUrlAsync(Url url)
    {
        var partitionKey = url.ShortcutCode.GetFirstThreeLettersOfHost();
        var urlEntityResponse = await tableClient.GetEntityIfExistsAsync<UrlTableEntity>(partitionKey, url.ShortcutCode);
        var urlEntity = urlEntityResponse.HasValue ? urlEntityResponse.Value : new UrlTableEntity();

        mapper.Map(source: url, destination: urlEntity);
        await tableClient.UpsertEntityAsync(urlEntity);
    }

    public async Task RemoveUrlAsync(Url url)
    {
        var partitionKey = url.ShortcutCode.GetFirstThreeLettersOfHost();
        await tableClient.DeleteEntityAsync(partitionKey, url.ShortcutCode);
    }

    public async Task<int> GetCurrentIdentifierAsync()
    {
        var currentIdentifier = 1;
        var identityResponse =
            await tableClient.GetEntityIfExistsAsync<UrlIdentity>(partitionKey: nameof(UrlIdentity), rowKey: nameof(UrlIdentity));
        if (identityResponse.HasValue)
        {
            currentIdentifier = identityResponse.Value.CurrentIdentifier;
        }

        return currentIdentifier;
    }

    public async Task UpdateIdentityAsync(int newIdentifier)
    {
        await tableClient.UpsertEntityAsync(new UrlIdentity { CurrentIdentifier = newIdentifier });
    }
}