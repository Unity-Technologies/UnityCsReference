// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        LOAD_COLOR = 1 << 20,
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

        // Glyph related errors.
        Invalid_Glyph_Index     = 0x10,
        Invalid_Character_Code  = 0x11,
        Invalid_Pixel_Size      = 0x17,

        // Additional errors codes will be added as necessary to cover new FontEngine features and functionality.
    }


    /// <summary>
    /// Rendering modes used by the Font Engine to render glyphs.
    /// </summary>
    [UsedByNativeCode]
    public enum GlyphRenderModes
    {
        SMOOTH_HINTED = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        SMOOTH = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        RASTER_HINTED = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,
        RASTER = GlyphRasterModes.RASTER_MODE_NO_HINTING | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_BITMAP | GlyphRasterModes.RASTER_MODE_1X,

        SDF = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_1X,
        SDF8 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_8X,
        SDF16 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_16X,
        SDF32 = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_MONO | GlyphRasterModes.RASTER_MODE_SDF | GlyphRasterModes.RASTER_MODE_32X,

        SDFAA = GlyphRasterModes.RASTER_MODE_HINTED | GlyphRasterModes.RASTER_MODE_8BIT | GlyphRasterModes.RASTER_MODE_SDFAA | GlyphRasterModes.RASTER_MODE_1X,
        //MSDF  = RasterModes.RASTER_MODE_HINTED | RasterModes.RASTER_MODE_8BIT | RasterModes.RASTER_MODE_MSDF | RasterModes.RASTER_MODE_1X,
        //MSDFA = RasterModes.RASTER_MODE_HINTED | RasterModes.RASTER_MODE_8BIT | RasterModes.RASTER_MODE_MSDFA | RasterModes.RASTER_MODE_1X,
    }


    [NativeHeader("Modules/TextCore/Native/FontEngine.h")]
    public sealed class FontEngine
    {
        private static readonly FontEngine s_Instance = new FontEngine();

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
            return (FontEngineError)LoadFontFace_With_Size_FromSourceFontFile_Internal(sourceFontFile, pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::LoadFontFace", IsFreeFunction = true)]
        static extern int LoadFontFace_With_Size_FromSourceFontFile_Internal(byte[] sourceFontFile, int pointSize);


        /// <summary>
        /// Set the size of the currently loaded font face.
        /// </summary>
        /// <param name="pointSize">The point size used to scale the font face.</param>
        /// <returns>Returns a value of zero if the font face was successfully scaled to the given point size.</returns>
        public static FontEngineError SetFaceSize(int pointSize)
        {
            return (FontEngineError)SetFaceSize_Internal(pointSize);
        }

        [NativeMethod(Name = "TextCore::FontEngine::SetFaceSize", IsFreeFunction = true)]
        static extern int SetFaceSize_Internal(int pointSize);


        /// <summary>
        /// Get the index of the glyph for the character mapped at Unicode value.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character for which to lookup the glyph index.</param>
        /// <returns>Returns the index of the glyph used by the character using the Unicode value. Returns zero if no glyph exists for the given Unicode value.</returns>
        [NativeMethod(Name = "TextCore::FontEngine::GetGlyphIndex", IsFreeFunction = true)]
        public static extern int GetGlyphIndex(uint unicode);


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

        [NativeMethod(Name = "TextCore::FontEngine::LoadGlyph", IsFreeFunction = true)]
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
            glyph = new Glyph();

            return TryGetGlyphWithUnicodeValue_Internal(unicode, flags, glyph);
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithUnicodeValue", IsFreeFunction = true)]
        static extern bool TryGetGlyphWithUnicodeValue_Internal(uint unicode, GlyphLoadFlags loadFlags, [Out] Glyph glyph);


        /// <summary>
        /// Try loading the glyph for the given index value and if available populate the glyph.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph that should be loaded.</param>
        /// <param name="flags">The Load Flags.</param>
        /// <param name="glyph">The glyph using the provided index or the .notdef glyph (index 0) if no glyph was found at that index.</param>
        /// <returns>Returns true if a glyph exists at the given index. Otherwise returns false.</returns>
        public static bool TryGetGlyphWithIndexValue(uint glyphIndex, GlyphLoadFlags flags, out Glyph glyph)
        {
            glyph = new Glyph();

            return TryGetGlyphWithIndexValue_Internal(glyphIndex, flags, glyph);
        }

        [NativeMethod(Name = "TextCore::FontEngine::TryGetGlyphWithIndexValue", IsFreeFunction = true)]
        static extern bool TryGetGlyphWithIndexValue_Internal(uint glyphIndex, GlyphLoadFlags loadFlags, [Out] Glyph glyph);


        /// <summary>
        /// Try rasterizing and adding the given glyph to the provided texture.
        /// </summary>
        /// <param name="glyph">The Glyph that should be added into the provided texture.</param>
        /// <param name="padding">The padding value around the glyph.</param>
        /// <param name="renderMode">The Rendering Mode for the Glyph.</param>
        /// <param name="texture">The Texture to which the glyph should be added.</param>
        /// <returns>Returns a value of zero if the glyph was successfully added to the texture.</returns>
        public static FontEngineError AddGlyphToTexture(Glyph glyph, int padding, GlyphRenderModes renderMode, Texture2D texture)
        {
            return (FontEngineError)AddGlyphToTexture_Internal(glyph, padding, renderMode, texture);
        }

        [NativeMethod(Name = "TextCore::FontEngine::AddGlyphToTexture", IsFreeFunction = true)]
        extern static int AddGlyphToTexture_Internal(Glyph glyph, int padding, GlyphRenderModes renderMode, Texture2D texture);


        /// <summary>
        /// Try rasterizing and adding the given list of glyphs to the provided texture.
        /// </summary>
        /// <param name="glyphs">The list of glyphs to be added into the provided texture.</param>
        /// <param name="padding">The padding value around the glyphs.</param>
        /// <param name="renderMode">The rendering mode used rasterize the glyphs.</param>
        /// <param name="texture">Returns a value of zero if the glyphs were successfully added to the texture.</param>
        /// <returns></returns>
        public static FontEngineError AddGlyphsToTexture(Glyph[] glyphs, int padding, GlyphRenderModes renderMode, Texture2D texture)
        {
            return (FontEngineError)AddGlyphsToTextureFromArray_Internal(glyphs, padding, renderMode, texture);
        }

        [NativeMethod(Name = "TextCore::FontEngine::AddGlyphsToTexture", IsFreeFunction = true)]
        extern static int AddGlyphsToTextureFromArray_Internal(Glyph[] glyphs, int padding, GlyphRenderModes renderMode, Texture2D texture);
    }
}
