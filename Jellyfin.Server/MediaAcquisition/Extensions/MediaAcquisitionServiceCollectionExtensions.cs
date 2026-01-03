using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Jellyfin.Server.MediaAcquisition.Configuration;
using Jellyfin.Server.MediaAcquisition.Data;
using Jellyfin.Server.MediaAcquisition.Events;
using Jellyfin.Server.MediaAcquisition.Indexers;
using Jellyfin.Server.MediaAcquisition.QBittorrent;
using Jellyfin.Server.MediaAcquisition.Services;
using Jellyfin.Server.MediaAcquisition.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.MediaAcquisition.Extensions;

/// <summary>
/// Extension methods for adding Media Acquisition services to the service collection.
/// </summary>
public static class MediaAcquisitionServiceCollectionExtensions
{
    /// <summary>
    /// The named HTTP client for qBittorrent.
    /// </summary>
    public const string QBittorrentHttpClient = "QBittorrent";

    /// <summary>
    /// The named HTTP client for torrent indexers.
    /// </summary>
    public const string IndexerHttpClient = "TorrentIndexer";

    /// <summary>
    /// Adds Media Acquisition services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMediaAcquisition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var section = configuration.GetSection("MediaAcquisition");
        services.Configure<MediaAcquisitionOptions>(section);

        var options = section.Get<MediaAcquisitionOptions>();
        if (options?.Enabled != true)
        {
            // Feature is disabled, skip registration
            return services;
        }

        // Register HTTP clients
        services.AddHttpClient(QBittorrentHttpClient, client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        });

        services.AddHttpClient(IndexerHttpClient, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });

        // Register qBittorrent client
        services.AddSingleton<IQBittorrentClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(QBittorrentHttpClient);
            var logger = sp.GetRequiredService<ILogger<QBittorrentClient>>();
            var opts = sp.GetRequiredService<IOptions<MediaAcquisitionOptions>>();
            return new QBittorrentClient(httpClient, logger, opts);
        });

        // Register repositories
        services.AddSingleton<ITorrentDownloadRepository, TorrentDownloadRepository>();

        // Register services
        services.AddSingleton<IMissingMediaService, MissingMediaService>();
        services.AddSingleton<IDownloadManagerService, DownloadManagerService>();
        services.AddSingleton<ITorrentSearchService, TorrentSearchService>();

        // Register event emitter
        services.AddSingleton<ITorrentProgressEventEmitter, TorrentProgressEventEmitter>();

        // Register indexers
        services.AddSingleton<IEnumerable<ITorrentIndexer>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MediaAcquisitionOptions>>().Value;
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var indexers = new List<ITorrentIndexer>();

            foreach (var indexerConfig in opts.Indexers)
            {
                if (!indexerConfig.Enabled)
                {
                    continue;
                }

                var httpClient = httpClientFactory.CreateClient(IndexerHttpClient);
                var logger = loggerFactory.CreateLogger<TorznabIndexer>();

                indexers.Add(new TorznabIndexer(httpClient, logger, indexerConfig));
            }

            return indexers;
        });

        // Register background workers
        services.AddHostedService<TorrentProgressWorker>();
        services.AddHostedService<AutoImportWorker>();

        return services;
    }
}
