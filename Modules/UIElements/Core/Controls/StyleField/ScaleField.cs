// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for entering Scale.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [UxmlElement]
    internal partial class ScaleField : BaseField<Scale>
    {
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
                    value = new Scale(e.newValue);
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
