using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public abstract class AbstractYoutubeLocalProvider<B, T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor where T : BaseItem {
    protected readonly ILogger<B> _logger;
    protected readonly IFileSystem _fileSystem;

    /// <summary>
    /// Providers name, this appears in the library metadata settings.
    /// </summary>
    public abstract string Name { get; }

    protected AbstractYoutubeLocalProvider(IFileSystem fileSystem, ILogger<B> logger) {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    protected FileSystemMetadata GetInfoJson(string path) {
        _logger.LogDebug("YTLocal GetInfoJson: {Path}", path);
        var fileInfo = _fileSystem.GetFileSystemInfo(path);
        var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));
        var directoryPath = directoryInfo.FullName;

        var files = _fileSystem.GetFiles(directoryPath);

        var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".info.json");

        var file = _fileSystem.GetFileInfo(specificFile);

        if (file.Exists) {
            _logger.LogDebug("Found info file named the same as the containing folder {FileName}", file.Name);
            return file;
        }

        var infoFiles = files.Where(a => a.Name.EndsWith(".info.json")).ToArray();

        if (infoFiles.Length == 1) {
            _logger.LogDebug("Found info file {FileName} within {Directory}", infoFiles[0].Name, directoryPath);
            return infoFiles[0];
        }

        _logger.LogInformation("Found a total of {Total} .info.json found in {Directory}", infoFiles.Length, directoryPath);

        return file;
    }

    /// <summary>
    /// Returns boolean if item has changed since last recorded.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="directoryService"></param>
    /// <returns></returns>
    public bool HasChanged(BaseItem item, IDirectoryService directoryService) {
        _logger.LogDebug("YTLocal HasChanged: {Name}", item.Name);
        var infoJson = GetInfoJson(item.Path);
        var result = infoJson.Exists && _fileSystem.GetLastWriteTimeUtc(infoJson) < item.DateLastSaved;
        _logger.LogDebug("YTLocal HasChanged Result: {Result}", result);
        return result;
    }

    /// <summary>
    /// Retrieves metadata of item.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="directoryService"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<MetadataResult<T>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken) {
        _logger.LogDebug("YTLocal GetMetadata: {Path}", info.Path);
        var result = new MetadataResult<T>();

        var infoFile = GetInfoJson(info.Path);
        if (!infoFile.Exists) {
            return Task.FromResult(result);
        }
        var jsonObj = Utils.ReadYTDLInfo(infoFile.FullName, cancellationToken);
        _logger.LogDebug("YTLocal GetMetadata Result: {Json}", jsonObj.ToString());
        result = this.GetMetadataImpl(jsonObj);

        return Task.FromResult(result);
    }

    internal abstract MetadataResult<T> GetMetadataImpl(YTDLData jsonObj);
}