// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Make a style field for editing an Aspect Ratio.
    /// </summary>
    internal class StyleRatioField : StylePropertyField<StyleRatio, RatioField, Ratio>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleRatio, RatioField, Ratio>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleRatio, RatioField, Ratio>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleRatioField();
        }
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-ratio-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleRatioField() : this(null) {}

        public StyleRatioField(string label) : base(label, new RatioField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override RatioField CreateValueField()
        {
            return new RatioField();
        }

        protected override StyleRatio CreateStyleValue(Ratio v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleRatio v)
        {
            return value == v;
        }
    }
}

