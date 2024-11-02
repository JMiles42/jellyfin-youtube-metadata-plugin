using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YTDLEpisodeProvider : AbstractYoutubeRemoteProvider<YTDLEpisodeProvider, Episode, EpisodeInfo>
{
    public YTDLEpisodeProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLEpisodeProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
    {
    }

    public override string Name => Constants.ProviderId;

    internal override MetadataResult<Episode> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToEpisode(jsonObj, id);

    internal override async Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
    {
        _logger.LogDebug("YTDLEpisodeProvider: GetAndCacheMetadata ", id);
        await Utils.YTDLMetadata(id, appPaths, cancellationToken);
    }
}