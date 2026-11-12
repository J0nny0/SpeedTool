using System.Text.Json.Serialization;
using SpeedTool.JSON;

namespace SpeedTool.Global.Definitions;

public sealed class GameWatcherSettings : IConfigurationSection
{
    [JsonInclude]
    public bool AutoLoadGame = false;

    [JsonInclude]
    public List<string> KnownGames = new();
}
