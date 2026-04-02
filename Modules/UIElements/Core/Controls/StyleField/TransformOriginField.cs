// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for entering Transform Origin.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class TransformOriginField : BaseField<TransformOrigin>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<TransformOrigin>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TransformOrigin>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TransformOriginField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-transform-origin-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// USS class name of the composite field in elements of this type.
        /// </summary>
        static readonly string compositeUssClassName = "unity-composite-field";
        internal static readonly UniqueStyleString compositeUssClassNameUnique = new(compositeUssClassName);

        /// <summary>
        /// USS class name of fields in elements of this type.
        /// </summary>
        static readonly string fieldUssClassName = compositeUssClassName + "__field";
        static readonly UniqueStyleString fieldUssClassNameUnique = new(fieldUssClassName);

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
        /// Constructor.
        /// </summary>
        public TransformOriginField() : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        public TransformOriginField(string label) : base(label, null)
        {
            AddToClassList(ussClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);

            m_XField = new LengthField("X").WithClassList(fieldUssClassNameUnique);
            m_YField = new LengthField("Y").WithClassList(fieldUssClassNameUnique);
            m_ZField = new FloatField("Z").WithClassList(fieldUssClassNameUnique);

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

        public override void SetValueWithoutNotify(TransformOrigin transformOrigin)
        {
            base.SetValueWithoutNotify(transformOrigin);
            m_XField.SetValueWithoutNotify(value.x);
            m_YField.SetValueWithoutNotify(value.y);
            m_ZField.SetValueWithoutNotify(value.z);
        }
    }
}
