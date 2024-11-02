namespace Jellyfin.Plugin.YoutubeMetadata;

internal class References
{
    private References()
    {
        _ = typeof(NYoutubeDL.YoutubeDLP);
        _ = typeof(Newtonsoft.Json.Formatting);
    }
}