using System.Text.Json;
using SpeedTool.Global.Definitions;
using SpeedTool.Platform;

namespace SpeedTool.Tests;

public sealed class GameWatcherTests
{
    [Fact]
    public void ShouldLoadRequiresRunningProcess()
    {
        Assert.False(GameWatcher.ShouldLoad(-1, "NFS13.exe", null, -1));
        Assert.True(GameWatcher.ShouldLoad(1234, "NFS13.exe", null, -1));
    }

    [Fact]
    public void ShouldNotReloadTheInstanceTheUserUnloaded()
    {
        Assert.False(GameWatcher.ShouldLoad(1234, "NFS13.exe", "NFS13.exe", 1234));
    }

    [Fact]
    public void ShouldLoadANewInstanceOfTheSameGame()
    {
        Assert.True(GameWatcher.ShouldLoad(5678, "NFS13.exe", "NFS13.exe", 1234));
    }

    [Fact]
    public void ShouldLoadADifferentGameWhileOneIsClosed()
    {
        Assert.True(GameWatcher.ShouldLoad(9999, "Other.exe", "NFS13.exe", 1234));
    }

    [Fact]
    public void ShouldLoadIsCaseInsensitiveOnExeNames()
    {
        Assert.False(GameWatcher.ShouldLoad(1234, "nfs13.EXE", "NFS13.exe", 1234));
    }

    [Fact]
    public void RunningProcessListIsDistinctSortedAndExcludesSelf()
    {
        var names = SpeedTool.Platform.ProcessList.GetRunningExeNames();
        Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain("SpeedTool", names);
        Assert.Equal(names, names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void GameWatcherSettingsRoundTrip()
    {
        var settings = new GameWatcherSettings
        {
            AutoLoadGame = true,
            KnownGames = { "/home/user/games/NFS13.speedtool" }
        };
        var node = JsonSerializer.SerializeToNode(settings, typeof(GameWatcherSettings), SourceGeneratorContext.Default);
        var back = (GameWatcherSettings)JsonSerializer.Deserialize(node!, typeof(GameWatcherSettings), SourceGeneratorContext.Default)!;

        Assert.True(back.AutoLoadGame);
        Assert.Equal("/home/user/games/NFS13.speedtool", back.KnownGames[0]);
    }
}
