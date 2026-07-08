// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.LowLevel
{
    /// <summary>
    /// Flags taken from freetype.h
    /// </summary>
    [UsedByNativeCode]
    [Flags]
    public enum GlyphLoadFlags
    {
        LOAD_DEFAULT = 0,
        LOAD_NO_SCALE = 1 << 0,
        LOAD_NO_HINTING = 1 << 1,
        LOAD_RENDER = 1 << 2,
        LOAD_NO_BITMAP = 1 << 3,
        //LOAD_VERTICAL_LAYOUT = 1 << 4,
        LOAD_FORCE_AUTOHINT = 1 << 5,
        //LOAD_CROP_BITMAP = 1 << 6,
        //LOAD_PEDANTIC = 1 << 7,
        //LOAD_IGNORE_GLOBAL_ADVANCE_WIDTH = 1 << 9,
        //LOAD_NO_RECURSE = 1 << 10,
        //LOAD_IGNORE_TRANSFORM = 1 << 11,
        LOAD_MONOCHROME = 1 << 12,
        //LOAD_LINEAR_DESIGN = 1 << 13,
        LOAD_NO_AUTOHINT = 1 << 15,
        /* Bits 16-19 are used by `LOAD_TARGET_' */
        LOAD_COLOR = 1 << 20,
        LOAD_COMPUTE_METRICS = 1 << 21,
        LOAD_BITMAP_METRICS_ONLY = 1 << 22
    }

    /// <summary>
    /// Rasterizing modes used by the Font Engine to raster glyphs.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    [Flags]
    internal enum GlyphRasterModes
    {
        RASTER_MODE_8BIT        = 0x1,
        RASTER_MODE_MONO        = 0x2,

        RASTER_MODE_NO_HINTING  = 0x4,
        RASTER_MODE_HINTED      = 0x8,

        RASTER_MODE_BITMAP      = 0x10,
        RASTER_MODE_SDF         = 0x20,
        RASTER_MODE_SDFAA       = 0x40,
        RASTER_MODE_STRONG      = 0x80,

        RASTER_MODE_MSDF        = 0x100,
        RASTER_MODE_MSDFA       = 0x200,
        // Reserved             = 0x400,
        // Reserved             = 0x800,

        RASTER_MODE_1X          = 0x1000,
        RASTER_MODE_8X          = 0x2000,
        RASTER_MODE_16X         = 0x4000,
        RASTER_MODE_32X         = 0x8000,

        RASTER_MODE_COLOR       = 0x10000,
    }

    /// <summary>
    /// Error codes returned and relevant to the various FontEngine functions.
    /// Source codes are located in fterrdef.h
    /// </summary>
    public enum FontEngineError
    {
        Success                 = 0x0,

        // Font file structure, type or path related errors.
        Invalid_File_Path       = 0x1,
        Invalid_File_Format     = 0x2,
        Invalid_File_Structure  = 0x3,
        Invalid_File            = 0x4,
        Invalid_Table           = 0x8,

        // Glyph related errors.
        Invalid_Glyph_Index     = 0x10,
        Invalid_Character_Code  = 0x11,
        Invalid_Pixel_Size      = 0x17,

        //
        Invalid_Library         = 0x21,

        // Font face related errors.
        Invalid_Face            = 0x23,

        Invalid_Library_or_Face = 0x29,

        // Font atlas generation and glyph rendering related errors.
        Atlas_Generation_Cancelled  = 0x64,
        Invalid_SharedTextureData   = 0x65,
        Invalid_FontFaceHandle      = 0x66,

        // OpenType Layout related errors.
        OpenTypeLayoutLookup_Mismatch = 0x74,

        // Additional errors codes will be added as necessary to cover new FontEngine features and functionality.
    }

    /// <summary>
    /// Rendering modes used by the Font Engine to render glyphs.
    /// </summary>
    [UsedByNativeCode]
    public enum GlyphRenderMode
    {
        DEFAULT         = 0x0, // Selects either SDFAA or COLOR based on the font face.

        SMOOTH_HINTED   = GlyphRasterModes.RASTER_MODE_HINTED     | GlyphRasterModes.RASTER_MODE_8BIT  | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        SMOOTH          = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_8BIT  | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        COLOR_HINTED    = GlyphRasterModes.RASTER_MODE_HINTED     | GlyphRasterModes.RASTER_MODE_COLOR | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        COLOR           = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_COLOR | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        RASTER_HINTED   = GlyphRasterModes.RASTER_MODE_HINTED     | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        RASTER          = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        SDF             = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_SDF    | GlyphRasterModes.RASTER_MODE_1X,
        SDF8            = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_SDF    | GlyphRasterModes.RASTER_MODE_8X,
        SDF16           = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_SDF    | GlyphRasterModes.RASTER_MODE_16X,
        SDF32           = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO  | GlyphRasterModes.RASTER_MODE_SDF    | GlyphRasterModes.RASTER_MODE_32X,

        SDFAA_HINTED    = GlyphRasterModes.RASTER_MODE_HINTED     | GlyphRasterModes.RASTER_MODE_8BIT  | GlyphRasterModes.RASTER_MODE_SDFAA  | GlyphRasterModes.RASTER_MODE_1X,
        SDFAA           = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_8BIT  | GlyphRasterModes.RASTER_MODE_SDFAA  | GlyphRasterModes.RASTER_MODE_1X,
    }

    // This enum needs to match GlyphRenderMode, except for DEFAULT being removed.
    internal enum GlyphRenderModeUI
    {
        SMOOTH_HINTED = GlyphRenderMode.SMOOTH_HINTED,
        SMOOTH = GlyphRenderMode.SMOOTH,
        COLOR_HINTED = GlyphRenderMode.COLOR_HINTED,
        COLOR = GlyphRenderMode.COLOR,
        RASTER_HINTED = GlyphRenderMode.RASTER_HINTED,
        RASTER = GlyphRenderMode.RASTER,
        SDF = GlyphRenderMode.SDF,
        SDF8 = GlyphRenderMode.SDF8,
        SDF16 = GlyphRenderMode.SDF16,
        SDF32 = GlyphRenderMode.SDF32,
        SDFAA_HINTED = GlyphRenderMode.SDFAA_HINTED,
        SDFAA = GlyphRenderMode.SDFAA
    }

    /// <summary>
    /// Editor-internal render modes that complement <see cref="GlyphRenderMode"/> but must not be exposed publicly.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal static class GlyphRenderModeInternal
    {
        // Only used by the editor's bitmap mode alongside the OS settings to disable the font smoothing.
        public const GlyphRenderMode RASTER_HINTED_STRONG = (GlyphRenderMode)(GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_STRONG | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X);
    }

    /// <summary>
    /// The modes available when packing glyphs into an atlas texture.
    /// </summary>
    [UsedByNativeCode]
    public enum GlyphPackingMode
    {
        BestShortSideFit    = 0x0,
        BestLongSideFit     = 0x1,
        BestAreaFit         = 0x2,
        BottomLeftRule      = 0x3,
        ContactPointRule    = 0x4,
    }

    /// <summary>
    /// A structure that contains information about a system font.
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [System.Diagnostics.DebuggerDisplay("{familyName} - {styleName}")]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct FontReference
    {
        /// <summary>
        /// The family name of the font face.
        /// </summary>
        public string familyName;

        /// <summary>
        /// The style name of the font face.
        /// </summary>
        public string styleName;

        /// <summary>
        /// The index of the font face.
        /// </summary>
        public int faceIndex;

        /// <summary>
        /// The file path of the font file.
        /// </summary>
        public string filePath;
    }

    [NativeHeader("Modules/TextCoreFontEngine/Native/FontEngine.h")]
    public sealed class FontEngine
    {
        private static Glyph[] s_Glyphs = new Glyph[16];
        private static uint[] s_GlyphIndexes_MarshallingArray_A;
        private static uint[] s_GlyphIndexes_MarshallingArray_B;

        private static GlyphMarshallingStruct[] s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[16];
        private static GlyphMarshallingStruct[] s_GlyphMarshallingStruct_OUT = new GlyphMarshallingStruct[16];

        private static GlyphRect[] s_FreeGlyphRects = new GlyphRect[16];
        private static GlyphRect[] s_UsedGlyphRects = new GlyphRect[16];

        private static GlyphAdjustmentRecord[] s_SingleAdjustmentRecords_MarshallingArray;

        private static SingleSubstitutionRecord[] s_SingleSubstitutionRecords_MarshallingArray;
        private static MultipleSubstitutionRecord[] s_MultipleSubstitutionRecords_MarshallingArray;
        private static AlternateSubstitutionRecord[] s_AlternateSubstitutionRecords_MarshallingArray;
        private static LigatureSubstitutionRecord[] s_LigatureSubstitutionRecords_MarshallingArray;
        private static ContextualSubstitutionRecord[] s_ContextualSubstitutionRecords_MarshallingArray;
        private static ChainingContextualSubstitutionRecord[] s_ChainingContextualSubstitutionRecords_MarshallingArray;

        private static GlyphPairAdjustmentRecord[] s_PairAdjustmentRecords_MarshallingArray;
        private static MarkToBaseAdjustmentRecord[] s_MarkToBaseAdjustmentRecords_MarshallingArray;
        private static MarkToMarkAdjustmentRecord[] s_MarkToMarkAdjustmentRecords_MarshallingArray;
        private static MarkToLigatureAdjustmentRecord[] s_MarkToLigatureAdjustmentRecords_MarshallingArray;

        private static Dictionary<uint, Glyph> s_GlyphLookupDictionary = new Dictionary<uint, Glyph>();

        static readonly ProfilerMarker s_TryAddGlyphsToTextureMarker = new ProfilerMarker("FontEngine.TryAddGlyphsToTexture");

        /// <summary>
        ///
        /// </summary>
        internal FontEngine() {}

        /// <summary>
        /// Initialize the Font Engine and library.
        /// </summary>
        /// <returns>Returns a value of zero if the initialization of the Font Engine was successful.</returns>
        public static FontEngineError InitializeFontEngine()
        {
            return (FontEngineError)InitializeFontEngine_Internal();
        }

        [NativeMethod(Name = "TextCore::FontEngine::InitFontEngine", IsFreeFunction = true)]
        static extern int InitializeFontEngine_Internal();


        /// <summary>
        /// Destroy and unload resources used by the Font Engine.
        /// </summary>
        /// <returns>Returns a value of zero if the Font Engine and used resources were successfully released.</returns>
        public static FontEngineError DestroyFontEngine()
        {
            return (FontEngineError)DestroyFontEngine_Internal();
        }

        [NativeMethod(Name = "TextCore::FontEngine::DestroyFontEngine", IsFreeFunction = true)]
        static extern int DestroyFontEngine_Internal();


        /// <summary>
        /// Force the cancellation of any atlas or glyph rendering process.
        /// Used primarily by the Font Asset Creator.
        /// </summary>
        internal static void SendCancellationRequest()
        {
            SendCancellationRequest_Internal();
        }

        [NativeMethod(Name = "TextCore::FontEngine::SendCancellationRequest", IsFreeFunction = true)]
        static extern void SendCancellationRequest_Internal();


        /// <summary>
        /// Determines if the font engine is currently in the process of rendering and adding glyphs into an atlas texture.
        /// Used primarily by the Font Asset Creator.
        /// </summary>
        internal static extern bool isProcessingDone
        {
            [NativeMethod(Name = "TextCore::FontEngine::GetIsProcessingDone", IsFreeFunction = true)]
            get;
        }


        /// <summary>
        /// Returns the generation progress on glyph packing and rendering.
        /// Used primarily by the Font Asset Creator.
        /// </summary>
        internal static extern float generationProgress
        {
            [NativeMethod(Name = "TextCore::FontEngine::GetGenerationProgress", IsFreeFunction = true)]
            get;
        }


        /// <summary>
        /// Loads the source font file at the given file path.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string filePath)
        {
            return (FontEngineError)LoadFontFace_Internal(filePath);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_Internal(string filePath);


        /// <summary>
        /// Loads the font file at the given file path and set it size to the specified point size.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>A value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string filePath, int pointSize)
        {
            return (FontEngineError)LoadFontFace_With_Size_Internal(filePath, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_Internal(string filePath, int pointSize);

        /// <summary>
        /// Loads the font file at the given file path and set it size and face index to the specified values.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load. When the font file is a TrueType collection (.TTC), this specifies the face index of the font face to load. If the font file is a TrueType Font (.TTF) or OpenType Font (.OTF) file, the face index is always 0.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string filePath, float pointSize, int faceIndex)
        {
            return (FontEngineError)LoadFontFace_With_Size_And_FaceIndex_Internal(filePath, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_And_FaceIndex_Internal(string filePath, int pointSize, int faceIndex);

        /// <summary>
        /// Loads the font file at the given file path and returns a handle that identifies the loaded face.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load.</param>
        /// <param name="faceHandle">A handle that identifies the loaded face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadFontFace(string filePath, float pointSize, int faceIndex, out FontFaceHandle faceHandle)
        {
            return (FontEngineError)LoadFontFace_With_Size_And_FaceIndex_FontFaceHandle_Internal(filePath, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex, out faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_And_FaceIndex_FontFaceHandle_Internal(string filePath, int pointSize, int faceIndex, out FontFaceHandle faceHandle);


        /// <summary>
        /// Loads the font file from the provided byte array.
        /// </summary>
        /// <param name="sourceFontFile">The byte array that contains the source font file.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(byte[] sourceFontFile)
        {
            if (sourceFontFile.Length == 0)
                return FontEngineError.Invalid_File;

            return (FontEngineError)LoadFontFace_FromSourceFontFile_Internal(sourceFontFile);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_FromSourceFontFile_Internal(byte[] sourceFontFile);


        /// <summary>
        /// Loads the font file from the provided byte array and set its size to the given point size.
        /// </summary>
        /// <param name="sourceFontFile">The byte array that contains the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(byte[] sourceFontFile, int pointSize)
        {
            if (sourceFontFile.Length == 0)
                return FontEngineError.Invalid_File;

            return (FontEngineError)LoadFontFace_With_Size_FromSourceFontFile_Internal(sourceFontFile, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FromSourceFontFile_Internal(byte[] sourceFontFile, int pointSize);

        /// <summary>
        /// Loads the font file from the provided byte array and set its size and face index to the specified values.
        /// </summary>
        /// <param name="sourceFontFile">The byte array that contains the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load. When the font file is a TrueType collection (.TTC), this specifies the face index of the font face to load. If the font file is a TrueType Font (.TTF) or OpenType Font (.OTF) file, the face index is always 0.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(byte[] sourceFontFile, float pointSize, int faceIndex)
        {
            if (sourceFontFile.Length == 0)
                return FontEngineError.Invalid_File;

            return (FontEngineError)LoadFontFace_With_Size_And_FaceIndex_FromSourceFontFile_Internal(sourceFontFile, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_And_FaceIndex_FromSourceFontFile_Internal(byte[] sourceFontFile, int pointSize, int faceIndex);

        /// <summary>
        /// Loads the font file from the provided byte array and returns a handle that identifies the loaded face.
        /// </summary>
        /// <param name="sourceFontFile">The byte array that contains the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load.</param>
        /// <param name="faceHandle">A handle that identifies the loaded face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadFontFace(byte[] sourceFontFile, float pointSize, int faceIndex, out FontFaceHandle faceHandle)
        {
            if (sourceFontFile == null || sourceFontFile.Length == 0)
            {
                faceHandle = default;
                return FontEngineError.Invalid_File;
            }

            return (FontEngineError)LoadFontFace_With_Size_And_FaceIndex_FromSourceFontFile_FontFaceHandle_Internal(sourceFontFile, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex, out faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_And_FaceIndex_FromSourceFontFile_FontFaceHandle_Internal(byte[] sourceFontFile, int pointSize, int faceIndex, out FontFaceHandle faceHandle);


        /// <summary>
        /// Loads the font file from the Unity font's internal font data. Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(Font font)
        {
            return (FontEngineError)LoadFontFace_FromFont_Internal(font);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_FromFont_Internal(Font font);


        /// <summary>
        /// Loads the font file from the Unity font's internal font data. Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(Font font, int pointSize)
        {
            return (FontEngineError)LoadFontFace_With_Size_FromFont_Internal(font, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FromFont_Internal(Font font, int pointSize);

        /// <summary>
        /// Loads the font file from the Unity font's internal font data. Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load. When the font file is a TrueType collection (.TTC), this specifies the face index of the font face to load. If the font file is a TrueType Font (.TTF) or OpenType Font (.OTF) file, the face index is always 0.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(Font font, float pointSize, int faceIndex)
        {
            return (FontEngineError)LoadFontFace_With_Size_and_FaceIndex_FromFont_Internal(font, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_and_FaceIndex_FromFont_Internal(Font font, int pointSize, int faceIndex);

        /// <summary>
        /// Loads the font file from the Unity font's internal font data and returns a handle that identifies the loaded face.
        /// Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load.</param>
        /// <param name="faceHandle">A handle that identifies the loaded face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadFontFace(Font font, float pointSize, int faceIndex, out FontFaceHandle faceHandle)
        {
            return (FontEngineError)LoadFontFace_With_Size_and_FaceIndex_FromFont_FontFaceHandle_Internal(font, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex, out faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_and_FaceIndex_FromFont_FontFaceHandle_Internal(Font font, int pointSize, int faceIndex, out FontFaceHandle faceHandle);

        /// <summary>
        /// Loads the font face into a private cache entry identified by <paramref name="ownerId"/>, isolated
        /// from any other consumer of the same source font (it gets its own per-face lock). Intended for the
        /// Font Asset Creator so its background generation does not contend with live dynamic font assets; the
        /// caller is responsible for unloading the returned handle (see <see cref="UnloadFontFace(FontFaceHandle)"/>).
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceIndex">The face index of the font face to load.</param>
        /// <param name="ownerId">A valid, caller-unique EntityId that isolates this face's cache entry.</param>
        /// <param name="faceHandle">A handle that identifies the loaded face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadFontFace(Font font, float pointSize, int faceIndex, EntityId ownerId, out FontFaceHandle faceHandle)
        {
            return (FontEngineError)LoadFontFace_With_Size_FaceIndex_OwnerId_FromFont_FontFaceHandle_Internal(font, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), faceIndex, ownerId, out faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FaceIndex_OwnerId_FromFont_FontFaceHandle_Internal(Font font, int pointSize, int faceIndex, EntityId ownerId, out FontFaceHandle faceHandle);

        /// <summary>
        /// Loads the font file from a potential system font by family and style name.
        /// </summary>
        /// <param name="familyName">The family name of the font face to load.</param>
        /// <param name="styleName">The style name of the font face to load.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string familyName, string styleName)
        {
            return (FontEngineError)LoadFontFace_by_FamilyName_and_StyleName_Internal(familyName, styleName);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_by_FamilyName_and_StyleName_Internal(string familyName, string styleName);

        /// <summary>
        /// Loads the font file from a potential system font by family and style name at the specified point size.
        /// </summary>
        /// <param name="familyName">The family name of the font face to load.</param>
        /// <param name="styleName">The style name of the font face to load.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string familyName, string styleName, float pointSize)
        {
            return (FontEngineError)LoadFontFace_With_Size_by_FamilyName_and_StyleName_Internal(familyName, styleName, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero));
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_by_FamilyName_and_StyleName_Internal(string familyName, string styleName, int pointSize);

        /// <summary>
        /// Loads the font file from a potential system font by family and style name and returns a handle that identifies the loaded face.
        /// </summary>
        /// <param name="familyName">The family name of the font face to load.</param>
        /// <param name="styleName">The style name of the font face to load.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <param name="faceHandle">A handle that identifies the loaded face.</param>
        /// <returns>A value of zero (0) if the font face was loaded successfully.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadFontFace(string familyName, string styleName, float pointSize, out FontFaceHandle faceHandle)
        {
            return (FontEngineError)LoadFontFace_With_Size_by_FamilyName_and_StyleName_FontFaceHandle_Internal(familyName, styleName, (int)Math.Round(pointSize, MidpointRounding.AwayFromZero), out faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_by_FamilyName_and_StyleName_FontFaceHandle_Internal(string familyName, string styleName, int pointSize, out FontFaceHandle faceHandle);

        /// <summary>
        /// Unloads current font face and removes it from the cache.
        /// </summary>
        /// <returns>A value of zero (0) if the font face was successfully unloaded and removed from the cache.</returns>
        public static FontEngineError UnloadFontFace()
        {
            return (FontEngineError)UnloadFontFace_Internal();
        }

        [NativeMethod(Name = "TextCore::FontEngine::UnloadFontFace", IsFreeFunction = true)]
        static extern int UnloadFontFace_Internal();

        /// <summary>
        /// Unloads the font face identified by the given handle and removes it from the cache.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to unload.</param>
        /// <returns>A value of zero (0) if the font face was successfully unloaded and removed from the cache.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError UnloadFontFace(FontFaceHandle faceHandle)
        {
            return (FontEngineError)UnloadFontFace_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::UnloadFontFace", IsFreeFunction = true)]
        static extern int UnloadFontFace_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Unloads all currently loaded font faces and removes them from the cache.
        /// </summary>
        /// <returns>A value of zero (0) if the font faces were successfully unloaded and removed from the cache.</returns>
        public static FontEngineError UnloadAllFontFaces()
        {
            return (FontEngineError)UnloadAllFontFaces_Internal();
        }

        [NativeMethod(Name = "TextCore::FontEngine::UnloadAllFontFaces", IsFreeFunction = true)]
        static extern int UnloadAllFontFaces_Internal();


        /// <summary>
        /// Gets the family names and styles of the system fonts.
        /// </summary>
        /// <returns>Returns the names and styles of the system fonts.</returns>
        public static string[] GetSystemFontNames()
        {
            string[] fontNames = GetSystemFontNames_Internal();

            if (fontNames != null && fontNames.Length == 0)
                return null;

            return fontNames;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetSystemFontNames", IsThreadSafe = true, IsFreeFunction = true)]
        static extern string[] GetSystemFontNames_Internal();


        /// <summary>
        /// Gets references to all system fonts.
        /// </summary>
        /// <returns>An array of font references for all system fonts.</returns>
        [NativeMethod(Name = "TextCore::FontEngine::GetSystemFontReferences", IsThreadSafe = true, IsFreeFunction = true)]
        internal static extern FontReference[] GetSystemFontReferences();

        /// <summary>
        /// Returns true if the font face identified by the given handle contains color glyphs.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <returns>Returns true if the font face contains color glyphs. Otherwise returns false.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool IsColorFontFace(FontFaceHandle faceHandle)
        {
            return IsColorFontFace_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::IsColorFontFace", IsFreeFunction = true)]
        static extern bool IsColorFontFace_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule", "UnityEditor.TextCoreTextEngineModule", "Unity.FontEngine.Editor.Tests")]
        internal static extern bool IsVariableFontFace();

        /// <summary>
        /// Returns true if the font face identified by the given handle is a variable font face.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <returns>Returns true if the font face is a variable font face. Otherwise returns false.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule", "UnityEditor.TextCoreTextEngineModule", "Unity.FontEngine.Editor.Tests")]
        internal static bool IsVariableFontFace(FontFaceHandle faceHandle)
        {
            return IsVariableFontFace_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::IsVariableFontFace", IsFreeFunction = true)]
        static extern bool IsVariableFontFace_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);


        /// <summary>
        /// Try finding and returning a reference to a system font for the given family name and style.
        /// </summary>
        /// <param name="familyName">The family name of the font.</param>
        /// <param name="styleName">The style name of the font.</param>
        /// <param name="fontRef">A FontReference to the matching system font.</param>
        /// <returns>Returns true if a reference to the system font was found. Otherwise returns false.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryGetSystemFontReference(string familyName, string styleName, out FontReference fontRef)
        {
            return TryGetSystemFontReference_Internal(familyName, styleName, out fontRef);
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetSystemFontReference", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetSystemFontReference_Internal(string familyName, string styleName, out FontReference fontRef);


        /// <summary>
        /// Set the size of the currently loaded font face.
        /// </summary>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was successfully scaled to the given point size.</returns>
        public static FontEngineError SetFaceSize(int pointSize)
        {
            return (FontEngineError)SetFaceSize_Internal(pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::SetFaceSize", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int SetFaceSize_Internal(int pointSize);

        /// <summary>
        /// Set the size of the font face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to resize. Updated to reflect the new size.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was successfully scaled to the given point size.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError SetFaceSize(ref FontFaceHandle faceHandle, int pointSize)
        {
            return (FontEngineError)SetFaceSize_FontFaceHandle_Internal(ref faceHandle, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::SetFaceSize", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int SetFaceSize_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, int pointSize);


        /// <summary>
        /// Get information about the currently loaded and sized font face.
        /// </summary>
        /// <returns>Returns the FaceInfo of the currently loaded font face.</returns>
        public static FaceInfo GetFaceInfo()
        {
            FaceInfo faceInfo = new FaceInfo();

            GetFaceInfo_Internal(ref faceInfo);

            return faceInfo;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFaceInfo", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int GetFaceInfo_Internal(ref FaceInfo faceInfo);

        /// <summary>
        /// Get information about the font face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <returns>Returns the FaceInfo of the loaded font face.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FaceInfo GetFaceInfo(FontFaceHandle faceHandle)
        {
            FaceInfo faceInfo = new FaceInfo();
            GetFaceInfo_FontFaceHandle_Internal(ref faceHandle, ref faceInfo);
            return faceInfo;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFaceInfo", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int GetFaceInfo_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, ref FaceInfo faceInfo);

        /// <summary>
        /// Get the number of faces and styles for the font identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static int GetFaceCount(FontFaceHandle faceHandle)
        {
            return GetFaceCount_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFaceCount", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int GetFaceCount_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Get the font face(s) and style(s) for the currently loaded font.
        /// </summary>
        /// <returns>Array containing the names of the font faces and styles.</returns>
        public static string[] GetFontFaces()
        {
            string[] faces = GetFontFaces_Internal();

            if (faces != null && faces.Length == 0)
                return null;

            return faces;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFontFaces", IsThreadSafe = true, IsFreeFunction = true)]
        static extern string[] GetFontFaces_Internal();

        /// <summary>
        /// Get the font face(s) and style(s) for the font identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static string[] GetFontFaces(FontFaceHandle faceHandle)
        {
            string[] faces = GetFontFaces_FontFaceHandle_Internal(ref faceHandle);

            if (faces != null && faces.Length == 0)
                return null;

            return faces;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFontFaces", IsThreadSafe = true, IsFreeFunction = true)]
        static extern string[] GetFontFaces_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Get the index of the glyph for the given character Unicode as modified by the variant selector for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static uint GetVariantGlyphIndex(FontFaceHandle faceHandle, uint unicode, uint variantSelectorUnicode)
        {
            return GetVariantGlyphIndex_FontFaceHandle_Internal(ref faceHandle, unicode, variantSelectorUnicode);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetVariantGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        static extern uint GetVariantGlyphIndex_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint unicode, uint variantSelectorUnicode);

        /// <summary>
        /// Get the index of the glyph for the character mapped at Unicode value for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static uint GetGlyphIndex(FontFaceHandle faceHandle, uint unicode)
        {
            return GetGlyphIndex_FontFaceHandle_Internal(ref faceHandle, unicode);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        static extern uint GetGlyphIndex_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint unicode);


        /// <summary>
        /// Try to get the glyph index for the character at the given Unicode value.
        /// </summary>
        /// <param name="unicode">The unicode value of the character for which to lookup the glyph index.</param>
        /// <param name="glyphIndex">The index of the glyph for the given unicode character or the .notdef glyph (index 0) if no glyph is available for the given Unicode value.</param>
        /// <returns>Returns true if the given unicode has a glyph index.</returns>
        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        public static extern bool TryGetGlyphIndex(uint unicode, out uint glyphIndex);

        /// <summary>
        /// Try to get the glyph index for the character at the given Unicode value for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryGetGlyphIndex(FontFaceHandle faceHandle, uint unicode, out uint glyphIndex)
        {
            return TryGetGlyphIndex_FontFaceHandle_Internal(ref faceHandle, unicode, out glyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetGlyphIndex_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint unicode, out uint glyphIndex);

        /// <summary>
        /// Gets the character map for the font face identified by the given handle, returning a mapping
        /// from glyph index to the list of Unicode code points that reference it.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <returns>A dictionary mapping each glyph index to its corresponding Unicode code points.</returns>
        internal static Dictionary<uint, List<int>> GetCharacterMap(FontFaceHandle faceHandle)
        {
            GlyphIndexCodePointMap[] map = GetFontCharacterMap_FontFaceHandle_Internal(ref faceHandle);

            Dictionary<uint, List<int>> characterMap = new Dictionary<uint, List<int>>(map.Length);

            for (int i = 0; i < map.Length; i++)
            {
                uint glyphIndex = (uint)map[i].glyphIndex;
                uint unicode = map[i].unicode;

                if (!characterMap.TryGetValue(glyphIndex, out List<int> codePoints))
                {
                    codePoints = new List<int>();
                    characterMap.Add(glyphIndex, codePoints);
                }
                codePoints.Add((int)unicode);
            }

            return characterMap;
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetFontCharacterMap", IsThreadSafe = true, IsFreeFunction = true)]
        static extern GlyphIndexCodePointMap[] GetFontCharacterMap_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Load the glyph at unicode value using the given load flags from the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static FontEngineError LoadGlyph(FontFaceHandle faceHandle, uint unicode, GlyphLoadFlags flags)
        {
            return (FontEngineError)LoadGlyph_FontFaceHandle_Internal(ref faceHandle, unicode, flags);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadGlyph", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int LoadGlyph_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint unicode, GlyphLoadFlags loadFlags);


        /// <summary>
        /// Try loading a glyph for the given unicode value. If available, populates the glyph and returns true. Otherwise returns false and populates the glyph with the .notdef / missing glyph data.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character whose glyph should be loaded.</param>
        /// <param name="flags">The Load Flags.</param>
        /// <param name="glyph">The glyph for the given character using the provided Unicode value or the .notdef glyph (index 0) if no glyph is available for the given Unicode value.</param>
        /// <returns>Returns true if a glyph exists for the given unicode value. Otherwise returns false.</returns>
        public static bool TryGetGlyphWithUnicodeValue(uint unicode, GlyphLoadFlags flags, out Glyph glyph)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct();

            if (TryGetGlyphWithUnicodeValue_Internal(unicode, flags, ref glyphStruct))
            {
                glyph = new Glyph(glyphStruct);

                return true;
            }

            // Set glyph to null if no glyph exists for the given unicode value.
            glyph = null;

            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithUnicodeValue", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetGlyphWithUnicodeValue_Internal(uint unicode, GlyphLoadFlags loadFlags, ref GlyphMarshallingStruct glyphStruct);

        /// <summary>
        /// Try loading a glyph for the given unicode value from the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryGetGlyphWithUnicodeValue(FontFaceHandle faceHandle, uint unicode, GlyphLoadFlags flags, out Glyph glyph)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct();

            if (TryGetGlyphWithUnicodeValue_FontFaceHandle_Internal(ref faceHandle, unicode, flags, ref glyphStruct))
            {
                glyph = new Glyph(glyphStruct);
                return true;
            }

            glyph = null;
            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithUnicodeValue", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetGlyphWithUnicodeValue_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint unicode, GlyphLoadFlags loadFlags, ref GlyphMarshallingStruct glyphStruct);


        /// <summary>
        /// Try loading the glyph for the given index value and if available populate the glyph.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph that should be loaded.</param>
        /// <param name="flags">The Load Flags.</param>
        /// <param name="glyph">The glyph using the provided index or the .notdef glyph (index 0) if no glyph was found at that index.</param>
        /// <returns>Returns true if a glyph exists at the given index. Otherwise returns false.</returns>
        public static bool TryGetGlyphWithIndexValue(uint glyphIndex, GlyphLoadFlags flags, out Glyph glyph)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct();

            if (TryGetGlyphWithIndexValue_Internal(glyphIndex, flags, ref glyphStruct))
            {
                glyph = new Glyph(glyphStruct);

                return true;
            }

            // Set glyph to null if no glyph exists for the given unicode value.
            glyph = null;

            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithIndexValue", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetGlyphWithIndexValue_Internal(uint glyphIndex, GlyphLoadFlags loadFlags, ref GlyphMarshallingStruct glyphStruct);

        /// <summary>
        /// Try loading the glyph for the given index value from the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryGetGlyphWithIndexValue(FontFaceHandle faceHandle, uint glyphIndex, GlyphLoadFlags flags, out Glyph glyph)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct();

            if (TryGetGlyphWithIndexValue_FontFaceHandle_Internal(ref faceHandle, glyphIndex, flags, ref glyphStruct))
            {
                glyph = new Glyph(glyphStruct);
                return true;
            }

            glyph = null;
            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithIndexValue", IsThreadSafe = true, IsFreeFunction = true)]
        static extern bool TryGetGlyphWithIndexValue_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint glyphIndex, GlyphLoadFlags loadFlags, ref GlyphMarshallingStruct glyphStruct);


        /// <summary>
        /// Try to pack the given glyph into the given texture width and height.
        /// </summary>
        /// <param name="glyph">The glyph to try to pack.</param>
        /// <param name="padding">The padding between this glyph and other glyphs.</param>
        /// <param name="packingMode">The packing algorithm used to pack the glyphs.</param>
        /// <param name="renderMode">The glyph rendering mode.</param>
        /// <param name="width">The width of the target atlas texture.</param>
        /// <param name="height">The height of the target atlas texture.</param>
        /// <param name="freeGlyphRects">List of GlyphRects representing the available space in the atlas.</param>
        /// <param name="usedGlyphRects">List of GlyphRects representing the occupied space in the atlas.</param>
        /// <returns></returns>
        internal static bool TryPackGlyphInAtlas(Glyph glyph, int padding, GlyphPackingMode packingMode, GlyphRenderMode renderMode, int width, int height, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyph);

            int freeGlyphRectCount = freeGlyphRects.Count;
            int usedGlyphRectCount = usedGlyphRects.Count;
            int totalGlyphRects = freeGlyphRectCount + usedGlyphRectCount;

            // Make sure marshalling arrays allocations are appropriate.
            if (s_FreeGlyphRects.Length < totalGlyphRects || s_UsedGlyphRects.Length < totalGlyphRects)
            {
                int newSize = Mathf.NextPowerOfTwo(totalGlyphRects + 1);
                s_FreeGlyphRects = new GlyphRect[newSize];
                s_UsedGlyphRects = new GlyphRect[newSize];
            }

            // Copy glyph rect data to marshalling arrays.
            int glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount);
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < freeGlyphRectCount)
                    s_FreeGlyphRects[i] = freeGlyphRects[i];

                if (i < usedGlyphRectCount)
                    s_UsedGlyphRects[i] = usedGlyphRects[i];
            }

            if (TryPackGlyphInAtlas_Internal(ref glyphStruct, padding, packingMode, renderMode, width, height, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount))
            {
                // Copy new glyph position to source glyph.
                glyph.glyphRect = glyphStruct.glyphRect;

                freeGlyphRects.Clear();
                usedGlyphRects.Clear();

                // Copy marshalled glyph rect data
                glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount);
                for (int i = 0; i < glyphRectCount; i++)
                {
                    if (i < freeGlyphRectCount)
                        freeGlyphRects.Add(s_FreeGlyphRects[i]);

                    if (i < usedGlyphRectCount)
                        usedGlyphRects.Add(s_UsedGlyphRects[i]);
                }

                return true;
            }

            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryPackGlyph", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryPackGlyphInAtlas_Internal(ref GlyphMarshallingStruct glyph, int padding, GlyphPackingMode packingMode, GlyphRenderMode renderMode, int width, int height,
            [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount);


        /// <summary>
        /// Pack glyphs in the given atlas size.
        /// </summary>
        /// <param name="glyphsToAdd">Glyphs to pack in atlas.</param>
        /// <param name="glyphsAdded">Glyphs packed in atlas.</param>
        /// <param name="padding">The padding between glyphs.</param>
        /// <param name="packingMode">The packing algorithm used to pack the glyphs.</param>
        /// <param name="renderMode">The glyph rendering mode.</param>
        /// <param name="width">The width of the target atlas texture.</param>
        /// <param name="height">The height of the target atlas texture.</param>
        /// <param name="freeGlyphRects">List of GlyphRects representing the available space in the atlas.</param>
        /// <param name="usedGlyphRects">List of GlyphRects representing the occupied space in the atlas.</param>
        /// <returns></returns>
        internal static bool TryPackGlyphsInAtlas(List<Glyph> glyphsToAdd, List<Glyph> glyphsAdded, int padding, GlyphPackingMode packingMode, GlyphRenderMode renderMode, int width, int height, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects)
        {
            // Determine potential total allocations required for glyphs and glyph rectangles.
            int glyphsToAddCount = glyphsToAdd.Count;
            int glyphsAddedCount = glyphsAdded.Count;
            int freeGlyphRectCount = freeGlyphRects.Count;
            int usedGlyphRectCount = usedGlyphRects.Count;
            int totalCount = glyphsToAddCount + glyphsAddedCount + freeGlyphRectCount + usedGlyphRectCount;

            // Make sure marshaling arrays allocations are appropriate.
            if (s_GlyphMarshallingStruct_IN.Length < totalCount || s_GlyphMarshallingStruct_OUT.Length < totalCount || s_FreeGlyphRects.Length < totalCount || s_UsedGlyphRects.Length < totalCount)
            {
                int newSize = Mathf.NextPowerOfTwo(totalCount + 1);
                s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[newSize];
                s_GlyphMarshallingStruct_OUT = new GlyphMarshallingStruct[newSize];
                s_FreeGlyphRects = new GlyphRect[newSize];
                s_UsedGlyphRects = new GlyphRect[newSize];
            }

            s_GlyphLookupDictionary.Clear();

            // Copy glyph data into appropriate marshaling array.
            for (int i = 0; i < totalCount; i++)
            {
                if (i < glyphsToAddCount)
                {
                    GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyphsToAdd[i]);

                    s_GlyphMarshallingStruct_IN[i] = glyphStruct;

                    // Add reference to glyph in lookup dictionary
                    if (s_GlyphLookupDictionary.ContainsKey(glyphStruct.index) == false)
                        s_GlyphLookupDictionary.Add(glyphStruct.index, glyphsToAdd[i]);
                }

                if (i < glyphsAddedCount)
                {
                    GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyphsAdded[i]);

                    s_GlyphMarshallingStruct_OUT[i] = glyphStruct;

                    // Add reference to glyph in lookup dictionary
                    if (s_GlyphLookupDictionary.ContainsKey(glyphStruct.index) == false)
                        s_GlyphLookupDictionary.Add(glyphStruct.index, glyphsAdded[i]);
                }

                if (i < freeGlyphRectCount)
                    s_FreeGlyphRects[i] = freeGlyphRects[i];

                if (i < usedGlyphRectCount)
                    s_UsedGlyphRects[i] = usedGlyphRects[i];
            }

            bool allGlyphsIncluded = TryPackGlyphsInAtlas_Internal(s_GlyphMarshallingStruct_IN, ref glyphsToAddCount, s_GlyphMarshallingStruct_OUT, ref glyphsAddedCount,
                padding, packingMode, renderMode, width, height,
                s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount);

            // Clear lists and / or re-allocate arrays.
            glyphsToAdd.Clear();
            glyphsAdded.Clear();
            freeGlyphRects.Clear();
            usedGlyphRects.Clear();

            // Copy marshaled glyph data back into the appropriate lists.
            for (int i = 0; i < totalCount; i++)
            {
                if (i < glyphsToAddCount)
                {
                    GlyphMarshallingStruct glyphStruct = s_GlyphMarshallingStruct_IN[i];
                    Glyph glyph = s_GlyphLookupDictionary[glyphStruct.index];

                    // Note: In theory, only new glyphRect x and y need to be copied.
                    glyph.metrics = glyphStruct.metrics;
                    glyph.glyphRect = glyphStruct.glyphRect;
                    glyph.scale = glyphStruct.scale;
                    glyph.atlasIndex = glyphStruct.atlasIndex;

                    glyphsToAdd.Add(glyph);
                }

                if (i < glyphsAddedCount)
                {
                    GlyphMarshallingStruct glyphStruct = s_GlyphMarshallingStruct_OUT[i];
                    Glyph glyph = s_GlyphLookupDictionary[glyphStruct.index];

                    glyph.metrics = glyphStruct.metrics;
                    glyph.glyphRect = glyphStruct.glyphRect;
                    glyph.scale = glyphStruct.scale;
                    glyph.atlasIndex = glyphStruct.atlasIndex;

                    glyphsAdded.Add(glyph);
                }

                if (i < freeGlyphRectCount)
                {
                    freeGlyphRects.Add(s_FreeGlyphRects[i]);
                }

                if (i < usedGlyphRectCount)
                {
                    usedGlyphRects.Add(s_UsedGlyphRects[i]);
                }
            }

            return allGlyphsIncluded;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryPackGlyphs", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryPackGlyphsInAtlas_Internal([Out] GlyphMarshallingStruct[] glyphsToAdd, ref int glyphsToAddCount, [Out] GlyphMarshallingStruct[] glyphsAdded, ref int glyphsAddedCount,
            int padding, GlyphPackingMode packingMode, GlyphRenderMode renderMode, int width, int height,
            [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount);


        /// <summary>
        /// Render and add glyph to the provided texture for the face identified by the given handle.
        /// </summary>
        internal static FontEngineError RenderGlyphToTexture(FontFaceHandle faceHandle, Glyph glyph, int padding, GlyphRenderMode renderMode, Texture2D texture)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyph);

            return (FontEngineError)RenderGlyphToTexture_FontFaceHandle_Internal(ref faceHandle, glyphStruct, padding, renderMode, texture);
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphToTexture", IsFreeFunction = true)]
        extern static int RenderGlyphToTexture_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, GlyphMarshallingStruct glyphStruct, int padding, GlyphRenderMode renderMode, Texture2D texture);


        /// <summary>
        /// Render and add the glyphs in the provided list to the texture for the face identified by the given handle.
        /// </summary>
        internal static FontEngineError RenderGlyphsToTexture(FontFaceHandle faceHandle, List<Glyph> glyphs, int padding, GlyphRenderMode renderMode, Texture2D texture)
        {
            int glyphCount = GlyphListToMarshallingArray(glyphs);

            int error = RenderGlyphsToTexture_FontFaceHandle_Internal(ref faceHandle, s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode, texture);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToTexture", IsFreeFunction = true)]
        extern static int RenderGlyphsToTexture_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode, Texture2D texture);


        /// <summary>
        /// Render and add the glyphs in the provided list to the texture buffer for the face identified by the given handle.
        /// </summary>
        internal static FontEngineError RenderGlyphsToTexture(FontFaceHandle faceHandle, List<Glyph> glyphs, int padding, GlyphRenderMode renderMode, byte[] texBuffer, int texWidth, int texHeight)
        {
            int glyphCount = GlyphListToMarshallingArray(glyphs);

            int error = RenderGlyphsToTextureBuffer_FontFaceHandle_Internal(ref faceHandle, s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode, texBuffer, texWidth, texHeight);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToTextureBuffer", IsThreadSafe = true, IsFreeFunction = true)]
        extern static int RenderGlyphsToTextureBuffer_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode, [Out] byte[] texBuffer, int texWidth, int texHeight);


        /// <summary>
        /// Render and add the glyphs in the provided list to the cached shared texture data for the face identified by the given handle.
        /// </summary>
        internal static FontEngineError RenderGlyphsToSharedTexture(FontFaceHandle faceHandle, List<Glyph> glyphs, int padding, GlyphRenderMode renderMode)
        {
            int glyphCount = GlyphListToMarshallingArray(glyphs);

            int error = RenderGlyphsToSharedTexture_FontFaceHandle_Internal(ref faceHandle, s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToSharedTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static int RenderGlyphsToSharedTexture_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode);


        /// <summary>
        /// Internal function used to get a reference to the shared texture data which is required for accessing the texture data outside of the main thread.
        /// </summary>
        [NativeMethod(Name = "TextCore::FontEngine::SetSharedTextureData", IsFreeFunction = true)]
        internal extern static void SetSharedTexture(Texture2D texture);


        /// <summary>
        /// Internal function used to release the shared texture data.
        /// </summary>
        [NativeMethod(Name = "TextCore::FontEngine::ReleaseSharedTextureData", IsThreadSafe = true, IsFreeFunction = true)]
        internal extern static void ReleaseSharedTexture();

        /// <summary>
        /// Internal function used to control if texture changes resulting from adding glyphs to an atlas texture will be uploaded to the graphic device immediately or delayed and batched.
        /// </summary>
        [NativeMethod(Name = "TextCore::FontEngine::SetTextureUploadMode", IsThreadSafe = true, IsFreeFunction = true)]
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal extern static void SetTextureUploadMode(bool shouldUploadImmediately);

        /// <summary>
        /// Internal function used to add glyph to atlas texture for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryAddGlyphToTexture(FontFaceHandle faceHandle, uint glyphIndex, int padding, GlyphPackingMode packingMode, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects, GlyphRenderMode renderMode, Texture2D texture, out Glyph glyph)
        {
            // Determine potential total allocations required for glyphs and glyph rectangles.
            int freeGlyphRectCount = freeGlyphRects.Count;
            int usedGlyphRectCount = usedGlyphRects.Count;
            int totalGlyphRects = freeGlyphRectCount + usedGlyphRectCount;

            // Make sure marshalling arrays allocations are appropriate.
            if (s_FreeGlyphRects.Length < totalGlyphRects || s_UsedGlyphRects.Length < totalGlyphRects)
            {
                int newSize = Mathf.NextPowerOfTwo(totalGlyphRects + 1);
                s_FreeGlyphRects = new GlyphRect[newSize];
                s_UsedGlyphRects = new GlyphRect[newSize];
            }

            // Copy glyph rect data to marshalling arrays.
            int glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount);
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < freeGlyphRectCount)
                    s_FreeGlyphRects[i] = freeGlyphRects[i];

                if (i < usedGlyphRectCount)
                    s_UsedGlyphRects[i] = usedGlyphRects[i];
            }

            GlyphMarshallingStruct glyphStruct;

            // Marshall data over to the native side.
            if (TryAddGlyphToTexture_FontFaceHandle_Internal(ref faceHandle, glyphIndex, padding, packingMode, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount, renderMode, texture, out glyphStruct))
            {
                // Copy marshalled data over to new glyph.
                glyph = new Glyph(glyphStruct);

                freeGlyphRects.Clear();
                usedGlyphRects.Clear();

                // Copy marshalled free and used GlyphRect data over.
                glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount);
                for (int i = 0; i < glyphRectCount; i++)
                {
                    if (i < freeGlyphRectCount)
                        freeGlyphRects.Add(s_FreeGlyphRects[i]);

                    if (i < usedGlyphRectCount)
                        usedGlyphRects.Add(s_UsedGlyphRects[i]);
                }

                return true;
            }

            glyph = null;

            return false;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryAddGlyphToTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryAddGlyphToTexture_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint glyphIndex, int padding,
            GlyphPackingMode packingMode, [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount,
            GlyphRenderMode renderMode, Texture2D texture, out GlyphMarshallingStruct glyph);


        /// <summary>
        /// Internal function used to add glyphs to atlas texture for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryAddGlyphsToTexture(FontFaceHandle faceHandle, List<Glyph> glyphsToAdd, List<Glyph> glyphsAdded, int padding, GlyphPackingMode packingMode, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects, GlyphRenderMode renderMode, Texture2D texture)
        {
            using var tryAddGlyphsToTextureScope = s_TryAddGlyphsToTextureMarker.Auto();

            int writeIndex = 0;
            bool keepCopyingData;

            int glyphsToAddCount = glyphsToAdd.Count;
            int glyphsAddedCount = 0;

            // Make sure marshalling arrays allocations are appropriate
            if (s_GlyphMarshallingStruct_IN.Length < glyphsToAddCount || s_GlyphMarshallingStruct_OUT.Length < glyphsToAddCount)
            {
                int newSize = Mathf.NextPowerOfTwo(glyphsToAddCount + 1);

                if (s_GlyphMarshallingStruct_IN.Length < glyphsToAddCount)
                    System.Array.Resize(ref s_GlyphMarshallingStruct_IN, newSize);

                if (s_GlyphMarshallingStruct_OUT.Length < glyphsToAddCount)
                    System.Array.Resize(ref s_GlyphMarshallingStruct_OUT, newSize);
            }

            // Determine potential total allocations required for glyphs and glyph rectangles.
            int freeGlyphRectCount = freeGlyphRects.Count;
            int usedGlyphRectCount = usedGlyphRects.Count;
            int totalGlyphRects = freeGlyphRectCount + usedGlyphRectCount + glyphsToAddCount;

            // Make sure marshalling arrays allocations are appropriate
            if (s_FreeGlyphRects.Length < totalGlyphRects || s_UsedGlyphRects.Length < totalGlyphRects)
            {
                int newSize = Mathf.NextPowerOfTwo(totalGlyphRects + 1);

                if (s_FreeGlyphRects.Length < totalGlyphRects)
                    System.Array.Resize(ref s_FreeGlyphRects, newSize);

                if (s_UsedGlyphRects.Length < totalGlyphRects)
                    System.Array.Resize(ref s_UsedGlyphRects, newSize);
            }

            s_GlyphLookupDictionary.Clear();

            // Copy data to marshalling arrays
            writeIndex = 0;
            keepCopyingData = true;
            while (keepCopyingData == true)
            {
                keepCopyingData = false;

                if (writeIndex < glyphsToAddCount)
                {
                    Glyph glyph = glyphsToAdd[writeIndex];

                    s_GlyphMarshallingStruct_IN[writeIndex] = new GlyphMarshallingStruct(glyph);
                    s_GlyphLookupDictionary.Add(glyph.index, glyph);
                    keepCopyingData = true;
                }

                if (writeIndex < freeGlyphRectCount)
                {
                    s_FreeGlyphRects[writeIndex] = freeGlyphRects[writeIndex];
                    keepCopyingData = true;
                }

                if (writeIndex < usedGlyphRectCount)
                {
                    s_UsedGlyphRects[writeIndex] = usedGlyphRects[writeIndex];
                    keepCopyingData = true;
                }

                writeIndex += 1;
            }

            bool allGlyphsAdded = TryAddGlyphsToTexture_FontFaceHandle_MultiThread(ref faceHandle, s_GlyphMarshallingStruct_IN, ref glyphsToAddCount, s_GlyphMarshallingStruct_OUT, ref glyphsAddedCount, padding, packingMode, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount, renderMode, texture);

            // Clear inbound lists
            glyphsToAdd.Clear();
            glyphsAdded.Clear();
            freeGlyphRects.Clear();
            usedGlyphRects.Clear();

            // Copy new data into appropriate data structure
            writeIndex = 0;
            keepCopyingData = true;
            while (keepCopyingData == true)
            {
                keepCopyingData = false;

                if (writeIndex < glyphsToAddCount)
                {
                    uint glyphIndex = s_GlyphMarshallingStruct_IN[writeIndex].index;
                    glyphsToAdd.Add(s_GlyphLookupDictionary[glyphIndex]);
                    keepCopyingData = true;
                }

                if (writeIndex < glyphsAddedCount)
                {
                    uint glyphIndex = s_GlyphMarshallingStruct_OUT[writeIndex].index;
                    Glyph glyph = s_GlyphLookupDictionary[glyphIndex];

                    glyph.atlasIndex = s_GlyphMarshallingStruct_OUT[writeIndex].atlasIndex;
                    glyph.scale = s_GlyphMarshallingStruct_OUT[writeIndex].scale;
                    glyph.glyphRect = s_GlyphMarshallingStruct_OUT[writeIndex].glyphRect;
                    glyph.metrics = s_GlyphMarshallingStruct_OUT[writeIndex].metrics;

                    glyphsAdded.Add(glyph);
                    keepCopyingData = true;
                }

                if (writeIndex < freeGlyphRectCount)
                {
                    freeGlyphRects.Add(s_FreeGlyphRects[writeIndex]);
                    keepCopyingData = true;
                }

                if (writeIndex < usedGlyphRectCount)
                {
                    usedGlyphRects.Add(s_UsedGlyphRects[writeIndex]);
                    keepCopyingData = true;
                }

                writeIndex += 1;
            }

            return allGlyphsAdded;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryAddGlyphsToTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryAddGlyphsToTexture_FontFaceHandle_MultiThread(ref FontFaceHandle faceHandle, [In, Out] GlyphMarshallingStruct[] glyphsToAdd, ref int glyphsToAddCount, [Out] GlyphMarshallingStruct[] glyphsAdded, ref int glyphsAddedCount,
            int padding, GlyphPackingMode packingMode, [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount,
            GlyphRenderMode renderMode, Texture2D texture);

        /// <summary>
        /// Internal function used to add multiple glyphs to atlas texture for the face identified by the given handle.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryAddGlyphsToTexture(FontFaceHandle faceHandle, List<uint> glyphIndexes, int padding, GlyphPackingMode packingMode, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects, GlyphRenderMode renderMode, Texture2D texture, out Glyph[] glyphs)
        {
            using var tryAddGlyphsToTextureScope = s_TryAddGlyphsToTextureMarker.Auto();

            glyphs = null;

            if (glyphIndexes == null || glyphIndexes.Count == 0)
            {
                return false;
            }

            int glyphCount = glyphIndexes.Count;

            // Make sure marshalling glyph index array allocations are appropriate.
            if (s_GlyphIndexes_MarshallingArray_A == null || s_GlyphIndexes_MarshallingArray_A.Length < glyphCount)
                s_GlyphIndexes_MarshallingArray_A = new uint[Mathf.NextPowerOfTwo(glyphCount + 1)];

            // Determine potential total allocations required for glyphs and glyph rectangles.
            int freeGlyphRectCount = freeGlyphRects.Count;
            int usedGlyphRectCount = usedGlyphRects.Count;
            int totalGlyphRects = freeGlyphRectCount + usedGlyphRectCount + glyphCount;

            // Make sure marshalling array(s) allocations are appropriate.
            if (s_FreeGlyphRects.Length < totalGlyphRects || s_UsedGlyphRects.Length < totalGlyphRects)
            {
                int newSize = Mathf.NextPowerOfTwo(totalGlyphRects + 1);
                s_FreeGlyphRects = new GlyphRect[newSize];
                s_UsedGlyphRects = new GlyphRect[newSize];
            }

            // Make sure marshaling array allocations are appropriate.
            if (s_GlyphMarshallingStruct_OUT.Length < glyphCount)
            {
                int newSize = Mathf.NextPowerOfTwo(glyphCount + 1);
                s_GlyphMarshallingStruct_OUT = new GlyphMarshallingStruct[newSize];
            }

            // Determine the max count
            int glyphRectCount = FontEngineUtilities.MaxValue(freeGlyphRectCount, usedGlyphRectCount, glyphCount);

            // Copy inbound data to Marshalling arrays.
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < glyphCount)
                    s_GlyphIndexes_MarshallingArray_A[i] = glyphIndexes[i];

                if (i < freeGlyphRectCount)
                    s_FreeGlyphRects[i] = freeGlyphRects[i];

                if (i < usedGlyphRectCount)
                    s_UsedGlyphRects[i] = usedGlyphRects[i];
            }

            // Marshall data over to the native side.
            bool allGlyphsAdded = TryAddGlyphsToTexture_FontFaceHandle_Internal(ref faceHandle, s_GlyphIndexes_MarshallingArray_A, padding, packingMode, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount, renderMode, texture, s_GlyphMarshallingStruct_OUT, ref glyphCount);

            // Make sure internal glyph array is properly sized.
            if (s_Glyphs == null || s_Glyphs.Length <= glyphCount)
                s_Glyphs = new Glyph[Mathf.NextPowerOfTwo(glyphCount + 1)];

            s_Glyphs[glyphCount] = null;

            freeGlyphRects.Clear();
            usedGlyphRects.Clear();

            // Determine the max count
            glyphRectCount = FontEngineUtilities.MaxValue(freeGlyphRectCount, usedGlyphRectCount, glyphCount);

            // Copy marshalled data back to their appropriate data structures.
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < glyphCount)
                    s_Glyphs[i] = new Glyph(s_GlyphMarshallingStruct_OUT[i]);

                if (i < freeGlyphRectCount)
                    freeGlyphRects.Add(s_FreeGlyphRects[i]);

                if (i < usedGlyphRectCount)
                    usedGlyphRects.Add(s_UsedGlyphRects[i]);
            }

            glyphs = s_Glyphs;

            return allGlyphsAdded;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryAddGlyphsToTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryAddGlyphsToTexture_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint[] glyphIndex, int padding,
            GlyphPackingMode packingMode, [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount,
            GlyphRenderMode renderMode, Texture2D texture, [In, Out] GlyphMarshallingStruct[] glyphs, ref int glyphCount);


        // ================================================
        // OPENTYPE RELATED FEATURES AND FUNCTIONS
        // ================================================

        /// <summary>
        /// Get the specified OpenType Layout table for the face identified by the given handle.
        /// </summary>
        internal static OTL_Table GetOpenTypeLayoutTable(FontFaceHandle faceHandle, OTL_TableType type)
        {
            return GetOpenTypeLayoutTable_FontFaceHandle_Internal(ref faceHandle, type);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetOpenTypeLayoutTable", IsFreeFunction = true)]
        static extern OTL_Table GetOpenTypeLayoutTable_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, OTL_TableType type);

        /// <summary>
        /// Get OpenType Layout scripts for the face identified by the given handle.
        /// </summary>
        internal static OTL_Script[] GetOpenTypeLayoutScripts(FontFaceHandle faceHandle)
        {
            return GetOpenTypeLayoutScripts_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetOpenTypeLayoutScripts", IsFreeFunction = true)]
        static extern OTL_Script[] GetOpenTypeLayoutScripts_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Get OpenType Layout features for the face identified by the given handle.
        /// </summary>
        internal static OTL_Feature[] GetOpenTypeLayoutFeatures(FontFaceHandle faceHandle)
        {
            return GetOpenTypeLayoutFeatures_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetOpenTypeLayoutFeatures", IsFreeFunction = true)]
        static extern OTL_Feature[] GetOpenTypeLayoutFeatures_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Get OpenType Layout lookups for the face identified by the given handle.
        /// </summary>
        internal static OTL_Lookup[] GetOpenTypeLayoutLookups(FontFaceHandle faceHandle)
        {
            return GetOpenTypeLayoutLookups_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetOpenTypeLayoutLookups", IsFreeFunction = true)]
        static extern OTL_Lookup[] GetOpenTypeLayoutLookups_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        // Required to prevent compilation errors on TMP 3.20.0 Preview 3.
        internal static OpenTypeFeature[] GetOpenTypeFontFeatureList() => throw new NotImplementedException();

        // ================================================
        // GSUB FONT FEATURES
        // ================================================

        #region SINGLE SUBSTITUTION
        /// <summary>
        /// Retrieve all single substitution records for the face identified by the given handle.
        /// </summary>
        internal static SingleSubstitutionRecord[] GetAllSingleSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllSingleSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllSingleSubstitutionRecords", IsFreeFunction = true)]
        static extern SingleSubstitutionRecord[] GetAllSingleSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static SingleSubstitutionRecord[] GetSingleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetSingleSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static SingleSubstitutionRecord[] GetSingleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetSingleSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static SingleSubstitutionRecord[] GetSingleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateSingleSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_SingleSubstitutionRecords_MarshallingArray, recordCount);
            GetSingleSubstitutionRecordsFromMarshallingArray(s_SingleSubstitutionRecords_MarshallingArray.AsSpan());
            s_SingleSubstitutionRecords_MarshallingArray[recordCount] = new SingleSubstitutionRecord();

            return s_SingleSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateSingleSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateSingleSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetSingleSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetSingleSubstitutionRecordsFromMarshallingArray(Span<SingleSubstitutionRecord> singleSubstitutionRecords);
        #endregion

        #region MULTIPLE SUBSTITUTION
        /// <summary>
        /// Retrieve all MultipleSubstitution records for the face identified by the given handle.
        /// </summary>
        internal static MultipleSubstitutionRecord[] GetAllMultipleSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllMultipleSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllMultipleSubstitutionRecords", IsFreeFunction = true)]
        static extern MultipleSubstitutionRecord[] GetAllMultipleSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static MultipleSubstitutionRecord[] GetMultipleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMultipleSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static MultipleSubstitutionRecord[] GetMultipleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMultipleSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static MultipleSubstitutionRecord[] GetMultipleSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateMultipleSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MultipleSubstitutionRecords_MarshallingArray, recordCount);
            GetMultipleSubstitutionRecordsFromMarshallingArray(s_MultipleSubstitutionRecords_MarshallingArray);
            s_MultipleSubstitutionRecords_MarshallingArray[recordCount] = new MultipleSubstitutionRecord();

            return s_MultipleSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMultipleSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMultipleSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetMultipleSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetMultipleSubstitutionRecordsFromMarshallingArray([Out] MultipleSubstitutionRecord[] substitutionRecords);

        #endregion

        #region ALTERNATE SUBSTITUTION
        /// <summary>
        /// Retrieve all alternate substitution records for the face identified by the given handle.
        /// </summary>
        internal static AlternateSubstitutionRecord[] GetAllAlternateSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllAlternateSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllAlternateSubstitutionRecords", IsFreeFunction = true)]
        static extern AlternateSubstitutionRecord[] GetAllAlternateSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static AlternateSubstitutionRecord[] GetAlternateSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetAlternateSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static AlternateSubstitutionRecord[] GetAlternateSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetAlternateSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static AlternateSubstitutionRecord[] GetAlternateSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateAlternateSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_AlternateSubstitutionRecords_MarshallingArray, recordCount);
            GetAlternateSubstitutionRecordsFromMarshallingArray(s_AlternateSubstitutionRecords_MarshallingArray);
            s_AlternateSubstitutionRecords_MarshallingArray[recordCount] = new AlternateSubstitutionRecord();

            return s_AlternateSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateAlternateSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateAlternateSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetAlternateSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetAlternateSubstitutionRecordsFromMarshallingArray([Out] AlternateSubstitutionRecord[] singleSubstitutionRecords);

        #endregion

        #region LIGATURE
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static LigatureSubstitutionRecord[] GetAllLigatureSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllLigatureSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllLigatureSubstitutionRecords", IsFreeFunction = true)]
        static extern LigatureSubstitutionRecord[] GetAllLigatureSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Internal function used to retrieve potential ligature substitution records for the given glyph index from the face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndex">Index of the glyph to check for potential substitution records.</param>
        /// <returns>An array that contains the substitution records for the provided glyph index.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetLigatureSubstitutionRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        /// <summary>
        /// Internal function used to retrieve potential ligature substitution records for the provided list of glyph indexes from the face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndexes">List of glyph indexes to check for potential substitution records.</param>
        /// <returns>An array that contains the substitution records for the provided list of glyph indexes.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetLigatureSubstitutionRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetLigatureSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetLigatureSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulateLigatureSubstitutionRecordMarshallingArray_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_LigatureSubstitutionRecords_MarshallingArray, recordCount);
            GetLigatureSubstitutionRecordsFromMarshallingArray(s_LigatureSubstitutionRecords_MarshallingArray);
            s_LigatureSubstitutionRecords_MarshallingArray[recordCount] = new LigatureSubstitutionRecord();

            return s_LigatureSubstitutionRecords_MarshallingArray;
        }

        private static LigatureSubstitutionRecord[] GetLigatureSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateLigatureSubstitutionRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_LigatureSubstitutionRecords_MarshallingArray, recordCount);
            GetLigatureSubstitutionRecordsFromMarshallingArray(s_LigatureSubstitutionRecords_MarshallingArray);
            s_LigatureSubstitutionRecords_MarshallingArray[recordCount] = new LigatureSubstitutionRecord();

            return s_LigatureSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateLigatureSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateLigatureSubstitutionRecordMarshallingArray_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::PopulateLigatureSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateLigatureSubstitutionRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetLigatureSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetLigatureSubstitutionRecordsFromMarshallingArray([Out] LigatureSubstitutionRecord[] ligatureSubstitutionRecords);
        #endregion

        #region CONTEXTUAL SUBSTITUTION
        internal static ContextualSubstitutionRecord[] GetAllContextualSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllContextualSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllContextualSubstitutionRecords", IsFreeFunction = true)]
        static extern ContextualSubstitutionRecord[] GetAllContextualSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static ContextualSubstitutionRecord[] GetContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetContextualSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static ContextualSubstitutionRecord[] GetContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetContextualSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static ContextualSubstitutionRecord[] GetContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateContextualSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_ContextualSubstitutionRecords_MarshallingArray, recordCount);
            GetContextualSubstitutionRecordsFromMarshallingArray(s_ContextualSubstitutionRecords_MarshallingArray);
            s_ContextualSubstitutionRecords_MarshallingArray[recordCount] = new ContextualSubstitutionRecord();

            return s_ContextualSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateContextualSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateContextualSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetContextualSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetContextualSubstitutionRecordsFromMarshallingArray([Out] ContextualSubstitutionRecord[] substitutionRecords);

        #endregion

        #region CHAINING CONTEXTUAL SUBSTITUTION
        internal static ChainingContextualSubstitutionRecord[] GetAllChainingContextualSubstitutionRecords(FontFaceHandle faceHandle)
        {
            return GetAllChainingContextualSubstitutionRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllChainingContextualSubstitutionRecords", IsFreeFunction = true)]
        static extern ChainingContextualSubstitutionRecord[] GetAllChainingContextualSubstitutionRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static ChainingContextualSubstitutionRecord[] GetChainingContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetChainingContextualSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static ChainingContextualSubstitutionRecord[] GetChainingContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetChainingContextualSubstitutionRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static ChainingContextualSubstitutionRecord[] GetChainingContextualSubstitutionRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateChainingContextualSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_ChainingContextualSubstitutionRecords_MarshallingArray, recordCount);
            GetChainingContextualSubstitutionRecordsFromMarshallingArray(s_ChainingContextualSubstitutionRecords_MarshallingArray);
            s_ChainingContextualSubstitutionRecords_MarshallingArray[recordCount] = new ChainingContextualSubstitutionRecord();

            return s_ChainingContextualSubstitutionRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateChainingContextualSubstitutionRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateChainingContextualSubstitutionRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetChainingContextualSubstitutionRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetChainingContextualSubstitutionRecordsFromMarshallingArray([Out] ChainingContextualSubstitutionRecord[] substitutionRecords);

        #endregion

        // ================================================
        // POSITIONAL ADJUSTMENTS FROM KERN TABLE
        // ================================================

        #region POSITIONAL ADJUSTMENTS
        /// <summary>
        /// Internal function used to retrieve positional adjustment records for the given glyph indexes from the font face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndexes">Array of glyph indexes to check for potential positional adjustment records.</param>
        /// <returns>Array containing the positional adjustments for pairs of glyphs.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static GlyphPairAdjustmentRecord[] GetGlyphPairAdjustmentTable(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulatePairAdjustmentRecordMarshallingArray_from_KernTable_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        internal static GlyphPairAdjustmentRecord[] GetGlyphPairAdjustmentRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes, out int recordCount)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            int error = PopulatePairAdjustmentRecordMarshallingArray_from_KernTable_FontFaceHandle(ref faceHandle, s_GlyphIndexes_MarshallingArray_A, out recordCount);

            if (error != 0 || recordCount == 0)
            {
                recordCount = 0;
                return null;
            }

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulatePairAdjustmentRecordMarshallingArrayFromKernTable", IsFreeFunction = true)]
        extern static int PopulatePairAdjustmentRecordMarshallingArray_from_KernTable_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        internal static GlyphPairAdjustmentRecord[] GetGlyphPairAdjustmentRecords(FontFaceHandle faceHandle, uint glyphIndex, out int recordCount)
        {
            int error = PopulatePairAdjustmentRecordMarshallingArray_from_GlyphIndex_FontFaceHandle(ref faceHandle, glyphIndex, out recordCount);

            if (error != 0 || recordCount == 0)
            {
                recordCount = 0;
                return null;
            }

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulatePairAdjustmentRecordMarshallingArrayFromKernTable", IsFreeFunction = true)]
        extern static int PopulatePairAdjustmentRecordMarshallingArray_from_GlyphIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint glyphIndex, out int recordCount);

        internal static GlyphPairAdjustmentRecord[] GetGlyphPairAdjustmentRecords(FontFaceHandle faceHandle, List<uint> newGlyphIndexes, List<uint> allGlyphIndexes)
        {
            GenericListToMarshallingArray(ref newGlyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);
            GenericListToMarshallingArray(ref allGlyphIndexes, ref s_GlyphIndexes_MarshallingArray_B);

            int error = PopulatePairAdjustmentRecordMarshallingArray_for_NewlyAddedGlyphIndexes_FontFaceHandle(ref faceHandle, s_GlyphIndexes_MarshallingArray_A, s_GlyphIndexes_MarshallingArray_B, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulatePairAdjustmentRecordMarshallingArrayFromKernTable", IsFreeFunction = true)]
        extern static int PopulatePairAdjustmentRecordMarshallingArray_for_NewlyAddedGlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] newGlyphIndexes, uint[] allGlyphIndexes, out int recordCount);

        internal static GlyphPairAdjustmentRecord GetGlyphPairAdjustmentRecord(FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex)
        {
            return GetGlyphPairAdjustmentRecord_FontFaceHandle_Internal(ref faceHandle, firstGlyphIndex, secondGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphPairAdjustmentRecord", IsFreeFunction = true)]
        static extern GlyphPairAdjustmentRecord GetGlyphPairAdjustmentRecord_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex);
        #endregion

        // ================================================
        // GPOS FONT FEATURES
        // ================================================

        #region SINGLE ADJUSTMENT
        internal static GlyphAdjustmentRecord[] GetSingleAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetSingleAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static GlyphAdjustmentRecord[] GetSingleAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetSingleAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static GlyphAdjustmentRecord[] GetSingleAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateSingleAdjustmentRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_SingleAdjustmentRecords_MarshallingArray, recordCount);
            GetSingleAdjustmentRecordsFromMarshallingArray(s_SingleAdjustmentRecords_MarshallingArray.AsSpan());
            s_SingleAdjustmentRecords_MarshallingArray[recordCount] = new GlyphAdjustmentRecord();

            return s_SingleAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateSingleAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateSingleAdjustmentRecordMarshallingArray_from_GlyphIndexes_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetSingleAdjustmentRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetSingleAdjustmentRecordsFromMarshallingArray(Span<GlyphAdjustmentRecord> singleSubstitutionRecords);
        #endregion

        #region PAIR ADJUSTMENTS
        /// <summary>
        /// Retrieve all potential glyph pair adjustment records for the given glyph.
        /// </summary>
        /// <param name="baseGlyphIndex">The index of the glyph.</param>
        /// <returns>An array that contains the adjustment records for the given glyph.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, uint glyphIndex)
        {
            return GetPairAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle, glyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetPairAdjustmentRecords", IsFreeFunction = true)]
        static extern GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint glyphIndex);

        internal static GlyphPairAdjustmentRecord GetPairAdjustmentRecord(FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex)
        {
            return GetPairAdjustmentRecord_FontFaceHandle_Internal(ref faceHandle, firstGlyphIndex, secondGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetPairAdjustmentRecord", IsFreeFunction = true)]
        static extern GlyphPairAdjustmentRecord GetPairAdjustmentRecord_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex);

        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static GlyphPairAdjustmentRecord[] GetAllPairAdjustmentRecords(FontFaceHandle faceHandle)
        {
            return GetAllPairAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllPairAdjustmentRecords", IsFreeFunction = true)]
        static extern GlyphPairAdjustmentRecord[] GetAllPairAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        /// <summary>
        /// Internal function used to retrieve the potential glyph pair adjustment records for the given list of glyphs from the face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndexes">List of glyph indexes to check for potential adjustment records.</param>
        /// <returns>An array that contains the glyph pair adjustment records.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetPairAdjustmentRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint glyphIndex)
        {
            GlyphIndexToMarshallingArray(glyphIndex, ref s_GlyphIndexes_MarshallingArray_A);

            return GetPairAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetPairAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulatePairAdjustmentRecordMarshallingArray_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        private static GlyphPairAdjustmentRecord[] GetPairAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulatePairAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_PairAdjustmentRecords_MarshallingArray, recordCount);
            GetPairAdjustmentRecordsFromMarshallingArray(s_PairAdjustmentRecords_MarshallingArray);
            s_PairAdjustmentRecords_MarshallingArray[recordCount] = new GlyphPairAdjustmentRecord();

            return s_PairAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulatePairAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulatePairAdjustmentRecordMarshallingArray_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::PopulatePairAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulatePairAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphPairAdjustmentRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetPairAdjustmentRecordsFromMarshallingArray(Span<GlyphPairAdjustmentRecord> glyphPairAdjustmentRecords);
        #endregion

        #region MARK TO BASE
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static MarkToBaseAdjustmentRecord[] GetAllMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle)
        {
            return GetAllMarkToBaseAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllMarkToBaseAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToBaseAdjustmentRecord[] GetAllMarkToBaseAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle, uint baseGlyphIndex)
        {
            return GetMarkToBaseAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle, baseGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToBaseAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint baseGlyphIndex);

        internal static MarkToBaseAdjustmentRecord GetMarkToBaseAdjustmentRecord(FontFaceHandle faceHandle, uint baseGlyphIndex, uint markGlyphIndex)
        {
            return GetMarkToBaseAdjustmentRecord_FontFaceHandle_Internal(ref faceHandle, baseGlyphIndex, markGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToBaseAdjustmentRecord", IsFreeFunction = true)]
        static extern MarkToBaseAdjustmentRecord GetMarkToBaseAdjustmentRecord_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint baseGlyphIndex, uint markGlyphIndex);

        /// <summary>
        /// Internal function used to retrieve the potential Mark-To-Base adjustment records for the given list of glyph indexes from the face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndexes">The list of glyph indexes.</param>
        /// <returns>An array that contains the adjustment records for the given list of glyph indexes.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToBaseAdjustmentRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToBaseAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulateMarkToBaseAdjustmentRecordMarshallingArray_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToBaseAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToBaseAdjustmentRecordsFromMarshallingArray(s_MarkToBaseAdjustmentRecords_MarshallingArray);
            s_MarkToBaseAdjustmentRecords_MarshallingArray[recordCount] = new MarkToBaseAdjustmentRecord();

            return s_MarkToBaseAdjustmentRecords_MarshallingArray;
        }

        private static MarkToBaseAdjustmentRecord[] GetMarkToBaseAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateMarkToBaseAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToBaseAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToBaseAdjustmentRecordsFromMarshallingArray(s_MarkToBaseAdjustmentRecords_MarshallingArray);
            s_MarkToBaseAdjustmentRecords_MarshallingArray[recordCount] = new MarkToBaseAdjustmentRecord();

            return s_MarkToBaseAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToBaseAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToBaseAdjustmentRecordMarshallingArray_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToBaseAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToBaseAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToBaseAdjustmentRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetMarkToBaseAdjustmentRecordsFromMarshallingArray(Span<MarkToBaseAdjustmentRecord> adjustmentRecords);
        #endregion

        #region MARK TO MARK
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static MarkToMarkAdjustmentRecord[] GetAllMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle)
        {
            return GetAllMarkToMarkAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllMarkToMarkAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToMarkAdjustmentRecord[] GetAllMarkToMarkAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle, uint baseMarkGlyphIndex)
        {
            return GetMarkToMarkAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle, baseMarkGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToMarkAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint baseMarkGlyphIndex);

        internal static MarkToMarkAdjustmentRecord GetMarkToMarkAdjustmentRecord(FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex)
        {
            return GetMarkToMarkAdjustmentRecord_FontFaceHandle_Internal(ref faceHandle, firstGlyphIndex, secondGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToMarkAdjustmentRecord", IsFreeFunction = true)]
        static extern MarkToMarkAdjustmentRecord GetMarkToMarkAdjustmentRecord_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex);

        /// <summary>
        /// Internal function used to retrieve the potential Mark-To-Mark adjustment records for the given list of glyph indexes from the face identified by the given handle.
        /// </summary>
        /// <param name="faceHandle">A handle that identifies the face to query.</param>
        /// <param name="glyphIndexes">The list of glyph indexes.</param>
        /// <returns>An array that contains the adjustment records for the given list of glyph indexes.</returns>
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToMarkAdjustmentRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToMarkAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulateMarkToMarkAdjustmentRecordMarshallingArray_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToMarkAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToMarkAdjustmentRecordsFromMarshallingArray(s_MarkToMarkAdjustmentRecords_MarshallingArray);
            s_MarkToMarkAdjustmentRecords_MarshallingArray[recordCount] = new MarkToMarkAdjustmentRecord();

            return s_MarkToMarkAdjustmentRecords_MarshallingArray;
        }

        private static MarkToMarkAdjustmentRecord[] GetMarkToMarkAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateMarkToMarkAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToMarkAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToMarkAdjustmentRecordsFromMarshallingArray(s_MarkToMarkAdjustmentRecords_MarshallingArray);
            s_MarkToMarkAdjustmentRecords_MarshallingArray[recordCount] = new MarkToMarkAdjustmentRecord();

            return s_MarkToMarkAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToMarkAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToMarkAdjustmentRecordMarshallingArray_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToMarkAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToMarkAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToMarkAdjustmentRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetMarkToMarkAdjustmentRecordsFromMarshallingArray(Span<MarkToMarkAdjustmentRecord> adjustmentRecords);
        #endregion

        #region MARK TO LIGATURE
        internal static MarkToLigatureAdjustmentRecord[] GetAllMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle)
        {
            return GetAllMarkToLigatureAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetAllMarkToLigatureAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToLigatureAdjustmentRecord[] GetAllMarkToLigatureAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle);

        internal static MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle, uint baseMarkGlyphIndex)
        {
            return GetMarkToLigatureAdjustmentRecords_FontFaceHandle_Internal(ref faceHandle, baseMarkGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToLigatureAdjustmentRecords", IsFreeFunction = true)]
        static extern MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint baseMarkGlyphIndex);

        internal static MarkToLigatureAdjustmentRecord GetMarkToLigatureAdjustmentRecord(FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex)
        {
            return GetMarkToLigatureAdjustmentRecord_FontFaceHandle_Internal(ref faceHandle, firstGlyphIndex, secondGlyphIndex);
        }

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToLigatureAdjustmentRecord", IsFreeFunction = true)]
        static extern MarkToLigatureAdjustmentRecord GetMarkToLigatureAdjustmentRecord_FontFaceHandle_Internal(ref FontFaceHandle faceHandle, uint firstGlyphIndex, uint secondGlyphIndex);

        internal static MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToLigatureAdjustmentRecords(faceHandle, s_GlyphIndexes_MarshallingArray_A);
        }

        internal static MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, List<uint> glyphIndexes)
        {
            GenericListToMarshallingArray(ref glyphIndexes, ref s_GlyphIndexes_MarshallingArray_A);

            return GetMarkToLigatureAdjustmentRecords(faceHandle, lookupIndex, s_GlyphIndexes_MarshallingArray_A);
        }

        private static MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle, uint[] glyphIndexes)
        {
            int error = PopulateMarkToLigatureAdjustmentRecordMarshallingArray_FontFaceHandle(ref faceHandle, glyphIndexes, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToLigatureAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToLigatureAdjustmentRecordsFromMarshallingArray(s_MarkToLigatureAdjustmentRecords_MarshallingArray);
            s_MarkToLigatureAdjustmentRecords_MarshallingArray[recordCount] = new MarkToLigatureAdjustmentRecord();

            return s_MarkToLigatureAdjustmentRecords_MarshallingArray;
        }

        private static MarkToLigatureAdjustmentRecord[] GetMarkToLigatureAdjustmentRecords(FontFaceHandle faceHandle, int lookupIndex, uint[] glyphIndexes)
        {
            int error = PopulateMarkToLigatureAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref faceHandle, glyphIndexes, lookupIndex, out int recordCount);

            if (error != 0 || recordCount == 0)
                return null;

            SetMarshallingArraySize(ref s_MarkToLigatureAdjustmentRecords_MarshallingArray, recordCount);
            GetMarkToLigatureAdjustmentRecordsFromMarshallingArray(s_MarkToLigatureAdjustmentRecords_MarshallingArray);
            s_MarkToLigatureAdjustmentRecords_MarshallingArray[recordCount] = new MarkToLigatureAdjustmentRecord();

            return s_MarkToLigatureAdjustmentRecords_MarshallingArray;
        }

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToLigatureAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToLigatureAdjustmentRecordMarshallingArray_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::PopulateMarkToLigatureAdjustmentRecordMarshallingArray", IsFreeFunction = true)]
        extern static int PopulateMarkToLigatureAdjustmentRecordMarshallingArray_for_LookupIndex_FontFaceHandle(ref FontFaceHandle faceHandle, uint[] glyphIndexes, int lookupIndex, out int recordCount);

        [NativeMethod(Name = "TextCore::FontEngine::GetMarkToLigatureAdjustmentRecordsFromMarshallingArray", IsFreeFunction = true)]
        extern static int GetMarkToLigatureAdjustmentRecordsFromMarshallingArray([Out] MarkToLigatureAdjustmentRecord[] adjustmentRecords);
        #endregion

        // ================================================
        // Harfbuzz - Methods
        // ================================================


        // ================================================
        // Utility Methods
        // ================================================

        static void GlyphIndexToMarshallingArray(uint glyphIndex, ref uint[] dstArray)
        {
            if (dstArray == null || dstArray.Length == 1)
                dstArray = new uint[8];

            dstArray[0] = glyphIndex;
            dstArray[1] = 0;
        }

        static void GenericListToMarshallingArray<T>(ref List<T> srcList, ref T[] dstArray)
        {
            int count = srcList.Count;

            if (dstArray == null || dstArray.Length <= count)
            {
                int size = Mathf.NextPowerOfTwo(count + 1);

                if (dstArray == null)
                    dstArray = new T[size];
                else
                    Array.Resize(ref dstArray, size);
            }

            // Copy list data to marshalling array
            for (int i = 0; i < count; i++)
                dstArray[i] = srcList[i];

            // Set marshalling array boundary / terminator to value of zero.
            dstArray[count] = default(T);
        }

        static int GlyphListToMarshallingArray(List<Glyph> glyphs)
        {
            int glyphCount = glyphs.Count;

            // Make sure marshaling array allocation is appropriate.
            if (s_GlyphMarshallingStruct_IN.Length < glyphCount)
                s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[Mathf.NextPowerOfTwo(glyphCount + 1)];

            // Copy data to marshalling buffer.
            for (int i = 0; i < glyphCount; i++)
                s_GlyphMarshallingStruct_IN[i] = new GlyphMarshallingStruct(glyphs[i]);

            return glyphCount;
        }

        /// <summary>
        ///
        /// </summary>
        static void SetMarshallingArraySize<T>(ref T[] marshallingArray, int recordCount)
        {
            if (marshallingArray == null || marshallingArray.Length <= recordCount)
            {
                int size = Mathf.NextPowerOfTwo(recordCount + 1);

                if (marshallingArray == null)
                    marshallingArray = new T[size];
                else
                    Array.Resize(ref marshallingArray, size);
            }
        }

        // ================================================
        // Experimental / Testing / Benchmarking Functions
        // ================================================

        /// <summary>
        /// Internal function used to reset an atlas texture to black
        /// </summary>
        /// <param name="srcTexture"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ResetAtlasTexture", IsFreeFunction = true)]
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal extern static void ResetAtlasTexture(Texture2D texture);

        /// <summary>
        /// Internal function used for testing rasterizing of shapes and glyphs.
        /// </summary>
        /// <param name="srcTexture">Texture containing the source shape to raster.</param>
        /// <param name="padding">Padding value.</param>
        /// <param name="renderMode">The rendering mode.</param>
        /// <param name="dstTexture">Texture containing the rastered shape.</param>
        [NativeMethod(Name = "TextCore::FontEngine::RenderToTexture", IsFreeFunction = true)]
        internal extern static void RenderBufferToTexture(Texture2D srcTexture, int padding, GlyphRenderMode renderMode, Texture2D dstTexture);


        /*
        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyph", IsFreeFunction = true)]
        extern public static void ModifyGlyph([Out] Glyph glyph);

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyph"></param>
        public static void ModifyGlyphStruct(Glyph glyph)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyph);

            ModifyGlyph_Internal(ref glyphStruct);

            glyph.metrics = glyphStruct.metrics;
            glyph.glyphRect = glyphStruct.glyphRect;
            glyph.scale = glyphStruct.scale;
            glyph.atlasIndex = glyphStruct.atlasIndex;
        }

        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyph", IsThreadSafe = true, IsFreeFunction = true)]
        extern static void ModifyGlyph_Internal(ref GlyphMarshallingStruct glyphs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyph"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyphMarshallingStruct", IsFreeFunction = true)]
        extern public static void ModifyGlyphMarshallingStruct(GlyphMarshallingStruct[] glyph);

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyph"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyphMarshallingStruct", IsFreeFunction = true)]
        extern public static void ModifyGlyphMarshallingStructArray(GlyphMarshallingStruct[] glyph);

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyph"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyphs", IsFreeFunction = true)]
        extern public static void ModifyGlyphStructArray([Out] GlyphMarshallingStruct[] glyph);

        /// <summary>
        ///
        /// </summary>
        /// <param name="glyph"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ModifyGlyphs", IsFreeFunction = true)]
        extern public static void ModifyGlyphArray([Out] Glyph[] glyph);

        [NativeMethod(Name = "TextCore::FontEngine::AccessFont", IsFreeFunction = true)]
        extern public static void AccessFont(Font font);
        */
    }

    internal struct FontEngineUtilities
    {
        internal static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }

        internal static int MaxValue(int a, int b, int c)
        {
            return a < b ? (b < c ? c : b) : (a < c ? c : a);
        }
    }
}
