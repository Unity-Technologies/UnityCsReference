// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class ReparentElementsCommand : Command<ReparentElementsCommand>
{
    const string CommandUndoName = "Reparent elements";

    public static ReparentElementsCommand GetPooled(object source, VisualElementAsset parentAsset, int index, VisualElementAsset[] childrenAssets)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ParentAsset = parentAsset;
        cmd.Index = index;
        cmd.ChildrenAssets = childrenAssets;
        return cmd;
    }

    /// <returns>
    /// The status of the execution, so a caller that has to stay in sync with the assets — a drop reporting
    /// whether it moved anything — can tell a refused command from an applied one.
    /// </returns>
    public static CommandExecutionStatus Execute(object source, VisualElementAsset parentAsset, int index, VisualElementAsset[] childrenAssets)
    {
        using var command = GetPooled(source, parentAsset, index, childrenAssets);
        return UICommandQueue.Execute(command).Status;
    }

    public VisualElementAsset ParentAsset { get; private set; }
    public int Index { get; private set; }
    public VisualElementAsset[] ChildrenAssets { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        ParentAsset = null;
        Index = -1;
        ChildrenAssets = null;
    }

    public override bool Validate()
    {
        if (ParentAsset == null || ParentAsset.visualTreeAsset == null)
            return false;
        if (Index < -1 || Index > ParentAsset.childCount)
            return false;
        if (ChildrenAssets == null)
            return false;
        foreach (var asset in ChildrenAssets)
        {
            if (asset == null)
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        var visualTreeAsset = ParentAsset.visualTreeAsset;
        context.RecordUndo(visualTreeAsset);

        foreach (var asset in ChildrenAssets)
        {
            var vta = asset.visualTreeAsset;
            {
                context.RecordUndo(vta);
                context.RecordUndo(vta.inlineSheet);
            }
        }

        context.RecordUndo(visualTreeAsset.inlineSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        for (var i = 0; i < ChildrenAssets.Length; ++i)
        {
            var asset = ChildrenAssets[i];
            if (Index < 0)
            {
                ParentAsset.Add(asset);
                continue;
            }
            var index = Index + i;
            ParentAsset.Insert(index, asset);
        }

        return CommandExecutionStatus.Success;
    }
}
