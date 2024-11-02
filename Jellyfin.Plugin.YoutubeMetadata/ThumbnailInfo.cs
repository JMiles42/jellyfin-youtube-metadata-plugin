using System.Text.Json.Serialization;
#pragma warning disable IDE1006 // Naming Styles

namespace Jellyfin.Plugin.YoutubeMetadata;

/// <summary>
/// Object should match how YTDL json looks.
/// </summary>
public class ThumbnailInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("resolution")]
    public string Resolution { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
