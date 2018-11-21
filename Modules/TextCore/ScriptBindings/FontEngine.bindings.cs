// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.LowLevel
{
    [UsedByNativeCode]
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
        //LOAD_COLOR = 1 << 20,
        LOAD_COMPUTE_METRICS = 1 << 21,
        LOAD_BITMAP_METRICS_ONLY = 1 << 22
    }

    /// <summary>
    /// Rasterizing modes used by the Font Engine to raster glyphs.
    /// </summary>
    internal enum GlyphRasterModes
    {
        RASTER_MODE_8BIT        = 0x1,
        RASTER_MODE_MONO        = 0x2,

        RASTER_MODE_NO_HINTING  = 0x4,
        RASTER_MODE_HINTED      = 0x8,

        RASTER_MODE_BITMAP      = 0x10,
        RASTER_MODE_SDF         = 0x20,
        RASTER_MODE_SDFAA       = 0x40,
        // Reserved             = 0x80,

        RASTER_MODE_MSDF        = 0x100,
        RASTER_MODE_MSDFA       = 0x200,
        // Reserved             = 0x400,
        // Reserved             = 0x800,

        RASTER_MODE_1X          = 0x1000,
        RASTER_MODE_8X          = 0x2000,
        RASTER_MODE_16X         = 0x4000,
        RASTER_MODE_32X         = 0x8000,
    }

    /// <summary>
    /// Error codes returned and relevant to the various FontEngine functions.
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

        // Additional errors codes will be added as necessary to cover new FontEngine features and functionality.
    }

    /// <summary>
    /// Rendering modes used by the Font Engine to render glyphs.
    /// </summary>
    [UsedByNativeCode]
    public enum GlyphRenderMode
    {
        SMOOTH_HINTED = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        SMOOTH = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        RASTER_HINTED = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        RASTER = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        SDF = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_1X,
        SDF8 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_8X,
        SDF16 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_16X,
        SDF32 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_32X,

        SDFAA_HINTED = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_SDFAA | GlyphRasterModes.RASTER_MODE_1X,
        SDFAA = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_SDFAA | GlyphRasterModes.RASTER_MODE_1X,
        //MSDF  = RasterModes.RASTER_MODE_HINTED | RasterModes.RASTER_MODE_8BIT | RasterModes.RASTER_MODE_MSDF | RasterModes.RASTER_MODE_1X,
        //MSDFA = RasterModes.RASTER_MODE_HINTED | RasterModes.RASTER_MODE_8BIT | RasterModes.RASTER_MODE_MSDFA | RasterModes.RASTER_MODE_1X,
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

    [NativeHeader("Modules/TextCore/Native/FontEngine/FontEngine.h")]
    public sealed class FontEngine
    {
        private static readonly FontEngine s_Instance = new FontEngine();

        private static uint[] s_GlyphIndexesToMarshall;

        private static GlyphMarshallingStruct[] s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[16];
        private static GlyphMarshallingStruct[] s_GlyphMarshallingStruct_OUT = new GlyphMarshallingStruct[16];

        private static GlyphRect[] s_FreeGlyphRects = new GlyphRect[16];
        private static GlyphRect[] s_UsedGlyphRects = new GlyphRect[16];

        private static GlyphPairAdjustmentRecord[] s_GlyphPairAdjustmentRecords;

        private static Dictionary<uint, Glyph> s_GlyphLookupDictionary = new Dictionary<uint, Glyph>();

        internal FontEngine() {}

        /// <summary>
        /// Returns the instance of the FontEngine
        /// </summary>
        /// <returns>Returns the singleton instance of the Font Engine.</returns>
        internal static FontEngine GetInstance()
        {
            return s_Instance;
        }

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
        /// Load the source font file at the given file path.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string filePath)
        {
            return (FontEngineError)LoadFontFace_Internal(filePath);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_Internal(string filePath);


        /// <summary>
        /// Load the font file at the given file path and set it size to the specified point size.
        /// </summary>
        /// <param name="filePath">The file path of the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(string filePath, int pointSize)
        {
            return (FontEngineError)LoadFontFace_With_Size_Internal(filePath, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_Internal(string filePath, int pointSize);


        /// <summary>
        /// Load the font file from the provided byte array.
        /// </summary>
        /// <param name="sourceFontFile">A byte array containing the source font file.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(byte[] sourceFontFile)
        {
            if (sourceFontFile.Length == 0)
                return FontEngineError.Invalid_File;

            return (FontEngineError)LoadFontFace_FromSourceFontFile_Internal(sourceFontFile);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_FromSourceFontFile_Internal(byte[] sourceFontFile);


        /// <summary>
        /// Load the font file from the provided byte array and set its size to the given point size.
        /// </summary>
        /// <param name="sourceFontFile">A byte array containing the source font file.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(byte[] sourceFontFile, int pointSize)
        {
            if (sourceFontFile.Length == 0)
                return FontEngineError.Invalid_File;

            return (FontEngineError)LoadFontFace_With_Size_FromSourceFontFile_Internal(sourceFontFile, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FromSourceFontFile_Internal(byte[] sourceFontFile, int pointSize);


        /// <summary>
        /// Load the font file from the Unity font's internal font data. Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(Font font)
        {
            return (FontEngineError)LoadFontFace_FromFont_Internal(font);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_FromFont_Internal(Font font);


        /// <summary>
        /// Load the font file from the Unity font's internal font data. Note the Unity font must be set to Dynamic with Include Font Data enabled.
        /// </summary>
        /// <param name="font">The font from which to load the data.</param>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was loaded successfully.</returns>
        public static FontEngineError LoadFontFace(Font font, int pointSize)
        {
            return (FontEngineError)LoadFontFace_With_Size_FromFont_Internal(font, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FromFont_Internal(Font font, int pointSize);


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
        /// Get the index of the glyph for the character mapped at Unicode value.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character for which to lookup the glyph index.</param>
        /// <returns>Returns the index of the glyph used by the character using the Unicode value. Returns zero if no glyph exists for the given Unicode value.</returns>
        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        internal static extern uint GetGlyphIndex(uint unicode);


        /// <summary>
        /// Try to get the glyph index for the character at the given Unicode value.
        /// </summary>
        /// <param name="unicode">The unicode value of the character for which to lookup the glyph index.</param>
        /// <param name="glyphIndex">The index of the glyph for the given unicode character or the .notdef glyph (index 0) if no glyph is available for the given Unicode value.</param>
        /// <returns>Returns true if the given unicode has a glyph index.</returns>
        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphIndex", IsThreadSafe = true, IsFreeFunction = true)]
        public static extern bool TryGetGlyphIndex(uint unicode, out uint glyphIndex);


        /// <summary>
        /// Load the glyph at unicode value using the given load flags.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character whose glyph should be loaded.</param>
        /// <param name="flags">The Load Flags.</param>
        /// <returns>Returns a value of zero if the glyph was successfully loaded for the character using the Unicode value.</returns>
        internal static FontEngineError LoadGlyph(uint unicode, GlyphLoadFlags flags)
        {
            return (FontEngineError)LoadGlyph_Internal(unicode, flags);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadGlyph", IsThreadSafe = true, IsFreeFunction = true)]
        static extern int LoadGlyph_Internal(uint unicode, GlyphLoadFlags loadFlags);


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
        /// Render and add glyph to the provided texture.
        /// </summary>
        /// <param name="glyph">The Glyph that should be added into the provided texture.</param>
        /// <param name="padding">The padding value around the glyph.</param>
        /// <param name="renderMode">The Rendering Mode for the Glyph.</param>
        /// <param name="texture">The Texture to which the glyph should be added.</param>
        /// <returns>Returns a value of zero if the glyph was successfully added to the texture.</returns>
        internal static FontEngineError RenderGlyphToTexture(Glyph glyph, int padding, GlyphRenderMode renderMode, Texture2D texture)
        {
            GlyphMarshallingStruct glyphStruct = new GlyphMarshallingStruct(glyph);

            return (FontEngineError)RenderGlyphToTexture_Internal(glyphStruct, padding, renderMode, texture);
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphToTexture", IsFreeFunction = true)]
        extern static int RenderGlyphToTexture_Internal(GlyphMarshallingStruct glyphStruct, int padding, GlyphRenderMode renderMode, Texture2D texture);


        /// <summary>
        /// Render and add the glyphs in the provided list to the texture.
        /// </summary>
        /// <param name="glyphs">The list of glyphs to be rendered and added to the provided texture.</param>
        /// <param name="padding">The padding value around the glyphs.</param>
        /// <param name="renderMode">The rendering mode used to rasterize the glyphs.</param>
        /// <param name="texture">Returns a value of zero if the glyphs were successfully added to the texture.</param>
        /// <returns></returns>
        internal static FontEngineError RenderGlyphsToTexture(List<Glyph> glyphs, int padding, GlyphRenderMode renderMode, Texture2D texture)
        {
            int glyphCount = glyphs.Count;

            // Make sure marshaling arrays allocations are appropriate.
            if (s_GlyphMarshallingStruct_IN.Length < glyphCount)
            {
                int newSize = Mathf.NextPowerOfTwo(glyphCount + 1);
                s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[newSize];
            }

            // Copy data to marshalling buffers
            for (int i = 0; i < glyphCount; i++)
                s_GlyphMarshallingStruct_IN[i] = new GlyphMarshallingStruct(glyphs[i]);

            // Call extern function to render and add glyphs to texture.
            int error = RenderGlyphsToTexture_Internal(s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode, texture);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToTexture", IsFreeFunction = true)]
        extern static int RenderGlyphsToTexture_Internal(GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode, Texture2D texture);


        internal static FontEngineError RenderGlyphsToTexture(List<Glyph> glyphs, int padding, GlyphRenderMode renderMode, byte[] texBuffer, int texWidth, int texHeight)
        {
            int glyphCount = glyphs.Count;

            // Make sure marshaling arrays allocations are appropriate.
            if (s_GlyphMarshallingStruct_IN.Length < glyphCount)
            {
                int newSize = Mathf.NextPowerOfTwo(glyphCount + 1);
                s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[newSize];
            }

            // Copy data to marshalling buffers
            for (int i = 0; i < glyphCount; i++)
                s_GlyphMarshallingStruct_IN[i] = new GlyphMarshallingStruct(glyphs[i]);

            int error = RenderGlyphsToTextureBuffer_Internal(s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode, texBuffer, texWidth, texHeight);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToTextureBuffer", IsThreadSafe = true, IsFreeFunction = true)]
        extern static int RenderGlyphsToTextureBuffer_Internal(GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode, [Out] byte[] texBuffer, int texWidth, int texHeight);


        /// <summary>
        /// Internal function used to render and add glyphs to the cached shared texture data from outside the main thread.
        /// It is necessary to use SetSharedTextureData(texture) prior to calling this function.
        /// </summary>
        /// <param name="glyphs">The list of glyphs to be added into the provided texture.</param>
        /// <param name="padding">The padding value around the glyphs.</param>
        /// <param name="renderMode">The rendering mode used to rasterize the glyphs.</param>
        /// <returns></returns>
        internal static FontEngineError RenderGlyphsToSharedTexture(List<Glyph> glyphs, int padding, GlyphRenderMode renderMode)
        {
            int glyphCount = glyphs.Count;

            // Make sure marshaling arrays allocations are appropriate.
            if (s_GlyphMarshallingStruct_IN.Length < glyphCount)
            {
                int newSize = Mathf.NextPowerOfTwo(glyphCount + 1);
                s_GlyphMarshallingStruct_IN = new GlyphMarshallingStruct[newSize];
            }

            // Copy data to marshalling buffers
            for (int i = 0; i < glyphCount; i++)
                s_GlyphMarshallingStruct_IN[i] = new GlyphMarshallingStruct(glyphs[i]);

            int error = RenderGlyphsToSharedTexture_Internal(s_GlyphMarshallingStruct_IN, glyphCount, padding, renderMode);

            return (FontEngineError)error;
        }

        [NativeMethod(Name = "TextCore::FontEngine::RenderGlyphsToSharedTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static int RenderGlyphsToSharedTexture_Internal(GlyphMarshallingStruct[] glyphs, int glyphCount, int padding, GlyphRenderMode renderMode);


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
        /// Internal function used to add glyph to atlas texture.
        /// </summary>
        internal static bool TryAddGlyphToTexture(uint glyphIndex, int padding, GlyphPackingMode packingMode, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects, GlyphRenderMode renderMode, Texture2D texture, out Glyph glyph)
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
            if (TryAddGlyphToTexture_Internal(glyphIndex, padding, packingMode, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount, renderMode, texture, out glyphStruct))
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

        //
        [NativeMethod(Name = "TextCore::FontEngine::TryAddGlyphToTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryAddGlyphToTexture_Internal(uint glyphIndex, int padding,
            GlyphPackingMode packingMode, [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount,
            GlyphRenderMode renderMode, Texture2D texture, out GlyphMarshallingStruct glyph);

        /// <summary>
        /// Internal function used to add multiple glyphs to atlas texture.
        /// </summary>
        internal static bool TryAddGlyphsToTexture(List<uint> glyphIndexes, int padding, GlyphPackingMode packingMode, List<GlyphRect> freeGlyphRects, List<GlyphRect> usedGlyphRects, GlyphRenderMode renderMode, Texture2D texture, out Glyph[] glyphs)
        {
            glyphs = null;

            if (glyphIndexes == null || glyphIndexes.Count == 0)
                return false;

            int glyphCount = glyphIndexes.Count;

            // Make sure marshalling glyph index array allocations are appropriate.
            if (s_GlyphIndexesToMarshall == null || s_GlyphIndexesToMarshall.Length < glyphCount)
            {
                if (s_GlyphIndexesToMarshall == null)
                    s_GlyphIndexesToMarshall = new uint[glyphCount];
                else
                {
                    int newSize = Mathf.NextPowerOfTwo(glyphCount + 1);
                    s_GlyphIndexesToMarshall = new uint[newSize];
                }
            }

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

            // Copy glyph indexes and glyph rect data to marshalling arrays.
            int glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount, glyphCount);
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < glyphCount)
                    s_GlyphIndexesToMarshall[i] = glyphIndexes[i];

                if (i < freeGlyphRectCount)
                    s_FreeGlyphRects[i] = freeGlyphRects[i];

                if (i < usedGlyphRectCount)
                    s_UsedGlyphRects[i] = usedGlyphRects[i];
            }

            // Marshall data over to the native side.
            bool allGlyphsAdded = TryAddGlyphsToTexture_Internal(s_GlyphIndexesToMarshall, padding, packingMode, s_FreeGlyphRects, ref freeGlyphRectCount, s_UsedGlyphRects, ref usedGlyphRectCount, renderMode, texture, s_GlyphMarshallingStruct_OUT, ref glyphCount);

            // Allocate array of glyphs
            glyphs = new Glyph[glyphCount];

            freeGlyphRects.Clear();
            usedGlyphRects.Clear();

            // Copy marshalled free and used GlyphRect data over.
            glyphRectCount = Mathf.Max(freeGlyphRectCount, usedGlyphRectCount, glyphCount);
            for (int i = 0; i < glyphRectCount; i++)
            {
                if (i < glyphCount)
                    glyphs[i] = new Glyph(s_GlyphMarshallingStruct_OUT[i]);

                if (i < freeGlyphRectCount)
                    freeGlyphRects.Add(s_FreeGlyphRects[i]);

                if (i < usedGlyphRectCount)
                    usedGlyphRects.Add(s_UsedGlyphRects[i]);
            }

            return allGlyphsAdded;
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryAddGlyphsToTexture", IsThreadSafe = true, IsFreeFunction = true)]
        extern static bool TryAddGlyphsToTexture_Internal(uint[] glyphIndex, int padding,
            GlyphPackingMode packingMode, [Out] GlyphRect[] freeGlyphRects, ref int freeGlyphRectCount, [Out] GlyphRect[] usedGlyphRects, ref int usedGlyphRectCount,
            GlyphRenderMode renderMode, Texture2D texture, [Out] GlyphMarshallingStruct[] glyphs, ref int glyphCount);

        /// <summary>
        /// Internal function used to retrieve positional adjustments for pairs of glyphs.
        /// </summary>
        /// <param name="glyphIndexes">List of glyph indexes to check for potential positional adjustment records.</param>
        /// <returns>Array containing the positional adjustments for pairs of glyphs.</returns>
        internal static GlyphPairAdjustmentRecord[] GetGlyphPairAdjustmentTable(uint[] glyphIndexes)
        {
            int maxGlyphPairAdjustmentRecords = glyphIndexes.Length * glyphIndexes.Length;

            if (s_GlyphPairAdjustmentRecords == null || s_GlyphPairAdjustmentRecords.Length < maxGlyphPairAdjustmentRecords)
            {
                s_GlyphPairAdjustmentRecords = new GlyphPairAdjustmentRecord[maxGlyphPairAdjustmentRecords];
            }

            int adjustmentRecordCount;
            if (GetGlyphPairAdjustmentTable_Internal(glyphIndexes, s_GlyphPairAdjustmentRecords, out adjustmentRecordCount) != 0)
            {
                // TODO: Add debug warning messages.
                return null;
            }

            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = new GlyphPairAdjustmentRecord[adjustmentRecordCount];

            for (int i = 0; i < adjustmentRecordCount; i++)
            {
                pairAdjustmentRecords[i] = s_GlyphPairAdjustmentRecords[i];
            }

            return pairAdjustmentRecords;
        }

        /// <summary>
        ///
        /// </summary>
        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphPairAdjustmentTable", IsFreeFunction = true)]
        extern static int GetGlyphPairAdjustmentTable_Internal(uint[] glyphIndexes, [Out] GlyphPairAdjustmentRecord[] glyphPairAdjustmentRecords, out int adjustmentRecordCount);

        // ================================================
        // Experimental / Testing / Benchmarking Functions
        // ================================================

        /// <summary>
        /// Internal function used to reset an atlas texture to black
        /// </summary>
        /// <param name="srcTexture"></param>
        [NativeMethod(Name = "TextCore::FontEngine::ResetAtlasTexture", IsFreeFunction = true)]
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
    }
}
