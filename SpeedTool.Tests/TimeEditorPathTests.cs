using System.Text.Json;
using SpeedTool.JSON;
using SpeedTool.Platform;
using SpeedTool.Splits;
using SpeedTool.Timer;

namespace SpeedTool.Tests;

// Replicates the exact data path taken when the user right-clicks -> "Edit Times":
//   MainWindow:  pb = GetPBRun(...) ?? new Run(game, cat, null).GetRunInfo()
//   TimeEditorWindow: RecalculateTimes() over the splits, CollectTotalTimes() on Save,
//   SaveRunAsPB -> JSON -> GetPBRun on next open.
public class TimeEditorPathTests
{
    private static RunInfo BuildRunInfoFromCategory(Game game, Category cat)
    {
        // Mirrors Run.GetRunInfo() without touching the Platform singleton
        var controller = new SplitsController(cat, null);
        return new RunInfo(game.Name, cat.Name, controller.AllSplits.Last().Times,
                           controller.AllSplits.Select(x => x.ToSplitInfo()).ToArray());
    }

    // Verbatim copy of TimeEditorWindow.RecalculateTimes/CollectTotalTimes state machine
    private class EditorSim
    {
        private SplitInfo[] splits;
        private TimeCollection nowTime = new();
        private int i = 0;

        public EditorSim(RunInfo ri) { splits = ri.Splits; }

        public TimeCollection RecalculateTimes()
        {
            while(i < splits.Length)
            {
                SplitInfo now = splits[i];
                if(i + 1 == splits.Length)
                {
                    nowTime += now.SegmentTime;
                    now.TotalTime = nowTime;
                    now.DeltaTime = new();
                    return nowTime;
                }
                SplitInfo next = splits[i + 1];
                if(next.Level > now.Level)
                {
                    i++;
                    now.TotalTime = RecalculateTimes();
                    now.DeltaTime = new();
                    i++;
                    continue;
                }
                if(next.Level < now.Level)
                {
                    nowTime += now.SegmentTime;
                    now.TotalTime = nowTime;
                    now.DeltaTime = new();
                    return nowTime;
                }

                nowTime += now.SegmentTime;
                now.TotalTime = nowTime;
                now.DeltaTime = new();
                i++;
            }

            return nowTime;
        }

        public TimeCollection CollectTotalTimes()
        {
            return splits.Last().TotalTime;
        }
    }

    private static string Serialize(RunInfo ri)
    {
        return JsonSerializer.Serialize(ri, typeof(RunInfo), SourceGeneratorContext.Default);
    }

    private static RunInfo? Deserialize(string json)
    {
        return JsonSerializer.Deserialize(json, typeof(RunInfo), SourceGeneratorContext.Default) as RunInfo;
    }

    [Fact]
    public void EditTimesPathWithStubProfileDoesNotThrow()
    {
        var game = new Game("TestGame", "test.exe", [new Category("any%", [new Split("New Split")])]);
        var cat = game.GetCategories()[0];

        // MainWindow: no PB -> fresh run info
        var ri = BuildRunInfoFromCategory(game, cat);
        var sim = new EditorSim(ri);
        sim.RecalculateTimes();
        var total = sim.CollectTotalTimes();

        // Save button -> SaveRunAsPB -> JSON
        var saved = new RunInfo(ri.GameName, ri.CategoryName, total, ri.Splits);
        var json = Serialize(saved);
        var loaded = Deserialize(json);

        Assert.NotNull(loaded);

        // Reopen the editor on the deserialized PB
        var sim2 = new EditorSim(loaded!);
        sim2.RecalculateTimes();
        sim2.CollectTotalTimes();
    }

    [Fact]
    public void EditTimesPathWithSubsplitsRoundTrip()
    {
        var parent = new Split("Parent");
        parent.AddSubsplit(new Split("Child 1"));
        parent.AddSubsplit(new Split("Child 2"));
        var game = new Game("TestGame", "test.exe", [new Category("any%", [parent, new Split("After")])]);
        var cat = game.GetCategories()[0];

        var ri = BuildRunInfoFromCategory(game, cat);
        var sim = new EditorSim(ri);
        sim.RecalculateTimes();

        var json = Serialize(new RunInfo(ri.GameName, ri.CategoryName, sim.CollectTotalTimes(), ri.Splits));
        var loaded = Deserialize(json);
        Assert.NotNull(loaded);
        var sim2 = new EditorSim(loaded!);
        sim2.RecalculateTimes();
        sim2.CollectTotalTimes();
    }

    [Fact]
    public void EditTimesPathWithCompletedRunTimes()
    {
        // A PB saved after a completed run: splits carry real segment times
        var game = new Game("NFS13", "NFS13.exe", [new Category("any%", [new Split("Race 1"), new Split("Race 2"), new Split("Race 3")])]);
        var cat = game.GetCategories()[0];
        var controller = new SplitsController(cat, null);

        var ri = new RunInfo(game.Name, cat.Name, new TimeCollection(),
                             controller.AllSplits.Select(x => x.ToSplitInfo()).ToArray());
        long t = 0;
        foreach(var s in ri.Splits)
        {
            t += TimeSpan.FromSeconds(30.5).Ticks;
            s.SegmentTime[(TimingMethod)0] = TimeSpan.FromSeconds(30.5);
            s.SegmentTime[(TimingMethod)1] = TimeSpan.FromSeconds(28.1);
            s.TotalTime[(TimingMethod)0] = new TimeSpan(t);
            s.TotalTime[(TimingMethod)1] = new TimeSpan(t - TimeSpan.FromSeconds(2.4).Ticks);
        }

        var sim = new EditorSim(ri);
        sim.RecalculateTimes();

        var json = Serialize(new RunInfo(ri.GameName, ri.CategoryName, sim.CollectTotalTimes(), ri.Splits));
        var loaded = Deserialize(json);
        Assert.NotNull(loaded);
        var sim2 = new EditorSim(loaded!);
        sim2.RecalculateTimes();
        sim2.CollectTotalTimes();
    }

    [Fact]
    public void RunGetRunInfoThrowsOnEmptyCategory()
    {
        // An empty category (no splits) makes Run.GetRunInfo() throw on AllSplits.Last()
        var game = new Game("Broken", "broken.exe", [new Category("empty", [])]);
        var controller = new SplitsController(game.GetCategories()[0], null);
        Assert.Empty(controller.AllSplits);
        Assert.Throws<InvalidOperationException>(() => controller.AllSplits.Last());
    }
}
