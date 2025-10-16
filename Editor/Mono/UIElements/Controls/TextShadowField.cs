// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering TextShadow.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class TextShadowField : BaseField<TextShadow>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<TextShadow>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TextShadow>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new TextShadowField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-text-shadow-field";

        ColorField m_ColorField;
        FloatField m_BlurRadiusField;
        Vector2Field m_OffsetField;

        public ColorField colorField => m_ColorField;
        public FloatField blurRadiusField => m_BlurRadiusField;
        public Vector2Field offsetField => m_OffsetField;

        public TextShadowField() : this(null) { }

        public TextShadowField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);

            m_ColorField = new ColorField("Color");
            m_BlurRadiusField = new FloatField("Blur Radius");
            m_OffsetField = new Vector2Field("Offset");

            m_BlurRadiusField.AddToClassList(alignedFieldUssClassName);
            m_OffsetField.AddToClassList(alignedFieldUssClassName);
            m_ColorField.AddToClassList(alignedFieldUssClassName);

            visualInput.Add(m_ColorField);
            visualInput.Add(m_BlurRadiusField);
            visualInput.Add(m_OffsetField);

            m_OffsetField.fields[0].label = "Horizontal";
            m_OffsetField.fields[1].label = "Vertical";

            m_ColorField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.color)
                {
                    var newVal = value;
                    newVal.color = e.newValue;
                    value = newVal;
                }
            });

            m_BlurRadiusField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.blurRadius)
                {
                    var newVal = value;
                    newVal.blurRadius = e.newValue;
                    value = newVal;
                }
            });

            m_OffsetField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.offset)
                {
                    var newVal = value;
                    newVal.offset = e.newValue;
                    value = newVal;
                }
            });
        }

        public override void SetValueWithoutNotify(TextShadow textShadow)
        {
            base.SetValueWithoutNotify(textShadow);
            m_ColorField.SetValueWithoutNotify(value.color);
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius);
            m_OffsetField.SetValueWithoutNotify(value.offset);
        }
    }

}
