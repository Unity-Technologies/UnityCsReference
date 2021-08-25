using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class PropertyField : VisualElement, IBindable
    {
        internal static readonly string foldoutTitleBoundLabelProperty = "unity-foldout-bound-title";

        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");

        public new class UxmlFactory : UxmlFactory<PropertyField, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;
            UxmlStringAttributeDescription m_Label;

            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
                m_Label = new UxmlStringAttributeDescription { name = "label" };
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

        public IBinding binding { get; set; }
        public string bindingPath { get; set; }

        public string label { get; set; }

        private SerializedProperty m_SerializedProperty;
        private PropertyField m_ParentPropertyField;

        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;

        public static readonly string ussClassName = "unity-property-field";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string inputUssClassName = ussClassName + "__input";

        public PropertyField() : this(null, string.Empty) {}

        public PropertyField(SerializedProperty property) : this(property, string.Empty) {}

        public PropertyField(SerializedProperty property, string label)
        {
            AddToClassList(ussClassName);
            this.label = label;

            if (property == null)
                return;

            bindingPath = property.propertyPath;
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
            Clear();

            var bindProperty = evt.bindProperty;
            m_SerializedProperty = bindProperty;
            if (bindProperty == null)
                return;

            var handler = ScriptAttributeUtility.GetHandler(m_SerializedProperty);
            if (handler.hasPropertyDrawer)
            {
                var customPropertyGUI = handler.propertyDrawer.CreatePropertyGUI(m_SerializedProperty);

                if (customPropertyGUI == null)
                {
                    GUIContent customLabel = string.IsNullOrEmpty(label) ? null : new GUIContent(label);
                    customPropertyGUI = new IMGUIContainer(() =>
                    {
                        var originalWideMode = InspectorElement.SetWideModeForWidth(this);

                        try
                        {
                    if (!m_SerializedProperty.isValid)
                        return;
                    
                            EditorGUI.BeginChangeCheck();
                            m_SerializedProperty.serializedObject.Update();

                            EditorGUILayout.PropertyField(m_SerializedProperty, customLabel, true);

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
                else
                {
                    RegisterPropertyChangesOnCustomDrawerElement(customPropertyGUI);
                }
                hierarchy.Add(customPropertyGUI);
            }
            else
            {
                var field = CreateFieldFromProperty(bindProperty);
                if (field != null)
                    hierarchy.Add(field);
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

        private VisualElement CreateFoldout(SerializedProperty property)
        {
            property = property.Copy();
            var foldout = new Foldout();
            bool hasCustomLabel = !string.IsNullOrEmpty(label);
            foldout.text = hasCustomLabel ? label : property.localizedDisplayName;
            foldout.value = property.isExpanded;
            foldout.bindingPath = property.propertyPath;
            foldout.name = "unity-foldout-" + property.propertyPath;

            // Get Foldout label.
            var foldoutToggle = foldout.Q<Toggle>(className: Foldout.toggleUssClassName);
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

        private VisualElement ConfigureField<TField, TValue>(TField field, SerializedProperty property)
            where TField : BaseField<TValue>
        {
            var fieldLabel = string.IsNullOrEmpty(label) ? property.localizedDisplayName : label;
            field.bindingPath = property.propertyPath;
            field.name = "unity-input-" + property.propertyPath;
            field.label = fieldLabel;

            var fieldLabelElement = field.Q<Label>(className: BaseField<TValue>.labelUssClassName);
            if (fieldLabelElement != null)
            {
                fieldLabelElement.userData = property.Copy();
            }

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

        private VisualElement CreateFieldFromProperty(SerializedProperty property)
        {
            var propertyType = property.propertyType;

            if (EditorGUI.HasVisibleChildFields(property, true))
                return CreateFoldout(property);

            TrimChildrenContainerSize(0);
            m_ChildrenContainer = null;

            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (property.type == "long")
                        return ConfigureField<LongField, long>(new LongField(), property);
                    return ConfigureField<IntegerField, int>(new IntegerField(), property);

                case SerializedPropertyType.Boolean:
                    return ConfigureField<Toggle, bool>(new Toggle(), property);

                case SerializedPropertyType.Float:
                    return ConfigureField<FloatField, float>(new FloatField(), property);

                case SerializedPropertyType.String:
                    return ConfigureField<TextField, string>(new TextField(), property);

                case SerializedPropertyType.Color:
                    return ConfigureField<ColorField, Color>(new ColorField(), property);

                case SerializedPropertyType.ObjectReference:
                {
                    var field = new ObjectField();
                    Type requiredType = null;

                    // Checking if the target ExtendsANativeType() avoids a native error when
                    // getting the type about: "type is not a supported pptr value"
                    var target = property.serializedObject.targetObject;
                    if (NativeClassExtensionUtilities.ExtendsANativeType(target))
                        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out requiredType);

                    if (requiredType == null)
                        requiredType = typeof(UnityEngine.Object);

                    field.objectType = requiredType;
                    return ConfigureField<ObjectField, UnityEngine.Object>(field, property);
                }
                case SerializedPropertyType.LayerMask:
                    return ConfigureField<LayerMaskField, int>(new LayerMaskField(), property);

                case SerializedPropertyType.Enum:
                {
                    Type enumType;
                    ScriptAttributeUtility.GetFieldInfoFromProperty(property, out enumType);
                    if (enumType != null && enumType.IsDefined(typeof(FlagsAttribute), false))
                    {
                        var field = new EnumFlagsField();
                        field.choices = property.enumDisplayNames.ToList();
                        field.value = (Enum)Enum.ToObject(enumType, property.intValue);
                        return ConfigureField<EnumFlagsField, Enum>(field, property);
                    }
                    else
                    {
                        var field = new PopupField<string>(property.enumDisplayNames.ToList(), property.enumValueIndex);
                        field.index = property.enumValueIndex;
                        return ConfigureField<PopupField<string>, string>(field, property);
                    }
                }
                case SerializedPropertyType.Vector2:
                    return ConfigureField<Vector2Field, Vector2>(new Vector2Field(), property);

                case SerializedPropertyType.Vector3:
                    return ConfigureField<Vector3Field, Vector3>(new Vector3Field(), property);

                case SerializedPropertyType.Vector4:
                    return ConfigureField<Vector4Field, Vector4>(new Vector4Field(), property);

                case SerializedPropertyType.Rect:
                    return ConfigureField<RectField, Rect>(new RectField(), property);

                case SerializedPropertyType.ArraySize:
                {
                    var field = new IntegerField();
                    field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                    field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                    field.RegisterValueChangedCallback((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                    return ConfigureField<IntegerField, int>(field, property);
                }

                case SerializedPropertyType.Character:
                {
                    var field = new TextField();
                    field.maxLength = 1;
                    return ConfigureField<TextField, string>(field, property);
                }

                case SerializedPropertyType.AnimationCurve:
                    return ConfigureField<CurveField, AnimationCurve>(new CurveField(), property);

                case SerializedPropertyType.Bounds:
                    return ConfigureField<BoundsField, Bounds>(new BoundsField(), property);

                case SerializedPropertyType.Gradient:
                    return ConfigureField<GradientField, Gradient>(new GradientField(), property);

                case SerializedPropertyType.Quaternion:
                    return null;
                case SerializedPropertyType.ExposedReference:
                    return null;
                case SerializedPropertyType.FixedBufferSize:
                    return null;

                case SerializedPropertyType.Vector2Int:
                    return ConfigureField<Vector2IntField, Vector2Int>(new Vector2IntField(), property);

                case SerializedPropertyType.Vector3Int:
                    return ConfigureField<Vector3IntField, Vector3Int>(new Vector3IntField(), property);

                case SerializedPropertyType.RectInt:
                    return ConfigureField<RectIntField, RectInt>(new RectIntField(), property);

                case SerializedPropertyType.BoundsInt:
                    return ConfigureField<BoundsIntField, BoundsInt>(new BoundsIntField(), property);

                case SerializedPropertyType.Generic:
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
