using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace Jellyfin.Plugin.YoutubeMetadata.UnitTests;

public class UtilsTest
{
    [Theory]
    [InlineData("3Blue1Brown - 20190113 - The_most_unexpected_answer_to_a_counting_puzzle [HEfHFsfGXjs].mkv",
        "HEfHFsfGXjs")]
    [InlineData("Foo", "")]
    [InlineData("3Blue1Brown - NA - 3Blue1Brown_-_Videos [UCYO_jab_esuFRV4b17AJtAw].info.json",
        "UCYO_jab_esuFRV4b17AJtAw")]
    public void GetYTIDTest(string fn, string expected)
    {
        Assert.Equal(expected, Utils.GetYTID(fn));


    }

    [Fact]
    public void CreatePersonTest()
    {
        var result = Utils.CreatePerson("3Blue1Brown", "UCYO_jab_esuFRV4b17AJtAw", PersonKind.Director);
        var expected = new PersonInfo
        {
            Name = "3Blue1Brown",
            Type = PersonKind.Director,
            ProviderIds = new Dictionary<string, string> { { "YoutubeMetadata", "UCYO_jab_esuFRV4b17AJtAw" } }
        };


        Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(result));
    }

    [Fact]
    public void GetVideoInfoPathTest()
    {
        var mockAppPath = Mock.Of<IServerApplicationPaths>(a =>
            a.CachePath == @"\foo\bar");

        var result = Utils.GetVideoInfoPath(mockAppPath, "id123");
        Assert.Equal(@"\foo\bar\youtubemetadata\id123\ytvideo.info.json", result);
    }

    [Fact]
    public void YTDLJsonToMovieTest()
    {
        var data = new YTDLData
        {
            Title = "Foo",
            Description = "Some cool movie!",
            UploadDate = "20220131",
            Uploader = "SomeGuyIKnow",
            ChannelId = "ABCDEFGHIJKLMNOPQRSTUVWX"
        };
        var result = Utils.YTDLJsonToMovie(data);
        var person = new PersonInfo
        {
            Name = "SomeGuyIKnow",
            ProviderIds = new Dictionary<string, string> { { "YoutubeMetadata", "ABCDEFGHIJKLMNOPQRSTUVWX" } }
        };
        var expected = new MetadataResult<Movie>
        {
            HasMetadata = true,
            Item = new Movie
            {
                Name = "Foo",
                Overview = "Some cool movie!",
                ProductionYear = 2022,
                PremiereDate = DateTime.ParseExact("20220131", "yyyyMMdd", null),
            },
            People = [person,]
        };

        Assert.True(result.HasMetadata);
        Assert.Equal("Foo", result.Item.Name);
        Assert.Equal("Some cool movie!", result.Item.Overview);
        Assert.Equal(2022, result.Item.ProductionYear);
        Assert.Equal("2022/01/31 12:00:00 AM", result.Item.PremiereDate.ToString());
        Assert.Equal("SomeGuyIKnow", result.People[0].Name);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWX", result.People[0].ProviderIds["YoutubeMetadata"]);
    }

    [Fact]
    public void YTDLJsonToMusicTest()
    {
        var data = new YTDLData
        {
            Title = "Foo",
            Description = "Some cool movie!",
            UploadDate = "20220131",
            Uploader = "SomeGuyIKnow",
            ChannelId = "ABCDEFGHIJKLMNOPQRSTUVWX"
        };
        var result = Utils.YTDLJsonToMusicVideo(data);
        var person = new PersonInfo
        {
            Name = "SomeGuyIKnow",
            ProviderIds = new Dictionary<string, string> { { "YoutubeMetadata", "ABCDEFGHIJKLMNOPQRSTUVWX" } }
        };

        Assert.True(result.HasMetadata);
        Assert.Equal("Foo", result.Item.Name);
        Assert.Equal("Some cool movie!", result.Item.Overview);
        Assert.Equal(2022, result.Item.ProductionYear);
        Assert.Equal("2022/01/31 12:00:00 AM", result.Item.PremiereDate.ToString());
        Assert.Equal("SomeGuyIKnow", result.People[0].Name);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWX", result.People[0].ProviderIds["YoutubeMetadata"]);
    }

    [Fact]
    public void YTDLJsonToMusicWithTrackTest()
    {
        var data = new YTDLData
        {
            Title = "Foo",
            Track = "Bar",
            Description = "Some cool movie!",
            UploadDate = "20220131",
            Uploader = "SomeGuyIKnow",
            ChannelId = "ABCDEFGHIJKLMNOPQRSTUVWX"
        };
        var result = Utils.YTDLJsonToMusicVideo(data);
        var person = new PersonInfo
        {
            Name = "SomeGuyIKnow",
            ProviderIds = new Dictionary<string, string> { { "YoutubeMetadata", "ABCDEFGHIJKLMNOPQRSTUVWX" } }
        };

        Assert.True(result.HasMetadata);
        Assert.Equal("Bar", result.Item.Name);
        Assert.Equal("Some cool movie!", result.Item.Overview);
        Assert.Equal(2022, result.Item.ProductionYear);
        Assert.Equal("2022/01/31 12:00:00 AM", result.Item.PremiereDate.ToString());
        Assert.Equal("SomeGuyIKnow", result.People[0].Name);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWX", result.People[0].ProviderIds["YoutubeMetadata"]);
    }

    [Fact]
    public void YTDLJsonToEpisodeTest()
    {
        var data = new YTDLData
        {
            Title = "Foo",
            Description = "Some cool movie!",
            UploadDate = "20220131",
            Uploader = "SomeGuyIKnow",
            ChannelId = "ABCDEFGHIJKLMNOPQRSTUVWX"
        };
        var result = Utils.YTDLJsonToEpisode(data);
        var person = new PersonInfo
        {
            Name = "SomeGuyIKnow",
            ProviderIds = new Dictionary<string, string> { { "YoutubeMetadata", "ABCDEFGHIJKLMNOPQRSTUVWX" } }
        };

        Assert.True(result.HasMetadata);
        Assert.Equal("Foo", result.Item.Name);
        Assert.Equal("Some cool movie!", result.Item.Overview);
        Assert.Equal(2022, result.Item.ProductionYear);
        Assert.Equal("2022/01/31 12:00:00 AM", result.Item.PremiereDate.ToString());
        Assert.Equal("SomeGuyIKnow", result.People[0].Name);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWX", result.People[0].ProviderIds["YoutubeMetadata"]);
        Assert.Equal("20220131-Foo", result.Item.ForcedSortName);
        Assert.Equal(1, result.Item.IndexNumber);
        Assert.Equal(1, result.Item.ParentIndexNumber);
    }

    [Fact]
    public void YTDLJsonToSeriesTest()
    {
        var data = new YTDLData
        {
            Title = "Foo",
            Description = "Some cool movie!",
            UploadDate = "20220131",
            Uploader = "SomeGuyIKnow",
            ChannelId = "ABCDEFGHIJKLMNOPQRSTUVWX"
        };
        var result = Utils.YTDLJsonToSeries(data);

        Assert.True(result.HasMetadata);
        Assert.Equal("SomeGuyIKnow", result.Item.Name);
        Assert.Equal("Some cool movie!", result.Item.Overview);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWX", result.Item.ProviderIds["YoutubeMetadata"]);
    }
}
