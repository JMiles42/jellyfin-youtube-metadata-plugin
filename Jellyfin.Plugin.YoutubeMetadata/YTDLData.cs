using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.YoutubeMetadata;

#pragma warning disable IDE1006 // Naming Styles
public class YTDLData
{
    // Human name
    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    // https://github.com/ytdl-org/youtube-dl/issues/1806
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    // Name for use in API?
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; }

    [JsonPropertyName("track")]
    public string? Track { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("thumbnails")]
    public List<ThumbnailInfo> Thumbnails { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];
#pragma warning restore IDE1006 // Naming Styles
}
