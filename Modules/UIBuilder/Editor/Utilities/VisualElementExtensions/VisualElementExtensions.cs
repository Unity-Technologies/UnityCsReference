// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static Unity.UI.Builder.BuilderUxmlAttributesView;

namespace Unity.UI.Builder
{
    internal static class VisualElementExtensions
    {
        static readonly List<string> s_SkippedAttributeNames = new List<string>()
        {
            "content-container",
            "class",
            "style",
            "template",
        };

        public static bool HasLinkedAttributeDescription(this VisualElement ve)
        {
            return ve.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) is UxmlAttributeDescription;
        }

        public static UxmlAttributeDescription GetLinkedAttributeDescription(this VisualElement ve)
        {
            // UxmlSerializedFields have a UxmlSerializedDataAttributeField as the parent
            var dataField = ve as UxmlSerializedDataAttributeField ?? ve.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            return (dataField ?? ve).GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlAttributeDescription;
        }

        public static void SetLinkedAttributeDescription(this VisualElement ve, UxmlAttributeDescription attribute)
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

        public static List<UxmlAttributeDescription> GetAttributeDescriptions(this VisualElement ve, bool useTraits = false)
        {
            var uxmlQualifiedName = GetUxmlQualifiedName(ve);

            var desc = UxmlSerializedDataRegistry.GetDescription(uxmlQualifiedName);
            if (desc != null && !useTraits)
                return desc.serializedAttributes.ToList<UxmlAttributeDescription>();

            var attributeList = new List<UxmlAttributeDescription>();
            if (!VisualElementFactoryRegistry.TryGetValue(uxmlQualifiedName, out var factoryList))
                return attributeList;

            #pragma warning disable CS0618 // Type or member is obsolete
            foreach (IUxmlFactory f in factoryList)
            {
                // For user created types, they may return null for uxmlAttributeDescription, so we need to check in order not to crash.
                if (f.uxmlAttributesDescription != null)
                {
                    foreach (var a in f.uxmlAttributesDescription)
                    {
                        // For user created types, they may `yield return null` which would create an array with a null, so we need
                        // to check in order not to crash.
                        if (a == null || s_SkippedAttributeNames.Contains(a.name))
                            continue;

                        attributeList.Add(a);
                    }
                }
            }
            #pragma warning restore CS0618 // Type or member is obsolete

            return attributeList;
        }

        static string GetUxmlQualifiedName(VisualElement ve)
        {
            var uxmlQualifiedName = ve.GetType().FullName;

            // Try get uxmlQualifiedName from the UxmlFactory.
            var factoryTypeName = $"{ve.GetType().FullName}+UxmlFactory";
            var asm = ve.GetType().Assembly;
            var factoryType = asm.GetType(factoryTypeName);
            if (factoryType != null)
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                var factoryTypeInstance = (IUxmlFactory)Activator.CreateInstance(factoryType);
                if (factoryTypeInstance != null)
                {
                    uxmlQualifiedName = factoryTypeInstance.uxmlQualifiedName;
                }
                #pragma warning restore CS0618 // Type or member is obsolete
            }

