// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Writes the editor extension setting (<see cref="EditorExtensionAuthoring"/>) on the root of a document.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal sealed class SetEditorExtensionModeCommand : Command<SetEditorExtensionModeCommand>
{
    const string CommandUndoName = "Set editor extension authoring";

    public static SetEditorExtensionModeCommand GetPooled(object source, VisualTreeAsset visualTreeAsset, bool enabled)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = visualTreeAsset;
        cmd.Enabled = enabled;
        return cmd;
    }

    public static CommandExecutionStatus Execute(object source, VisualTreeAsset visualTreeAsset, bool enabled)
    {
        using var command = GetPooled(source, visualTreeAsset, enabled);
        return UICommandQueue.Execute(command).Status;
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public bool Enabled { get; private set; }

    public override string UndoName => CommandUndoName;

    // Changes no element: nothing has to re-clone or re-style.
    public override CommandCategory Category => CommandCategory.None;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        Enabled = false;
    }

    public override bool Validate() => VisualTreeAsset != null && VisualTreeAsset.visualTree != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        VisualTreeAsset.visualTree.SetAttribute(EditorExtensionAuthoring.AttributeName, Enabled.ToString());

        // The preview switches panels right away, which re-tracks the document: it must already count as dirty.
        EditorUtility.SetDirty(VisualTreeAsset);
        return CommandExecutionStatus.Success;
    }
}
