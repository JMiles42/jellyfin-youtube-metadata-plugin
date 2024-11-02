using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.YoutubeMetadata;

public class Constants
{
    public const string PluginName = "YouTube Metadata 2 YH";
    public const string ProviderId = "YoutubeMetadata";
    public const string PluginGuid = "e1204173-3802-4d53-980e-9d1b851fde84";
    public const string ChannelUrl = "https://www.youtube.com/channel/{0}";
    public const string VideoUrl = "https://www.youtube.com/watch?v={0}";
    public const string SearchQuery = "https://www.youtube.com/results?search_query={0}&sp=EgIQAg%253D%253D";

    public const string YTCHANNEL_RE = @"(?<=\[)[a-zA-Z0-9\-_]{24}(?=\])";
    public const string YTID_RE = @"(?<=\[)[a-zA-Z0-9\-_]{11}(?=\])";

    public static Regex YoutubeChannelIdRegex = new(YTCHANNEL_RE, RegexOptions.Compiled);
    public static Regex YoutubeVideoIdRegex = new(YTID_RE, RegexOptions.Compiled);

}