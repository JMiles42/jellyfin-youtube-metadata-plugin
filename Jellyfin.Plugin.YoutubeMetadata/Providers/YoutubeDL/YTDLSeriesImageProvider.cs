﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers.YoutubeDL
{
    public class YTDLSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<YTDLImageProvider> _logger;

        public YTDLSeriesImageProvider(IServerConfigurationManager config, IFileSystem fileSystem, ILogger<YTDLImageProvider> logger)
        {
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public string Name => Constants.PluginName;

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 1;

        /// <summary>
        /// Gets the supported images.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Disc
            };
        }

        /// <summary>
        /// Retrieves image for item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            _logger.LogDebug("GetImages: {Name}", item.Name);
            var result = new List<RemoteImageInfo>();
            var name = item.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogInformation("Youtube ID not found in Item: {item.Name}", item.Name);
                return result;
            }
            var ytPath = Utils.GetVideoInfoPath(_config.ApplicationPaths, name);
            var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
            if (!(Utils.IsFresh(fileInfo)))
            {
                _logger.LogDebug(name, " is not fresh.");
                await Utils.YTDLMetadata(name, _config.ApplicationPaths, cancellationToken);
            }
            var path = Utils.GetVideoInfoPath(_config.ApplicationPaths, name);
            var video = Utils.ReadYTDLInfo(path, cancellationToken);
            if (video != null)
            {
                result.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = video.thumbnails[^1].url,
                    Type = ImageType.Primary
                });
            }
            return result;
        }

        /// <summary>
        /// Gets the image response.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = Plugin.Instance.GetHttpClient();
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns True if item is supported.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Supports(BaseItem item)
            => item is Series;
    }
}
