using System;
using Azure;
using Azure.Data.Tables;

namespace UrlShortenerApp.Models.Entities;

public class UrlIdentity : ITableEntity
{
    public int CurrentIdentifier { get; set; }
    public string PartitionKey { get; set; } = nameof(UrlIdentity);
    public string RowKey { get; set; } = nameof(UrlIdentity);
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}