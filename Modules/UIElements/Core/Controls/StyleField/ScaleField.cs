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
    /// Makes a field for entering Scale.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ScaleField : BaseField<Scale>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Scale>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Scale>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new ScaleField();
        }

        Vector3Field m_VectorField;

        // Returns the Vector3Field for the Scale component.
        public Vector3Field vectorField => m_VectorField;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScaleField() : this(null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The Label text.</param>
        public ScaleField(string label)
            : base(label, null)
        {
            m_VectorField = new Vector3Field();

            visualInput.Add(m_VectorField);

            m_VectorField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.value)
                {
                    var newVal = value;
                    newVal.value = e.newValue;
                    value = newVal;
                }
            });

            SetValueWithoutNotify(Scale.Initial());
        }

        public override void SetValueWithoutNotify(Scale scale)
        {
            base.SetValueWithoutNotify(scale);
            m_VectorField.SetValueWithoutNotify(value.value);
        }
    }
}
