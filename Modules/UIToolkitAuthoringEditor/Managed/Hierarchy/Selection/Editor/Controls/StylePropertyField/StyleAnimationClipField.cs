// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
