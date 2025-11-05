// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using BackgroundSize = UnityEngine.UIElements.BackgroundSize;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundSize.
    /// </summary>
    internal class StyleBackgroundSizeField : StylePropertyField<StyleBackgroundSize, BackgroundSizeStyleField, BackgroundSize>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleBackgroundSize, BackgroundSizeStyleField, BackgroundSize>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleBackgroundSize, BackgroundSizeStyleField, BackgroundSize>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleBackgroundSizeField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-size-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundSizeField() : this(null) {}

        public StyleBackgroundSizeField(string label) : base(label, new BackgroundSizeStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override BackgroundSizeStyleField CreateValueField()
        {
            return new BackgroundSizeStyleField();
        }

        protected override StyleBackgroundSize CreateStyleValue(BackgroundSize v)
        {
            return v;
        }
    }
}
