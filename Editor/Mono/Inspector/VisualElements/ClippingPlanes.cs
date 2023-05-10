// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector
{
    internal class ClippingPlanes : BaseCompositeField<Vector2, FloatField, float>
    {
        public new class UxmlFactory : UxmlFactory<ClippingPlanes, UxmlTraits> { }

        const string k_CompositeInputStyle = "unity-composite-field__input";
        const string k_CompositeFieldStyle = "unity-composite-field__field";
        const string k_NearClipStyle = "unity-near-clip-input";
        const string k_FarClipStyle = "unity-far-clip-input";
        const float k_NearFarLabelsWidth = EditorGUI.kNearFarLabelsWidth;

        bool m_DirtyX;
        bool m_DirtyY;

        readonly SerializedProperty[] m_Properties = new SerializedProperty[2];

        public SerializedProperty nearClip
        {
            get => m_Properties[0];
            set
            {
                m_Properties[0] = value;
                MockPropertyField(fields[0], EditorGUI.s_NearAndFarLabels[0], m_Properties[0]);
                Update();
            }
        }
        public SerializedProperty farClip
        {
            get => m_Properties[1];
            set
            {
                m_Properties[1] = value;
                MockPropertyField(fields[1], EditorGUI.s_NearAndFarLabels[1], m_Properties[1]);
                Update();
            }
        }

        internal override FieldDescription[] DescribeFields() => new[]
        {
                new FieldDescription("Near", k_NearClipStyle, r => r.x, (ref Vector2 r, float v) =>
                {
                    r.x = v;
                    m_DirtyX = true;
                }),
                new FieldDescription("Far", k_FarClipStyle, r => r.y, (ref Vector2 r, float v) =>
                {
                    r.y = v;
                    m_DirtyY = true;
                }),
            };

        public ClippingPlanes() : base(labelProperty, 2)
        {
            AddToClassList(BaseField<bool>.alignedFieldUssClassName);

            RegisterCallback<AttachToPanelEvent>(e =>
            {
                e.elementTarget.Q(className: k_CompositeInputStyle)?.RemoveFromClassList(k_CompositeInputStyle);

                foreach (var field in fields)
                {
                    field.RemoveFromClassList(k_CompositeFieldStyle);
                    field.RemoveFromClassList(BaseField<bool>.alignedFieldUssClassName);
                    field.style.marginLeft = field.style.marginRight = 0;

                    var label = field.Q<Label>();
                    label.style.flexBasis = label.style.minWidth = new StyleLength(k_NearFarLabelsWidth);
                    label.style.marginLeft = label.style.marginRight = 0;
                }
            });
            RegisterCallback<ChangeEvent<Vector2>>(e =>
            {
                if (m_DirtyX)
                {
                    m_Properties[0].floatValue = e.newValue.x;
                    m_Properties[0].serializedObject.ApplyModifiedProperties();
                }

                if (m_DirtyY)
                {
                    m_Properties[1].floatValue = e.newValue.y;
                    m_Properties[1].serializedObject.ApplyModifiedProperties();
                }

                m_DirtyX = m_DirtyY = false;
            });
        }

        public void Update()
        {
            value = new Vector2(m_Properties[0]?.floatValue ?? default, m_Properties[1]?.floatValue ?? default);

            if (m_Properties[0] != null)
                fields[0]?.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(fields[0], m_Properties[0]));

            if (m_Properties[1] != null)
                fields[1]?.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(fields[1], m_Properties[1]));
        }

        static BaseField<TValue> MockPropertyField<TValue>(BaseField<TValue> field, GUIContent content, SerializedProperty property)
        {
            field.label = content.text;
            field.tooltip = content.tooltip;
            field.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            BindingsStyleHelpers.RegisterRightClickMenu(field, property);
            return field;
        }
    }
}
