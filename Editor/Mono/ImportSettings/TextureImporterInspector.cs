// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor.Build;
using System.Collections.Generic;
using System;
using UnityEditor.AssetImporters;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;
using VirtualTexturing = UnityEngine.Rendering.VirtualTexturing;

namespace UnityEditor
{
    // Cubemap convolution mode
    public enum TextureImporterCubemapConvolution
    {
        // Do not convolve cubemap (default).
        None = 0,
        // Convolve for specular reflections with varying roughness.
        Specular = 1,
        // Convolve for diffuse-only reflection (irradiance cubemap).
        Diffuse = 2
    }

    // Kept for backward compatibility
    public enum TextureImporterRGBMMode
    {
        Auto = 0,
        On = 1,
        Off = 2,
        Encoded = 3,
    }

    [CustomEditor(typeof(TextureImporter))]
    [CanEditMultipleObjects]
    internal class TextureImporterInspector : AssetImporterEditor
    {
        public static string s_DefaultPlatformName = "DefaultTexturePlatform";

        [Flags]
        private enum TextureInspectorGUIElement
        {
            None = 0,
            PowerOfTwo = 1 << 0,
            Readable = 1 << 1,
            AlphaHandling = 1 << 2,
            ColorSpace = 1 << 3,
            MipMaps = 1 << 4,
            NormalMap = 1 << 5,
            Sprite = 1 << 6,
            Cookie = 1 << 7,
            CubeMapConvolution = 1 << 8,
            CubeMapping = 1 << 9,
            SingleChannelComponent = 1 << 11,
            PngGamma = 1 << 12,
            VTOnly = 1 << 13,
            ElementsAtlas = 1 << 14,
            Swizzle = 1 << 15,
        }

        private struct TextureInspectorTypeGUIProperties
        {
            public TextureInspectorGUIElement   commonElements;
            public TextureInspectorGUIElement   advancedElements;
            public TextureImporterShape         shapeCaps;

            public TextureInspectorTypeGUIProperties(TextureInspectorGUIElement _commonElements, TextureInspectorGUIElement _advancedElements, TextureImporterShape _shapeCaps)
            {
                commonElements          = _commonElements;
                advancedElements        = _advancedElements;
                shapeCaps               = _shapeCaps;
            }
        }

        SerializedProperty m_TextureType;
        internal TextureImporterType textureType
        {
            get
            {
                if (m_TextureType.hasMultipleDifferentValues)
                    return (TextureImporterType)0;
                return (TextureImporterType)(m_TextureType.intValue);
            }
        }

        private delegate void GUIMethod(TextureInspectorGUIElement guiElements);
        private Dictionary<TextureInspectorGUIElement, GUIMethod> m_GUIElementMethods = new Dictionary<TextureInspectorGUIElement, GUIMethod>();


        internal bool textureTypeHasMultipleDifferentValues
        {
            get { return m_TextureType.hasMultipleDifferentValues; }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            EditorPrefs.SetBool("TextureImporterShowAdvanced", m_ShowAdvanced);
        }

        // Don't show the imported texture as a separate editor
        public override bool showImportedObject { get { return false; } }

        public static bool IsCompressedDXTTextureFormat(TextureImporterFormat format)
        {
            return (format == TextureImporterFormat.DXT1 || format == TextureImporterFormat.DXT5);
        }

        // Which platforms should we display?
        // For each of these, what are the formats etc. to display?
        [SerializeField]
        internal List<TextureImportPlatformSettings> m_PlatformSettings;

        TextureInspector textureInspector
        {
            get => preview as TextureInspector;
        }

        internal static readonly TextureImporterFormat[] kFormatsWithCompressionSettings =
        {
            TextureImporterFormat.DXT1Crunched,
            TextureImporterFormat.DXT5Crunched,
            TextureImporterFormat.ETC_RGB4Crunched,
            TextureImporterFormat.ETC2_RGBA8Crunched,
            TextureImporterFormat.PVRTC_RGB2,
            TextureImporterFormat.PVRTC_RGB4,
            TextureImporterFormat.PVRTC_RGBA2,
            TextureImporterFormat.PVRTC_RGBA4,
            TextureImporterFormat.ETC_RGB4,
            TextureImporterFormat.ETC2_RGB4,
            TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            TextureImporterFormat.ETC2_RGBA8,
            TextureImporterFormat.ASTC_4x4,
            TextureImporterFormat.ASTC_5x5,
            TextureImporterFormat.ASTC_6x6,
            TextureImporterFormat.ASTC_8x8,
            TextureImporterFormat.ASTC_10x10,
            TextureImporterFormat.ASTC_12x12,
            TextureImporterFormat.ASTC_HDR_4x4,
            TextureImporterFormat.ASTC_HDR_5x5,
            TextureImporterFormat.ASTC_HDR_6x6,
            TextureImporterFormat.ASTC_HDR_8x8,
            TextureImporterFormat.ASTC_HDR_10x10,
            TextureImporterFormat.ASTC_HDR_12x12,
            TextureImporterFormat.BC6H,
            TextureImporterFormat.BC7
        };

        readonly AnimBool m_ShowBumpGenerationSettings = new AnimBool();
        readonly AnimBool m_ShowCubeMapSettings = new AnimBool();
        readonly AnimBool m_ShowElementsAtlasSettings = new AnimBool();
        readonly AnimBool m_ShowGenericSpriteSettings = new AnimBool();
        readonly AnimBool m_ShowMipMapSettings = new AnimBool();
        readonly AnimBool m_ShowSpriteMeshTypeOption = new AnimBool();
        readonly GUIContent m_EmptyContent = new GUIContent(" ");

        readonly int[] m_FilterModeOptions = (int[])(Enum.GetValues(typeof(FilterMode)));

        string  m_ImportWarning = null;
        private void UpdateImportWarning()
        {
            TextureImporter importer = target as TextureImporter;
            m_ImportWarning = importer ? importer.GetImportWarnings() : null;
        }

        internal class Styles
        {
            public readonly GUIContent textureTypeTitle = EditorGUIUtility.TrTextContent("Texture Type", "What will this texture be used for?");
            public readonly GUIContent[] textureTypeOptions =
            {
                EditorGUIUtility.TrTextContent("Default", "Texture is a normal image such as a diffuse texture or other."),
                EditorGUIUtility.TrTextContent("Normal map", "Texture is a bump or normal map."),
                EditorGUIUtility.TrTextContent("Editor GUI and Legacy GUI", "Texture is used for a GUI element."),
                EditorGUIUtility.TrTextContent("Sprite (2D and UI)", "Texture is used for a sprite."),
                EditorGUIUtility.TrTextContent("Cursor", "Texture is used for a cursor."),
                EditorGUIUtility.TrTextContent("Cookie", "Texture is a cookie you put on a light."),
                EditorGUIUtility.TrTextContent("Lightmap", "Texture is a lightmap."),
                EditorGUIUtility.TrTextContent("Directional Lightmap", "Texture is a directional lightmap."),
                EditorGUIUtility.TrTextContent("Shadowmask", "Texture is a shadowmask texture."),
                EditorGUIUtility.TrTextContent("Single Channel", "Texture is a one component texture."),
            };
            public readonly int[] textureTypeValues =
            {
                (int)TextureImporterType.Default,
                (int)TextureImporterType.NormalMap,
                (int)TextureImporterType.GUI,
                (int)TextureImporterType.Sprite,
                (int)TextureImporterType.Cursor,
                (int)TextureImporterType.Cookie,
                (int)TextureImporterType.Lightmap,
                (int)TextureImporterType.DirectionalLightmap,
                (int)TextureImporterType.Shadowmask,
                (int)TextureImporterType.SingleChannel,
            };

            public readonly GUIContent textureShape = EditorGUIUtility.TrTextContent("Texture Shape", "What shape is this texture?");
            readonly GUIContent textureShape2D = EditorGUIUtility.TrTextContent("2D", "Texture is 2D.");
            readonly GUIContent textureShapeCube = EditorGUIUtility.TrTextContent("Cube", "Texture is a Cubemap.");
            readonly GUIContent textureShape2DArray = EditorGUIUtility.TrTextContent("2D Array", "Texture is a 2D Array.");
            readonly GUIContent textureShape3D = EditorGUIUtility.TrTextContent("3D", "Texture is 3D.");
            public readonly Dictionary<TextureImporterShape, GUIContent[]> textureShapeOptionsDictionnary = new Dictionary<TextureImporterShape, GUIContent[]>();
            public readonly Dictionary<TextureImporterShape, int[]> textureShapeValuesDictionnary = new Dictionary<TextureImporterShape, int[]>();

            public readonly GUIContent defaultPlatform = EditorGUIUtility.TrTextContent("Default");

            public readonly GUIContent filterMode = EditorGUIUtility.TrTextContent("Filter Mode");
            public readonly GUIContent[] filterModeOptions =
            {
                EditorGUIUtility.TrTextContent("Point (no filter)"),
                EditorGUIUtility.TrTextContent("Bilinear"),
                EditorGUIUtility.TrTextContent("Trilinear")
            };

            public readonly GUIContent cookieType = EditorGUIUtility.TrTextContent("Light Type");
            public readonly GUIContent[] cookieOptions =
            {
                EditorGUIUtility.TrTextContent("Spotlight"),
                EditorGUIUtility.TrTextContent("Directional"),
                EditorGUIUtility.TrTextContent("Point"),
            };
            public readonly GUIContent generateFromBump = EditorGUIUtility.TrTextContent("Create from Grayscale", "The grayscale of the image is used as a heightmap for generating the normal map.");
            public readonly GUIContent bumpiness = EditorGUIUtility.TrTextContent("Bumpiness");
            public readonly GUIContent bumpFiltering = EditorGUIUtility.TrTextContent("Filtering");
            public readonly GUIContent[] bumpFilteringOptions =
            {
                EditorGUIUtility.TrTextContent("Sharp"),
                EditorGUIUtility.TrTextContent("Smooth"),
            };

            public readonly GUIContent flipGreenChannel = EditorGUIUtility.TrTextContent("Flip Green Channel",
                "Invert values in the normal map green (Y) channel. Use on normal maps that were produced for non-Unity normal orientation convention.");
            public readonly GUIContent swizzle = EditorGUIUtility.TrTextContent("Swizzle",
                "Reorder and invert texture color channels. For each of R,G,B,A channels pick where the channel data comes from.");

