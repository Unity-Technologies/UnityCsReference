// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditor.Modules;
using UnityEngine;
using UnityEditor.Build;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Experimental.AssetImporters;
using Object = UnityEngine.Object;

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
    };

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

        public new void OnDisable()
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

        internal static bool IsGLESMobileTargetPlatform(BuildTarget target)
        {
            return target == BuildTarget.iOS || target == BuildTarget.tvOS || target == BuildTarget.Android || target == BuildTarget.Tizen;
        }

        // Which platforms should we display?
        // For each of these, what are the formats etc. to display?
        [SerializeField]
        internal List<TextureImportPlatformSettings> m_PlatformSettings;


        internal static int[] s_TextureFormatsValueAll;

        internal static int[] TextureFormatsValueAll
        {
            get
            {
                if (s_TextureFormatsValueAll != null)
                    return s_TextureFormatsValueAll;

                bool requireETC = false;
                bool requirePVRTC = false;
                bool requireATC = false;
                bool requireETC2 = false;
                bool requireASTC = false;

                // Build available formats based on available platforms
                BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms();
                foreach (BuildPlatform platform in validPlatforms)
                {
                    switch (platform.defaultTarget)
                    {
                        case BuildTarget.Android:
                            requirePVRTC = true;
                            requireETC = true;
                            requireATC = true;
                            requireETC2 = true;
                            requireASTC = true;
                            break;
                        case BuildTarget.iOS:
                            requirePVRTC = true;
                            break;
                        case BuildTarget.tvOS:
                            requirePVRTC = true;
                            requireASTC = true;
                            break;
                        case BuildTarget.Tizen:
                            requireETC = true;
                            break;
                    }
                }
                List<int> formatValues = new List<int>();

                formatValues.AddRange(new[] {
                    (int)TextureImporterFormat.DXT1,
                    (int)TextureImporterFormat.DXT5,
                });

                if (requireETC)
                    formatValues.Add((int)TextureImporterFormat.ETC_RGB4);

                if (requirePVRTC)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.PVRTC_RGB2,
                        (int)TextureImporterFormat.PVRTC_RGBA2,
                        (int)TextureImporterFormat.PVRTC_RGB4,
                        (int)TextureImporterFormat.PVRTC_RGBA4
                    });

                if (requireATC)
                    formatValues.AddRange(new int[] {
                        (int)TextureImporterFormat.ATC_RGB4,
                        (int)TextureImporterFormat.ATC_RGBA8,
                    });

                if (requireETC2)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.ETC2_RGB4,
                        (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
                        (int)TextureImporterFormat.ETC2_RGBA8
                    });

                if (requireASTC)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.ASTC_RGB_4x4,
                        (int)TextureImporterFormat.ASTC_RGB_5x5,
                        (int)TextureImporterFormat.ASTC_RGB_6x6,
                        (int)TextureImporterFormat.ASTC_RGB_8x8,
                        (int)TextureImporterFormat.ASTC_RGB_10x10,
                        (int)TextureImporterFormat.ASTC_RGB_12x12,
                        (int)TextureImporterFormat.ASTC_RGBA_4x4,
                        (int)TextureImporterFormat.ASTC_RGBA_5x5,
                        (int)TextureImporterFormat.ASTC_RGBA_6x6,
                        (int)TextureImporterFormat.ASTC_RGBA_8x8,
                        (int)TextureImporterFormat.ASTC_RGBA_10x10,
                        (int)TextureImporterFormat.ASTC_RGBA_12x12
                    });


                formatValues.AddRange(new[] {
                    (int)TextureImporterFormat.RGB16,
                    (int)TextureImporterFormat.ARGB16,
                    (int)TextureImporterFormat.RGBA16,

                    (int)TextureImporterFormat.RGB24,
                    (int)TextureImporterFormat.Alpha8,
                    (int)TextureImporterFormat.ARGB32,
                    (int)TextureImporterFormat.RGBA32,
                    (int)TextureImporterFormat.RGBAHalf,
                    (int)TextureImporterFormat.BC6H,
                    (int)TextureImporterFormat.BC7,

                    (int)TextureImporterFormat.DXT1Crunched,
                    (int)TextureImporterFormat.DXT5Crunched,
                    (int)TextureImporterFormat.ETC_RGB4Crunched,
                    (int)TextureImporterFormat.ETC2_RGBA8Crunched,
                });

                s_TextureFormatsValueAll = formatValues.ToArray();
                return s_TextureFormatsValueAll;
            }
        }

        internal static int[] s_NormalFormatsValueAll;
        internal static int[] NormalFormatsValueAll
        {
            get
            {
                bool requireETC = false;
                bool requirePVRTC = false;
                bool requireATC = false;
                bool requireETC2 = false;
                bool requireASTC = false;

                // Build available normals formats based on available platforms
                BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms();
                foreach (BuildPlatform platform in validPlatforms)
                {
                    switch (platform.defaultTarget)
                    {
                        case BuildTarget.Android:
                            requirePVRTC = true;
                            requireATC = true;
                            requireETC = true;
                            requireETC2 = true;
                            requireASTC = true;
                            break;
                        case BuildTarget.iOS:
                        case BuildTarget.tvOS:
                            requirePVRTC = true;
                            requireETC = true;
                            break;
                        case BuildTarget.Tizen:
                            requireETC = true;
                            break;
                    }
                }
                List<int> formatValues = new List<int>();

                formatValues.AddRange(new[] {
                    (int)TextureImporterFormat.DXT5
                });

                if (requirePVRTC)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.PVRTC_RGB2,
                        (int)TextureImporterFormat.PVRTC_RGBA2,
                        (int)TextureImporterFormat.PVRTC_RGB4,
                        (int)TextureImporterFormat.PVRTC_RGBA4,
                    });

                if (requireATC)
                    formatValues.AddRange(new int[] {
                        (int)TextureImporterFormat.ATC_RGB4,
                        (int)TextureImporterFormat.ATC_RGBA8,
                    });

                if (requireETC)
                    formatValues.AddRange(new int[] {
                        (int)TextureImporterFormat.ETC_RGB4,
                    });

                if (requireETC2)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.ETC2_RGB4,
                        (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
                        (int)TextureImporterFormat.ETC2_RGBA8
                    });

                if (requireASTC)
                    formatValues.AddRange(new[] {
                        (int)TextureImporterFormat.ASTC_RGB_4x4,
                        (int)TextureImporterFormat.ASTC_RGB_5x5,
                        (int)TextureImporterFormat.ASTC_RGB_6x6,
                        (int)TextureImporterFormat.ASTC_RGB_8x8,
                        (int)TextureImporterFormat.ASTC_RGB_10x10,
                        (int)TextureImporterFormat.ASTC_RGB_12x12,
                        (int)TextureImporterFormat.ASTC_RGBA_4x4,
                        (int)TextureImporterFormat.ASTC_RGBA_5x5,
                        (int)TextureImporterFormat.ASTC_RGBA_6x6,
                        (int)TextureImporterFormat.ASTC_RGBA_8x8,
                        (int)TextureImporterFormat.ASTC_RGBA_10x10,
                        (int)TextureImporterFormat.ASTC_RGBA_12x12
                    });

                formatValues.AddRange(new[] {
                    (int)TextureImporterFormat.ARGB16,
                    (int)TextureImporterFormat.RGBA16,

                    (int)TextureImporterFormat.RGBA32,

                    (int)TextureImporterFormat.DXT5Crunched
                });

                s_NormalFormatsValueAll = formatValues.ToArray();

                return s_NormalFormatsValueAll;
            }
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
            TextureImporterFormat.ATC_RGB4,
            TextureImporterFormat.ATC_RGBA8,
            TextureImporterFormat.ETC_RGB4,
            TextureImporterFormat.ETC2_RGB4,
            TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            TextureImporterFormat.ETC2_RGBA8,
            TextureImporterFormat.ASTC_RGB_4x4,
            TextureImporterFormat.ASTC_RGB_5x5,
            TextureImporterFormat.ASTC_RGB_6x6,
            TextureImporterFormat.ASTC_RGB_8x8,
            TextureImporterFormat.ASTC_RGB_10x10,
            TextureImporterFormat.ASTC_RGB_12x12,
            TextureImporterFormat.ASTC_RGBA_4x4,
            TextureImporterFormat.ASTC_RGBA_5x5,
            TextureImporterFormat.ASTC_RGBA_6x6,
            TextureImporterFormat.ASTC_RGBA_8x8,
            TextureImporterFormat.ASTC_RGBA_10x10,
            TextureImporterFormat.ASTC_RGBA_12x12
        };

