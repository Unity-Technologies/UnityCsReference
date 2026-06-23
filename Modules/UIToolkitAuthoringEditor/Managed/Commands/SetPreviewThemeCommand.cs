// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Command dispatched when the user selects a preview theme in the viewport theme menu.
/// </summary>
sealed class SetPreviewThemeCommand : Command<SetPreviewThemeCommand>
{
    public static SetPreviewThemeCommand GetPooled(object source, PreviewThemeState themeState, ThemeStyleSheet theme)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_ThemeState = themeState;
        cmd.Theme = theme;
        return cmd;
    }

    public static void Execute(object source, PreviewThemeState themeState, ThemeStyleSheet theme)
    {
        using var command = GetPooled(source, themeState, theme);
        UICommandQueue.Execute(command);
    }

    PreviewThemeState m_ThemeState;

    public ThemeStyleSheet Theme { get; private set; }

    protected override void Init()
    {
        base.Init();
        m_ThemeState = null;
        Theme = null;
    }

    public override CommandExecutionStatus Execute()
    {
        m_ThemeState.SelectedTheme = Theme;
        return CommandExecutionStatus.Success;
    }
}
