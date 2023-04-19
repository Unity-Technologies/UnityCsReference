// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    /// <summary>
    /// This view displays and edits the list of uxml attributes of an object in a uxml document.
    /// </summary>
    internal class BuilderUxmlAttributesView
    {
        static readonly string s_AttributeFieldRowUssClassName = "unity-builder-attribute-field-row";
        static readonly string s_AttributeFieldUssClassName = "unity-builder-attribute-field";

        public static readonly string UxmlSerializedDataPathPrefix = nameof(UxmlSerializedDataDescriptionObject.serializedData) + ".";

        VisualTreeAsset m_UxmlDocument;
        VisualElement m_CurrentElement;
        VisualElementAsset m_CurrentUxmlElement;
        object m_CurrentSubObject;
        UxmlObjectAsset m_CurrentUxmlSubObject;

        // UxmlTraits
        List<UxmlAttributeDescription> m_UxmlTraitAttributes;
        static List<UxmlAttributeDescription> s_EmptyAttributeList = new();

        // UxmlSerializedData
        internal UxmlSerializedDataDescription m_SerializedDataDescription;
        internal UxmlSerializedDataDescriptionObject m_SerializedDataDescriptionData;
        internal SerializedObject m_CurrentElementScriptableObject;

        internal class UxmlSerializedDataDescriptionObject : ScriptableObject
        {
            [SerializeReference]
            public UxmlSerializedData serializedData;
        }

        enum AttributeFieldSource
        {
            /// <summary>
            /// Uses BindableElements
            /// </summary>
            UxmlTraits,

            /// <summary>
            /// Uses PropertyFields with nested BindableElements.
            /// </summary>
            UxmlSerializedData,
        }

        bool m_IsInTemplateInstance;

        /// <summary>
        /// Gets or sets the value that indicates whether undo-redo is enabled when editing fields.
        /// </summary>
        public bool undoEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the value that indicates whether UxmlTraits.Init() should be invoked on attribute value change.
        /// </summary>
        public bool callInitOnValueChange { get; set; } = true;

        /// <summary>
        /// The container of fields generated from uxml attributes.
        /// </summary>
        public VisualElement fieldsContainer { get; set; }

        /// <summary>
        /// The visual element being edited.
        /// </summary>
        public VisualElement currentElement => m_CurrentElement;

        /// <summary>
        /// The uxml document being edited.
        /// </summary>
        public VisualTreeAsset uxmlDocument => m_UxmlDocument;

        /// <summary>
        /// Returns the object associated to the uxml element attributes being edited by the view.
        /// </summary>
        public object attributesOwner => m_CurrentSubObject != null ? m_CurrentSubObject : m_CurrentElement;

        /// <summary>
        /// Returns the uxml element of which attributes are being edited by the view.
        /// </summary>
        public UxmlAsset attributesUxmlOwner => m_CurrentUxmlSubObject != null ? m_CurrentUxmlSubObject : m_CurrentUxmlElement;

        /// <summary>
        /// Returns the list of attributes.
        /// </summary>
        public IEnumerable<UxmlAttributeDescription> attributes =>
            CurrentFieldSource == AttributeFieldSource.UxmlSerializedData ?
            m_SerializedDataDescription.serializedAttributes :
            m_UxmlTraitAttributes;

        AttributeFieldSource CurrentFieldSource
        {
            get
            {
                if (m_SerializedDataDescription != null && !AlwaysUseUxmlTraits)
                    return AttributeFieldSource.UxmlSerializedData;
                return AttributeFieldSource.UxmlTraits;
            }
        }

        /// <summary>
        /// Provided for debug purposes to force the builder to use UxmlTraits
        /// </summary>
        internal static bool AlwaysUseUxmlTraits { get; set; }

        /// <summary>
        /// Finds the attribute description with the specified name.
        /// </summary>
        /// <param name="attributeName">The name of the attribute to seek.</param>
        /// <returns>The attribute description to seek.</returns>
        public UxmlAttributeDescription FindAttribute(string attributeName) => FindAttribute(attributeName, attributes);

        UxmlAttributeDescription FindAttribute(string attributeName, IEnumerable<UxmlAttributeDescription> uxmlAttributes)
        {
            foreach (var attr in uxmlAttributes)
            {
                if (attr.name == attributeName)
                    return attr;
            }

            return null;
        }

        /// <summary>
        /// Sets the specified VisualElement as the owner of attributes to be edited.
        /// </summary>
        /// <param name="uxmlDocument">The uxml document being edited</param>
        /// <param name="visualElement">The VisualElement that provides attributes to be edited</param>
        /// <param name="isInTemplate">Indicates whether the VisualElement is in a template instance</param>
        public void SetAttributesOwner(VisualTreeAsset uxmlDocument, VisualElement visualElement, bool isInTemplate = false)
        {
            m_IsInTemplateInstance = isInTemplate;
            SetAttributesOwner(uxmlDocument, visualElement, null, null);
            fieldsContainer.Clear();
        }

        /// <summary>
        /// Sets the specified sub object in the specified VisualElement as the owner of attributes to be edited.
        /// </summary>
        /// <param name="uxmlDocument">The uxml document being edited</param>
        /// <param name="visualElement">The VisualElement that owns the selected sub object</param>
        /// <param name="uxmlObjectElement">The uxml element from which the selected sub object was created</param>
        /// <param name="objectElement">The sub object that provides attributes to be edited</param>
        public virtual void SetAttributesOwner(VisualTreeAsset uxmlDoc, VisualElement visualElement, UxmlObjectAsset uxmlObjectElement, object objectElement)
        {
            m_UxmlDocument = uxmlDoc;
            m_CurrentUxmlElement = visualElement.GetVisualElementAsset();
            m_CurrentElement = visualElement;
            m_CurrentUxmlSubObject = uxmlObjectElement;
            m_CurrentSubObject = objectElement;
            m_SerializedDataDescription = null;
            m_CurrentElementScriptableObject = null;
            m_UxmlTraitAttributes = s_EmptyAttributeList;

            var element = m_CurrentSubObject ?? m_CurrentElement;

            if (element != null)
            {
                var elementType = m_CurrentSubObject != null ? m_CurrentUxmlSubObject.fullTypeName : m_CurrentElement.fullTypeName;
                m_SerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(elementType);
                if (m_SerializedDataDescription != null)
                {
                    m_SerializedDataDescriptionData = ScriptableObject.CreateInstance<UxmlSerializedDataDescriptionObject>();

                    UxmlSerializedData serializedData;
                    if (attributesUxmlOwner is VisualElementAsset elementAsset)
                    {
                        // Directly edit the elements serializedData.
                        elementAsset.serializedData ??= m_SerializedDataDescription.CreateSerializedData();
                        serializedData = elementAsset.serializedData;
                    }
                    else
                    {
                        serializedData = m_SerializedDataDescription.CreateSerializedData();
                    }

                    m_SerializedDataDescription.SyncSerializedData(element, serializedData);
                    m_SerializedDataDescriptionData.serializedData = serializedData;
                    m_CurrentElementScriptableObject = new SerializedObject(m_SerializedDataDescriptionData);
                }
            }

            if (m_CurrentSubObject != null)
            {
                var factory = m_UxmlDocument.GetUxmlObjectFactory(m_CurrentUxmlSubObject);
                m_UxmlTraitAttributes = factory.GetTraits().uxmlAttributesDescription.ToList();
            }
            else if (m_CurrentElement != null)
            {
                m_UxmlTraitAttributes = m_CurrentElement.GetAttributeDescriptions(true);
            }

            callInitOnValueChange = CurrentFieldSource == AttributeFieldSource.UxmlTraits;
        }

        /// <summary>
        /// Clears the the attributes owner.
        /// </summary>
        public void ResetAttributesOwner()
        {
            SetAttributesOwner(null, null);
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        public virtual void Refresh()
        {
            fieldsContainer.Clear();

            if (attributesOwner == null || attributes.Count() == 0)
                return;

            GenerateUxmlAttributeFields();
        }

        /// <summary>
        /// Generates fields from the uxml attributes.
        /// </summary>
        protected virtual void GenerateUxmlAttributeFields()
        {
            if (CurrentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                // UxmlTraits
                foreach (var attribute in m_UxmlTraitAttributes)
                {
                    if (attribute == null || attribute.name == null || IsAttributeIgnored(attribute))
                        continue;
                    CreateAttributeRow(attribute);
                }

                return;
            }

            // UxmlSerializedData
            foreach (var desc in attributes)
            {
                fieldsContainer.AddToClassList(InspectorElement.ussClassName);
                if (desc is UxmlSerializedAttributeDescription attributeDescription &&
                    attributeDescription.serializedField.GetCustomAttribute<HideInInspector>() == null)
                {
                    CreateAttributeRow(attributeDescription, UxmlSerializedDataPathPrefix + attributeDescription.serializedField.Name);
                }
            }

            BindUxmlSerializedData();
        }

        protected void BindUxmlSerializedData()
        {
            if (m_CurrentElementScriptableObject != null)
                fieldsContainer.Bind(m_CurrentElementScriptableObject);
        }

        /// <summary>
        /// Indicates whether the specified uxml attribute should be ignored.
        /// </summary>
        /// <param name="attribute">The attribute to evaluate.</param>
        /// <returns></returns>
        protected virtual bool IsAttributeIgnored(UxmlAttributeDescription attribute)
        {
            if (attributesOwner is VisualElement)
            {
                // Temporary check until we add an "obsolete" mechanism to uxml attribute description.
                return attribute.name == "show-horizontal-scroller" || attribute.name == "show-vertical-scroller" || attribute.name == "name";
            }
            return false;
        }

        private static string GetRemapAttributeNameToCSProperty(object attributesOwner, string attributeName)
        {
            if (attributesOwner is VisualElement)
            {
                if (attributesOwner is ObjectField && attributeName == "type")
                    return "objectType";
                else if (attributeName == "readonly")
                    return "isReadOnly";
            }

            var camel = BuilderNameUtilities.ConvertDashToCamel(attributeName);
            return camel;
        }

        internal string GetRemapAttributeNameToCSProperty(string attributeName)
        {
            return GetRemapAttributeNameToCSProperty(attributesOwner, attributeName);
        }

        internal string GetRemapCSPropertyToAttributeName(string CSProperty)
        {
            if (attributesOwner is VisualElement)
            {
                if (attributesOwner is ObjectField && CSProperty == "objectType")
                    return "type";
                else if (CSProperty == "isReadonly")
                    return "readOnly";
            }

            return BuilderNameUtilities.ConvertCamelToDash(CSProperty);
        }

        object GetAttributeValueNotMatchingCSPropertyName(string attributeName)
        {
            if (attributesOwner is ScrollView scrollView)
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
            else if (attributesOwner is ListView listView)
            {
                if (attributeName == "horizontal-scrolling")
                    return listView.horizontalScrollingEnabled;
            }
            else if (attributesOwner is BoundsField boundsField)
            {
                if (attributeName == "cx")
                    return boundsField.value.center.x;
                else if (attributeName == "cy")
                    return boundsField.value.center.y;
                else if (attributeName == "cz")
                    return boundsField.value.center.z;
                else
                    if (attributeName == "ex")
                    return boundsField.value.size.x;
                else
                    if (attributeName == "ey")
                    return boundsField.value.size.y;
                else
                    if (attributeName == "ez")
                    return boundsField.value.size.z;
            }
            else if (attributesOwner is BoundsIntField boundsIntField)
            {
                if (attributeName == "px")
                    return boundsIntField.value.position.x;
                else if (attributeName == "py")
                    return boundsIntField.value.position.y;
                else if (attributeName == "pz")
                    return boundsIntField.value.position.z;
                else
                    if (attributeName == "sx")
                    return boundsIntField.value.size.x;
                else
                    if (attributeName == "sy")
                    return boundsIntField.value.size.y;
                else
                    if (attributeName == "sz")
                    return boundsIntField.value.size.z;
            }
            else if (attributesOwner is RectField rectField)
            {
                if (attributeName == "x")
                    return rectField.value.x;
                else if (attributeName == "y")
                    return rectField.value.y;
                else if (attributeName == "w")
                    return rectField.value.width;
                else if (attributeName == "h")
                    return rectField.value.height;
            }
            else if (attributesOwner is Vector2Field vec2Field)
            {
                if (attributeName == "x")
                    return vec2Field.value.x;
                else if (attributeName == "y")
                    return vec2Field.value.y;
            }
            else if (attributesOwner is Vector3Field vec3Field)
            {
                if (attributeName == "x")
                    return vec3Field.value.x;
                else if (attributeName == "y")
                    return vec3Field.value.y;
                else if (attributeName == "z")
                    return vec3Field.value.z;
            }
            else if (attributesOwner is Vector4Field vec4Field)
            {
                if (attributeName == "x")
                    return vec4Field.value.x;
                else if (attributeName == "y")
                    return vec4Field.value.y;
                else if (attributeName == "z")
                    return vec4Field.value.z;
                else if (attributeName == "w")
                    return vec4Field.value.w;
            }
            else if (attributesOwner is Vector2IntField vec2IntField)
            {
                if (attributeName == "x")
                    return vec2IntField.value.x;
                else if (attributeName == "y")
                    return vec2IntField.value.y;
            }
            else if (attributesOwner is Vector3IntField vec3IntField)
            {
                if (attributeName == "x")
                    return vec3IntField.value.x;
                else if (attributeName == "y")
                    return vec3IntField.value.y;
                else if (attributeName == "z")
                    return vec3IntField.value.z;
            }

            return null;
        }

        static VisualElement GetRootFieldElement(VisualElement visualElement)
        {
            // UxmlSerializedFields have a PropertyField as the parent
            var currentElement = visualElement;
            while (currentElement != null)
            {
                if (currentElement is PropertyField propertyField &&
                    propertyField?.HasLinkedAttributeDescription() == true)
                    return propertyField;
                currentElement = currentElement.parent;
            }

            return visualElement;
        }

        static string GetAttributeName(VisualElement visualElement)
        {
            var desc = visualElement.GetLinkedAttributeDescription();
            return desc != null ? desc.name : ((IBindable)visualElement).bindingPath;
        }

        string GetBindingPropertyName(VisualElement visualElement)
        {
            // UxmlSerializedFields have a PropertyField as the parent
            var propertyField = visualElement as PropertyField ?? visualElement.parent as PropertyField;
            if (propertyField != null)
            {
                var serializedAttribute = propertyField.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                return serializedAttribute.serializedField.Name;
            }

            var bindingField = ((BindableElement)visualElement);
            return GetRemapAttributeNameToCSProperty(bindingField.bindingPath);
        }

        static BuilderStyleRow GetLinkedStyleRow(VisualElement visualElement)
        {
            return GetRootFieldElement(visualElement).GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
        }

        IEnumerable<VisualElement> GetAttributeFields()
        {
            if (CurrentFieldSource == AttributeFieldSource.UxmlSerializedData)
                return fieldsContainer.Query<PropertyField>().Where(ve => ve.HasLinkedAttributeDescription()).Build().AsEnumerable<VisualElement>();
            return fieldsContainer.Query<BindableElement>().Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
        }

        protected virtual BuilderStyleRow CreateAttributeRow(string attributeName, VisualElement parent = null)
        {
            if (CurrentFieldSource == AttributeFieldSource.UxmlSerializedData)
            {
                var foundField = m_SerializedDataDescription.FindAttributeWithUxmlName(attributeName);
                if (foundField != null)
                {
                    return CreateAttributeRow(foundField, UxmlSerializedDataPathPrefix + foundField.serializedField.Name, parent);
                }
            }

            return CreateAttributeRow(FindAttribute(attributeName, m_UxmlTraitAttributes), parent);
        }

        /// <summary>
        /// Creates a row in the fields container for the specified attribute.
        /// </summary>
        /// <param name="attribute">The target attribute</param>
        /// <param name="parent">The parent where to add the row</param>
        /// <returns></returns>
        protected BuilderStyleRow CreateAttributeRow(UxmlAttributeDescription attribute, VisualElement parent = null)
        {
            BindableElement fieldElement = CreateAttributeField(attribute);

            parent ??= fieldsContainer;

            // Create row.
            var styleRow = new BuilderStyleRow();

            styleRow.AddToClassList($"{s_AttributeFieldRowUssClassName}-{attribute.name}");
            styleRow.Add(fieldElement);

            // Link the field.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);
            fieldElement.SetLinkedAttributeDescription(attribute);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            // Set initial value.
            UpdateAttributeField(fieldElement);

            // Setup field binding path.
            fieldElement.bindingPath = attribute.name;

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator((evt) => BuildAttributeFieldContextualMenu(evt.menu, fieldElement)));

            if (fieldElement.GetFieldStatusIndicator() != null)
            {
                fieldElement.GetFieldStatusIndicator().populateMenuItems =
                    (menu) => BuildAttributeFieldContextualMenu(menu, fieldElement);
            }

            UpdateFieldStatus(fieldElement);

            return styleRow;
        }

        /// <summary>
        /// Creates a row in the fields container for the specified UxmlSerializedData attribute.
        /// </summary>
        /// <param name="attribute">The target attribute</param>
        /// <param name="propertyPath">The SerializedProperty path for this field</param>
        /// <param name="parent">The parent where to add the row</param>
        /// <returns></returns>
        protected BuilderStyleRow CreateAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath, VisualElement parent = null)
        {
            // We dont currently support UxmlObjects
            if (attribute.isUxmlObject)
                return null;

            var fieldElement = new PropertyField
            {
                bindingPath = propertyPath,
                label = BuilderNameUtilities.ConvertDashToHuman(attribute.name)
            };

            fieldElement.RegisterCallback<TooltipEvent>(e =>
            {
                // Only show tooltip on labels
                if (e.target is Label)
                {
                    var tooltip = attribute.serializedField.GetCustomAttribute<TooltipAttribute>();
                    var valueInfo = GetValueInfo(fieldElement);

                    e.tooltip = BuilderInspector.GetFieldTooltip(fieldElement, valueInfo, tooltip?.tooltip);
                }
                else
                {
                    e.tooltip = null;
                }

                e.rect = fieldElement.GetTooltipRect();
                e.StopPropagation();
            },
            useTrickleDown:TrickleDown.TrickleDown);

            // Register for all changes including and child changes such as those generated from list property items.
            fieldElement.RegisterCallback<SerializedPropertyChangeEvent>(OnPropertyFieldValueChange, TrickleDown.TrickleDown);

            parent ??= fieldsContainer;

            // Create row.
            var styleRow = new BuilderStyleRow();

            styleRow.AddToClassList($"{s_AttributeFieldRowUssClassName}-{propertyPath}");
            styleRow.Add(fieldElement);

            // Link the PropertyField to the BuilderStyleRow.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);

            // Link the PropertyField to the UxmlSerializedAttributeDescription.
            fieldElement.SetLinkedAttributeDescription(attribute);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            UpdateAttributeOverrideStyle(fieldElement);

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator((evt) => BuildAttributeFieldContextualMenu(evt.menu, fieldElement)));

            if (fieldElement.GetFieldStatusIndicator() != null)
            {
                fieldElement.GetFieldStatusIndicator().populateMenuItems =
                    (menu) => BuildAttributeFieldContextualMenu(menu, fieldElement);
            }

            UpdateFieldStatus(fieldElement);

            return styleRow;
        }

        /// <summary>
        /// Creates a field from the specified attribute.
        /// </summary>
        /// <param name="attribute">The target attribute</param>
        /// <returns></returns>
        protected virtual BindableElement CreateAttributeField(UxmlAttributeDescription attribute)
        {
            var factory = BuilderUxmlAttributeFieldFactoryRegistry.GetFactory(attributesOwner, attributesUxmlOwner, attribute);
            var uiField = factory.CreateField(attributesOwner, attributesUxmlOwner, attribute, OnAttributeValueChanged);
            uiField.AddToClassList($"{s_AttributeFieldUssClassName}-{attribute.name}");
            uiField.SetProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName, factory);
            return uiField as BindableElement;
        }

        /// <summary>
        /// Gets the value of the specified attribute.
        /// </summary>
        /// <param name="attribute">The target attribute.</param>
        /// <returns></returns>
        protected virtual object GetAttributeValue(UxmlAttributeDescription attribute)
        {
            var objType = attributesOwner.GetType();
            var csPropertyName = GetRemapAttributeNameToCSProperty(attribute.name);
            var fieldInfo = objType.GetProperty(csPropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            return fieldInfo == null ? GetAttributeValueNotMatchingCSPropertyName(attribute.name) : fieldInfo.GetValue(attributesOwner, null);
        }

        void OnPropertyFieldValueChange(SerializedPropertyChangeEvent evt)
        {
            var fieldElement = GetRootFieldElement(evt.target as VisualElement);
            var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;

            // We choose to disregard callbacks when the value remains unchanged,
            // which occurs during the initial binding of a field or during an Unset operation.
            description.TryGetValueFromObject(attributesOwner, out var previousValue);
            var newValue = description.GetSerializedValue(m_SerializedDataDescriptionData.serializedData);

            // Unity serializes null values as default objects, so we need to do the same to compare.
            if (previousValue == null && !typeof(Object).IsAssignableFrom(description.type) && description.type.GetConstructor(Type.EmptyTypes) != null)
                previousValue = Activator.CreateInstance(description.type);

            if (UxmlAttributeComparison.ObjectEquals(previousValue, newValue))
                return;

            // Apply changes to the element
            m_SerializedDataDescriptionData.serializedData.Deserialize(attributesOwner);

            // Now resync as its possible that the setters made changes during Deserialize, e.g clamping values.
            m_SerializedDataDescription.SyncSerializedData(attributesOwner, m_SerializedDataDescriptionData.serializedData);
            m_CurrentElementScriptableObject.UpdateIfRequiredOrScript();
            newValue = description.GetSerializedValue(m_SerializedDataDescriptionData.serializedData);

            string stringValue;
            if (newValue == null || !UxmlAttributeConverter.TryConvertToString(newValue, m_UxmlDocument, out stringValue))
                stringValue = newValue?.ToString();

            PostAttributeValueChange(fieldElement, stringValue);
        }

        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        void UpdateAttributeField(BindableElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();
            object fieldValue = GetAttributeValue(attribute);

            if (fieldValue == null)
            {
                if (attributesOwner is EnumField defaultEnumField &&
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

                else if (attributesOwner is EnumFlagsField defaultEnumFlagsField &&
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
                else if (!(fieldElement is ObjectField or TextField))
                {
                    return;
                }
            }

            if ((attribute.name.Equals("allow-add") || attribute.name.Equals("allow-remove")) &&
                attributesOwner is BaseListView)
            {
                var styleRow =
                    fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
                styleRow?.contentContainer.AddToClassList(BuilderConstants.InspectorListViewAllowAddRemoveFieldClassName);
            }

            UpdateAttributeField(fieldElement, attribute, fieldValue);

            UpdateAttributeOverrideStyle(fieldElement);
            UpdateFieldStatus(fieldElement);
        }

        internal virtual void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            var styleRow =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedAttributeDescriptionVEPropertyName) as
                    UxmlAttributeDescription;
            styleRow.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, IsAttributeOverriden(attribute));
        }

        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        /// <para name="attribute">The attribute related to the field</para>
        /// <param name="value">The new value</param>
        protected virtual void UpdateAttributeField(BindableElement fieldElement, UxmlAttributeDescription attribute, object value)
        {
            if (fieldElement.HasProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName))
            {
                var fieldFactory = fieldElement.GetProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName) as
                        IBuilderUxmlAttributeFieldFactory;
                fieldFactory.SetFieldValue(fieldElement, attributesOwner, uxmlDocument, attributesUxmlOwner, attribute, value);
            }
        }

        /// <summary>
        /// Gets details about the value of the specified field.
        /// </summary>
        /// <param name="fieldElement">The target field.</param>
        protected virtual FieldValueInfo GetValueInfo(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();
            var attributeIsOverriden = IsAttributeOverriden(attribute);
            var valueSourceType = attributeIsOverriden ? FieldValueSourceInfoType.Inline : FieldValueSourceInfoType.Default;

            return new FieldValueInfo()
            {
                type = FieldValueInfoType.UXMLAttribute,
                name = attribute.name,
                valueBinding = new FieldValueBindingInfo(FieldValueBindingInfoType.Constant),
                valueSource = new FieldValueSourceInfo(valueSourceType)
            };
        }

        /// <summary>
        /// Updates the status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to update.</param>
        protected virtual void UpdateFieldStatus(VisualElement fieldElement)
        {
            fieldElement = GetRootFieldElement(fieldElement);
            var valueInfo = GetValueInfo(fieldElement);

            fieldElement.SetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName, valueInfo);
            BuilderInspector.UpdateFieldStatusIconAndStyling(currentElement, fieldElement, valueInfo);

            if (CurrentFieldSource == AttributeFieldSource.UxmlTraits)
                BuilderInspector.UpdateFieldTooltip(fieldElement, valueInfo);
        }

        /// <summary>
        /// Notifies that the list of attributes has changed.
        /// </summary>
        protected virtual void NotifyAttributesChanged()
        {
        }

        protected bool IsAttributeOverriden(UxmlAttributeDescription attribute)
        {
            return IsAttributeOverriden(attributesOwner, attributesUxmlOwner, attribute);
        }

        /// <summary>
        /// Indicates whether the specified uxml attribute is defined in the uxml element related to the specified instance.
        /// </summary>
        /// <param name="attributesOwner">An instance of the uxml element that owns the uxml attribute</param>
        /// <param name="attribute">The uxml attribute</param>
        public static bool IsAttributeOverriden(VisualElement attributesOwner, UxmlAttributeDescription attribute)
        {
            return IsAttributeOverriden(attributesOwner, attributesOwner.GetVisualElementAsset(), attribute);
        }

        /// <summary>
         /// Indicates whether the specified attribute is defined in the specified uxml element.
         /// </summary>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute to evaluate.</param>
        /// <param name="attribute">The uxml attribute.</param>
         /// <returns></returns>
        public static bool IsAttributeOverriden(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            if (attributeOwner is VisualElement ve)
            {
                if (attributeUxmlOwner != null && attribute.name == "picking-mode")
                {
                    var veaAttributeValue = attributeUxmlOwner.GetAttributeValue(attribute.name);
                    if (veaAttributeValue != null &&
                        veaAttributeValue.ToLower() != attribute.defaultValueAsString.ToLower())
                        return true;
                }
                else if (attribute.name == "name")
                {
                    if (!string.IsNullOrEmpty(ve.name))
                        return true;
                }
                else if (BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(ve, attribute.name))
                {
                    return true;
                }
            }

            if (attributeUxmlOwner != null && attributeUxmlOwner.HasAttribute(attribute.name))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the value of the specified attribute field to its default value.
        /// </summary>
        /// <param name="fieldElement">The field to reset.</param>
        void ResetAttributeFieldToDefault(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();
            ResetAttributeFieldToDefault(fieldElement, attribute);

            // Clear override.
            var styleRow = GetLinkedStyleRow(fieldElement);
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

            if (CurrentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                var styleFields = styleRow.Query<BindableElement>().ToList();
                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
            }
        }

        /// <summary>
        /// Resets the value of the specified attribute field to its default value.
        /// </summary>
        /// <param name="fieldElement">The field to reset</param>
        /// <para name="attribute">The attribute related to the field</para>
        protected virtual void ResetAttributeFieldToDefault(VisualElement fieldElement, UxmlAttributeDescription attribute)
        {
            if (fieldElement.HasProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName))
            {
                var fieldFactory = fieldElement.GetProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName) as IBuilderUxmlAttributeFieldFactory;
                fieldFactory.ResetFieldValue(fieldElement, attributesOwner, uxmlDocument, attributesUxmlOwner, attribute);
            }
            else
            {
                var desc = attribute as UxmlSerializedAttributeDescription;

                desc.SetSerializedValue(m_SerializedDataDescriptionData.serializedData, desc.defaultValueClone);
                m_CurrentElementScriptableObject.UpdateIfRequiredOrScript();
                m_SerializedDataDescriptionData.serializedData.Deserialize(attributesOwner);

                // Rebind to the new default value
                fieldElement.Bind(m_CurrentElementScriptableObject);
            }
        }

        protected virtual void BuildAttributeFieldContextualMenu(DropdownMenu menu, VisualElement fieldElement)
        {
            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetAttributeProperty,
                action =>
                {
                    var fieldElement = action.userData as VisualElement;
                    if (fieldElement == null)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = GetAttributeName(fieldElement);
                    var bindingProperty = GetBindingPropertyName(fieldElement);
                    var isAttributeOverrideAttribute =
                        m_IsInTemplateInstance
                        && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(m_CurrentElement,
                            attributeName);

                    return (attributesUxmlOwner != null && attributesUxmlOwner.HasAttribute(attributeName)) || isAttributeOverrideAttribute
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled;
                },
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute?.name == null)
                            continue;

                        if (m_IsInTemplateInstance
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
            if (undoEnabled)
            {
                Undo.RegisterCompleteObjectUndo(m_UxmlDocument,
                    BuilderConstants.ChangeAttributeValueUndoMessage);
            }

            if (m_IsInTemplateInstance)
            {
                var parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(m_CurrentElement);
                var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                var attributeOverrides =
                    new List<TemplateAsset.AttributeOverride>(parentTemplateAsset.attributeOverrides);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (attributeOverride.m_ElementName == m_CurrentElement.name)
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
                foreach (var attribute in attributes)
                {
                    if (attribute?.name == null)
                        continue;

                    // Unset value in asset.
                    attributesUxmlOwner.RemoveAttribute(attribute.name);
                }

                var fields = GetAttributeFields();
                foreach (var fieldElement in fields)
                {
                    // Reset UI value.
                    ResetAttributeFieldToDefault(fieldElement);
                    UpdateFieldStatus(fieldElement);
                }

                // Call Init();
                CallInitOnElement();

                // Notify of changes.
                NotifyAttributesChanged();
            }
        }

        private UxmlTraits GetCurrentElementTraits()
        {
            string uxmlTypeName = null;

            if (m_CurrentElement is TemplateContainer)
            {
                uxmlTypeName = BuilderConstants.BuilderInspectorTemplateInstance;
            }
            else
            {
                uxmlTypeName = m_CurrentUxmlElement != null ? m_CurrentUxmlElement.fullTypeName : m_CurrentElement.GetType().ToString();
            }

            List<IUxmlFactory> factories = null;

            // Workaround: TemplateContainer.UxmlTrais.Init() cannot be called multiple times. Otherwise, the source template is loaded again into the template container without clearing the previous content.
            if (uxmlTypeName == BuilderConstants.UxmlInstanceTypeName || !VisualElementFactoryRegistry.TryGetValue(uxmlTypeName, out factories))
            {
                // We fallback on the VisualElement factory if we don't find any so
                // we can update the modified attributes. This fixes the TemplateContainer
                // factory not found.
                VisualElementFactoryRegistry.TryGetValue(BuilderConstants.UxmlVisualElementTypeName,
                    out factories);
            }

            if (factories == null)
                return null;

            return factories[0].GetTraits() as UxmlTraits;
        }

        internal void CallInitOnElement()
        {
            if (!callInitOnValueChange)
                return;

            var traits = GetCurrentElementTraits();

            if (traits == null)
                return;

            var context = new CreationContext(null, null, m_UxmlDocument, m_CurrentElement);

            traits.Init(m_CurrentElement, m_CurrentUxmlElement, context);
        }

        internal void CallInitOnTemplateChild(VisualElement visualElement, VisualElementAsset vea,
           List<CreationContext.AttributeOverrideRange> attributeOverrides)
        {
            if (!callInitOnValueChange)
                return;

            var traits = GetCurrentElementTraits();

            if (traits == null)
                return;

            var context = new CreationContext(null, attributeOverrides, null, null);

            traits.Init(visualElement, vea, context);
        }

        void UnsetAttributeProperty(DropdownMenuAction action)
        {
            var fieldElement = action.userData as VisualElement;
            var bindingPath = GetAttributeName(fieldElement);
            UnsetAttributeProperty(fieldElement);

            if (CurrentFieldSource != AttributeFieldSource.UxmlTraits)
                return;

            // When unsetting the type value for an enum field, we also need to clear the value field as well.
            if (attributesOwner is EnumField && bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
            if (attributesOwner is EnumFlagsField && bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
        }

        public void UnsetAttributeProperty(VisualElement fieldElement)
        {
            var attributeName = GetAttributeName(fieldElement);

            // Undo/Redo
            if (undoEnabled)
            {
                Undo.RegisterCompleteObjectUndo(m_UxmlDocument,
                    BuilderConstants.ChangeAttributeValueUndoMessage);
            }

            // Unset value in asset.

            if (m_IsInTemplateInstance)
            {
                var templateContainer = BuilderAssetUtilities.GetVisualElementRootTemplate(m_CurrentElement);
                var templateAsset = templateContainer.GetVisualElementAsset() as TemplateAsset;

                if (templateAsset != null)
                {
                    var builder = Builder.ActiveWindow;
                    var hierarchyView = builder.hierarchy.elementHierarchyView;
                    var selectionId = hierarchyView.GetSelectedItemId();

                    templateAsset.RemoveAttributeOverride(m_CurrentElement.name, attributeName);

                    builder.OnEnableAfterAllSerialization();

                    hierarchyView.SelectItemById(selectionId);
                }
            }
            else
            {
                attributesUxmlOwner.RemoveAttribute(attributeName);

                // Reset UI value.
                ResetAttributeFieldToDefault(fieldElement);

                // Call Init();
                CallInitOnElement();

                // Notify of changes.
                NotifyAttributesChanged();

                UpdateFieldStatus(fieldElement);
                Refresh();
            }
        }

        void OnAttributeValueChange(ChangeEvent<string> evt)
        {
            var field = evt.elementTarget as TextField;
            PostAttributeValueChange(field, evt.newValue);
        }

        internal void OnValidatedAttributeValueChange(ChangeEvent<string> evt, Regex regex, string message)
        {
            var field = evt.elementTarget as TextField;
            if (!string.IsNullOrEmpty(evt.newValue) && !regex.IsMatch(evt.newValue))
            {
                Builder.ShowWarning(string.Format(message, field.label));
                field.SetValueWithoutNotify(evt.previousValue);
                evt.StopPropagation();
                return;
            }

            // Sync with serialized property
            if (CurrentFieldSource == AttributeFieldSource.UxmlSerializedData)
            {
                var prop = m_CurrentElementScriptableObject.FindProperty(UxmlSerializedDataPathPrefix + field.bindingPath);
                prop.stringValue = evt.newValue;
                m_CurrentElementScriptableObject.ApplyModifiedProperties();
                m_SerializedDataDescriptionData.serializedData.Deserialize(attributesOwner);
            }

            OnAttributeValueChange(evt);
        }

        void OnAttributeValueChanged(VisualElement field, UxmlAttributeDescription attribute, object value, string uxmlValue)
        {
            var attributeType = attribute.GetType();
            bool needRefresh = false;

            if (value is Object asset)
            {
                var assetType = attributeType.IsGenericType ? attributeType.GetGenericArguments()[0] : (attribute as IUxmlAssetAttributeDescription)?.assetType;

                if (!string.IsNullOrEmpty(uxmlValue) && !uxmlDocument.AssetEntryExists(uxmlValue, assetType))
                    uxmlDocument.RegisterAssetEntry(uxmlValue, assetType, asset);
            }
            else if (CurrentFieldSource == AttributeFieldSource.UxmlTraits &&
                attributeType.IsGenericType &&
                !attributeType.GetGenericArguments()[0].IsEnum &&
                attributeType.GetGenericArguments()[0] is Type)
            {
                if (attributesOwner is EnumField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField);
                    needRefresh = true;
                }
                else if (attributesOwner is EnumFlagsField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField);
                    needRefresh = true;
                }

                // If the type of an object field changes, we have to refresh the inspector to ensure it has the correct type associated with it.
                if (attributesOwner is ObjectField && attribute.name == "type")
                {
                    needRefresh = true;
                }
            }

            PostAttributeValueChange(field, uxmlValue);

            if (needRefresh)
                Refresh();
        }

        void PostAttributeValueChange(VisualElement field, string value)
        {
            var attributeName = GetAttributeName(field);

            // Undo/Redo
            if (undoEnabled)
            {
                Undo.RegisterCompleteObjectUndo(m_UxmlDocument,
                    BuilderConstants.ChangeAttributeValueUndoMessage);
            }

            // Set value in asset.
            if (m_IsInTemplateInstance)
            {
                TemplateContainer templateContainerParent =
                    BuilderAssetUtilities.GetVisualElementRootTemplate(m_CurrentElement);

                if (templateContainerParent != null)
                {
                    var templateAsset = templateContainerParent.GetVisualElementAsset() as TemplateAsset;
                    var currentVisualElementName = m_CurrentElement.name;

                    if (!string.IsNullOrEmpty(currentVisualElementName))
                    {
                        templateAsset.SetAttributeOverride(currentVisualElementName, attributeName, value);

                        var document = Builder.ActiveWindow.document;
                        var rootElement = Builder.ActiveWindow.viewport.documentRootElement;

                        var elementsToChange = templateContainerParent.Query<VisualElement>(currentVisualElementName);
                        elementsToChange.ForEach(x =>
                        {
                            var templateVea =
                                x.GetProperty(VisualTreeAsset.LinkedVEAInTemplatePropertyName) as VisualElementAsset;
                            var attributeOverrides =
                                BuilderAssetUtilities.GetAccumulatedAttributeOverrides(m_CurrentElement);
                            CallInitOnTemplateChild(x, templateVea, attributeOverrides);
                        });
                    }
                }
            }
            else
            {
                attributesUxmlOwner.SetAttribute(attributeName, value);

                // Call Init();
                CallInitOnElement();
            }

            // Mark field as overridden.
            var styleRow = GetLinkedStyleRow(field);

            if (styleRow != null)
            {
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

                if (CurrentFieldSource == AttributeFieldSource.UxmlTraits)
                {
                    var styleFields = styleRow.Query<BindableElement>().ToList();

                    foreach (var styleField in styleFields)
                    {
                        styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                        var styleBindingPath = GetAttributeName(styleField);
                        if (attributeName == styleBindingPath)
                        {
                            styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                        }
                        else if (!string.IsNullOrEmpty(styleBindingPath) &&
                                 attributeName != styleBindingPath &&
                                 !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                        {
                            styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                        }
                    }
                }
            }

            // Notify of changes.
            NotifyAttributesChanged();

            if (styleRow != null)
            {
                UpdateFieldStatus(field);
            }
        }
    }
}
