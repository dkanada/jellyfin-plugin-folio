using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Folio;

/// <summary>
/// Synchronizes collections using the SeriesName property on applicable books.
/// </summary>
public sealed class CollectionManager
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly ILogger<CollectionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionManager"/> class.
    /// </summary>
    public CollectionManager(
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        ILogger<CollectionManager> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = logger;
    }

    public async Task UpdateLibrary(CancellationToken cancellationToken)
    {
        var eligibleBooks = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Book],
            MediaTypes = [MediaType.Book],
            SourceTypes = [SourceType.Library],
            DtoOptions = new DtoOptions(true),
        })
            .OfType<Book>()
            .Where(b => !string.IsNullOrWhiteSpace(b.SeriesName))
            .ToList();

        if (eligibleBooks.Count == 0)
        {
            return;
        }

        var eligibleSeries = eligibleBooks
            .GroupBy(b => b.SeriesName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList());

        var library = await _collectionManager.GetCollectionsFolder(true).ConfigureAwait(false);
        if (library is null)
        {
            return;
        }

        var activeCollections = _libraryManager.GetItemList(new InternalItemsQuery
        {
            Parent = library,
            IncludeItemTypes = [BaseItemKind.BoxSet],
            DtoOptions = new DtoOptions(true),
            Recursive = false,
        })
            .OfType<BoxSet>()
            .Where(c => c.ProviderIds.TryGetValue(Plugin.PluginId.ToString(), out _))
            .ToDictionary(c => c.ProviderIds[Plugin.PluginId.ToString()], c => c);

        var staleCollections = activeCollections.Where(c => !eligibleSeries.ContainsKey(c.Key));

        foreach (var (seriesName, collection) in staleCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("deleting collection: {SeriesName}", seriesName);

            activeCollections.Remove(seriesName);
            _libraryManager.DeleteItem(collection, new DeleteOptions { DeleteFileLocation = true });
        }

        foreach (var (seriesName, books) in eligibleSeries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!activeCollections.TryGetValue(seriesName, out var collection))
            {
                _logger.LogDebug("creating collection: {SeriesName}", seriesName);

                var creation = new CollectionCreationOptions
                {
                    ParentId = library.Id,
                    Name = seriesName,
                    ProviderIds = new(StringComparer.OrdinalIgnoreCase) { [Plugin.PluginId.ToString()] = seriesName },
                    ItemIdList = books.Select(b => b.Id.ToString("N")).ToArray(),
                };

                collection = await _collectionManager.CreateCollectionAsync(creation).ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug("updating collection: {SeriesName}", seriesName);

                if (!string.Equals(collection.Name, seriesName, StringComparison.Ordinal))
                {
                    collection.Name = seriesName;

                    await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }

                var eligibleIds = books.Select(b => b.Id).ToHashSet();
                var activeIds = collection.GetLinkedChildren().Select(i => i.Id).ToHashSet();

                var toAdd = eligibleIds.Except(activeIds).ToArray();
                if (toAdd.Length > 0)
                {
                    await _collectionManager.AddToCollectionAsync(collection.Id, toAdd).ConfigureAwait(false);
                }

                var toRemove = activeIds.Except(eligibleIds).ToArray();
                if (toRemove.Length > 0)
                {
                    await _collectionManager.RemoveFromCollectionAsync(collection.Id, toRemove).ConfigureAwait(false);
                }
            }

            await UpdateImage(collection, books, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateImage(BoxSet collection, IReadOnlyList<Book> books, CancellationToken cancellationToken)
    {
        if (collection.HasImage(ImageType.Primary, 0))
        {
            return;
        }

        var candidates = books
            .Select(b => b.GetImageInfo(ImageType.Primary, 0))
            .Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Path))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var selection = candidates[Random.Shared.Next(candidates.Count)]!;

        collection.AddImage(
            new ItemImageInfo
            {
                Type = ImageType.Primary,
                Path = selection.Path,
                DateModified = selection.DateModified,
                Height = selection.Height,
                Width = selection.Width,
                BlurHash = selection.BlurHash,
            });

        await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
    }
}
