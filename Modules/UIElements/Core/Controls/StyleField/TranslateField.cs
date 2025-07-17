// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for entering Translate.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class TranslateField : BaseField<Translate>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Translate>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Translate>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TranslateField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-translate-field";
        /// <summary>
        /// USS class name of the composite field in elements of this type.
        /// </summary>
        static readonly string compositeUssClassName = "unity-composite-field";
        /// <summary>
        /// USS class name of the fields that make up the composite field in elements of this type.
        /// </summary>
        static readonly string compositeFieldUssClassName = compositeUssClassName + "__field";

        LengthField m_XField;
        LengthField m_YField;
        FloatField m_ZField;

        /// <summary>
        /// Returns the LengthField for the X component.
        /// </summary>
        public LengthField xField => m_XField;
        /// <summary>
        /// Returns the LengthField for the Y component.
        /// </summary>
        public LengthField yField => m_YField;
        /// <summary>
        /// Returns the FloatField for the Z component.
        /// </summary>
        public FloatField zField => m_ZField;

        /// <summary>
        /// Constructs a TranslateField.
        /// </summary>
        public TranslateField()
            : this(null) { }

        /// <summary>
        /// Constructs a TranslateField.
        /// </summary>
        /// <param name="label">The text to use as a label for the field.</param>
        public TranslateField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);

            m_XField = new LengthField("X") { classList = { compositeFieldUssClassName } };
            m_YField = new LengthField("Y") { classList = { compositeFieldUssClassName } };
            m_ZField = new FloatField("Z")  { classList = { compositeFieldUssClassName } };
            visualInput.Add(m_XField);
            visualInput.Add(m_YField);
            visualInput.Add(m_ZField);

            m_XField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.x)
                {
                    var newVal = value;
                    newVal.x = e.newValue;
                    value = newVal;
                }
            });

            m_YField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.y)
                {
                    var newVal = value;
                    newVal.y = e.newValue;
                    value = newVal;
                }
            });

            m_ZField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.z)
                {
                    var newVal = value;
                    newVal.z = e.newValue;
                    value = newVal;
                }
            });
        }

        public override void SetValueWithoutNotify(Translate t)
        {
            base.SetValueWithoutNotify(t);
            m_XField.SetValueWithoutNotify(value.x);
            m_YField.SetValueWithoutNotify(value.y);
            m_ZField.SetValueWithoutNotify(value.z);
        }

        public override string ToString()
        {
            return $"(x:{m_XField.value}, y:{m_YField.value}, z:{m_ZField.value})";
        }
    }
}
