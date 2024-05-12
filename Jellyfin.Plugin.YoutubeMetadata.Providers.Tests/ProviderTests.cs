using MediaBrowser.Controller.Providers;
using Jellyfin.Plugin.YoutubeMetadata.Providers;
using Xunit;
using Moq;
using System.Threading;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Collections.Generic;
using System.Text.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using System.Net.Http;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.YoutubeMetadata.UnitTests;

public class YTDLEpisodeProviderTest
{
        private readonly Mock<MediaBrowser.Model.IO.IFileSystem>                                 _jf_fs;
        private readonly MockFileSystem                                                          _fs;
        private readonly Mock<IHttpClientFactory>                                                _mockFactory;
        private readonly Mock<MediaBrowser.Controller.Configuration.IServerConfigurationManager> _config;
        private readonly EpisodeInfo                                                             _epInfo;
        private readonly MediaBrowser.Model.IO.FileSystemMetadata                                _fs_metadata;
        private readonly CancellationToken                                                       _token;
        private readonly YTDLEpisodeProvider                                                     _provider;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<YTDLEpisodeProvider>>         _logger;

        public YTDLEpisodeProviderTest()
        {
                _jf_fs       = new();
                _fs          = new(new Dictionary<string, MockFileData> { });
                _mockFactory = new();
                _config      = new();
                _config.Setup(config => config.ApplicationPaths.CachePath).Returns("\\cache");
                _epInfo      = new();
                _fs_metadata = new();
                _token       = new();
                _logger      = new();
                _provider    = new(_jf_fs.Object, _mockFactory.Object, _logger.Object, _config.Object, _fs);
        }

        [Fact]
        public void RemoteProviderCachedResultsTest()
        {
                var thumbnails = new List<ThumbnailInfo>();

                var tn = new ThumbnailInfo
                {
                        url        = "https://www.something.com",
                        width      = 10,
                        height     = 10,
                        resolution = "10x10",
                        id         = "id912az"
                };

                var json = new YTDLData
                {
                        uploader    = "Someone",
                        upload_date = "20211215",
                        title       = "Cool Video",
                        description = "This is the best video.",
                        channel_id  = "12345",
                        thumbnails  = thumbnails
                };

                _fs_metadata.LastWriteTimeUtc = DateTime.Today.AddDays(-1);
                _fs_metadata.Exists           = true;

                _jf_fs.Setup(fs => fs.GetFileSystemInfo(@"\cache\youtubemetadata\AAAAAAAAAAA\ytvideo.info.json"))
                      .Returns(_fs_metadata);

                _fs.AddFile(@"\cache\youtubemetadata\AAAAAAAAAAA\ytvideo.info.json",
                            new(JsonSerializer.Serialize(json)));

                _epInfo.Path = "/Something [AAAAAAAAAAA].mkv";

                //var provider = new YTDLEpisodeProvider(_jf_fs.Object, new Mock<Microsoft.Extensions.Logging.ILogger<YTDLEpisodeProvider>>().Object, _config.Object, _fs);
                var metadata = _provider.GetMetadata(_epInfo, _token);
                metadata.Wait();
                Assert.Equal(json.title,       metadata.Result.Item.Name);
                Assert.Equal(json.description, metadata.Result.Item.Overview);
                Assert.Equal(2021,             metadata.Result.Item.ProductionYear);

                Assert.Equal(DateTime.ParseExact(json.upload_date, "yyyyMMdd", null),
                             metadata.Result.Item.PremiereDate);

                Assert.Equal(1,         metadata.Result.Item.IndexNumber);
                Assert.Equal(1,         metadata.Result.Item.ParentIndexNumber);
                Assert.Equal("Someone", metadata.Result.People[0].Name);
        }

        [Theory]
        [InlineData("/Foo.mkv")]
        [InlineData("/2019 - Foo (AAAAAAAAAAA).mp4")]
        public void RemoteProviderInvalidIdTest(string path)
        {
                _epInfo.Path = path;

                var provider = new YTDLEpisodeProvider(_jf_fs.Object,
                                                       _mockFactory.Object,
                                                       new Mock<Microsoft.Extensions.Logging.ILogger<
                                                                       YTDLEpisodeProvider>>().Object,
                                                       _config.Object,
                                                       _fs);

                var metadata = provider.GetMetadata(_epInfo, _token);
                metadata.Wait();
                Assert.False(metadata.Result.HasMetadata);
        }

