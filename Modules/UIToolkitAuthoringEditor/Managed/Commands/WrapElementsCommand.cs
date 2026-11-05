// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Moves contiguous siblings into a new parent created at the index of the first of them.
/// </summary>
internal sealed class WrapElementsCommand : Command<WrapElementsCommand>
{
    const string CommandUndoName = "Wrap elements";

    public VisualElementAsset[] Elements { get; private set; }
    public Type WrapperType { get; private set; }
    public string VariantName { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    public static WrapElementsCommand GetPooled(object source, VisualElementAsset[] elements, Type wrapperType,
        string variantName = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Elements = elements;
        cmd.WrapperType = wrapperType;
        cmd.VariantName = variantName;
        return cmd;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] elements, Type wrapperType,
        string variantName = null)
    {
        using var command = GetPooled(source, elements, wrapperType, variantName);
        return UICommandQueue.Execute(command).Status;
    }

    public static bool Validate(VisualElementAsset[] elements)
    {
        if (elements == null || elements.Length == 0)
            return false;

        var parentAsset = elements[0]?.parentAsset;
        if (parentAsset is not VisualElementAsset || parentAsset.visualTreeAsset == null)
            return false;

        var minIndex = int.MaxValue;
        var maxIndex = -1;
        foreach (var element in elements)
        {
            if (element == null || element.parentAsset != parentAsset)
                return false;

            var index = parentAsset.IndexOf(element);
            minIndex = Math.Min(minIndex, index);
            maxIndex = Math.Max(maxIndex, index);
        }

        // Contiguous among the element children; UxmlObject children in between do not break the run.
        for (var i = minIndex; i <= maxIndex; ++i)
        {
            if (parentAsset[i] is VisualElementAsset sibling && Array.IndexOf(elements, sibling) < 0)
                return false;
        }

        return true;
    }

    protected override void Init()
    {
        base.Init();
        Elements = null;
        WrapperType = null;
        VariantName = null;
    }

    public override bool Validate()
    {
        if (!Validate(Elements))
            return false;

        var description = ElementHierarchyUtility.GetDescription(WrapperType);
        if (description == null)
            return false;

        if (!ElementHierarchyUtility.AcceptsChildOfType(Elements[0].parentAsset, WrapperType))
            return false;

        foreach (var element in Elements)
        {
            if (!ElementHierarchyUtility.AcceptsChild(description, element))
                return false;
        }

        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        var vta = Elements[0].visualTreeAsset;
        context.RecordUndo(vta);
        context.RecordUndo(vta.inlineSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var parentAsset = (VisualElementAsset)Elements[0].parentAsset;
        var vta = parentAsset.visualTreeAsset;

        var indices = new int[Elements.Length];
        var ordered = new VisualElementAsset[Elements.Length];
        for (var i = 0; i < Elements.Length; ++i)
        {
            indices[i] = parentAsset.IndexOf(Elements[i]);
            ordered[i] = Elements[i];
        }
        Array.Sort(indices, ordered);

        var wrapper = vta.AddElementOfType(parentAsset, WrapperType.FullName);
        wrapper.serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(WrapperType);

        // A default configuration sets up an empty element, so it could add children beside the wrapped ones.
        if (!string.IsNullOrEmpty(VariantName))
            ElementConfiguratorRegistry.Configure(WrapperType, VariantName, vta, wrapper);

        parentAsset.Insert(indices[0], wrapper);

        foreach (var element in ordered)
            wrapper.Add(element);

        using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);
        toSelectAssets.Add(wrapper);
        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);

        return CommandExecutionStatus.Success;
    }
}
