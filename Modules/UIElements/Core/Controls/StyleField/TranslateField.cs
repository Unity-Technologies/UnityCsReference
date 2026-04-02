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
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
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
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of the composite field in elements of this type.
        /// </summary>
        static readonly string compositeUssClassName = "unity-composite-field";
        static readonly UniqueStyleString compositeUssClassNameUnique = new(compositeUssClassName);

        /// <summary>
        /// USS class name of the visual input in elements of this type.
        /// </summary>
        static readonly string compositeFieldInputUssClassName = compositeUssClassName + "__input";
        static readonly UniqueStyleString compositeFieldInputUssClassNameUnique = new(compositeFieldInputUssClassName);

        /// <summary>
        /// USS class name of the fields that make up the composite field in elements of this type.
        /// </summary>
        static readonly string compositeFieldUssClassName = compositeUssClassName + "__field";
        static readonly UniqueStyleString compositeFieldUssClassNameUnique = new(compositeFieldUssClassName);

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
            AddToClassList(ussClassNameUnique);
            AddToClassList(compositeUssClassNameUnique);
            visualInput.AddToClassList(compositeFieldInputUssClassNameUnique);

            m_XField = new LengthField("X").WithClassList(compositeFieldUssClassNameUnique);
            m_YField = new LengthField("Y").WithClassList(compositeFieldUssClassNameUnique);
            m_ZField = new FloatField("Z").WithClassList(compositeFieldUssClassNameUnique);
            visualInput.Add(m_XField);
            visualInput.Add(m_YField);
            visualInput.Add(m_ZField);

            m_XField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.x)
                {
                    value = new Translate(e.newValue, value.y, value.z);
                }
            });

            m_YField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.y)
                {
                    value = new Translate(value.x, e.newValue, value.z);
                }
            });

            m_ZField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.z)
                {
                    value = new Translate(value.x, value.y, e.newValue);
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
