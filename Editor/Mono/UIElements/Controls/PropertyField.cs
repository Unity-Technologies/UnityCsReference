// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A SerializedProperty wrapper VisualElement that, on Bind(), will generate the correct field elements with the correct binding paths. For more information, refer to [[wiki:UIE-uxml-element-PropertyField|UXML element PropertyField]].
    /// </summary>
    public class PropertyField : VisualElement, IBindable
    {
        private static readonly Regex s_MatchPPtrTypeName = new Regex(@"PPtr\<(\w+)\>");
        internal static readonly string foldoutTitleBoundLabelProperty = "unity-foldout-bound-title";
        internal static readonly string decoratorDrawersContainerClassName = "unity-decorator-drawers-container";
        internal static readonly string listViewBoundFieldProperty = "unity-list-view-property-field-bound";
        static readonly string listViewNamePrefix = "unity-list-";
        static readonly string buttonGroupNamePrefix = "unity-button-group-";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            #pragma warning disable 649
            [SerializeField] private string bindingPath;
            [SerializeField] internal string label;
            [SerializeField, HideInInspector] private bool displayLabel;
            #pragma warning restore 649

            public override object CreateInstance() => new PropertyField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (PropertyField)obj;
                e.bindingPath = bindingPath;
                e.label = (displayLabel || !string.IsNullOrEmpty(label)) ? label : null;
            }

            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes)
            {
                bag.TryGetAttributeValue("label", out label);
                displayLabel = label != null;
                handledAttributes.Add("display-label");
                handledAttributes.Add("label");
            }
        }

        /// <summary>
        /// Instantiates a <see cref="PropertyField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<PropertyField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PropertyField"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;
            UxmlStringAttributeDescription m_Label;

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
                m_Label = new UxmlStringAttributeDescription { name = "label", defaultValue = null };
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var field = ve as PropertyField;
                if (field == null)
                    return;

                field.label = m_Label.GetValueFromBag(bag, cc);

                string propPath = m_PropertyPath.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(propPath))
                    field.bindingPath = propPath;
            }
        }

        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        public IBinding binding { get; set; }

        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        public string bindingPath { get; set; }

        /// <summary>
        /// Optionally overwrite the label of the generate property field. If no label is provided the string will be taken from the SerializedProperty.
        /// </summary>
        public string label { get; set; }

        SerializedObject m_SerializedObject;
        internal SerializedObject serializedObject => m_SerializedObject;

        SerializedProperty m_SerializedProperty;
        string m_SerializedPropertyReferenceTypeName;
        PropertyField m_ParentPropertyField;
        int m_FoldoutDepth;
        VisualElement m_InspectorElement;
        VisualElement m_ContextWidthElement;

        int m_DrawNestingLevel;
        PropertyField m_DrawParentProperty;
        VisualElement m_DecoratorDrawersContainer;

        SerializedProperty serializedProperty => m_SerializedProperty;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-property-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of property fields in inspector elements
        /// </summary>
        public static readonly string inspectorElementUssClassName = ussClassName + "__inspector-property";

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField() : this(null, null) {}

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <param name="property">Providing a SerializedProperty in the construct just sets the bindingPath. You will still have to call Bind() on the PropertyField afterwards.</param>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField(SerializedProperty property) : this(property, null) {}

        /// <summary>
        /// PropertyField constructor.
        /// </summary>
        /// <param name="property">Providing a SerializedProperty in the construct just sets the bindingPath. You will still have to call Bind() on the PropertyField afterwards.</param>
        /// <param name="label">Optionally overwrite the property label.</param>
        /// <remarks>
        /// You will still have to call Bind() on the PropertyField afterwards.
        /// </remarks>
        public PropertyField(SerializedProperty property, string label)
        {
            AddToClassList(ussClassName);
            this.label = label;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);

            if (property == null)
                return;

            bindingPath = property.propertyPath;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_FoldoutDepth = this.GetFoldoutDepth();

            var currentElement = parent;
            while (currentElement != null)
            {
                if (currentElement.ClassListContains(InspectorElement.ussClassName))
                {
                    AddToClassList(inspectorElementUssClassName);
                    m_InspectorElement = currentElement;
                }

                if (currentElement.ClassListContains(PropertyEditor.s_MainContainerClassName))
                {
                    m_ContextWidthElement = currentElement;
                    break;
                }

                currentElement = currentElement.parent;
            }
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            RemoveFromClassList(inspectorElementUssClassName);
        }

        [EventInterest(typeof(SerializedPropertyBindEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is SerializedPropertyBindEvent bindEvent)
            {
                Reset(bindEvent);
                evt.StopPropagation();
            }
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultActionAtTarget override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
        }

        void Reset(SerializedProperty newProperty)
        {
            string newPropertyTypeName = null;
            var newPropertyType = newProperty.propertyType;

            if (newPropertyType == SerializedPropertyType.ManagedReference)
            {
                newPropertyTypeName = newProperty.managedReferenceFullTypename;
            }

            bool newPropertyTypeIsDifferent = true;

            var serializedObjectIsValid = m_SerializedObject != null && m_SerializedObject.m_NativeObjectPtr != IntPtr.Zero && m_SerializedObject.isValid;

            if (serializedObjectIsValid && m_SerializedProperty != null && m_SerializedProperty.isValid && newPropertyType == m_SerializedProperty.propertyType)
            {
                if(newPropertyType == SerializedPropertyType.ManagedReference)
                {
                    newPropertyTypeIsDifferent = newPropertyTypeName != m_SerializedPropertyReferenceTypeName;
                }
                else
                {
                    newPropertyTypeIsDifferent = false;
                }
            }

            m_SerializedProperty = newProperty;
            m_SerializedPropertyReferenceTypeName = newPropertyTypeName;
            m_SerializedObject = newProperty.serializedObject;

            // if we already have a serialized property, determine if the property field can be reused without reset
            // this is only supported for non propertydrawer types
            if (m_ChildField != null && !newPropertyTypeIsDifferent)
            {
                var propertyHandler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);
                ResetDecoratorDrawers(propertyHandler);

                var newField = CreateOrUpdateFieldFromProperty(newProperty, m_ChildField);
                // there was an issue where we weren't able to swap the bindings on the original field
                if (newField != m_ChildField)
                {
                    m_ChildField.Unbind();
                    var childIndex = IndexOf(m_ChildField);
                    if (childIndex >= 0)
                    {
                        m_ChildField.RemoveFromHierarchy();
                        m_ChildField = newField;
                        hierarchy.Insert(childIndex, m_ChildField);
                    }
                }

                return;
            }

            m_ChildField?.Unbind();
            m_ChildField = null;
            m_DecoratorDrawersContainer = null;

            Clear();

            if (m_SerializedProperty == null || !m_SerializedProperty.isValid)
                return;

            ComputeNestingLevel();

            VisualElement customPropertyGUI = null;

            // Case 1292133: set proper nesting level before calling CreatePropertyGUI
            var handler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);

            using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
            {
                if (handler.hasPropertyDrawer)
                {
                    handler.propertyDrawer.m_PreferredLabel = label ?? serializedProperty.localizedDisplayName;
                    customPropertyGUI = handler.propertyDrawer.CreatePropertyGUI(m_SerializedProperty);

                    if (customPropertyGUI == null)
                    {
                        customPropertyGUI = CreatePropertyIMGUIContainer();
                        m_imguiChildField = customPropertyGUI;
                    }
                    else
                    {
                        RegisterPropertyChangesOnCustomDrawerElement(customPropertyGUI);
                    }
                }
                else
                {
                    customPropertyGUI = CreateOrUpdateFieldFromProperty(m_SerializedProperty);
                    m_ChildField = customPropertyGUI;
                }
            }

            ResetDecoratorDrawers(handler);

            if (customPropertyGUI != null)
            {
                PropagateNestingLevel(customPropertyGUI);
                hierarchy.Add(customPropertyGUI);
            }

            if (m_SerializedProperty.propertyType == SerializedPropertyType.ManagedReference)
            {
                BindingExtensions.TrackPropertyValue(m_ChildField, m_SerializedProperty,
                    (e) => this.Bind(m_SerializedProperty.serializedObject));
            }
        }

        private void ResetDecoratorDrawers(PropertyHandler handler)
        {
             var decorators = handler.decoratorDrawers;

             if (decorators == null || decorators.Count == 0 || m_DrawNestingLevel > 0)
             {
                 if (m_DecoratorDrawersContainer != null)
                 {
                     Remove(m_DecoratorDrawersContainer);
                     m_DecoratorDrawersContainer = null;
                 }

                 return;
             }

             if (m_DecoratorDrawersContainer == null)
             {
                 m_DecoratorDrawersContainer = new VisualElement();
                 m_DecoratorDrawersContainer.AddToClassList(decoratorDrawersContainerClassName);
                 Insert(0, m_DecoratorDrawersContainer);
             }
             else
             {
                 m_DecoratorDrawersContainer.Clear();
             }

             foreach (var decorator in decorators)
             {
                 var ve = decorator.CreatePropertyGUI();

                 if (ve == null)
                 {
                     ve = new IMGUIContainer(() =>
                     {
                         var decoratorRect = new Rect();
                         decoratorRect.height = decorator.GetHeight();
                         decoratorRect.width = resolvedStyle.width;
                         decorator.OnGUI(decoratorRect);
                         ve.style.height = decoratorRect.height;
                     });
                     ve.style.height = decorator.GetHeight();
                 }

                 m_DecoratorDrawersContainer.Add(ve);
             }
        }

        private void Reset(SerializedPropertyBindEvent evt)
        {
            Reset(evt.bindProperty);
        }

        private VisualElement CreatePropertyIMGUIContainer()
        {
            GUIContent customLabel = string.IsNullOrEmpty(label) ? null : new GUIContent(label);

            var imguiContainer = new IMGUIContainer(() =>
            {
                var originalWideMode = InspectorElement.SetWideModeForWidth(this);
                var oldLabelWidth = EditorGUIUtility.labelWidth;

                try
                {
                    if (!serializedProperty.isValid)
                        return;

                    if (m_InspectorElement is InspectorElement inspectorElement)
                    {
                        //set the current PropertyHandlerCache to the current editor
                        ScriptAttributeUtility.propertyHandlerCache = inspectorElement.editor.propertyHandlerCache;
                    }

                    EditorGUI.BeginChangeCheck();
                    serializedProperty.serializedObject.Update();

                    if (classList.Contains(inspectorElementUssClassName))
                    {
                        var spacing = 0f;

                        if (m_imguiChildField != null)
                        {
                            spacing = m_imguiChildField.worldBound.x - m_InspectorElement.worldBound.x - m_InspectorElement.resolvedStyle.paddingLeft;
                        }

                        var imguiSpacing = EditorGUI.kLabelWidthMargin - EditorGUI.kLabelWidthPadding;
                        var contextWidthElement = m_ContextWidthElement ?? m_InspectorElement;
                        var contextWidth = contextWidthElement.resolvedStyle.width;
                        var labelWidth = (contextWidth * EditorGUI.kLabelWidthRatio - imguiSpacing - spacing);
                        var minWidth = EditorGUI.kMinLabelWidth + EditorGUI.kLabelWidthPadding;
                        var minLabelWidth = Mathf.Max(minWidth - spacing, 0f);

                        EditorGUIUtility.labelWidth = Mathf.Max(labelWidth, minLabelWidth);
                    }
                    else
                    {
                        if (m_FoldoutDepth > 0)
                            EditorGUI.indentLevel += m_FoldoutDepth;
                    }

                    // Wait at last minute to call GetHandler, sometimes the handler cache is cleared between calls.
                    var handler = ScriptAttributeUtility.GetHandler(serializedProperty);
                    using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
                    {
                        // Decorator drawers are already handled on the uitk side
                        handler.skipDecoratorDrawers = true;

                        if (label == null)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, true);
                        }
                        else if (label == string.Empty)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedProperty, new GUIContent(label), true);
                        }
                    }

                    if (!classList.Contains(inspectorElementUssClassName))
                    {
                        if (m_FoldoutDepth > 0)
                            EditorGUI.indentLevel -= m_FoldoutDepth;
                    }

                    serializedProperty.serializedObject.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        DispatchPropertyChangedEvent();
                    }
                }
                finally
                {
                    EditorGUIUtility.wideMode = originalWideMode;

                    if (classList.Contains(inspectorElementUssClassName))
                    {
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
            });

            return imguiContainer;
        }

        private void ComputeNestingLevel()
        {
            m_DrawNestingLevel = 0;
            for (var ve = m_DrawParentProperty; ve != null; ve = ve.m_DrawParentProperty)
            {
                if (ve.m_SerializedProperty == m_SerializedProperty ||
                    ScriptAttributeUtility.CanUseSameHandler(ve.m_SerializedProperty, m_SerializedProperty))
                {
                    m_DrawNestingLevel = ve.m_DrawNestingLevel + 1;
                    break;
                }
            }
        }

        private void PropagateNestingLevel(VisualElement customPropertyGUI)
        {
            var p = customPropertyGUI as PropertyField;
            if (p != null)
            {
                p.m_DrawParentProperty = this;
            }

            int childCount = customPropertyGUI.hierarchy.childCount;
            for (var i = 0; i < childCount; i++)
            {
                PropagateNestingLevel(customPropertyGUI.hierarchy[i]);
            }
        }

        private void Rebind()
        {
            if (m_SerializedProperty == null)
                return;

            var serializedObject = m_SerializedProperty.serializedObject;
            this.Unbind();
            this.Bind(serializedObject);
        }

        private void UpdateArrayFoldout(
            ChangeEvent<int> changeEvent,
            PropertyField targetPropertyField,
            PropertyField parentPropertyField)
        {
            var propertyIntValue = targetPropertyField.m_SerializedProperty.intValue;

            if (targetPropertyField == null || targetPropertyField.m_SerializedProperty == null || parentPropertyField == null && targetPropertyField.m_SerializedProperty.intValue == changeEvent.newValue)
                return;

            var parentSerializedObject = parentPropertyField?.m_SerializedProperty?.serializedObject;

            if (propertyIntValue != changeEvent.newValue)
            {
                var serialiedObject = targetPropertyField.m_SerializedProperty.serializedObject;
                serialiedObject.UpdateIfRequiredOrScript();
                targetPropertyField.m_SerializedProperty.intValue = changeEvent.newValue;
                serialiedObject.ApplyModifiedProperties();
            }

            if (parentPropertyField != null)
            {
                parentPropertyField.RefreshChildrenProperties(parentPropertyField.m_SerializedProperty.Copy(), true);
                return;
            }
        }

        private List<PropertyField> m_ChildrenProperties;

        /// <summary>
        /// stores the child field if there is only a single child. Used for updating bindings when this field is rebound.
        /// </summary>
        private VisualElement m_ChildField;

        private VisualElement m_imguiChildField;
        private VisualElement m_ChildrenContainer;

        void TrimChildrenContainerSize(int targetSize)
        {
            if (m_ChildrenProperties != null)
            {
                while (m_ChildrenProperties.Count > targetSize)
                {
                    var c = m_ChildrenProperties.Count - 1;
                    var pf = m_ChildrenProperties[c];
                    pf.Unbind();
                    pf.RemoveFromHierarchy();
                    m_ChildrenProperties.RemoveAt(c);
                }
            }
        }

        void RefreshChildrenProperties(SerializedProperty property, bool bindNewFields)
        {
            if (m_ChildrenContainer == null)
            {
                return;
            }

            var endProperty = property.GetEndProperty();
            int propCount = 0;

            if (m_ChildrenProperties == null)
            {
                m_ChildrenProperties = new List<PropertyField>();
            }

            property.NextVisible(true); // Expand the first child.
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                PropertyField field = null;
                var propPath = property.propertyPath;
                if (propCount < m_ChildrenProperties.Count)
                {
                    field = m_ChildrenProperties[propCount];
                    field.bindingPath = propPath;
                }
                else
                {
                    field = new PropertyField(property);
                    field.m_ParentPropertyField = this;
                    m_ChildrenProperties.Add(field);
                    field.bindingPath = propPath;
                }
                field.name = "unity-property-field-" + propPath;

                if (bindNewFields)
                    field.Bind(property.serializedObject);

                // Not yet knowing what type of field we are dealing with, we defer the showMixedValue value setting
                // to be automatically done via the next Reset call
                m_ChildrenContainer.Add(field);
                propCount++;
            }
            while (property.NextVisible(false)); // Never expand children.

            TrimChildrenContainerSize(propCount);
        }

        private VisualElement CreateFoldout(SerializedProperty property, object originalField = null)
        {
            property = property.Copy();
            if (originalField is not Foldout foldout)
            {
                foldout = new Foldout();
            }

            var hasCustomLabel = !string.IsNullOrEmpty(label);
            foldout.text = hasCustomLabel ? label : property.localizedDisplayName;
            foldout.bindingPath = property.propertyPath;
            foldout.name = "unity-foldout-" + property.propertyPath;

            // Make PropertyField foldout react even when disabled, like EditorGUILayout.Foldout.
            var foldoutToggle = foldout.Q<Toggle>(className: Foldout.toggleUssClassName);
            foldoutToggle.m_Clickable.acceptClicksIfDisabled = true;

            // Get Foldout label.
            var foldoutLabel = foldoutToggle.Q<Label>(className: Toggle.textUssClassName);
            if (hasCustomLabel)
            {
                foldoutLabel.text = foldout.text;
            }
            else
            {
                foldoutLabel.bindingPath = property.propertyPath;
                foldoutLabel.SetProperty(foldoutTitleBoundLabelProperty, true);
            }

            m_ChildrenContainer = foldout;

            RefreshChildrenProperties(property, false);

            return foldout;
        }

        void OnFieldValueChanged(EventBase evt)
        {
            if (evt.target == m_ChildField && m_SerializedProperty.isValid)
            {
                if (m_SerializedProperty.propertyType == SerializedPropertyType.ArraySize && evt is ChangeEvent<int> changeEvent)
                {
                    UpdateArrayFoldout(changeEvent, this, m_ParentPropertyField);
                }

                DispatchPropertyChangedEvent();
            }
        }

        private TField ConfigureField<TField, TValue>(TField field, SerializedProperty property, Func<TField> factory)
            where TField : BaseField<TValue>
        {
            if (field == null)
            {
                field = factory();
                field.RegisterValueChangedCallback((evt) => OnFieldValueChanged(evt));
            }

            var fieldLabel = label ?? property.localizedDisplayName;
            field.bindingPath = property.propertyPath;
            field.SetProperty(BaseField<TValue>.serializedPropertyCopyName, property.Copy());
            field.name = "unity-input-" + property.propertyPath;
            field.label = fieldLabel;

            ConfigureFieldStyles<TField, TValue>(field);

            return field;
        }

        internal static void ConfigureFieldStyles<TField, TValue>(TField field) where TField : BaseField<TValue>
        {
            field.labelElement.AddToClassList(labelUssClassName);
            field.visualInput.AddToClassList(inputUssClassName);
            field.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);

            var nestedFields = field.visualInput.Query<VisualElement>(
                classes: new []{BaseField<TValue>.ussClassName, BaseCompositeField<int, IntegerField, int>.ussClassName} );

            nestedFields.ForEach(x =>
            {
                x.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);
            });
        }

        VisualElement ConfigureListView(ListView listView, SerializedProperty property, Func<ListView> factory)
        {
            if (listView == null)
            {
                listView = factory();
                listView.showBorder = true;
                listView.selectionType = SelectionType.Multiple;
                listView.showAddRemoveFooter = true;
                listView.showBoundCollectionSize = true;
                listView.showFoldoutHeader = true;
                listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
                listView.itemsSourceSizeChanged += DispatchPropertyChangedEvent;
            }

            var propertyCopy = property.Copy();
            var listViewName = $"{listViewNamePrefix}{property.propertyPath}";
            listView.headerTitle = string.IsNullOrEmpty(label) ? propertyCopy.localizedDisplayName : label;
            listView.userData = propertyCopy;
            listView.bindingPath = property.propertyPath;
            listView.viewDataKey = listViewName;
            listView.name = listViewName;
            listView.SetProperty(listViewBoundFieldProperty, this);

            // Make list view foldout react even when disabled, like EditorGUILayout.Foldout.
            var toggle = listView.headerFoldout?.toggle;
            if (toggle != null)
                toggle.m_Clickable.acceptClicksIfDisabled = true;

            return listView;
        }

        VisualElement ConfigureToggleButtonGroup(ToggleButtonGroup buttonGroup, SerializedProperty property, Func<ToggleButtonGroup> factory)
        {
            var propertyCopy = property.Copy();

            if (buttonGroup == null)
            {
                buttonGroup = factory();
                buttonGroup.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
                buttonGroup.RegisterValueChangedCallback(OnToggleGroupChanged);
            }

            var lengthProperty = propertyCopy.FindPropertyRelative("m_Length");
            var dataProperty = propertyCopy.FindPropertyRelative("m_Data");

            var length = lengthProperty.intValue;

            // Check the field for ToggleButtonGroupStatePropertiesAttribute
            var fieldInfo = ScriptAttributeUtility.GetFieldInfoFromProperty(property, out _);
            foreach (var attribute in fieldInfo.GetCustomAttributes(false))
            {
                if (attribute is not ToggleButtonGroupStatePropertiesAttribute stateProperties)
                    continue;

                if (stateProperties.length >= 0)
                {
                    length = stateProperties.length;
                    lengthProperty.intValue = length;
                    lengthProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                buttonGroup.allowEmptySelection = stateProperties.allowEmptySelection;
                buttonGroup.isMultipleSelection = stateProperties.allowMultipleSelection;
                break;
            }

            var buttonGroupName = $"{buttonGroupNamePrefix}{property.propertyPath}";
            var fieldLabel = label ?? property.localizedDisplayName;
            buttonGroup.userData = propertyCopy;
            buttonGroup.bindingPath = property.propertyPath;
            buttonGroup.SetProperty(BaseField<ToggleButtonGroupState>.serializedPropertyCopyName, propertyCopy);
            buttonGroup.name = buttonGroupName;
            buttonGroup.label = fieldLabel;
            buttonGroup.Q(className: ToggleButtonGroup.buttonGroupClassName).Clear();

            // Track changes to the ToggleButtonGroupState values.
            buttonGroup.TrackPropertyValue(propertyCopy);
            buttonGroup.TrackPropertyValue(propertyCopy, OnPropertyChanged);
            buttonGroup.TrackPropertyValue(lengthProperty, OnPropertyChanged);
            buttonGroup.TrackPropertyValue(dataProperty, OnPropertyChanged);

            for (var i = 0; i < length; i++)
            {
                buttonGroup.Add(new Button { text = i.ToString() });
            }

            void OnToggleGroupChanged(ChangeEvent<ToggleButtonGroupState> evt)
            {
                propertyCopy.structValue = evt.newValue;
                propertyCopy.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            void OnPropertyChanged(SerializedProperty _)
            {
                var currentLength = buttonGroup.value.length;
                var newLength = lengthProperty.intValue;
                var dataValue = dataProperty.ulongValue;

                // Empty state is initialized with max length instead of zero, but we don't want to spawn all buttons here. 
                if (dataValue == 0 && newLength == ToggleButtonGroupState.maxLength)
                {
                    newLength = 0;
                }

                if (currentLength < newLength)
                {
                    for (var i = currentLength; i < newLength; i++)
                    {
                        buttonGroup.Add(new Button { text = i.ToString() });
                    }
                }
                else if (currentLength > newLength)
                {
                    for (var i = newLength; i < currentLength; i++)
                    {
                        var lastItem = buttonGroup.Query<Button>(className: ToggleButtonGroup.buttonClassName).Last();
                        if (lastItem == null)
                            break;

                        lastItem.RemoveFromHierarchy();
                    }
                }

                buttonGroup.SetValueWithoutNotify((ToggleButtonGroupState)propertyCopy.structValue);
            }

            return buttonGroup;
        }

        private VisualElement CreateOrUpdateFieldFromProperty(SerializedProperty property, object originalField = null)
        {
            var propertyType = property.propertyType;

            if (EditorGUI.HasVisibleChildFields(property, true) && !property.isArray && property.type != nameof(ToggleButtonGroupState))
                return CreateFoldout(property, originalField);

            TrimChildrenContainerSize(0);
            m_ChildrenContainer = null;

            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (property.type == "long")
                        return ConfigureField<LongField, long>(originalField as LongField, property,
                            () => new LongField());
                    if (property.type == "ulong")
                        return ConfigureField<UnsignedLongField, ulong>(originalField as UnsignedLongField, property,
                            () => new UnsignedLongField());
                    if (property.type == "uint")
                        return ConfigureField<UnsignedIntegerField, uint>(originalField as UnsignedIntegerField, property,
                            () => new UnsignedIntegerField());

                {
                    var intField = ConfigureField<IntegerField, int>(originalField as IntegerField, property,
                        () => new IntegerField()) as IntegerField;

                    if (intField != null)
                    {
                        // If the field was recycled from an ArraySize property
                        intField.isDelayed = false;
                    }

                    return intField;
                }
                case SerializedPropertyType.Boolean:
                    return ConfigureField<Toggle, bool>(originalField as Toggle, property, () => new Toggle());

                case SerializedPropertyType.Float:
                    if (property.type == "double")
                        return ConfigureField<DoubleField, double>(originalField as DoubleField, property, () => new DoubleField());
                    return ConfigureField<FloatField, float>(originalField as FloatField, property, () => new FloatField());

                case SerializedPropertyType.String:
                {
                    var strField = ConfigureField<TextField, string>(originalField as TextField, property,
                        () => new TextField()) as TextField;
                    strField.maxLength = -1; //Can happen when used from Character
                    return strField;
                }

                case SerializedPropertyType.Color:
                    return ConfigureField<ColorField, Color>(originalField as ColorField, property, () => new ColorField());

                case SerializedPropertyType.ObjectReference:
                {
                    var field = ConfigureField<ObjectField, UnityEngine.Object>(originalField as ObjectField, property, () => new ObjectField());

                    Type requiredType = null;

                    // Checking if the target ExtendsANativeType() avoids a native error when
                    // getting the type about: "type is not a supported pptr value"
                    var target = property.serializedObject.targetObject;
                    if (NativeClassExtensionUtilities.ExtendsANativeType(target))
                        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out requiredType);

                    // case 1423715: built-in types that are defined on the native side will not reference a C# type, but rather a PPtr<Type>, so in the
                    // case where we can't extract the C# type from the FieldInfo, we need to extract it from the string representation.
                    if (requiredType == null)
                    {
                        var targetTypeName = s_MatchPPtrTypeName.Match(property.type).Groups[1].Value;
                        foreach (var objectTypes in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>())
                        {
                            if (!objectTypes.Name.Equals(targetTypeName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            // We ignore C# types as they can can be confused with a built-in type with the same name,
                            // we can use the FieldInfo to find MonoScript types. (UUM-29499)
                            if (typeof(MonoBehaviour).IsAssignableFrom(objectTypes) || typeof(ScriptableObject).IsAssignableFrom(objectTypes))
                                continue;

                            requiredType = objectTypes;
                            break;
                        }
                    }

                    field.SetObjectTypeWithoutDisplayUpdate(requiredType);
                    field.UpdateDisplay();

                    return field;
                }
                case SerializedPropertyType.LayerMask:
                    return ConfigureField<LayerMaskField, int>(originalField as LayerMaskField, property, () => new LayerMaskField());

                case SerializedPropertyType.Enum:
                {
                    ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var enumType);

                    if (enumType != null && enumType.IsDefined(typeof(FlagsAttribute), false))
                    {
                        // We should use property.longValue instead of intValue once long enum types are supported.
                        var enumData = EnumDataUtility.GetCachedEnumData(enumType);
                        if (originalField != null && originalField is EnumFlagsField enumFlagsField)
                        {
                            enumFlagsField.choices = enumData.displayNames.ToList();
                            enumFlagsField.value = (Enum)Enum.ToObject(enumType, property.intValue);
                        }
                        return ConfigureField<EnumFlagsField, Enum>(originalField as EnumFlagsField, property,
                            () => new EnumFlagsField
                            {
                                choices = enumData.displayNames.ToList(),
                                value = (Enum)Enum.ToObject(enumType, property.intValue)
                            });
                    }
                    else
                    {
                        // We need to use property.enumDisplayNames[property.enumValueIndex] as the source of truth for
                        // the popup index, because enumData.displayNames and property.enumDisplayNames might not be
                        // in the same order.
                        var enumData = enumType != null ? (EnumData?)EnumDataUtility.GetCachedEnumData(enumType) : null;
                        var propertyDisplayNames = EditorGUI.EnumNamesCache.GetEnumDisplayNames(property);
                        var popupEntries = (enumData?.displayNames ?? propertyDisplayNames).ToList();
                        int propertyFieldIndex = (property.enumValueIndex < 0 || property.enumValueIndex >= propertyDisplayNames.Length
                            ? PopupField<string>.kPopupFieldDefaultIndex : (enumData != null
                                ? Array.IndexOf(enumData.Value.displayNames, propertyDisplayNames[property.enumValueIndex])
                                : property.enumValueIndex));
                        if (originalField != null && originalField is PopupField<string> popupField)
                        {
                            popupField.choices = popupEntries;
                            popupField.index = propertyFieldIndex;
                        }
                        return ConfigureField<PopupField<string>, string>(originalField as PopupField<string>, property,
                            () => new PopupField<string>(popupEntries, property.enumValueIndex)
                            {
                                index = propertyFieldIndex
                            });
                    }
                }
                case SerializedPropertyType.Vector2:
                    return ConfigureField<Vector2Field, Vector2>(originalField as Vector2Field, property, () => new Vector2Field());

                case SerializedPropertyType.Vector3:
                    return ConfigureField<Vector3Field, Vector3>(originalField as Vector3Field, property, () => new Vector3Field());

                case SerializedPropertyType.Vector4:
                    return ConfigureField<Vector4Field, Vector4>(originalField as Vector4Field, property, () => new Vector4Field());

                case SerializedPropertyType.Rect:
                    return ConfigureField<RectField, Rect>(originalField as RectField, property, () => new RectField());

                case SerializedPropertyType.ArraySize:
                {
                    IntegerField field = originalField as IntegerField;
                    if (field == null)
                    {
                        field = new IntegerField();
                        field.RegisterValueChangedCallback((evt) => OnFieldValueChanged(evt));
                    }

                    field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                    field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                    return ConfigureField<IntegerField, int>(field, property, () => new IntegerField());
                }

                case SerializedPropertyType.FixedBufferSize:
                    return ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());

                case SerializedPropertyType.Character:
                {
                    TextField field = originalField as TextField;
                    if (field != null)
                        field.maxLength = 1;
                    return ConfigureField<TextField, string>(field, property, () => new TextField { maxLength = 1 });
                }

                case SerializedPropertyType.AnimationCurve:
                    return ConfigureField<CurveField, AnimationCurve>(originalField as CurveField, property, () => new CurveField());

                case SerializedPropertyType.Bounds:
                    return ConfigureField<BoundsField, Bounds>(originalField as BoundsField, property, () => new BoundsField());

                case SerializedPropertyType.Gradient:
                    return ConfigureField<GradientField, Gradient>(originalField as GradientField, property, () => new GradientField());

                case SerializedPropertyType.Quaternion:
                    return null;
                case SerializedPropertyType.ExposedReference:
                    return null;

                case SerializedPropertyType.Vector2Int:
                    return ConfigureField<Vector2IntField, Vector2Int>(originalField as Vector2IntField, property, () => new Vector2IntField());

                case SerializedPropertyType.Vector3Int:
                    return ConfigureField<Vector3IntField, Vector3Int>(originalField as Vector3IntField, property, () => new Vector3IntField());

                case SerializedPropertyType.RectInt:
                    return ConfigureField<RectIntField, RectInt>(originalField as RectIntField, property, () => new RectIntField());

                case SerializedPropertyType.BoundsInt:
                    return ConfigureField<BoundsIntField, BoundsInt>(originalField as BoundsIntField, property, () => new BoundsIntField());

                case SerializedPropertyType.Hash128:
                    return ConfigureField<Hash128Field, Hash128>(originalField as Hash128Field, property, () => new Hash128Field());

                case SerializedPropertyType.Generic:
                    if (property.type == nameof(ToggleButtonGroupState))
                    {
                        return ConfigureToggleButtonGroup(originalField as ToggleButtonGroup, property, () => new ToggleButtonGroup());
                    }

                    return property.isArray
                        ? ConfigureListView(originalField as ListView, property, () => new ListView())
                        : null;

                default:
                    return null;
            }
        }

        private void RegisterPropertyChangesOnCustomDrawerElement(VisualElement customPropertyDrawer)
        {
            // We dispatch this async in order to minimize the number of changeEvents we'll end up dispatching
            customPropertyDrawer.RegisterCallback<ChangeEvent<SerializedProperty>>((changeEvent) => AsyncDispatchPropertyChangedEvent());

            // Now we add property change events for known SerializedPropertyTypes. Since we don't know what this
            // drawer can edit or what it will end up containing we need to register everything
            customPropertyDrawer.RegisterCallback<ChangeEvent<int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<bool>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<float>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<double>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<string>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Color>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<UnityEngine.Object>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            // SerializedPropertyType.LayerMask -> int
            // SerializedPropertyType.Enum is handled either by string or
            customPropertyDrawer.RegisterCallback<ChangeEvent<Enum>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector4>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Rect>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            // SerializedPropertyType.ArraySize ->  int
            // SerializedPropertyType.Character -> string
            customPropertyDrawer.RegisterCallback<ChangeEvent<AnimationCurve>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Bounds>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Gradient>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Quaternion>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<RectInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<BoundsInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Hash128>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<ToggleButtonGroupState>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
        }

        private int m_PropertyChangedCounter = 0;

        void AsyncDispatchPropertyChangedEvent()
        {
            m_PropertyChangedCounter++;
            schedule.Execute(() => ExecuteAsyncDispatchPropertyChangedEvent());
        }

        void ExecuteAsyncDispatchPropertyChangedEvent()
        {
            m_PropertyChangedCounter--;

            if (m_PropertyChangedCounter <= 0)
            {
                DispatchPropertyChangedEvent();
                m_PropertyChangedCounter = 0;
            }
        }

        private void DispatchPropertyChangedEvent()
        {
            using (var evt = SerializedPropertyChangeEvent.GetPooled(m_SerializedProperty))
            {
                evt.elementTarget = this;
                SendEvent(evt);
            }
        }

        /// <summary>
        /// Registers this callback to receive SerializedPropertyChangeEvent when a value is changed.
        /// </summary>
        public void RegisterValueChangeCallback(EventCallback<SerializedPropertyChangeEvent> callback)
        {
            if (callback != null)
            {
                this.RegisterCallback<SerializedPropertyChangeEvent>((evt) =>
                {
                    if (evt.target == this)
                        callback(evt);
                });
            }
        }
    }
}
