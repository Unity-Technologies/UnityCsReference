// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleFloat.
    /// </summary>
    internal class StyleFloatField : StylePropertyField<StyleFloat, FloatField, float>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleFloat, FloatField, float>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleFloat, FloatField, float>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleFloatField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-float-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleFloatField() : this(null) {}

        public StyleFloatField(string label) : base(label, new FloatField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override FloatField CreateValueField()
        {
            return new FloatField();
        }

        protected override StyleFloat CreateStyleValue(float v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleFloat v)
        {
            return value == v;
        }
    }
}
