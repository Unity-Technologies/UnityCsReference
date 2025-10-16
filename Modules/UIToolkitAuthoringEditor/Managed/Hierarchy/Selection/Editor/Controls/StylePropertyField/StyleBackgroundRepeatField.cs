// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using BackgroundRepeat = UnityEngine.UIElements.BackgroundRepeat;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundRepeat.
    /// </summary>
    internal class StyleBackgroundRepeatField : StylePropertyField<StyleBackgroundRepeat, BackgroundRepeatStyleField, BackgroundRepeat>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleBackgroundRepeat, BackgroundRepeatStyleField, BackgroundRepeat>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleBackgroundRepeat, BackgroundRepeatStyleField, BackgroundRepeat>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleBackgroundRepeatField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-repeat-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundRepeatField() : this(null) {}

        public StyleBackgroundRepeatField(string label) : base(label, new BackgroundRepeatStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
