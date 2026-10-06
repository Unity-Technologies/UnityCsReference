// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal sealed partial class BorderRadiusFoldout : BorderFoldout
    {
        static readonly string[] k_PropertyNames =
        {
            "borderTopLeftRadius",
            "borderTopRightRadius",
            "borderBottomRightRadius",
            "borderBottomLeftRadius"
        };

        static readonly string[] k_Labels = { "Top-Left", "Top-Right", "Bottom-Right", "Bottom-Left" };

        static readonly string[] k_Tooltips =
        {
            "<b>USS property: border-top-left-radius</b>\nThe radius of the top-left corner when a rounded rectangle is drawn in the element's box.",
            "<b>USS property: border-top-right-radius</b>\nThe radius of the top-right corner when a rounded rectangle is drawn in the element's box.",
            "<b>USS property: border-bottom-right-radius</b>\nThe radius of the bottom-right corner when a rounded rectangle is drawn in the element's box.",
            "<b>USS property: border-bottom-left-radius</b>\nThe radius of the bottom-left corner when a rounded rectangle is drawn in the element's box."
        };

        protected override IReadOnlyList<string> propertyNames => k_PropertyNames;
        protected override IReadOnlyList<string> fieldLabels => k_Labels;
        protected override IReadOnlyList<string> fieldTooltips => k_Tooltips;

        public StyleLengthField topLeftField => fields[0];
        public StyleLengthField topRightField => fields[1];
        public StyleLengthField bottomRightField => fields[2];
        public StyleLengthField bottomLeftField => fields[3];

        public BorderRadiusFoldout() : this("Radius") { }

        public BorderRadiusFoldout(string text) : base(text) { }
    }
}
