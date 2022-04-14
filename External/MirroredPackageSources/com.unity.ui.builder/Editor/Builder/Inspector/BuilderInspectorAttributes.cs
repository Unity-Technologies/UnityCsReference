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

            m_AttributesSection.text = currentVisualElement.typeName;

            if (m_Selection.selectionType != BuilderSelectionType.Element &&
                m_Selection.selectionType != BuilderSelectionType.ElementInTemplateInstance &&
                m_Selection.selectionType != BuilderSelectionType.ElementInControlInstance)
                return;

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance && string.IsNullOrEmpty(currentVisualElement.name))
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

        public void DisableNameRow()
        {
            var nameField = m_AttributesSection.Query<BindableElement>().Where(e => e.bindingPath == "name").First();
            var styleRow = nameField.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            styleRow.SetEnabled(false);
        }

        void GenerateAttributeFields()
        {
            var attributeList = currentVisualElement.GetAttributeDescriptions();

            foreach (var attribute in attributeList)
            {
                if (attribute == null || attribute.name == null || IsAttributeIgnored(attribute))
                    continue;

                var styleRow = CreateAttributeRow(attribute);
                m_AttributesSection.Add(styleRow);
            }
        }

        static bool IsAttributeIgnored(UxmlAttributeDescription attribute)
        {
            // Temporary check until we add an "obsolete" mechanism to uxml attribute description.
            return attribute.name == "show-horizontal-scroller" || attribute.name == "show-vertical-scroller";
        }

        BuilderStyleRow CreateAttributeRow(UxmlAttributeDescription attribute)
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
            else if (attributeType.IsGenericType && 
                attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>))
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

                var uiField = new TextField(fieldLabel) {isDelayed = true};

                var completer = new FieldSearchCompleter<TypeInfo>(uiField);
                uiField.RegisterCallback<AttachToPanelEvent, FieldSearchCompleter<TypeInfo>>((evt, c) =>
                {
                    // When possible, the popup should have the same width as the input field, so that the auto-complete
                    // characters will try to match said input field.
                    c.popup.anchoredControl = ((VisualElement)evt.target).Q(className: "unity-text-field__input");
                }, completer);
                completer.matcherCallback += (str, info) => info.value.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0;
                completer.itemHeight = 36;
                completer.dataSourceCallback += () =>
                {
                    return TypeCache.GetTypesDerivedFrom(desiredType)
                        .Where(t => !t.IsGenericType)
                        // Remove UIBuilder types from the list
                        .Where(t => t.Assembly != GetType().Assembly)
                        .Select(GetTypeInfo);
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

            // Set initial value.
            RefreshAttributeField(fieldElement);

            // Setup field binding path.
            fieldElement.bindingPath = attribute.name;
            fieldElement.tooltip = attribute.name;

            // Context menu.
            fieldElement.AddManipulator(new ContextualMenuManipulator(BuildAttributeFieldContextualMenu));

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
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute = fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlAttributeDescription;

            var veType = currentVisualElement.GetType();
            var csPropertyName = GetRemapAttributeNameToCSProperty(attribute.name);
            var fieldInfo = veType.GetProperty(csPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            object veValueAbstract = null;
            if (fieldInfo == null)
            {
                veValueAbstract = GetCustomValueAbstract(attribute.name);
            }
            else
            {
                veValueAbstract = fieldInfo.GetValue(currentVisualElement, null);
            }

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

                return;
            }

            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();

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
            else if (attribute is UxmlStringAttributeDescription && fieldElement is TextField)
            {
                (fieldElement as TextField).SetValueWithoutNotify(GetAttributeStringValue(veValueAbstract));
            }
            else if (attribute is UxmlFloatAttributeDescription && fieldElement is FloatField)
            {
                (fieldElement as FloatField).SetValueWithoutNotify((float)veValueAbstract);
            }
            else if (attribute is UxmlDoubleAttributeDescription && fieldElement is DoubleField)
            {
                (fieldElement as DoubleField).SetValueWithoutNotify((double)veValueAbstract);
            }
            else if (attribute is UxmlIntAttributeDescription && fieldElement is IntegerField)
            {
                if (veValueAbstract is int)
                    (fieldElement as IntegerField).SetValueWithoutNotify((int)veValueAbstract);
                else if (veValueAbstract is float)
                    (fieldElement as IntegerField).SetValueWithoutNotify(Convert.ToInt32(veValueAbstract));
            }
            else if (attribute is UxmlLongAttributeDescription && fieldElement is LongField)
            {
                (fieldElement as LongField).SetValueWithoutNotify((long)veValueAbstract);
            }
            else if (attribute is UxmlBoolAttributeDescription && fieldElement is Toggle)
            {
                (fieldElement as Toggle).SetValueWithoutNotify((bool)veValueAbstract);
            }
            else if (attribute is UxmlColorAttributeDescription && fieldElement is ColorField)
            {
                (fieldElement as ColorField).SetValueWithoutNotify((Color)veValueAbstract);
            }
            else if (attributeType.IsGenericType && 
                attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>) && 
                fieldElement is ObjectField)
            {
                (fieldElement as ObjectField).SetValueWithoutNotify((UnityEngine.Object)veValueAbstract);
            }
            else if (attributeType.IsGenericType &&
                     !attributeType.GetGenericArguments()[0].IsEnum &&
                     attributeType.GetGenericArguments()[0] is Type &&
                     fieldElement is TextField textField &&
                     veValueAbstract is Type veTypeValue)
            {
                var fullTypeName = veTypeValue.AssemblyQualifiedName;
                var fullTypeNameSplit = fullTypeName.Split(',');
                textField.SetValueWithoutNotify($"{fullTypeNameSplit[0]},{fullTypeNameSplit[1]}");
            }
            else if (attributeType.IsGenericType &&
                     attributeType.GetGenericArguments()[0].IsEnum &&
                     fieldElement is BaseField<Enum>)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

                var uiField = fieldElement as BaseField<Enum>;

                string enumAttributeValueStr;
                if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
                {
                    // Special case for template children and usageHints that is not set in the visual element when set in the builder
                    var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);
                    var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                    var fieldAttributeOverride = parentTemplateAsset.attributeOverrides.FirstOrDefault(x => x.m_AttributeName == attribute.name && x.m_ElementName == currentVisualElement.name);

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
                        uiField.SetValueWithoutNotify(parsedValue);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogException(exception);
                        // use default if anything went wrong
                        uiField.SetValueWithoutNotify(defaultEnumValue);
                    }
                    catch (OverflowException exception)
                    {
                        Debug.LogException(exception);
                        // use default if anything went wrong
                        uiField.SetValueWithoutNotify(defaultEnumValue);
                    }
                }
            }
            else if (fieldElement is TextField)
            {
                (fieldElement as TextField).SetValueWithoutNotify(veValueAbstract.ToString());
            }

            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            if (IsAttributeOverriden(attribute))
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
        }

        string GetAttributeStringValue(object attributeValue)
        {
            string value;
            if (attributeValue is Enum @enum)
                value = @enum.ToString();
            else if (attributeValue is List<string> list)
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
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute = fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as UxmlAttributeDescription;

            var attributeType = attribute.GetType();
            var vea = currentVisualElement.GetVisualElementAsset();

            if (attribute is UxmlStringAttributeDescription && fieldElement is TextField)
            {
                var a = attribute as UxmlStringAttributeDescription;
                var f = fieldElement as TextField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlStringAttributeDescription && fieldElement is EnumField &&
                     currentVisualElement is EnumField)
            {
                var a = attribute as UxmlStringAttributeDescription;
                var f = fieldElement as EnumField;
                if (null == f.type)
                    f.SetValueWithoutNotify(null);
                else
                    f.SetValueWithoutNotify((Enum)Enum.ToObject(f.type, 0));
            }
            else if (attribute is UxmlStringAttributeDescription && fieldElement is EnumFlagsField &&
                     currentVisualElement is EnumFlagsField)
            {
                var f = fieldElement as EnumFlagsField;
                if (null == f.type)
                    f.SetValueWithoutNotify(null);
                else
                    f.SetValueWithoutNotify((Enum)Enum.ToObject(f.type, 0));
            }
            else if (attribute is UxmlFloatAttributeDescription && fieldElement is FloatField)
            {
                var a = attribute as UxmlFloatAttributeDescription;
                var f = fieldElement as FloatField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlDoubleAttributeDescription && fieldElement is DoubleField)
            {
                var a = attribute as UxmlDoubleAttributeDescription;
                var f = fieldElement as DoubleField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlIntAttributeDescription && fieldElement is IntegerField)
            {
                var a = attribute as UxmlIntAttributeDescription;
                var f = fieldElement as IntegerField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlLongAttributeDescription && fieldElement is LongField)
            {
                var a = attribute as UxmlLongAttributeDescription;
                var f = fieldElement as LongField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlBoolAttributeDescription && fieldElement is Toggle)
            {
                var a = attribute as UxmlBoolAttributeDescription;
                var f = fieldElement as Toggle;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attribute is UxmlColorAttributeDescription && fieldElement is ColorField)
            {
                var a = attribute as UxmlColorAttributeDescription;
                var f = fieldElement as ColorField;
                f.SetValueWithoutNotify(a.defaultValue);
            }
            else if (attributeType.IsGenericType &&
                     !attributeType.GetGenericArguments()[0].IsEnum &&
                     attributeType.GetGenericArguments()[0] is Type &&
                     fieldElement is TextField)
            {
                var a = attribute as TypedUxmlAttributeDescription<Type>;
                var f = fieldElement as TextField;
                if (a.defaultValue == null)
                    f.SetValueWithoutNotify(string.Empty);
                else
                    f.SetValueWithoutNotify(a.defaultValue.ToString());
            }
            else if (attributeType.IsGenericType &&
                     attributeType.GetGenericArguments()[0].IsEnum &&
                     fieldElement is BaseField<Enum>)
            {
                var propInfo = attributeType.GetProperty("defaultValue");
                var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

                var uiField = fieldElement as BaseField<Enum>;
                uiField.SetValueWithoutNotify(defaultEnumValue);
            }
            else if (fieldElement is TextField)
            {
                (fieldElement as TextField).SetValueWithoutNotify(string.Empty);
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
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetAttributeProperty,
                action =>
                {
                    var fieldElement = action.userData as BindableElement;
                    if (fieldElement == null)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = fieldElement.bindingPath;
                    var vea = currentVisualElement.GetVisualElementAsset();
                    var isAttributeOverrideAttribute = m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance
                        && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(currentVisualElement, attributeName);

                    return (vea != null && vea.HasAttribute(attributeName)) || isAttributeOverrideAttribute
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;
                },
                evt.target);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                UnsetAllAttributes,
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
                evt.target);
        }

        void UnsetAllAttributes(DropdownMenuAction action)
        {
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
            {
                var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);
                var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                var attributeOverrides = new List<TemplateAsset.AttributeOverride>(parentTemplateAsset.attributeOverrides);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (attributeOverride.m_ElementName == currentVisualElement.name)
                    {
                        parentTemplateAsset.RemoveAttributeOverride(attributeOverride.m_ElementName, attributeOverride.m_AttributeName);
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

                var fields = m_AttributesSection.Query<BindableElement>().Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
                foreach (var fieldElement in fields)
                {
                    // Reset UI value.
                    ResetAttributeFieldToDefault(fieldElement);
                }

                // Call Init();
                CallInitOnElement();

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

        void UnsetAttributeProperty(BindableElement fieldElement)
        {
            var attributeName = fieldElement.bindingPath;
            // Undo/Redo
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

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
                CallInitOnElement();

                // Notify of changes.
                m_Selection.NotifyOfHierarchyChange(m_Inspector);
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
                    Builder.ShowWarning(string.Format(BuilderConstants.TypeAttributeMustDeriveFromMessage, field.label, desiredType.FullName));
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

        void OnValidatedAttributeValueChange(ChangeEvent<string> evt, Regex regex, string message)
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

        void OnAssetAttributeValueChange(ChangeEvent<UnityEngine.Object> evt)
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
                vta.RegisterAssetEntry(uri, field.objectType, evt.newValue);

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
            Undo.RegisterCompleteObjectUndo(m_Inspector.visualTreeAsset, BuilderConstants.ChangeAttributeValueUndoMessage);

            // Set value in asset.
            var vea = currentVisualElement.GetVisualElementAsset();
            var vta = currentVisualElement.GetVisualTreeAsset();

            if (m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance)
            {
                TemplateContainer templateContainerParent = BuilderAssetUtilities.GetVisualElementRootTemplate(currentVisualElement);

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
                            var templateVea = x.GetProperty(VisualTreeAsset.LinkedVEAInTemplatePropertyName) as VisualElementAsset;
                            var attributeOverrides = BuilderAssetUtilities.GetAccumulatedAttributeOverrides(currentVisualElement);
                            CallInitOnTemplateChild(x, templateVea, attributeOverrides);
                        });
                    }
                }
            }
            else
            {
                vea.SetAttributeValue(field.bindingPath, value);

                // Call Init();
                CallInitOnElement();
            }

            // Mark field as overridden.
            var styleRow = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
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

            // Notify of changes.
            m_Selection.NotifyOfHierarchyChange(m_Inspector);
        }

        void CallInitOnElement()
        {
            var fullTypeName = currentVisualElement.GetType().ToString();

            List<IUxmlFactory> factoryList;

            if (!VisualElementFactoryRegistry.TryGetValue(fullTypeName, out factoryList))
            {
                // We fallback on the BindableElement factory if we don't find any so
                // we can update the modified attributes. This fixes the TemplateContainer
                // factory not found.
                if (!VisualElementFactoryRegistry.TryGetValue(BuilderConstants.UxmlBindableElementTypeName, out factoryList))
                {
                    return;
                }
            }

            var traits = factoryList[0].GetTraits();

            if (traits == null)
                return;

            var context = new CreationContext(null, null, m_Inspector.visualTreeAsset, currentVisualElement);
            var vea = currentVisualElement.GetVisualElementAsset();

            try
            {
                traits.Init(currentVisualElement, vea, context);
            }
            catch
            {
                // HACK: This throws in 2019.3.0a4 because usageHints property throws when set after the element has already been added to the panel.
            }
        }

        void CallInitOnTemplateChild(VisualElement visualElement, VisualElementAsset vea, List<TemplateAsset.AttributeOverride> attributeOverrides)
        {
            List<IUxmlFactory> factoryList;

            var fullTypeName = currentVisualElement.GetType().ToString();
            if (!VisualElementFactoryRegistry.TryGetValue(fullTypeName, out factoryList))
            {
                if (!VisualElementFactoryRegistry.TryGetValue(BuilderConstants.UxmlBindableElementTypeName, out factoryList))
                {
                    return;
                }
            }

            var traits = factoryList[0].GetTraits();

            if (traits == null)
                return;

            var context = new CreationContext(null, attributeOverrides, null, null);

            traits.Init(visualElement, vea, context);
        }
    }
}
