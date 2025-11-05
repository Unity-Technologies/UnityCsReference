// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using BackgroundPosition = UnityEngine.UIElements.BackgroundPosition;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundPosition.
    /// </summary>
    internal class StyleBackgroundPositionField : StylePropertyField<StyleBackgroundPosition, BackgroundPositionStyleField, BackgroundPosition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleBackgroundPosition, BackgroundPositionStyleField, BackgroundPosition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleBackgroundPosition, BackgroundPositionStyleField, BackgroundPosition>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleBackgroundPositionField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-position-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundPositionField() : this(null) {}

        public StyleBackgroundPositionField(string label) : base(label, new BackgroundPositionStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override BackgroundPositionStyleField CreateValueField()
        {
            return new BackgroundPositionStyleField();
        }

        protected override StyleBackgroundPosition CreateStyleValue(BackgroundPosition v)
        {
            return v;
        }
    }
}
