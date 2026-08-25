using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Folio;

/// <summary>
/// Checks all metadata providers for potential issues when this plugin is installed.
/// </summary>
public class EventConsumer : IEventConsumer<PluginInstalledEventArgs>
{
    private const string MovieCollectionProviderName = "TheMovieDb";

    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILogger<EventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventConsumer"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <param name="logger">The logger.</param>
    public EventConsumer(IServerConfigurationManager serverConfigurationManager, ILogger<EventConsumer> logger)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task OnEvent(PluginInstalledEventArgs eventArgs)
    {
        if (!eventArgs.Argument.Id.Equals(Plugin.PluginId))
        {
            return Task.CompletedTask;
        }

        var config = _serverConfigurationManager.Configuration;
        var metadataOptions = config.MetadataOptions;
        var seriesOptions = metadataOptions.FirstOrDefault(option => string.Equals(option.ItemType, "BoxSet", StringComparison.OrdinalIgnoreCase)) ?? new MetadataOptions { ItemType = "BoxSet" };

        // just check the disabled fetchers and log a warning message for now
        if (!seriesOptions.DisabledImageFetchers.Contains(MovieCollectionProviderName) || !seriesOptions.DisabledMetadataFetchers.Contains(MovieCollectionProviderName))
        {
            _logger.LogWarning("providers are enabled that might fetch incorrect metadata and images for book collections");
        }

        seriesOptions.DisabledImageFetchers = [.. seriesOptions.DisabledImageFetchers, MovieCollectionProviderName];
        seriesOptions.DisabledMetadataFetchers = [.. seriesOptions.DisabledMetadataFetchers, MovieCollectionProviderName];

        // _serverConfigurationManager.SaveConfiguration();
        // _logger.LogWarning("updating metadata options for plugin {PluginName}", eventArgs.Argument.Name);

        return Task.CompletedTask;
    }
}
