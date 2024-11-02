using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YTDLSeriesProvider : AbstractYoutubeRemoteProvider<YTDLSeriesProvider, Series, SeriesInfo>
{

    public YTDLSeriesProvider(
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            ILogger<YTDLSeriesProvider> logger,
            IServerConfigurationManager config,
            System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs)
    {
    }

    /// <inheritdoc />
    public override string Name => Constants.ProviderId;

    public override async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        _logger.LogDebug("YTDLSeries GetMetadata: {Path}", info.Path);
        MetadataResult<Series> result = new();
        var name = info.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogDebug("YTDLSeries GetMetadata: No name found for media: {Path}", info.Path);
            result.HasMetadata = false;
            return result;
        }
        var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, name);
        var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
        _logger.LogDebug("YTDLSeries GetMetadata: FileInfo: {Path} ", fileInfo.Name);
        if (!IsFresh(fileInfo))
        {
            _logger.LogDebug("YTDLSeries GetMetadata: {Name} is not fresh.", fileInfo.Name);
            await this.GetAndCacheMetadata(name, this._config.ApplicationPaths, cancellationToken);
        }
        var video = ReadYTDLInfo(ytPath, cancellationToken);

        if (video is null)
        {
            return result;
        }

        try
        {
            result = this.GetMetadataImpl(video, video.ChannelId);
        }
        catch (System.ArgumentException e)
        {
            _logger.LogError("YTDLSeries GetMetadata: Error parsing json: {Title}; {@Video}", video.Title, video);
        }
        return result;
    }

    internal override MetadataResult<Series> GetMetadataImpl(YTDLData jsonObj, string id) => Utils.YTDLJsonToSeries(jsonObj);

    internal override async Task GetAndCacheMetadata(
            string name,
            IServerApplicationPaths appPaths,
            CancellationToken cancellationToken)
    {
        _logger.LogDebug("YTDLSeries GetMetadataImpl: GetAndCacheMetadata {Name}", name);
        var ytPath = GetVideoInfoPath(this._config.ApplicationPaths, name);
        var fileInfo = _fileSystem.GetFileSystemInfo(ytPath);
        if (!IsFresh(fileInfo))
        {
            _logger.LogDebug("YTDLSeries GetMetadataImpl: {Name} is not fresh", fileInfo.Name);
            var searchResult = Utils.SearchChannel(name, appPaths, cancellationToken);
            await searchResult;
            await Utils.GetChannelInfo(searchResult.Result, name, appPaths, cancellationToken);
        }

    }
    public override Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        _logger.LogDebug("YTDLSeries GetImageResponse: {URL}", url);
        return _httpClientFactory.CreateClient(Constants.ProviderId).GetAsync(url, cancellationToken);
    }
}