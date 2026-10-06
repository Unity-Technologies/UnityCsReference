// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class SetElementNameCommand : Command<SetElementNameCommand>
{
    const string CommandUndoName = "Rename element";

    public static SetElementNameCommand GetPooled(object source, VisualElementAsset elementVea, string name)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementAsset = elementVea;
        cmd.Name = name;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset elementVea, string name)
    {
        using var command = GetPooled(source, elementVea, name);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset ElementAsset { get; private set; }
    public string Name { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        ElementAsset = null;
        Name = null;
    }

    public override bool Validate() => ElementAsset != null && ElementAsset.visualTreeAsset != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(ElementAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        UxmlAssetUtilities.SetAttributeAndSyncSerializedData(ElementAsset, nameof(VisualElement.name), Name);
        return CommandExecutionStatus.Success;
    }
}
