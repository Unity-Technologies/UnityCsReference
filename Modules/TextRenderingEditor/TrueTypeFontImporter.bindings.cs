// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum FontTextureCase
    {
        Dynamic = -2,
        Unicode = -1,
        [InspectorName("ASCII default set")]
        ASCII = 0,
        ASCIIUpperCase = 1,
        ASCIILowerCase = 2,
        CustomSet = 3
    }

    public enum FontRenderingMode
    {
        Smooth = 0,
        HintedSmooth = 1,
        HintedRaster = 2,
        OSDefault = 3,
    }

    public enum AscentCalculationMode
    {
        [InspectorName("Legacy version 2 mode (glyph bounding boxes)")]
        Legacy2x = 0,
        [InspectorName("Face ascender metric")]
        FaceAscender = 1,
        [InspectorName("Face bounding box metric")]
        FaceBoundingBox = 2
    }

    [NativeHeader("Modules/TextRenderingEditor/TrueTypeFontImporter.h")]
    public sealed class TrueTypeFontImporter : AssetImporter
    {
        public extern int fontSize { get; set; }
        public extern bool includeFontData { get; set; }
        public extern AscentCalculationMode ascentCalculationMode { get; set; }
        public extern string customCharacters { get; set; }
        public extern int characterSpacing { get; set; }
        public extern int characterPadding { get; set; }
        public extern FontRenderingMode fontRenderingMode { get; set; }
        public extern bool shouldRoundAdvanceValue { get; set; }

        [NativeProperty("FontNameFromTTFData", false, TargetType.Function)] public extern string fontTTFName { get; }
        [NativeProperty("ForceTextureCase", false, TargetType.Function)] public extern FontTextureCase fontTextureCase { get; set; }

        [NativeProperty("MarshalledFontReferences", false, TargetType.Function)] public extern Font[] fontReferences { get; set; }
        [NativeProperty("MarshalledFontNames", false, TargetType.Function)] public extern string[] fontNames { get; set; }

        internal extern bool IsFormatSupported();
        public extern Font GenerateEditableFont(string path);

        internal extern Font[] MarshalledLookupFallbackFontReferences(string[] names);
        internal Font[] LookupFallbackFontReferences(string[] names)
        {
            return MarshalledLookupFallbackFontReferences(names);
        }
    }
}
