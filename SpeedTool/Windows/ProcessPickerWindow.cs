using System.Numerics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SpeedTool.Platform;
using SpeedTool.Splits;

namespace SpeedTool.Windows;

using Window = Platform.Window;

public sealed class ProcessPickerWindow : Window
{
    public ProcessPickerWindow(string? preselect = null) : base(WindowOptions.Default, new Vector2D<int>(520, 240))
    {
        platform = Platform.Platform.SharedPlatform;
        Text = "SpeedTool -- Attach to process";
        processes = ProcessList.GetRunningExeNames();
        if(preselect != null)
        {
            var index = Array.FindIndex(processes, p => p.Equals(preselect, StringComparison.OrdinalIgnoreCase));
            if(index != -1)
                selected = index;
        }
    }

    protected override void OnUI(double dt)
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowBgAlpha(0.0f);
        ImGui.Begin("AttachToProcessWindow", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration |
                                                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

        ImGui.PushFont(GetFont("UI"));

        ImGui.Text("Pick the process to attach the timer to:");
        if(ImGui.BeginCombo("##process", processes.Length > 0 ? processes[selected] : "<no running processes>"))
        {
            for(int i = 0; i < processes.Length; i++)
            {
                var isSelected = i == selected;
                if(ImGui.Selectable(processes[i], ref isSelected))
                    selected = i;
                if(isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.Separator();

        if(processes.Length > 0)
        {
            if(ImGui.Button("Load profile"))
            {
                var path = platform.FindKnownGameFile(processes[selected]);
                if(path != null)
                {
                    platform.LoadGame(Game.LoadFromFile(path));
                    Close();
                }
                else
                    noProfile = true;
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Load an existing game profile for this exe");

            ImGui.SameLine();

            if(ImGui.Button("Create profile"))
            {
                var game = CreateStubGame(processes[selected]);
                platform.LoadGame(game);
                platform.AddWindow(new GameEditorWindow(game));
                Close();
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Start a new profile pre-filled with this exe, then edit and save it");

            ImGui.SameLine();

            if(ImGui.Button("Ignore"))
            {
                platform.IgnoreGameExe(processes[selected]);
                Close();
            }

            if(noProfile)
                ImGui.Text("No known profile for this process. Create one?");
        }
        else
        {
            ImGui.Text("No running processes found.");
        }

        ImGui.PopFont();
        ImGui.End();
    }

    private static Game CreateStubGame(string exeName)
    {
        var name = exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? exeName[..^4] : exeName;
        return new Game(name, exeName, [new Category("any%", [new Split("New Split")])]);
    }

    private readonly Platform.Platform platform;
    private readonly string[] processes;
    private int selected;
    private bool noProfile;
}
