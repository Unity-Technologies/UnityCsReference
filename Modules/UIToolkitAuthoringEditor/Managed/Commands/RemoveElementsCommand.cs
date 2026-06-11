// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveElementsCommand : Command<RemoveElementsCommand>
{
    const string CommandUndoName = "Delete elements";

    public static RemoveElementsCommand GetPooled(object source, VisualElementAsset[] toRemoveAssets)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ToRemoveAssets = toRemoveAssets;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset[] toRemoveAssets)
    {
        using var command = GetPooled(source, toRemoveAssets);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset[] ToRemoveAssets { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        ToRemoveAssets = null;
    }

    public override bool Validate()
    {
        if (ToRemoveAssets == null)
            return false;
        foreach (var asset in ToRemoveAssets)
        {
            if (asset == null || asset.visualTreeAsset == null)
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ToRemoveAssets)
        {
            var vta = asset.visualTreeAsset;
            if (vta && set.Add(vta))
                context.RecordUndo(vta);
        }
    }

    public override CommandExecutionStatus Execute()
    {
        foreach (var asset in ToRemoveAssets)
            asset.RemoveFromHierarchy();
        return CommandExecutionStatus.Success;
    }
}