            public readonly GUIContent cubemap = EditorGUIUtility.TrTextContent("Mapping");
            public readonly GUIContent[] cubemapOptions =
            {
                EditorGUIUtility.TrTextContent("Auto"),
                EditorGUIUtility.TrTextContent("6 Frames Layout (Cubic Environment)", "Texture contains 6 images arranged in one of the standard cubemap layouts - cross or sequence (+x, -x, +y, -y, +z, -z). Texture can be in vertical or horizontal orientation."),
                EditorGUIUtility.TrTextContent("Latitude-Longitude Layout (Cylindrical)", "Texture contains an image of a ball unwrapped such that latitude and longitude are mapped to horizontal and vertical dimensions (as on a globe)."),
                EditorGUIUtility.TrTextContent("Mirrored Ball (Spheremap)", "Texture contains an image of a mirrored ball.")
            };
            public readonly int[] cubemapValues2 =
            {
                (int)TextureImporterGenerateCubemap.AutoCubemap,
                (int)TextureImporterGenerateCubemap.FullCubemap,
                (int)TextureImporterGenerateCubemap.Cylindrical,
                (int)TextureImporterGenerateCubemap.Spheremap
            };

            public readonly GUIContent cubemapConvolution = EditorGUIUtility.TrTextContent("Convolution Type");
            public readonly GUIContent[] cubemapConvolutionOptions =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("Specular (Glossy Reflection)", "Convolve cubemap for specular reflections with varying smoothness (Glossy Reflections)."),
                EditorGUIUtility.TrTextContent("Diffuse (Irradiance)", "Convolve cubemap for diffuse-only reflection (Irradiance Cubemap).")
            };
            public readonly int[] cubemapConvolutionValues =
            {
                (int)TextureImporterCubemapConvolution.None,
                (int)TextureImporterCubemapConvolution.Specular,
                (int)TextureImporterCubemapConvolution.Diffuse
            };

            public readonly GUIContent seamlessCubemap = EditorGUIUtility.TrTextContent("Fixup Edge Seams", "Enable if this texture is used for glossy reflections.");
            public readonly GUIContent textureFormat = EditorGUIUtility.TrTextContent("Format");

            public readonly GUIContent mipmapFadeOutToggle = EditorGUIUtility.TrTextContent("Fadeout to Gray");
            public readonly GUIContent mipmapFadeStartMip = EditorGUIUtility.TrTextContent("Fade Start Mip");
            public readonly GUIContent mipmapFadeEndMip = EditorGUIUtility.TrTextContent("Fade End Mip");
            public readonly GUIContent mipmapFadeOut = EditorGUIUtility.TrTextContent("Fade Range");
            public readonly GUIContent readWrite = EditorGUIUtility.TrTextContent("Read/Write", "Enable to be able to access the raw pixel data from code.");
            public readonly GUIContent useMipmapLimits = EditorGUIUtility.TrTextContent("Use Mipmap Limits", "Disable this if the number of mips to upload should not be limited by the quality settings. (effectively: always upload at full resolution, regardless of the global mipmap limit or mipmap limit groups)");
            public readonly GUIContent mipmapLimitGroupName = EditorGUIUtility.TrTextContent("Mipmap Limit Group", "Select a Mipmap Limit Group for this texture. If you do not add this texture to a Mipmap Limit Group, or Unity cannot find the group name you provide, Unity limits the number of mips it uploads to the maximum defined by the Global Texture Mipmap Limit (see Quality Settings). If Unity can find the Mipmap Limit Group you specify, it respects that group's limit.");
            public readonly GUIContent mipmapLimitGroupWarning = EditorGUIUtility.TrTextContent("This texture takes the default mipmap limit settings because Unity cannot find the mipmap limit group you have designated. Consult your project's Quality Settings for a list of mipmap limit groups.");
            public readonly GUIContent streamingMipmaps = EditorGUIUtility.TrTextContent("Mip Streaming", "Only load larger mipmaps as needed to render the current game cameras. Requires texture streaming to be enabled in quality settings.");
            public readonly GUIContent streamingMipmapsPriority = EditorGUIUtility.TrTextContent("Priority", "Mipmap streaming priority when there's contention for resources. Positive numbers represent higher priority. Valid range is -128 to 127.");
            public readonly GUIContent vtOnly = EditorGUIUtility.TrTextContent("Virtual Texture Only", "Texture is optimized for use as a virtual texture and can only be used as a virtual texture.");

            public readonly GUIContent alphaSource = EditorGUIUtility.TrTextContent("Alpha Source", "How is the alpha generated for the imported texture.");
            public readonly GUIContent[] alphaSourceOptions =
            {
                EditorGUIUtility.TrTextContent("None", "No Alpha will be used."),
                EditorGUIUtility.TrTextContent("Input Texture Alpha", "Use Alpha from the input texture if one is provided."),
                EditorGUIUtility.TrTextContent("From Gray Scale", "Generate Alpha from image gray scale."),
            };
            public readonly int[] alphaSourceValues =
            {
                (int)TextureImporterAlphaSource.None,
                (int)TextureImporterAlphaSource.FromInput,
                (int)TextureImporterAlphaSource.FromGrayScale,
            };

            public readonly GUIContent singleChannelComponent = EditorGUIUtility.TrTextContent("Channel", "As which color/alpha component the single channel texture is treated.");
            public readonly GUIContent[] singleChannelComponentOptions =
            {
                EditorGUIUtility.TrTextContent("Alpha", "Use the alpha channel (compression not supported)."),
                EditorGUIUtility.TrTextContent("Red", "Use the red color component."),
            };
            public readonly int[] singleChannelComponentValues =
            {
                (int)TextureImporterSingleChannelComponent.Alpha,
                (int)TextureImporterSingleChannelComponent.Red,
            };

            public readonly GUIContent generateMipMaps = EditorGUIUtility.TrTextContent("Generate Mipmaps", "Create progressively smaller versions of the texture, for reduced texture shimmering and better GPU performance when the texture is viewed at a distance.");
            public readonly GUIContent sRGBTexture = EditorGUIUtility.TrTextContent("sRGB (Color Texture)", "Texture content is stored in gamma space. Non-HDR color textures should enable this flag (except if used for IMGUI).");
            public readonly GUIContent borderMipMaps = EditorGUIUtility.TrTextContent("Replicate Border", "Replicate pixel values from texture borders into smaller mipmap levels. Mostly used for Cookie texture types.");
            public readonly GUIContent mipMapsPreserveCoverage = EditorGUIUtility.TrTextContent("Preserve Coverage", "The alpha channel of generated mipmaps will preserve coverage for the alpha test. Useful for foliage textures.");
            public readonly GUIContent alphaTestReferenceValue = EditorGUIUtility.TrTextContent("Alpha Cutoff", "The reference value used during the alpha test. Controls mipmap coverage.");
            public readonly GUIContent mipMapFilter = EditorGUIUtility.TrTextContent("Mipmap Filtering");
            public readonly GUIContent[] mipMapFilterOptions =
            {
                EditorGUIUtility.TrTextContent("Box"),
                EditorGUIUtility.TrTextContent("Kaiser"),
            };
            public readonly GUIContent npot = EditorGUIUtility.TrTextContent("Non-Power of 2", "How non-power-of-two textures are scaled on import.");
            public readonly GUIContent generateCubemap = EditorGUIUtility.TrTextContent("Generate Cubemap");

            public readonly GUIContent spriteMode = EditorGUIUtility.TrTextContent("Sprite Mode");
            public readonly GUIContent[] spriteModeOptions =
            {
                EditorGUIUtility.TrTextContent("Single"),
                EditorGUIUtility.TrTextContent("Multiple"),
                EditorGUIUtility.TrTextContent("Polygon"),
            };
            public readonly GUIContent[] spriteMeshTypeOptions =
            {
                EditorGUIUtility.TrTextContent("Full Rect"),
                EditorGUIUtility.TrTextContent("Tight"),
            };

            public readonly GUIContent spritePixelsPerUnit = EditorGUIUtility.TrTextContent("Pixels Per Unit", "How many pixels in the sprite correspond to one unit in the world.");
            public readonly GUIContent spriteExtrude = EditorGUIUtility.TrTextContent("Extrude Edges", "How much empty area to leave around the sprite in the generated mesh.");
            public readonly GUIContent spriteMeshType = EditorGUIUtility.TrTextContent("Mesh Type", "Type of sprite mesh to generate.");
            public readonly GUIContent spriteAlignment = EditorGUIUtility.TrTextContent("Pivot", "Sprite pivot point in its localspace. May be used for syncing animation frames of different sizes.");
            public readonly GUIContent[] spriteAlignmentOptions =
            {
                EditorGUIUtility.TrTextContent("Center"),
                EditorGUIUtility.TrTextContent("Top Left"),
                EditorGUIUtility.TrTextContent("Top"),
                EditorGUIUtility.TrTextContent("Top Right"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("Bottom Left"),
                EditorGUIUtility.TrTextContent("Bottom"),
                EditorGUIUtility.TrTextContent("Bottom Right"),
                EditorGUIUtility.TrTextContent("Custom"),
            };
            public readonly GUIContent spriteGenerateFallbackPhysicsShape = EditorGUIUtility.TrTextContent("Generate Physics Shape", "Generates a default physics shape from the outline of the Sprite/s when a physics shape has not been set in the Sprite Editor.");

            public readonly GUIContent alphaIsTransparency = EditorGUIUtility.TrTextContent("Alpha Is Transparency", "If the alpha channel of your texture represents transparency, enable this property to dilate the color channels of visible texels into fully transparent areas. This effectively adds padding around transparent areas that prevents filtering artifacts from forming on their edges. Unity does not support this property for HDR textures. \n\nThis property makes the color data of invisible texels undefined. Disable this property to preserve invisible texels' original color data.");

            public readonly GUIContent showAdvanced = EditorGUIUtility.TrTextContent("Advanced", "Show advanced settings.");

            public readonly GUIContent psdRemoveMatte = EditorGUIUtility.TrTextContent("Remove PSD Matte", "Enable special processing for PSD that has transparency, as color pixels will be tweaked (blended with white color).");

            public readonly GUIContent ignorePngGamma = EditorGUIUtility.TrTextContent("Ignore PNG Gamma", "Ignore the Gamma attribute value in PNG files.");
            public readonly GUIContent readWriteWarning = EditorGUIUtility.TrTextContent("Textures larger than 8192 can not be Read/Write enabled. Value will be ignored.");

            public readonly GUIContent flipbookColumns = EditorGUIUtility.TrTextContent("Columns", "Source image is divided into this amount of columns.");
            public readonly GUIContent flipbookRows = EditorGUIUtility.TrTextContent("Rows", "Source image is divided into this amount of rows.");

            public Styles()
            {
                // This is far from ideal, but it's better than having tons of logic in the GUI code itself.
                // The combination should not grow too much.
                GUIContent[] s2D_Options = { textureShape2D };
                GUIContent[] sCube_Options = { textureShapeCube };
                GUIContent[] s2D_Cube_Options = { textureShape2D, textureShapeCube };
                GUIContent[] sAll_Options = { textureShape2D, textureShapeCube, textureShape2DArray, textureShape3D };
                textureShapeOptionsDictionnary.Add(TextureImporterShape.Texture2D, s2D_Options);
                textureShapeOptionsDictionnary.Add(TextureImporterShape.TextureCube, sCube_Options);
                textureShapeOptionsDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube, s2D_Cube_Options);
                textureShapeOptionsDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube | TextureImporterShape.Texture2DArray | TextureImporterShape.Texture3D, sAll_Options);

