using Jellyfin.Plugin.YoutubeMetadata.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Jellyfin.Plugin.YoutubeMetadata;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => Constants.PluginName;

    public override Guid Id => Guid.Parse(Constants.PluginGuid);

    private readonly IHttpClientFactory _httpClientFactory;
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClientFactory httpClientFactory) : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _httpClientFactory = httpClientFactory;
    }

    public static Plugin Instance { get; private set; }
    public HttpClient GetHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        httpClient.DefaultRequestHeaders.UserAgent.Add(
                                                       new("YTMetadata", Version.ToString()));

        return httpClient;
    }
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
                {
                        Name                 = this.Name,
                        EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                },
        ];
    }
}