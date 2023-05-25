using System;
using Azure;
using Azure.Data.Tables;

namespace UrlShortenerApp.Models.Entities;

public class UrlTableEntity : ITableEntity
{
    public string FullUrl { get; set; }
    public int Count { get; set; }
    
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}