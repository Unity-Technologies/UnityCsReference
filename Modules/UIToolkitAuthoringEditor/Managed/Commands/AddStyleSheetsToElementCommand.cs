// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class AddStyleSheetsToElementCommand : Command<AddStyleSheetsToElementCommand>
{
    const string CommandUndoName = "Add style sheets to element";

    public static AddStyleSheetsToElementCommand GetPooled(object source, VisualElementAsset visualElementAsset, StyleSheet[] styleSheets)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualElementAsset = visualElementAsset;
        cmd.StyleSheets = styleSheets;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset visualElementAsset, StyleSheet[] styleSheets)
    {
        using var command = GetPooled(source, visualElementAsset, styleSheets);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset VisualElementAsset { get; private set; }
    public StyleSheet[] StyleSheets { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling | CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        VisualElementAsset = null;
        StyleSheets = null;
    }

    public override bool Validate()
    {
        if (VisualElementAsset == null || VisualElementAsset.visualTreeAsset == null || StyleSheets == null)
            return false;

        foreach (var styleSheet in StyleSheets)
        {
            if (styleSheet == null)
                return false;
            if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(styleSheet)))
                return false;
        }

        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualElementAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        // Remove existing style sheets so that they are re-added at the end.
        foreach (var styleSheet in StyleSheets)
            VisualElementAsset.stylesheets.Remove(styleSheet);

        VisualElementAsset.stylesheets.AddRange(StyleSheets);
        return CommandExecutionStatus.Success;
    }
}
