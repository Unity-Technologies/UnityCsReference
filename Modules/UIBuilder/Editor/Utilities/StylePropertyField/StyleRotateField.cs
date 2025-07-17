// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Makes a style field for editing a StyleRotate.
    /// </summary>
    internal class StyleRotateField : StylePropertyField<StyleRotate, RotateField, Rotate>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleRotate, RotateField, Rotate>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleRotate, RotateField, Rotate>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleRotateField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rotate-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructor.
        /// </summary>
        public StyleRotateField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleRotateField(string label) : base(label, new RotateField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
