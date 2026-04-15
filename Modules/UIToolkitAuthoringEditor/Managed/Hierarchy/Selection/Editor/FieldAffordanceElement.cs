// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UxmlAttributeFlags = UnityEngine.UIElements.UxmlSerializedData.UxmlAttributeFlags;

namespace Unity.UIToolkit.Editor
{
    internal class FieldAffordanceElement : VisualElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new FieldAffordanceElement();
        }

        // Tooltips
        public static readonly string FieldStatusIndicatorDefaultTooltip = "Default Value";
        public static readonly string FieldStatusIndicatorInlineTooltip = "Inline Value\n\nValue is set directly from the property field.";
        public static readonly string FieldStatusIndicatorInheritedTooltip = "Inherited Value\n\nParent: {0} {1}";
        public static readonly string FieldStatusIndicatorLocalTooltip = "Local Value\n\nValue is set directly from the property field.";
        public static readonly string FieldStatusIndicatorFromSelectorTooltip = "Selector Value\n\nSelector: {0}\nSheet: {1}";
        public static readonly string FieldStatusIndicatorVariableTooltip = "Variable Value\n\nVariable: {0}\nSheet: {1}";
        public static readonly string FieldStatusIndicatorUnresolvedVariableTooltip = "Unresolved variable\n\nVariable or Sheet is missing.";
        public static readonly string FieldStatusIndicatorResolvedBindingTooltip = "Resolved Binding";
        public static readonly string FieldStatusIndicatorUnresolvedBindingTooltip = "Unresolved Binding\nEdit Binding for more details.";
        public static readonly string FieldStatusIndicatorUnhandledBindingTooltip = "Unhandled Binding (resolution pending)";
        public static readonly string FieldTooltipDataDefinitionBindingFormatString = "{0}\n\nData Source: {1}\nData Source Path: {2}\nBinding Mode: {3}\nConverter(s) Used: {4}";

        // Class names
        public static readonly string InspectorLocalStyleBindingClassName = "unity-field-affordance-element__style--binding";
        public static readonly string InspectorLocalStyleUnresolvedBindingClassName = "unity-field-affordance-element__style--unresolved-binding";
        public static readonly string InspectorLocalStyleDefaultStatusClassName = "unity-field-affordance-element__style--default";
        public static readonly string InspectorLocalStyleUnresolvedVariableClassName = "unity-field-affordance-element__style--unresolved-variable";
        public static readonly string InspectorLocalStyleVariableClassName = "unity-field-affordance-element__style--variable";
        public static readonly string InspectorLocalStyleInheritedClassName = "unity-field-affordance-element__style--inherited";
        public static readonly string InspectorLocalStyleSelectorClassName = "unity-field-affordance-element__style--uss-selector";
        public static readonly string InspectorLocalStyleSelectorElementClassName = "unity-field-affordance-element__style--selector-element";

        internal const string BindingNotDefinedAttributeString = "Not Defined";
        internal static string NotDefinedString = L10n.Tr(BindingNotDefinedAttributeString);

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string s_UssClassName = "unity-field-affordance-element";
        /// <summary>
        /// Name of the content element.
        /// </summary>
        public static readonly string s_ContentElementName = "content-element";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string s_ContentElementUSSClassName = s_UssClassName + "__content";
        /// <summary>
        /// Name of the icon element.
        /// </summary>
        public static readonly string s_IconElementName = "icon-element";
        /// <summary>
        /// USS class name of icon elements in elements of this type.
        /// </summary>
        public static readonly string s_IconElementUSSClassName = s_UssClassName + "__icon";


        internal event Action<FieldAffordanceData> onContextChanged;

        /// <summary>
        /// Callback used to add menu items to the contextual menu of the associated field before it opens.
        /// </summary>
        public Action<DropdownMenu> populateMenuItems;

        string m_CachedTooltip;
        internal bool isTooltipDirty { get; set; }

        FieldAffordanceData m_Data;

        public FieldAffordanceData fieldAffordanceData
        {
            get => m_Data;
            private set
            {
                if (m_Data != null)
                {
                    m_Data.propertyChanged -= OnFieldAffordanceDataChanged;
                }

                m_Data = value;
                if (m_Data != null)
                    m_Data.propertyChanged += OnFieldAffordanceDataChanged;

                Refresh();
            }
        }

        public FieldAffordanceElement()
        {
            AddToClassList(s_UssClassName);
            AddToClassList(InspectorLocalStyleDefaultStatusClassName);
            var contextMenuManipulator = new ContextualMenuManipulator((evt) =>
            {
                populateMenuItems?.Invoke(evt.menu);

                // Stop immediately to not propagate the event to the row.
                evt.StopImmediatePropagation();
            });

            contextMenuManipulator.acceptClicksIfDisabled = true;

            // Show menu also on left-click
            contextMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            var contentElement = new VisualElement { name = s_ContentElementName };

            contentElement.AddToClassList(s_ContentElementUSSClassName);
            contentElement.AddManipulator(contextMenuManipulator);

            var iconElement = new VisualElement() { name = s_IconElementName, pickingMode = PickingMode.Ignore };

            iconElement.AddToClassList(s_IconElementUSSClassName);

            contentElement.Add(iconElement);
            Add(contentElement);

            fieldAffordanceData = new FieldAffordanceData();

            onContextChanged += OnContextChanged;
        }

        public void SetProperty(SerializedProperty property)
        {
            fieldAffordanceData.type = FieldAffordanceDataType.UXMLAttribute;
            var uxmlFlagsValue = (UxmlAttributeFlags)property.serializedObject.FindProperty(property.propertyPath + "_UxmlAttributeFlags").enumValueIndex;
            var isOverridenInUxml = UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(uxmlFlagsValue);

            fieldAffordanceData.sourceTypeInfo = isOverridenInUxml ? FieldAffordanceSourceInfoType.Inline : FieldAffordanceSourceInfoType.Default;

            this.Unbind();
            this.TrackPropertyValue(property, serializedProperty =>
            {
                uxmlFlagsValue = (UxmlAttributeFlags)property.serializedObject.FindProperty(property.propertyPath + "_UxmlAttributeFlags").enumValueIndex;
                isOverridenInUxml = UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(uxmlFlagsValue);

                m_Data.sourceTypeInfo = isOverridenInUxml ? FieldAffordanceSourceInfoType.Inline : FieldAffordanceSourceInfoType.Default;
                Refresh();
            });

            onContextChanged?.Invoke(fieldAffordanceData);
        }

        void OnFieldAffordanceDataChanged(object sender, BindablePropertyChangedEventArgs e)
        {
            Refresh();
        }

        void OnContextChanged(FieldAffordanceData fieldAffordanceData)
        {
            Refresh();
        }

        void Refresh()
        {
            isTooltipDirty = true;
            RefreshStyling();
        }

        void RefreshStyling()
        {
            EnableInClassList(InspectorLocalStyleBindingClassName, fieldAffordanceData.sourceTypeInfo == FieldAffordanceSourceInfoType.ResolvedBinding);
            EnableInClassList(InspectorLocalStyleUnresolvedBindingClassName, fieldAffordanceData.sourceTypeInfo is FieldAffordanceSourceInfoType.UnhandledBinding or FieldAffordanceSourceInfoType.UnresolvedBinding);
            EnableInClassList(InspectorLocalStyleVariableClassName, fieldAffordanceData.sourceTypeInfo == FieldAffordanceSourceInfoType.USSVariable);
            EnableInClassList(InspectorLocalStyleUnresolvedVariableClassName, fieldAffordanceData.sourceTypeInfo == FieldAffordanceSourceInfoType.USSVariable && fieldAffordanceData.variableSheet == null);
            EnableInClassList(InspectorLocalStyleSelectorClassName, fieldAffordanceData.sourceTypeInfo is FieldAffordanceSourceInfoType.LocalUSSSelector or FieldAffordanceSourceInfoType.MatchingUSSSelector);
            EnableInClassList(InspectorLocalStyleInheritedClassName, fieldAffordanceData.sourceTypeInfo == FieldAffordanceSourceInfoType.Inherited);
            EnableInClassList(InspectorLocalStyleDefaultStatusClassName, fieldAffordanceData.sourceTypeInfo is FieldAffordanceSourceInfoType.Default or FieldAffordanceSourceInfoType.Inline);
        }

        internal string BuildTooltip()
        {
            switch (fieldAffordanceData.sourceTypeInfo)
            {
                case FieldAffordanceSourceInfoType.Inline:
                    return FieldStatusIndicatorInlineTooltip;
                case FieldAffordanceSourceInfoType.ResolvedBinding:
                    if (fieldAffordanceData.binding is DataBinding resolvedBinding)
                    {
                        GetBindingStrings(out var dataSourceString, out var dataSourcePathStr);
                        return string.Format(FieldTooltipDataDefinitionBindingFormatString,
                            FieldStatusIndicatorResolvedBindingTooltip,
                            dataSourceString,
                            dataSourcePathStr,
                            resolvedBinding?.bindingMode.ToString(),
                            GetFormattedConvertersString(resolvedBinding?.uiToSourceConvertersString,
                                resolvedBinding?.sourceToUiConvertersString));
                    }
                    return FieldStatusIndicatorResolvedBindingTooltip;
                case FieldAffordanceSourceInfoType.UnresolvedBinding:
                    if (fieldAffordanceData.binding is DataBinding unresolvedBinding)
                    {
                        GetBindingStrings(out var unresolvedBindingDataSourceString, out var unresolvedBindingDataSourcePathStr);
                        return string.Format(FieldTooltipDataDefinitionBindingFormatString,
                            FieldStatusIndicatorUnresolvedBindingTooltip,
                            unresolvedBindingDataSourceString,
                            unresolvedBindingDataSourcePathStr,
                            unresolvedBinding.bindingMode.ToString(),
                            GetFormattedConvertersString(unresolvedBinding.uiToSourceConvertersString,
                                unresolvedBinding.sourceToUiConvertersString));
                    }
                    return FieldStatusIndicatorUnresolvedBindingTooltip;
                case FieldAffordanceSourceInfoType.UnhandledBinding:
                    if (fieldAffordanceData.binding is DataBinding unhandledBinding)
                    {
                        GetBindingStrings(out var unhandledBindingDataSourceString, out var unhandledBindingDataSourcePathStr);
                        return string.Format(FieldTooltipDataDefinitionBindingFormatString,
                            FieldStatusIndicatorUnhandledBindingTooltip,
                            unhandledBindingDataSourceString,
                            unhandledBindingDataSourcePathStr,
                            unhandledBinding.bindingMode.ToString(),
                            GetFormattedConvertersString(unhandledBinding.uiToSourceConvertersString,
                                unhandledBinding.sourceToUiConvertersString));
                    }
                    return FieldStatusIndicatorUnhandledBindingTooltip;
                case FieldAffordanceSourceInfoType.USSVariable:
                    if (fieldAffordanceData.variableSheet != null)
                        return string.Format(FieldStatusIndicatorVariableTooltip,
                            fieldAffordanceData.inlineValue.ToString(),
                            GetSheetName(fieldAffordanceData.variableSheet));
                    return FieldStatusIndicatorUnresolvedVariableTooltip;
                case FieldAffordanceSourceInfoType.LocalUSSSelector:
                    return FieldStatusIndicatorLocalTooltip;
                case FieldAffordanceSourceInfoType.MatchingUSSSelector:
                    var selectorSheetName = GetSheetName(fieldAffordanceData.selector.sheet);
                    var selector = StyleSheetExporter.Default.ToUssString(fieldAffordanceData.selector.sheet, fieldAffordanceData.selector.complexSelector);
                    return string.Format(FieldStatusIndicatorFromSelectorTooltip,
                        selector,
                        selectorSheetName);
                case FieldAffordanceSourceInfoType.Inherited:
                    return FieldStatusIndicatorInheritedTooltip;
                default:
                    return FieldStatusIndicatorDefaultTooltip;
            }
        }

        internal string GetTooltip()
        {
            if (isTooltipDirty)
            {
                m_CachedTooltip = BuildTooltip();
                isTooltipDirty = false;
            }

            return m_CachedTooltip;
        }

        [EventInterest(typeof(TooltipEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is TooltipEvent tooltipEvent)
            {
                tooltipEvent.rect = worldBound;
                tooltipEvent.tooltip = GetTooltip();
            }
        }

        void GetBindingStrings(out string dataSourceString, out string dataSourcePathStr)
        {
            dataSourceString = NotDefinedString;
            dataSourcePathStr = NotDefinedString;
            if (fieldAffordanceData.binding is DataBinding dataBinding)
            {
                var currentElementDataSource = GetBindingDataSourceOrRelativeHierarchicalDataSource(fieldAffordanceData.targetElement, dataBinding.property);
                dataSourceString = currentElementDataSource?.ToString() ?? NotDefinedString;
                dataSourcePathStr = dataBinding.dataSourcePath.IsEmpty ? NotDefinedString : dataBinding.dataSourcePath.ToString();
            }
        }

        string GetSheetName(StyleSheet styleSheet)
        {
            if (styleSheet == null)
                return null;

            var fullPath = AssetDatabase.GetAssetPath(styleSheet);
            return fullPath == null ? styleSheet.name : Path.GetFileName(fullPath);
        }

        static string GetFormattedConvertersString(string convertersToSource, string convertersToUI)
        {
            if (string.IsNullOrEmpty(convertersToSource) && string.IsNullOrEmpty(convertersToUI))
            {
                return L10n.Tr("None");
            }

            if (string.IsNullOrEmpty(convertersToSource))
            {
                return convertersToUI;
            }

            if (string.IsNullOrEmpty(convertersToUI))
            {
                return convertersToSource;
            }

            return $"{convertersToSource}, {convertersToUI}";
        }

        public static object GetBindingDataSourceOrRelativeHierarchicalDataSource(VisualElement element, BindingId id)
        {
            if (!element.TryGetBinding(id, out var binding))
                return null;

            if (binding is IDataSourceProvider { dataSource: { } } provider)
                return provider.dataSource;

            var context = element.GetHierarchicalDataSourceContext();
            var dataSource = context.dataSource;

            return PropertyContainer.TryGetValue(ref dataSource, context.dataSourcePath, out object relativeDataSource)
                ? relativeDataSource
                : dataSource;
        }
    }
}
