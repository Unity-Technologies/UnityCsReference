using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// This view displays and edits the list of uxml attributes of an object in a uxml document.
    /// </summary>
    internal class BuilderUxmlAttributesView
    {
        static readonly string s_AttributeFieldRowUssClassName = "unity-builder-attribute-field-row";
        static readonly string s_AttributeFieldUssClassName = "unity-builder-attribute-field";
        VisualTreeAsset m_UxmlDocument;
        VisualElement m_CurrentElement;
        VisualElementAsset m_CurrentUxmlElement;
        object m_CurrentSubObject;
        UxmlObjectAsset m_CurrentUxmlSubObject;
        List<UxmlAttributeDescription> m_Attributes;
        static List<UxmlAttributeDescription> s_EmptyAttributeList = new();

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
        public List<UxmlAttributeDescription> attributes => m_Attributes;

        /// <summary>
        /// Finds the attribute description with the specified name.
        /// </summary>
        /// <param name="attributeName">The name of the attribute to seek.</param>
        /// <returns>The attribute description to seek.</returns>
        public UxmlAttributeDescription FindAttribute(string attributeName)
        {
            foreach (var attr in m_Attributes)
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

            if (m_CurrentSubObject != null)
            {
                var factory = m_UxmlDocument.GetUxmlObjectFactory(m_CurrentUxmlSubObject);
                m_Attributes = factory.GetTraits().uxmlAttributesDescription.ToList();
            }
            else if (m_CurrentElement != null)
            {
                m_Attributes = m_CurrentElement.GetAttributeDescriptions();
            }

            m_Attributes ??= s_EmptyAttributeList;
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

            if (attributesOwner == null || m_Attributes.Count == 0)
                return;

            GenerateUxmlAttributeFields();
        }

        /// <summary>
        /// Generates fields from the uxml attributes.
        /// </summary>
        protected virtual void GenerateUxmlAttributeFields()
        {
            foreach (var attribute in m_Attributes)
            {
                if (attribute == null || attribute.name == null || IsAttributeIgnored(attribute))
                    continue;
                CreateAttributeRow(attribute);
            }
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

        protected BuilderStyleRow CreateAttributeRow(string attributeName, VisualElement parent = null)
        {
            return CreateAttributeRow(FindAttribute(attributeName), parent);
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

        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        void UpdateAttributeField(BindableElement fieldElement)
        {
            var styleRow =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute =
                fieldElement.GetLinkedAttributeDescription();
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

            UpdateAttributeField(fieldElement, attribute, fieldValue);
            styleRow.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, IsAttributeOverriden(attribute));

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
        protected virtual FieldValueInfo GetValueInfo(BindableElement fieldElement)
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
        protected virtual void UpdateFieldStatus(BindableElement fieldElement)
        {
            var valueInfo = GetValueInfo(fieldElement);

            fieldElement.SetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName, valueInfo);
            BuilderInspector.UpdateFieldStatusIconAndStyling(currentElement, fieldElement, valueInfo);
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
        void ResetAttributeFieldToDefault(BindableElement fieldElement)
        {
            var styleRow =
                fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
            var attribute = fieldElement.GetLinkedAttributeDescription();

            ResetAttributeFieldToDefault(fieldElement, attribute);

            // Clear override.
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            var styleFields = styleRow.Query<BindableElement>().ToList();
            foreach (var styleField in styleFields)
            {
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            }
        }

        /// <summary>
        /// Resets the value of the specified attribute field to its default value.
        /// </summary>
        /// <param name="fieldElement">The field to reset</param>
        /// <para name="attribute">The attribute related to the field</para>
        protected virtual void ResetAttributeFieldToDefault(BindableElement fieldElement, UxmlAttributeDescription attribute)
        {
            if (fieldElement.HasProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName))
            {
                var fieldFactory = fieldElement.GetProperty(BuilderConstants.AttributeFieldFactoryVEPropertyName) as IBuilderUxmlAttributeFieldFactory;
                fieldFactory.ResetFieldValue(fieldElement, attributesOwner, uxmlDocument, attributesUxmlOwner, attribute);
            }
        }

        protected virtual void BuildAttributeFieldContextualMenu(DropdownMenu menu, VisualElement fieldElement)
        {
            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetAttributeProperty,
                action =>
                {
                    var fieldElement = action.userData as BindableElement;
                    if (fieldElement == null)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = fieldElement.bindingPath;
                    var bindingProperty = GetRemapAttributeNameToCSProperty(attributesOwner, attributeName);
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
                    foreach (var attribute in m_Attributes)
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
                foreach (var attribute in m_Attributes)
                {
                    if (attribute?.name == null)
                        continue;

                    // Unset value in asset.
                    attributesUxmlOwner.RemoveAttribute(attribute.name);
                }

                var fields = fieldsContainer.Query<BindableElement>()
                    .Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
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
            var fieldElement = action.userData as BindableElement;
            UnsetAttributeProperty(fieldElement);

            // When unsetting the type value for an enum field, we also need to clear the value field as well.
            if (attributesOwner is EnumField && fieldElement.bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
            if (attributesOwner is EnumFlagsField && fieldElement.bindingPath == "type")
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField);
            }
        }

        public void UnsetAttributeProperty(BindableElement fieldElement)
        {
            var attributeName = fieldElement.bindingPath;

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

            OnAttributeValueChange(evt);
        }

        void OnAttributeValueChanged(VisualElement field, UxmlAttributeDescription attribute, object value, string uxmlValue)
        {
            var attributeType = attribute.GetType();
            bool needRefresh = false;

            if (value is UnityEngine.Object asset)
            {
                var assetType = attributeType.GetGenericArguments()[0];

                if (!string.IsNullOrEmpty(uxmlValue) && !uxmlDocument.AssetEntryExists(uxmlValue, assetType))
                    uxmlDocument.RegisterAssetEntry(uxmlValue, assetType, asset);
            }
            else if (attributeType.IsGenericType && !attributeType.GetGenericArguments()[0].IsEnum && attributeType.GetGenericArguments()[0] is Type)
            {
                if (attributesOwner is EnumField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField);
                }
                else if (attributesOwner is EnumFlagsField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField);
                }
                needRefresh = true;
            }

            PostAttributeValueChange(field, uxmlValue);

            if (needRefresh)
                Refresh();
        }

        void PostAttributeValueChange(VisualElement field, string value)
        {
            var attributeName = (field as BindableElement).bindingPath;

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
            var styleRow = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;

            if (styleRow != null)
            {
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    if (attributeName == styleField.bindingPath)
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                             attributeName != styleField.bindingPath &&
                             !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    }
                }
            }

            // Notify of changes.
            NotifyAttributesChanged();

            if (styleRow != null)
            {
                UpdateFieldStatus(field as BindableElement);
            }
        }
    }
}
