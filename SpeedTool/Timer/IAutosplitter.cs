namespace SpeedTool.Timer;

public interface IAutosplitter : ITimerSource, IDisposable
{
    string InjectedExeName { get; }

    void ReloadScript(string script);

    void Cycle();
}
