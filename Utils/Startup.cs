using System;
using System.IO;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortenerApp.Services;
using UrlShortenerApp.Services.Abstractions;
using UrlShortenerApp.Utils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace UrlShortenerApp.Utils;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder.AddJsonFile(Path.Combine(builder.GetContext().ApplicationRootPath, "appsettings.json"));
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext()
            .Configuration;
        
        builder.Services.AddSingleton<TableClient>(_ =>
        {
            const string ConnectionStringName = "TableAccount";
            var connectionString = configuration.GetConnectionString(ConnectionStringName) ??
                                   throw new InvalidOperationException($"'{ConnectionStringName}' connection string is not provided.");

            const string TableNameConfigurationPath = "Azure:Table:Name";
            var tableName = configuration[TableNameConfigurationPath];

            var tableClient = new TableClient(connectionString, tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        builder.Services.AddSingleton<QueueClient>(_ =>
        {
            const string ConnectionStringName = "QueueAccount";
            var connectionString = configuration.GetConnectionString(ConnectionStringName) ??
                                   throw new InvalidOperationException($"'{ConnectionStringName}' connection string is not provided.");

            const string QueueNameConfigurationPath = "Azure:Queue:Name";
            var queueName = configuration[QueueNameConfigurationPath];

            var queueClient = new QueueClient(connectionString, queueName);
            queueClient.CreateIfNotExists();
            return queueClient;
        });

        builder.Services.AddSingleton<Random>(_ => Random.Shared);

        builder.Services.AddTransient<ILetterGenerator, RandomLetterGenerator>();
        builder.Services.AddTransient<ITextEntropier, UpperLowerCaseTextEntropier>();
        builder.Services.AddTransient<IUrlShortener, RandomLetterUrlShortener>();
    }
    
}