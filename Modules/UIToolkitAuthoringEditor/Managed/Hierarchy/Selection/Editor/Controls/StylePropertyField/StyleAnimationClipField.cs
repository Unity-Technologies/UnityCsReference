// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class StyleUIAnimationClipField : StylePropertyField<StyleUIAnimationClip, UIAnimationClipField, UIAnimationClip>
    {
        public new static readonly string ussClassName = "unity-animation-clip-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleUIAnimationClipField() : this(null) {}

        public StyleUIAnimationClipField(string label) : base(label, new UIAnimationClipField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override UIAnimationClipField CreateValueField()
        {
            return new UIAnimationClipField();
        }

        protected override StyleUIAnimationClip CreateStyleValue(UIAnimationClip v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleUIAnimationClip v)
        {
            return value == v;
        }
    }
}
