﻿using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace Jellyfin.Plugin.YoutubeMetadata;

/// <summary>
/// Register webhook services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddScoped<IFileSystem, FileSystem>();
    }
}
