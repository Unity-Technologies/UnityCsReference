// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
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
        public static readonly string builderBoundPropertyFieldName = "unity-builder-bound-property-field";
        public static readonly string builderSerializedPropertyFieldName = "unity-builder-serialized-property-field";

        static readonly string s_TempSerializedRootPath = nameof(TempSerializedData.serializedData) + ".";

        VisualTreeAsset m_UxmlDocument;
        VisualElement m_CurrentElement;
        VisualElementAsset m_CurrentUxmlElement;

        // UxmlTraits
        List<UxmlAttributeDescription> m_UxmlTraitAttributes;
        static readonly List<UxmlAttributeDescription> s_EmptyAttributeList = new();
        static readonly List<UxmlObjectAsset> s_UxmlAssets = new();

        // UxmlSerializedData
        internal UxmlSerializedDataDescription m_SerializedDataDescription;
        internal SerializedObject m_CurrentElementSerializedObject;
        SerializedObject m_BoundValuesSerializedObject;
        VisualTreeAsset m_SerializedDataTreeAssetBindingsCopy;
        TempSerializedData m_TempSerializedData;

        public string serializedRootPath { get; set; }

        internal class UxmlAssetSerializedDataRoot : VisualElement
        {
            public UxmlSerializedDataDescription dataDescription;
            public string rootPath;
            public override string ToString() => $"{rootPath} ({dataDescription})";
        }

        // Makes it easier to identify the root when dealing with nested classes
        internal class UxmlSerializedDataAttributeField : VisualElement
        {
        }

        // For when we need to view the serialized data from a temp visual element, such as one created by script.
        class TempSerializedData : ScriptableObject
        {
            [SerializeReference]
            public UxmlSerializedData serializedData;
        }

        protected enum AttributeFieldSource
        {
            /// <summary>
            /// Uses BindableElements
            /// </summary>
            UxmlTraits,

            /// <summary>
            /// Uses UxmlSerializedDataAttributeField with nested BindableElements.
            /// </summary>
            UxmlSerializedData,
        }

        bool m_IsInTemplateInstance;

        protected bool isInTemplateInstance => m_IsInTemplateInstance;

        public UxmlSerializedData uxmlSerializedData => m_CurrentUxmlElement != null ? m_CurrentUxmlElement.serializedData : m_TempSerializedData.serializedData;

        /// <summary>
        /// Are we able to edit the element or just view its data?
        /// </summary>
        internal bool readOnly => m_CurrentUxmlElement == null;

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
        /// Returns the uxml element of which attributes are being edited by the view.
        /// </summary>
        public UxmlAsset attributesUxmlOwner => m_CurrentUxmlElement;

        /// <summary>
        /// Returns the list of attributes.
        /// </summary>
        public IReadOnlyList<UxmlAttributeDescription> attributes =>
            currentFieldSource == AttributeFieldSource.UxmlSerializedData ?
            m_SerializedDataDescription.serializedAttributes :
            m_UxmlTraitAttributes;

        protected AttributeFieldSource currentFieldSource
        {
            get
            {
                if (m_SerializedDataDescription != null && !alwaysUseUxmlTraits)
                    return AttributeFieldSource.UxmlSerializedData;
                return AttributeFieldSource.UxmlTraits;
            }
        }

        /// <summary>
        /// Provided for debug purposes to force the builder to use UxmlTraits
        /// </summary>
        internal static bool alwaysUseUxmlTraits { get; set; }

        public BuilderUxmlAttributesView()
        {
            Undo.undoRedoPerformed += CallDeserializeOnElement;
        }

        ~BuilderUxmlAttributesView()
        {
            Undo.undoRedoPerformed -= CallDeserializeOnElement;
        }

        /// <summary>
        /// Finds the attribute description with the specified name.
        /// </summary>
        /// <param name="attributeName">The name of the attribute to seek.</param>
        /// <returns>The attribute description to seek.</returns>
        public UxmlAttributeDescription FindAttribute(string attributeName) => FindAttribute(attributeName, attributes);

        static UxmlAttributeDescription FindAttribute(string attributeName, IEnumerable<UxmlAttributeDescription> uxmlAttributes)
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
            SetAttributesOwner(uxmlDocument, visualElement);
            fieldsContainer.Clear();
        }

        /// <summary>
        /// Sets the specified sub object in the specified VisualElement as the owner of attributes to be edited.
        /// </summary>
        /// <param name="uxmlDocument">The uxml document being edited</param>
        /// <param name="visualElement">The VisualElement that owns the selected sub object</param>
        /// <param name="objectElement">The sub object that provides attributes to be edited</param>
        public void SetAttributesOwner(VisualTreeAsset uxmlDocument, VisualElement visualElement)
        {
            m_UxmlDocument = uxmlDocument;
            m_CurrentUxmlElement = visualElement.GetVisualElementAsset();
            m_CurrentElement = visualElement;
            m_SerializedDataDescription = null;
            m_CurrentElementSerializedObject = null;
            m_UxmlTraitAttributes = s_EmptyAttributeList;
            serializedRootPath = null;
            if (m_TempSerializedData != null)
                m_TempSerializedData.serializedData = null;

            if (m_CurrentElement != null)
            {
                m_SerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(m_CurrentElement.fullTypeName);
                if (m_SerializedDataDescription != null)
                {
                    if (m_CurrentUxmlElement == null)
                    {
                        // This is a temp element. We can not modify it but we should show its values in the inspector.
                        if (m_TempSerializedData == null)
                        {
                            m_TempSerializedData = ScriptableObject.CreateInstance<TempSerializedData>();
                            m_TempSerializedData.hideFlags = HideFlags.NotEditable;
                        }

                        m_TempSerializedData.serializedData = m_SerializedDataDescription.CreateSerializedData();
                        m_SerializedDataDescription.SyncSerializedData(m_CurrentElement, m_TempSerializedData.serializedData);
                        serializedRootPath = s_TempSerializedRootPath;
                        m_CurrentElementSerializedObject = new SerializedObject(m_TempSerializedData);
                        m_BoundValuesSerializedObject = new SerializedObject(m_TempSerializedData);
                    }
                    else
                    {
                        if (m_CurrentUxmlElement.serializedData == null)
                        {
                            m_CurrentUxmlElement.serializedData = m_SerializedDataDescription.CreateSerializedData();
                            m_CurrentUxmlElement.serializedData.uxmlAssetId = m_CurrentUxmlElement.id;
                            m_SerializedDataDescription.SyncSerializedData(m_CurrentElement, m_CurrentUxmlElement.serializedData);
                        }
                        else
                        {
                            // We treat the serialized data as the source of truth.
                            // There are times when we may need to resync, such as when an undo/redo was performed.
                            CallDeserializeOnElement();
                        }

                        m_UxmlDocument.hideFlags = HideFlags.DontUnloadUnusedAsset;

                        var isTemplateInstance = false;
                        int index = -1;
                        for (int i = 0; i < m_UxmlDocument.visualElementAssets.Count; ++i)
                        {
                            if (m_UxmlDocument.visualElementAssets[i].id == m_CurrentUxmlElement.id)
                            {
                                index = i;
                                break;
                            }
                        }

                        if (index == -1)
                        {
                            for (int i = 0; i < m_UxmlDocument.templateAssets.Count; ++i)
                            {
                                if (m_UxmlDocument.templateAssets[i].id == m_CurrentUxmlElement.id)
                                {
                                    index = i;
                                    isTemplateInstance = true;
                                    break;
                                }
                            }
                        }

                        var arrayPath = isTemplateInstance
                            ? nameof(VisualTreeAsset.m_TemplateAssets)
                            : nameof(VisualTreeAsset.m_VisualElementAssets);
                        serializedRootPath = $"{arrayPath}.Array.data[{index}].{nameof(VisualElementAsset.m_SerializedData)}.";
                        m_CurrentElementSerializedObject = new SerializedObject(m_UxmlDocument);

                        m_SerializedDataTreeAssetBindingsCopy = m_UxmlDocument.DeepCopy(false);
                        m_BoundValuesSerializedObject = new SerializedObject(m_SerializedDataTreeAssetBindingsCopy);
                    }
                }
                m_UxmlTraitAttributes = m_CurrentElement.GetAttributeDescriptions(true);
            }

            callInitOnValueChange = currentFieldSource == AttributeFieldSource.UxmlTraits;
        }

        public void SetBoundValue(VisualElement fieldElement, object value)
        {
            var dataField = fieldElement as UxmlSerializedDataAttributeField ?? fieldElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            var serializedAttribute =
                dataField.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
            var property = m_BoundValuesSerializedObject.FindProperty(serializedRootPath + serializedAttribute.serializedField.Name);

            property.boxedValue = value;
            m_BoundValuesSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal void RemoveBindingFromSerializedData(string property)
        {
            var serializedPath = serializedRootPath + "bindings";
            var serializedProperty = m_CurrentElementSerializedObject.FindProperty(serializedPath);

            for (var i = 0; i < serializedProperty.arraySize; i++)
            {
                var p = serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("property").stringValue;
                if (p == property)
                {
                    RemoveUxmlObjectInstance(serializedProperty, i);
                    break;
                }
            }
        }

        internal static string GetSerializedDataRoot(string path)
        {
            // Extract the root path, it will look like:
            // "m_VisualElementAssets.Array.data[x].m_SerializedData"
            var searchIndex = $"{nameof(VisualTreeAsset.m_VisualElementAssets)}.Array.data[".Length;
            var endIndex = path.IndexOf(']', searchIndex) + nameof(VisualElementAsset.m_SerializedData).Length + 1;
            return path.Substring(0, endIndex + 1);
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

            if (m_CurrentElement == null || attributes.Count == 0)
                return;

            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                GenerateUxmlTraitsAttributeFields();
            }
            else
            {
                GenerateSerializedAttributeFields();
            }
        }

        protected virtual void GenerateUxmlTraitsAttributeFields()
        {
            foreach (var attribute in m_UxmlTraitAttributes)
            {
                if (attribute == null || attribute.name == null || IsAttributeIgnored(attribute))
                    continue;

                CreateTraitsAttributeRow(attribute);
            }
        }

        /// <summary>
        /// Generates fields from the uxml attributes.
        /// </summary>
        protected virtual void GenerateSerializedAttributeFields()
        {
            // UxmlSerializedData
            var root = new UxmlAssetSerializedDataRoot { dataDescription = m_SerializedDataDescription, rootPath = serializedRootPath };
            fieldsContainer.Add(root);
            GenerateSerializedAttributeFields(m_SerializedDataDescription, root);
        }

        protected void GenerateSerializedAttributeFields(UxmlSerializedDataDescription dataDescription, UxmlAssetSerializedDataRoot parent)
        {
            foreach (var desc in dataDescription.serializedAttributes)
            {
                fieldsContainer.AddToClassList(InspectorElement.ussClassName);
                if (desc != null && desc.serializedField.GetCustomAttribute<HideInInspector>() == null)
                {
                    CreateSerializedAttributeRow(desc, $"{parent.rootPath}{desc.serializedField.Name}", parent);
                }
            }
        }

        /// <summary>
        /// Indicates whether the specified uxml attribute should be ignored.
        /// </summary>
        /// <param name="attribute">The attribute to evaluate.</param>
        /// <returns></returns>
        protected virtual bool IsAttributeIgnored(UxmlAttributeDescription attribute)
        {
            if (m_CurrentElement != null)
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
            return GetRemapAttributeNameToCSProperty(m_CurrentElement, attributeName);
        }

        internal string GetRemapCSPropertyToAttributeName(string CSProperty)
        {
            if (m_CurrentElement != null)
            {
                if (m_CurrentElement is ObjectField && CSProperty == "objectType")
                    return "type";
                else if (CSProperty == "isReadonly")
                    return "readOnly";
            }

            return BuilderNameUtilities.ConvertCamelToDash(CSProperty);
        }

        object GetAttributeValueNotMatchingCSPropertyName(string attributeName)
        {
            if (m_CurrentElement is ScrollView scrollView)
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
            else if (m_CurrentElement is ListView listView)
            {
                if (attributeName == "horizontal-scrolling")
                    return listView.horizontalScrollingEnabled;
            }
            else if (m_CurrentElement is BoundsField boundsField)
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
            else if (m_CurrentElement is BoundsIntField boundsIntField)
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
            else if (m_CurrentElement is RectField rectField)
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
            else if (m_CurrentElement is Vector2Field vec2Field)
            {
                if (attributeName == "x")
                    return vec2Field.value.x;
                else if (attributeName == "y")
                    return vec2Field.value.y;
            }
            else if (m_CurrentElement is Vector3Field vec3Field)
            {
                if (attributeName == "x")
                    return vec3Field.value.x;
                else if (attributeName == "y")
                    return vec3Field.value.y;
                else if (attributeName == "z")
                    return vec3Field.value.z;
            }
            else if (m_CurrentElement is Vector4Field vec4Field)
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
            else if (m_CurrentElement is Vector2IntField vec2IntField)
            {
                if (attributeName == "x")
                    return vec2IntField.value.x;
                else if (attributeName == "y")
                    return vec2IntField.value.y;
            }
            else if (m_CurrentElement is Vector3IntField vec3IntField)
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

        internal static VisualElement GetRootFieldElement(VisualElement visualElement)
        {
            if (visualElement == null)
                return null;

            var dataField = visualElement as UxmlSerializedDataAttributeField ?? visualElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            return dataField ?? visualElement;
        }

        static string GetAttributeName(VisualElement visualElement)
        {
            var desc = visualElement.GetLinkedAttributeDescription();
            return desc != null ? desc.name : ((IBindable)visualElement).bindingPath;
        }

        string GetBindingPropertyName(VisualElement visualElement)
        {
            // UxmlSerializedFields have a UxmlSerializedDataAttributeField as the parent
            var dataField = visualElement as UxmlSerializedDataAttributeField ?? visualElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            if (dataField != null)
            {
                var serializedAttribute = dataField.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                return serializedAttribute.serializedField.Name;
            }

            var desc = visualElement.GetLinkedAttributeDescription();
            return GetRemapAttributeNameToCSProperty(desc.name);
        }

        internal static BuilderStyleRow GetLinkedStyleRow(VisualElement visualElement)
        {
            return GetRootFieldElement(visualElement).GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
        }

        internal void UndoRecordDocument(string reason)
        {
            if (undoEnabled)
            {
                Undo.RegisterCompleteObjectUndo(m_UxmlDocument, reason);
            }
        }

        IEnumerable<VisualElement> GetAttributeFields()
        {
            if (currentFieldSource == AttributeFieldSource.UxmlSerializedData)
                return fieldsContainer.Query<UxmlSerializedDataAttributeField>().Where(ve => ve.HasLinkedAttributeDescription()).Build().ToList();
            return fieldsContainer.Query<BindableElement>().Where(e => !string.IsNullOrEmpty(e.bindingPath)).ToList();
        }

        /// <summary>
        /// Creates a row in the fields container for the specified attribute using the UxmlTraits system.
        /// </summary>
        /// <param name="attribute">The target attribute</param>
        /// <param name="parent">The parent where to add the row</param>
        /// <returns></returns>
        protected virtual BuilderStyleRow CreateTraitsAttributeRow(UxmlAttributeDescription attribute, VisualElement parent = null)
        {
            var fieldElement = CreateTraitsAttributeField(attribute);

            parent ??= fieldsContainer;

            // Create row.
            var styleRow = new BuilderStyleRow();

            styleRow.AddToClassList($"{s_AttributeFieldRowUssClassName}-{attribute.name}");
            styleRow.Add(fieldElement);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            // Setup field binding path.
            fieldElement.bindingPath = attribute.name;

            SetupStyleRow(styleRow, fieldElement, attribute);

            return styleRow;
        }

        VisualElement CreateUxmlObjectAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath)
        {
            var property = m_CurrentElementSerializedObject.FindProperty(propertyPath);
            var labelText = BuilderNameUtilities.ConvertDashToHuman(attribute.name);

            if (typeof(IList).IsAssignableFrom(attribute.type))
            {
                var listView = new ListView
                {
                    bindingPath = propertyPath,
                    virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    headerTitle = labelText,
                    showAddRemoveFooter = true,
                    showFoldoutHeader = true,
                    showBorder = true,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                    showBoundCollectionSize = false,
                    bindItem = (ve, i) =>
                    {
                        property.serializedObject.UpdateIfRequiredOrScript();

                        ve.Clear();
                        var item = property.GetArrayElementAtIndex(i);
                        var instance = item.boxedValue;
                        if (instance != null)
                        {
                            var desc = UxmlSerializedDataRegistry.GetDescription(instance.GetType().DeclaringType.FullName);
                            var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = item.propertyPath + "." };
                            ve.Add(root);

                            GenerateSerializedAttributeFields(desc, root);
                            ve.Bind(m_CurrentElementSerializedObject);
                        }
                    },
                    makeItem = () => new VisualElement(),
                    overridingAddButtonBehavior = (bv, btn) =>
                    {
                        ShowAddUxmlObjectMenu(btn, attribute, t =>
                        {
                            AddUxmlObjectInstance(property, t);
                        });
                    }
                };
                listView.onRemove += l => RemoveUxmlObjectInstance(property, l.selectedIndex);
                listView.bindingPath = propertyPath;
                listView.itemIndexChanged += (a, b) => MoveUxmlObjectInstances(property, attribute as UxmlSerializedUxmlObjectAttributeDescription, a, b);
                return listView;
            }

            var foldout = new Foldout { text = labelText };
            foldout.TrackPropertyValue(property, p => UpdateUxmlObjectReferenceFieldAddRemoveButtons(p, attribute, foldout, true));
            UpdateUxmlObjectReferenceFieldAddRemoveButtons(property, attribute, foldout, false);
            return foldout;
        }

        void UpdateUxmlObjectReferenceFieldAddRemoveButtons(SerializedProperty property, UxmlSerializedAttributeDescription attribute, Foldout field, bool bind = false)
        {
            property.serializedObject.UpdateIfRequiredOrScript();
            const string buttonName = "uxml-button";

            var previousType = field.GetProperty("previousType") as string;

            // Only update if the actual instance type changed
            if (previousType == property.managedReferenceFullTypename)
                return;

            field.SetProperty("previousType", property.managedReferenceFullTypename);

            property = property.Copy();
            field.Clear();
            field.Q(buttonName)?.RemoveFromHierarchy();
            var serializedInstanced = property.managedReferenceValue;

            if (serializedInstanced != null)
            {
                var removeButton = new Button { name = buttonName, text = "Delete" };
                removeButton.clicked += () =>
                {
                    RemoveUxmlObjectInstance(property, 0);
                };
                field.Q<Toggle>().Add(removeButton);

                var desc = UxmlSerializedDataRegistry.GetDescription(serializedInstanced.GetType().DeclaringType.FullName);
                var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = property.propertyPath + "."};
                field.Add(root);
                GenerateSerializedAttributeFields(desc, root);
                if (bind)
                    root.Bind(m_CurrentElementSerializedObject);
            }
            else
            {
                var addButton = new Button { name = buttonName, text = "Add" };
                addButton.clicked += () =>
                {
                    ShowAddUxmlObjectMenu(addButton, attribute, t =>
                    {
                        AddUxmlObjectInstance(property, t);
                    });
                };
                field.Q<Toggle>().Add(addButton);
            }
        }

        void ShowAddUxmlObjectMenu(VisualElement element, UxmlSerializedAttributeDescription attribute, Action<Type> action)
        {
            if (attribute.uxmlObjectAcceptedTypes.Count == 1)
            {
                action(attribute.uxmlObjectAcceptedTypes[0]);
            }
            else if (attribute.uxmlObjectAcceptedTypes.Count > 1)
            {
                var menu = new GenericDropdownMenu();
                foreach (var type in attribute.uxmlObjectAcceptedTypes)
                {
                    var name = ObjectNames.NicifyVariableName(type.DeclaringType.Name);

                    menu.AddItem(name, false, () =>
                    {
                        action(type);
                    });
                }
                menu.DropDown(element.parent.worldBound, element, true);
            }
        }

        internal void AddUxmlObjectInstance(SerializedProperty property, Type serializedDataType)
        {
            var undoGroup = Undo.GetCurrentGroup();
            var desc = UxmlSerializedDataRegistry.GetDescription(serializedDataType.DeclaringType.FullName);
            var instance = desc.CreateDefaultSerializedData();

            if (property.isArray)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                var item = property.GetArrayElementAtIndex(property.arraySize - 1);
                item.managedReferenceValue = instance;
            }
            else
            {
                property.managedReferenceValue = instance;
            }

            property.serializedObject.ApplyModifiedProperties();
            Undo.IncrementCurrentGroup();
            CallDeserializeOnElement();

            SynchronizePath(property.propertyPath, true, out var _, out var _, out var _);
            Undo.CollapseUndoOperations(undoGroup);
            NotifyAttributesChanged();
        }

        internal void RemoveUxmlObjectInstance(SerializedProperty property, int index)
        {
            var undoGroup = Undo.GetCurrentGroup();

            SynchronizePath(property.propertyPath, true, out var uxmlAsset, out var _, out var _);
            Undo.IncrementCurrentGroup();

            if (property.isArray)
            {
                if (property.arraySize == 0)
                    return;

                index = index == -1 ? property.arraySize - 1 : index;
                property.DeleteArrayElementAtIndex(index);
            }
            else
            {
                if (property.managedReferenceValue == null)
                    return;

                property.managedReferenceValue = null;
            }

            property.serializedObject.ApplyModifiedProperties();
            Undo.IncrementCurrentGroup();
            UndoRecordDocument(BuilderConstants.ModifyUxmlObject);

            if (property.isArray)
            {
                if (s_UxmlAssets.Count > index)
                    m_UxmlDocument.RemoveUxmlObject(s_UxmlAssets[index].id);
            }
            else
            {
                if (uxmlAsset != null)
                    m_UxmlDocument.RemoveUxmlObject(((UxmlObjectAsset)uxmlAsset).id);
            }
            CallDeserializeOnElement();
            Undo.CollapseUndoOperations(undoGroup);

            // We need to force an update because we made changes to the asset
            property.serializedObject.UpdateIfRequiredOrScript();

            NotifyAttributesChanged();
        }

        internal void MoveUxmlObjectInstances(SerializedProperty property, UxmlSerializedUxmlObjectAttributeDescription attributeDescription, int src, int dst)
        {
            var undoGroup = Undo.GetCurrentGroup();
            SynchronizePath(property.propertyPath, true, out var uxmlAsset, out var _, out var _);
            Undo.IncrementCurrentGroup();
            UndoRecordDocument(BuilderConstants.ModifyUxmlObject);

            m_UxmlDocument.MoveUxmlObject(uxmlAsset as UxmlAsset, attributeDescription.rootName, src, dst);
            CallDeserializeOnElement();
            Undo.CollapseUndoOperations(undoGroup);

            NotifyAttributesChanged();
        }

        /// <summary>
        /// Creates a row in the fields container for the specified UxmlSerializedData attribute.
        /// </summary>
        protected virtual BuilderStyleRow CreateSerializedAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath, VisualElement parent = null)
        {
            parent ??= fieldsContainer;
            var fieldElement = new UxmlSerializedDataAttributeField();

            if (attribute.isUxmlObject)
            {
                var uxmlObjectField = CreateUxmlObjectAttributeRow(attribute, propertyPath);
                uxmlObjectField.Bind(m_CurrentElementSerializedObject);
                fieldElement.Add(uxmlObjectField);
            }
            else
            {
                var propertyField = new PropertyField
                {
                    name = builderSerializedPropertyFieldName,
                    bindingPath = propertyPath,
                    label = BuilderNameUtilities.ConvertDashToHuman(attribute.name)
                };

                var boundValuePropertyField = new PropertyField
                {
                    name = builderBoundPropertyFieldName,
                    bindingPath = propertyPath,
                    label = BuilderNameUtilities.ConvertDashToHuman(attribute.name)
                };

                void TooltipCallback(TooltipEvent e) => OnTooltipEvent(e, propertyField, attribute);
                propertyField.RegisterCallback<TooltipEvent>(TooltipCallback, TrickleDown.TrickleDown);
                boundValuePropertyField.RegisterCallback<TooltipEvent>(TooltipCallback, TrickleDown.TrickleDown);

                boundValuePropertyField.style.display = DisplayStyle.None;

                // We only care about changes when not in readOnly mode.
                if (!readOnly)
                {
                    propertyField.RegisterCallback<SerializedPropertyBindEvent>(OnSerializedPropertyBindEvent);
                    boundValuePropertyField.Bind(m_BoundValuesSerializedObject);
                }

                fieldElement.Add(propertyField);
                fieldElement.Add(boundValuePropertyField);

                propertyField.Bind(m_CurrentElementSerializedObject);
            }

            // Create row.
            var styleRow = new BuilderStyleRow();

            styleRow.AddToClassList($"{s_AttributeFieldRowUssClassName}-{propertyPath}");
            styleRow.Add(fieldElement);

            // Ensure the row is added to the inspector hierarchy before refreshing
            parent.Add(styleRow);

            SetupStyleRow(styleRow, fieldElement, attribute);

            return styleRow;
        }

        protected void SetupStyleRow(BuilderStyleRow styleRow, VisualElement fieldElement, UxmlAttributeDescription attribute)
        {
            // Link the PropertyField to the BuilderStyleRow.
            fieldElement.SetContainingRow(styleRow);
            styleRow.AddLinkedFieldElement(fieldElement);

            // Link the PropertyField to the UxmlSerializedAttributeDescription.
            fieldElement.SetLinkedAttributeDescription(attribute);

            // Set initial value.
            UpdateAttributeField(fieldElement);

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator((evt) => BuildAttributeFieldContextualMenu(evt.menu, styleRow)));

            if (fieldElement.GetFieldStatusIndicator() != null)
            {
                fieldElement.GetFieldStatusIndicator().populateMenuItems =
                    (menu) => BuildAttributeFieldContextualMenu(menu, styleRow);
            }
        }

        void OnTooltipEvent(TooltipEvent e, PropertyField propertyField, UxmlSerializedAttributeDescription attribute)
        {
            // Only show tooltip on labels
            if (e.target is Label)
            {
                var tooltip = attribute.serializedField.GetCustomAttribute<TooltipAttribute>();
                var valueInfo = GetValueInfo(propertyField);

                e.tooltip = BuilderInspector.GetFieldTooltip(propertyField, valueInfo, tooltip?.tooltip, false);
            }
            else
            {
                e.tooltip = null;
            }

            e.rect = propertyField.GetTooltipRect();
            e.StopPropagation();
        }

        protected void OnSerializedPropertyBindEvent(SerializedPropertyBindEvent evt)
        {
            var target = (VisualElement)evt.target;

            // Unregister in the event of a rebind
            target.UnregisterCallback<SerializedPropertyChangeEvent>(OnPropertyFieldValueChange);

            // When a propertyfield is first bound it sends a change event, we dont want this event so
            // we need to wait until binding is completed before we register for change events.
            EditorApplication.delayCall += () =>
            {
                target.RegisterCallback<SerializedPropertyChangeEvent>(OnPropertyFieldValueChange);

                // Remove the binding info so that the RightClickFieldMenuEvent does not use the SerializedObject menu instead.
                var baseField = target.Q<PropertyField>(builderSerializedPropertyFieldName)?.Q<BindableElement>();
                if (baseField != null)
                    baseField.userData = null;
            };
        }

        /// <summary>
        /// Creates a field from the specified attribute using the UxmlTraits system.
        /// </summary>
        /// <param name="attribute">The target attribute</param>
        /// <returns></returns>
        protected virtual BindableElement CreateTraitsAttributeField(UxmlAttributeDescription attribute)
        {
            var attributesOwner = m_CurrentElement;
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
            var attributesOwner = m_CurrentElement;
            if (attribute is UxmlSerializedAttributeDescription uxmlSerializedAttribute)
            {
                uxmlSerializedAttribute.TryGetValueFromObject(attributesOwner, out var value);
                return value;
            }
            else
            {
                var objType = attributesOwner.GetType();
                var csPropertyName = GetRemapAttributeNameToCSProperty(attribute.name);
                var fieldInfo = objType.GetProperty(csPropertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                return fieldInfo == null ? GetAttributeValueNotMatchingCSPropertyName(attribute.name) : fieldInfo.GetValue(attributesOwner, null);
            }
        }

        internal bool SynchronizePath(string propertyPath, bool changeUxmlAssets, out object uxmlAsset, out object serializedData, out object attributeOwner)
        {
            var pathParts = propertyPath.Substring(serializedRootPath.Length).Split('.');

            var undoCreated = false;
            UxmlSerializedDataDescription currentDataDescription;
            object currentUxmlSerializedData = uxmlSerializedData;
            object currentAttributesOwner = m_CurrentElement;
            var currentAttributesUxmlOwner = attributesUxmlOwner;

            for (int i = 0; i < pathParts.Length; ++i)
            {
                if (currentAttributesOwner == null)
                {
                    uxmlAsset = currentAttributesUxmlOwner;
                    serializedData = currentUxmlSerializedData;
                    attributeOwner = currentAttributesOwner;
                    return false;
                }

                var attributeOwnerList = currentAttributesOwner as IList;
                var uxmlSerializedDataList = currentUxmlSerializedData as IList;

                // Is the current value a list?
                if (attributeOwnerList != null && uxmlSerializedDataList != null)
                {
                    var dataPath = pathParts[i + 1];
                    var arrayItemIndexStart = dataPath.IndexOf('[') + 1;
                    var arrayItemIndexEnd = dataPath.IndexOf(']');
                    var indexString = dataPath.Substring(arrayItemIndexStart, arrayItemIndexEnd - arrayItemIndexStart);
                    var listIndex = int.Parse(indexString);

                    currentAttributesUxmlOwner = s_UxmlAssets[listIndex];
                    currentAttributesOwner = attributeOwnerList[listIndex];
                    currentUxmlSerializedData = uxmlSerializedDataList[listIndex];

                    i += 1;
                    continue;
                }
                else
                {
                    currentDataDescription = UxmlSerializedDataRegistry.GetDescription(currentAttributesOwner.GetType().FullName);
                }

                var name = pathParts[i];
                var attribute = currentDataDescription.FindAttributeWithPropertyName(name) as UxmlSerializedUxmlObjectAttributeDescription;
                if (attribute == null)
                    break;

                attribute.TryGetValueFromObject(currentAttributesOwner, out currentAttributesOwner);
                currentUxmlSerializedData = attribute.GetSerializedValue(currentUxmlSerializedData);

                attributeOwnerList = currentAttributesOwner as IList;
                uxmlSerializedDataList = currentUxmlSerializedData as IList;
                var expectedListCount = currentUxmlSerializedData == null ? 0 : 1;

                // Sync lists
                if (attributeOwnerList != null && uxmlSerializedDataList != null)
                {
                    expectedListCount = uxmlSerializedDataList.Count;

                    // Add Attribute owner instances
                    if (attributeOwnerList.Count < expectedListCount)
                    {
                        for (int j = attributeOwnerList.Count; j < expectedListCount; ++j)
                        {
                            var itemSerializedData = uxmlSerializedDataList[j] as UxmlSerializedData;
                            var instance = itemSerializedData.CreateInstance();
                            itemSerializedData.Deserialize(instance);
                            attributeOwnerList.Add(instance);
                        }
                    }
                    // Remove Attribute owner instances
                    else if (attributeOwnerList.Count > expectedListCount)
                    {
                        var valuesToRemove = attributeOwnerList.Count - expectedListCount;
                        for (int j = 0; j < valuesToRemove; ++j)
                        {
                            attributeOwnerList.RemoveAt(attributeOwnerList.Count - 1);
                        }
                    }
                }

                s_UxmlAssets.Clear();
                m_UxmlDocument.CollectUxmlObjectAssets(currentAttributesUxmlOwner, attribute.rootName, s_UxmlAssets);

                if (s_UxmlAssets.Count != expectedListCount)
                {
                    if (!changeUxmlAssets)
                    {
                        uxmlAsset = currentAttributesUxmlOwner;
                        serializedData = currentUxmlSerializedData;
                        attributeOwner = currentAttributesOwner;
                        return false;
                    }

                    // Sync UxmlAsset list
                    if (!undoCreated)
                    {
                        undoCreated = true;
                        UndoRecordDocument(BuilderConstants.ModifyUxmlObject);
                    }

                    // Add UxmlObjectAssets
                    if (s_UxmlAssets.Count < expectedListCount)
                    {
                        for (int j = s_UxmlAssets.Count; j < expectedListCount; ++j)
                        {
                            var fullTypeName = attributeOwnerList != null ? attributeOwnerList[j].GetType().FullName : currentAttributesOwner.GetType().FullName;
                            var asset = m_UxmlDocument.AddUxmlObject(currentAttributesUxmlOwner, attribute.rootName, fullTypeName);

                            // Assign the new asset id to the serialized data
                            var sd = uxmlSerializedDataList != null ? (UxmlSerializedData)uxmlSerializedDataList[j] : (UxmlSerializedData)currentUxmlSerializedData;
                            sd.uxmlAssetId = asset.id;

                            s_UxmlAssets.Add(asset);
                        }
                    }
                    // Remove UxmlObjectAssets
                    else if (s_UxmlAssets.Count > expectedListCount)
                    {
                        var valuesToRemove = s_UxmlAssets.Count - expectedListCount;

                        int j = 0;
                        while (valuesToRemove > 0 && j < s_UxmlAssets.Count)
                        {
                            var asset = s_UxmlAssets[j];
                            var assetFound = false;

                            // Check if this asset still exists in the serialized data.
                            for (int k = 0; k < uxmlSerializedDataList.Count; ++k)
                            {
                                var sd = uxmlSerializedDataList != null ? (UxmlSerializedData)uxmlSerializedDataList[k] : (UxmlSerializedData)currentUxmlSerializedData;
                                if (sd.uxmlAssetId == asset.id)
                                {
                                    // This asset is still used, skip it.
                                    assetFound = true;
                                    break;
                                }
                            }

                            if (!assetFound)
                            {
                                // Rewove it
                                m_UxmlDocument.RemoveUxmlObject(asset.id);
                                s_UxmlAssets.RemoveAt(j);
                                valuesToRemove--;
                            }

                            j++;
                        }
                    }
                }

                if (!attribute.isList)
                    currentAttributesUxmlOwner = currentUxmlSerializedData == null ? null : s_UxmlAssets[0];
            }

            uxmlAsset = currentAttributesUxmlOwner;
            serializedData = currentUxmlSerializedData;
            attributeOwner = currentAttributesOwner;
            return true;
        }

        void OnPropertyFieldValueChange(SerializedPropertyChangeEvent evt)
        {
            if (m_CurrentElement == null)
            {
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();

            var fieldElement = GetRootFieldElement(evt.target as VisualElement);
            var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;

            object currentAttributeOwner = m_CurrentElement;
            var currentAttributeUxmlOwner = attributesUxmlOwner;
            var currentUxmlSerializedData = uxmlSerializedData;

            if (fieldElement.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>()?.dataDescription.isUxmlObject == true)
            {
                SynchronizePath(evt.changedProperty.propertyPath, true, out var uxmlAsset, out var serializedData, out currentAttributeOwner);
                currentAttributeUxmlOwner = uxmlAsset as UxmlAsset;
                currentUxmlSerializedData = serializedData as UxmlSerializedData;
            }

            // We choose to disregard callbacks when the value remains unchanged,
            // which can occur during the initial binding of a field or during an Unset operation.
            description.TryGetValueFromObject(currentAttributeOwner, out var previousValue);
            var newValue = description.GetSerializedValue(currentUxmlSerializedData);

            // Unity serializes null values as default objects, so we need to do the same to compare.
            if (!description.isUxmlObject && previousValue == null && !typeof(Object).IsAssignableFrom(description.type) && description.type.GetConstructor(Type.EmptyTypes) != null)
                previousValue = Activator.CreateInstance(description.type);

            if (UxmlAttributeComparison.ObjectEquals(previousValue, newValue))
                return;

            // Apply changes to the whole element
            m_CurrentElementSerializedObject.ApplyModifiedProperties();

            CallDeserializeOnElement();

            // Now resync as its possible that the setters made changes during Deserialize, e.g clamping values.
            m_SerializedDataDescription.SyncSerializedData(m_CurrentElement, uxmlSerializedData);
            Undo.IncrementCurrentGroup();

            if (newValue == null || !UxmlAttributeConverter.TryConvertToString(newValue, m_UxmlDocument, out var stringValue))
                stringValue = newValue?.ToString();

            PostAttributeValueChange(fieldElement, stringValue, currentAttributeUxmlOwner);

            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        void UpdateAttributeField(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();

            if (attribute is not UxmlSerializedAttributeDescription)
            {
                var fieldValue = GetAttributeValue(attribute);

                if (fieldValue == null)
                {
                    if (m_CurrentElement is EnumField defaultEnumField &&
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

                    else if (m_CurrentElement is EnumFlagsField defaultEnumFlagsField &&
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
                    else if (!(fieldElement is ObjectField or BaseField<string>))
                    {
                        return;
                    }
                }

                if ((attribute.name.Equals("allow-add") || attribute.name.Equals("allow-remove")) &&
                    m_CurrentElement is BaseListView)
                {
                    var styleRow =
                        fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as VisualElement;
                    styleRow?.contentContainer.AddToClassList(BuilderConstants.InspectorListViewAllowAddRemoveFieldClassName);
                }

                UpdateAttributeField(fieldElement, attribute, fieldValue);
            }

            UpdateAttributeOverrideStyle(fieldElement);
            UpdateFieldStatus(fieldElement);
        }

        internal virtual void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();
            var row = fieldElement.GetContainingRow();

            if (attribute == null || row == null)
                return;

            var attributeIsOverriden = false;
            var fieldElements = row.GetLinkedFieldElements();
            foreach (var field in fieldElements)
            {
                attributeIsOverriden |= IsAttributeOverriden(field);
            }

            row.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, attributeIsOverriden);

            // We dont do this in UxmlSerialized data as it breaks UxmlObject overrides, each field is responsible for its own override state.
            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
            {
                row.Query<BindableElement>().ForEach(styleField =>
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);

                    if (attributeIsOverriden)
                    {
                        string bindingPath = null;
                        string attributeName = null;

                        if (attribute is UxmlSerializedAttributeDescription serializedAttributeDescription && styleField.bindingPath != null)
                        {
                            var pathParts = styleField.bindingPath.Split(".");
                            bindingPath = pathParts[^1];
                            attributeName = serializedAttributeDescription.serializedField.Name;
                        }
                        else
                        {
                            bindingPath = styleField.bindingPath;
                            attributeName = attribute.name;
                        }

                        if (attributeName == bindingPath)
                        {
                            styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                        }
                        else if (!string.IsNullOrEmpty(bindingPath) &&
                                 attributeName != bindingPath &&
                                 !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                        {
                            styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                        }
                    }
                    else
                    {
                        styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                        styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    }
                });
            }
        }

        /// <summary>
        /// Refreshes the value and status of the specified field.
        /// </summary>
        /// <param name="fieldElement">The field to refresh</param>
        /// <para name="attribute">The attribute related to the field</para>
        /// <param name="value">The new value</param>
        protected void UpdateAttributeField(VisualElement fieldElement, UxmlAttributeDescription attribute, object value)
        {
            var attributesOwner = m_CurrentElement;
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
            var attributeIsOverriden = IsAttributeOverriden(fieldElement);
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

            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
                BuilderInspector.UpdateFieldTooltip(fieldElement, valueInfo);
        }

        public void SendNotifyAttributesChanged() => NotifyAttributesChanged();

        /// <summary>
        /// Notifies that the list of attributes has changed.
        /// </summary>
        protected virtual void NotifyAttributesChanged()
        {
        }

        protected bool IsAttributeOverriden(VisualElement fieldElement)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();

            if (currentFieldSource == AttributeFieldSource.UxmlSerializedData)
            {
                if (readOnly)
                    return false;

                var rootElement = fieldElement?.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>();
                if (rootElement == null)
                {
                    Debug.LogError("[UI Builder] Serialization error. Root can't be found.");
                    return false;
                }

                if (SynchronizePath(rootElement.rootPath, false, out var uxmlAsset, out var _, out var attributeOwner))
                {
                    return IsAttributeOverriden(attributeOwner, uxmlAsset as UxmlAsset, attribute);
                }

                return false;
            }

            var attributesOwner = m_CurrentElement;
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
            if (attribute is UxmlSerializedAttributeDescription { isUxmlObject: true })
                return false;

            if (attributeOwner is VisualElement ve)
            {
                if (attributeUxmlOwner != null && attribute.name == "picking-mode")
                {
                    var veaAttributeValue = attributeUxmlOwner.GetAttributeValue(attribute.name);
                    var bindingProperty = GetRemapAttributeNameToCSProperty(attributeOwner, attribute.name);
                    var isBound = DataBindingUtility.TryGetBinding(ve, new PropertyPath(bindingProperty), out _);
                    if (isBound || veaAttributeValue != null &&
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
                else
                {
                    var template = BuilderAssetUtilities.GetVisualElementRootTemplate(ve);
                    var templateVta = template?.GetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                    var linkedOpenVta = ve.GetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                    var bindingProperty = GetRemapAttributeNameToCSProperty(attributeOwner, attribute.name);
                    if ((templateVta == null || templateVta == linkedOpenVta) && DataBindingUtility.TryGetBinding(ve, new PropertyPath(bindingProperty), out _))
                    {
                        return true;
                    }
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
        /// <param name="removeBinding">Whether the binding in this field is removed or not</param>
        void ResetAttributeFieldToDefault(VisualElement fieldElement, bool removeBinding)
        {
            var attribute = fieldElement.GetLinkedAttributeDescription();

            if (removeBinding)
            {
                // Remove bindings
                var bindingProperty = GetBindingPropertyName(fieldElement);
                var hasBinding = DataBindingUtility.TryGetBinding(m_CurrentElement, new PropertyPath(bindingProperty),
                    out _);

                if (hasBinding)
                {
                    m_CurrentElement.ClearBinding(bindingProperty);
                    m_UxmlDocument.RemoveBinding(m_CurrentElement.GetVisualElementAsset(), bindingProperty);
                }
            }

            ResetAttributeFieldToDefault(fieldElement, attribute);

            UpdateAttributeOverrideStyle(fieldElement);

            // Clear override.
            var styleRow = GetLinkedStyleRow(fieldElement);
            styleRow.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
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
                fieldFactory.ResetFieldValue(fieldElement, m_CurrentElement, uxmlDocument, attributesUxmlOwner, attribute);
            }
            else
            {
                var desc = attribute as UxmlSerializedAttributeDescription;

                desc.SetSerializedValue(uxmlSerializedData, desc.defaultValueClone);
                m_CurrentElementSerializedObject.UpdateIfRequiredOrScript();
                CallDeserializeOnElement();

                // Rebind to the new default value
                fieldElement.Bind(m_CurrentElementSerializedObject);
            }
        }

        protected virtual void BuildAttributeFieldContextualMenu(DropdownMenu menu, BuilderStyleRow styleRow)
        {
            var fields = styleRow.GetLinkedFieldElements();
            var fieldElement = fields[0]; // Assume there's only one field for default case.

            // Dont add menu items to the root of UxmlObjects, they conflict with the field menu items, e.g multiple "Unset" menu items would be added.
            if (fieldElement.GetLinkedAttributeDescription() is UxmlSerializedAttributeDescription desc && desc.isUxmlObject)
                return;

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                (a) => UnsetAttributeProperty(a.userData as VisualElement, true),
                action =>
                {
                    if (action.userData is not VisualElement field)
                        return DropdownMenuAction.Status.Disabled;

                    var attributeName = GetAttributeName(field);
                    var bindingProperty = GetBindingPropertyName(field);
                    var isAttributeOverrideAttribute =
                        m_IsInTemplateInstance
                        && BuilderAssetUtilities.HasAttributeOverrideInRootTemplate(m_CurrentElement, attributeName);
                    var canUnsetBinding = !m_IsInTemplateInstance && DataBindingUtility.TryGetBinding(m_CurrentElement, new PropertyPath(bindingProperty), out _);

                    // Check UxmlObjects
                    bool hasAttributeOverride = false;
                    if (currentFieldSource == AttributeFieldSource.UxmlSerializedData)
                    {
                        var root = field.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>();
                        if (SynchronizePath(root.rootPath, false, out var uxmlAsset, out var _, out var attributeOwner))
                        {
                            hasAttributeOverride = IsAttributeOverriden(attributeOwner, uxmlAsset as UxmlAsset, field.GetLinkedAttributeDescription());
                        }
                    }
                    else
                    {
                        hasAttributeOverride = attributesUxmlOwner?.HasAttribute(attributeName) == true;
                    }
                    return hasAttributeOverride || isAttributeOverrideAttribute || canUnsetBinding
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled;
                },
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    if (action.userData is not VisualElement)
                        return DropdownMenuAction.Status.Disabled;

                    if (IsAnyAttributeSet())
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                },
                fieldElement);
        }

        internal bool IsAnyAttributeSet()
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

                if (IsAttributeOverriden(m_CurrentElement, attribute))
                    return true;
            }

            if (attributesUxmlOwner != null)
            {
                // Do we have any UxmlObjects?
                var entry = m_UxmlDocument.GetUxmlObjectEntry(attributesUxmlOwner.id);
                if (entry.uxmlObjectAssets?.Count > 0)
                    return true;
            }

            return false;
        }

        internal void UnsetAllAttributes()
        {
            var undoGroup = Undo.GetCurrentGroup();
            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);
            var builder = Builder.ActiveWindow;

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

                var hierarchyView = builder.hierarchy.elementHierarchyView;
                var selectionId = hierarchyView.GetSelectedItemId();

                builder.OnEnableAfterAllSerialization();

                hierarchyView.SelectItemById(selectionId);
            }
            else
            {
                if (currentFieldSource == AttributeFieldSource.UxmlTraits)
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
                        ResetAttributeFieldToDefault(fieldElement, true);
                        UpdateFieldStatus(fieldElement);
                    }
                    CallInitOnElement();
                }
                else
                {
                    // Clear UxmlObjects
                    var entry = m_UxmlDocument.GetUxmlObjectEntry(attributesUxmlOwner.id);
                    if (entry.uxmlObjectAssets?.Count > 0)
                    {
                        // Make a copy as the list will be modified during remove.
                        using var _ = ListPool<int>.Get(out var uxmlObjectIds);
                        foreach (var uoa in entry.uxmlObjectAssets)
                        {
                            uxmlObjectIds.Add(uoa.id);
                        }
                        foreach (var id in uxmlObjectIds)
                        {
                            m_UxmlDocument.RemoveUxmlObject(id);
                        }
                    }

                    // Clear attribute overrides
                    foreach (var attribute in m_SerializedDataDescription.serializedAttributes)
                    {
                        if (attribute.isUxmlObject)
                            continue;

                        m_CurrentUxmlElement.RemoveAttribute(attribute.name);
                    }

                    // Reset the whole UxmlSerializedData but keep the id.
                    var uxmlAssetId = m_CurrentUxmlElement.serializedData.uxmlAssetId;
                    m_CurrentUxmlElement.serializedData = m_SerializedDataDescription.CreateDefaultSerializedData();
                    m_CurrentUxmlElement.serializedData.uxmlAssetId = uxmlAssetId;
                    CallDeserializeOnElement();
                }

                // Notify of changes.
                NotifyAttributesChanged();
                Refresh();
                builder.inspector.headerSection.Refresh();
            }

            Undo.CollapseUndoOperations(undoGroup);
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

        internal void CallDeserializeOnElement()
        {
            if (currentFieldSource == AttributeFieldSource.UxmlTraits || uxmlSerializedData == null)
                return;

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(m_CurrentElement);
            uxmlSerializedData.Deserialize(m_CurrentElement);
        }

        internal void CallInitOnElement()
        {
            if (!callInitOnValueChange)
                return;

            var traits = GetCurrentElementTraits();

            if (traits == null)
                return;

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(m_CurrentElement);

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

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(m_CurrentElement);

            var context = new CreationContext(null, attributeOverrides, visualElement.visualTreeAssetSource, null);
            traits.Init(visualElement, vea, context);
        }

        public void UnsetAttributeProperty(VisualElement fieldElement, bool removeBinding)
        {
            var attributeName = GetAttributeName(fieldElement);

            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);

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

                UnsetEnumValue(attributeName, removeBinding);
            }
            else
            {
                var currentAttributesUxmlOwner = attributesUxmlOwner;
                var currentSerializedData = uxmlSerializedData;

                if (fieldElement.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>() is { } dataRoot && dataRoot.dataDescription.isUxmlObject)
                {
                    SynchronizePath(dataRoot.rootPath, false, out var uxmlOwner, out var serializedData, out var _);
                    currentAttributesUxmlOwner = uxmlOwner as UxmlAsset;
                    currentSerializedData = serializedData as UxmlSerializedData;
                }

                if (currentFieldSource == AttributeFieldSource.UxmlTraits)
                {
                    currentAttributesUxmlOwner.RemoveAttribute(attributeName);

                    // Reset UI value.
                    ResetAttributeFieldToDefault(fieldElement, removeBinding);

                    // Call Init();
                    CallInitOnElement();
                }
                else
                {
                    if (removeBinding)
                    {
                        var bindingProperty = GetBindingPropertyName(fieldElement);
                        RemoveBindingFromSerializedData(bindingProperty);
                    }

                    currentAttributesUxmlOwner.RemoveAttribute(attributeName);
                    var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                    description.SetSerializedValue(currentSerializedData, description.defaultValue);
                    CallDeserializeOnElement();
                }

                UnsetEnumValue(attributeName, removeBinding);

                NotifyAttributesChanged();
                Refresh();
            }
        }

        void UnsetEnumValue(string attributeName, bool removeBinding)
        {
            if (attributeName != "type")
                return;

            // When unsetting the type value for an enum field, we also need to clear the value field as well.
            if (m_CurrentElement is EnumField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField, removeBinding);
            }
            if (m_CurrentElement is EnumFlagsField)
            {
                // If the current value is not defined in the new enum type, we need to clear the property because
                // it will otherwise throw an exception.
                var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                UnsetAttributeProperty(valueField, removeBinding);
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
            if (currentFieldSource == AttributeFieldSource.UxmlSerializedData)
            {
                var prop = m_CurrentElementSerializedObject.FindProperty(serializedRootPath + field.bindingPath);
                prop.stringValue = evt.newValue;
                m_CurrentElementSerializedObject.ApplyModifiedProperties();
                CallDeserializeOnElement();
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
            else if (currentFieldSource == AttributeFieldSource.UxmlTraits &&
                attributeType.IsGenericType &&
                !attributeType.GetGenericArguments()[0].IsEnum &&
                attributeType.GetGenericArguments()[0] is Type)
            {
                if (m_CurrentElement is EnumField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField, true);
                    needRefresh = true;
                }
                else if (m_CurrentElement is EnumFlagsField)
                {
                    // If the current value is not defined in the new enum type, we need to clear the property because
                    // it will otherwise throw an exception.
                    var valueField = fieldsContainer.Query<EnumFlagsField>().Where(f => f.label == "Value").First();
                    UnsetAttributeProperty(valueField, true);
                    needRefresh = true;
                }

                // If the type of an object field changes, we have to refresh the inspector to ensure it has the correct type associated with it.
                if (m_CurrentElement is ObjectField && attribute.name == "type")
                {
                    needRefresh = true;
                }
            }

            PostAttributeValueChange(field, uxmlValue);

            if (needRefresh)
                Refresh();
        }

        void PostAttributeValueChange(VisualElement field, string value, UxmlAsset uxmlAsset = null)
        {
            if (field == null)
                return;

            var attributeName = GetAttributeName(field);

            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);

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
                uxmlAsset ??= attributesUxmlOwner;
                uxmlAsset.SetAttribute(attributeName, value);

                // Call Init();
                CallInitOnElement();
            }

            // Mark field as overridden.
            UpdateAttributeOverrideStyle(field);

            // Notify of changes.
            NotifyAttributesChanged();

            var styleRow = GetLinkedStyleRow(field);
            if (styleRow != null)
                UpdateFieldStatus(field);
        }
    }
}
