using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.ExternalId;

public class YTVideoExternalId : IExternalId
{
    public bool Supports(IHasProviderIds item)
        => item is Movie or Episode or MusicVideo;

    public string ProviderName
        => Constants.ProviderId;

    public string Key
        => Constants.ProviderId;

    public ExternalIdMediaType? Type
        => null;

    public string UrlFormatString
        => Constants.VideoUrl;
}