                int[] s2D_Values = { (int)TextureImporterShape.Texture2D };
                int[] sCube_Values = { (int)TextureImporterShape.TextureCube };
                int[] s2D_Cube_Values = { (int)TextureImporterShape.Texture2D, (int)TextureImporterShape.TextureCube };
                int[] sAll_Values = { (int)TextureImporterShape.Texture2D, (int)TextureImporterShape.TextureCube, (int)TextureImporterShape.Texture2DArray, (int)TextureImporterShape.Texture3D };
                textureShapeValuesDictionnary.Add(TextureImporterShape.Texture2D, s2D_Values);
                textureShapeValuesDictionnary.Add(TextureImporterShape.TextureCube, sCube_Values);
                textureShapeValuesDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube, s2D_Cube_Values);
                textureShapeValuesDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube | TextureImporterShape.Texture2DArray | TextureImporterShape.Texture3D, sAll_Values);
            }
        }

        internal static Styles s_Styles;

        TextureInspectorTypeGUIProperties[] m_TextureTypeGUIElements = new TextureInspectorTypeGUIProperties[Enum.GetValues(typeof(TextureImporterType)).Length];
        List<TextureInspectorGUIElement>    m_GUIElementsDisplayOrder = new List<TextureInspectorGUIElement>();


        void ToggleFromInt(SerializedProperty property, GUIContent label)
        {
            var content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), label, property);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            int value = EditorGUILayout.Toggle(content, property.intValue > 0) ? 1 : 0;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.intValue = value;
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }

        void EnumPopup(SerializedProperty property, System.Type type, GUIContent label)
        {
            EditorGUILayout.IntPopup(property,
                EditorGUIUtility.TempContent(System.Enum.GetNames(type)),
                System.Enum.GetValues(type) as int[],
                label);
        }

        SerializedProperty m_AlphaSource;
        SerializedProperty m_ConvertToNormalMap;
        SerializedProperty m_HeightScale;
        SerializedProperty m_NormalMapFilter;
        SerializedProperty m_FlipGreenChannel;
        SerializedProperty m_Swizzle;
        SerializedProperty m_GenerateCubemap;
        SerializedProperty m_CubemapConvolution;
        SerializedProperty m_SeamlessCubemap;
        SerializedProperty m_BorderMipMap;
        SerializedProperty m_MipMapsPreserveCoverage;
        SerializedProperty m_AlphaTestReferenceValue;
        SerializedProperty m_NPOTScale;
        SerializedProperty m_IsReadable;
        SerializedProperty m_StreamingMipmaps;
        SerializedProperty m_StreamingMipmapsPriority;
        SerializedProperty m_IgnoreMipmapLimit;
        SerializedProperty m_MipmapLimitGroupName;

        SerializedProperty m_VTOnly;
        SerializedProperty m_sRGBTexture;
        SerializedProperty m_EnableMipMap;
        SerializedProperty m_MipMapMode;
        SerializedProperty m_FadeOut;
        SerializedProperty m_MipMapFadeDistanceStart;
        SerializedProperty m_MipMapFadeDistanceEnd;

        SerializedProperty m_Aniso;
        SerializedProperty m_FilterMode;
        SerializedProperty m_WrapU;
        SerializedProperty m_WrapV;
        SerializedProperty m_WrapW;

        SerializedProperty m_SpritePixelsToUnits;
        SerializedProperty m_SpriteExtrude;
        SerializedProperty m_SpriteMeshType;
        SerializedProperty m_Alignment;
        SerializedProperty m_SpritePivot;
        SerializedProperty m_SpriteGenerateFallbackPhysicsShape;

        SerializedProperty m_AlphaIsTransparency;
        SerializedProperty m_IgnorePngGamma;
        SerializedProperty m_PSDRemoveMatte;

        SerializedProperty m_TextureShape;

        SerializedProperty m_SpriteMode;

        SerializedProperty m_FlipbookRows;
        SerializedProperty m_FlipbookColumns;

        SerializedProperty m_SingleChannelComponent;

        SerializedProperty m_CookieLightType;

        SerializedProperty m_PlatformSettingsArrProp;

        List<TextureImporterType> m_TextureTypes;
        internal SpriteImportMode spriteImportMode
        {
            get
            {
                return (SpriteImportMode)m_SpriteMode.intValue;
            }
        }

        bool m_ShowAdvanced = false;

        int     m_TextureWidth = 0;
        int     m_TextureHeight = 0;
        bool    m_IsPOT = false;
        bool    m_IsPNG, m_IsPSD;

        void CacheSerializedProperties()
        {
            m_AlphaSource = serializedObject.FindProperty("m_AlphaUsage");
            m_ConvertToNormalMap = serializedObject.FindProperty("m_ConvertToNormalMap");
            m_HeightScale = serializedObject.FindProperty("m_HeightScale");
            m_NormalMapFilter = serializedObject.FindProperty("m_NormalMapFilter");
            m_FlipGreenChannel = serializedObject.FindProperty("m_FlipGreenChannel");
            m_Swizzle = serializedObject.FindProperty("m_Swizzle");
            m_GenerateCubemap = serializedObject.FindProperty("m_GenerateCubemap");
            m_SeamlessCubemap = serializedObject.FindProperty("m_SeamlessCubemap");
            m_BorderMipMap = serializedObject.FindProperty("m_BorderMipMap");
            m_MipMapsPreserveCoverage = serializedObject.FindProperty("m_MipMapsPreserveCoverage");
            m_AlphaTestReferenceValue = serializedObject.FindProperty("m_AlphaTestReferenceValue");
            m_NPOTScale = serializedObject.FindProperty("m_NPOTScale");
            m_IsReadable = serializedObject.FindProperty("m_IsReadable");
            m_StreamingMipmaps = serializedObject.FindProperty("m_StreamingMipmaps");
            m_StreamingMipmapsPriority = serializedObject.FindProperty("m_StreamingMipmapsPriority");
            m_IgnoreMipmapLimit = serializedObject.FindProperty("m_IgnoreMipmapLimit");
            m_MipmapLimitGroupName = serializedObject.FindProperty("m_MipmapLimitGroupName");
            m_VTOnly = serializedObject.FindProperty("m_VTOnly");
            m_sRGBTexture = serializedObject.FindProperty("m_sRGBTexture");
            m_EnableMipMap = serializedObject.FindProperty("m_EnableMipMap");
            m_MipMapMode = serializedObject.FindProperty("m_MipMapMode");
            m_FadeOut = serializedObject.FindProperty("m_FadeOut");
            m_MipMapFadeDistanceStart = serializedObject.FindProperty("m_MipMapFadeDistanceStart");
            m_MipMapFadeDistanceEnd = serializedObject.FindProperty("m_MipMapFadeDistanceEnd");

            m_Aniso = serializedObject.FindProperty("m_TextureSettings.m_Aniso");
            m_FilterMode = serializedObject.FindProperty("m_TextureSettings.m_FilterMode");
            m_WrapU = serializedObject.FindProperty("m_TextureSettings.m_WrapU");
            m_WrapV = serializedObject.FindProperty("m_TextureSettings.m_WrapV");
            m_WrapW = serializedObject.FindProperty("m_TextureSettings.m_WrapW");

            m_CubemapConvolution = serializedObject.FindProperty("m_CubemapConvolution");

            m_SpriteMode = serializedObject.FindProperty("m_SpriteMode");
            m_SpritePixelsToUnits = serializedObject.FindProperty("m_SpritePixelsToUnits");
            m_SpriteExtrude = serializedObject.FindProperty("m_SpriteExtrude");
            m_SpriteMeshType = serializedObject.FindProperty("m_SpriteMeshType");
            m_Alignment = serializedObject.FindProperty("m_Alignment");
            m_SpritePivot = serializedObject.FindProperty("m_SpritePivot");
            m_SpriteGenerateFallbackPhysicsShape = serializedObject.FindProperty("m_SpriteGenerateFallbackPhysicsShape");

            m_AlphaIsTransparency = serializedObject.FindProperty("m_AlphaIsTransparency");
            m_IgnorePngGamma = serializedObject.FindProperty("m_IgnorePngGamma");
            m_PSDRemoveMatte = serializedObject.FindProperty("m_PSDRemoveMatte");

            m_TextureType = serializedObject.FindProperty("m_TextureType");
            m_TextureShape = serializedObject.FindProperty("m_TextureShape");

            m_SingleChannelComponent = serializedObject.FindProperty("m_SingleChannelComponent");

            m_FlipbookRows = serializedObject.FindProperty("m_FlipbookRows");
            m_FlipbookColumns = serializedObject.FindProperty("m_FlipbookColumns");

            m_CookieLightType = serializedObject.FindProperty("m_CookieLightType");

            m_PlatformSettingsArrProp = serializedObject.FindProperty("m_PlatformSettings");

            m_TextureTypes = new List<TextureImporterType>();
            foreach (var o in targets)
            {
                var targetObject = (TextureImporter)o;
                m_TextureTypes.Add(targetObject.textureType);
            }
        }

        void InitializeGUI()
        {
            // This is where we decide what GUI elements are displayed depending on the texture type.
            // TODO: Maybe complement the bitfield with a list to add a concept of order in the display. Not sure if necessary...
            TextureImporterShape shapeCapsAll =
                TextureImporterShape.Texture2D |
                TextureImporterShape.TextureCube |
                TextureImporterShape.Texture2DArray |
                TextureImporterShape.Texture3D;

            var sharedAdvanced = TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps | TextureInspectorGUIElement.PngGamma | TextureInspectorGUIElement.Swizzle;

            m_TextureTypeGUIElements[(int)TextureImporterType.Default]      = new TextureInspectorTypeGUIProperties(
                TextureInspectorGUIElement.ColorSpace | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.CubeMapConvolution | TextureInspectorGUIElement.CubeMapping | TextureInspectorGUIElement.ElementsAtlas,
                sharedAdvanced
                | TextureInspectorGUIElement.VTOnly
                , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.NormalMap]    = new TextureInspectorTypeGUIProperties(
                TextureInspectorGUIElement.NormalMap | TextureInspectorGUIElement.CubeMapping | TextureInspectorGUIElement.ElementsAtlas,
                sharedAdvanced
                | TextureInspectorGUIElement.VTOnly
                , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.Sprite]       = new TextureInspectorTypeGUIProperties(
                TextureInspectorGUIElement.Sprite,
                TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.MipMaps | TextureInspectorGUIElement.ColorSpace,
                TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Cookie]       = new TextureInspectorTypeGUIProperties(
                TextureInspectorGUIElement.Cookie | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.CubeMapping,
                sharedAdvanced,
                TextureImporterShape.Texture2D | TextureImporterShape.TextureCube);
            m_TextureTypeGUIElements[(int)TextureImporterType.SingleChannel] = new TextureInspectorTypeGUIProperties(
                TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.SingleChannelComponent | TextureInspectorGUIElement.CubeMapping | TextureInspectorGUIElement.ElementsAtlas,
                sharedAdvanced
                , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.GUI]          = new TextureInspectorTypeGUIProperties(
                0,
                TextureInspectorGUIElement.AlphaHandling | sharedAdvanced,
                TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Cursor]       = new TextureInspectorTypeGUIProperties(
                0,
                TextureInspectorGUIElement.AlphaHandling | sharedAdvanced,
                TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Lightmap]     = new TextureInspectorTypeGUIProperties(
                0,
                sharedAdvanced
                , TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.DirectionalLightmap] = new TextureInspectorTypeGUIProperties(
                0,
                sharedAdvanced
                , TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Shadowmask]   = new TextureInspectorTypeGUIProperties(
                0,
                sharedAdvanced
                , TextureImporterShape.Texture2D);

            m_GUIElementMethods.Clear();
            m_GUIElementMethods.Add(TextureInspectorGUIElement.PowerOfTwo, this.POTScaleGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Readable, this.ReadableGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.VTOnly, this.VTOnlyGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.ColorSpace, this.ColorSpaceGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.AlphaHandling, this.AlphaHandlingGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.PngGamma, this.PngGammaGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.MipMaps, this.MipMapGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.NormalMap, this.BumpGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Sprite, this.SpriteGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Cookie, this.CookieGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.CubeMapping, this.CubemapMappingGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.ElementsAtlas, this.ElementsAtlasGui);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Swizzle, this.SwizzleGui);

            // This list dictates the order in which the GUI Elements are displayed.
            // It could be different for each TextureImporterType but let's keep it simple for now.
            m_GUIElementsDisplayOrder.Clear();
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.ElementsAtlas);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.CubeMapping);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.CubeMapConvolution);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Cookie);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.ColorSpace);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.AlphaHandling);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.SingleChannelComponent);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.NormalMap);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Sprite);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.PowerOfTwo);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Readable);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.VTOnly);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.MipMaps);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.PngGamma);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Swizzle);

            UnityEngine.Debug.Assert(m_GUIElementsDisplayOrder.Count == (Enum.GetValues(typeof(TextureInspectorGUIElement)).Length - 1), "Some GUIElement are not present in the list."); // -1 because TextureInspectorGUIElement.None
        }

        public override void OnEnable()
        {
            base.OnEnable();

            s_DefaultPlatformName = TextureImporter.defaultPlatformName; // Can't be called everywhere so we save it here for later use.

            m_ShowAdvanced = EditorPrefs.GetBool("TextureImporterShowAdvanced", m_ShowAdvanced);

            CacheSerializedProperties();

            BuildTargetList();

            m_ShowBumpGenerationSettings.valueChanged.AddListener(Repaint);
            m_ShowBumpGenerationSettings.value = m_ConvertToNormalMap.intValue > 0;
            m_ShowCubeMapSettings.valueChanged.AddListener(Repaint);
            m_ShowCubeMapSettings.value = (TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.TextureCube;
            m_ShowElementsAtlasSettings.valueChanged.AddListener(Repaint);
            m_ShowElementsAtlasSettings.value = (TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.Texture2DArray || (TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.Texture3D;
            //@TODO change to use spriteMode enum when available
            m_ShowGenericSpriteSettings.valueChanged.AddListener(Repaint);
            m_ShowGenericSpriteSettings.value = m_SpriteMode.intValue != 0;
            m_ShowSpriteMeshTypeOption.valueChanged.AddListener(Repaint);
            m_ShowSpriteMeshTypeOption.value = ShouldShowSpriteMeshTypeOption();
            m_ShowMipMapSettings.valueChanged.AddListener(Repaint);
            m_ShowMipMapSettings.value = m_EnableMipMap.boolValue && (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.Texture3D;

            InitializeGUI();

            var importer = target as TextureImporter;
            if (importer == null)
                return;

            importer.GetWidthAndHeight(ref m_TextureWidth, ref m_TextureHeight);
            m_IsPOT = IsPowerOfTwo(m_TextureWidth) && IsPowerOfTwo(m_TextureHeight);
            var ext = FileUtil.GetPathExtension(importer.assetPath).ToLowerInvariant();
            m_IsPSD = ext == "psd";
            m_IsPNG = ext == "png";
        }

        void SetSerializedPropertySettings(TextureImporterSettings settings)
        {
            m_AlphaSource.intValue = (int)settings.alphaSource;
            m_ConvertToNormalMap.intValue = settings.convertToNormalMap ? 1 : 0;
            m_HeightScale.floatValue = settings.heightmapScale;
            m_NormalMapFilter.intValue = (int)settings.normalMapFilter;
            m_FlipGreenChannel.intValue = settings.flipGreenChannel ? 1 : 0;
            m_Swizzle.uintValue = settings.swizzleRaw;
            m_GenerateCubemap.intValue = (int)settings.generateCubemap;
            m_CubemapConvolution.intValue = (int)settings.cubemapConvolution;
            m_SeamlessCubemap.intValue = settings.seamlessCubemap ? 1 : 0;
            m_BorderMipMap.intValue = settings.borderMipmap ? 1 : 0;
            m_MipMapsPreserveCoverage.intValue = settings.mipMapsPreserveCoverage ? 1 : 0;
            m_AlphaTestReferenceValue.floatValue = settings.alphaTestReferenceValue;
            m_NPOTScale.intValue = (int)settings.npotScale;
            m_IsReadable.intValue = settings.readable ? 1 : 0;
            m_StreamingMipmaps.intValue = settings.streamingMipmaps ? 1 : 0;
            m_StreamingMipmapsPriority.intValue = settings.streamingMipmapsPriority;
            m_IgnoreMipmapLimit.intValue = settings.ignoreMipmapLimit ? 1 : 0;
            m_VTOnly.intValue = settings.vtOnly ? 1 : 0;
            m_EnableMipMap.intValue = settings.mipmapEnabled ? 1 : 0;
            m_sRGBTexture.intValue = settings.sRGBTexture ? 1 : 0;
            m_MipMapMode.intValue = (int)settings.mipmapFilter;
            m_FadeOut.intValue = settings.fadeOut ? 1 : 0;
            m_MipMapFadeDistanceStart.intValue = settings.mipmapFadeDistanceStart;
            m_MipMapFadeDistanceEnd.intValue = settings.mipmapFadeDistanceEnd;

            m_SpriteMode.intValue = settings.spriteMode;
            m_SpritePixelsToUnits.floatValue = settings.spritePixelsPerUnit;
            m_SpriteExtrude.intValue = (int)settings.spriteExtrude;
            m_SpriteMeshType.intValue = (int)settings.spriteMeshType;
            m_Alignment.intValue = settings.spriteAlignment;
            m_SpriteGenerateFallbackPhysicsShape.intValue = settings.spriteGenerateFallbackPhysicsShape ? 1 : 0;

            m_WrapU.intValue = (int)settings.wrapMode;
            m_WrapV.intValue = (int)settings.wrapMode;
            m_FilterMode.intValue = (int)settings.filterMode;
            m_Aniso.intValue = settings.aniso;

            m_AlphaIsTransparency.intValue = settings.alphaIsTransparency ? 1 : 0;
            m_IgnorePngGamma.intValue = settings.ignorePngGamma ? 1 : 0;

            m_TextureType.intValue = (int)settings.textureType;
            m_TextureShape.intValue = (int)settings.textureShape;

            m_SingleChannelComponent.intValue = (int)settings.singleChannelComponent;

            m_FlipbookRows.intValue = settings.flipbookRows;
            m_FlipbookColumns.intValue = settings.flipbookColumns;
        }

        internal TextureImporterSettings GetSerializedPropertySettings()
        {
            return GetSerializedPropertySettings(new TextureImporterSettings());
        }

        internal TextureImporterSettings GetSerializedPropertySettings(TextureImporterSettings settings)
        {
            if (!m_AlphaSource.hasMultipleDifferentValues)
                settings.alphaSource = (TextureImporterAlphaSource)m_AlphaSource.intValue;

            if (!m_ConvertToNormalMap.hasMultipleDifferentValues)
                settings.convertToNormalMap = m_ConvertToNormalMap.intValue > 0;

            if (!m_HeightScale.hasMultipleDifferentValues)
                settings.heightmapScale = m_HeightScale.floatValue;

            if (!m_NormalMapFilter.hasMultipleDifferentValues)
                settings.normalMapFilter = (TextureImporterNormalFilter)m_NormalMapFilter.intValue;
            if (!m_FlipGreenChannel.hasMultipleDifferentValues)
                settings.flipGreenChannel = m_FlipGreenChannel.intValue > 0;
            if (!m_Swizzle.hasMultipleDifferentValues)
                settings.swizzleRaw = m_Swizzle.uintValue;

            if (!m_GenerateCubemap.hasMultipleDifferentValues)
                settings.generateCubemap = (TextureImporterGenerateCubemap)m_GenerateCubemap.intValue;

            if (!m_CubemapConvolution.hasMultipleDifferentValues)
                settings.cubemapConvolution = (TextureImporterCubemapConvolution)m_CubemapConvolution.intValue;

            if (!m_SeamlessCubemap.hasMultipleDifferentValues)
                settings.seamlessCubemap = m_SeamlessCubemap.intValue > 0;

            if (!m_BorderMipMap.hasMultipleDifferentValues)
                settings.borderMipmap = m_BorderMipMap.intValue > 0;

            if (!m_MipMapsPreserveCoverage.hasMultipleDifferentValues)
                settings.mipMapsPreserveCoverage = m_MipMapsPreserveCoverage.intValue > 0;

            if (!m_AlphaTestReferenceValue.hasMultipleDifferentValues)
                settings.alphaTestReferenceValue = m_AlphaTestReferenceValue.floatValue;

            if (!m_NPOTScale.hasMultipleDifferentValues)
                settings.npotScale = (TextureImporterNPOTScale)m_NPOTScale.intValue;

            if (!m_IsReadable.hasMultipleDifferentValues)
                settings.readable = m_IsReadable.intValue > 0;

            if (!m_StreamingMipmaps.hasMultipleDifferentValues)
                settings.streamingMipmaps = m_StreamingMipmaps.intValue > 0;
            if (!m_StreamingMipmapsPriority.hasMultipleDifferentValues)
                settings.streamingMipmapsPriority = m_StreamingMipmapsPriority.intValue;
            if (!m_IgnoreMipmapLimit.hasMultipleDifferentValues)
                settings.ignoreMipmapLimit = m_IgnoreMipmapLimit.intValue > 0;

            if (!m_VTOnly.hasMultipleDifferentValues)
                settings.vtOnly = m_VTOnly.intValue > 0;

            if (!m_sRGBTexture.hasMultipleDifferentValues)
                settings.sRGBTexture = m_sRGBTexture.intValue > 0;

            if (!m_EnableMipMap.hasMultipleDifferentValues)
                settings.mipmapEnabled = m_EnableMipMap.intValue > 0;

            if (!m_MipMapMode.hasMultipleDifferentValues)
                settings.mipmapFilter = (TextureImporterMipFilter)m_MipMapMode.intValue;

            if (!m_FadeOut.hasMultipleDifferentValues)
                settings.fadeOut = m_FadeOut.intValue > 0;

            if (!m_MipMapFadeDistanceStart.hasMultipleDifferentValues)
                settings.mipmapFadeDistanceStart = m_MipMapFadeDistanceStart.intValue;

            if (!m_MipMapFadeDistanceEnd.hasMultipleDifferentValues)
                settings.mipmapFadeDistanceEnd = m_MipMapFadeDistanceEnd.intValue;

            if (!m_SpriteMode.hasMultipleDifferentValues)
                settings.spriteMode = m_SpriteMode.intValue;

            if (!m_SpritePixelsToUnits.hasMultipleDifferentValues)
                settings.spritePixelsPerUnit = m_SpritePixelsToUnits.floatValue;

            if (!m_SpriteExtrude.hasMultipleDifferentValues)
                settings.spriteExtrude = (uint)m_SpriteExtrude.intValue;

            if (!m_SpriteMeshType.hasMultipleDifferentValues)
                settings.spriteMeshType = (SpriteMeshType)m_SpriteMeshType.intValue;

            if (!m_Alignment.hasMultipleDifferentValues)
                settings.spriteAlignment = m_Alignment.intValue;

            if (!m_SpritePivot.hasMultipleDifferentValues)
                settings.spritePivot = m_SpritePivot.vector2Value;

            if (!m_SpriteGenerateFallbackPhysicsShape.hasMultipleDifferentValues)
                settings.spriteGenerateFallbackPhysicsShape = m_SpriteGenerateFallbackPhysicsShape.intValue > 0;

            if (!m_WrapU.hasMultipleDifferentValues)
                settings.wrapModeU = (TextureWrapMode)m_WrapU.intValue;
            if (!m_WrapV.hasMultipleDifferentValues)
                settings.wrapModeU = (TextureWrapMode)m_WrapV.intValue;
            if (!m_WrapW.hasMultipleDifferentValues)
                settings.wrapModeU = (TextureWrapMode)m_WrapW.intValue;

            if (!m_FilterMode.hasMultipleDifferentValues)
                settings.filterMode = (FilterMode)m_FilterMode.intValue;

            if (!m_Aniso.hasMultipleDifferentValues)
                settings.aniso = m_Aniso.intValue;


            if (!m_AlphaIsTransparency.hasMultipleDifferentValues)
                settings.alphaIsTransparency = m_AlphaIsTransparency.intValue > 0;

            if (!m_TextureType.hasMultipleDifferentValues)
                settings.textureType = (TextureImporterType)m_TextureType.intValue;

            if (!m_TextureShape.hasMultipleDifferentValues)
                settings.textureShape = (TextureImporterShape)m_TextureShape.intValue;

            if (!m_SingleChannelComponent.hasMultipleDifferentValues)
                settings.singleChannelComponent = (TextureImporterSingleChannelComponent)m_SingleChannelComponent.intValue;

            if (!m_IgnorePngGamma.hasMultipleDifferentValues)
                settings.ignorePngGamma = m_IgnorePngGamma.intValue > 0;

            if (!m_FlipbookRows.hasMultipleDifferentValues)
                settings.flipbookRows = m_FlipbookRows.intValue;
            if (!m_FlipbookColumns.hasMultipleDifferentValues)
                settings.flipbookColumns = m_FlipbookColumns.intValue;

            return settings;
        }

        void CookieGUI(TextureInspectorGUIElement guiElements)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Popup(m_CookieLightType, s_Styles.cookieOptions, s_Styles.cookieType);

            if (EditorGUI.EndChangeCheck())
                SetCookieLightTypeDefaults((TextureImporterCookieLightType)m_CookieLightType.intValue);
        }

        void CubemapMappingGUI(TextureInspectorGUIElement guiElements)
        {
            m_ShowCubeMapSettings.target = (TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.TextureCube;
            if (EditorGUILayout.BeginFadeGroup(m_ShowCubeMapSettings.faded))
            {
                if ((TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.TextureCube)
                {
                    using (new EditorGUI.DisabledScope(!m_IsPOT && m_NPOTScale.intValue == (int)TextureImporterNPOTScale.None))
                    {
                        Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                        GUIContent label = EditorGUI.BeginProperty(controlRect, s_Styles.cubemap, m_GenerateCubemap);

                        EditorGUI.showMixedValue = m_GenerateCubemap.hasMultipleDifferentValues || m_SeamlessCubemap.hasMultipleDifferentValues;

                        EditorGUI.BeginChangeCheck();

                        int value = EditorGUI.IntPopup(controlRect, label, m_GenerateCubemap.intValue, s_Styles.cubemapOptions, s_Styles.cubemapValues2);
                        if (EditorGUI.EndChangeCheck())
                            m_GenerateCubemap.intValue = value;

                        EditorGUI.EndProperty();
                        EditorGUI.indentLevel++;

                        // Convolution
                        if (ShouldDisplayGUIElement(guiElements, TextureInspectorGUIElement.CubeMapConvolution))
                        {
                            EditorGUILayout.IntPopup(m_CubemapConvolution,
                                s_Styles.cubemapConvolutionOptions,
                                s_Styles.cubemapConvolutionValues,
                                s_Styles.cubemapConvolution);
                        }

                        ToggleFromInt(m_SeamlessCubemap, s_Styles.seamlessCubemap);

                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        void ElementsAtlasGui(TextureInspectorGUIElement guiElements)
        {
            var shape = (TextureImporterShape)m_TextureShape.intValue;
            var isLayerShape = shape == TextureImporterShape.Texture2DArray || shape == TextureImporterShape.Texture3D;
            m_ShowElementsAtlasSettings.target = isLayerShape;
            if (EditorGUILayout.BeginFadeGroup(m_ShowElementsAtlasSettings.faded) && isLayerShape)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_FlipbookColumns, s_Styles.flipbookColumns);
                if (EditorGUI.EndChangeCheck())
                {
                    var val = m_FlipbookColumns.intValue;
                    val = Mathf.Clamp(val, 1, m_TextureWidth);
                    m_FlipbookColumns.intValue = val;
                }
                if (m_TextureWidth % m_FlipbookColumns.intValue != 0)
                    EditorGUILayout.HelpBox($"Image width {m_TextureWidth} does not divide into {m_FlipbookColumns.intValue} columns exactly", MessageType.Warning, true);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_FlipbookRows, s_Styles.flipbookRows);
                if (EditorGUI.EndChangeCheck())
                {
                    var val = m_FlipbookRows.intValue;
                    val = Mathf.Clamp(val, 1, m_TextureHeight);
                    m_FlipbookRows.intValue = val;
                }
                if (m_TextureHeight % m_FlipbookRows.intValue != 0)
                    EditorGUILayout.HelpBox($"Image height {m_TextureHeight} does not divide into {m_FlipbookRows.intValue} rows exactly", MessageType.Warning, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }

        void ColorSpaceGUI(TextureInspectorGUIElement guiElements)
        {
            if (CountImportersWithHDR(targets, out int countHDR) && countHDR == 0)
            {
                ToggleFromInt(m_sRGBTexture, s_Styles.sRGBTexture);
            }
        }

        void POTScaleGUI(TextureInspectorGUIElement guiElements)
        {
            using (new EditorGUI.DisabledScope(m_IsPOT))
            {
                EnumPopup(m_NPOTScale, typeof(TextureImporterNPOTScale), s_Styles.npot);
            }
        }

        void ReadableGUI(TextureInspectorGUIElement guiElements)
        {
            bool enabled = CanReadWrite();
            using (new EditorGUI.DisabledScope(!enabled))
            {
                ToggleFromInt(m_IsReadable, s_Styles.readWrite);
                if (!enabled && m_IsReadable.intValue > 0)
                {
                    EditorGUILayout.HelpBox(s_Styles.readWriteWarning.text, MessageType.Warning, true);
                }
            }
        }

        void StreamingMipmapsGUI()
        {
            // only 2D & Cubemap shapes support streaming mipmaps right now
            var shape = (TextureImporterShape)m_TextureShape.intValue;
            var shapeHasStreaming = shape == TextureImporterShape.Texture2D || shape == TextureImporterShape.TextureCube;
            if (!shapeHasStreaming)
                return;

            // some texture types are not relevant for mip streaming
            var type = textureType;
            if (type == TextureImporterType.Cookie || type == TextureImporterType.GUI || type == TextureImporterType.Cursor)
                return;

            ToggleFromInt(m_StreamingMipmaps, s_Styles.streamingMipmaps);
            if (m_StreamingMipmaps.boolValue && !m_StreamingMipmaps.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_StreamingMipmapsPriority, s_Styles.streamingMipmapsPriority);
                EditorGUI.indentLevel--;
            }
        }

        void VTOnlyGUI(TextureInspectorGUIElement guiElements)
        {
            // only 2D shape supports VT right now
            var shape = (TextureImporterShape)m_TextureShape.intValue;
            var shapeHasVT = shape == TextureImporterShape.Texture2D;
            if (!shapeHasVT)
                return;
            ToggleFromInt(m_VTOnly, s_Styles.vtOnly);
        }


        void AlphaHandlingGUI(TextureInspectorGUIElement guiElements)
        {
            bool showAlphaSource = true;
            if (ShouldDisplayGUIElement(guiElements, TextureInspectorGUIElement.SingleChannelComponent))
            {
                EditorGUILayout.IntPopup(m_SingleChannelComponent, s_Styles.singleChannelComponentOptions, s_Styles.singleChannelComponentValues, s_Styles.singleChannelComponent);

                showAlphaSource = (m_SingleChannelComponent.intValue == (int)TextureImporterSingleChannelComponent.Alpha);
            }

            if (showAlphaSource)
            {
                int countWithAlpha = 0;
                int countHDR = 0;

                bool success = CountImportersWithAlpha(targets, out countWithAlpha);
                success = success && CountImportersWithHDR(targets, out countHDR);

                EditorGUILayout.IntPopup(m_AlphaSource, s_Styles.alphaSourceOptions, s_Styles.alphaSourceValues, s_Styles.alphaSource);

                bool showAlphaIsTransparency = success && (TextureImporterAlphaSource)m_AlphaSource.intValue != TextureImporterAlphaSource.None && countHDR == 0; // AlphaIsTransparency is not properly implemented for HDR texture yet.
                using (new EditorGUI.DisabledScope(assetTarget != null && !showAlphaIsTransparency))
                {
                    ToggleFromInt(m_AlphaIsTransparency, s_Styles.alphaIsTransparency);
                }
            }

            // This is pure backward compatibility codepath. It can be removed when we decide that the time has come
            if (m_IsPSD)
            {
                EditorGUILayout.PropertyField(m_PSDRemoveMatte, s_Styles.psdRemoveMatte);
            }
        }

        private bool ShouldShowSpriteMeshTypeOption()
        {
            return m_SpriteMode.intValue != (int)SpriteImportMode.Polygon && !m_SpriteMode.hasMultipleDifferentValues;
        }

        private void SpriteGUI(TextureInspectorGUIElement guiElements)
        {
            // Sprite mode selection
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.IntPopup(m_SpriteMode, s_Styles.spriteModeOptions, new[] { 1, 2, 3 }, s_Styles.spriteMode);

            // Ensure that PropertyField focus will be cleared when we change spriteMode.
            if (EditorGUI.EndChangeCheck())
            {
                GUIUtility.keyboardControl = 0;
            }

            EditorGUI.indentLevel++;

            // Show generic attributes
            m_ShowGenericSpriteSettings.target = (m_SpriteMode.intValue != 0);
            if (EditorGUILayout.BeginFadeGroup(m_ShowGenericSpriteSettings.faded))
            {
                EditorGUILayout.PropertyField(m_SpritePixelsToUnits, s_Styles.spritePixelsPerUnit);

                m_ShowSpriteMeshTypeOption.target = ShouldShowSpriteMeshTypeOption();
                if (EditorGUILayout.BeginFadeGroup(m_ShowSpriteMeshTypeOption.faded))
                {
                    EditorGUILayout.IntPopup(m_SpriteMeshType, s_Styles.spriteMeshTypeOptions, new[] { 0, 1 }, s_Styles.spriteMeshType);
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUILayout.IntSlider(m_SpriteExtrude, 0, 32, s_Styles.spriteExtrude);

                if (m_SpriteMode.intValue == (int)SpriteImportMode.Single)
                {
                    EditorGUILayout.Popup(m_Alignment, s_Styles.spriteAlignmentOptions, s_Styles.spriteAlignment);

                    if (m_Alignment.intValue == (int)SpriteAlignment.Custom)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(m_SpritePivot, m_EmptyContent);
                        GUILayout.EndHorizontal();
                    }
                }

                if (m_SpriteMode.intValue != (int)SpriteImportMode.Polygon)
                    ToggleFromInt(m_SpriteGenerateFallbackPhysicsShape, s_Styles.spriteGenerateFallbackPhysicsShape);

                using (new EditorGUI.DisabledScope(targets.Length != 1))
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sprite Editor"))
                    {
                        if (HasModified())
                        {
                            // To ensure Sprite Editor Window to have the latest texture import setting,
                            // We must applied those modified values first.
                            string dialogText = "Unapplied import settings for \'" + ((TextureImporter)target).assetPath + "\'.\n";
                            dialogText += "Apply and continue to sprite editor or cancel.";
                            if (EditorUtility.DisplayDialog("Unapplied import settings", dialogText, "Apply", "Cancel"))
                            {
                                SaveChanges();
                                SpriteUtilityWindow.ShowSpriteEditorWindow(this.assetTarget);

                                // We reimported the asset which destroyed the editor, so we can't keep running the UI here.
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            SpriteUtilityWindow.ShowSpriteEditorWindow(this.assetTarget);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel--;
        }

        internal static void DoMipmapLimitsGUI(SerializedProperty ignoreMipmapLimitProp, SerializedProperty groupNameProp)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            // The property itself is "ignoreMipmapLimit". However, in the UI, we display it
            // as "Use Mipmap Limits" as it makes more sense to hide the group names dropdown
            // when "Use Mipmap Limits" is toggled off rather than when "Ignore Mipmap Limit"
            // is toggled on.
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
            GUIContent label = EditorGUI.BeginProperty(rect, s_Styles.useMipmapLimits, ignoreMipmapLimitProp);
            bool useMipmapLimits = ignoreMipmapLimitProp.propertyType == SerializedPropertyType.Integer ? ignoreMipmapLimitProp.intValue == 0 : !ignoreMipmapLimitProp.boolValue;
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                useMipmapLimits = EditorGUI.Toggle(rect, label, useMipmapLimits);
                if (changed.changed)
                {
                    if (ignoreMipmapLimitProp.propertyType == SerializedPropertyType.Integer)
                    {
                        ignoreMipmapLimitProp.intValue = useMipmapLimits ? 0 : 1;
                    }
                    else
                    {
                        ignoreMipmapLimitProp.boolValue = !useMipmapLimits;
                    }
                }
            }
            EditorGUI.EndProperty();

            if (useMipmapLimits)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    List<string> options = new List<string>();
                    options.Add(L10n.Tr("None (Use Global Mipmap Limit)"));

                    // Add all known groups
                    var groupNames = TextureMipmapLimitGroups.GetGroups();
                    if (groupNames.Length > 0)
                    {
                        options.Add(string.Empty); // Separator
                        options.AddRange(groupNames);
                    }

                    // If a group is not known, make sure to add it to the options anyway
                    if (groupNameProp.stringValue != string.Empty && !options.Contains(groupNameProp.stringValue))
                    {
                        options.Add(string.Empty); // Seperator
                        options.Add(groupNameProp.stringValue);
                    }

                    rect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
                    label = EditorGUI.BeginProperty(rect, s_Styles.mipmapLimitGroupName, groupNameProp);
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        int selectedIndex = groupNameProp.hasMultipleDifferentValues ? -1 : ((groupNameProp.stringValue == string.Empty) ? 0 : options.IndexOf(groupNameProp.stringValue));
                        selectedIndex = EditorGUI.Popup(rect, label, selectedIndex, options.ToArray());
                        if (changed.changed)
                        {
                            groupNameProp.stringValue = selectedIndex == 0 ? string.Empty : options[selectedIndex];
                        }
                    }
                    EditorGUI.EndProperty();

                    bool displayGroupNameWarning = groupNameProp.stringValue.Length > 0 && !TextureMipmapLimitGroups.HasGroup(groupNameProp.stringValue);
                    if (displayGroupNameWarning)
                    {
                        EditorGUILayout.HelpBox(s_Styles.mipmapLimitGroupWarning.text, MessageType.Warning, true);
                    }
                }
            }
        }

        void MipMapGUI(TextureInspectorGUIElement guiElements)
        {
            ToggleFromInt(m_EnableMipMap, s_Styles.generateMipMaps);

            m_ShowMipMapSettings.target =
                m_EnableMipMap.boolValue &&
                !m_EnableMipMap.hasMultipleDifferentValues &&
                // 3D textures don't use any of mipmap settings, just the "yes/no" flag
                (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.Texture3D;

            if (EditorGUILayout.BeginFadeGroup(m_ShowMipMapSettings.faded))
            {
                EditorGUI.indentLevel++;

                // If VTOnly, then we don't show the MipmapLimits GUI since its values are ignored anyway
                if ((TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.Texture2D && !m_VTOnly.boolValue)
                {
                    DoMipmapLimitsGUI(m_IgnoreMipmapLimit, m_MipmapLimitGroupName);
                }

                StreamingMipmapsGUI();

                EditorGUILayout.Popup(m_MipMapMode, s_Styles.mipMapFilterOptions, s_Styles.mipMapFilter);

                ToggleFromInt(m_MipMapsPreserveCoverage, s_Styles.mipMapsPreserveCoverage);
                if (m_MipMapsPreserveCoverage.intValue != 0 && !m_MipMapsPreserveCoverage.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AlphaTestReferenceValue, s_Styles.alphaTestReferenceValue);
                    EditorGUI.indentLevel--;
                }

                ToggleFromInt(m_BorderMipMap, s_Styles.borderMipMaps);

                // Mipmap fadeout
                ToggleFromInt(m_FadeOut, s_Styles.mipmapFadeOutToggle);
                if (m_FadeOut.intValue > 0)
                {
                    const int minLimit = 0;
                    const int maxLimit = 10;

                    // For presets, we need two separate controls as we have 2 separate SerializedProperties.
                    EditorGUI.indentLevel++;
                    if (Presets.Preset.IsEditorTargetAPreset(target))
                    {
                        // Fade Start
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_MipMapFadeDistanceStart, s_Styles.mipmapFadeStartMip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MipMapFadeDistanceStart.intValue = Math.Clamp(m_MipMapFadeDistanceStart.intValue, minLimit, m_MipMapFadeDistanceEnd.intValue);
                        }

                        // Fade End
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(m_MipMapFadeDistanceEnd, s_Styles.mipmapFadeEndMip);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MipMapFadeDistanceEnd.intValue = Math.Clamp(m_MipMapFadeDistanceEnd.intValue, m_MipMapFadeDistanceStart.intValue, maxLimit);
                        }
                    }
                    else
                    {
                        // Fade Range
                        EditorGUI.BeginChangeCheck();
                        float min = m_MipMapFadeDistanceStart.intValue;
                        float max = m_MipMapFadeDistanceEnd.intValue;
                        EditorGUILayout.MinMaxSlider(s_Styles.mipmapFadeOut, ref min, ref max, minLimit, maxLimit);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MipMapFadeDistanceStart.intValue = Mathf.RoundToInt(min);
                            m_MipMapFadeDistanceEnd.intValue = Mathf.RoundToInt(max);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }

        void PngGammaGUI(TextureInspectorGUIElement guiElements)
        {
            if (m_IsPNG)
                ToggleFromInt(m_IgnorePngGamma, s_Styles.ignorePngGamma);
        }

        void BumpGUI(TextureInspectorGUIElement guiElements)
        {
            EditorGUI.BeginChangeCheck();

            ToggleFromInt(m_ConvertToNormalMap, s_Styles.generateFromBump);
            m_ShowBumpGenerationSettings.target = m_ConvertToNormalMap.intValue > 0;
            if (EditorGUILayout.BeginFadeGroup(m_ShowBumpGenerationSettings.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(m_HeightScale, 0.0F, 0.3F, s_Styles.bumpiness);
                EditorGUILayout.Popup(m_NormalMapFilter, s_Styles.bumpFilteringOptions, s_Styles.bumpFiltering);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            ToggleFromInt(m_FlipGreenChannel, s_Styles.flipGreenChannel);

            if (EditorGUI.EndChangeCheck())
                BaseTextureImportPlatformSettings.SyncPlatformSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings));
        }

        // A label, and then four dropdown popups to pick RGBA swizzle sources.
        // Code flow modeled similar to a Vector4Field.
        static readonly int s_SwizzleFieldHash = "SwizzleField".GetHashCode();
        static readonly string[] s_SwizzleOptions = new[] {"R","G","B","A", "1-R","1-G","1-B","1-A", "0","1" };
        static uint SwizzleField(GUIContent label, uint swizzle)
        {
            var rect = EditorGUILayout.s_LastRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector4, label), EditorStyles.numberField);
            var id = GUIUtility.GetControlID(s_SwizzleFieldHash, FocusType.Keyboard, rect);
            rect = EditorGUI.MultiFieldPrefixLabel(rect, id, label, 4);
            rect.height = EditorGUI.kSingleLineHeight;

            float w = (rect.width - 3 * EditorGUI.kSpacingSubLabel) / 4;
            var subRect = new Rect(rect) {width = w};
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < 4; i++)
            {
                int shift = 8 * i;
                uint swz = (swizzle >> shift) & 0xFF;
                swz = (uint)EditorGUI.Popup(subRect, (int)swz, s_SwizzleOptions);
                swizzle &= ~(0xFFu << shift);
                swizzle |= swz << shift;
                subRect.x += w + EditorGUI.kSpacingSubLabel;
            }
            EditorGUI.indentLevel = oldIndent;
            return swizzle;
        }

        static void SwizzleField(SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), label, property);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var value = SwizzleField(label, property.uintValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.uintValue = value;
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }

        void SwizzleGui(TextureInspectorGUIElement guiElements)
        {
            SwizzleField(m_Swizzle, s_Styles.swizzle);
        }

        bool m_ShowPerAxisWrapModes = false;

        private bool TargetsHaveNPOTTextures()
        {
            foreach (var target in targets)
            {
                int w = -1, h = -1;
                var imp = (TextureImporter)target;
                imp.GetWidthAndHeight(ref w, ref h);
                if (!Mathf.IsPowerOfTwo(w) || !Mathf.IsPowerOfTwo(h))
                {
                    return true;
                }
            }

            return false;
        }

        void TextureSettingsGUI()
        {
            // Wrap mode
            // NOTE: once we get ability to have 3D/Volume texture shapes, should pass true for isVolume based on m_TextureShape
            bool isVolume = false;
            TextureInspector.WrapModePopup(m_WrapU, m_WrapV, m_WrapW, isVolume, ref m_ShowPerAxisWrapModes, assetTarget == null);

            // Display warning about repeat wrap mode on restricted npot emulation
            if (m_NPOTScale.intValue == (int)TextureImporterNPOTScale.None &&
                (m_WrapU.intValue == (int)TextureWrapMode.Repeat || m_WrapV.intValue == (int)TextureWrapMode.Repeat) &&
                !ShaderUtil.hardwareSupportsFullNPOT)
            {
                bool displayWarning = TargetsHaveNPOTTextures();
                if (displayWarning)
                {
                    GUIContent c = EditorGUIUtility.TrTextContent("Graphics device doesn't support Repeat wrap mode on NPOT textures. Falling back to Clamp.");
                    EditorGUILayout.HelpBox(c.text, MessageType.Warning, true);
                }
            }

            // Filter mode
            EditorGUILayout.IntPopup(m_FilterMode, s_Styles.filterModeOptions, m_FilterModeOptions, s_Styles.filterMode);

            // Aniso
            bool showAniso = (FilterMode)m_FilterMode.intValue != FilterMode.Point
                && m_EnableMipMap.intValue > 0
                && (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.TextureCube
                && (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.Texture3D;
            using (new EditorGUI.DisabledScope(!showAniso))
            {
                EditorGUILayout.IntSlider(m_Aniso, 0, 16, "Aniso Level");

                TextureInspector.DoAnisoGlobalSettingNote(m_Aniso.intValue);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (s_Styles == null)
                s_Styles = new Styles();

            bool wasEnabled = GUI.enabled;

            EditorGUILayout.Space();

            // Texture Usage
            int oldTextureType = m_TextureType.intValue;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntPopup(m_TextureType, s_Styles.textureTypeOptions, s_Styles.textureTypeValues, s_Styles.textureTypeTitle);
            // (case 857001) EndChangeCheck will return true even if the same value is selected.
            // Consequently the sprite will be reset to Single mode and looks very confusing to the user.
            int newTextureType = m_TextureType.intValue;
            if (EditorGUI.EndChangeCheck() && (oldTextureType != newTextureType))
            {
                // please note that in GetSerializedPropertySettings() we will init TextureImporterSettings from current state
                //   and at this point m_TextureType still has *old* value
                // meaning that we still need to change TextureImporterSettings textureType manually
                // NB we do these weird things partly because ApplyTextureType has early out
                // NB hence we want settings to have *old* textureType when calling it
                TextureImporterSettings settings = GetSerializedPropertySettings();
                settings.ApplyTextureType((TextureImporterType)newTextureType);
                settings.textureType = (TextureImporterType)newTextureType;

                SetSerializedPropertySettings(settings);

                BaseTextureImportPlatformSettings.SyncPlatformSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings));
            }

            // Texture Shape
            int[] shapeArray = s_Styles.textureShapeValuesDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps];
            using (new EditorGUI.DisabledScope(shapeArray.Length == 1 || m_TextureType.intValue == (int)TextureImporterType.Cookie)) // Cookie is a special case because the cookie type drives the shape of the texture
            {
                EditorGUILayout.IntPopup(m_TextureShape, s_Styles.textureShapeOptionsDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps], s_Styles.textureShapeValuesDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps], s_Styles.textureShape);
            }

            // Switching usage can lead to a subset of the current available shapes.
            if (Array.IndexOf(shapeArray, m_TextureShape.intValue) == -1)
            {
                m_TextureShape.intValue = shapeArray[0];
            }

            EditorGUILayout.Space();

            // Show advanced settings for texture types that have the same subset of advanced settings (rather than just those that are exactly the same type)
            bool showAdvanced = false;
            if (m_TextureType.hasMultipleDifferentValues)
            {
                showAdvanced = true;
                int iteratedTextureType = (int)m_TextureTypes[0];
                TextureInspectorGUIElement firstAdvancedElements = m_TextureTypeGUIElements[iteratedTextureType].advancedElements;
                for (int selectionIndex = 1; selectionIndex < m_TextureTypes.Count; selectionIndex++)
                {
                    iteratedTextureType = (int)m_TextureTypes[selectionIndex];
                    if (firstAdvancedElements != m_TextureTypeGUIElements[iteratedTextureType].advancedElements)
                    {
                        showAdvanced = false;
                        break;
                    }
                }
            }
            else
            {
                showAdvanced = true;
            }

            if (showAdvanced)
            {
                DoGUIElements(m_TextureTypeGUIElements[newTextureType].commonElements, m_GUIElementsDisplayOrder);
                if (m_TextureTypeGUIElements[newTextureType].advancedElements != 0)
                {
                    EditorGUILayout.Space();

                    m_ShowAdvanced = EditorGUILayout.Foldout(m_ShowAdvanced, s_Styles.showAdvanced, true);
                    if (m_ShowAdvanced)
                    {
                        EditorGUI.indentLevel++;
                        DoGUIElements(m_TextureTypeGUIElements[newTextureType].advancedElements, m_GUIElementsDisplayOrder);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.Space();

            // Filter mode, aniso, and wrap mode GUI
            TextureSettingsGUI();

            BaseTextureImportPlatformSettings.InitPlatformSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings));
            m_PlatformSettings.ForEach(settings => settings.CacheSerializedProperties(m_PlatformSettingsArrProp));
            GUILayout.Space(10);

            //Show platform grouping
            int selectedPage = EditorGUILayout.BeginPlatformGrouping(BaseTextureImportPlatformSettings.GetBuildPlayerValidPlatforms(), s_Styles.defaultPlatform, EditorStyles.frameBox, idx =>
            {
                var ps = m_PlatformSettings[idx + 1];
                var model = ps.model;
                if (model.isDefault)
                    return false;
                if (model.overriddenIsDifferent || model.allAreOverridden)
                    return true;
                return false;
            });


            //Show platform settings
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                BaseTextureImportPlatformSettings.ShowPlatformSpecificSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings), selectedPage);
                // Doing it this way is slow, but it ensure Presets get updated correctly whenever the UI is being changed.
                if (changed.changed)
                {
                    Undo.RegisterCompleteObjectUndo(targets, "Inspector");
                    BaseTextureImportPlatformSettings.ApplyPlatformSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings));
                }
            }

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();

            // screw this - after lots of retries i have no idea how to poll it only when we change related stuff
            UpdateImportWarning();
            if (!string.IsNullOrEmpty(m_ImportWarning))
                EditorGUILayout.HelpBox(m_ImportWarning, MessageType.Warning);

            if (!m_TextureShape.hasMultipleDifferentValues && !m_EnableMipMap.hasMultipleDifferentValues && !m_NPOTScale.hasMultipleDifferentValues)
            {
                // To avoid complexity of combinations of settings (e.g., tex1 is rescaled to POT with mipmap enabled, tex2 is NPOT with mipmap disabled, then all is fine but we would still get warnings)
                // we only show the warnings for a single texture (or a group with the same values)

                if (m_TextureShape.intValue == (int)TextureImporterShape.Texture2D && m_EnableMipMap.boolValue && m_NPOTScale.intValue == (int)TextureImporterNPOTScale.None && TargetsHaveNPOTTextures())
                {
                    BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                    UnityEngine.Rendering.GraphicsDeviceType[] activeTargetGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(buildTarget);
                    if (Array.Exists(activeTargetGraphicsAPIs, api => api == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2))
                    {
                        GUIContent c;
                        if (buildTarget == BuildTarget.WebGL)
                            c = EditorGUIUtility.TrTextContent("NPOT textures are not fully supported on WebGL1. On these devices, mipmapping will be disabled.");
                        else if (buildTarget == BuildTarget.Android)
                            c = EditorGUIUtility.TrTextContent("Some Android devices running on OpenGLES2 may not support NPOT textures. If this is the case, mipmapping will be disabled.");
                        else
                            c = EditorGUIUtility.TrTextContent("Some devices running on OpenGLES2 may not support NPOT textures. If this is the case, mipmapping will be disabled.");

                        EditorGUILayout.HelpBox(c.text, MessageType.Warning, true);
                    }
                }
            }


            GUI.enabled = wasEnabled;
        }

        bool ShouldDisplayGUIElement(TextureInspectorGUIElement guiElements, TextureInspectorGUIElement guiElement)
        {
            return ((guiElements & guiElement) == guiElement);
        }

        private void DoGUIElements(TextureInspectorGUIElement guiElements, List<TextureInspectorGUIElement> guiElementsDisplayOrder)
        {
            foreach (TextureInspectorGUIElement guiElement in guiElementsDisplayOrder)
            {
                if (ShouldDisplayGUIElement(guiElements, guiElement) && m_GUIElementMethods.ContainsKey(guiElement))
                {
                    m_GUIElementMethods[guiElement](guiElements);
                }
            }
        }

        // Returns false if method fails to get info
        static bool CountImportersWithAlpha(Object[] importers, out int count)
        {
            // DoesSourceTextureHaveAlpha will throw exception on importer reset (593478).
            try
            {
                count = 0;
                foreach (Object t in importers)
                    if ((t as TextureImporter).DoesSourceTextureHaveAlpha())
                        count++;
                return true;
            }
            catch
            {
                count = importers.Length;
                return false;
            }
        }

        static bool CountImportersWithHDR(Object[] importers, out int count)
        {
            // DoesSourceTextureHaveAlpha will throw exception on importer reset (593478).
            try
            {
                count = 0;
                foreach (Object t in importers)
                    if ((t as TextureImporter).IsSourceTextureHDR())
                        count++;
                return true;
            }
            catch
            {
                count = importers.Length;
                return false;
            }
        }

        void SetCookieLightTypeDefaults(TextureImporterCookieLightType cookieLightType)
        {
            // Note that, out of all of these, only the TextureShape is truly strongly enforced.
            // The other settings are nothing more than recommended defaults and can be modified
            // by the user at any time.
            switch (cookieLightType)
            {
                case TextureImporterCookieLightType.Spot:
                    m_BorderMipMap.intValue = 1;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Clamp;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.AutoCubemap;
                    m_TextureShape.intValue = (int)TextureImporterShape.Texture2D;
                    break;
                case TextureImporterCookieLightType.Point:
                    m_BorderMipMap.intValue = 0;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Clamp;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.Spheremap;
                    m_TextureShape.intValue = (int)TextureImporterShape.TextureCube;
                    break;
                case TextureImporterCookieLightType.Directional:
                    m_BorderMipMap.intValue = 0;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Repeat;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.AutoCubemap;
                    m_TextureShape.intValue = (int)TextureImporterShape.Texture2D;
                    break;
            }
        }

        internal static string[] BuildTextureStrings(int[] texFormatValues)
        {
            string[] retval = new string[texFormatValues.Length];
            for (int i = 0; i < texFormatValues.Length; i++)
            {
                int val = texFormatValues[i];
                retval[i] = " " + (val < 0 ? "Auto" : GraphicsFormatUtility.GetFormatString((TextureFormat)val));
            }
            return retval;
        }

        internal static bool IsFormatRequireCompressionSetting(TextureImporterFormat format)
        {
            return ArrayUtility.Contains<TextureImporterFormat>(TextureImporterInspector.kFormatsWithCompressionSettings, format);
        }

        private static bool IsPowerOfTwo(int f)
        {
            return ((f & (f - 1)) == 0);
        }

        bool CanReadWrite()
        {
            foreach (TextureImportPlatformSettings ps in m_PlatformSettings)
            {
                if (ps.model.platformTextureSettings.maxTextureSize > TextureImporter.MaxTextureSizeAllowedForReadable)
                    return false;
            }
            return true;
        }

        public virtual void BuildTargetList()
        {
            BuildPlatform[] validPlatforms = BaseTextureImportPlatformSettings.GetBuildPlayerValidPlatforms();

            m_PlatformSettings = new List<TextureImportPlatformSettings>();
            m_PlatformSettings.Add(new TextureImportPlatformSettings(s_DefaultPlatformName, BuildTarget.StandaloneWindows, this));

            foreach (BuildPlatform bp in validPlatforms)
                m_PlatformSettings.Add(new TextureImportPlatformSettings(bp.name, bp.defaultTarget, this));
        }

        public override bool HasModified()
        {
            if (base.HasModified())
                return true;

            foreach (TextureImportPlatformSettings ps in m_PlatformSettings)
            {
                if (ps.model.HasChanged())
                    return true;
            }

            return false;
        }


        [Obsolete("UnityUpgradeable () -> DiscardChanges")]
        protected override void ResetValues()
        {
            DiscardChanges();
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();

            CacheSerializedProperties();

            BuildTargetList();
            System.Diagnostics.Debug.Assert(!HasModified(), "TextureImporter settings are marked as modified after calling Reset.");
        }

        protected override void Apply()
        {
            SpriteUtilityWindow.ApplySpriteEditorWindow();
            base.Apply();
            RefreshPreviewChannelSelection();
            BaseTextureImportPlatformSettings.ApplyPlatformSettings(m_PlatformSettings.ConvertAll<BaseTextureImportPlatformSettings>(x => x as BaseTextureImportPlatformSettings));
        }

        public override void DrawPreview(Rect previewArea)
        {
            base.DrawPreview(previewArea);

            //Drawing texture previewers for VT only textures will have generated texture tile request.
            //We need to update the VT system to actually stream these tiles into VRAM and render the textures correctly.
            if (textureInspector != null && textureInspector.hasTargetUsingVTMaterial)
                VirtualTexturing.System.Update();
        }

        private void RefreshPreviewChannelSelection()
        {
            //If the Preview is null or NOT the TextureInspector (e.g. ObjectPreview) then return, we do not need to refresh the preview channel selection
            if (!(preview is TextureInspector))
                return;

            string platformName = BuildPipeline.GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
            for (int i = 0; i < targets.Length; i++)
            {
                //Is preview currently Alpha-only format? Skip if not.
                bool wasAlphaOnlyTextureFormat = textureInspector.targets[i] is Texture2D texture && GraphicsFormatUtility.IsAlphaOnlyFormat(texture.format);
                if (!wasAlphaOnlyTextureFormat)
                    continue;

                //Get the Texture Importer
                TextureImporter t = (TextureImporter)targets[i];

                //Are we about to set the preview to an Alpha-only format?
                TextureImporterFormat textureImporterFormat = t.GetPlatformTextureSettings(platformName).format;
                TextureFormat textureFormat = textureImporterFormat == TextureImporterFormat.Automatic ? (TextureFormat)t.GetAutomaticFormat(platformName) : (TextureFormat)textureImporterFormat;

                //Where the preview is no-longer alpha only, reset all previews to RGB - Note: It is not possible to mix channel selection in TextureInspector.
                if (!GraphicsFormatUtility.IsAlphaOnlyFormat(textureFormat))
                {
                    textureInspector.m_PreviewMode = TextureInspector.PreviewMode.RGB;
                    break;
                }
            }
        }
    }
}
