using MediaBrowser.Model.Plugins;


namespace Jellyfin.Plugin.YoutubeMetadata.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableDownloadingMetadata { get; set; } = true;

    public static IDTypes IDType { get; set; }

    public PluginConfiguration()
    {
        // defaults
        IDType = IDTypes.YTDLP;
    }
}
