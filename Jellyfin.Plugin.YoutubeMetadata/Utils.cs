using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata;

public static class Utils
{
    public static bool IsFresh(MediaBrowser.Model.IO.FileSystemMetadata fileInfo)
    {
        if (fileInfo.Exists && (DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc).Days <= 10))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///  Returns the Youtube ID from the file path. Matches last 11 character field inside square brackets.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetYTID(string name)
    {
        var match = Regex.Match(name, Constants.YTID_RE);
        if (!match.Success)
        {
            match = Regex.Match(name, Constants.YTCHANNEL_RE);
        }

        return match.Value;
    }

    /// <summary>
    /// Creates a person object of type director for the provided name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="channel_id"></param>
    /// <returns></returns>
    public static PersonInfo CreatePerson(string name, string channel_id, PersonKind personKind) => new()
    {
        Name = name,
        Type = personKind,
        ProviderIds = new()
        {
            { Constants.ProviderId, channel_id },
        },
    };

    public static void AddPersonIfValid<T>(this MetadataResult<T> result,
        string? name,
        string? channel_id,
        PersonKind personKind)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (string.IsNullOrEmpty(channel_id))
        {
            return;
        }

        result.AddPerson(CreatePerson(name, channel_id, personKind));
    }

    /// <summary>
    /// Returns path to where metadata json file should be.
    /// </summary>
    /// <param name="appPaths"></param>
    /// <param name="youtubeID"></param>
    /// <returns></returns>
    public static string GetVideoInfoPath(IServerApplicationPaths appPaths, string youtubeID) =>
        Path.Combine(appPaths.CachePath, "youtubemetadata", youtubeID, "ytvideo.info.json");

    public static async Task<string?> SearchChannel(string query, IServerApplicationPaths appPaths,
        CancellationToken cancellationToken)
    {
        if (!Plugin.Instance.Configuration.EnableDownloadingMetadata)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ytd = new YoutubeDLP();
        var url = string.Format(Constants.SearchQuery, System.Web.HttpUtility.UrlEncode(query));
        ytd.Options.VerbositySimulationOptions.Simulate = true;
        ytd.Options.GeneralOptions.FlatPlaylist = true;
        ytd.Options.VideoSelectionOptions.PlaylistItems = "1";
        ytd.Options.VerbositySimulationOptions.Print = "url";
        List<string> ytdl_errs = [];
        List<string> ytdl_out = [];
        ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
        ytd.StandardOutputEvent += (sender, output) => ytdl_out.Add(output);
        var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
        if (File.Exists(cookie_file))
        {
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
        }

        await ytd.DownloadAsync(url);
        if (ytdl_out.Count > 0)
        {
            var uri = new Uri(ytdl_out[0]);
            return uri.Segments[^1];
        }

        return null;
    }

    public static async Task<bool> ValidCookie(IServerApplicationPaths appPaths, CancellationToken cancellationToken)
    {
        if (!Plugin.Instance.Configuration.EnableDownloadingMetadata)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ytd = new YoutubeDLP();
        var task = ytd.DownloadAsync("https://www.youtube.com/playlist?list=WL");
        List<string> ytdl_errs = [];
        ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
        ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
        ytd.Options.VerbositySimulationOptions.SkipDownload = true;
        var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
        if (File.Exists(cookie_file))
        {
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
        }

        await task;

        foreach (var err in ytdl_errs)
        {
            var match = Regex.Match(err, @".*The playlist does not exist\..*");
            if (match.Success)
            {
                return false;
            }
        }

        return true;
    }

    public static async Task GetChannelInfo(string id, string name, IServerApplicationPaths appPaths,
        CancellationToken cancellationToken)
    {
        if (!Plugin.Instance.Configuration.EnableDownloadingMetadata)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ytd = new YoutubeDLP();
        ytd.Options.VideoSelectionOptions.PlaylistItems = "0";
        ytd.Options.FilesystemOptions.WriteInfoJson = true;
        var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", name, "ytvideo");
        ytd.Options.FilesystemOptions.Output = dataPath;
        var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
        if (File.Exists(cookie_file))
        {
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
        }

        List<string> ytdl_errs = [];
        ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
        var task = ytd.DownloadAsync(string.Format(Constants.ChannelUrl, id));
        await task;
    }

    public static async Task YTDLMetadata(string id, IServerApplicationPaths appPaths,
        CancellationToken cancellationToken)
    {
        if (!Plugin.Instance.Configuration.EnableDownloadingMetadata)
        {
            return;
        }

        //var foo = await ValidCookie(appPaths, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var ytd = new YoutubeDLP();
        ytd.Options.FilesystemOptions.WriteInfoJson = true;
        ytd.Options.VerbositySimulationOptions.SkipDownload = true;
        var cookie_file = Path.Join(appPaths.PluginsPath, "YoutubeMetadata", "cookies.txt");
        if (File.Exists(cookie_file))
        {
            ytd.Options.FilesystemOptions.Cookies = cookie_file;
        }

        var dlstring = "https://www.youtube.com/watch?v=" + id;
        var dataPath = Path.Combine(appPaths.CachePath, "youtubemetadata", id, "ytvideo");
        ytd.Options.FilesystemOptions.Output = dataPath;

        List<string> ytdl_errs = [];
        ytd.StandardErrorEvent += (sender, error) => ytdl_errs.Add(error);
        var task = ytd.DownloadAsync(dlstring);
        await task;
    }

    /// <summary>
    /// Reads JSON data from file.
    /// </summary>
    /// <param name="metaFile"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static YTDLData ReadYTDLInfo(string fpath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var jsonString = File.ReadAllText(fpath);
        return JsonSerializer.Deserialize<YTDLData>(jsonString);
    }

    /// <summary>
    /// Provides a Movie Metadata Result from a json object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static MetadataResult<Movie> YTDLJsonToMovie(YTDLData json)
    {
        var item = new Movie();
        var result = new MetadataResult<Movie>
        {
            HasMetadata = true,
            Item = item
        };
        result.Item.Name = json.Title;
        result.Item.Overview = json.Description;
        var date = ParseDate(json.UploadDate);

        if (date.HasValue)
        {
            var dt = date.Value;
            result.Item.ProductionYear = dt.Year;
            result.Item.PremiereDate = dt;
            result.Item.ForcedSortName = dt.ToString("yyyyMMdd") + "-" + result.Item.Name;
        }
        else
        {
            result.Item.ForcedSortName = result.Item.Name;
        }

        result.Item.Tags = json.Tags.ToArray();
        result.Item.Genres = json.Categories.ToArray();

        if (!string.IsNullOrWhiteSpace(json.Uploader))
        {
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Director);
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Actor);
        }

        return result;
    }

    /// <summary>
    /// Provides a MusicVideo Metadata Result from a json object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTDLData json)
    {
        var item = new MusicVideo();
        var result = new MetadataResult<MusicVideo>
        {
            HasMetadata = true,
            Item = item
        };
        result.Item.Name = string.IsNullOrEmpty(json.Track) ? json.Title : json.Track;
        result.Item.Artists = [json.Artist,];
        result.Item.Album = json.Album;
        result.Item.Overview = json.Description;
        var date = ParseDate(json.UploadDate);

        if (date.HasValue)
        {
            var dt = date.Value;
            result.Item.ProductionYear = dt.Year;
            result.Item.PremiereDate = dt;
            result.Item.ForcedSortName = dt.ToString("yyyyMMdd") + "-" + result.Item.Name;
        }
        else
        {
            result.Item.ForcedSortName = result.Item.Name;
        }

        result.Item.Tags = json.Tags.ToArray();
        result.Item.Genres = json.Categories.ToArray();

        if (!string.IsNullOrWhiteSpace(json.Uploader))
        {
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Director);
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Actor);
        }

        return result;
    }

    /// <summary>
    /// Provides a Episode Metadata Result from a json object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static MetadataResult<Episode> YTDLJsonToEpisode(YTDLData json)
    {
        var item = new Episode();
        var result = new MetadataResult<Episode>
        {
            HasMetadata = true,
            Item = item,
        };
        result.Item.Name = json.Title;
        result.Item.Overview = json.Description;
        var date = ParseDate(json.UploadDate);

        if (date.HasValue)
        {
            var dt = date.Value;
            result.Item.ProductionYear = dt.Year;
            result.Item.PremiereDate = dt;
            result.Item.ForcedSortName = dt.ToString("yyyyMMdd") + "-" + result.Item.Name;
        }
        else
        {
            result.Item.ForcedSortName = result.Item.Name;
        }

        if (!string.IsNullOrWhiteSpace(json.Uploader))
        {
            result.Item.SeriesName = json.Uploader;
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Director);
            result.AddPersonIfValid(json.Uploader, json.ChannelId ?? json.UploaderId, PersonKind.Actor);
        }

        result.Item.Tags = json.Tags.ToArray();
        result.Item.Genres = json.Categories.ToArray();

        result.Item.IndexNumber = 1;
        result.Item.ParentIndexNumber = 1;
        return result;
    }

    /// <summary>
    /// Provides a MusicVideo Metadata Result from a json object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static MetadataResult<Series> YTDLJsonToSeries(YTDLData json)
    {
        var item = new Series();
        var result = new MetadataResult<Series>
        {
            HasMetadata = true,
            Item = item,
        };
        result.Item.Name = json.Uploader;
        result.Item.Overview = json.Description;

        result.Item.TrySetProviderId(Constants.ProviderId, json.ChannelId ?? json.UploaderId);
        return result;
    }

    public static DateTime? ParseDate(string? datetime)
    {
        if (string.IsNullOrEmpty(datetime))
        {
            return null;
        }

        try
        {
            return DateTime.ParseExact(datetime, "yyyyMMdd", null);
        }
        catch
        {
            return null;
        }
    }
}
