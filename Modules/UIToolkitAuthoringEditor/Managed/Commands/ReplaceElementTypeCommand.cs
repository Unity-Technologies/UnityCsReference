// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Changes the type of elements in place. Name, classes, inline styles, style sheets, children and the
/// attributes the new type also declares are kept; the attributes it does not declare are dropped. A named variant
/// of the new type is then applied on top; its default configuration is not, as it would overwrite authored values.
/// </summary>
internal sealed class ReplaceElementTypeCommand : Command<ReplaceElementTypeCommand>
{
    const string CommandUndoName = "Replace element type";

    public VisualElementAsset[] Elements { get; private set; }
    public Type ElementType { get; private set; }
    public string VariantName { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    public static ReplaceElementTypeCommand GetPooled(object source, VisualElementAsset[] elements, Type elementType,
        string variantName = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Elements = elements;
        cmd.ElementType = elementType;
        cmd.VariantName = variantName;
        return cmd;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] elements, Type elementType,
        string variantName = null)
    {
        using var command = GetPooled(source, elements, elementType, variantName);
        return UICommandQueue.Execute(command).Status;
    }

    public static bool Validate(VisualElementAsset[] elements)
    {
        if (elements == null || elements.Length == 0)
            return false;

        foreach (var element in elements)
        {
            if (element == null || element.visualTreeAsset == null || element.parentAsset == null)
                return false;

            // A template instance is defined by the document it instantiates, not by a type.
            if (element is TemplateAsset)
                return false;
        }

        return true;
    }

    protected override void Init()
    {
        base.Init();
        Elements = null;
        ElementType = null;
        VariantName = null;
    }

    public override bool Validate()
    {
        if (!Validate(Elements))
            return false;

        var description = ElementHierarchyUtility.GetDescription(ElementType);
        if (description == null)
            return false;

        foreach (var element in Elements)
        {
            if (!ElementHierarchyUtility.AcceptsChildOfType(element.parentAsset, ElementType) ||
                !ElementHierarchyUtility.AcceptsChildrenOf(description, element))
                return false;
        }

        return true;
    }

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
        var description = ElementHierarchyUtility.GetDescription(ElementType);
        var exporter = VisualTreeAssetExporter.Default;
        using var exportListHandle = ListPool<UxmlAsset>.Get(out var exportList);
        using var fieldsHandle = ListPool<UxmlObjectAsset>.Get(out var fields);
        using var toSelectHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);

        foreach (var element in Elements)
        {
            var vta = element.visualTreeAsset;
            var parentAsset = element.parentAsset;
            var index = parentAsset.IndexOf(element);

            element.fullTypeName = ElementType.FullName;
            element.xmlNamespace = vta.FindUxmlNamespaceDefinitionForTypeName(parentAsset, ElementType.FullName);
            RedundantAttributeUpgrader.UpgradeElement(element, description);

            element.GetChildrenUxmlObjectAssets(fields);
            foreach (var field in fields)
            {
                if (field.isField && !description.IsUxmlObjectField(field.fullTypeName))
                    field.RemoveFromHierarchy();
            }

            // A round trip through UXML rebuilds the serialized data of the new type from the kept attributes.
            exportList.Clear();
            exportList.Add(element);
            var export = exporter.ToUxmlString(vta, exportList);
            var importer = new TempVisualTreeAssetImporter();
            importer.ImportXmlFromString(export, out var replacementVta);

            try
            {
                var replacement = (VisualElementAsset)replacementVta.visualTree[0];
                element.RemoveFromHierarchy();
                parentAsset.Insert(index, replacement);
                if (!string.IsNullOrEmpty(VariantName))
                    ElementConfiguratorRegistry.Configure(ElementType, VariantName, vta, replacement);
                toSelectAssets.Add(replacement);
            }
            finally
            {
                Object.DestroyImmediate(replacementVta);
            }
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
        return CommandExecutionStatus.Success;
    }
}
