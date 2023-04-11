﻿using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller;
using System.Net.Http;

namespace Jellyfin.Plugin.YoutubeMetadata.Providers;

public class YTDLMovieProvider : AbstractYoutubeRemoteProvider<YTDLMovieProvider, Movie, MovieInfo> {
	public YTDLMovieProvider(
			IFileSystem fileSystem,
			IHttpClientFactory httpClientFactory,
			ILogger<YTDLMovieProvider> logger,
			IServerConfigurationManager config,
			System.IO.Abstractions.IFileSystem afs) : base(fileSystem, httpClientFactory, logger, config, afs) {
	}

	public override string Name => Constants.ProviderId;

	internal override MetadataResult<Movie> GetMetadataImpl(YTDLData jsonObj, string id) => YTDLJsonToMovie(jsonObj, id);

	internal override async Task GetAndCacheMetadata(
			string id,
			IServerApplicationPaths appPaths,
			CancellationToken cancellationToken) => await Utils.YTDLMetadata(id, appPaths, cancellationToken);
}