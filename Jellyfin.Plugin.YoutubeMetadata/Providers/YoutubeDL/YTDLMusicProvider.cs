using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YTDLMusicProvider : AbstractYoutubeRemoteProvider<YTDLMusicProvider, MusicVideo, MusicVideoInfo>
{
    public YTDLMusicProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLMusicProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
    {
    }

    public override string Name => Constants.ProviderId;

    internal override MetadataResult<MusicVideo> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToMusicVideo(jsonObj, id);

    internal override async Task GetAndCacheMetadata(
            string id,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
    {
        await Utils.YTDLMetadata(id, appPaths, cancellationToken);
    }
}