// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A SerializedProperty wrapper VisualElement that, on Bind(), will generate the correct field elements with the correct bindingPaths.
    /// </summary>
    public class PropertyField : VisualElement, IBindable
    {
        internal static readonly string foldoutTitleBoundLabelProperty = "unity-foldout-bound-title";

        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");

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

        private SerializedProperty m_SerializedProperty;
        private PropertyField m_ParentPropertyField;
        private int m_FoldoutDepth;

        private int m_DrawNestingLevel;
        private PropertyField m_DrawParentProperty;

        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;

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

            if (property == null)
                return;

            bindingPath = property.propertyPath;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_FoldoutDepth = this.GetFoldoutDepth();
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedPropertyBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent);

            // Don't allow the binding of `this` to continue because `this` is not
            // the actually bound field, it is just a container.
            evt.StopPropagation();
        }

        private void Reset(SerializedPropertyBindEvent evt)
        {
            if (m_SerializedProperty != null)
            {
                // if we already have a serialized property, determine if the property field can be reused without reset
                // this is only supported for non propertydrawer types
                if (m_ChildField != null && m_SerializedProperty.propertyType == evt.bindProperty.propertyType)
                {
                    var newField = CreateOrUpdateFieldFromProperty(evt.bindProperty, m_ChildField);
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
            }

            Clear();
            m_ChildField = null;
            var bindProperty = evt.bindProperty;
            m_SerializedProperty = bindProperty;
            if (bindProperty == null)
                return;

            ComputeNestingLevel();

            VisualElement customPropertyGUI = null;

            // Case 1292133: set proper nesting level before calling CreatePropertyGUI
            var handler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);
            using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
            {
                if (handler.hasPropertyDrawer)
                {
                    customPropertyGUI = handler.propertyDrawer.CreatePropertyGUI(m_SerializedProperty);

                    if (customPropertyGUI == null)
                    {
                        customPropertyGUI = CreatePropertyIMGUIContainer();
                    }
                    else
                    {
                        RegisterPropertyChangesOnCustomDrawerElement(customPropertyGUI);
                    }
                }
                else
                {
                    customPropertyGUI = CreateOrUpdateFieldFromProperty(bindProperty);
                    m_ChildField = customPropertyGUI;
                }
            }

            if (customPropertyGUI != null)
            {
                PropagateNestingLevel(customPropertyGUI);
                hierarchy.Add(customPropertyGUI);
            }
        }

        private VisualElement CreatePropertyIMGUIContainer()
        {
            GUIContent customLabel = string.IsNullOrEmpty(label) ? null : new GUIContent(label);

            return new IMGUIContainer(() =>
            {
                var originalWideMode = InspectorElement.SetWideModeForWidth(this);
                try
                {
                    if (!m_SerializedProperty.isValid)
                        return;

                    EditorGUI.BeginChangeCheck();
                    m_SerializedProperty.serializedObject.Update();

                    if (m_FoldoutDepth > 0)
                        EditorGUI.indentLevel += m_FoldoutDepth;

                    // Wait at last minute to call GetHandler, sometimes the handler cache is cleared between calls.
                    var handler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);
                    using (var nestingContext = handler.ApplyNestingContext(m_DrawNestingLevel))
                    {
                        if (label == null)
                        {
                            EditorGUILayout.PropertyField(m_SerializedProperty, true);
                        }
                        else if (label == string.Empty)
                        {
                            EditorGUILayout.PropertyField(m_SerializedProperty, GUIContent.none, true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(m_SerializedProperty, new GUIContent(label), true);
                        }
                    }

                    if (m_FoldoutDepth > 0)
                        EditorGUI.indentLevel -= m_FoldoutDepth;

                    m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        DispatchPropertyChangedEvent();
                    }
                }
                finally
                {
                    EditorGUIUtility.wideMode = originalWideMode;
                }
            });
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
                    if (field.bindingPath != propPath)
                    {
                        field.bindingPath = property.propertyPath;
                        field.Bind(property.serializedObject);
                    }
                }
                else
                {
                    field = new PropertyField(property);
                    field.m_ParentPropertyField = this;
                    m_ChildrenProperties.Add(field);

                    if (bindNewFields)
                        field.Bind(property.serializedObject);
                }
                field.name = "unity-property-field-" + property.propertyPath;

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
            var foldout = originalField != null && originalField is Foldout ? originalField as Foldout : new Foldout();
            bool hasCustomLabel = !string.IsNullOrEmpty(label);
            foldout.text = hasCustomLabel ? label : property.localizedDisplayName;
            foldout.value = property.isExpanded;
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

        private VisualElement ConfigureField<TField, TValue>(TField field, SerializedProperty property, Func<TField> factory)
            where TField : BaseField<TValue>
        {
            if (field == null)
            {
                field = factory();
            }
            var fieldLabel = label ?? property.localizedDisplayName;
            field.bindingPath = property.propertyPath;
            field.SetProperty(BaseField<TValue>.serializedPropertyCopyName, property.Copy());
            field.name = "unity-input-" + property.propertyPath;
            field.label = fieldLabel;

            field.labelElement.AddToClassList(labelUssClassName);
            field.visualInput.AddToClassList(inputUssClassName);

            // These default values are based off IMGUI
            m_LabelWidthRatio = 0.45f;
            m_LabelExtraPadding = 2.0f;

            field.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            field.RegisterValueChangedCallback((evt) =>
            {
                if (evt.target == field)
                {
                    DispatchPropertyChangedEvent();
                }
            });

            if (!(parent is InspectorElement inspectorElement))
                return field;

            field.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var baseField = field as BaseField<TValue>;

                // Calculate all extra padding from the containing element's contents
                var totalPadding = resolvedStyle.paddingLeft + resolvedStyle.paddingRight +
                    resolvedStyle.marginLeft + resolvedStyle.marginRight;

                // Get inspector element padding next
                totalPadding += inspectorElement.resolvedStyle.paddingLeft +
                    inspectorElement.resolvedStyle.paddingRight +
                    inspectorElement.resolvedStyle.marginLeft +
                    inspectorElement.resolvedStyle.marginRight;

                var labelElement = baseField.labelElement;

                // Then get label padding
                totalPadding += labelElement.resolvedStyle.paddingLeft + labelElement.resolvedStyle.paddingRight +
                    labelElement.resolvedStyle.marginLeft + labelElement.resolvedStyle.marginRight;

                // Then get base field padding
                totalPadding += field.resolvedStyle.paddingLeft + field.resolvedStyle.paddingRight +
                    field.resolvedStyle.marginLeft + field.resolvedStyle.marginRight;

                // Not all visual input controls have the same padding so we can't base our total padding on
                // that information.  Instead we add a flat value to totalPadding to best match the hard coded
                // calculation in IMGUI
                totalPadding += m_LabelExtraPadding;

                // Formula to follow IMGUI label width settings
                var newWidth = resolvedStyle.width * m_LabelWidthRatio - totalPadding;
                if (Mathf.Abs(labelElement.resolvedStyle.width - newWidth) > UIRUtility.k_Epsilon)
                    labelElement.style.width = newWidth;
            });

            return field;
        }

        VisualElement ConfigureListView(ListView listView, SerializedProperty property)
        {
            var propertyCopy = property.Copy();
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showBorder = true;
            listView.showAddRemoveFooter = true;
            listView.showBoundCollectionSize = true;
            listView.showFoldoutHeader = true;
            listView.headerTitle = string.IsNullOrEmpty(label) ? propertyCopy.localizedDisplayName : label;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.userData = propertyCopy;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listView.bindingPath = property.propertyPath;
            listView.viewDataKey = property.propertyPath;
            listView.name = "unity-list-" + property.propertyPath;
            listView.Bind(property.serializedObject);
            return listView;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio))
            {
                m_LabelWidthRatio = labelWidthRatio;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding))
            {
                m_LabelExtraPadding = labelExtraPadding;
            }
        }

        private VisualElement CreateOrUpdateFieldFromProperty(SerializedProperty property, object originalField = null)
        {
            var propertyType = property.propertyType;

            if (EditorGUI.HasVisibleChildFields(property, true))
                return CreateFoldout(property, originalField);

            TrimChildrenContainerSize(0);
            m_ChildrenContainer = null;

            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (property.type == "long")
                        return ConfigureField<LongField, long>(originalField as LongField, property, () => new LongField());
                    return ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());

                case SerializedPropertyType.Boolean:
                    return ConfigureField<Toggle, bool>(originalField as Toggle, property, () => new Toggle());

                case SerializedPropertyType.Float:
                    if (property.type == "double")
                        return ConfigureField<DoubleField, double>(originalField as DoubleField, property, () => new DoubleField());
                    return ConfigureField<FloatField, float>(originalField as FloatField, property, () => new FloatField());

                case SerializedPropertyType.String:
                    return ConfigureField<TextField, string>(originalField as TextField, property, () => new TextField());

                case SerializedPropertyType.Color:
                    return ConfigureField<ColorField, Color>(originalField as ColorField, property, () => new ColorField());

                case SerializedPropertyType.ObjectReference:
                {
                    ObjectField field = originalField as ObjectField;
                    if (field == null)
                        field = new ObjectField();

                    Type requiredType = null;

                    // Checking if the target ExtendsANativeType() avoids a native error when
                    // getting the type about: "type is not a supported pptr value"
                    var target = property.serializedObject.targetObject;
                    if (NativeClassExtensionUtilities.ExtendsANativeType(target))
                        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out requiredType);

                    if (requiredType == null)
                        requiredType = typeof(UnityEngine.Object);

                    field.objectType = requiredType;
                    return ConfigureField<ObjectField, UnityEngine.Object>(field, property, () => new ObjectField());
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
                        field = new IntegerField();
                    field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                    field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                    field.RegisterValueChangedCallback((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                    return ConfigureField<IntegerField, int>(field, property, () => new IntegerField());
                }

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
                case SerializedPropertyType.FixedBufferSize:
                    return null;

                case SerializedPropertyType.Vector2Int:
                    return ConfigureField<Vector2IntField, Vector2Int>(originalField as Vector2IntField, property, () => new Vector2IntField());

                case SerializedPropertyType.Vector3Int:
                    return ConfigureField<Vector3IntField, Vector3Int>(originalField as Vector3IntField, property, () => new Vector3IntField());

                case SerializedPropertyType.RectInt:
                    return ConfigureField<RectIntField, RectInt>(originalField as RectIntField, property, () => new RectIntField());

                case SerializedPropertyType.BoundsInt:
                    return ConfigureField<BoundsIntField, BoundsInt>(originalField as BoundsIntField, property, () => new BoundsIntField());


                case SerializedPropertyType.Generic:
                    return property.isArray
                        ? ConfigureListView(new ListView(), property)
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
            customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3Int>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<RectInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<BoundsInt>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
            customPropertyDrawer.RegisterCallback<ChangeEvent<Hash128>>((changeEvent) => AsyncDispatchPropertyChangedEvent());
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
                evt.target = this;
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
