using ImGuiCodeEditor;
using ImGuiNET;

namespace SpeedTool.Tests;

public sealed class TextEditorTests
{
    static TextEditorTests()
    {
        ImGui.CreateContext();
    }
    [Fact]
    public void MultiLineInsertAtLineStartDoesNotDuplicateContent()
    {
        var te = new TextEditor();
        te.SetText("hello world");

        te.InsertTextAt(new Coordinates(0, 0), "AB\nCD");

        Assert.Equal("AB\nCDhello world\n", te.GetText());
    }

    [Fact]
    public void MultiLineInsertKeepsExistingLines()
    {
        var te = new TextEditor();
        te.SetText("local a = 1\nlocal b = 2");

        te.InsertTextAt(new Coordinates(0, 0), "-- header\n");

        Assert.Equal("-- header\nlocal a = 1\nlocal b = 2\n", te.GetText());
    }

    [Fact]
    public void MultiLineInsertHandlesCarriageReturns()
    {
        var te = new TextEditor();
        te.SetText("start");

        te.InsertTextAt(new Coordinates(0, 0), "one\r\ntwo\r\n");

        Assert.Equal("one\ntwo\nstart\n", te.GetText());
    }

    [Fact]
    public void InsertAtEndOfLineAppendsNewLine()
    {
        var te = new TextEditor();
        te.SetText("abc");

        te.InsertTextAt(new Coordinates(0, 3), "\nnext");

        Assert.Equal("abc\nnext\n", te.GetText());
    }
}
