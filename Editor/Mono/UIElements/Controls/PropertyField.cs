// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class PropertyField : VisualElement, IBindable
    {
        private static readonly string s_PropertyFieldClassName = "unity-property-field";
        private static readonly string s_WrapperClassName = "unity-property-field-wrapper";
        private static readonly string s_LabelClassName = "unity-property-field-label";
        private static readonly string s_InputClassName = "unity-property-field-input";

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

        public PropertyField() : this(null, string.Empty) {}

        public PropertyField(SerializedProperty property) : this(property, string.Empty) {}

        public PropertyField(SerializedProperty property, string label)
        {
            AddToClassList(s_WrapperClassName);
            this.label = label;

            if (property == null)
                return;

            bindingPath = property.propertyPath;
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
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
                var customPropertyGUI = (handler.propertyDrawer as UIElementsPropertyDrawer)?.CreatePropertyGUI(m_SerializedProperty);
                if (customPropertyGUI == null)
                {
                    customPropertyGUI = new IMGUIContainer(() =>
                    {
                        EditorGUI.BeginChangeCheck();
                        m_SerializedProperty.serializedObject.Update();

                        EditorGUILayout.PropertyField(m_SerializedProperty, true);

                        m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                        EditorGUI.EndChangeCheck();
                    });
                }
                shadow.Add(customPropertyGUI);
            }
            else
            {
                var field = CreateFieldFromProperty(bindProperty);
                if (field != null)
                    shadow.Add(field);
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
            if (targetPropertyField == null || targetPropertyField.m_SerializedProperty == null)
                return;

            // We need to unbind *first* before we change the array size property value.
            // If we don't, the binding system could try to sync properties that no longer
            // exist - if the array shrunk.
            var parentSerializedObject = parentPropertyField?.m_SerializedProperty?.serializedObject;
            if (parentSerializedObject != null)
                parentPropertyField.Unbind();

            // We're forcefully updating the SerializedProperty value here, even
            // though we have a binding on it, because the very next step is to
            // Rebind() it. The Rebind() will regenerate the field (this field) and
            // bind it to another copy of this property. We need the value to be correct
            // on that copy of this property.
            var serialiedObject = targetPropertyField.m_SerializedProperty.serializedObject;
            serialiedObject.UpdateIfRequiredOrScript();
            targetPropertyField.m_SerializedProperty.intValue = changeEvent.newValue;
            serialiedObject.ApplyModifiedProperties();

            // We rebind the parent property field (which should be the foldout expanded field)
            // so that it regenerates (and rebinds) all array property fields (the new
            // number of them).
            if (parentSerializedObject != null)
                parentPropertyField.Bind(parentSerializedObject);
        }

        private VisualElement CreateFoldout(SerializedProperty property)
        {
            property = property.Copy();
            var foldout = new Foldout() { text = property.localizedDisplayName };
            foldout.value = property.isExpanded;
            foldout.bindingPath = property.propertyPath;
            foldout.name = "Foldout:" + property.propertyPath;

            var endProperty = property.GetEndProperty();
            property.NextVisible(true); // Expand the first child.
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                var field = new PropertyField(property);
                field.m_ParentPropertyField = this;
                field.name = "PropertyField:" + property.propertyPath;
                if (field == null)
                    continue;

                foldout.Add(field);
            }
            while (property.NextVisible(false)); // Never expand children.

            return foldout;
        }

        private VisualElement CreateLabeledField<T>(T input, SerializedProperty property) where T : VisualElement, IBindable
        {
            var field = new VisualElement();
            field.AddToClassList(s_PropertyFieldClassName);

            var label = new Label();
            label.AddToClassList(s_LabelClassName);
            label.text = string.IsNullOrEmpty(this.label)
                ? property.localizedDisplayName
                : this.label;
            field.Add(label);

            input.AddToClassList(s_InputClassName);
            input.bindingPath = property.propertyPath;
            input.name = "Input:" + property.propertyPath;
            field.Add(input);

            return field;
        }

        private VisualElement CreateFieldFromProperty(SerializedProperty property)
        {
            var propertyType = property.propertyType;

            if (EditorGUI.HasVisibleChildFields(property))
                return CreateFoldout(property);

            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    return CreateLabeledField(new IntegerField(), property);
                case SerializedPropertyType.Boolean:
                    return CreateLabeledField(new Toggle(), property);
                case SerializedPropertyType.Float:
                    return CreateLabeledField(new FloatField(), property);
                case SerializedPropertyType.String:
                    return CreateLabeledField(new TextField(), property);
                case SerializedPropertyType.Color:
                    return CreateLabeledField(new ColorField(), property);
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
                    return CreateLabeledField(field, property);
                }
                case SerializedPropertyType.LayerMask:
                    return CreateLabeledField(new LayerMaskField(), property);
                case SerializedPropertyType.Enum:
                {
                    var field = new PopupField<string>(property.enumDisplayNames.ToList(), property.enumValueIndex);
                    field.index = property.enumValueIndex;
                    return CreateLabeledField(field, property);
                }
                case SerializedPropertyType.Vector2:
                    return CreateLabeledField(new Vector2Field(), property);
                case SerializedPropertyType.Vector3:
                    return CreateLabeledField(new Vector3Field(), property);
                case SerializedPropertyType.Vector4:
                    return CreateLabeledField(new Vector4Field(), property);
                case SerializedPropertyType.Rect:
                    return CreateLabeledField(new RectField(), property);
                case SerializedPropertyType.ArraySize:
                {
                    var field = new IntegerField();
                    field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                    field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                    field.OnValueChanged((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                    return CreateLabeledField(field, property);
                }
                case SerializedPropertyType.Character:
                {
                    var field = new TextField();
                    field.maxLength = 1;
                    return CreateLabeledField(field, property);
                }
                case SerializedPropertyType.AnimationCurve:
                    return CreateLabeledField(new CurveField(), property);
                case SerializedPropertyType.Bounds:
                    return CreateLabeledField(new BoundsField(), property);
                case SerializedPropertyType.Gradient:
                    return CreateLabeledField(new GradientField(), property);
                case SerializedPropertyType.Quaternion:
                    return null;
                case SerializedPropertyType.ExposedReference:
                    return null;
                case SerializedPropertyType.FixedBufferSize:
                    return null;
                case SerializedPropertyType.Vector2Int:
                    return CreateLabeledField(new Vector2IntField(), property);
                case SerializedPropertyType.Vector3Int:
                    return CreateLabeledField(new Vector3IntField(), property);
                case SerializedPropertyType.RectInt:
                    return CreateLabeledField(new RectIntField(), property);
                case SerializedPropertyType.BoundsInt:
                    return CreateLabeledField(new BoundsIntField(), property);
                case SerializedPropertyType.Generic:
                default:
                    return null;
            }
        }
    }
}
