using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers
{
    public class YoutubeMetadataProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _json;
        private readonly ILogger<YoutubeMetadataProvider> _logger;
        private readonly ILibraryManager _libmanager;

        private static YoutubeMetadataProvider current;

        public const string BaseUrl = "https://m.youtube.com/";

        public YoutubeMetadataProvider(IServerConfigurationManager config, IFileSystem fileSystem, IJsonSerializer json, ILogger<YoutubeMetadataProvider> logger, ILibraryManager libmanager)
        {
            _config = config;
            _fileSystem = fileSystem;
            _json = json;
            _logger = logger;
            _libmanager = libmanager;
            Current = this;
        }

        /// <inheritdoc />
        public string Name => "YouTube Metadata";

        /// <inheritdoc />
        public int Order => 1;

        public static YoutubeMetadataProvider Current { get => current; set => current = value; }

        /// <inheritdoc />
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RemoteSearchResult>());

        private string GetPathByTitle(string title)
        {
            var query = new InternalItemsQuery { Name = title };
            var results = _libmanager.GetItemsResult(query);
            return results.Items[0].Path;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>();
            var id = Utils.GetYTID(GetPathByTitle(info.Name));

            _logger.LogInformation(id);

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, id);

                var video = _json.DeserializeFromFile<Google.Apis.YouTube.v3.Data.Video>(path);
                if (video != null)
                {
                    result.Item = new Movie();
                    result.HasMetadata = true;
                    result.Item.OriginalTitle = info.Name;
                    ProcessResult(result.Item, video);
                    result.AddPerson(Utils.CreatePerson(video.Snippet.ChannelTitle, video.Snippet.ChannelId));
                }
            }
            else
            {
                _logger.LogInformation("Youtube ID not found in filename of title: " + info.Name);
            }

            return result;
        }

        /// <summary>
        /// Processes the found metadata into the Movie entity.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        /// <param name="preferredLanguage"></param>
        public static void ProcessResult(Video item, Google.Apis.YouTube.v3.Data.Video result)
        {
            item.Name = result.Snippet.Title;
            item.Overview = result.Snippet.Description;
            var date = DateTime.Parse(result.Snippet.PublishedAtRaw);
            item.ProductionYear = date.Year;
            item.PremiereDate = date;
        }

        /// <summary>
        /// Checks and returns data in local cache, downloads and returns if not present.
        /// </summary>
        /// <param name="youtubeID"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal Task EnsureInfo(string youtubeID, CancellationToken cancellationToken)
        {
            var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, youtubeID);

            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 10)
                {
                    return Task.CompletedTask;
                }
            }
            return Utils.APIDownload(youtubeID, _config.ApplicationPaths, cancellationToken, Utils.DownloadType.Video);
        }
        
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
