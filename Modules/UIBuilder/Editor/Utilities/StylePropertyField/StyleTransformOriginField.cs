// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Makes a style field for editing a StyleTransformOrigin.
    /// </summary>
    internal class StyleTransformOriginField : StylePropertyField<StyleTransformOrigin, TransformOriginField, TransformOrigin>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleTransformOrigin, TransformOriginField, TransformOrigin>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleTransformOrigin, TransformOriginField, TransformOrigin>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleTransformOriginField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-transform-origin-field";
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
        public StyleTransformOriginField()
            : base(null, new TransformOriginField()) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleTransformOriginField(string label)
            : base(label, new TransformOriginField(label))
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
