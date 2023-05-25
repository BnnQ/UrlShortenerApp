using System;
using System.IO;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortenerApp.Services;
using UrlShortenerApp.Services.Abstractions;
using UrlShortenerApp.Services.MapperProfiles;
using UrlShortenerApp.Utils;
// ReSharper disable RedundantTypeArgumentsOfMethod

[assembly: FunctionsStartup(typeof(Startup))]

namespace UrlShortenerApp.Utils;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder.AddJsonFile(Path.Combine(builder.GetContext()
            .ApplicationRootPath, "appsettings.json"));
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext()
            .Configuration;

        const string QueueConnectionStringName = "QueueAccount";
        var queueConnectionString = configuration.GetConnectionString(QueueConnectionStringName) ??
                                    throw new InvalidOperationException(
                                        $"'{QueueConnectionStringName}' connection string is not provided.");

        const string TableConnectionStringName = "TableAccount";
        var tableConnectionString = configuration.GetConnectionString(TableConnectionStringName) ??
                                    throw new InvalidOperationException(
                                        $"'{TableConnectionStringName}' connection string is not provided.");

        builder.Services.AddAzureClients(clients =>
        {
            clients.AddQueueServiceClient(queueConnectionString);
            clients.AddQueueServiceClient(tableConnectionString);
        });

        builder.Services.AddAutoMapper(profiles =>
        {
            profiles.AddProfile<UrlTableProfile>();
        });

        builder.Services.AddSingleton<TableClient>(_ =>
        {
            const string TableNameConfigurationPath = "Azure:Table:Name";
            var tableName = configuration[TableNameConfigurationPath];

            var tableClient = new TableClient(tableConnectionString, tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        });

        builder.Services.AddSingleton<Random>(_ => Random.Shared);

        builder.Services.AddSingleton<ILetterGenerator, ConsistentUniqueLetterGenerator>(_ =>
        {
            return new ConsistentUniqueLetterGenerator(availableSymbols: new[]
            {
                'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', 'G', 'g', 'H', 'h', 'I', 'i', 'J', 'j', 'K', 'k', 'L', 'l', 'M',
                'm',
                'N', 'n', 'O', 'o', 'P', 'p', 'Q', 'q', 'R', 'r', 'S', 's', 'T', 't', 'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x', 'Y', 'y', 'Z',
                'z'
            });
        });

        builder.Services.AddTransient<ITextEntropier, UpperLowerCaseTextEntropier>();
        builder.Services.AddTransient<UrlShortener>();
        builder.Services.AddSingleton<IUrlRepository, AzureTableUrlRepository>();
    }
}