        [Fact]
        public void YTDLJsonToMovieTest()
        {
                var thumbnails = new List<ThumbnailInfo>();

                var tn = new ThumbnailInfo
                {
                        url        = "https://www.something.com",
                        width      = 10,
                        height     = 10,
                        resolution = "10x10",
                        id         = "id912az"
                };

                thumbnails.Add(tn);

                var json = new YTDLData
                {
                        uploader    = "Someone",
                        upload_date = "20211215",
                        title       = "Cool Video",
                        description = "This is the best video.",
                        channel_id  = "12345",
                        thumbnails  = thumbnails
                };

                var result = YTDLEpisodeProvider.YTDLJsonToEpisode(json, "id123");
                Assert.Equal(json.title,                                              result.Item.Name);
                Assert.Equal(json.description,                                        result.Item.Overview);
                Assert.Equal(2021,                                                    result.Item.ProductionYear);
                Assert.Equal(DateTime.ParseExact(json.upload_date, "yyyyMMdd", null), result.Item.PremiereDate);
                Assert.Equal(1,                                                       result.Item.IndexNumber);
                Assert.Equal(1,                                                       result.Item.ParentIndexNumber);
                Assert.Equal("Someone",                                               result.People[0].Name);
        }

        public static IEnumerable<object[]> MusicJsonTests =>
                        new List<object[]>
                        {
                                new object[]
                                {
                                        new YTDLData
                                        {
                                                title       = "Foo",
                                                album       = "Bar",
                                                artist      = "Someone",
                                                upload_date = "20211215",
                                                description = "Some music",
                                                uploader    = "ankenyr",
                                                channel_id  = "abc123",
                                        },
                                        new MetadataResult<MusicVideo>
                                        {
                                                HasMetadata = true,
                                                Item = new()
                                                {
                                                        Name = "Foo",
                                                        Album = "Bar",
                                                        Artists = new List<string> { "Someone" },
                                                        Overview = "Some music",
                                                        ProductionYear = 2021,
                                                        PremiereDate = DateTime.ParseExact("20211215", "yyyyMMdd", null)
                                                },
                                                People = new()
                                                {
                                                        new()
                                                        {
                                                                Name        = "ankenyr",
                                                                Type        = PersonKind.Director,
                                                                ProviderIds = new() { { "YoutubeMetadata", "abc123" } }
                                                        }
                                                }
                                        }
                                },
                                new object[]
                                {
                                        new YTDLData
                                        {
                                                track       = "FooTrack",
                                                album       = "Bar",
                                                artist      = "Someone",
                                                upload_date = "20211215",
                                                description = "Some music",
                                                uploader    = "ankenyr",
                                                channel_id  = "abc123",
                                        },
                                        new MetadataResult<MusicVideo>
                                        {
                                                HasMetadata = true,
                                                Item = new()
                                                {
                                                        Name = "Foo",
                                                        Album = "Bar",
                                                        Artists = new List<string> { "Someone" },
                                                        Overview = "Some music",
                                                        ProductionYear = 2021,
                                                        PremiereDate = DateTime.ParseExact("20211215", "yyyyMMdd", null)
                                                },
                                                People = new()
                                                {
                                                        new()
                                                        {
                                                                Name        = "ankenyr",
                                                                Type        = PersonKind.Director,
                                                                ProviderIds = new() { { "YoutubeMetadata", "abc123" } }
                                                        }
                                                }
                                        }
                                }
                        };

        [Theory]
        [MemberData(nameof(MusicJsonTests))]
        public void YTDLJsonToMusicVideo(YTDLData json, MetadataResult<MusicVideo> expected)
        {
                var result = JsonSerializer.Serialize(YTDLEpisodeProvider.YTDLJsonToMusicVideo(json, "id123"));
                Assert.Equal(JsonSerializer.Serialize(expected), result);

        }

        public static IEnumerable<object[]> MovieJsonTests =>
                        new List<object[]>
                        {
                                new object[]
                                {
                                        new YTDLData
                                        {
                                                title       = "Foo",
                                                upload_date = "20211215",
                                                description = "Some movie",
                                                uploader    = "ankenyr",
                                                channel_id  = "abc123",
                                        },
                                        new MetadataResult<Movie>
                                        {
                                                HasMetadata = true,
                                                Item = new()
                                                {
                                                        Name = "Foo",
                                                        Overview = "Some movie",
                                                        ProductionYear = 2021,
                                                        PremiereDate = DateTime.ParseExact("20211215", "yyyyMMdd", null)
                                                },
                                                People = new()
                                                {
                                                        new()
                                                        {
                                                                Name        = "ankenyr",
                                                                Type        = PersonKind.Director,
                                                                ProviderIds = new() { { "YoutubeMetadata", "abc123" } }
                                                        }
                                                }
                                        }
                                }
                        };

        [Theory]
        [MemberData(nameof(MovieJsonTests))]
        public void YTDLJsonToMovie(YTDLData json, MetadataResult<Movie> expected)
        {
                var result = JsonSerializer.Serialize(YTDLEpisodeProvider.YTDLJsonToMovie(json, "id123"));
                Assert.Equal(JsonSerializer.Serialize(expected), result);
        }
}