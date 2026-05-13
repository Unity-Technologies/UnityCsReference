// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using static Unity.UI.Builder.BuilderUxmlAttributesView;

namespace Unity.UI.Builder
{
    internal static class VisualElementExtensions
    {
        public static bool HasLinkedAttributeDescription(this VisualElement ve)
        {
            return ve.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) is UxmlSerializedAttributeDescription;
        }

        public static UxmlSerializedAttributeDescription GetLinkedAttributeDescription(this VisualElement ve)
        {
            // UxmlSerializedFields have a UxmlSerializedDataAttributeField as the parent
            var dataField = ve as UxmlSerializedDataAttributeField ?? ve.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            return (dataField ?? ve).GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlSerializedAttributeDescription;
        }

        public static void SetLinkedAttributeDescription(this VisualElement ve, UxmlSerializedAttributeDescription attribute)
        {
            ve.SetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName, attribute);
        }

        public static void SetInspectorStylePropertyName(this VisualElement ve, string styleName)
        {
            ve.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, styleName);
            var cSharpStyleName = BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName);
            ve.SetProperty(BuilderConstants.InspectorStyleBindingPropertyNameVEPropertyName, $"style.{cSharpStyleName}");
        }

        public static BuilderStyleRow GetContainingRow(this VisualElement ve)
        {
            return ve.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
        }

        public static void SetContainingRow(this VisualElement ve, BuilderStyleRow row)
        {
            ve.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, row);
        }

        public static List<VisualElement> GetLinkedFieldElements(this BuilderStyleRow row)
        {
            return row.GetProperty(BuilderConstants.InspectorLinkedFieldsForStyleRowVEPropertyName) as List<VisualElement>;
        }

        public static void AddLinkedFieldElement(this BuilderStyleRow row, VisualElement fieldElement)
        {
            var list = row.GetProperty(BuilderConstants.InspectorLinkedFieldsForStyleRowVEPropertyName) as List<VisualElement>;
            list ??= new List<VisualElement>();
            list.Add(fieldElement);
            row.SetProperty(BuilderConstants.InspectorLinkedFieldsForStyleRowVEPropertyName, list);
        }

        public static VisualTreeAsset GetVisualTreeAsset(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedVisualTreeAssetVEPropertyName);
            if (obj == null)
                return null;

            var vta = obj as VisualTreeAsset;
            return vta;
        }

        public static VisualElementAsset GetVisualElementAsset(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName);
            if (obj == null)
                return null;

            var vea = obj as VisualElementAsset;
            return vea;
        }

        public static VisualTreeAsset GetInstancedVisualTreeAsset(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName);
            if (obj == null)
                return null;

            var vta = obj as VisualTreeAsset;
            return vta;
        }

        public static VisualElementAsset GetVisualElementAssetInTemplate(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.visualElementAsset;

            if (obj == null)
                return null;

            var vea = obj as VisualElementAsset;
            return vea;
        }

        public static void SetVisualElementAsset(this VisualElement element, VisualElementAsset vea)
        {
            element.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);
        }

        public static StyleSheet GetStyleSheet(this VisualElement element)
        {
            return BuilderSharedStyles.GetStyleSheetElementProperty(element);
        }

        public static StyleComplexSelector GetStyleComplexSelector(this VisualElement element)
        {
            return BuilderSharedStyles.GetSelectorProperty(element);
        }

        public static bool IsLinkedToAsset(this VisualElement element)
        {
            var vta = element.GetVisualTreeAsset();
            if (vta != null)
                return true;

            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return true;

            var styleSheet = element.GetStyleSheet();
            if (styleSheet != null)
                return true;

            var scs = element.GetStyleComplexSelector();
            if (scs != null)
                return true;

            return false;
        }

        public static bool IsPartOfCurrentDocument(this VisualElement element)
        {
            return element.GetVisualElementAsset() != null || element.GetVisualTreeAsset() != null || BuilderSharedStyles.IsDocumentElement(element);
        }

        public static bool IsPartOfActiveVisualTreeAsset(this VisualElement element, BuilderDocument builderDocument)
        {
            var isSubDocument = builderDocument != null && builderDocument.activeOpenUXMLFile.isChildSubDocument;
            var elementVTA = element.GetVisualTreeAsset();
            var activeVTA = builderDocument == null ? elementVTA : builderDocument.activeOpenUXMLFile.visualTreeAsset;

            var belongsToActiveVisualTreeAsset = (VisualTreeAsset)element.GetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName) == builderDocument?.visualTreeAsset;
            var hasAssetLink = element.GetVisualElementAsset() != null && belongsToActiveVisualTreeAsset;
            var hasVTALink = elementVTA != null && elementVTA == activeVTA && !(element is TemplateContainer);

            var isDocumentRootElement = !isSubDocument && BuilderSharedStyles.IsDocumentElement(element);

            return hasAssetLink || hasVTALink || isDocumentRootElement;
        }

        public static bool IsActiveSubDocumentRoot(this VisualElement element, BuilderDocument builderDocument)
        {
            if (!builderDocument.activeOpenUXMLFile.isChildSubDocument ||
                element is not TemplateContainer templateContainer ||
                templateContainer.templateSource != builderDocument.activeOpenUXMLFile.visualTreeAsset ||
                element.visualTreeAssetSource.ResolveTemplate(templateContainer.templateId) != builderDocument.activeOpenUXMLFile.visualTreeAsset)
            {
                return false;
            }

            var templateAsset = templateContainer.GetVisualElementAsset() as TemplateAsset;
            var activeOpenUxmlFile = builderDocument.activeOpenUXMLFile;
            return templateAsset == activeOpenUxmlFile.templateAsset;
        }

        public static bool IsSelector(this VisualElement element)
        {
            return BuilderSharedStyles.IsSelectorElement(element);
        }

        public static bool IsParentSelector(this VisualElement element)
        {
            return BuilderSharedStyles.IsParentSelectorElement(element);
        }

        public static bool IsStyleSheet(this VisualElement element)
        {
            return BuilderSharedStyles.IsStyleSheetElement(element);
        }

        public static StyleSheet GetClosestStyleSheet(this VisualElement element)
        {
            if (element == null)
                return null;

            var ss = element.GetStyleSheet();
            if (ss != null)
                return ss;

            return element.parent.GetClosestStyleSheet();
        }

        public static VisualElement GetClosestElementPartOfCurrentDocument(this VisualElement element)
        {
            if (element == null)
                return null;

            if (element.IsPartOfCurrentDocument())
                return element;

            return element.parent.GetClosestElementPartOfCurrentDocument();
        }

        public static VisualElement GetClosestElementThatIsValid(this VisualElement element, Func<VisualElement, bool> test)
        {
            if (element == null)
                return null;

            if (test(element))
                return element;

            return element.parent.GetClosestElementPartOfCurrentDocument();
        }

        static void FindElementsRecursive(VisualElement parent, Func<VisualElement, bool> predicate, List<VisualElement> selected)
        {
            if (predicate(parent))
                selected.Add(parent);

            foreach (var child in parent.Children())
                FindElementsRecursive(child, predicate, selected);
        }

        public static List<VisualElement> FindElements(this VisualElement element, Func<VisualElement, bool> predicate)
        {
            var selected = new List<VisualElement>();

            FindElementsRecursive(element, predicate, selected);

            return selected;
        }

        public static VisualElement FindElement(this VisualElement element, Func<VisualElement, bool> predicate)
        {
            var selected = new List<VisualElement>();

            FindElementsRecursive(element, predicate, selected);

            if (selected.Count == 0)
                return null;

            return selected[0];
        }

        static void FindSelectedElementsRecursive(VisualElement parent, List<VisualElement> selected)
        {
            if (SelectionUtility.IsSelected(parent))
                selected.Add(parent);

            foreach (var child in parent.Children())
                FindSelectedElementsRecursive(child, selected);
        }

        public static List<VisualElement> FindSelectedElements(this VisualElement element)
        {
            var selected = new List<VisualElement>();

            FindSelectedElementsRecursive(element, selected);

            return selected;
        }

        public static bool IsFocused(this VisualElement element)
        {
            if (element.focusController == null)
                return false;

            return element.focusController.focusedElement == element;
        }

        public static bool HasAnyAncestorInList(this VisualElement element, IEnumerable<VisualElement> ancestors)
        {
            foreach (var ancestor in ancestors)
            {
                if (ancestor == element)
                    continue;

                if (element.HasAncestor(ancestor))
                    return true;
            }

            return false;
        }

        public static bool HasAncestor(this VisualElement element, VisualElement ancestor)
        {
            if (ancestor == null || element == null)
                return false;

            if (element == ancestor)
                return true;

            return element.parent.HasAncestor(ancestor);
        }

        public static VisualElement GetFirstAncestorWithClass(this VisualElement element, string className)
        {
            if (element == null)
                return null;

            if (element.ClassListContains(className))
                return element;

            return element.parent.GetFirstAncestorWithClass(className);
        }

        static CustomStyleProperty<string> s_BuilderElementStyleProperty = new CustomStyleProperty<string>("--builder-style");

        public static void RegisterCustomBuilderStyleChangeEvent(this VisualElement element, Action<BuilderElementStyle> onElementStyleChanged)
        {
            element.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(s_BuilderElementStyleProperty, out var value))
                {
                    BuilderElementStyle elementStyle;
                    try
                    {
                        elementStyle = (BuilderElementStyle)Enum.Parse(typeof(BuilderElementStyle), value, true);
                        onElementStyleChanged.Invoke(elementStyle);
                    }
                    catch
                    {
                        throw new NotSupportedException($"The `{value}` value is not supported for {s_BuilderElementStyleProperty.name} property.");
                    }
                }
            });
        }

        public static VisualElement GetVisualInput(this VisualElement ve)
        {
            var visualInput = ve.GetValueByReflection("visualInput") as VisualElement;

            if (visualInput == null)
                visualInput = ve.Q("unity-visual-input");

            return visualInput;
        }

        public static FieldStatusIndicator GetFieldStatusIndicator(this VisualElement field)
        {
            // UxmlSerialization uses a common parent.
            var dataField = field as UxmlSerializedDataAttributeField ?? field.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            if (dataField != null)
                field = dataField;

            var statusIndicator = field.GetProperty(FieldStatusIndicator.s_FieldStatusIndicatorVEPropertyName) as FieldStatusIndicator;

            if (statusIndicator == null)
            {
                var row = field.GetContainingRow();

                if (row == null)
                    return null;

                // If the field has a name then look for a FieldStatusIndicator in the same containing row that has
                // targetFieldName matching the field's name.
                if (!string.IsNullOrEmpty(field.name))
                {
                    statusIndicator = row.Query<FieldStatusIndicator>().Where((b) => b.targetFieldName == field.name);
                }

                // If a status indicator matching the field's name could not be found then pick the first FieldMenuButton in the row.
                if (statusIndicator == null)
                {
                    statusIndicator = row.Q<FieldStatusIndicator>();
                }

                // If no status indicator could not be found in the row then create a new one and insert it to
                // the row right after the Override indicators container.
                if (statusIndicator == null)
                {
                    statusIndicator = new FieldStatusIndicator();

                    row.hierarchy.Insert(1, statusIndicator);
                }

                statusIndicator.targetField = field;
            }

            return statusIndicator;
        }

        public static string GetUxmlTypeName(this VisualElement element)
        {
            if (null == element)
                return null;

            var desc = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            if (null != desc)
                return desc.uxmlName;
            return element.typeName;
        }
    }

    enum BuilderElementStyle
    {
        Default,
        Highlighted
    }
}
