// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveElementsCommand : Command<RemoveElementsCommand>
{
    const string CommandUndoName = "Delete elements";

    public VisualElementAsset[] ElementsToRemove { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    public static RemoveElementsCommand GetPooled(object source, VisualElementAsset[] elements)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementsToRemove = elements;
        return cmd;
    }

    public static bool Validate(object source, VisualElementAsset[] elements)
    {
        using var cmd = GetPooled(source, elements);
        return cmd.Validate();
    }

    public static bool Validate(ReadOnlySpan<VisualElementAsset> elements)
    {
        if (elements.IsEmpty)
            return false;
        foreach (var element in elements)
        {
            if (element == null || element.visualTreeAsset == null)
                return false;
        }
        return true;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] elements)
    {
        using var command = GetPooled(source, elements);
        return UICommandQueue.Execute(command).Status;
    }

    protected override void Init()
    {
        base.Init();
        ElementsToRemove = null;
    }

    public override bool Validate()
        => Validate(ElementsToRemove.AsSpan());

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ElementsToRemove)
        {
            var vta = asset.visualTreeAsset;
            if (vta && set.Add(vta))
                context.RecordUndo(vta);
        }
    }

    public override CommandExecutionStatus Execute()
    {
        foreach (var asset in ElementsToRemove)
            asset.RemoveFromHierarchy();
        return CommandExecutionStatus.Success;
    }
}