            return uxmlQualifiedName;
        }

        public static Dictionary<string, string> GetOverriddenAttributes(this VisualElement ve)
        {
            var attributeList = ve.GetAttributeDescriptions();
            var overriddenAttributes = new Dictionary<string, string>();

            foreach (var attribute in attributeList)
            {
                if (attribute?.name == null)
                    continue;

                if (attribute is UxmlSerializedAttributeDescription attributeDescription)
                {
                    // UxmlSerializedData
                    if (attributeDescription.TryGetValueFromObject(ve, out var value) &&
                        UxmlAttributeComparison.ObjectEquals(value, attributeDescription.defaultValue))
                    {
                        continue;
                    }

                    string valueAsString = null;
                    if (value != null)
                        UxmlAttributeConverter.TryConvertToString(value, ve.visualTreeAssetSource, out valueAsString);
                    overriddenAttributes.Add(attribute.name, valueAsString);
                }
                else
                {
                    // UxmlTraits
                    var veType = ve.GetType();
                    var camel = BuilderNameUtilities.ConvertDashToCamel(attribute.name);
                    var fieldInfo = veType.GetProperty(camel);
                    if (fieldInfo != null)
                    {
                        var veValueAbstract = fieldInfo.GetValue(ve, null);
                        if (veValueAbstract == null)
                            continue;

                        var veValueStr = veValueAbstract.ToString();
                        if (veValueStr == "False")
                            veValueStr = "false";
                        else if (veValueStr == "True")
                            veValueStr = "true";

                        // The result of Type.ToString is not enough for us to find the correct Type.
                        if (veValueAbstract is Type type)
                            veValueStr = $"{type.FullName}, {type.Assembly.GetName().Name}";

                        if (veValueAbstract is IEnumerable<string> enumerable)
                            veValueStr = string.Join(",", enumerable);

                        var attributeValueStr = attribute.defaultValueAsString;
                        if (veValueStr == attributeValueStr)
                            continue;

                        overriddenAttributes.Add(attribute.name, veValueStr);
                    }
                    // This is a special patch that allows to search for built-in elements' attribute specifically
                    // without needing to add to the public API.
                    // Allowing to search for internal/private properties in all cases could lead to unforeseen issues.
                    else if (ve is EnumField or EnumFlagsField && camel == "type")
                    {
                        fieldInfo = veType.GetProperty(camel, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var veValueAbstract = fieldInfo.GetValue(ve, null);
                        if (!(veValueAbstract is Type type))
                            continue;

                        var veValueStr = $"{type.FullName}, {type.Assembly.GetName().Name}";
                        var attributeValueStr = attribute.defaultValueAsString;
                        if (veValueStr == attributeValueStr)
                            continue;
                        overriddenAttributes.Add(attribute.name, veValueStr);
                    }
                }
            }

            return overriddenAttributes;
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

            var obj = element.GetProperty(VisualTreeAsset.LinkedVEAInTemplatePropertyName);

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
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName);
            if (obj == null)
                return null;

            var styleSheet = obj as StyleSheet;
            return styleSheet;
        }

        public static StyleComplexSelector GetStyleComplexSelector(this VisualElement element)
        {
            if (element == null)
                return null;

            var obj = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName);
            if (obj == null)
                return null;

            var scs = obj as StyleComplexSelector;
            return scs;
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

        public static bool IsSelected(this VisualElement element)
        {
            var vta = element.GetVisualTreeAsset();
            if (vta != null)
                return vta.IsSelected();

            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return vea.IsSelected();

            var veaInTemplate = element.GetVisualElementAssetInTemplate();
            if (veaInTemplate != null)
                return veaInTemplate.IsSelected();

            var styleSheet = element.GetStyleSheet();
            if (styleSheet != null)
                return styleSheet.IsSelected();

            var scs = element.GetStyleComplexSelector();
            if (scs != null)
                return scs.IsSelected();

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
                templateContainer.templateSource != builderDocument.activeOpenUXMLFile.visualTreeAsset)
            {
                return false;
            }

            var templateAsset = templateContainer.GetVisualElementAsset() as TemplateAsset;
            var activeOpenUxmlFile = builderDocument.activeOpenUXMLFile;
            var templateAssetIndex =
                activeOpenUxmlFile.openSubDocumentParent.visualTreeAsset.templateAssets.IndexOf(templateAsset);
            return templateAssetIndex == activeOpenUxmlFile.openSubDocumentParentSourceTemplateAssetIndex;
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
            if (parent.IsSelected())
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

        public static IBuilderUxmlAttributeFieldFactory GetFieldFactory(this VisualElement field) => field.GetProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName) as IBuilderUxmlAttributeFieldFactory;

        public static string GetUxmlTypeName(this VisualElement element)
        {
            if (null == element)
                return null;

            var desc = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            if (null != desc)
                return desc.uxmlName;

            if (VisualElementFactoryRegistry.TryGetValue(element.fullTypeName, out var factories))
                return factories[0].uxmlName;

            if (VisualElementFactoryRegistry.TryGetValue(element.GetType(), out factories))
                return factories[0].uxmlName;

            return element.typeName;
        }

        public static string GetUxmlFullTypeName(this VisualElement element)
        {
            if (null == element)
                return null;

            var desc = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            if (null != desc)
                return desc.uxmlFullName;

            if (VisualElementFactoryRegistry.TryGetValue(element.fullTypeName, out var factories))
                return factories[0].uxmlQualifiedName;

            if (VisualElementFactoryRegistry.TryGetValue(element.GetType(), out factories))
                return factories[0].uxmlQualifiedName;

            return null;
        }
    }

    enum BuilderElementStyle
    {
        Default,
        Highlighted
    }
}
