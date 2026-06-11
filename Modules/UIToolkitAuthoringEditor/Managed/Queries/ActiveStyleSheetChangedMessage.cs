// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class ActiveStyleSheetChangedMessage : Command<ActiveStyleSheetChangedMessage>
{
    public static ActiveStyleSheetChangedMessage GetPooled(CommandSources.CommandSource source, StyleSheet activeStyleSheet)
    {
        var command = GetPooled();
        command.Source = source;
        command.StyleSheet = activeStyleSheet;
        return command;
    }

    public static void Execute(CommandSources.CommandSource source, StyleSheet activeStyleSheet)
    {
        using var command = GetPooled(source, activeStyleSheet);
        UICommandQueue.Execute(command);
    }

    public override CommandCategory Category => CommandCategory.None;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
    }

    public StyleSheet StyleSheet { get; private set; }
}
