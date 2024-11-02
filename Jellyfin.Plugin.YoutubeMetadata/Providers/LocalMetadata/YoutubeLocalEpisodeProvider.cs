using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YoutubeLocalEpisodeProvider : AbstractYoutubeLocalProvider<YoutubeLocalEpisodeProvider, Episode>
{
    public YoutubeLocalEpisodeProvider(IFileSystem fileSystem, ILogger<YoutubeLocalEpisodeProvider> logger) : base(
        fileSystem, logger)
    {
    }

    public override string Name => Constants.ProviderId;

    internal override MetadataResult<Episode> GetMetadataImpl(YTDLData jsonObj)
    {
        return Utils.YTDLJsonToEpisode(jsonObj);
    }
}
