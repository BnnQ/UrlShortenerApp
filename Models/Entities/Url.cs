using System;
using Azure;
using ITableEntity = Azure.Data.Tables.ITableEntity;

namespace UrlShortenerApp.Models.Entities;

public class Url : ITableEntity
{
    public string FullUrl { get; set; }
    public int Count { get; set; }
    
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}