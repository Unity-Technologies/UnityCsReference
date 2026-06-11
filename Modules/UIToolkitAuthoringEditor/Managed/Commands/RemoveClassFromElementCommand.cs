// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class RemoveClassFromElementCommand : Command<RemoveClassFromElementCommand>
{
    const string CommandUndoName = "Remove class from element";

    public static RemoveClassFromElementCommand GetPooled(object source, VisualElementAsset vea, string className)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementAsset = vea;
        cmd.ClassName = className;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset vea, string className)
    {
        using var command = GetPooled(source, vea, className);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset ElementAsset { get; private set; }
    public string ClassName { get; private set; }

    public override string UndoName => CommandUndoName;

    protected override void Init()
    {
        ElementAsset = null;
        ClassName = null;
        base.Init();
    }

    public override bool Validate()
    {
        return ElementAsset != null &&
               !string.IsNullOrEmpty(ClassName);
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(ElementAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        ElementAsset.RemoveStyleClass(ClassName);
        return CommandExecutionStatus.Success;
    }
}
