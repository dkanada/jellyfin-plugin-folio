using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Folio;

/// <summary>
/// Runs after a library scan is completed to process all Folio collections.
/// </summary>
public sealed class CollectionTask : ILibraryPostScanTask
{
    private readonly CollectionManager _collectionManager;

    public CollectionTask(
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        ILogger<CollectionManager> logger)
    {
        _collectionManager = new CollectionManager(libraryManager, collectionManager, logger);
    }

    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        await _collectionManager.UpdateLibrary(cancellationToken).ConfigureAwait(false);
        progress.Report(100);
    }
}
