// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Profiling;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
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
        static readonly string s_UxmlButtonUssClassName = "unity-builder-uxml-object-button";
        public static readonly string builderSerializedPropertyFieldName = "unity-builder-serialized-property-field";
        static readonly string s_TempSerializedRootPath = nameof(TempSerializedData.serializedData) + ".";

        // Used in tests.
        // ReSharper disable MemberCanBePrivate.Global
        internal const string attributeOverrideMarkerName = "BuilderUxmlAttributesView.UpdateAttributeOverrideStyle";
        internal const string updateFieldStatusMarkerName = "BuilderUxmlAttributesView.UpdateFieldStatus";
        internal const string postAttributeValueChangedMarkerName = "BuilderUxmlAttributesView.PostAttributeValueChange";
        // ReSharper restore MemberCanBePrivate.Global

        static readonly ProfilerMarker k_UpdateAttributeOverrideStyleMarker = new (attributeOverrideMarkerName);
        static readonly ProfilerMarker k_UpdateFieldStatusMarker = new (updateFieldStatusMarkerName);
        static readonly ProfilerMarker k_PostAttributeValueChangedMarker = new (postAttributeValueChangedMarkerName);

        VisualTreeAsset m_UxmlDocument;
        VisualElement m_CurrentElement;
        VisualElementAsset m_CurrentUxmlElement;
        protected internal BuilderInspector inspector;

        // UxmlTraits
        List<UxmlAttributeDescription> m_UxmlTraitAttributes;
        static readonly List<UxmlAttributeDescription> s_EmptyAttributeList = new();

        // Sync path
        static readonly List<UxmlObjectAsset> s_UxmlAssets = new();
        static readonly object[] s_SingleUxmlSerializedData = new object[1];
        static readonly Dictionary<string, string[]> m_PathPartsCache = new();

        // UxmlSerializedData
        internal UxmlSerializedDataDescription m_SerializedDataDescription;
        internal SerializedObject m_CurrentElementSerializedObject;
        TempSerializedData m_TempSerializedData;
        int? m_CurrentUndoGroup;
        readonly List<(VisualElement target, SerializedProperty property)> m_BatchedChanges = new();
        bool m_DocumentUndoRecorded;
        static bool s_IsInsideUndoRedoUpdate;

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

        class CustomPropertyDrawerField : VisualElement
        {
        }

        // For when we need to view the serialized data from a temp visual element, such as one created by script.
        class TempSerializedData : ScriptableObject
        {
            [SerializeReference]
            public UxmlSerializedData serializedData;
        }

        internal class DisableUndoScope : IDisposable
        {
            BuilderUxmlAttributesView m_View;

            public DisableUndoScope(BuilderUxmlAttributesView view)
            {
                m_View = view;
                m_View.SetUndoEnabled(false);
            }

            public void Dispose()
            {
                m_View.SetUndoEnabled(true);
            }
        }

        internal enum AttributeFieldSource
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

        internal AttributeFieldSource currentFieldSource
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

        public BuilderUxmlAttributesView(BuilderInspector inspector = null)
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            BindingsStyleHelpers.HandleRightClickMenu = HandleRightClickMenu;
            this.inspector = inspector;
            SerializedObjectBindingContext.PostProcessTrackedPropertyChanges += ProcessBatchedChanges;
        }

        ~BuilderUxmlAttributesView()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            BindingsStyleHelpers.HandleRightClickMenu = null;
            SerializedObjectBindingContext.PostProcessTrackedPropertyChanges -= ProcessBatchedChanges;
        }

        void OnUndoRedoPerformed()
        {
            CallDeserializeOnElement();

            try
            {
                // We need to discard any change events that happen during the undo/redo update in order to avoid reapplying those changes.
                s_IsInsideUndoRedoUpdate = true;
                UIEventRegistration.UpdateSchedulers();
            }
            finally
            {
                s_IsInsideUndoRedoUpdate = false;
            }
        }

        bool HandleRightClickMenu(VisualElement ve)
        {
            while (ve != null)
            {
                if (ve is UxmlAssetSerializedDataRoot)
                    return true;
                ve = ve.parent;
            }
            return false;
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

            // Special case for toggle button groups.
            // We want to sync the length of the state with the number of buttons in the hierarchy.
            if (visualElement is ToggleButtonGroup group)
            {
                var stateProperty = m_CurrentElementSerializedObject.FindProperty(serializedRootPath + "value");
                var lengthProperty = stateProperty.FindPropertyRelative("m_Length");
                var length = lengthProperty.intValue;

                var buttonCount = group.Query<Button>().ToList().Count;
                if (buttonCount != length && buttonCount < ToggleButtonGroupState.maxLength)
                {
                    var value = group.value;
                    value.length = buttonCount;
                    group.SetValueWithoutNotify(value);

                    lengthProperty.intValue = buttonCount;
                    m_CurrentElementSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>
        /// Sets the specified sub object in the specified VisualElement as the owner of attributes to be edited.
        /// </summary>
        /// <param name="uxmlDocumentAsset">The uxml document being edited</param>
        /// <param name="visualElement">The VisualElement that owns the selected sub object</param>
        public void SetAttributesOwner(VisualTreeAsset uxmlDocumentAsset, VisualElement visualElement)
        {
            var uxmlAsset = visualElement.GetVisualElementAsset();
            m_UxmlDocument = uxmlDocumentAsset;
            m_CurrentUxmlElement = uxmlAsset;
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

                        m_UxmlDocument.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

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
                    }
                }
                m_UxmlTraitAttributes = m_CurrentElement.GetAttributeDescriptions(true);
            }

            callInitOnValueChange = currentFieldSource == AttributeFieldSource.UxmlTraits;
        }

        public void SetInlineValue(VisualElement fieldElement, string property)
        {
            if (serializedRootPath == null)
                return;

            var path = serializedRootPath + property;
            var result = SynchronizePath(path, false);
            var dataDescription = UxmlSerializedDataRegistry.GetDescription(result.attributeOwner.GetType().FullName);
            var attribute = dataDescription.FindAttributeWithPropertyName(property);
            if (attribute == null)
                return;

            var bindableElement = fieldElement?.Q<BindableElement>();
            var binding = bindableElement?.GetBinding(BindingExtensions.s_SerializedBindingId);
            if (binding is not SerializedObjectBindingBase bindingBase)
                return;

            var serializedProperty = m_CurrentElementSerializedObject.FindProperty(path);

            object value;
            var handler = ScriptAttributeUtility.GetHandler(serializedProperty);

            if (result.uxmlAsset.TryGetAttributeValue(attribute.name, out var uxmlValueString) &&
                UxmlAttributeConverter.TryConvertFromString(attribute.type, uxmlValueString, new CreationContext(m_UxmlDocument), out var uxmlValue))
            {
                value = uxmlValue;
            }
            else
            {
                value = attribute.defaultValueClone;
            }

            if (handler.hasPropertyDrawer)
            {
                serializedProperty.boxedValue = value;
                serializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var propField = fieldElement.Q<PropertyField>() ?? fieldElement.GetFirstAncestorOfType<PropertyField>();
                var context = SerializedObjectBindingContext.GetBindingContextFromElement(propField);
                context?.UpdateRevision();
            }
            else
            {
                bindingBase.SyncValueWithoutNotify(value);
            }
        }

        public void SetBoundValue(VisualElement fieldElement, object value)
        {
            var dataField = fieldElement as UxmlSerializedDataAttributeField ?? fieldElement.GetFirstAncestorOfType<UxmlSerializedDataAttributeField>();
            var serializedAttribute = dataField.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
            var property = m_CurrentElementSerializedObject.FindProperty(serializedRootPath + serializedAttribute.serializedField.Name);

            var bindableElement = fieldElement.Q<BindableElement>();
            var binding = bindableElement?.GetBinding(BindingExtensions.s_SerializedBindingId);
            if (binding is not SerializedObjectBindingBase bindingBase)
                return;

            var handler = ScriptAttributeUtility.GetHandler(property);
            if (handler.hasPropertyDrawer)
            {
                var previous = property.boxedValue;
                if (previous != value)
                {
                    property.boxedValue = value;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    var propField = fieldElement.Q<PropertyField>() ?? fieldElement.GetFirstAncestorOfType<PropertyField>();
                    var context = SerializedObjectBindingContext.GetBindingContextFromElement(propField);
                    context?.UpdateRevision();
                }
            }
            else
            {
                bindingBase.SyncValueWithoutNotify(value);
            }
        }

        void SetUndoEnabled(bool enableUndo)
        {
            undoEnabled = enableUndo;
        }

        internal void RemoveBindingFromSerializedData(VisualElement fieldElement, string property)
        {
            var serializedPath = serializedRootPath + "bindings";
            var bindingsSerializedProperty = m_CurrentElementSerializedObject.FindProperty(serializedPath);

            Undo.RegisterCompleteObjectUndo(bindingsSerializedProperty.m_SerializedObject.targetObject, GetUndoMessage(bindingsSerializedProperty));

            for (var i = 0; i < bindingsSerializedProperty.arraySize; i++)
            {
                var item = bindingsSerializedProperty.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("property").stringValue == property)
                {
                    if (fieldElement != null)
                        SetInlineValue(fieldElement, property);
                    bindingsSerializedProperty.DeleteArrayElementAtIndex(i);
                    bindingsSerializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    SyncUxmlObjectChanges(serializedPath);
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

        protected static string GetAttributeName(VisualElement visualElement)
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

            var name = visualElement.GetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName) as string;
            return name;
        }

        internal static BuilderStyleRow GetLinkedStyleRow(VisualElement visualElement)
        {
            return GetRootFieldElement(visualElement).GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
        }

        internal void UndoRecordDocument(string reason)
        {
            if (undoEnabled)
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(m_UxmlDocument, reason);
            }
        }

        IEnumerable<VisualElement> GetAttributeFields()
        {
            if (currentFieldSource == AttributeFieldSource.UxmlSerializedData)
                return fieldsContainer.Query<UxmlSerializedDataAttributeField>().Where(ve => ve.HasLinkedAttributeDescription()).Build();
            return fieldsContainer.Query<BindableElement>().Where(e => !string.IsNullOrEmpty(e.bindingPath)).Build();
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

        protected VisualElement CreateUxmlObjectAttributeRow(UxmlSerializedAttributeDescription attribute, string propertyPath)
        {
            var property = m_CurrentElementSerializedObject.FindProperty(propertyPath);
            var labelText = BuilderNameUtilities.ConvertDashToHuman(attribute.name);

            if (attribute.isList)
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
                    reorderable = true,
                    reorderMode = ListViewReorderMode.Animated,
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

                            CreateUxmlObjectField(item, desc, root);
                            ve.Bind(m_CurrentElementSerializedObject);
                        }
                    },
                    makeItem = () => new VisualElement(),
                    overridingAddButtonBehavior = (bv, btn) =>
                    {
                        ShowAddUxmlObjectMenu(btn, attribute, t =>
                        {
                            AddUxmlObjectToSerializedData(property, t);
                        });
                    },
                    onRemove = l =>
                    {
                        if (property.arraySize > 0)
                        {
                            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, GetUndoMessage(property));

                            int index = l.selectedIndex >= 0 ? l.selectedIndex : property.arraySize - 1;
                            property.DeleteArrayElementAtIndex(index);
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            SyncUxmlObjectChanges(property.propertyPath);
                        }
                    },
                };
                listView.bindingPath = propertyPath;
                listView.itemIndexChanged += (_, _) =>
                {
                    SyncUxmlObjectChanges(property.propertyPath);
                };
                return listView;
            }

            var foldout = new Foldout { text = labelText };
            foldout.TrackPropertyValue(property, p => UpdateUxmlObjectReferenceFieldAddRemoveButtons(p, attribute, foldout, true));
            UpdateUxmlObjectReferenceFieldAddRemoveButtons(property, attribute, foldout, false);
            return foldout;
        }

        protected void CreateUxmlObjectField(SerializedProperty serializedProperty, UxmlSerializedDataDescription dataDescription, UxmlAssetSerializedDataRoot root)
        {
            var handler = ScriptAttributeUtility.GetHandler(serializedProperty);
            if (handler.hasPropertyDrawer)
            {
                CreateCustomPropertyDrawerField(serializedProperty, root);
            }
            else
            {
                GenerateSerializedAttributeFields(dataDescription, root);
            }
        }

        protected void CreateCustomPropertyDrawerField(SerializedProperty serializedProperty, VisualElement root)
        {
            var drawerRoot = new CustomPropertyDrawerField();
            drawerRoot.AddManipulator(new ContextualMenuManipulator(BuildCustomPropertyDrawerMenu));
            root.Add(drawerRoot);

            var propertyField = new PropertyField { bindingPath = serializedProperty.propertyPath };
            drawerRoot.Add(propertyField);
            TrackCustomPropertyDrawerFields(drawerRoot, serializedProperty);

            // The hiearachy is not complete yet so we need to defer the update
            root.schedule.Execute(() => UpdateCustomPropertyDrawerAttributeOverrideStyle(drawerRoot));
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
                var removeButton = new Button { name = buttonName, classList = { s_UxmlButtonUssClassName }, text = "Delete" };
                removeButton.clicked += () =>
                {
                    AddUxmlObjectToSerializedData(property, null);
                };
                field.Q<Toggle>().Add(removeButton);

                var desc = UxmlSerializedDataRegistry.GetDescription(serializedInstanced.GetType().DeclaringType.FullName);
                var root = new UxmlAssetSerializedDataRoot { dataDescription = desc, rootPath = property.propertyPath + "." };
                field.Add(root);

                CreateUxmlObjectField(property, desc, root);
                if (bind)
                    field.Bind(m_CurrentElementSerializedObject);
            }
            else
            {
                var addButton = new Button { name = buttonName, classList = { s_UxmlButtonUssClassName }, text = "Add" };
                addButton.clicked += () =>
                {
                    ShowAddUxmlObjectMenu(addButton, attribute, t =>
                    {
                        AddUxmlObjectToSerializedData(property, t);
                    });
                };
                field.Q<Toggle>().Add(addButton);
            }
        }

        void TrackCustomPropertyDrawerListElements(VisualElement listField, SerializedProperty property)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                var item = property.GetArrayElementAtIndex(i);
                TrackCustomPropertyDrawerFields(listField, item);
            }
        }

        void TrackCustomPropertyDrawerFields(VisualElement uxmlObjectField, SerializedProperty property)
        {
            var instance = property.boxedValue;
            if (instance == null)
                return;

            var dataDescription = UxmlSerializedDataRegistry.GetDescription(instance.GetType().DeclaringType.FullName);
            var itemRoot = new UxmlAssetSerializedDataRoot
            {
                dataDescription = dataDescription,
                rootPath = property.propertyPath + ".",
                name = property.propertyPath
            };
            uxmlObjectField.Add(itemRoot);

            foreach (var desc in dataDescription.serializedAttributes)
            {
                var fieldElement = new UxmlSerializedDataAttributeField { name = desc.serializedField.Name };
                fieldElement.SetLinkedAttributeDescription(desc);

                itemRoot.Add(fieldElement);
                var p = property.FindPropertyRelative(desc.serializedField.Name);

                if (desc.isUxmlObject)
                {
                    // Track a UxmlObject field/list that we do not have control of.
                    fieldElement.TrackPropertyValue(p, OnTrackedUxmlObjectReferenceChanged);

                    if (p.isArray)
                        TrackCustomPropertyDrawerListElements(fieldElement, p);
                    else
                        TrackCustomPropertyDrawerFields(fieldElement, p);
                }
                else
                {
                    TrackElementPropertyValue(fieldElement, p);
                }
            }
        }

        void OnTrackedUxmlObjectReferenceChanged(object element, SerializedProperty property)
        {
            var fieldElement = element as VisualElement;
            fieldElement.Clear();

            if (property.isArray)
                TrackCustomPropertyDrawerListElements(fieldElement, property);
            else
                TrackCustomPropertyDrawerFields(fieldElement, property);

            SyncUxmlObjectChanges(property.propertyPath);
        }

        internal void AddUxmlObjectToSerializedData(SerializedProperty property, Type type)
        {
            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, GetUndoMessage(property));

            if (property.isArray)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property = property.GetArrayElementAtIndex(property.arraySize - 1);
            }
            property.managedReferenceValue = type != null ? UxmlSerializedDataCreator.CreateUxmlSerializedData(type.DeclaringType) : null;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            SyncUxmlObjectChanges(property.propertyPath);
        }

        internal void SyncUxmlObjectChanges(string propertyPath)
        {
            if (s_IsInsideUndoRedoUpdate)
                return;

            var undoGroup = GetCurrentUndoGroup();
            Undo.IncrementCurrentGroup();
            CallDeserializeOnElement();
            SynchronizePath(propertyPath, true);
            NotifyAttributesChanged();
            Undo.CollapseUndoOperations(undoGroup);
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

                void TooltipCallback(TooltipEvent e) => OnTooltipEvent(e, propertyField, attribute);
                propertyField.RegisterCallback<TooltipEvent>(TooltipCallback, TrickleDown.TrickleDown);

                // We only care about changes when not in readOnly mode.
                if (!readOnly)
                {
                    TrackElementPropertyValue(propertyField, propertyPath);
                }

                fieldElement.Add(propertyField);

                propertyField.Bind(m_CurrentElementSerializedObject);

                // Special case for ToggleButtonGroup
                if (m_CurrentElement is ToggleButtonGroup && attribute.name == nameof(ToggleButtonGroup.value))
                {
                    propertyField.RegisterCallback<SerializedPropertyBindEvent>(OnPropertyFieldBound);
                }
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

        void OnPropertyFieldBound(SerializedPropertyBindEvent evt)
        {
            if (m_CurrentElement is not ToggleButtonGroup groupElement)
                return;

            var propertyField = evt.elementTarget;
            var groupField = propertyField.Q<ToggleButtonGroup>();
            if (groupField == null)
                return;

            // Special case for toggle button groups.
            // We want to sync the length of the value with the number of buttons in the hierarchy, and we want to match the
            // allowMultipleSelection and allowEmptySelection attributes so that the value matches the state of the group.
            var obj = m_CurrentElementSerializedObject;
            var valueProperty = obj.FindProperty(serializedRootPath + nameof(ToggleButtonGroup.value));
            var multipleProperty = obj.FindProperty(serializedRootPath + nameof(ToggleButtonGroup.isMultipleSelection));
            var allowEmptyProperty = obj.FindProperty(serializedRootPath + nameof(ToggleButtonGroup.allowEmptySelection));

            groupField.isMultipleSelection = multipleProperty.boolValue;
            groupField.allowEmptySelection = allowEmptyProperty.boolValue;

            var fieldElement = GetRootFieldElement(propertyField);
            fieldElement.TrackPropertyValue(multipleProperty, p =>
            {
                groupField.isMultipleSelection = p.boolValue;
                groupElement.isMultipleSelection = p.boolValue;
                valueProperty.structValue = groupField.value;
                p.serializedObject.ApplyModifiedProperties();

                attributesUxmlOwner.SetAttribute("is-multiple-selection", p.boolValue.ToString().ToLower());
                attributesUxmlOwner.SetAttribute("value", groupField.value.ToString());
                PostAttributeValueChange(fieldElement, groupField.value.ToString(), attributesUxmlOwner);
            });
            fieldElement.TrackPropertyValue(allowEmptyProperty, p =>
            {
                groupField.allowEmptySelection = p.boolValue;
                groupElement.allowEmptySelection = p.boolValue;
                valueProperty.structValue = groupField.value;
                p.serializedObject.ApplyModifiedProperties();

                attributesUxmlOwner.SetAttribute("allow-empty-selection", p.boolValue.ToString().ToLower());
                attributesUxmlOwner.SetAttribute("value", groupField.value.ToString());
                PostAttributeValueChange(fieldElement, groupField.value.ToString(), attributesUxmlOwner);
            });
        }

        protected void SetupStyleRow(BuilderStyleRow styleRow, VisualElement fieldElement, UxmlAttributeDescription attribute)
        {
            // Link the PropertyField to the BuilderStyleRow.
            fieldElement.SetContainingRow(styleRow);
            styleRow.AddLinkedFieldElement(fieldElement);

            // Link the PropertyField to the UxmlSerializedAttributeDescription.
            fieldElement.SetLinkedAttributeDescription(attribute);

            // Save the property name.
            var propertyName = attribute.name;
            var bindingProperty = GetRemapAttributeNameToCSProperty(propertyName);
            fieldElement.SetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName, bindingProperty);

            // Set initial value.
            UpdateAttributeField(fieldElement);

            // Context menu.
            styleRow.AddManipulator(new ContextualMenuManipulator(BuildAttributeFieldContextualMenu));

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

        internal struct SynchronizePathResult
        {
            public bool success;
            public UxmlAsset uxmlAsset;
            public object serializedData;
            public UxmlSerializedDataDescription dataDescription;
            public UxmlSerializedAttributeDescription attributeDescription;
            public object attributeOwner;
        }

        /// <summary>
        /// Synchronizes the UXML serialized data to the current UXML asset and sub-UXML objects that are part of the path.
        /// __Note__: To synchronize the attribute owner when extracting, call <see cref="CallDeserializeOnElement"/>.
        /// </summary>
        /// <param name="propertyPath">The full serialized property path.</param>
        /// <param name="changeUxmlAssets">Whether to add missing UXML assets in the path.
        /// <returns></returns>
        internal SynchronizePathResult SynchronizePath(string propertyPath, bool changeUxmlAssets)
        {
            SynchronizePathResult result = default;

            if (string.IsNullOrEmpty(propertyPath))
                return result;

            // Cache the split so we don't have to do it every time.
            if (!m_PathPartsCache.TryGetValue(propertyPath, out var pathParts))
            {
                pathParts = propertyPath.Substring(serializedRootPath.Length).Split('.');
                m_PathPartsCache[propertyPath] = pathParts;
            }

            m_DocumentUndoRecorded = false;
            object currentUxmlSerializedData = uxmlSerializedData;
            var currentAttributesUxmlOwner = attributesUxmlOwner;
            result.attributeOwner = m_CurrentElement;

            for (int i = 0; i < pathParts.Length; ++i)
            {
                // Is the current value a list?
                if (currentUxmlSerializedData is IList serializedDataList)
                {
                    // Find the item index from the path and extract it.
                    var dataPath = pathParts[i + 1];
                    var arrayItemIndexStart = dataPath.IndexOf('[') + 1;
                    var arrayItemIndexEnd = dataPath.IndexOf(']');
                    var indexString = dataPath.Substring(arrayItemIndexStart, arrayItemIndexEnd - arrayItemIndexStart);
                    var listIndex = int.Parse(indexString);

                    currentAttributesUxmlOwner = s_UxmlAssets[listIndex];
                    currentUxmlSerializedData = serializedDataList[listIndex];

                    if (result.attributeOwner is IList attributeOwnerList && listIndex < attributeOwnerList.Count)
                    {
                        result.attributeOwner = attributeOwnerList[listIndex];
                    }
                    else
                    {
                        result.attributeOwner = null; // We could not extract the value
                    }

                    i += 1;
                    continue;
                }

                result.dataDescription = UxmlSerializedDataRegistry.GetDescription(currentUxmlSerializedData.GetType().DeclaringType.FullName);

                var name = pathParts[i];
                result.attributeDescription = result.dataDescription.FindAttributeWithPropertyName(name);
                var attributeObjectDescription = result.attributeDescription as UxmlSerializedUxmlObjectAttributeDescription;
                if (attributeObjectDescription == null)
                    break;

                result.attributeDescription.TryGetValueFromObject(result.attributeOwner, out result.attributeOwner);

                currentUxmlSerializedData = result.attributeDescription.GetSerializedValue(currentUxmlSerializedData);
                var uxmlSerializedDataList = currentUxmlSerializedData as IList;

                // If we are not syncing a list then its a single field but we still treat it as a list.
                if (uxmlSerializedDataList == null)
                {
                    s_SingleUxmlSerializedData[0] = currentUxmlSerializedData;
                    uxmlSerializedDataList = s_SingleUxmlSerializedData;
                }

                if (!SyncUxmlAssetsFromSerializedData(uxmlSerializedDataList, currentAttributesUxmlOwner, attributeObjectDescription, changeUxmlAssets))
                {
                    if (!changeUxmlAssets)
                    {
                        result.uxmlAsset = currentAttributesUxmlOwner;
                        result.serializedData = currentUxmlSerializedData;
                        result.success = false;
                        return result;
                    }
                }

                if (!attributeObjectDescription.isList)
                    currentAttributesUxmlOwner = currentUxmlSerializedData == null ? null : s_UxmlAssets[0];
            }

            // We need to update the serialized object if we made changes.
            if (changeUxmlAssets)
                m_CurrentElementSerializedObject.UpdateIfRequiredOrScript();

            result.uxmlAsset = currentAttributesUxmlOwner;
            result.serializedData = currentUxmlSerializedData;
            result.success = true;
            return result;
        }

        bool SyncUxmlAssetsFromSerializedData(IList uxmlSerializedData, UxmlAsset parentAsset,
            UxmlSerializedUxmlObjectAttributeDescription attributeDescription, bool canMakeChanges)
        {
            bool contentsChanged = false;

            s_UxmlAssets.Clear();
            using var listPool = ListPool<UxmlObjectAsset>.Get(out var collectedUxmlAssets);
            m_UxmlDocument.CollectUxmlObjectAssets(parentAsset, attributeDescription.rootName, collectedUxmlAssets);

            // Sync the list by checking each item is at the expected index and moving/adding items as needed.
            using var hashSetPool = HashSetPool<int>.Get(out var duplicateIds);
            for (int j = 0; j < uxmlSerializedData.Count; ++j)
            {
                var currentSerializedData = uxmlSerializedData[j] as UxmlSerializedData;

                if (currentSerializedData != null && currentSerializedData.uxmlAssetId != 0)
                {
                    // When a list element is copied it may also copy the id of the original element.
                    // If the id has already been used we clear it so a new one can be assigned.
                    if (duplicateIds.Contains(currentSerializedData.uxmlAssetId))
                        currentSerializedData.uxmlAssetId = 0;
                }

                // Find matching UxmlObjectAsset
                if (!ExtractOrCreateUxmlSerializedDataUxmlAsset(currentSerializedData, parentAsset, attributeDescription,
                    canMakeChanges, collectedUxmlAssets, out var foundUxmlAsset, j))
                {
                    if (!canMakeChanges)
                        return false;
                    contentsChanged = true;
                }

                duplicateIds.Add(foundUxmlAsset.id);
                s_UxmlAssets.Add(foundUxmlAsset);
            }

            // If we uxml assets remnaining then the serialized data must have been removed and we should do the same.
            for (int j = 0; j < collectedUxmlAssets.Count; ++j)
            {
                if (collectedUxmlAssets[j] == null)
                    continue;

                contentsChanged = true;
                RecordDocumentUndoOnce();

                // We need to do this to ensure that any dependencies are also removed.
                m_UxmlDocument.RemoveUxmlObject(collectedUxmlAssets[j].id);
            }

            if (contentsChanged)
                m_UxmlDocument.SetUxmlObjectAssets(parentAsset, attributeDescription.rootName, s_UxmlAssets);

            return true;
        }

        bool ExtractOrCreateUxmlSerializedDataUxmlAsset(UxmlSerializedData uxmlSerializedData, UxmlAsset parentAsset,
            UxmlSerializedUxmlObjectAttributeDescription attributeDescription, bool canMakeChanges,
            List<UxmlObjectAsset> uxmlObjectAssets, out UxmlObjectAsset uxmlAsset, int expectedIndex)
        {
            // If the asset id is 0 then we do not currently have a UxmlAsset for this serialized data
            if (uxmlSerializedData?.uxmlAssetId != 0)
            {
                // Check if the data is at the expected index.
                if (expectedIndex < uxmlObjectAssets.Count)
                {
                    if (uxmlObjectAssets[expectedIndex] != null &&
                        ((uxmlSerializedData == null && uxmlObjectAssets[expectedIndex]?.isNull == true) ||
                        uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[expectedIndex]?.id))
                    {
                        uxmlAsset = uxmlObjectAssets[expectedIndex];

                        // We dont remove the asset from the list as it will break the expected index but we do set it to null
                        uxmlObjectAssets[expectedIndex] = null;
                        return true;
                    }
                }

                if (!canMakeChanges)
                {
                    uxmlAsset = null;
                    return false;
                }

                RecordDocumentUndoOnce();

                // See if we can find it at another index
                for (int i = 0; i < uxmlObjectAssets.Count; ++i)
                {
                    if (uxmlObjectAssets[i] == null)
                        continue;

                    if ((uxmlSerializedData == null && uxmlObjectAssets[i].isNull) ||
                        uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[i].id)
                    {
                        uxmlAsset = uxmlObjectAssets[i];
                        uxmlObjectAssets[i] = null;
                        return false;
                    }
                }
            }

            if (!canMakeChanges)
            {
                uxmlAsset = null;
                return false;
            }

            RecordDocumentUndoOnce();

            // We could not find the asset so we need to create a new one.
            uxmlAsset = CreateUxmlObjectAsset(attributeDescription, uxmlSerializedData, parentAsset);

            return false;
        }

        /// <summary>
        /// Checks if any serialized data fields are not set to their default values. If any are not, apply those changes to the UXML asset.
        /// </summary>
        /// <param name="uxmlSerializedData">The serialized data to check for non-default values.</param>
        /// <param name="uxmlAsset">The asset to apply the uxml attributes to.</param>
        void SyncSerializedDataToNewUxmlAsset(UxmlSerializedData uxmlSerializedData, UxmlAsset uxmlAsset)
        {
            if (uxmlSerializedData == null)
                return;

            var description = UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);
            foreach (var attribute in description.serializedAttributes)
            {
                if (attribute.isUxmlObject)
                {
                    var attributeUxmlObjectDescription = attribute as UxmlSerializedUxmlObjectAttributeDescription;
                    if (attribute.isList)
                    {
                        // Extract the serialized data list
                        var serializedDataList = (IList)attribute.GetSerializedValue(uxmlSerializedData);
                        foreach (UxmlSerializedData serializedDataItem in serializedDataList)
                        {
                            CreateUxmlObjectAsset(attributeUxmlObjectDescription, serializedDataItem, uxmlAsset);
                        }
                    }
                    else
                    {
                        var serializedData = attribute.GetSerializedValue(uxmlSerializedData) as UxmlSerializedData;
                        CreateUxmlObjectAsset(attributeUxmlObjectDescription, serializedData, uxmlAsset);
                    }
                }
                else
                {
                    var attributeValue = attribute.GetSerializedValue(uxmlSerializedData);
                    if (!UxmlAttributeComparison.ObjectEquals(attributeValue, attribute.defaultValue))
                    {
                        if (attributeValue == null || !UxmlAttributeConverter.TryConvertToString(attributeValue, m_UxmlDocument, out var stringValue))
                            stringValue = attributeValue?.ToString();

                        using (new DisableUndoScope(this))
                        {
                            PostAttributeValueChange(attribute.name, stringValue, uxmlAsset);
                        }
                    }
                }
            }
        }

        UxmlObjectAsset CreateUxmlObjectAsset(UxmlSerializedUxmlObjectAttributeDescription attribute, UxmlSerializedData serializedData, UxmlAsset parentAsset)
        {
            var fullTypeName = serializedData == null ? UxmlAsset.NullNodeType : serializedData.GetType().DeclaringType.FullName;
            var uxmlAsset = m_UxmlDocument.AddUxmlObject(parentAsset, attribute.rootName, fullTypeName);

            // Assign the new asset id to the serialized data
            if (serializedData != null)
            {
                SyncSerializedDataToNewUxmlAsset(serializedData, uxmlAsset);
                serializedData.uxmlAssetId = uxmlAsset.id;
            }

            return uxmlAsset;
        }

        void RecordDocumentUndoOnce()
        {
            if (!m_DocumentUndoRecorded)
            {
                UndoRecordDocument(BuilderConstants.ModifyUxmlObject);
                m_DocumentUndoRecorded = true;
            }
        }

        protected void TrackElementPropertyValue(VisualElement target, SerializedProperty property)
        {
            // We use TrackPropertyValue because it does not send a change event when it is bound and its safer
            // than relying on change events which may not always be sent, such as when using a custom drawer.
            target.TrackPropertyValue(property, OnTrackedPropertyValueChange);
        }

        protected void TrackElementPropertyValue(VisualElement target, string path)
        {
            var property = m_CurrentElementSerializedObject.FindProperty(path);
            TrackElementPropertyValue(target, property);
        }

        void OnTrackedPropertyValueChange(object obj, SerializedProperty property)
        {
            if (s_IsInsideUndoRedoUpdate || m_CurrentElement == null || obj is not VisualElement target || target.panel == null)
                return;

            m_BatchedChanges.Add((target, property));
        }

        int GetCurrentUndoGroup()
        {
            // We track the undo group so we can fold multiple change events into 1.
            if (m_CurrentUndoGroup == null)
            {
                m_CurrentUndoGroup = Undo.GetCurrentGroup();
                EditorApplication.delayCall += () => m_CurrentUndoGroup = null;
            }

            return m_CurrentUndoGroup.Value;
        }

        void ProcessBatchedChanges()
        {
            if (m_BatchedChanges.Count == 0)
                return;

            var undoGroup = GetCurrentUndoGroup();
            using var pool = ListPool<(UxmlAsset uxmlOwner, VisualElement fieldElement, object newValue, UxmlSerializedData serializedData)>.Get(out var changesToProcess);

            try
            {
                foreach (var changeEvent in m_BatchedChanges)
                {
                    var fieldElement = GetRootFieldElement(changeEvent.target);

                    Undo.IncrementCurrentGroup();
                    var revertGroup = Undo.GetCurrentGroup();
                    var result = SynchronizePath(changeEvent.property.propertyPath, true);

                    var description = result.attributeDescription ?? fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                    var currentAttributeUxmlOwner = result.uxmlAsset;
                    var currentUxmlSerializedData = result.serializedData as UxmlSerializedData;
                    var newValue = description.GetSerializedValue(currentUxmlSerializedData);

                    // We choose to disregard callbacks when the value remains unchanged,
                    // which can occur during an Undo/Redo when the bound fields are updated.
                    // To do this we compare the value on the attribute owner to its serialized value,
                    // if they match then we consider them to have not changed.
                    if (result.attributeOwner != null)
                    {
                        description.TryGetValueFromObject(result.attributeOwner, out var previousValue);

                        // Unity serializes null values as default objects, so we need to do the same to compare.
                        if (!description.isUxmlObject && previousValue == null && !typeof(Object).IsAssignableFrom(description.type) && description.type.GetConstructor(Type.EmptyTypes) != null)
                            previousValue = Activator.CreateInstance(description.type);

                        if (UxmlAttributeComparison.ObjectEquals(previousValue, newValue))
                        {
                            Undo.RevertAllDownToGroup(revertGroup);
                            return;
                        }
                    }

                    changesToProcess.Add((currentAttributeUxmlOwner, fieldElement, newValue, currentUxmlSerializedData));
                }
            }
            finally
            {
                m_BatchedChanges.Clear();
            }

            // Apply changes to the whole element
            m_CurrentElementSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            CallDeserializeOnElement();

            // Now resync as its possible that the setters made changes during Deserialize, e.g clamping values.
            m_SerializedDataDescription.SyncSerializedData(m_CurrentElement, uxmlSerializedData);

            foreach (var change in changesToProcess)
            {
                if (change.newValue == null || !UxmlAttributeConverter.TryConvertToString(change.newValue, m_UxmlDocument, out var stringValue))
                    stringValue = change.newValue?.ToString();
                
                PostAttributeValueChange(change.fieldElement, stringValue, change.uxmlOwner);

                // Use the undo group of the field if it has one, otherwise use the current undo group
                var fieldUndoGroup = change.fieldElement.Q<BindableElement>()?.GetProperty(SerializedObjectBindingBase.UndoGroupPropertyKey);
                if (fieldUndoGroup != null)
                    undoGroup = (int)fieldUndoGroup;
            }
            
            // Update the serialized object to reflect the changes made by PostAttributeValueChange.
            m_CurrentElementSerializedObject.UpdateIfRequiredOrScript();

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

        void UpdateCustomPropertyDrawerAttributeOverrideStyle(CustomPropertyDrawerField fieldElement)
        {
            // When an assembly reload occurs this may be called before the view is fully initialized.
            if (m_CurrentElementSerializedObject == null)
                return;
            var overridde = IsAttributeOverriden(fieldElement.Q<PropertyField>().bindingPath);
            var style = fieldElement.GetFirstAncestorOfType<BuilderStyleRow>();
            style?.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, overridde);
        }

        internal virtual void UpdateAttributeOverrideStyle(VisualElement fieldElement)
        {
            using var marker = k_UpdateAttributeOverrideStyleMarker.Auto();

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
            using var marker = k_UpdateFieldStatusMarker.Auto();

            fieldElement = GetRootFieldElement(fieldElement);
            var valueInfo = GetValueInfo(fieldElement);

            fieldElement.SetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName, valueInfo);
            BuilderInspector.UpdateFieldStatusIconAndStyling(inspector?.currentVisualElement, fieldElement, valueInfo, false);

            if (currentFieldSource == AttributeFieldSource.UxmlTraits)
                BuilderInspector.UpdateFieldTooltip(fieldElement, valueInfo, m_CurrentElement);
        }

        public void SendNotifyAttributesChanged() => NotifyAttributesChanged();

        /// <summary>
        /// Notifies that the list of attributes has changed.
        /// </summary>
        protected virtual void NotifyAttributesChanged(string attributeName = null)
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

                var result = SynchronizePath(rootElement.rootPath, false);
                if (result.success)
                {
                    return IsAttributeOverriden(result.uxmlAsset == m_CurrentUxmlElement ? m_CurrentElement : null, result.uxmlAsset, attribute);
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
        /// Indicates whether the specified attribute is defined in the specified UMXL element.
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

        public bool IsAttributeOverriden(string propertyPath)
        {
            var result = SynchronizePath(propertyPath, false);
            if (result.success)
            {
                return result.uxmlAsset.HasAttribute(result.attributeDescription.name) ||
                    (result.attributeDescription.isUxmlObject && result.uxmlAsset != null);
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
                var styleFields = styleRow.Query<BindableElement>().Build();
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

        protected VisualElement FindField(string propertyPath)
        {
            foreach (var e in fieldsContainer.Query().Build())
            {
                if (e is PropertyField propertyField)
                {
                    if (propertyField.bindingPath == propertyPath)
                        return e;
                }
                else if (e is BindableElement bindableElement)
                {
                    if (bindableElement.bindingPath == propertyPath)
                        return e;
                }

                if (e.userData is SerializedProperty property && property.propertyPath == propertyPath)
                    return e;
            }
            return null;
        }

        void BuildAttributeFieldContextualMenu(ContextualMenuPopulateEvent evt) => BuildAttributeFieldContextualMenu(evt.menu, evt.currentTarget as BuilderStyleRow);

        void BuildCustomPropertyDrawerMenu(ContextualMenuPopulateEvent evt)
        {
            var target = evt.triggerEvent.target as VisualElement;
            var property = target.userData as SerializedProperty ?? target.parent.userData as SerializedProperty;
            if (property == null)
                return;

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                (a) => UnsetAttributeProperty(a.userData as SerializedProperty, true),
                action =>
                {
                    if (action.userData is not SerializedProperty property)
                        return DropdownMenuAction.Status.Disabled;

                    if (IsAttributeOverriden(property.propertyPath))
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                },
                property);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllAttributes(),
                action =>
                {
                    if (IsAnyAttributeSet())
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                });
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

                        var result = SynchronizePath(root.rootPath, false);
                        if (result.success)
                        {
                            hasAttributeOverride = IsAttributeOverriden(result.uxmlAsset == m_CurrentUxmlElement ? m_CurrentElement : null, result.uxmlAsset, field.GetLinkedAttributeDescription());
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
                    if (IsAnyAttributeSet())
                        return DropdownMenuAction.Status.Normal;
                    return DropdownMenuAction.Status.Disabled;
                });
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

        internal virtual void UnsetAllAttributes()
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
                inspector.headerSection.Refresh();
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

        protected virtual void CallDeserializeOnElement()
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

        public void UnsetAttributeProperty(string propertyPath, bool removeBinding)
        {
            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);

            var result = SynchronizePath(propertyPath, false);
            if (!result.success)
                return;

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

                    templateAsset.RemoveAttributeOverride(m_CurrentElement.name, result.attributeDescription.name);

                    builder.OnEnableAfterAllSerialization();

                    hierarchyView.SelectItemById(selectionId);
                }

                UnsetEnumValue(result.attributeDescription.name, removeBinding);
            }
            else
            {
                if (removeBinding)
                {
                    var bindingProperty = result.attributeDescription.serializedField.Name;
                    RemoveBindingFromSerializedData(null, bindingProperty);
                }

                result.uxmlAsset.RemoveAttribute(result.attributeDescription.name);
                result.attributeDescription.SetSerializedValue(result.serializedData, result.attributeDescription.defaultValue);
                CallDeserializeOnElement();

                UnsetEnumValue(result.attributeDescription.name, removeBinding);

                NotifyAttributesChanged();

                m_CurrentElementSerializedObject.UpdateIfRequiredOrScript();
                Refresh();
            }
        }

        public void UnsetAttributeProperty(SerializedProperty property, bool removeBinding)
        {
            var result = SynchronizePath(property.propertyPath, false);
            if (!result.success)
                return;

            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);

            // Unset value in asset.
            if (m_IsInTemplateInstance)
            {
                UnsetTemplateAttribute(result.attributeDescription.name, removeBinding);
            }
            else
            {
                result.uxmlAsset.RemoveAttribute(result.attributeDescription.name);
                result.attributeDescription.SetSerializedValue(result.serializedData, result.attributeDescription.defaultValue);
                CallDeserializeOnElement();

                UnsetEnumValue(result.attributeDescription.name, removeBinding);

                NotifyAttributesChanged(result.attributeDescription.name);
                Refresh();
            }
        }

        public void UnsetAttributeProperty(VisualElement fieldElement, bool removeBinding)
        {
            var attributeName = GetAttributeName(fieldElement);

            UndoRecordDocument(BuilderConstants.ChangeAttributeValueUndoMessage);

            // Unset value in asset.
            if (m_IsInTemplateInstance)
            {
                UnsetTemplateAttribute(attributeName, removeBinding);
            }
            else
            {
                var currentAttributesUxmlOwner = attributesUxmlOwner;
                var currentSerializedData = uxmlSerializedData;

                if (fieldElement.GetFirstAncestorOfType<UxmlAssetSerializedDataRoot>() is { } dataRoot && dataRoot.dataDescription.isUxmlObject)
                {
                    var result = SynchronizePath(dataRoot.rootPath, false);
                    currentAttributesUxmlOwner = result.uxmlAsset;
                    currentSerializedData = result.serializedData as UxmlSerializedData;
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
                        RemoveBindingFromSerializedData(fieldElement, bindingProperty);
                    }

                    currentAttributesUxmlOwner.RemoveAttribute(attributeName);
                    var description = fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                    description.SetSerializedValue(currentSerializedData, description.defaultValue);
                    CallDeserializeOnElement();
                }

                UnsetEnumValue(attributeName, removeBinding);

                NotifyAttributesChanged(attributeName);
                Refresh();
            }
        }

        void UnsetTemplateAttribute(string attributeName, bool removeBinding)
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

        protected void UnsetEnumValue(string attributeName, bool removeBinding)
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

                Undo.RegisterCompleteObjectUndo(prop.m_SerializedObject.targetObject, GetUndoMessage(prop));

                prop.stringValue = evt.newValue;
                m_CurrentElementSerializedObject.ApplyModifiedPropertiesWithoutUndo();

                OnTrackedPropertyValueChange(field, prop);
            }
            else
            {
                OnAttributeValueChange(evt);
            }
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

            using var marker = k_PostAttributeValueChangedMarker.Auto();
            var attributeName = GetAttributeName(field);
            PostAttributeValueChange(attributeName, value, uxmlAsset);

            // Mark field as overridden.
            if (field.GetFirstAncestorOfType<CustomPropertyDrawerField>() is { } customPropertyDrawer)
                UpdateCustomPropertyDrawerAttributeOverrideStyle(customPropertyDrawer);
            else
                UpdateAttributeOverrideStyle(field);

            // Notify of changes.
            NotifyAttributesChanged(attributeName);

            var styleRow = GetLinkedStyleRow(field);
            if (styleRow != null)
                UpdateFieldStatus(field);
        }

        void PostAttributeValueChange(string attributeName, string value, UxmlAsset uxmlAsset = null)
        {
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
        }

        static string GetUndoMessage(SerializedProperty prop)
        {
            var undoMessage = $"Modified {prop.name}";
            if (prop.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {prop.m_SerializedObject.targetObject.name}";

            return undoMessage;
        }
    }
}
