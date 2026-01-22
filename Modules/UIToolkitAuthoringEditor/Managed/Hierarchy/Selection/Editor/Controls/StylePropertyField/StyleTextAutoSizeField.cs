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
    /// Makes a style field for editing a StyleTextAutoSize.
    /// </summary>
    internal class StyleTextAutoSizeField : StylePropertyField<StyleTextAutoSize, TextAutoSizeStyleField, TextAutoSize>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleTextAutoSize, TextAutoSizeStyleField, TextAutoSize>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleTextAutoSize, TextAutoSizeStyleField, TextAutoSize>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleTextAutoSizeField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-style-text-auto-size-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleTextAutoSizeField() : this(null) {}

        public StyleTextAutoSizeField(string label) : base(label, new TextAutoSizeStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override TextAutoSizeStyleField CreateValueField()
        {
            return new TextAutoSizeStyleField();
        }

        protected override StyleTextAutoSize CreateStyleValue(TextAutoSize v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleTextAutoSize v)
        {
            return value == v;
        }
    }
}
