using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorAttributes : IBuilderInspectorSection
    {
        static readonly Dictionary<Type, TypeInfo> s_CachedTypeInfos = new Dictionary<Type, TypeInfo>();

        static readonly UnityEngine.Pool.ObjectPool<BuilderAttributeTypeName> s_TypeNameItemPool =
            new UnityEngine.Pool.ObjectPool<BuilderAttributeTypeName>(
                () => new BuilderAttributeTypeName(),
                null,
                c => c.ClearType());

        static TypeInfo GetTypeInfo(Type type)
        {
            if (!s_CachedTypeInfos.TryGetValue(type, out var typeInfo))
                s_CachedTypeInfos[type] = typeInfo = new TypeInfo(type);
            return typeInfo;
        }

        internal readonly struct TypeInfo
        {
            public readonly Type type;
            public readonly string value;

            public TypeInfo(Type type)
            {
                this.type = type;
                this.value = $"{type.FullName}, {type.Assembly.GetName().Name}";
            }
        }

        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        PersistedFoldout m_AttributesSection;

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_AttributesSection;

        public BuilderInspectorAttributes(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_AttributesSection = m_Inspector.Q<PersistedFoldout>("inspector-attributes-foldout");
        }

        public void Refresh()
        {
            m_AttributesSection.Clear();

            if (currentVisualElement == null)
                return;

            if (m_Selection.selectionType != BuilderSelectionType.Element &&
                m_Selection.selectionType != BuilderSelectionType.ElementInTemplateInstance &&
                m_Selection.selectionType != BuilderSelectionType.ElementInControlInstance)
                return;

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance &&
                string.IsNullOrEmpty(currentVisualElement.name))
            {
                var helpBox = new HelpBox();
                helpBox.AddToClassList(BuilderConstants.InspectorClassHelpBox);
                helpBox.text = BuilderConstants.NoNameElementAttributes;

                m_AttributesSection.Add(helpBox);
            }

            GenerateAttributeFields();

            // Forward focus to the panel header.
            m_AttributesSection
                .Query()
                .Where(e => e.focusable)
                .ForEach((e) => m_Inspector.AddFocusable(e));
        }

        public void Enable()
        {
            m_AttributesSection.contentContainer.SetEnabled(true);
        }

        public void Disable()
        {
            m_AttributesSection.contentContainer.SetEnabled(false);
        }

        void GenerateAttributeFields()
        {
            var attributeList = currentVisualElement.GetAttributeDescriptions();

            foreach (var attribute in attributeList)
            {
                if (attribute == null || attribute.name == null || IsAttributeIgnored(attribute))
                    continue;

                CreateAttributeRow(m_AttributesSection, attribute);
            }
        }

        static bool IsAttributeIgnored(UxmlAttributeDescription attribute)
        {
            // Temporary check until we add an "obsolete" mechanism to uxml attribute description.
            return attribute.name == "show-horizontal-scroller" || attribute.name == "show-vertical-scroller" || attribute.name == "name";
        }

        BuilderStyleRow CreateAttributeRow(VisualElement parent, UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();

            // Generate field label.
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);
            BindableElement fieldElement;
            if (attribute is UxmlStringAttributeDescription)
            {
                // Hard-coded
                if (attribute.name.Equals("value") && currentVisualElement is EnumField enumField)
                {
                    var uiField = new EnumField("Value");
                    if (null != enumField.value)
                        uiField.Init(enumField.value, enumField.includeObsoleteValues);
                    else
                        uiField.SetValueWithoutNotify(null);
                    uiField.RegisterValueChangedCallback(evt =>
                        PostAttributeValueChange(uiField, uiField.value.ToString()));
                    fieldElement = uiField;
                }
                else if (attribute.name.Equals("value") && currentVisualElement is EnumFlagsField enumFlagsField)
                {
                    var uiField = new EnumFlagsField("Value");
                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    if (null != enumFlagsField.value)
                        uiField.Init(enumFlagsField.value, enumFlagsField.includeObsoleteValues);
                    else
                        uiField.SetValueWithoutNotify(null);
                    uiField.RegisterValueChangedCallback(evt =>
                        PostAttributeValueChange(uiField, uiField.value.ToString()));
                    fieldElement = uiField;
                }
                else if (attribute.name.Equals("value") && currentVisualElement is TagField tagField)
                {
                    var uiField = new TagField("Value");
                    uiField.RegisterValueChangedCallback(evt =>
                        PostAttributeValueChange(uiField, uiField.value.ToString()));
                    fieldElement = uiField;
                }
                else
                {
                    var uiField = new TextField(fieldLabel);
                    if (attribute.name.Equals("name") || attribute.name.Equals("view-data-key"))
                        uiField.RegisterValueChangedCallback(e =>
                        {
                            OnValidatedAttributeValueChange(e, BuilderNameUtilities.attributeRegex,
                                BuilderConstants.AttributeValidationSpacialCharacters);
                        });
                    else if (attribute.name.Equals("binding-path"))
                        uiField.RegisterValueChangedCallback(e =>
                        {
                            OnValidatedAttributeValueChange(e, BuilderNameUtilities.bindingPathAttributeRegex,
                                BuilderConstants.BindingPathAttributeValidationSpacialCharacters);
                        });
                    else
                        uiField.RegisterValueChangedCallback(OnAttributeValueChange);

                    if (attribute.name.Equals("text") || attribute.name.Equals("label"))
                    {
                        uiField.multiline = true;
                        uiField.AddToClassList(BuilderConstants.InspectorMultiLineTextFieldClassName);
                    }

                    if (attribute.name.Equals("mask-character"))
                    {
                        uiField.maxLength = 1;
                    }

                    fieldElement = uiField;
                }
            }
            else if (attribute is UxmlFloatAttributeDescription)
            {
                var uiField = new FloatField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlDoubleAttributeDescription)
            {
                var uiField = new DoubleField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlIntAttributeDescription)
            {
                if (attribute.name.Equals("value") && currentVisualElement is LayerField)
                {
                    var uiField = new LayerField("Value");
                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    fieldElement = uiField;
                }
                else if (attribute.name.Equals("value") && currentVisualElement is LayerMaskField)
                {
                    var uiField = new LayerMaskField("Value");
                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    fieldElement = uiField;
                }
                else
                {
                    var uiField = new IntegerField(fieldLabel);
                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    fieldElement = uiField;
                }
            }
            else if (attribute is UxmlLongAttributeDescription)
            {
                var uiField = new LongField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlBoolAttributeDescription)
            {
                var uiField = new Toggle(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attribute is UxmlColorAttributeDescription)
            {
                var uiField = new ColorField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>))
            {
                var assetType = attributeType.GetGenericArguments()[0];
                var uiField = new ObjectField(fieldLabel) { objectType = assetType };
                uiField.RegisterValueChangedCallback(OnAssetAttributeValueChange);
                fieldElement = uiField;
            }
            else if (attributeType.IsGenericType &&
                     !attributeType.GetGenericArguments()[0].IsEnum &&
                     attributeType.GetGenericArguments()[0] is Type)
            {
                var desiredType = attributeType.GetGenericArguments()[0];

                var uiField = new TextField(fieldLabel) { isDelayed = true };

                var completer = new FieldSearchCompleter<TypeInfo>(uiField);
                uiField.RegisterCallback<AttachToPanelEvent, FieldSearchCompleter<TypeInfo>>((evt, c) =>
                {
                    // When possible, the popup should have the same width as the input field, so that the auto-complete
                    // characters will try to match said input field.
                    c.popup.anchoredControl = evt.elementTarget.Q(className: "unity-text-field__input");
                }, completer);
                completer.matcherCallback += (str, info) => info.value.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0;
                completer.itemHeight = 36;
                completer.dataSourceCallback += () =>
                {
                    var desiredTypeInfo = new TypeInfo(desiredType);

                    return TypeCache.GetTypesDerivedFrom(desiredType)
                        .Where(t => !t.IsGenericType)
                        // Remove UIBuilder types from the list
                        .Where(t => t.Assembly != GetType().Assembly)
                        .Select(GetTypeInfo)
                        .Append(desiredTypeInfo);
                };
                completer.getTextFromDataCallback += info => info.value;
                completer.makeItem = () => s_TypeNameItemPool.Get();
                completer.destroyItem = e =>
                {
                    if (e is BuilderAttributeTypeName typeItem)
                        s_TypeNameItemPool.Release(typeItem);
                };
                completer.bindItem = (v, i) =>
                {
                    if (v is BuilderAttributeTypeName l)
                        l.SetType(completer.results[i].type, completer.textField.text);
                };

                uiField.RegisterValueChangedCallback(e => OnValidatedTypeAttributeChange(e, desiredType));

                fieldElement = uiField;
                uiField.RegisterCallback<DetachFromPanelEvent, FieldSearchCompleter<TypeInfo>>((evt, c) =>
                {
                    c.popup.RemoveFromHierarchy();
                }, completer);
                uiField.userData = completer;
            }
            else if (attributeType.IsGenericType && attributeType.GetGenericArguments()[0].IsEnum)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

                if (defaultEnumValue.GetType().GetCustomAttribute<FlagsAttribute>() == null)
                {
                    // Create and initialize the EnumField.
                    var uiField = new EnumField(fieldLabel);
                    uiField.Init(defaultEnumValue);

                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    fieldElement = uiField;
                }
                else
                {
                    // Create and initialize the EnumFlagsField.
                    var uiField = new EnumFlagsField(fieldLabel);
                    uiField.Init(defaultEnumValue);

                    uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                    fieldElement = uiField;
                }
            }
            else
            {
                var uiField = new TextField(fieldLabel);
                uiField.RegisterValueChangedCallback(OnAttributeValueChange);
                fieldElement = uiField;
            }

            // Create row.
            var styleRow = new BuilderStyleRow();
            styleRow.Add(fieldElement);

            // Link the field.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName, attribute);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            // Set initial value.
            RefreshAttributeField(fieldElement);

            // Setup field binding path.
            fieldElement.bindingPath = attribute.name;
            fieldElement.tooltip = attribute.name;

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator((evt) => BuildAttributeFieldContextualMenu(evt.menu, fieldElement)));

            if (fieldElement.GetFieldStatusIndicator() != null)
            {
                fieldElement.GetFieldStatusIndicator().populateMenuItems =
                    (menu) => BuildAttributeFieldContextualMenu(menu, fieldElement);
            }

            m_Inspector.UpdateFieldStatus(fieldElement, null);
            return styleRow;
        }

        string GetRemapAttributeNameToCSProperty(string attributeName)
        {
            if (currentVisualElement is ObjectField && attributeName == "type")
                return "objectType";
            else if (attributeName == "readonly")
                return "isReadOnly";

            var camel = BuilderNameUtilities.ConvertDashToCamel(attributeName);
            return camel;
        }

        object GetCustomValueAbstract(string attributeName)
        {
            if (currentVisualElement is ScrollView scrollView)
            {
                if (attributeName == "show-horizontal-scroller")
                {
                    return scrollView.horizontalScrollerVisibility != ScrollerVisibility.Hidden;
                }
                else if (attributeName == "show-vertical-scroller")
                {
                    return scrollView.verticalScrollerVisibility != ScrollerVisibility.Hidden;
                }
                else if (attributeName == "touch-scroll-type")
                {
                    return scrollView.touchScrollBehavior;
                }
            }
            else if (currentVisualElement is ListView listView)
            {
                if (attributeName == "horizontal-scrolling")
                    return listView.horizontalScrollingEnabled;
            }

            return null;
        }

        void RefreshAttributeField(BindableElement fieldElement)
        {
            var styleRow =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as
                    UxmlAttributeDescription;

            var veType = currentVisualElement.GetType();
            var csPropertyName = GetRemapAttributeNameToCSProperty(attribute.name);
            var fieldInfo = veType.GetProperty(csPropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            object veValueAbstract = null;
            if (fieldInfo == null)
            {
                veValueAbstract = GetCustomValueAbstract(attribute.name);
            }
            else
            {
                veValueAbstract = fieldInfo.GetValue(currentVisualElement, null);
            }

            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();
            var isAssetAttribute = attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>);

            if (veValueAbstract == null)
            {
                if (currentVisualElement is EnumField defaultEnumField &&
                    attribute.name == "value")
                {
                    if (defaultEnumField.type == null)
                    {
                        fieldElement.SetEnabled(false);
                    }
                    else
                    {
                        ((EnumField)fieldElement).PopulateDataFromType(defaultEnumField.type);
                        fieldElement.SetEnabled(true);
                    }
                }
                else if (currentVisualElement is EnumFlagsField defaultEnumFlagsField &&
                    attribute.name == "value")
                {
                    if (defaultEnumFlagsField.type == null)
                    {
                        fieldElement.SetEnabled(false);
                    }
                    else
                    {
                        ((EnumFlagsField)fieldElement).PopulateDataFromType(defaultEnumFlagsField.type);
                        fieldElement.SetEnabled(true);
                    }
                }
                else if (isAssetAttribute && fieldElement is ObjectField objectField)
                {
                    if (vea != null && attribute.TryGetValueFromBagAsString(vea, CreationContext.Default, out var value))
                    {
                        // Asset wasn't loaded correctly, most likely due to an invalid path. Show the missing reference.
                        var asset = m_Inspector.visualTreeAsset.GetAsset(value, objectField.objectType);
                        objectField.SetValueWithoutNotify(asset);

                        styleRow.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, true);
                        m_Inspector.UpdateFieldStatus(fieldElement, null);
                    }
                }

                return;
            }

            if (attribute is UxmlStringAttributeDescription &&
                attribute.name == "value" &&
                currentVisualElement is EnumField enumField
                && fieldElement is EnumField inputEnumField)
            {
                var hasValue = enumField.value != null;
                if (hasValue)
                    inputEnumField.Init(enumField.value, enumField.includeObsoleteValues);
                else
                    inputEnumField.SetValueWithoutNotify(null);
                inputEnumField.SetEnabled(hasValue);
            }
            else if (attribute is UxmlStringAttributeDescription &&
                     attribute.name == "value" &&
                     currentVisualElement is TagField tagField
                     && fieldElement is TagField inputTagField)
            {
                inputTagField.SetValueWithoutNotify(tagField.value);
            }
            else if (attribute is UxmlIntAttributeDescription &&
                     attribute.name == "value" &&
                     currentVisualElement is LayerField layerField
                     && fieldElement is LayerField inputLayerField)
            {
                inputLayerField.SetValueWithoutNotify(layerField.value);
            }
            else if (attribute is UxmlIntAttributeDescription &&
                     attribute.name == "value" &&
                     currentVisualElement is LayerMaskField layerMaskField
                     && fieldElement is LayerMaskField inputLayerMaskField)
            {
                inputLayerMaskField.SetValueWithoutNotify(layerMaskField.value);
            }
            else if (attribute is UxmlStringAttributeDescription &&
                attribute.name == "value" &&
                currentVisualElement is EnumFlagsField enumFlagsField
                && fieldElement is EnumFlagsField inputEnumFlagsField)
            {
                var hasValue = enumFlagsField.value != null;
                if (hasValue)
                    inputEnumFlagsField.Init(enumFlagsField.value, enumFlagsField.includeObsoleteValues);
                else
                    inputEnumFlagsField.SetValueWithoutNotify(null);
                inputEnumFlagsField.SetEnabled(hasValue);
            }
            else if (attribute is UxmlStringAttributeDescription && fieldElement is TextField textField)
            {
                textField.SetValueWithoutNotify(GetAttributeStringValue(veValueAbstract));
            }
            else if (attribute is UxmlFloatAttributeDescription && fieldElement is FloatField floatField)
            {
                floatField.SetValueWithoutNotify((float) veValueAbstract);
            }
            else if (attribute is UxmlDoubleAttributeDescription && fieldElement is DoubleField doubleField)
            {
                doubleField.SetValueWithoutNotify((double) veValueAbstract);
            }
            else if (attribute is UxmlIntAttributeDescription && fieldElement is IntegerField integerField)
            {
                if (veValueAbstract is int)
                    integerField.SetValueWithoutNotify((int) veValueAbstract);
                else if (veValueAbstract is float)
                    integerField.SetValueWithoutNotify(Convert.ToInt32(veValueAbstract));
            }
            else if (attribute is UxmlLongAttributeDescription && fieldElement is LongField longField)
            {
                longField.SetValueWithoutNotify((long) veValueAbstract);
            }
            else if (attribute is UxmlBoolAttributeDescription && fieldElement is Toggle toggle)
            {
                toggle.SetValueWithoutNotify((bool) veValueAbstract);
            }
            else if (attribute is UxmlColorAttributeDescription && fieldElement is ColorField colorField)
            {
                colorField.SetValueWithoutNotify((Color) veValueAbstract);
            }
            else if (isAssetAttribute && fieldElement is ObjectField objectField)
            {
                objectField.SetValueWithoutNotify((Object)veValueAbstract);
            }
            else if (attributeType.IsGenericType &&
                     !attributeType.GetGenericArguments()[0].IsEnum &&
                     attributeType.GetGenericArguments()[0] is Type &&
                     fieldElement is TextField typeTextField &&
                     veValueAbstract is Type veTypeValue)
            {
                var fullTypeName = veTypeValue.AssemblyQualifiedName;
                var fullTypeNameSplit = fullTypeName.Split(',');
                typeTextField.SetValueWithoutNotify($"{fullTypeNameSplit[0]},{fullTypeNameSplit[1]}");
            }
            else if (attributeType.IsGenericType &&
                     attributeType.GetGenericArguments()[0].IsEnum &&
                     fieldElement is BaseField<Enum> baseEnumField)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

                string enumAttributeValueStr;
                if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
                {
                    // Special case for template children and usageHints that is not set in the visual element when set in the builder
                    var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);
                    var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                    var fieldAttributeOverride = parentTemplateAsset.attributeOverrides.FirstOrDefault(x =>
                        x.m_AttributeName == attribute.name && x.m_ElementName == currentVisualElement.name);

                    if (fieldAttributeOverride.m_ElementName == currentVisualElement.name)
                    {
                        enumAttributeValueStr = fieldAttributeOverride.m_Value;
                    }
                    else
                    {
                        enumAttributeValueStr = veValueAbstract.ToString();
                    }
                }
                else
                {
                    enumAttributeValueStr = vea?.GetAttributeValue(attribute.name);
                }

                // Set the value from the UXML attribute.
                if (!string.IsNullOrEmpty(enumAttributeValueStr))
                {
                    try
                    {
                        var parsedValue = Enum.Parse(defaultEnumValue.GetType(), enumAttributeValueStr, true) as Enum;
                        baseEnumField.SetValueWithoutNotify(parsedValue);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogException(exception);
                        // use default if anything went wrong
                        baseEnumField.SetValueWithoutNotify(defaultEnumValue);
                    }
                    catch (OverflowException exception)
                    {
                        Debug.LogException(exception);
                        // use default if anything went wrong
                        baseEnumField.SetValueWithoutNotify(defaultEnumValue);
                    }
                }
            }
            else if (fieldElement is TextField defaultTextField)
            {
                defaultTextField.SetValueWithoutNotify(veValueAbstract.ToString());
            }

            styleRow.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, IsAttributeOverriden(attribute));
            m_Inspector.UpdateFieldStatus(fieldElement, null);
        }

        string GetAttributeStringValue(object attributeValue)
        {
            string value;
            if (attributeValue is Enum @enum)
                value = @enum.ToString();
            else if (attributeValue is IEnumerable<string> list)
            {
                value = string.Join(",", list.ToArray());
            }
            else
            {
                value = attributeValue.ToString();
            }

            return value;
        }

        bool IsAttributeOverriden(UxmlAttributeDescription attribute)
        {
            return IsAttributeOverriden(currentVisualElement, attribute);
        }

        public static bool IsAttributeOverriden(VisualElement currentVisualElement, UxmlAttributeDescription attribute)
        {
            var vea = currentVisualElement.GetVisualElementAsset();
            if (vea != null && attribute.name == "picking-mode")
            {
                var veaAttributeValue = vea.GetAttributeValue(attribute.name);
                if (veaAttributeValue != null &&
                    veaAttributeValue.ToLower() != attribute.defaultValueAsString.ToLower())
                    return true;
            }
            else if (attribute.name == "name")
            {
                if (!string.IsNullOrEmpty(currentVisualElement.name))
                    return true;
            }
            else if (vea != null && vea.HasAttribute(attribute.name))
            {
                return true;
            }
            else if (BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(currentVisualElement, attribute.name))
            {
                return true;
            }

            return false;
        }

        void ResetAttributeFieldToDefault(BindableElement fieldElement)
        {
            var styleRow =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as
                    UxmlAttributeDescription;

            var attributeType = attribute.GetType();

            if (attribute is UxmlStringAttributeDescription stringAttribute && fieldElement is TextField textField)
            {
                textField.SetValueWithoutNotify(stringAttribute.defaultValue);
            }
            else if (attribute is UxmlStringAttributeDescription && fieldElement is EnumField enumField &&
                     currentVisualElement is EnumField)
            {
                if (null == enumField.type)
                    enumField.SetValueWithoutNotify(null);
                else
                    enumField.SetValueWithoutNotify((Enum)Enum.ToObject(enumField.type, 0));
            }
            else if (attribute is UxmlStringAttributeDescription && fieldElement is EnumFlagsField enumFlagsField &&
                     currentVisualElement is EnumFlagsField)
            {
                if (null == enumFlagsField.type)
                    enumFlagsField.SetValueWithoutNotify(null);
                else
                    enumFlagsField.SetValueWithoutNotify((Enum)Enum.ToObject(enumFlagsField.type, 0));
            }
            else if (attribute is UxmlFloatAttributeDescription floatAttribute && fieldElement is FloatField floatField)
            {
                floatField.SetValueWithoutNotify(floatAttribute.defaultValue);
            }
            else if (attribute is UxmlDoubleAttributeDescription doubleAttribute && fieldElement is DoubleField doubleField)
            {
                doubleField.SetValueWithoutNotify(doubleAttribute.defaultValue);
            }
            else if (attribute is UxmlIntAttributeDescription intAttribute && fieldElement is IntegerField intField)
            {
                intField.SetValueWithoutNotify(intAttribute.defaultValue);
            }
            else if (attribute is UxmlLongAttributeDescription longAttribute && fieldElement is LongField longField)
            {
                longField.SetValueWithoutNotify(longAttribute.defaultValue);
            }
            else if (attribute is UxmlBoolAttributeDescription boolAttribute && fieldElement is Toggle toggle)
            {
                toggle.SetValueWithoutNotify(boolAttribute.defaultValue);
            }
            else if (attribute is UxmlColorAttributeDescription colorAttribute && fieldElement is ColorField colorField)
            {
                colorField.SetValueWithoutNotify(colorAttribute.defaultValue);
            }
            else if (attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>) &&
                     fieldElement is ObjectField objectField)
            {
                objectField.SetValueWithoutNotify(default);
            }
            else if (attributeType.IsGenericType &&
                     !attributeType.GetGenericArguments()[0].IsEnum &&
                     attributeType.GetGenericArguments()[0] is Type &&
                     fieldElement is TextField typeTextField)
            {
                var a = attribute as TypedUxmlAttributeDescription<Type>;
                if (a.defaultValue == null)
                    typeTextField.SetValueWithoutNotify(string.Empty);
                else
                    typeTextField.SetValueWithoutNotify(a.defaultValue.ToString());
            }
            else if (attributeType.IsGenericType &&
                     attributeType.GetGenericArguments()[0].IsEnum &&
                     fieldElement is BaseField<Enum> baseEnumField)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

                baseEnumField.SetValueWithoutNotify(defaultEnumValue);
            }
            else if (fieldElement is TextField defaultTextField)
            {
                defaultTextField.SetValueWithoutNotify(string.Empty);
            }

            // Clear override.
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            var styleFields = styleRow.Query<BindableElement>().ToList();
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            }
        }

        void BuildAttributeFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildAttributeFieldContextualMenu(evt.menu, evt.elementTarget);
        }

        void BuildAttributeFieldContextualMenu(DropdownMenu menu, VisualElement fieldElement)
        {
            // if the context menu is already populated by the field (text field) then ignore
            if (menu.MenuItems() != null & menu.MenuItems().Count > 0)
                return;

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetAttributeProperty,
                action =>
                {
                    var fieldElement = action.userData as BindableElement;
                    if (fieldElement == null)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = fieldElement.bindingPath;
                    var vea = currentVisualElement.GetVisualElementAsset();
                    var isAttributeOverrideAttribute =
                        m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance
                        && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(currentVisualElement,
                            attributeName);

                    return (vea != null && vea.HasAttribute(attributeName)) || isAttributeOverrideAttribute
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled;
                },
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    var attributeList = currentVisualElement.GetAttributeDescriptions();
                    foreach (var attribute in attributeList)
                    {
                        if (attribute?.name == null)
                            continue;

                        if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance
                            && attribute.name == "name")
                        {
                            continue;
                        }

                        if (IsAttributeOverriden(attribute))
                            return DropdownMenuAction.Status.Normal;
                    }

                    return DropdownMenuAction.Status.Disabled;
                },
                fieldElement);
        }

        internal void UnsetAllAttributes()
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset,
                BuilderConstants.ChangeAttributeValueUndoMessage);

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
            {
                var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);
                var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                var attributeOverrides =
                    new List<TemplateAsset.AttributeOverride>(parentTemplateAsset.attributeOverrides);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (attributeOverride.m_ElementName == currentVisualElement.name)
                    {
                        parentTemplateAsset.RemoveAttributeOverride(attributeOverride.m_ElementName,
                            attributeOverride.m_AttributeName);
                    }
                }

                var builder = Builder.ActiveWindow;
                var hierarchyView = builder.hierarchy.elementHierarchyView;
                var selectionId = hierarchyView.GetSelectedItemId();

                builder.OnEnableAfterAllSerialization();

                hierarchyView.SelectItemById(selectionId);
            }
            else
            {
                var attributeList = currentVisualElement.GetAttributeDescriptions();
                var vea = currentVisualElement.GetVisualElementAsset();

                foreach (var attribute in attributeList)
                {
                    if (attribute?.name == null)
                        continue;

                    // Unset value in asset.
                    vea.RemoveAttribute(attribute.name);
                }

                var fields = m_AttributesSection.Query<BindableElement>()
                    .Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
                foreach (var fieldElement in fields)
                {
                    // Reset UI value.
                    ResetAttributeFieldToDefault(fieldElement);
                    m_Inspector.UpdateFieldStatus(fieldElement, null);
                }

                // Call Init();
                m_Inspector.CallInitOnElement();

                // Notify of changes.
                m_Selection.NotifyOfHierarchyChange(m_Inspector);
            }
        }

        void UnsetAttributeProperty(DropdownMenuAction action)
        {
            var fieldElement = action.userData as BindableElement;
            UnsetAttributeProperty(fieldElement);

            // When unsetting the type value for an enum field, we also need to clear the value field as well.
            if (currentVisualElement is EnumField && fieldElement.bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = root.Query<EnumField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
            if (currentVisualElement is EnumFlagsField && fieldElement.bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = root.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
        }

        public void UnsetAttributeProperty(BindableElement fieldElement)
        {
            var attributeName = fieldElement.bindingPath;
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset,
                BuilderConstants.ChangeAttributeValueUndoMessage);

            // Unset value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
            {
                var templateContainer = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);
                var templateAsset = templateContainer.GetVisualElementAsset() as TemplateAsset;

                if (templateAsset != null)
                {
                    var builder = Builder.ActiveWindow;
                    var hierarchyView = builder.hierarchy.elementHierarchyView;
                    var selectionId = hierarchyView.GetSelectedItemId();

                    templateAsset.RemoveAttributeOverride(currentVisualElement.name, attributeName);

                    builder.OnEnableAfterAllSerialization();

                    hierarchyView.SelectItemById(selectionId);
                }
            }
            else
            {
                vea.RemoveAttribute(attributeName);

                // Reset UI value.
                ResetAttributeFieldToDefault(fieldElement);

                // Call Init();
                m_Inspector.CallInitOnElement();

                // Notify of changes.
                m_Selection.NotifyOfHierarchyChange(m_Inspector);

                m_Inspector.UpdateFieldStatus(fieldElement, null);
                Refresh();
            }
        }

        void OnAttributeValueChange(ChangeEvent<string> evt)
        {
            var field = evt.target as TextField;
            PostAttributeValueChange(field, evt.newValue);
        }

        void OnValidatedTypeAttributeChange(ChangeEvent<string> evt, Type desiredType)
        {
            var field = evt.target as TextField;
            var typeName = evt.newValue;
            var fullTypeName = typeName;

            Type type = null;
            if (!string.IsNullOrEmpty(typeName))
            {
                type = Type.GetType(fullTypeName, false);

                // Try some auto-fixes.
                if (type == null)
                {
                    fullTypeName = typeName + ", UnityEngine.CoreModule";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null)
                {
                    fullTypeName = typeName + ", UnityEditor";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null && typeName.Contains("."))
                {
                    var split = typeName.Split('.');
                    fullTypeName = typeName + $", {split[0]}.{split[1]}Module";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null)
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.TypeAttributeInvalidTypeMessage, field.label));
                    evt.StopPropagation();
                    return;
                }
                else if (!desiredType.IsAssignableFrom(type))
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.TypeAttributeMustDeriveFromMessage, field.label,
                        desiredType.FullName));
                    evt.StopPropagation();
                    return;
                }
            }

            if (currentVisualElement is EnumField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = root.Query<EnumField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
            else if (currentVisualElement is EnumFlagsField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = root.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }

            field.value = fullTypeName;
            PostAttributeValueChange(field, fullTypeName);

            Refresh();
        }

        internal void OnValidatedAttributeValueChange(ChangeEvent<string> evt, Regex regex, string message)
        {
            var field = evt.target as TextField;
            if (!string.IsNullOrEmpty(evt.newValue) && !regex.IsMatch(evt.newValue))
            {
                Builder.ShowWarning(string.Format(message, field.label));
                field.SetValueWithoutNotify(evt.previousValue);
                evt.StopPropagation();
                return;
            }

            OnAttributeValueChange(evt);
        }

        void OnAttributeValueChange(ChangeEvent<float> evt)
        {
            var field = evt.target as FloatField;
            PostAttributeValueChange(field, evt.newValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
        }

        void OnAttributeValueChange(ChangeEvent<double> evt)
        {
            var field = evt.target as DoubleField;
            PostAttributeValueChange(field, evt.newValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
        }

        void OnAttributeValueChange(ChangeEvent<int> evt)
        {
            var field = evt.target as BaseField<int>;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<long> evt)
        {
            var field = evt.target as LongField;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void OnAttributeValueChange(ChangeEvent<bool> evt)
        {
            var field = evt.target as Toggle;
            PostAttributeValueChange(field, evt.newValue.ToString().ToLower());
        }

        void OnAttributeValueChange(ChangeEvent<Color> evt)
        {
            var field = evt.target as ColorField;
            PostAttributeValueChange(field, "#" + ColorUtility.ToHtmlStringRGBA(evt.newValue));
        }

        void OnAssetAttributeValueChange(ChangeEvent<Object> evt)
        {
            var field = evt.target as ObjectField;

            var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessageUxml);

                // Revert the change.
                field.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            var vta = m_Inspector.visualTreeAsset;
            var uri = URIHelpers.MakeAssetUri(evt.newValue);

            if (!string.IsNullOrEmpty(uri) && !vta.AssetEntryExists(uri, field.objectType))
            {
                vta.RegisterAssetEntry(uri, field.objectType, evt.newValue);
            }

            PostAttributeValueChange(field, uri);
        }

        void OnAttributeValueChange(ChangeEvent<Enum> evt)
        {
            var field = evt.target as BaseField<Enum>;
            PostAttributeValueChange(field, evt.newValue.ToString());
        }

        void PostAttributeValueChange(BindableElement field, string value)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset,
                BuilderConstants.ChangeAttributeValueUndoMessage);

            // Set value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();
            var vta = currentVisualElement.GetVisualTreeAsset();

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
            {
                TemplateContainer templateContainerParent =
                    BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);

                if (templateContainerParent != null)
                {
                    var templateAsset = templateContainerParent.GetVisualElementAsset() as TemplateAsset;
                    var currentVisualElementName = currentVisualElement.name;

                    if (!string.IsNullOrEmpty(currentVisualElementName))
                    {
                        templateAsset.SetAttributeOverride(currentVisualElementName, field.bindingPath, value);

                        var document = Builder.ActiveWindow.document;
                        var rootElement = Builder.ActiveWindow.viewport.documentRootElement;

                        var elementsToChange = templateContainerParent.Query<VisualElement>(currentVisualElementName);
                        elementsToChange.ForEach(x =>
                        {
                            var templateVea =
                                x.GetProperty(VisualTreeAsset.LinkedVEAInTemplatePropertyName) as VisualElementAsset;
                            var attributeOverrides =
                                BuilderAssetUtilities.GetAccumulatedAttributeOverrides(currentVisualElement);
                            m_Inspector.CallInitOnTemplateChild(x, templateVea, attributeOverrides);
                        });
                    }
                }
            }
            else
            {
                vea.SetAttribute(field.bindingPath, value);

                // Call Init();
                m_Inspector.CallInitOnElement();
            }

            // Mark field as overridden.
            var styleRow = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;

            if (styleRow != null)
            {
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    if (field.bindingPath == styleField.bindingPath)
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                             field.bindingPath != styleField.bindingPath &&
                             !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    }
                }
            }

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);

            if (styleRow != null)
            {
                m_Inspector.UpdateFieldStatus(field, null);
            }
        }
    }
}
