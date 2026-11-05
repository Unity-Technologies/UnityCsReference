// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Removes elements and moves their children into the slot they occupied, keeping their order.
/// </summary>
internal sealed class UnwrapElementsCommand : Command<UnwrapElementsCommand>
{
    const string CommandUndoName = "Unwrap elements";

    public VisualElementAsset[] Elements { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    public static UnwrapElementsCommand GetPooled(object source, VisualElementAsset[] elements)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Elements = elements;
        return cmd;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] elements)
    {
        using var command = GetPooled(source, elements);
        return UICommandQueue.Execute(command).Status;
    }

    public static bool Validate(VisualElementAsset[] elements)
    {
        if (elements == null || elements.Length == 0)
            return false;

        foreach (var element in elements)
        {
            if (element == null || element.visualTreeAsset == null)
                return false;

            // Unwrapping a template instance would mean unpacking it, which is its own operation.
            if (element is TemplateAsset)
                return false;

            if (element.parentAsset is not VisualElementAsset parentAsset)
                return false;

            var parentDescription = UxmlSerializedDataRegistry.GetDescription(parentAsset.fullTypeName);
            if (parentDescription != null && !ElementHierarchyUtility.AcceptsChildrenOf(parentDescription, element))
                return false;
        }

        return true;
    }

    protected override void Init()
    {
        base.Init();
        Elements = null;
    }

    public override bool Validate() => Validate(Elements);

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var element in Elements)
        {
            var vta = element.visualTreeAsset;
            if (set.Add(vta))
            {
                context.RecordUndo(vta);
                context.RecordUndo(vta.inlineSheet);
            }
        }
    }

    public override CommandExecutionStatus Execute()
    {
        using var childrenHandle = ListPool<VisualElementAsset>.Get(out var children);
        using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);

        foreach (var element in Elements)
        {
            var parentAsset = element.parentAsset;
            var index = parentAsset.IndexOf(element);

            children.Clear();
            for (var i = 0; i < element.childCount; ++i)
            {
                if (element[i] is VisualElementAsset child)
                    children.Add(child);
            }

            for (var i = 0; i < children.Count; ++i)
                parentAsset.Insert(index + 1 + i, children[i]);

            element.RemoveFromHierarchy();
            toSelectAssets.AddRange(children);
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
        return CommandExecutionStatus.Success;
    }
}
