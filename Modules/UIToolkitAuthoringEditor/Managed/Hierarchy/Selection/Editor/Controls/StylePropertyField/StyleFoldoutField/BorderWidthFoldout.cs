// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal sealed partial class BorderWidthFoldout : BorderFoldout
    {
        static readonly string[] k_PropertyNames =
        {
            "borderTopWidth",
            "borderRightWidth",
            "borderBottomWidth",
            "borderLeftWidth"
        };

        static readonly string[] k_Labels = { "Top", "Right", "Bottom", "Left" };

        static readonly string[] k_Tooltips =
        {
            "<b>USS property: border-top-width</b>\nSpace reserved for the top edge of the border during the layout phase.",
            "<b>USS property: border-right-width</b>\nSpace reserved for the right edge of the border during the layout phase.",
            "<b>USS property: border-bottom-width</b>\nSpace reserved for the bottom edge of the border during the layout phase.",
            "<b>USS property: border-left-width</b>\nSpace reserved for the left edge of the border during the layout phase."
        };

        static readonly string[] k_ValidationSyntaxes =
        {
            "border-top-width",
            "border-right-width",
            "border-bottom-width",
            "border-left-width"
        };

        protected override IReadOnlyList<string> propertyNames => k_PropertyNames;
        protected override IReadOnlyList<string> fieldLabels => k_Labels;
        protected override IReadOnlyList<string> fieldTooltips => k_Tooltips;

        protected override StylePropertyValidation GetValidation(int index) => new Syntax(k_ValidationSyntaxes[index]);

        public StyleLengthField topField => fields[0];
        public StyleLengthField rightField => fields[1];
        public StyleLengthField bottomField => fields[2];
        public StyleLengthField leftField => fields[3];

        public BorderWidthFoldout() : this("Width") { }

        public BorderWidthFoldout(string text) : base(text) { }
    }
}