#pragma warning disable 649
        internal static string[] s_TextureFormatStringsAll;
        internal static string[] s_TextureFormatStringsWiiU;
        internal static string[] s_TextureFormatStringsPSP2;
        internal static string[] s_TextureFormatStringsSwitch;
        internal static string[] s_TextureFormatStringsWebGL;
        internal static string[] s_TextureFormatStringsApplePVR;
        internal static string[] s_TextureFormatStringsAndroid;
        internal static string[] s_TextureFormatStringsTizen;
        internal static string[] s_TextureFormatStringsSingleChannel;
        internal static string[] s_TextureFormatStringsDefault;
        internal static string[] s_NormalFormatStringsDefault;

        enum CookieMode
        {
            Spot = 0, Directional = 1, Point = 2
        }

        readonly AnimBool m_ShowBumpGenerationSettings = new AnimBool();
        readonly AnimBool m_ShowCubeMapSettings = new AnimBool();
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
            public readonly GUIContent textureTypeTitle = EditorGUIUtility.TextContent("Texture Type|What will this texture be used for?");
            public readonly GUIContent[] textureTypeOptions =
            {
                EditorGUIUtility.TextContent("Default|Texture is a normal image such as a diffuse texture or other."),
                EditorGUIUtility.TextContent("Normal map|Texture is a bump or normal map."),
                EditorGUIUtility.TextContent("Editor GUI and Legacy GUI|Texture is used for a GUI element."),
                EditorGUIUtility.TextContent("Sprite (2D and UI)|Texture is used for a sprite."),
                EditorGUIUtility.TextContent("Cursor|Texture is used for a cursor."),
                EditorGUIUtility.TextContent("Cookie|Texture is a cookie you put on a light."),
                EditorGUIUtility.TextContent("Lightmap|Texture is a lightmap."),
                EditorGUIUtility.TextContent("Single Channel|Texture is a one component texture."),
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
                (int)TextureImporterType.SingleChannel
            };

            public readonly GUIContent textureShape = EditorGUIUtility.TextContent("Texture Shape|What shape is this texture?");
            private readonly GUIContent textureShape2D = EditorGUIUtility.TextContent("2D|Texture is 2D.");
            private readonly  GUIContent textureShapeCube = EditorGUIUtility.TextContent("Cube|Texture is a Cubemap.");
            public readonly Dictionary<TextureImporterShape, GUIContent[]> textureShapeOptionsDictionnary = new Dictionary<TextureImporterShape, GUIContent[]>();
            public readonly Dictionary<TextureImporterShape, int[]> textureShapeValuesDictionnary = new Dictionary<TextureImporterShape, int[]>();


            public readonly GUIContent filterMode = EditorGUIUtility.TextContent("Filter Mode");
            public readonly GUIContent[] filterModeOptions =
            {
                EditorGUIUtility.TextContent("Point (no filter)"),
                EditorGUIUtility.TextContent("Bilinear"),
                EditorGUIUtility.TextContent("Trilinear")
            };

            public readonly GUIContent cookieType = EditorGUIUtility.TextContent("Light Type");
            public readonly GUIContent[] cookieOptions =
            {
                EditorGUIUtility.TextContent("Spotlight"),
                EditorGUIUtility.TextContent("Directional"),
                EditorGUIUtility.TextContent("Point"),
            };
            public readonly GUIContent generateFromBump = EditorGUIUtility.TextContent("Create from Grayscale|The grayscale of the image is used as a heightmap for generating the normal map.");
            public readonly GUIContent bumpiness = EditorGUIUtility.TextContent("Bumpiness");
            public readonly GUIContent bumpFiltering = EditorGUIUtility.TextContent("Filtering");
            public readonly GUIContent[] bumpFilteringOptions =
            {
                EditorGUIUtility.TextContent("Sharp"),
                EditorGUIUtility.TextContent("Smooth"),
            };
            public readonly GUIContent cubemap = EditorGUIUtility.TextContent("Mapping");
            public readonly GUIContent[] cubemapOptions =
            {
                EditorGUIUtility.TextContent("Auto"),
                EditorGUIUtility.TextContent("6 Frames Layout (Cubic Environment)|Texture contains 6 images arranged in one of the standard cubemap layouts - cross or sequence (+x,-x, +y, -y, +z, -z). Texture can be in vertical or horizontal orientation."),
                EditorGUIUtility.TextContent("Latitude-Longitude Layout (Cylindrical)|Texture contains an image of a ball unwrapped such that latitude and longitude are mapped to horizontal and vertical dimensions (as on a globe)."),
                EditorGUIUtility.TextContent("Mirrored Ball (Spheremap)|Texture contains an image of a mirrored ball.")
            };
            public readonly int[] cubemapValues2 =
            {
                (int)TextureImporterGenerateCubemap.AutoCubemap,
                (int)TextureImporterGenerateCubemap.FullCubemap,
                (int)TextureImporterGenerateCubemap.Cylindrical,
                (int)TextureImporterGenerateCubemap.Spheremap
            };

            public readonly GUIContent cubemapConvolution = EditorGUIUtility.TextContent("Convolution Type");
            public readonly GUIContent[] cubemapConvolutionOptions =
            {
                EditorGUIUtility.TextContent("None"),
                EditorGUIUtility.TextContent("Specular (Glossy Reflection)|Convolve cubemap for specular reflections with varying smoothness (Glossy Reflections)."),
                EditorGUIUtility.TextContent("Diffuse (Irradiance)|Convolve cubemap for diffuse-only reflection (Irradiance Cubemap).")
            };
            public readonly int[] cubemapConvolutionValues =
            {
                (int)TextureImporterCubemapConvolution.None,
                (int)TextureImporterCubemapConvolution.Specular,
                (int)TextureImporterCubemapConvolution.Diffuse
            };

            public readonly GUIContent seamlessCubemap = EditorGUIUtility.TextContent("Fixup Edge Seams|Enable if this texture is used for glossy reflections.");
            public readonly GUIContent textureFormat = EditorGUIUtility.TextContent("Format");

            public readonly GUIContent defaultPlatform = EditorGUIUtility.TextContent("Default");
            public readonly GUIContent mipmapFadeOutToggle = EditorGUIUtility.TextContent("Fadeout Mip Maps");
            public readonly GUIContent mipmapFadeOut = EditorGUIUtility.TextContent("Fade Range");
            public readonly GUIContent readWrite = EditorGUIUtility.TextContent("Read/Write Enabled|Enable to be able to access the raw pixel data from code.");

            public readonly GUIContent alphaSource = EditorGUIUtility.TextContent("Alpha Source|How is the alpha generated for the imported texture.");
            public readonly GUIContent[] alphaSourceOptions =
            {
                EditorGUIUtility.TextContent("None|No Alpha will be used."),
                EditorGUIUtility.TextContent("Input Texture Alpha|Use Alpha from the input texture if one is provided."),
                EditorGUIUtility.TextContent("From Gray Scale|Generate Alpha from image gray scale."),
            };
            public readonly int[] alphaSourceValues =
            {
                (int)TextureImporterAlphaSource.None,
                (int)TextureImporterAlphaSource.FromInput,
                (int)TextureImporterAlphaSource.FromGrayScale,
            };

            public readonly GUIContent generateMipMaps = EditorGUIUtility.TextContent("Generate Mip Maps");
            public readonly GUIContent sRGBTexture = EditorGUIUtility.TextContent("sRGB (Color Texture)|Texture content is stored in gamma space. Non-HDR color textures should enable this flag (except if used for IMGUI).");
            public readonly GUIContent borderMipMaps = EditorGUIUtility.TextContent("Border Mip Maps");
            public readonly GUIContent mipMapsPreserveCoverage = EditorGUIUtility.TextContent("Mip Maps Preserve Coverage|The alpha channel of generated Mip Maps will preserve coverage during the alpha test.");
            public readonly GUIContent alphaTestReferenceValue = EditorGUIUtility.TextContent("Alpha Cutoff Value|The reference value used during the alpha test. Controls Mip Map coverage.");
            public readonly GUIContent mipMapFilter = EditorGUIUtility.TextContent("Mip Map Filtering");
            public readonly GUIContent[] mipMapFilterOptions =
            {
                EditorGUIUtility.TextContent("Box"),
                EditorGUIUtility.TextContent("Kaiser"),
            };
            public readonly GUIContent npot = EditorGUIUtility.TextContent("Non Power of 2|How non-power-of-two textures are scaled on import.");
            public readonly GUIContent generateCubemap = EditorGUIUtility.TextContent("Generate Cubemap");

            public readonly GUIContent compressionQuality = EditorGUIUtility.TextContent("Compressor Quality");
            public readonly GUIContent compressionQualitySlider = EditorGUIUtility.TextContent("Compressor Quality|Use the slider to adjust compression quality from 0 (Fastest) to 100 (Best)");
            public readonly GUIContent[] mobileCompressionQualityOptions =
            {
                EditorGUIUtility.TextContent("Fast"),
                EditorGUIUtility.TextContent("Normal"),
                EditorGUIUtility.TextContent("Best")
            };

            public readonly GUIContent spriteMode = EditorGUIUtility.TextContent("Sprite Mode");
            public readonly GUIContent[] spriteModeOptions =
            {
                EditorGUIUtility.TextContent("Single"),
                EditorGUIUtility.TextContent("Multiple"),
                EditorGUIUtility.TextContent("Polygon"),
            };
            public readonly GUIContent[] spriteMeshTypeOptions =
            {
                EditorGUIUtility.TextContent("Full Rect"),
                EditorGUIUtility.TextContent("Tight"),
            };

            public readonly GUIContent spritePackingTag = EditorGUIUtility.TextContent("Packing Tag|Tag for the Sprite Packing system.");
            public readonly GUIContent spritePixelsPerUnit = EditorGUIUtility.TextContent("Pixels Per Unit|How many pixels in the sprite correspond to one unit in the world.");
            public readonly GUIContent spriteExtrude = EditorGUIUtility.TextContent("Extrude Edges|How much empty area to leave around the sprite in the generated mesh.");
            public readonly GUIContent spriteMeshType = EditorGUIUtility.TextContent("Mesh Type|Type of sprite mesh to generate.");
            public readonly GUIContent spriteAlignment = EditorGUIUtility.TextContent("Pivot|Sprite pivot point in its localspace. May be used for syncing animation frames of different sizes.");
            public readonly GUIContent[] spriteAlignmentOptions =
            {
                EditorGUIUtility.TextContent("Center"),
                EditorGUIUtility.TextContent("Top Left"),
                EditorGUIUtility.TextContent("Top"),
                EditorGUIUtility.TextContent("Top Right"),
                EditorGUIUtility.TextContent("Left"),
                EditorGUIUtility.TextContent("Right"),
                EditorGUIUtility.TextContent("Bottom Left"),
                EditorGUIUtility.TextContent("Bottom"),
                EditorGUIUtility.TextContent("Bottom Right"),
                EditorGUIUtility.TextContent("Custom"),
            };
            public readonly GUIContent spriteGenerateFallbackPhysicsShape = EditorGUIUtility.TrTextContent("Generate Physics Shape", "Generates a default physics shape from the outline of the Sprite/s when a physics shape has not been set in the Sprite Editor.");

            public readonly GUIContent alphaIsTransparency = EditorGUIUtility.TextContent("Alpha Is Transparency|If the provided alpha channel is transparency, enable this to pre-filter the color to avoid texture filtering artifacts. This is not supported for HDR textures.");
            public readonly GUIContent etc1Compression = EditorGUIUtility.TextContent("Compress using ETC1 (split alpha channel)|Alpha for this texture will be preserved by splitting the alpha channel to another texture, and both resulting textures will be compressed using ETC1.");

            public readonly GUIContent crunchedCompression = EditorGUIUtility.TextContent("Use Crunch Compression|Texture is crunch-compressed to save space on disk when applicable.");

            public readonly GUIContent showAdvanced = EditorGUIUtility.TextContent("Advanced|Show advanced settings.");

            public Styles()
            {
                // This is far from ideal, but it's better than having tons of logic in the GUI code itself.
                // The combination should not grow too much anyway since only Texture3D will be added later.
                GUIContent[] s2D_Options = { textureShape2D };
                GUIContent[] sCube_Options = { textureShapeCube };
                GUIContent[] s2D_Cube_Options = { textureShape2D, textureShapeCube };
                textureShapeOptionsDictionnary.Add(TextureImporterShape.Texture2D, s2D_Options);
                textureShapeOptionsDictionnary.Add(TextureImporterShape.TextureCube, sCube_Options);
                textureShapeOptionsDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube, s2D_Cube_Options);

                int[] s2D_Values = { (int)TextureImporterShape.Texture2D };
                int[] sCube_Values = { (int)TextureImporterShape.TextureCube };
                int[] s2D_Cube_Values = { (int)TextureImporterShape.Texture2D, (int)TextureImporterShape.TextureCube };
                textureShapeValuesDictionnary.Add(TextureImporterShape.Texture2D, s2D_Values);
                textureShapeValuesDictionnary.Add(TextureImporterShape.TextureCube, sCube_Values);
                textureShapeValuesDictionnary.Add(TextureImporterShape.Texture2D | TextureImporterShape.TextureCube, s2D_Cube_Values);
            }
        }

        internal static Styles s_Styles;

        TextureInspectorTypeGUIProperties[] m_TextureTypeGUIElements = new TextureInspectorTypeGUIProperties[Enum.GetValues(typeof(TextureImporterType)).Length];
        List<TextureInspectorGUIElement>    m_GUIElementsDisplayOrder = new List<TextureInspectorGUIElement>();


        void ToggleFromInt(SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            int value = EditorGUILayout.Toggle(label, property.intValue > 0) ? 1 : 0;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.intValue = value;
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
        SerializedProperty m_GenerateCubemap;
        SerializedProperty m_CubemapConvolution;
        SerializedProperty m_SeamlessCubemap;
        SerializedProperty m_BorderMipMap;
        SerializedProperty m_MipMapsPreserveCoverage;
        SerializedProperty m_AlphaTestReferenceValue;
        SerializedProperty m_NPOTScale;
        SerializedProperty m_IsReadable;
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

        SerializedProperty m_SpritePackingTag;
        SerializedProperty m_SpritePixelsToUnits;
        SerializedProperty m_SpriteExtrude;
        SerializedProperty m_SpriteMeshType;
        SerializedProperty m_Alignment;
        SerializedProperty m_SpritePivot;
        SerializedProperty m_SpriteGenerateFallbackPhysicsShape;

        SerializedProperty m_AlphaIsTransparency;

        SerializedProperty m_TextureShape;

        SerializedProperty m_SpriteMode;
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

        void CacheSerializedProperties()
        {
            m_AlphaSource = serializedObject.FindProperty("m_AlphaUsage");
            m_ConvertToNormalMap = serializedObject.FindProperty("m_ConvertToNormalMap");
            m_HeightScale = serializedObject.FindProperty("m_HeightScale");
            m_NormalMapFilter = serializedObject.FindProperty("m_NormalMapFilter");
            m_GenerateCubemap = serializedObject.FindProperty("m_GenerateCubemap");
            m_SeamlessCubemap = serializedObject.FindProperty("m_SeamlessCubemap");
            m_BorderMipMap = serializedObject.FindProperty("m_BorderMipMap");
            m_MipMapsPreserveCoverage = serializedObject.FindProperty("m_MipMapsPreserveCoverage");
            m_AlphaTestReferenceValue = serializedObject.FindProperty("m_AlphaTestReferenceValue");
            m_NPOTScale = serializedObject.FindProperty("m_NPOTScale");
            m_IsReadable = serializedObject.FindProperty("m_IsReadable");
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
            m_SpritePackingTag = serializedObject.FindProperty("m_SpritePackingTag");
            m_SpritePixelsToUnits = serializedObject.FindProperty("m_SpritePixelsToUnits");
            m_SpriteExtrude = serializedObject.FindProperty("m_SpriteExtrude");
            m_SpriteMeshType = serializedObject.FindProperty("m_SpriteMeshType");
            m_Alignment = serializedObject.FindProperty("m_Alignment");
            m_SpritePivot = serializedObject.FindProperty("m_SpritePivot");
            m_SpriteGenerateFallbackPhysicsShape = serializedObject.FindProperty("m_SpriteGenerateFallbackPhysicsShape");

            m_AlphaIsTransparency = serializedObject.FindProperty("m_AlphaIsTransparency");

            m_TextureType = serializedObject.FindProperty("m_TextureType");
            m_TextureShape = serializedObject.FindProperty("m_TextureShape");
        }

        void InitializeGUI()
        {
            // This is where we decide what GUI elements are displayed depending on the texture type.
            // TODO: Maybe complement the bitfield with a list to add a concept of order in the display. Not sure if necessary...
            TextureImporterShape shapeCapsAll = TextureImporterShape.Texture2D | TextureImporterShape.TextureCube;

            m_TextureTypeGUIElements[(int)TextureImporterType.Default]      = new TextureInspectorTypeGUIProperties(TextureInspectorGUIElement.ColorSpace | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.CubeMapConvolution | TextureInspectorGUIElement.CubeMapping,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps
                    , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.NormalMap]    = new TextureInspectorTypeGUIProperties(TextureInspectorGUIElement.NormalMap | TextureInspectorGUIElement.CubeMapping,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps
                    , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.Sprite]       = new TextureInspectorTypeGUIProperties(TextureInspectorGUIElement.Sprite,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.MipMaps | TextureInspectorGUIElement.ColorSpace,
                    TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Cookie]       = new TextureInspectorTypeGUIProperties(TextureInspectorGUIElement.Cookie | TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.CubeMapping,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps,
                    TextureImporterShape.Texture2D | TextureImporterShape.TextureCube);
            m_TextureTypeGUIElements[(int)TextureImporterType.SingleChannel] = new TextureInspectorTypeGUIProperties(TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.CubeMapping,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps
                    , shapeCapsAll);
            m_TextureTypeGUIElements[(int)TextureImporterType.GUI]          = new TextureInspectorTypeGUIProperties(0,
                    TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps,
                    TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Cursor]       = new TextureInspectorTypeGUIProperties(0,
                    TextureInspectorGUIElement.AlphaHandling | TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps,
                    TextureImporterShape.Texture2D);
            m_TextureTypeGUIElements[(int)TextureImporterType.Lightmap]     = new TextureInspectorTypeGUIProperties(0,
                    TextureInspectorGUIElement.PowerOfTwo | TextureInspectorGUIElement.Readable | TextureInspectorGUIElement.MipMaps
                    , TextureImporterShape.Texture2D);

            m_GUIElementMethods.Clear();
            m_GUIElementMethods.Add(TextureInspectorGUIElement.PowerOfTwo, this.POTScaleGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Readable, this.ReadableGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.ColorSpace, this.ColorSpaceGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.AlphaHandling, this.AlphaHandlingGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.MipMaps, this.MipMapGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.NormalMap, this.BumpGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Sprite, this.SpriteGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.Cookie, this.CookieGUI);
            m_GUIElementMethods.Add(TextureInspectorGUIElement.CubeMapping, this.CubemapMappingGUI);

            // This list dictates the order in which the GUI Elements are displayed.
            // It could be different for each TextureImporterType but let's keep it simple for now.
            m_GUIElementsDisplayOrder.Clear();
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.CubeMapping);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.CubeMapConvolution);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Cookie);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.ColorSpace);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.AlphaHandling);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.NormalMap);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Sprite);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.PowerOfTwo);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.Readable);
            m_GUIElementsDisplayOrder.Add(TextureInspectorGUIElement.MipMaps);

            UnityEngine.Debug.Assert(m_GUIElementsDisplayOrder.Count == (Enum.GetValues(typeof(TextureInspectorGUIElement)).Length - 1), "Some GUIElement are not present in the list."); // -1 because TextureInspectorGUIElement.None
        }

        public override void OnEnable()
        {
            s_DefaultPlatformName = TextureImporter.defaultPlatformName; // Can't be called everywhere so we save it here for later use.

            m_ShowAdvanced = EditorPrefs.GetBool("TextureImporterShowAdvanced", m_ShowAdvanced);

            CacheSerializedProperties();

            m_ShowBumpGenerationSettings.valueChanged.AddListener(Repaint);
            m_ShowCubeMapSettings.valueChanged.AddListener(Repaint);
            m_ShowCubeMapSettings.value = (TextureImporterShape)m_TextureShape.intValue == TextureImporterShape.TextureCube;
            //@TODO change to use spriteMode enum when available
            m_ShowGenericSpriteSettings.valueChanged.AddListener(Repaint);
            m_ShowGenericSpriteSettings.value = m_SpriteMode.intValue != 0;
            m_ShowSpriteMeshTypeOption.valueChanged.AddListener(Repaint);
            m_ShowSpriteMeshTypeOption.value = ShouldShowSpriteMeshTypeOption();
            m_ShowMipMapSettings.valueChanged.AddListener(Repaint);
            m_ShowMipMapSettings.value = m_EnableMipMap.boolValue;

            InitializeGUI();

            var importer = target as TextureImporter;
            if (importer == null)
                return;

            importer.GetWidthAndHeight(ref m_TextureWidth, ref m_TextureHeight);
            m_IsPOT = IsPowerOfTwo(m_TextureWidth) && IsPowerOfTwo(m_TextureHeight);

            InitializeTextureFormatStrings();
        }

        void SetSerializedPropertySettings(TextureImporterSettings settings)
        {
            m_AlphaSource.intValue = (int)settings.alphaSource;
            m_ConvertToNormalMap.intValue = settings.convertToNormalMap ? 1 : 0;
            m_HeightScale.floatValue = settings.heightmapScale;
            m_NormalMapFilter.intValue = (int)settings.normalMapFilter;
            m_GenerateCubemap.intValue = (int)settings.generateCubemap;
            m_CubemapConvolution.intValue = (int)settings.cubemapConvolution;
            m_SeamlessCubemap.intValue = settings.seamlessCubemap ? 1 : 0;
            m_BorderMipMap.intValue = settings.borderMipmap ? 1 : 0;
            m_MipMapsPreserveCoverage.intValue = settings.mipMapsPreserveCoverage ? 1 : 0;
            m_AlphaTestReferenceValue.floatValue = settings.alphaTestReferenceValue;
            m_NPOTScale.intValue = (int)settings.npotScale;
            m_IsReadable.intValue = settings.readable ? 1 : 0;
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

            m_TextureType.intValue = (int)settings.textureType;
            m_TextureShape.intValue = (int)settings.textureShape;
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

            return settings;
        }

        void CookieGUI(TextureInspectorGUIElement guiElements)
        {
            EditorGUI.BeginChangeCheck();
            CookieMode cm;
            if (m_BorderMipMap.intValue > 0)
                cm = CookieMode.Spot;
            else if (m_TextureShape.intValue == (int)TextureImporterShape.TextureCube)
                cm = CookieMode.Point;
            else
                cm = CookieMode.Directional;

            cm = (CookieMode)EditorGUILayout.Popup(s_Styles.cookieType, (int)cm, s_Styles.cookieOptions);
            if (EditorGUI.EndChangeCheck())
                SetCookieMode(cm);

            if (cm == CookieMode.Point)
            {
                m_TextureShape.intValue = (int)TextureImporterShape.TextureCube;
            }
            else
            {
                m_TextureShape.intValue = (int)TextureImporterShape.Texture2D;
            }
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
                        EditorGUI.showMixedValue = m_GenerateCubemap.hasMultipleDifferentValues || m_SeamlessCubemap.hasMultipleDifferentValues;

                        EditorGUI.BeginChangeCheck();

                        int value = EditorGUILayout.IntPopup(s_Styles.cubemap, m_GenerateCubemap.intValue, s_Styles.cubemapOptions, s_Styles.cubemapValues2);
                        if (EditorGUI.EndChangeCheck())
                            m_GenerateCubemap.intValue = value;

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
                        EditorGUI.showMixedValue = false;
                        EditorGUILayout.Space();
                    }
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        void ColorSpaceGUI(TextureInspectorGUIElement guiElements)
        {
            ToggleFromInt(m_sRGBTexture, s_Styles.sRGBTexture);
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
            ToggleFromInt(m_IsReadable, s_Styles.readWrite);
        }


        void AlphaHandlingGUI(TextureInspectorGUIElement guiElements)
        {
            int countWithAlpha = 0;
            int countHDR = 0;

            bool success = CountImportersWithAlpha(targets, out countWithAlpha);
            success = success && CountImportersWithHDR(targets, out countHDR);

            EditorGUI.showMixedValue = m_AlphaSource.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int newAlphaUsage = EditorGUILayout.IntPopup(s_Styles.alphaSource, m_AlphaSource.intValue, s_Styles.alphaSourceOptions, s_Styles.alphaSourceValues);

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                m_AlphaSource.intValue = newAlphaUsage;
            }

            bool showAlphaIsTransparency = success && (TextureImporterAlphaSource)m_AlphaSource.intValue != TextureImporterAlphaSource.None && countHDR == 0; // AlphaIsTransparency is not properly implemented for HDR texture yet.
            using (new EditorGUI.DisabledScope(!showAlphaIsTransparency))
            {
                ToggleFromInt(m_AlphaIsTransparency, s_Styles.alphaIsTransparency);
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
                EditorGUILayout.PropertyField(m_SpritePackingTag, s_Styles.spritePackingTag);
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
                                ApplyAndImport();
                                SpriteEditorWindow.GetWindow();

                                // We reimported the asset which destroyed the editor, so we can't keep running the UI here.
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            SpriteEditorWindow.GetWindow();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel--;
        }

        void MipMapGUI(TextureInspectorGUIElement guiElements)
        {
            ToggleFromInt(m_EnableMipMap, s_Styles.generateMipMaps);

            m_ShowMipMapSettings.target = m_EnableMipMap.boolValue && !m_EnableMipMap.hasMultipleDifferentValues;
            if (EditorGUILayout.BeginFadeGroup(m_ShowMipMapSettings.faded))
            {
                EditorGUI.indentLevel++;
                ToggleFromInt(m_BorderMipMap, s_Styles.borderMipMaps);
                EditorGUILayout.Popup(m_MipMapMode, s_Styles.mipMapFilterOptions, s_Styles.mipMapFilter);

                ToggleFromInt(m_MipMapsPreserveCoverage, s_Styles.mipMapsPreserveCoverage);
                if (m_MipMapsPreserveCoverage.intValue != 0 && !m_MipMapsPreserveCoverage.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AlphaTestReferenceValue, s_Styles.alphaTestReferenceValue);
                    EditorGUI.indentLevel--;
                }

                // Mipmap fadeout
                ToggleFromInt(m_FadeOut, s_Styles.mipmapFadeOutToggle);
                if (m_FadeOut.intValue > 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    float min = m_MipMapFadeDistanceStart.intValue;
                    float max = m_MipMapFadeDistanceEnd.intValue;
                    EditorGUILayout.MinMaxSlider(s_Styles.mipmapFadeOut, ref min, ref max, 0, 10);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_MipMapFadeDistanceStart.intValue = Mathf.RoundToInt(min);
                        m_MipMapFadeDistanceEnd.intValue = Mathf.RoundToInt(max);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
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

            if (EditorGUI.EndChangeCheck())
                SyncPlatformSettings();
        }

        bool m_ShowPerAxisWrapModes = false;

        void TextureSettingsGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Wrap mode
            // NOTE: once we get ability to have 3D/Volume texture shapes, should pass true for isVolume based on m_TextureShape
            bool isVolume = false;
            TextureInspector.WrapModePopup(m_WrapU, m_WrapV, m_WrapW, isVolume, ref m_ShowPerAxisWrapModes);


            // Display warning about repeat wrap mode on restricted npot emulation
            if (m_NPOTScale.intValue == (int)TextureImporterNPOTScale.None &&
                (m_WrapU.intValue == (int)TextureWrapMode.Repeat || m_WrapV.intValue == (int)TextureWrapMode.Repeat) &&
                !ShaderUtil.hardwareSupportsFullNPOT)
            {
                bool displayWarning = false;
                foreach (var target in targets)
                {
                    int w = -1, h = -1;
                    var imp = (TextureImporter)target;
                    imp.GetWidthAndHeight(ref w, ref h);
                    if (!Mathf.IsPowerOfTwo(w) || !Mathf.IsPowerOfTwo(h))
                    {
                        displayWarning = true;
                        break;
                    }
                }

                if (displayWarning)
                {
                    GUIContent c = EditorGUIUtility.TextContent("Graphics device doesn't support Repeat wrap mode on NPOT textures. Falling back to Clamp.");
                    EditorGUILayout.HelpBox(c.text, MessageType.Warning, true);
                }
            }

            // Filter mode
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_FilterMode.hasMultipleDifferentValues;
            FilterMode filter = (FilterMode)m_FilterMode.intValue;
            if ((int)filter == -1)
            {
                if (m_FadeOut.intValue > 0 || m_ConvertToNormalMap.intValue > 0)
                    filter = FilterMode.Trilinear;
                else
                    filter = FilterMode.Bilinear;
            }
            filter = (FilterMode)EditorGUILayout.IntPopup(s_Styles.filterMode, (int)filter, s_Styles.filterModeOptions, m_FilterModeOptions);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_FilterMode.intValue = (int)filter;

            // Aniso
            bool showAniso = (FilterMode)m_FilterMode.intValue != FilterMode.Point
                && m_EnableMipMap.intValue > 0
                && (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.TextureCube;
            using (new EditorGUI.DisabledScope(!showAniso))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = m_Aniso.hasMultipleDifferentValues;
                int aniso = m_Aniso.intValue;
                if (aniso == -1)
                    aniso = 1;
                aniso = EditorGUILayout.IntSlider("Aniso Level", aniso, 0, 16);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    m_Aniso.intValue = aniso;

                TextureInspector.DoAnisoGlobalSettingNote(aniso);
            }

            if (EditorGUI.EndChangeCheck())
                ApplySettingsToTexture();
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            bool wasEnabled = GUI.enabled;

            EditorGUILayout.Space();

            // Texture Usage
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_TextureType.hasMultipleDifferentValues;
            int newTextureType = EditorGUILayout.IntPopup(s_Styles.textureTypeTitle, m_TextureType.intValue, s_Styles.textureTypeOptions, s_Styles.textureTypeValues);
            EditorGUI.showMixedValue = false;
            // (case 857001) EndChangeCheck will return true even if the same value is selected.
            // Consequently the sprite will be reset to Single mode and looks very confusing to the user.
            if (EditorGUI.EndChangeCheck() && (m_TextureType.intValue != newTextureType))
            {
                TextureImporterSettings settings = GetSerializedPropertySettings();
                settings.ApplyTextureType((TextureImporterType)newTextureType);
                m_TextureType.intValue = newTextureType;

                SetSerializedPropertySettings(settings);

                SyncPlatformSettings();
                ApplySettingsToTexture();
            }

            // Texture Shape
            int[] shapeArray = s_Styles.textureShapeValuesDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps];
            using (new EditorGUI.DisabledScope(shapeArray.Length == 1 || m_TextureType.intValue == (int)TextureImporterType.Cookie)) // Cookie is a special case because the cookie type drives the shape of the texture
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = m_TextureShape.hasMultipleDifferentValues;
                int newTextureShape = EditorGUILayout.IntPopup(s_Styles.textureShape, m_TextureShape.intValue, s_Styles.textureShapeOptionsDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps], s_Styles.textureShapeValuesDictionnary[m_TextureTypeGUIElements[(int)newTextureType].shapeCaps]);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    m_TextureShape.intValue = newTextureShape;
            }

            // Switching usage can lead to a subset of the current available shapes.
            if (Array.IndexOf(shapeArray, m_TextureShape.intValue) == -1)
            {
                m_TextureShape.intValue = shapeArray[0];
            }

            EditorGUILayout.Space();

            if (!m_TextureType.hasMultipleDifferentValues)
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

            ShowPlatformSpecificSettings();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            ApplyRevertGUI();
            GUILayout.EndHorizontal();

            // screw this - after lots of retries i have no idea how to poll it only when we change related stuff
            UpdateImportWarning();
            if (m_ImportWarning != null)
                EditorGUILayout.HelpBox(m_ImportWarning, MessageType.Warning);

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

        void ApplySettingsToTexture()
        {
            foreach (AssetImporter importer in targets)
            {
                Texture tex = AssetDatabase.LoadMainAssetAtPath(importer.assetPath) as Texture;
                if (tex != null) // This can happen if the texture fails to import (for example, cube texture with non-PoT input).
                {
                    if (m_Aniso.intValue != -1)
                        TextureUtil.SetAnisoLevelNoDirty(tex, m_Aniso.intValue);
                    if (m_FilterMode.intValue != -1)
                        TextureUtil.SetFilterModeNoDirty(tex, (FilterMode)m_FilterMode.intValue);
                    if ((m_WrapU.intValue != -1 || m_WrapV.intValue != -1 || m_WrapW.intValue != -1) &&
                        !m_WrapU.hasMultipleDifferentValues && !m_WrapV.hasMultipleDifferentValues && !m_WrapW.hasMultipleDifferentValues)
                    {
                        TextureUtil.SetWrapModeNoDirty(tex, (TextureWrapMode)m_WrapU.intValue, (TextureWrapMode)m_WrapV.intValue, (TextureWrapMode)m_WrapW.intValue);
                    }
                }
            }

            SceneView.RepaintAll();
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

        void SetCookieMode(CookieMode cm)
        {
            switch (cm)
            {
                case CookieMode.Spot:
                    m_BorderMipMap.intValue = 1;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Clamp;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.AutoCubemap;
                    m_TextureShape.intValue = (int)TextureImporterShape.Texture2D;
                    break;
                case CookieMode.Point:
                    m_BorderMipMap.intValue = 0;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Clamp;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.Spheremap;
                    m_TextureShape.intValue = (int)TextureImporterShape.TextureCube;
                    break;
                case CookieMode.Directional:
                    m_BorderMipMap.intValue = 0;
                    m_WrapU.intValue = m_WrapV.intValue = m_WrapW.intValue = (int)TextureWrapMode.Repeat;
                    m_GenerateCubemap.intValue = (int)TextureImporterGenerateCubemap.AutoCubemap;
                    m_TextureShape.intValue = (int)TextureImporterShape.Texture2D;
                    break;
            }
        }

        void SyncPlatformSettings()
        {
            foreach (TextureImportPlatformSettings ps in m_PlatformSettings)
                ps.Sync();
        }

        internal static string[] BuildTextureStrings(int[] texFormatValues)
        {
            string[] retval = new string[texFormatValues.Length];
            for (int i = 0; i < texFormatValues.Length; i++)
            {
                int val = texFormatValues[i];
                retval[i] = " " + TextureUtil.GetTextureFormatString((TextureFormat)val);
            }
            return retval;
        }

        internal static void InitializeTextureFormatStrings()
        {
            if (s_TextureFormatStringsApplePVR == null)
                s_TextureFormatStringsApplePVR = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueApplePVR);
            if (s_TextureFormatStringsAndroid == null)
                s_TextureFormatStringsAndroid = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueAndroid);
            if (s_TextureFormatStringsTizen == null)
                s_TextureFormatStringsTizen = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueTizen);
            if (s_TextureFormatStringsWebGL == null)
                s_TextureFormatStringsWebGL = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueWebGL);
            if (s_TextureFormatStringsWiiU == null)
                s_TextureFormatStringsWiiU = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueWiiU);
            if (s_TextureFormatStringsPSP2 == null)
                s_TextureFormatStringsPSP2 = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValuePSP2);
            if (s_TextureFormatStringsSwitch == null)
                s_TextureFormatStringsSwitch = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueSwitch);
            if (s_TextureFormatStringsDefault == null)
                s_TextureFormatStringsDefault = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueDefault);
            if (s_NormalFormatStringsDefault == null)
                s_NormalFormatStringsDefault = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kNormalFormatsValueDefault);
            if (s_TextureFormatStringsSingleChannel == null)
                s_TextureFormatStringsSingleChannel = TextureImporterInspector.BuildTextureStrings(TextureImportPlatformSettings.kTextureFormatsValueSingleChannel);
        }

        internal static bool IsFormatRequireCompressionSetting(TextureImporterFormat format)
        {
            return ArrayUtility.Contains<TextureImporterFormat>(TextureImporterInspector.kFormatsWithCompressionSettings, format);
        }

        protected void ShowPlatformSpecificSettings()
        {
            BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms().ToArray();
            GUILayout.Space(10);
            int shownTextureFormatPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, s_Styles.defaultPlatform);
            TextureImportPlatformSettings realPS = m_PlatformSettings[shownTextureFormatPage + 1];

            if (!realPS.isDefault)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = realPS.overriddenIsDifferent;

                string title = "Override for " + validPlatforms[shownTextureFormatPage].title.text;
                bool newOverride = EditorGUILayout.ToggleLeft(title, realPS.overridden);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    realPS.SetOverriddenForAll(newOverride);
                    SyncPlatformSettings();
                }
            }

            // Disable size and format GUI if not overwritten for all objects
            bool notAllOverriddenForThisPlatform = (!realPS.isDefault && !realPS.allAreOverridden);
            using (new EditorGUI.DisabledScope(notAllOverriddenForThisPlatform))
            {
                // acquire the platform support module for this platform, and present the appropriate UI
                ITextureImportSettingsExtension textureSettingsExtension = ModuleManager.GetTextureImportSettingsExtension(realPS.m_Target);
                textureSettingsExtension.ShowImportSettings(this, realPS);

                //just do this once, regardless of whether things changed
                SyncPlatformSettings();
            }

            EditorGUILayout.EndPlatformGrouping();
        }

        private static bool IsPowerOfTwo(int f)
        {
            return ((f & (f - 1)) == 0);
        }

        public static BuildPlatform[] GetBuildPlayerValidPlatforms()
        {
            List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            return validPlatforms.ToArray();
        }

        public virtual void BuildTargetList()
        {
            BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms();

            m_PlatformSettings = new List<TextureImportPlatformSettings>();
            m_PlatformSettings.Add(new TextureImportPlatformSettings(TextureImporterInspector.s_DefaultPlatformName, BuildTarget.StandaloneWindows, this));

            foreach (BuildPlatform bp in validPlatforms)
                m_PlatformSettings.Add(new TextureImportPlatformSettings(bp.name, bp.defaultTarget, this));
        }

        public override bool HasModified()
        {
            if (base.HasModified())
                return true;

            foreach (TextureImportPlatformSettings ps in m_PlatformSettings)
            {
                if (ps.HasChanged())
                    return true;
            }

            return false;
        }

        public static void SelectMainAssets(Object[] targets)
        {
            ArrayList newSelection = new ArrayList();
            foreach (AssetImporter importer in targets)
            {
                Texture tex = AssetDatabase.LoadMainAssetAtPath(importer.assetPath) as Texture;
                if (tex)
                    newSelection.Add(tex);
            }
            // The selection can be empty if for some reason the asset import failed. In this case, we don't want to cancel out the original selection so that user can correct its settings.
            if (newSelection.Count > 0)
                Selection.objects = newSelection.ToArray(typeof(Object)) as Object[];
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            CacheSerializedProperties();

            BuildTargetList();
            System.Diagnostics.Debug.Assert(!HasModified(), "TextureImporter settings are marked as modified after calling Reset.");
            ApplySettingsToTexture();

            // since some texture types (like Cubemaps) might add/remove new assets during import
            //  and main asset of these textures might change,
            // update selection to include main assets (case 561340)
            SelectMainAssets(targets);
        }

        protected override void Apply()
        {
            // Get SpriteEditorWindow to apply changed property
            SpriteEditorWindow.TextureImporterApply(serializedObject);
            base.Apply();
            SyncPlatformSettings();
            foreach (TextureImportPlatformSettings ps in m_PlatformSettings)
                ps.Apply();
        }
    }
}
