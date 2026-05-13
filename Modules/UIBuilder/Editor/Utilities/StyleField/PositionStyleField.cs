// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    partial class PositionStyleField : DimensionStyleField
    {
        static readonly string k_FieldClassName = "unity-position-style-field";

        public PositionAnchorPoint point { get; set; }

        public PositionStyleField() : this(null) { }

        public PositionStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshAnchor(newValue);
        }

        private void RefreshAnchor(string newValue)
        {
            if (point != null)
                point.SetValueWithoutNotify(newValue != StyleFieldConstants.KeywordAuto);
        }
    }
}
