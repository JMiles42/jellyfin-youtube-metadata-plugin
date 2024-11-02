using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YoutubeLocalMusicProvider : AbstractYoutubeLocalProvider<YoutubeLocalMusicProvider, MusicVideo>
{
    public YoutubeLocalMusicProvider(IFileSystem fileSystem, ILogger<YoutubeLocalMusicProvider> logger) : base(fileSystem, logger) { }

    public override string Name => Constants.ProviderId;

    internal override MetadataResult<MusicVideo> GetMetadataImpl(YTDLData jsonObj)
    {
        return Utils.YTDLJsonToMusicVideo(jsonObj);
    }
}