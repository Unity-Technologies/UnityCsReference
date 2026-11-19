// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Changes the type of elements in place. Name, classes, inline styles, style sheets, children and the
/// attributes the new type also declares are kept; the attributes it does not declare, or whose value it cannot
/// parse, are dropped. A named variant of the new type is then applied on top; its default configuration is not, as
/// it would overwrite authored values.
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
        string variantName = null, AuthoringContext requestingContext = null)
    {
        using var command = GetPooled(source, elements, elementType, variantName);
        command.RequestingContext = requestingContext;
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
        using var removedHandle = ListPool<string>.Get(out var removed);
        var scratchData = description.CreateSerializedData();
        VisualTreeAsset contextVta = null;
        var cc = default(CreationContext);

        foreach (var element in Elements)
        {
            var vta = element.visualTreeAsset;
            var parentAsset = element.parentAsset;
            var index = parentAsset.IndexOf(element);

            if (vta != contextVta)
            {
                contextVta = vta;
                cc = new CreationContext(vta);
            }

            element.fullTypeName = ElementType.FullName;
            element.xmlNamespace = vta.FindUxmlNamespaceDefinitionForTypeName(parentAsset, ElementType.FullName);
            RedundantAttributeUpgrader.UpgradeElement(element, description);
            RemoveUnparsableAttributes(element, description, scratchData, cc, removed);

            element.GetChildrenUxmlObjectAssets(fields);
            foreach (var field in fields)
            {
                var accepted = field.isField
                    ? description.IsUxmlObjectField(field.fullTypeName)
                    : AcceptsUnnamedUxmlObject(description, field.fullTypeName);
                if (accepted)
                    continue;

                field.RemoveFromHierarchy();
                var fieldName = field.fullTypeName.Substring(field.fullTypeName.LastIndexOf('.') + 1);
                removed.Add(QualifiedName(element, fieldName));
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

        if (removed.Count > 0)
        {
            Debug.LogWarning($"Replace Element Type: removed attributes and UXML objects that are not valid for " +
                $"{ElementType.Name}: {string.Join(", ", removed)}");
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(RequestingContext, toSelectAssets);
        return CommandExecutionStatus.Success;
    }

    static void RemoveUnparsableAttributes(
        VisualElementAsset element,
        UxmlSerializedDataDescription description,
        UxmlSerializedData scratchData,
        CreationContext cc,
        List<string> removed)
    {
        if (element.properties == null)
            return;

        using var unparsableHandle = ListPool<string>.Get(out var unparsable);
        foreach (var property in element.properties)
        {
            var attribute = description.FindAttributeWithUxmlName(property.name);
            if (attribute == null)
                continue;

            var result = UxmlSerializer.TryParseSerializedAttribute(property.value, scratchData, attribute, cc);
            if (!result.success && result.hasError)
                unparsable.Add(property.name);
        }

        foreach (var name in unparsable)
        {
            element.RemoveAttribute(name);
            removed.Add(QualifiedName(element, name));
        }
    }

    static string QualifiedName(VisualElementAsset element, string name)
    {
        var elementName = element.GetAttributeValue("name");
        return string.IsNullOrEmpty(elementName) ? name : elementName + "." + name;
    }

    // UxmlObjects written directly under the element, without a field name, such as the columns of a MultiColumnListView.
    static bool AcceptsUnnamedUxmlObject(UxmlSerializedDataDescription description, string fullTypeName)
    {
        var objectDataType = UxmlSerializedDataRegistry.GetDescription(fullTypeName)?.serializedDataType;
        if (objectDataType == null)
            return false;

        foreach (var attribute in description.serializedAttributes)
        {
            if (attribute is not UxmlSerializedUxmlObjectAttributeDescription uxmlObjectAttribute ||
                !string.IsNullOrEmpty(uxmlObjectAttribute.rootName))
                continue;

            var expectedType = uxmlObjectAttribute.type;
            if (uxmlObjectAttribute.isList)
                expectedType = expectedType.IsArray ? expectedType.GetElementType() : expectedType.GetGenericArguments()[0];
            if (expectedType.IsAssignableFrom(objectDataType))
                return true;
        }

        return false;
    }
}
