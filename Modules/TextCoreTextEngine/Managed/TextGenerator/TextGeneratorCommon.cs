// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Horizontal text alignment options.
    /// </summary>
    [Flags]
    enum HorizontalAlignment
    {
        Left        = 0x1,
        Center      = 0x2,
        Right       = 0x4,
        Justified   = 0x8,
        Flush       = 0x10,
        Geometry    = 0x20
    }

    /// <summary>
    /// Vertical text alignment options.
    /// </summary>
    [Flags]
    enum VerticalAlignment
    {
        Top         = 0x100,
        Middle      = 0x200,
        Bottom      = 0x400,
        Baseline    = 0x800,
        Midline     = 0x1000,
        Capline     = 0x2000,
    }

    /// <summary>
    /// Text alignment options.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal enum TextAlignment
    {
        TopLeft = HorizontalAlignment.Left | VerticalAlignment.Top,
        TopCenter = HorizontalAlignment.Center | VerticalAlignment.Top,
        TopRight = HorizontalAlignment.Right | VerticalAlignment.Top,
        TopJustified = HorizontalAlignment.Justified | VerticalAlignment.Top,
        TopFlush = HorizontalAlignment.Flush | VerticalAlignment.Top,
        TopGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Top,

        MiddleLeft = HorizontalAlignment.Left | VerticalAlignment.Middle,
        MiddleCenter = HorizontalAlignment.Center | VerticalAlignment.Middle,
        MiddleRight = HorizontalAlignment.Right | VerticalAlignment.Middle,
        MiddleJustified = HorizontalAlignment.Justified | VerticalAlignment.Middle,
        MiddleFlush = HorizontalAlignment.Flush | VerticalAlignment.Middle,
        MiddleGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Middle,

        BottomLeft = HorizontalAlignment.Left | VerticalAlignment.Bottom,
        BottomCenter = HorizontalAlignment.Center | VerticalAlignment.Bottom,
        BottomRight = HorizontalAlignment.Right | VerticalAlignment.Bottom,
        BottomJustified = HorizontalAlignment.Justified | VerticalAlignment.Bottom,
        BottomFlush = HorizontalAlignment.Flush | VerticalAlignment.Bottom,
        BottomGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Bottom,

        BaselineLeft = HorizontalAlignment.Left | VerticalAlignment.Baseline,
        BaselineCenter = HorizontalAlignment.Center | VerticalAlignment.Baseline,
        BaselineRight = HorizontalAlignment.Right | VerticalAlignment.Baseline,
        BaselineJustified = HorizontalAlignment.Justified | VerticalAlignment.Baseline,
        BaselineFlush = HorizontalAlignment.Flush | VerticalAlignment.Baseline,
        BaselineGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Baseline,

        MidlineLeft = HorizontalAlignment.Left | VerticalAlignment.Midline,
        MidlineCenter = HorizontalAlignment.Center | VerticalAlignment.Midline,
        MidlineRight = HorizontalAlignment.Right | VerticalAlignment.Midline,
        MidlineJustified = HorizontalAlignment.Justified | VerticalAlignment.Midline,
        MidlineFlush = HorizontalAlignment.Flush | VerticalAlignment.Midline,
        MidlineGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Midline,

        CaplineLeft = HorizontalAlignment.Left | VerticalAlignment.Capline,
        CaplineCenter = HorizontalAlignment.Center | VerticalAlignment.Capline,
        CaplineRight = HorizontalAlignment.Right | VerticalAlignment.Capline,
        CaplineJustified = HorizontalAlignment.Justified | VerticalAlignment.Capline,
        CaplineFlush = HorizontalAlignment.Flush | VerticalAlignment.Capline,
        CaplineGeoAligned = HorizontalAlignment.Geometry | VerticalAlignment.Capline,
    }

    /// <summary>
    /// Font styles and text decorations
    /// </summary>
    [Flags]
    public enum FontStyles
    {
        Normal          = 0x0,
        Bold            = 0x1,
        Italic          = 0x2,
        Underline       = 0x4,
        LowerCase       = 0x8,
        UpperCase       = 0x10,
        SmallCaps       = 0x20,
        Strikethrough   = 0x40,
        Superscript     = 0x80,
        Subscript       = 0x100,
        Highlight       = 0x200,
    }

    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal enum TextOverflowMode { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5, Linked = 6 }
    
    internal enum TextureMapping { Character = 0, Line = 1, Paragraph = 2, MatchAspect = 3 }
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal enum TextWrappingMode { NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 };
    internal enum TextInputSource { TextInputBox = 0, SetText = 1, SetTextArray = 2, TextString = 3 };
}
