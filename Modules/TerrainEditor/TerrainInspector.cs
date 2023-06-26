// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/*
GUILayout.TextureGrid number of horiz elements doesn't work
*/

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEngine.TerrainTools;
using UnityEditor.TerrainTools;
using UnityEditor.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    // must match Terrain.cpp TerrainTools
    // NOTE: TerrainTool does not seem like an appropriate name for this enum. This dictates what category is selected in
    // the terrain inspector. A more appropriate name for this may be TerrainCategory or ToolCategory.
    internal enum TerrainTool
    {
        None = -1,
        CreateNeighbor = 0,
        Paint = 1,
        PlaceTree,
        PaintDetail,
        TerrainSettings,
        TerrainToolCount
    }

    namespace TerrainTools
    {
        [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
        public class TerrainToolShortcutContext : IShortcutToolContext
        {
            internal TerrainToolShortcutContext(TerrainInspector editor)
            {
                terrainEditor = editor;
            }

            bool IShortcutToolContext.active
            {
                get { return !(TerrainInspector.s_activeTerrainInspector != 0 && TerrainInspector.s_activeTerrainInspector != terrainEditor.GetInstanceID()); }
            }

            public void SelectPaintTool<T>() where T : TerrainPaintTool<T>
            {
                terrainEditor.SelectToolByType(typeof(T));
            }

            public void SelectPaintToolWithOverlays<T>() where T : TerrainPaintToolWithOverlays<T>
            {
                terrainEditor.SelectToolByType(typeof(T));
            }

            internal TerrainInspector terrainEditor { get; }
        }

        [MovedFrom("UnityEditor.Experimental.TerrainAPI")]
        public static class TerrainInspectorUtility
        {
            public static void TerrainShaderValidationGUI(Material material)
            {
                if (material == null)
                    return;

                bool isShaderValid;
                bool.TryParse(material.GetTag("TerrainCompatible", false), out isShaderValid);
                RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
                string shaderPath = renderPipeline?.defaultTerrainMaterial?.shader.name;
                string pipelineShaderTag = material.GetTag("RenderPipeline", false);
                switch (renderPipeline?.GetType().Name)
                {
                    case "HDRenderPipelineAsset":
                        isShaderValid = pipelineShaderTag.Equals("HDRenderPipeline") && isShaderValid;
                        break;
                    case "UniversalRenderPipelineAsset":
                        isShaderValid = pipelineShaderTag.Equals("UniversalPipeline") && isShaderValid;
                        break;
                    case null: // Default legacy render pipeline
                        shaderPath = "a shader from Nature/Terrain";
                        isShaderValid = pipelineShaderTag.Equals("") && isShaderValid;
                        break;
                    default: // Custom SRP doesn't require a warning
                        return;
                }

                if (!isShaderValid)
                {
                    EditorGUILayout.HelpBox($"The provided Material's shader might be unsuitable for use with Terrain in the active render pipeline. We recommend you use {shaderPath} instead.\n\n" +
                        $"If this isn't the case, add the \"TerrainCompatible\" = \"True\" tag in your shader's property block to suppress this warning.", MessageType.Warning, false);
                }
                else if (ShaderUtil.HasTangentChannel(material.shader))
                {
                    EditorGUILayout.HelpBox($"The selected Material's shader uses tangent geometry, which Terrain doesn't support. We recommend you use {shaderPath} instead.", MessageType.Warning, false);
                }
            }

            internal static float ScaledSliderWithRounding(GUIContent content, float valueInPercent, float minVal, float maxVal, float scale, float precision)
            {
                EditorGUI.BeginChangeCheck();

                minVal *= scale;
                maxVal *= scale;
                float v = Mathf.Round(valueInPercent * scale / precision) * precision;
                v = Mathf.Clamp(v, minVal, maxVal);   // this keeps the slider knob from disappearing
                v = EditorGUILayout.Slider(content, v, minVal, maxVal);

                if (EditorGUI.EndChangeCheck())
                {
                    return v / scale;
                }
                return valueInPercent;
            }

            internal static float PowerSlider(GUIContent content, float value, float minVal, float maxVal, float power, GUILayoutOption[] options = null)
                => EditorGUILayout.PowerSlider(content, Mathf.Clamp(value, minVal, maxVal), minVal, maxVal, power, options);
        }
    }

    [CustomEditor(typeof(Terrain))]
    internal class TerrainInspector : Editor
    {
        internal class Styles
        {
            public GUIStyle gridListText = "GridListText";
            public GUIStyle largeSquare = new GUIStyle("Button")
            {
                fixedHeight = 22
            };
            public GUIStyle command = "Command";
            public Texture settingsIcon = EditorGUIUtility.IconContent("SettingsIcon").image;

            // List of tools supported by the editor
            public readonly GUIContent[] toolIcons =
            {
                EditorGUIUtility.TrIconContent("TerrainInspector.TerrainToolAdd", "Create Neighbor Terrains"),
                EditorGUIUtility.TrIconContent("TerrainInspector.TerrainToolSplat", "Paint Terrain"),
                EditorGUIUtility.TrIconContent("TerrainInspector.TerrainToolTrees", "Paint Trees"),
                EditorGUIUtility.TrIconContent("TerrainInspector.TerrainToolPlants", "Paint Details"),
                EditorGUIUtility.TrIconContent("TerrainInspector.TerrainToolSettings", "Terrain Settings")
            };

            public readonly GUIContent[] toolNames =
            {
                EditorGUIUtility.TrTextContent("Create Neighbor Terrains", "Click the edges to create neighbor terrains"),
                EditorGUIUtility.TrTextContent("Paint Terrain", "Select a tool from the drop-down list"),
                EditorGUIUtility.TrTextContent("Paint Trees", "Click to paint trees.\n\nHold shift and click to erase trees.\n\nHold Ctrl and click to erase only trees of the selected type."),
                EditorGUIUtility.TrTextContent("Paint Details", "Click to paint details.\n\nHold shift and click to erase details.\n\nHold Ctrl and click to erase only details of the selected type."),
                EditorGUIUtility.TrTextContent("Terrain Settings")
            };

            public readonly GUIContent brushSize = EditorGUIUtility.TrTextContent("Brush Size", "Size of the brush used to paint.");
            public readonly GUIContent opacity = EditorGUIUtility.TrTextContent("Opacity", "Strength of the applied effect.");
            public readonly GUIContent settings = EditorGUIUtility.TrTextContent("Settings");
            public readonly GUIContent mismatchedTerrainData = EditorGUIUtility.TextContentWithIcon(
                "The TerrainData used by the TerrainCollider component is different from this terrain. Would you like to assign the same TerrainData to the TerrainCollider component?",
                "console.warnicon");

            public readonly GUIContent assign = EditorGUIUtility.TrTextContent("Assign");
            public readonly GUIContent duplicateTab = EditorGUIUtility.TrTextContent("This inspector tab is not the active Terrain inspector, paint functionality disabled.");
            public readonly GUIContent makeMeActive = EditorGUIUtility.TrTextContent("Activate this inspector");
 
            // Heightmaps
            public readonly GUIContent textures = EditorGUIUtility.TrTextContent("Texture Resolutions (On Terrain Data)");
            public readonly GUIContent requireResampling = EditorGUIUtility.TrTextContent("Require resampling on change");
            public readonly GUIContent importRaw  = EditorGUIUtility.TrTextContent("Import Raw...", "The Import Raw button allows you to set the terrain's heightmap from an image file in the RAW grayscale format. RAW format can be generated by third party terrain editing tools (such as Bryce) and can also be opened, edited and saved by Photoshop. This allows for sophisticated generation and editing of terrains outside Unity.");
            public readonly GUIContent exportRaw = EditorGUIUtility.TrTextContent("Export Raw...", "The Export Raw button allows you to save the terrain's heightmap to an image file in the RAW grayscale format. RAW format can be generated by third party terrain editing tools (such as Bryce) and can also be opened, edited and saved by Photoshop. This allows for sophisticated generation and editing of terrains outside Unity.");

            public readonly GUIContent bakeLightProbesForTrees = EditorGUIUtility.TrTextContent("Bake Light Probes For Trees", "If the option is enabled, Unity will create internal light probes at the position of each tree (these probes are internal and will not affect other renderers in the scene) and apply them to tree renderers for lighting. Otherwise trees are still affected by LightProbeGroups. The option is only effective for trees that have LightProbe enabled on their prototype prefab.");
            public readonly GUIContent deringLightProbesForTrees = EditorGUIUtility.TrTextContent("Remove Light Probe Ringing", "When enabled, removes visible overshooting often observed as ringing on objects affected by intense lighting at the expense of reduced contrast.");
            public readonly GUIContent refresh = EditorGUIUtility.TrTextContent("Refresh", "When you save a tree asset from the modelling app, you will need to click the Refresh button (shown in the inspector when the tree painting tool is selected) in order to see the updated trees on your terrain.");

            // Settings
            public readonly GUIContent basicTerrain = EditorGUIUtility.TrTextContent("Basic Terrain");
            public readonly GUIContent groupingID = EditorGUIUtility.TrTextContent("Grouping ID", "Grouping ID for auto connection");
            public readonly GUIContent allowAutoConnect = EditorGUIUtility.TrTextContent("Auto Connect", "Allow the current terrain tile to automatically connect to neighboring tiles sharing the same grouping ID.");
            public readonly GUIContent attemptReconnect = EditorGUIUtility.TrTextContent("Reconnect", "Will attempt to re-run auto connection");
            public readonly GUIContent drawTerrain = EditorGUIUtility.TrTextContent("Draw", "Toggle the rendering of terrain");
            public readonly GUIContent enableHeightmapRayTracing = EditorGUIUtility.TrTextContent("Enable Ray Tracing Support", "When enabling this option, RayTracingAccelerationStructure.CullInstances function will populate the acceleration structure with terrain geometries.");
            public readonly GUIContent drawInstancedTerrain = EditorGUIUtility.TrTextContent("Draw Instanced" , "Toggle terrain instancing rendering");
            public readonly GUIContent qualitySettings = EditorGUIUtility.TrTextContent("Quality Settings");
            public readonly GUIContent ignoreQualitySettings = EditorGUIUtility.TrTextContent("Ignore Quality Settings", "Toggle whether this terrain should ignore the current active quality settings' terrain overrides.");
            public readonly GUIContent pixelError = EditorGUIUtility.TrTextContent("Pixel Error", "The accuracy of the mapping between the terrain maps (heightmap, textures, etc.) and the generated terrain; higher values indicate lower accuracy but lower rendering overhead.");
            public readonly GUIContent heightmapMinLODLimit = EditorGUIUtility.TrTextContent("Minimum Detail Limit", "Enforces a minimum level of detail on the Heightmap.\n0 enforces no limit - higher values enforce progressively more detail.\nMost users will not need this setting - see documentation.");
            public readonly GUIContent heightmapMaxLODLimit = EditorGUIUtility.TrTextContent("Maximum Complexity Limit", "Enforces a maximum level of detail on the Heightmap.\n0 enforces no limit - higher values enforce progressively more detail.\nThis can be useful to limit detail on low-end devices.");
            public readonly GUIContent baseMapDist = EditorGUIUtility.TrTextContent("Base Map Dist.", "The maximum distance at which terrain textures will be displayed at full resolution. Beyond this distance, a lower resolution composite image will be used for efficiency.");
            public readonly GUIContent castShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "Does the terrain cast shadows?");
            public readonly GUIContent createMaterial = EditorGUIUtility.TrTextContent("Create...", "Create a new Material asset to be used by the terrain by duplicating the current default Terrain material.");
            public readonly GUIContent reflectionProbes = EditorGUIUtility.TrTextContent("Reflection Probes", "How reflection probes are used on terrain. Only effective when using built-in standard material or a custom material which supports rendering with reflection.");
            public readonly GUIContent preserveTreePrototypeLayers = EditorGUIUtility.TextContent("Preserve Tree Prototype Layers|Enable this option if you want your tree instances to take on the layer values of their prototype prefabs, rather than the terrain GameObject's layer.");
            public readonly GUIContent treeAndDetails = EditorGUIUtility.TrTextContent("Tree & Detail Objects");
            public readonly GUIContent drawTrees = EditorGUIUtility.TrTextContent("Draw", "Should trees, grass and details be drawn?");
            public readonly GUIContent treeMotionVectorOverrideMode = EditorGUIUtility.TrTextContent("Tree Motion Vectors", "Select the motion vector mode to be used for all of the trees painted on the terrain. 'Inherit From Prototype' will use the value from the tree prototype's mesh renderer.");
            public readonly GUIContent detailObjectDistance = EditorGUIUtility.TrTextContent("Detail Distance", "The distance (from camera) beyond which details will be culled.");
            public readonly GUIContent detailObjectDensity = EditorGUIUtility.TrTextContent("Detail Density Scale", "Scaling factor applied to the density of all details. Only affects details with the \"Affected by Density Scale\" option enabled.");
            public readonly GUIContent detailScatterMode = EditorGUIUtility.TrTextContent("Detail Scatter Mode", "The scatter mode type to be used while painting details. Coverage paints areas detail should be populated in based on their density setting, Instance Count paints the amount per sample.");
            public readonly GUIContent treeDistance = EditorGUIUtility.TrTextContent("Tree Distance", "The distance (from camera) beyond which trees will be culled. For SpeedTree trees this parameter is controlled by the LOD group settings.");
            public readonly GUIContent treeBillboardDistance = EditorGUIUtility.TrTextContent("Billboard Start", "The distance (from camera) at which 3D tree objects will be replaced by billboard images. For SpeedTree trees this parameter is controlled by the LOD group settings.");
            public readonly GUIContent treeCrossFadeLength = EditorGUIUtility.TrTextContent("Fade Length", "Distance over which trees will transition between 3D objects and billboards. For SpeedTree trees this parameter is controlled by the LOD group settings.");
            public readonly GUIContent treeMaximumFullLODCount = EditorGUIUtility.TrTextContent("Max Mesh Trees", "The maximum number of visible trees that will be represented as solid 3D meshes. Beyond this limit, trees will be replaced with billboards. For SpeedTree trees this parameter is controlled by the LOD group settings.");
            public readonly GUIContent grassWindSettings = EditorGUIUtility.TrTextContent("Wind Settings for Grass (On Terrain Data)");
            public readonly GUIContent wavingGrassStrength = EditorGUIUtility.TrTextContent("Speed", "The speed of the wind as it blows grass.");
            public readonly GUIContent wavingGrassSpeed = EditorGUIUtility.TrTextContent("Size", "The size of the 'ripples' on grassy areas as the wind blows over them.");
            public readonly GUIContent wavingGrassAmount = EditorGUIUtility.TrTextContent("Bending", "The degree to which grass objects are bent over by the wind.");
            public readonly GUIContent wavingGrassTint = EditorGUIUtility.TrTextContent("Grass Tint", "Overall color tint applied to grass objects.");
            public readonly GUIContent meshResolution = EditorGUIUtility.TrTextContent("Mesh Resolution (On Terrain Data)");
            public readonly GUIContent holesSettings = EditorGUIUtility.TrTextContent("Holes Settings (On Terrain Data)");
            public readonly GUIContent holesCompressionToggle = EditorGUIUtility.TrTextContent("Compress Holes Texture", "If enabled, holes texture will be compressed at runtime if compression supported.");

            public static readonly GUIContent renderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");

            public static readonly GUIContent heightmapResolution = EditorGUIUtility.TrTextContent("Heightmap Resolution", "Pixel resolution of the terrain's heightmap (should be a power of two plus one, eg, 513 = 512 + 1)");
            public static readonly GUIContent[] heightmapResolutionStrings =
            {
                EditorGUIUtility.TrTextContent("33 x 33", "Pixels"),
                EditorGUIUtility.TrTextContent("65 x 65", "Pixels"),
                EditorGUIUtility.TrTextContent("129 x 129", "Pixels"),
                EditorGUIUtility.TrTextContent("257 x 257", "Pixels"),
                EditorGUIUtility.TrTextContent("513 x 513", "Pixels"),
                EditorGUIUtility.TrTextContent("1025 x 1025", "Pixels"),
                EditorGUIUtility.TrTextContent("2049 x 2049", "Pixels"),
                EditorGUIUtility.TrTextContent("4097 x 4097", "Pixels")
            };
            public static readonly int[] heightmapResolutionInts =
            {
                33,
                65,
                129,
                257,
                513,
                1025,
                2049,
                4097
            };

            public static readonly GUIContent alphamapResolution = EditorGUIUtility.TrTextContent("Control Texture Resolution", "Resolution of the \"splatmap\" that controls the blending of the different terrain materials.");
            public static readonly GUIContent[] alphamapResolutionStrings =
            {
                EditorGUIUtility.TrTextContent("16 x 16", "Pixels"),
                EditorGUIUtility.TrTextContent("32 x 32", "Pixels"),
                EditorGUIUtility.TrTextContent("64 x 64", "Pixels"),
                EditorGUIUtility.TrTextContent("128 x 128", "Pixels"),
                EditorGUIUtility.TrTextContent("256 x 256", "Pixels"),
                EditorGUIUtility.TrTextContent("512 x 512", "Pixels"),
                EditorGUIUtility.TrTextContent("1024 x 1024", "Pixels"),
                EditorGUIUtility.TrTextContent("2048 x 2048", "Pixels"),
                EditorGUIUtility.TrTextContent("4096 x 4096", "Pixels")
            };
            public static readonly int[] alphamapResolutionInts =
            {
                16,
                32,
                64,
                128,
                256,
                512,
                1024,
                2048,
                4096
            };

            public static readonly GUIContent basemapResolution = EditorGUIUtility.TrTextContent("Base Texture Resolution", "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance.");
            public static readonly GUIContent[] basemapResolutionStrings =
            {
                EditorGUIUtility.TrTextContent("16 x 16", "Pixels"),
                EditorGUIUtility.TrTextContent("32 x 32", "Pixels"),
                EditorGUIUtility.TrTextContent("64 x 64", "Pixels"),
                EditorGUIUtility.TrTextContent("128 x 128", "Pixels"),
                EditorGUIUtility.TrTextContent("256 x 256", "Pixels"),
                EditorGUIUtility.TrTextContent("512 x 512", "Pixels"),
                EditorGUIUtility.TrTextContent("1024 x 1024", "Pixels"),
                EditorGUIUtility.TrTextContent("2048 x 2048", "Pixels"),
                EditorGUIUtility.TrTextContent("4096 x 4096", "Pixels")
            };
            public static readonly int[] basemapResolutionInts =
            {
                16,
                32,
                64,
                128,
                256,
                512,
                1024,
                2048,
                4096
            };
        }
        internal static Styles styles;

        // Source terrain
        Terrain m_Terrain;
        TerrainCollider m_TerrainCollider;

        internal float brushStrength
        {
            get;
            set;
        }

        public float brushSize
        {
            get; set;
        }

        // storing the previous values to update the BrushAttributesOverlay
        internal float prevBrushSize
        {
            get;
            set;
        }

        internal float prevBrushStrength
        {
            get;
            set;
        }

        private static readonly float kOldMinBrushSize = 0.1f;
        float kOldMaxBrushSize
        {
            get
            {
                float safetyFactorHack = 0.9375f;
                return Mathf.Floor(Mathf.Min(m_Terrain.terrainData.size.x, m_Terrain.terrainData.size.z) * safetyFactorHack);
            }
        }

        int lastTextureResolutionPerTile = 0;
        internal void GetBrushSizeLimits(
            out float minBrushSize, out float maxBrushSize,
            int textureResolutionPerTile = 0)
        {
            if (textureResolutionPerTile == 0)
                textureResolutionPerTile = lastTextureResolutionPerTile;
            else
                lastTextureResolutionPerTile = textureResolutionPerTile;

            if (textureResolutionPerTile <= 0)
            {
                minBrushSize = kOldMinBrushSize;
                maxBrushSize = kOldMaxBrushSize;
            }
            else
            {
                float terrainSize = 1.0f;
                if (m_Terrain && m_Terrain.terrainData)
                {
                    terrainSize = Mathf.Min(
                        m_Terrain.terrainData.size.x,
                        m_Terrain.terrainData.size.z);
                }

                TerrainPaintUtility.GetBrushWorldSizeLimits(
                    out minBrushSize, out maxBrushSize,
                    terrainSize,
                    textureResolutionPerTile);
            }
        }

        // hot key state
        float m_GrowBrushTime = 0.0f;
        float m_ShrinkBrushTime = 0.0f;
        bool m_GrowShrinkSlow = false;

        float m_IncreaseOpacityTime = 0.0f;
        float m_DecreaseOpacityTime = 0.0f;
        bool m_OpacitySlow = false;

        const float kHeightmapBrushScale = 0.01F;
        const float kMinBrushStrength = (1.1F / ushort.MaxValue) / kHeightmapBrushScale;
        const float kMaxBrushStrength = 1.0f;

        BrushList m_BrushList;

        internal BrushList brushList {  get { if (m_BrushList == null) { m_BrushList = new BrushList(); } return m_BrushList; } }


        internal int m_ActivePaintToolIndex = 0;
        static internal string[] m_PaintToolNames = null;
        static internal Dictionary<string, ITerrainPaintTool> m_ToolsMap;
        static internal Dictionary<string, Type> m_ToolNameToType = null;
        static internal Dictionary<Type, string> m_TypeToToolName = null;
        static OnPaintContext onPaintEditContext = new OnPaintContext(new RaycastHit(), null, Vector2.zero, 0.0f, 0.0f);
        static OnInspectorGUIContext onInspectorGUIEditContext = new OnInspectorGUIContext();
        static OnSceneGUIContext onSceneGUIEditContext = new OnSceneGUIContext(null, new RaycastHit(), null, 0.0f, 0.0f, 0);

        internal static bool s_ActiveTerrainToolIsEditorTool = true;
        private static ITerrainPaintTool m_ActiveTerrainTool = null; // NOT EDITOR TOOL
        public static ITerrainPaintTool GetActiveTerrainTool()
        {
            if (s_ActiveTerrainToolIsEditorTool)
            {
                var tool = EditorToolManager.GetActiveTool();
                if (tool is ITerrainPaintTool) return (ITerrainPaintTool)tool;
            }
            else
            {
                return m_ActiveTerrainTool;
            }

            return null;
        }

        internal static int s_TerrainEditorHash = "TerrainEditor".GetHashCode();

        // The instance ID of the active inspector.
        // It's defined as the last inspector that had one of its terrain tools selected.
        // If a terrain inspector is the only one when created, it also becomes active.
        static internal int s_activeTerrainInspector = 0;
        static internal TerrainInspector s_activeTerrainInspectorInstance = null;

        List<ReflectionProbeBlendInfo> m_BlendInfoList = new List<ReflectionProbeBlendInfo>();

        private AnimBool m_ShowReflectionProbesGUI = new AnimBool();

        bool m_ShowCreateMaterialButton = false;
        bool m_LODTreePrototypePresent = false;

        private RendererLightingSettings m_Lighting;

        private static Terrain s_LastActiveTerrain;

        static void ChangeTool(ShortcutArguments args, Action<TerrainInspector> action)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            TerrainInspector editor = context.terrainEditor;
            editor.lastTextureResolutionPerTile = 0;       // reset texture resolution, new tool may use different resolution
            action(editor);
            EditorApplication.SetSceneRepaintDirty();
            editor.Repaint();
        }

        // legacy method -- leaving in for compatibility but
        // now just forwards call to internal function
        public void SelectPaintTool(Type toolType)
        {
            SelectToolByType(toolType);
        }

        internal void SelectToolByType(Type toolType)
        {
            if(!m_TypeToToolName.ContainsKey(toolType))
            {
                Debug.LogError($"Terrain Tool of type {toolType} does not exist");
                return;
            }

            if (typeof(ITerrainPaintToolWithOverlays).IsAssignableFrom(toolType) && typeof(ITerrainPaintTool).IsAssignableFrom(toolType))
            {
                // case: terrain tool with overlays
                SelectOverlaysTool(m_TypeToToolName[toolType]);
                return;
            }

            // case: else its an old terrain tool
            SelectToolByName(m_TypeToToolName[toolType]);

        }

        internal void SelectToolByName(string toolName)
        {
            // check if old terrain tool or new overlays terrain tool
            if (typeof(ITerrainPaintToolWithOverlays).IsAssignableFrom(m_ToolNameToType[toolName]) &&
                typeof(ITerrainPaintTool).IsAssignableFrom(m_ToolNameToType[toolName]))
            {
                // case: terrain tool with overlays
                SelectOverlaysTool(toolName);
                return;
            }

            // else its an old terrain tool
            var instanceProperty = m_ToolNameToType[toolName].GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var mi = instanceProperty.GetGetMethod();
            ITerrainPaintTool tool = (ITerrainPaintTool)mi.Invoke(null, null); // create the tool

            Debug.Assert(!string.IsNullOrEmpty(toolName) && m_ToolNameToType.ContainsKey(toolName),
                $"Cannot select tool with invalid tool name: {toolName}. Make sure the tool name is correct and that the tool is loaded.");

            SetCurrentPaintToolInactive();
            selectedCategory = GetCategory(toolName);
            lastTextureResolutionPerTile = 0;
            if (IsPaintTool(toolName))
            {
                m_ActivePaintToolIndex = GetPaintToolIndex(toolName);
            }
            SetCurrentPaintToolActive(tool);
            Repaint();
        }

        internal void SelectOverlaysTool(string toolName)
        {
            // can select tool using GetSingleton bc its an editor tool (this function is not called for old terrain tools)
            var tool = (ITerrainPaintTool)EditorToolManager.GetSingleton(m_ToolNameToType[toolName]);
            Debug.Assert(!string.IsNullOrEmpty(toolName) && m_ToolNameToType.ContainsKey(toolName),
                $"Cannot select tool with invalid tool name: {toolName}. Make sure the tool name is correct and that the tool is loaded.");
            selectedCategory = GetCategory(toolName);
            lastTextureResolutionPerTile = 0;
            SetCurrentPaintToolInactive();
            SetCurrentPaintToolActive(tool);
            if (IsPaintTool(toolName))
            {
                m_ActivePaintToolIndex = GetPaintToolIndex(toolName);
            }
            Repaint();
        }

        internal int GetPaintToolIndex(string toolName)
        {
            for (int index = 0; index < m_PaintToolNames.Length; index++)
            {
                if (m_PaintToolNames[index] == toolName)
                {
                    // found it!
                    return index;
                }
            }
            Debug.LogError("GetPaintToolIndex: Cannot find tool '" + toolName + "'");
            return -1;
        }


        [FormerlyPrefKeyAs("Terrain/Tree Brush", "f5")]
        [Shortcut("Terrain/Tree Brush", typeof(TerrainToolShortcutContext), KeyCode.F5)]
        static void SelectPlaceTreeTool(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.selectedCategory = TerrainTool.PlaceTree);
        }

        [FormerlyPrefKeyAs("Terrain/Detail Brush", "f6")]
        [Shortcut("Terrain/Detail Brush", typeof(TerrainToolShortcutContext), KeyCode.F6)]
        static void SelectPaintDetailTool(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.selectedCategory = TerrainTool.PaintDetail);
        }

        [FormerlyPrefKeyAs("Terrain/Previous Brush", ",")]
        [Shortcut("Terrain/Previous Brush", typeof(TerrainToolShortcutContext), KeyCode.Comma)]
        static void PreviousBrush(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.brushList.SelectPrevBrush());
        }

        [FormerlyPrefKeyAs("Terrain/Next Brush", ".")]
        [Shortcut("Terrain/Next Brush", typeof(TerrainToolShortcutContext), KeyCode.Period)]
        static void NextBrush(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.brushList.SelectNextBrush());
        }

        [FormerlyPrefKeyAs("Terrain/Previous Detail", "#,")]
        [Shortcut("Terrain/Previous Detail", typeof(TerrainToolShortcutContext), KeyCode.Comma, ShortcutModifiers.Shift)]
        static void PreviousDetail(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.DetailDelta(-1));
        }

        [FormerlyPrefKeyAs("Terrain/Next Detail", "#.")]
        [Shortcut("Terrain/Next Detail", typeof(TerrainToolShortcutContext), KeyCode.Period, ShortcutModifiers.Shift)]
        static void NextDetail(ShortcutArguments args)
        {
            ChangeTool(args, editor => editor.DetailDelta(1));
        }

        static void ApplyClutchKeys(ShortcutArguments args, TerrainInspector editor, ref float forwardTime, ref float backwardTime, ref bool slow)
        {
            if (args.stage == ShortcutStage.Begin)
            {
                // if the backward-key is already pressed -- we keeps that direction but slow it
                if (backwardTime > 0.0f)
                {
                    slow = true;
                }
                else
                {
                    // go forward (start timer), full speed
                    forwardTime = Mathf.Epsilon;
                    slow = false;

                    // zero out the last time so we don't get a large first delta hiccup
                    editor.m_LastHotkeyApplyTime = EditorApplication.timeSinceStartup;
                }
            }
            else
            {
                // ShortcutStage.End -- turn off grow
                forwardTime = 0.0f;
                // if backwards is still pressed, disable slow, but reset the timer (restart acceleration)
                if (backwardTime > 0.0f)
                {
                    slow = false;
                    backwardTime = Mathf.Epsilon;
                }
            }

            // make sure we repaint when we start or stop
            EditorApplication.SetSceneRepaintDirty();
            editor.Repaint();
        }

        [FormerlyPrefKeyAs("Terrain/Grow Brush Size", "RightBracket")]
        [ClutchShortcut("Terrain/Grow Brush Size", typeof(TerrainToolShortcutContext), KeyCode.RightBracket)]
        static void GrowBrushSize(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            TerrainInspector editor = context.terrainEditor;
            ApplyClutchKeys(args, editor, ref editor.m_GrowBrushTime, ref editor.m_ShrinkBrushTime, ref editor.m_GrowShrinkSlow);
        }

        [FormerlyPrefKeyAs("Terrain/Shrink Brush Size", "LeftBracket")]
        [ClutchShortcut("Terrain/Shrink Brush Size", typeof(TerrainToolShortcutContext), KeyCode.LeftBracket)]
        static void ShrinkBrushSize(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            TerrainInspector editor = context.terrainEditor;
            ApplyClutchKeys(args, editor, ref editor.m_ShrinkBrushTime, ref editor.m_GrowBrushTime, ref editor.m_GrowShrinkSlow);
        }

        [FormerlyPrefKeyAs("Terrain/Increase Brush Opacity", "Plus")]
        [ClutchShortcut("Terrain/Increase Brush Opacity", typeof(TerrainToolShortcutContext), KeyCode.Equals)]
        static void IncreaseBrushOpacity(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            TerrainInspector editor = context.terrainEditor;
            ApplyClutchKeys(args, editor, ref editor.m_IncreaseOpacityTime, ref editor.m_DecreaseOpacityTime, ref editor.m_OpacitySlow);
        }

        [FormerlyPrefKeyAs("Terrain/Decrease Brush Opacity", "Minus")]
        [ClutchShortcut("Terrain/Decrease Brush Opacity", typeof(TerrainToolShortcutContext), KeyCode.Minus)]
        static void DecreaseBrushOpacity(ShortcutArguments args)
        {
            TerrainToolShortcutContext context = (TerrainToolShortcutContext)args.context;
            TerrainInspector editor = context.terrainEditor;
            ApplyClutchKeys(args, editor, ref editor.m_DecreaseOpacityTime, ref editor.m_IncreaseOpacityTime, ref editor.m_OpacitySlow);
        }

        float SmoothMax(float x, float max)
        {
            return (1.0f - 1.0f / (x / max + 1.0f)) * max;
        }

        float SmoothMaxBlended(float x, float max, float hardness)
        {
            // approximates Mathf.Max(x, max) -- see https://www.desmos.com/calculator/zdwwpe7xwu
            float smoothPoint = max * hardness;
            return Mathf.Min(x, smoothPoint) + SmoothMax(Mathf.Max(x - smoothPoint, 0.0f), max - smoothPoint);
        }

        void BrushSizeHotkeyManipulation(ref float hotkeyTime, bool shrink, float deltaTime, float distance, bool slow)
        {
            if ((hotkeyTime > 0.0f) && (distance > 0.0f))
            {
                const float k_DistanceBias = 5.0f;
                const float k_AdditivePerSecond = 1.0f;
                const float k_SlowDistanceScale = 0.02f;
                const float k_TargetDistanceScale = 2.0f;

                // smooth clamped acceleration
                float blend = SmoothMaxBlended(hotkeyTime, 1.0f, 0.5f);
                float distanceScale = Mathf.Lerp(k_SlowDistanceScale, k_TargetDistanceScale, blend);
                if (slow)
                    distanceScale = Mathf.Min(distanceScale, k_SlowDistanceScale);

                // Sqrt behaves great at large distances (speeds up adjust speed), but gets super slow close up
                // so we offset by a small bias to keep it in a usable range
                float changePercentPerSecond = distanceScale * Mathf.Sqrt(distance + k_DistanceBias);

                // apply both percentage based and additive adjustments
                // percentage increases adjust speed at larger sizes
                // additive keeps it from getting too slow at small sizes
                float changePercent = Mathf.Pow(1.0f + changePercentPerSecond, deltaTime);
                float changeAdditive = k_AdditivePerSecond * deltaTime * changePercentPerSecond;

                // apply scale and offset, in the desired direction
                brushSize = shrink ? (brushSize - changeAdditive) / changePercent : brushSize * changePercent + changeAdditive;

                // clamp to range
                float minBrushSize, maxBrushSize;
                GetBrushSizeLimits(out minBrushSize, out maxBrushSize);
                brushSize = Mathf.Clamp(brushSize, minBrushSize, maxBrushSize);

                hotkeyTime += deltaTime;
            }
        }

        void BrushOpacityHotkeyManipulation(ref float hotkeyTime, bool shrink, float deltaTime, bool slow)
        {
            if (hotkeyTime > 0.0f)
            {
                const float k_SlowOpacitySpeed = 0.05f;
                const float k_TargetOpacitySpeed = 1.0f;

                // smooth clamped acceleration
                float blend = SmoothMaxBlended(hotkeyTime, 1.0f, 0.5f);
                float changeAmount = Mathf.Lerp(k_SlowOpacitySpeed, k_TargetOpacitySpeed, blend);
                if (slow)
                    changeAmount = Mathf.Min(changeAmount, k_SlowOpacitySpeed);

                changeAmount *= deltaTime;

                brushStrength = shrink ? brushStrength - changeAmount : brushStrength + changeAmount;
                brushStrength = Mathf.Clamp(brushStrength, kMinBrushStrength, kMaxBrushStrength);

                hotkeyTime += deltaTime;
            }
        }

        void ForceRepaintOnHotkeys()
        {
            if ((m_GrowBrushTime > 0.0f) ||
                (m_ShrinkBrushTime > 0.0f) ||
                (m_IncreaseOpacityTime > 0.0f) ||
                (m_DecreaseOpacityTime > 0.0f))
            {
                Repaint();
                EditorApplication.SetSceneRepaintDirty();
            }
        }

        double m_LastHotkeyApplyTime = 0.0;
        internal void HotkeyApply(float distance)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - m_LastHotkeyApplyTime);
            m_LastHotkeyApplyTime = currentTime;

            BrushSizeHotkeyManipulation(ref m_GrowBrushTime, false, deltaTime, distance, m_GrowShrinkSlow);
            BrushSizeHotkeyManipulation(ref m_ShrinkBrushTime, true, deltaTime, distance, m_GrowShrinkSlow);
            BrushOpacityHotkeyManipulation(ref m_IncreaseOpacityTime, false, deltaTime, m_OpacitySlow);
            BrushOpacityHotkeyManipulation(ref m_DecreaseOpacityTime, true, deltaTime, m_OpacitySlow);
        }

        void DetailDelta(int delta)
        {
            if (delta != 0)
            {
                switch (selectedCategory)
                {
                    case TerrainTool.PaintDetail:
                        var detailsTool = (PaintDetailsTool)((ITerrainPaintTool)EditorToolManager.GetSingleton(typeof(PaintDetailsTool)));
                        detailsTool.selectedDetail = (int)Mathf.Repeat(detailsTool.selectedDetail + delta, m_Terrain.terrainData.detailPrototypes.Length);
                        Event.current.Use();
                        Repaint();
                        break;
                    case TerrainTool.PlaceTree:
                        var protos = m_Terrain.terrainData.treePrototypes;
                        var treesTool = (PaintTreesTool)EditorTools.EditorToolManager.GetSingleton(typeof(PaintTreesTool));
                        if (treesTool.selectedTree >= 0)
                            treesTool.selectedTree = (int)Mathf.Repeat(treesTool.selectedTree + delta, protos.Length);
                        else if (delta == -1 && protos.Length > 0)
                            treesTool.selectedTree = protos.Length - 1;
                        else if (delta == 1 && protos.Length > 0)
                            treesTool.selectedTree = 0;
                        Event.current.Use();
                        Repaint();
                        break;
                }
            }
        }

        internal static bool IsPaintTool(string toolName)
        {
            return toolName != CreateTerrainTool.k_ToolName &&
                   toolName != PaintTreesTool.k_ToolName &&
                   toolName != PaintDetailsTool.k_ToolName;
        }

        static bool IsTerrainPartOfPrefabOrAsset(Terrain terrain)
        {
            return EditorUtility.IsPersistent(terrain) || PrefabUtility.IsPartOfPrefabAsset(terrain);
        }

        public void LoadTools()
        {
            if ((m_ToolNameToType != null && m_TypeToToolName != null && m_ToolsMap != null) || IsTerrainPartOfPrefabOrAsset(target as Terrain)) return;

            m_ToolsMap = new Dictionary<string, ITerrainPaintTool>();
            m_ToolNameToType = new Dictionary<string, Type>();
            m_TypeToToolName = new Dictionary<Type, string>();
            m_PaintToolNames = null;

            var paintToolNames = new List<string>();

            foreach (var klass in TypeCache.GetTypesDerivedFrom(typeof(ITerrainPaintTool)))
            {
                if (klass.IsAbstract) continue;

                ITerrainPaintTool tool = null;
                string toolName = "";
                if (!typeof(ITerrainPaintToolWithOverlays).IsAssignableFrom(klass) && typeof(ITerrainPaintTool).IsAssignableFrom(klass))
                {
                    // case: old terrain tools
                    var instanceProperty = klass.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    var mi = instanceProperty.GetGetMethod();
                    tool = (ITerrainPaintTool)mi.Invoke(null, null); // create the tool
                    toolName = tool.GetName();
                } else if (typeof(ITerrainPaintToolWithOverlays).IsAssignableFrom(klass) && typeof(ITerrainPaintTool).IsAssignableFrom(klass))
                {
                    // case: overlays terrain tools
                    tool = (ITerrainPaintTool)EditorToolManager.GetSingleton(klass);
                    toolName = tool.GetName();
                }

                if (tool == null || toolName == "")
                {
                    Debug.LogWarning("tool is inheriting from neither ITerrainPaintTool nor ITerrainPaintToolWithOverlays");
                }

                // if a tool with the given tool name already exists
                if (m_ToolNameToType.TryGetValue(toolName, out var existingToolType))
                {
                    // if this tool is a builtin tool
                    if (klass.Assembly.GetCustomAttributes(typeof(AssemblyIsEditorAssembly), false).Any())
                        continue;
                    // if existing tool is an override
                    if (existingToolType.Assembly.GetCustomAttributes(typeof(AssemblyIsEditorAssembly), false).Length == 0)
                    {
                        Debug.LogWarning($"A TerrainTool override already exists for ${toolName}. Check to make sure there are not multiple tools that return the same tool name in your project. Skipping tool loading for now.");
                        continue;
                    }
                }

                if(m_ToolsMap.TryGetValue(toolName, out var existingTool))
                {
                    if (klass.Assembly.GetCustomAttributes(typeof(AssemblyIsEditorAssembly), false).Any()) continue;

                    if(existingTool.GetType().Assembly.GetCustomAttributes(typeof(AssemblyIsEditorAssembly), false).Length == 0)
                    {
                        Debug.LogWarning($"A TerrainTools override already exists for {toolName}. Check to make sure there are not multiple tools with the same tool name in your project. This specific tool will not be loaded.");
                        continue;
                    }
                }
                m_ToolsMap[toolName] = tool;
                m_ToolNameToType[toolName] = klass;
                m_TypeToToolName[klass] = toolName;

                // need to track paint category tools for the dropdown selection
                if (IsPaintTool(toolName) && !paintToolNames.Contains(toolName))
                {
                    paintToolNames.Add(toolName);
                }
            }

            m_PaintToolNames = paintToolNames.ToArray();
        }


        void Initialize()
        {
            m_Terrain = target as Terrain;
            ActivateTerrainRenderFlags();
        }


        void LoadInspectorSettings()
        {
            TerrainTool cat = TerrainTool.None;
            var activeTool = GetActiveTerrainTool();

            if (activeTool != null)
            {
                // don't load from editorprefs. just get the tool and category info from the active tool
                var toolName = activeTool.GetName();
                if(IsPaintTool(toolName))
                {
                    m_ActivePaintToolIndex = GetPaintToolIndex(toolName);
                }
                else
                {
                    string activePaintToolName = EditorPrefs.GetString("TerrainActivePaintToolName", PaintHeightTool.k_ToolName);
                    m_ActivePaintToolIndex = GetPaintToolIndex(activePaintToolName);
                }

                cat = GetCategory(toolName);
            }
            else
            {
                // load from editorprefs
                string activePaintToolName = EditorPrefs.GetString("TerrainActivePaintToolName", PaintHeightTool.k_ToolName);
                m_ActivePaintToolIndex = GetPaintToolIndex(activePaintToolName);
                // loading category from EditorPrefs instead of using GetCategory(activePaintToolName)
                // this is because activePaintToolName only stores the name of TerrainCategory.Paint tools
                // if the category is different (tree/details/neighbors) it will be loaded in the SelectCategory(cat) which is called below
                cat = (TerrainTool)EditorPrefs.GetInt("TerrainActiveToolCategory", (int)TerrainTool.Paint);
            }

            if (m_ActivePaintToolIndex < 0)
            {
                // default to paint height tool in case previously active tool no longer exists after domain reload
                m_ActivePaintToolIndex = GetPaintToolIndex(PaintHeightTool.k_ToolName);
            }

            Debug.Assert(IsPaintTool(m_PaintToolNames[m_ActivePaintToolIndex]), "The loaded value for the active paint tool name is not actually a Paint tool: " + m_PaintToolNames[m_ActivePaintToolIndex]);

            SelectCategory(cat); // if cat != paint, then this should help select the correct tool

            brushList.UpdateSelection(EditorPrefs.GetInt("TerrainSelectedBrush", 0));
            brushStrength = EditorPrefs.GetFloat("TerrainBrushStrength", 0.5f);
            brushSize = EditorPrefs.GetFloat("TerrainBrushSize", 25.0f);

            prevBrushStrength = brushStrength;
            prevBrushSize = brushSize;

        }

        void SaveInspectorSettings()
        {
            EditorPrefs.SetInt("TerrainActiveToolCategory", (int)selectedCategory); // we save the category to use in LoadInspectorSettings()
            EditorPrefs.SetInt("TerrainSelectedBrush", brushList.selectedIndex);
            EditorPrefs.SetFloat("TerrainBrushSize", brushSize);
            EditorPrefs.SetFloat("TerrainBrushStrength", brushStrength);
            if (m_PaintToolNames != null) // check prevents null reference exception on domain reload.
            {
                EditorPrefs.SetString("TerrainActivePaintToolName", m_PaintToolNames[m_ActivePaintToolIndex]);
            }
        }

        void SelectCategory(TerrainTool category, bool selectTool = true)
        {
            selectedCategory = category;

            if(selectTool && !IsTerrainPartOfPrefabOrAsset(target as Terrain))
            {
                switch (selectedCategory)
                {
                    case TerrainTool.Paint:
                        Debug.Assert(IsPaintTool(m_PaintToolNames[m_ActivePaintToolIndex]), $"m_ActivePaintToolName ({m_PaintToolNames[m_ActivePaintToolIndex]}) is not a Paint tool");
                        SelectToolByName(m_PaintToolNames[m_ActivePaintToolIndex]);
                        break;
                    case TerrainTool.CreateNeighbor:
                        SelectToolByName(CreateTerrainTool.k_ToolName);
                        break;
                    case TerrainTool.PaintDetail:
                        SelectToolByName(PaintDetailsTool.k_ToolName);
                        break;
                    case TerrainTool.PlaceTree:
                        SelectToolByName(PaintTreesTool.k_ToolName);
                        break;
                    case TerrainTool.TerrainSettings:
                        if (GetActiveTerrainTool() != null)
                        {
                            ToolManager.RestorePreviousPersistentTool();
                        }
                        break;
                    case TerrainTool.None:
                        break;
                    default:
                        break;
                }
            }

            Repaint();
        }
        static bool ShouldShowCreateMaterialButton(Material material, UnityEngine.Object target)
        {
            return (material == null
                || GraphicsSettings.currentRenderPipeline != null && material == GraphicsSettings.currentRenderPipeline.defaultTerrainMaterial
                || material == AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Standard.mat")
                || material == AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Diffuse.mat")
                || material == AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Specular.mat"))
                && !Presets.Preset.IsEditorTargetAPreset(target);
        }

        // this function serves to update m_SelectedCategory when the tool is changed
        // this will update the inspector category when the tool is changed
        void OnActiveToolChanged(EditorTool prev, EditorTool curr)
        {
            if (!IsEditorActive(this)) return;

            var prevIsTerrainTool = prev is ITerrainPaintTool;
            var currIsTerrainTool = curr is ITerrainPaintTool;

            // this can happen if the user has a locked editor window with a terrain inspector
            // that is set to be the active terrain inspector instance but doesn't
            // actually have a terrain object selected
            if (!prevIsTerrainTool && !currIsTerrainTool) return;

            if (prevIsTerrainTool && !currIsTerrainTool)
            {
                if (selectedCategory != TerrainTool.TerrainSettings)
                {
                    SelectCategory(TerrainTool.None, false);
                }
            }
            else if (currIsTerrainTool)
            {
                SelectCategory(GetCategory(((ITerrainPaintTool)curr).GetName()), false);
            }
        }

        public static bool IsEditorActive(TerrainInspector editor)
        {
            return s_activeTerrainInspectorInstance == editor;
        }

        static PropertyEditor GetPropertyEditorFor(TerrainInspector editor)
        {
            var propertyEditors = Resources.FindObjectsOfTypeAll(typeof(PropertyEditor));
            foreach (var p in propertyEditors)
            {
                foreach (var e in ((PropertyEditor)p).tracker.activeEditors)
                {
                    if (e == editor) return ((PropertyEditor)p);
                }
            }

            return null;
        }

        static void SetActiveEditor(TerrainInspector editor)
        {
            if (editor == s_activeTerrainInspectorInstance) return;

            s_activeTerrainInspectorInstance?.SaveInspectorSettings();
            s_activeTerrainInspectorInstance = editor; // set active instance here before loading settings
            s_activeTerrainInspectorInstance?.LoadInspectorSettings();

            // repaint all property editors
            var propertyEditors = Resources.FindObjectsOfTypeAll(typeof(PropertyEditor));
            foreach (var p in propertyEditors)
            {
                ((PropertyEditor)p).Repaint();
            }
        }

        public void OnEnable()
        {
            var terrain = target as Terrain;
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            // if onEnable is called but there is no active editor, that means we are in full screen mode
            bool fullScreenMode = true;
            if (s_activeTerrainInspector == 0 || s_activeTerrainInspectorInstance == null)
            {
                var inspectors = InspectorWindow.GetInspectors();
                foreach (var inspector in inspectors)
                {
                    var editors = inspector.tracker.activeEditors;
                    foreach (var editor in editors)
                    {
                        if (editor == this)
                        {
                            // Acquire active inspector ownership if there is no other inspector active.
                            s_activeTerrainInspector = GetInstanceID();
                            s_activeTerrainInspectorInstance = this;
                            fullScreenMode = false;

                        }
                    }
                }
            }

            // we want to set the active terrain inspector anyways for Overlays
            if (fullScreenMode)
            {
                s_activeTerrainInspector = GetInstanceID();
                s_activeTerrainInspectorInstance = this;
            }

            EditorApplication.update += ForceRepaintOnHotkeys;
            m_ShowReflectionProbesGUI.valueChanged.AddListener(Repaint);
            m_ShowReflectionProbesGUI.value = terrain.reflectionProbeUsage != ReflectionProbeUsage.Off;
            m_ShowCreateMaterialButton = ShouldShowCreateMaterialButton(terrain.materialTemplate, serializedObject.targetObject);

            LoadTools();

            LoadInspectorSettings();

            ActivateTerrainRenderFlags();

            m_Lighting = new RendererLightingSettings(serializedObject);
            m_Lighting.showLightingSettings = new SavedBool($"{target.GetType()}.ShowLightingSettings", true);
            m_Lighting.showLightmapSettings = new SavedBool($"{target.GetType()}.ShowLightmapSettings", true);
            m_Lighting.showBakedLightmap = new SavedBool($"{target.GetType()}.ShowBakedLightmapSettings", false);
            m_Lighting.showRealtimeLightmap = new SavedBool($"{target.GetType()}.ShowRealtimeLightmapSettings", false);

            m_TerrainToolContext = new TerrainToolShortcutContext(this);
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_TerrainToolContext);

            SceneView.duringSceneGui += OnSceneGUICallback;
            Lightmapping.lightingDataUpdated += LightingDataUpdatedRepaint;
            EditorToolManager.activeToolChanged += OnActiveToolChanged;

            s_LastActiveTerrain = terrain;
        }

        public void OnDisable()
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_TerrainToolContext);
            PaintContext.ApplyDelayedActions();
            SceneView.duringSceneGui -= OnSceneGUICallback;
            Lightmapping.lightingDataUpdated -= LightingDataUpdatedRepaint;
            EditorToolManager.activeToolChanged -= OnActiveToolChanged;

            SaveInspectorSettings();
            SetCurrentPaintToolInactive();


            EditorApplication.update -= ForceRepaintOnHotkeys;

            m_ShowReflectionProbesGUI.valueChanged.RemoveListener(Repaint);

            // Return active inspector ownership.
            if (s_activeTerrainInspectorInstance == this)
                s_activeTerrainInspectorInstance = null;
            if (s_activeTerrainInspector == GetInstanceID())
                s_activeTerrainInspector = 0;

            if (s_LastActiveTerrain == this)
                s_LastActiveTerrain = null;
        }

        TerrainToolShortcutContext m_TerrainToolContext;

        TerrainTool m_SelectedCategory;

        private TerrainTool selectedCategory
        {
            get => m_SelectedCategory;

            set
            {
                m_SelectedCategory = value;
                s_activeTerrainInspector = GetInstanceID();
                ActivateTerrainRenderFlags();
            }
        }

        internal TerrainTool GetCategory(string toolName)
        {
            Debug.Assert(!string.IsNullOrEmpty(toolName), "Trying to select TerrainCategory for an empty or null tool name");
            Debug.Assert(m_ToolNameToType.ContainsKey(toolName), "Trying to select a TerrainCategory for a tool name that does not exist");

            if (toolName == PaintTreesTool.k_ToolName)
            {
                return TerrainTool.PlaceTree;
            }
            else if (toolName == PaintDetailsTool.k_ToolName)
            {
                return TerrainTool.PaintDetail;
            }
            else if (toolName == CreateTerrainTool.k_ToolName)
            {
                return TerrainTool.CreateNeighbor;
            }
            else if(IsPaintTool(toolName))
            {
                return TerrainTool.Paint;
            }

            throw new Exception($"No Terrain Tool Category was found for the provided tool name: {toolName}. Make sure the tool name is valid and that the tool is actually loaded.");
        }

        // this function used to be called in CheckToolActivation function
        // now it should be called when the selected tool is changed
        private void ActivateTerrainRenderFlags()
        {
            if (m_Terrain != null)
            {
                // When switching tool reactivate all renderflags
                m_Terrain.editorRenderFlags = TerrainRenderFlags.All;
            }
        }

        // this is a bunch of tracking to ensure we don't mess up the tool mode callbacks
        private void SetCurrentPaintToolActive(ITerrainPaintTool paintTool)
        {
            if (GetActiveTerrainTool() == null)
            {
                if (paintTool != null)
                {
                    paintTool.OnEnterToolMode();

                    // is editor tool?
                    if (paintTool is ITerrainPaintToolWithOverlays)
                    {
                        // deactivate the editor tool
                        ToolManager.SetActiveTool((EditorTool)paintTool);

                        s_ActiveTerrainToolIsEditorTool = true;

                        // set it active in the overlay as well
                        TerrainTransientToolbarOverlay.SetEditorToolActive((ITerrainPaintToolWithOverlays) paintTool);
                    }
                    else
                    {
                        // else it is a regular terrain paint tool that we are activating
                        m_ActiveTerrainTool = paintTool;
                        s_ActiveTerrainToolIsEditorTool = false;

                    }
                }
            }
        }

        private void SetCurrentPaintToolInactive()
        {
            if (GetActiveTerrainTool() != null)
            {
                ITerrainPaintTool paintTool = GetActiveTerrainTool();
                if (paintTool != null)
                {
                    paintTool.OnExitToolMode();

                    // is editor tool?
                    if (paintTool is ITerrainPaintToolWithOverlays)
                    {
                        ToolManager.RestorePreviousPersistentTool();
                        s_ActiveTerrainToolIsEditorTool = false;
                    }
                    else
                    {
                        // else it is a regular terrain paint tool that we are deactivating
                        m_ActiveTerrainTool = null;
                    }
                }
            }

        }

        public static void MenuButton(GUIContent title, string menuName, Terrain terrain, int userData)
        {
            var t = EditorGUIUtility.TempContent(title.text, styles.settingsIcon);
            t.tooltip = title.tooltip;
            Rect r = GUILayoutUtility.GetRect(t, styles.largeSquare);
            if (GUI.Button(r, t, styles.largeSquare))
            {
                MenuCommand context = new MenuCommand(terrain, userData);
                EditorUtility.DisplayPopupMenu(new Rect(r.x, r.y, 0, 0), menuName, context);
            }
        }

        private static Rect GetAspectRect(int elementCount, int approxSize, int extraLineHeight, out int itemsPerRow)
        {
            // account for when currentViewWidth = 0
            float editorWidth = EditorGUIUtility.currentViewWidth > 0f ? EditorGUIUtility.currentViewWidth : 240f;
            itemsPerRow = Mathf.CeilToInt((editorWidth - 20) / approxSize);
            int yCount = (elementCount + itemsPerRow - 1) / itemsPerRow;
            Rect r1 = GUILayoutUtility.GetAspectRect(itemsPerRow / (float)yCount);
            Rect r2 = GUILayoutUtility.GetRect(10, extraLineHeight * yCount);
            r1.height += r2.height;
            return r1;
        }

        public static int AspectSelectionGridImageAndText(int selected, int itemCount, GUI.CustomSelectionGridItemGUI itemGUI, int approxSize, GUIContent emptyString, out bool doubleClick)
        {
            EditorGUILayout.BeginVertical(GUIContent.none, EditorStyles.helpBox, GUILayout.MinHeight(10));
            int retval = 0;

            doubleClick = false;

            if (itemCount > 0)
            {
                int itemsPerRow = 0;
                Rect rect = GetAspectRect(itemCount, approxSize, 12, out itemsPerRow);

                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.clickCount == 2 && rect.Contains(evt.mousePosition))
                {
                    doubleClick = true;
                    evt.Use();
                }
                retval = GUI.DoCustomSelectionGrid(rect, Math.Min(selected, itemCount - 1), itemCount, itemGUI, itemsPerRow, styles.gridListText);
            }
            else
            {
                GUILayout.Label(emptyString);
            }

            GUILayout.EndVertical();
            return retval;
        }

        public static int AspectSelectionGridImageAndText(int selected, GUIContent[] textures, int approxSize, GUIContent emptyString, out bool doubleClick)
        {
            return AspectSelectionGridImageAndText(selected, textures.Length, (i, rect, style, controlID) =>
            {
                if (Event.current.type == EventType.Repaint)
                {
                    bool mouseHover = rect.Contains(Event.current.mousePosition);
                    style.Draw(rect, textures[i], GUI.enabled && mouseHover && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == controlID), GUI.enabled && GUIUtility.hotControl == controlID, i == selected, false);
                    if (mouseHover)
                    {
                        GUIUtility.mouseUsed = true;
                        if (!String.IsNullOrEmpty(textures[i].tooltip))
                            GUIStyle.SetMouseTooltip(textures[i].tooltip, rect);
                    }
                }
            }, approxSize, emptyString, out doubleClick);
        }

        private void ShowOverrideBar()
        {
            var rect = EditorGUILayout.s_LastRect;
            var prevMargin = EditorGUIUtility.leftMarginCoord;
            EditorGUIUtility.leftMarginCoord = 2;
            EditorGUI.DrawOverrideBackgroundApplicable(rect);
            EditorGUIUtility.leftMarginCoord = prevMargin;
        }

        public void ShowDetailStats()
        {
            GUILayout.Space(3);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            int totalPatches = m_Terrain.terrainData.detailPatchCount * m_Terrain.terrainData.detailPatchCount;
            EditorGUILayout.LabelField($"Detail patches currently allocated: {totalPatches}");

            int maxDetails = totalPatches * PaintDetailsToolUtility.GetMaxDetailInstancesPerPatch(m_Terrain.terrainData);
            EditorGUILayout.LabelField($"Detail instance density: {maxDetails}");

            EditorGUI.indentLevel = oldIndent;
            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private bool m_ShowBasicTerrainSettings = true;
        private bool m_ShowTreeAndDetailSettings = true;
        private bool m_ShowGrassWindSettings = true;
        private bool m_ShowTerrainQualitySettings = true;

        private static void MarkDirty(Terrain terrain)
        {
            EditorApplication.SetSceneRepaintDirty();
            EditorUtility.SetDirty(terrain);

            if (!EditorApplication.isPlaying)
                SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        }

        private void MarkTerrainDataDirty()
        {
            // In cases where terrain data is embedded in the scene (i.e. it's not an asset),
            // we need to dirty the scene if terrainData has changed.
            if (!EditorUtility.IsPersistent(m_Terrain.terrainData) && !EditorApplication.isPlaying)
                SceneManagement.EditorSceneManager.MarkSceneDirty(m_Terrain.gameObject.scene);
        }

        private class DoCreateTerrainMaterial : ProjectWindowCallback.EndNameEditAction
        {
            public Terrain terrain;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                if (terrain != null)
                {
                    var obj = EditorUtility.InstanceIDToObject(instanceId) as Material;
                    AssetDatabase.CreateAsset(obj, AssetDatabase.GenerateUniqueAssetPath(pathName));
                    Undo.RecordObject(terrain, "Terrain property change");
                    terrain.materialTemplate = obj;
                    TerrainInspector.MarkDirty(terrain);
                    Selection.activeObject = terrain;
                }
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile)
            {
                Selection.activeObject = terrain;
            }
        }

        private string GetTerrainShaderName()
        {
            RenderPipelineAsset currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string retString = "shaders in Nature/Terrain";
            if (currentRP)
                retString = currentRP.defaultTerrainMaterial.shader.name;
            return retString;
        }

        public void ShowSettings()
        {
            TerrainData terrainData = m_Terrain.terrainData;
            TerrainQualityOverrides overrideFlags = QualitySettings.terrainQualityOverrides;

            m_ShowBasicTerrainSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowBasicTerrainSettings, styles.basicTerrain);

            if (m_ShowBasicTerrainSettings)
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();

                var groupingID = EditorGUILayout.IntField(styles.groupingID, m_Terrain.groupingID);

                EditorGUILayout.BeginHorizontal();
                var allowAutoConnect = EditorGUILayout.Toggle(styles.allowAutoConnect, m_Terrain.allowAutoConnect);
                if (GUILayout.Button(styles.attemptReconnect))
                    Terrain.SetConnectivityDirty();
                EditorGUILayout.EndHorizontal();

                var drawHeightmap = EditorGUILayout.Toggle(styles.drawTerrain, m_Terrain.drawHeightmap);
                var drawInstanced = EditorGUILayout.Toggle(styles.drawInstancedTerrain, m_Terrain.drawInstanced);
                var enableHeightmapRayTracing = EditorGUILayout.Toggle(styles.enableHeightmapRayTracing, m_Terrain.enableHeightmapRayTracing);

                float heightmapPixelError = m_Terrain.heightmapPixelError;
                bool overridePixelError = overrideFlags.HasFlag(TerrainQualityOverrides.PixelError) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overridePixelError))
                {
                    heightmapPixelError = EditorGUILayout.Slider(styles.pixelError, m_Terrain.heightmapPixelError, 1, 200);
                    if (overridePixelError)
                        ShowOverrideBar();
                }

                // 17 is the hard-coded patch size in the engine.
                int numLevels = Mathf.RoundToInt(Mathf.Log(terrainData.heightmapResolution / 17, 2));
                m_Terrain.heightmapMinimumLODSimplification = EditorGUILayout.IntSlider(styles.heightmapMinLODLimit, m_Terrain.heightmapMinimumLODSimplification, 0, numLevels);
                m_Terrain.heightmapMaximumLOD = EditorGUILayout.IntSlider(styles.heightmapMaxLODLimit, m_Terrain.heightmapMaximumLOD, 0, numLevels);

                float basemapDistance = m_Terrain.basemapDistance;
                bool overrideBasemapDistance = overrideFlags.HasFlag(TerrainQualityOverrides.BasemapDistance) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideBasemapDistance))
                {
                    basemapDistance = TerrainInspectorUtility.PowerSlider(styles.baseMapDist, m_Terrain.basemapDistance, 0.0f, 20000.0f, 2.0f);
                    if (overrideBasemapDistance)
                        ShowOverrideBar();
                }

                var shadowCastingMode = (ShadowCastingMode)EditorGUILayout.EnumPopup(styles.castShadows, m_Terrain.shadowCastingMode);

                var reflectionProbeUsage = (ReflectionProbeUsage)EditorGUILayout.EnumPopup(styles.reflectionProbes, m_Terrain.reflectionProbeUsage);
                m_ShowReflectionProbesGUI.target = reflectionProbeUsage != ReflectionProbeUsage.Off;
                if (EditorGUILayout.BeginFadeGroup(m_ShowReflectionProbesGUI.faded))
                {
                    EditorGUI.indentLevel++;
                    RendererEditorBase.Probes.ShowClosestReflectionProbes(m_BlendInfoList);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var materialTemplate = EditorGUILayout.ObjectField("Material", m_Terrain.materialTemplate, typeof(Material), false) as Material;
                if (EditorGUI.EndChangeCheck())
                    m_ShowCreateMaterialButton = ShouldShowCreateMaterialButton(materialTemplate, serializedObject.targetObject);
                if (m_ShowCreateMaterialButton && GUILayout.Button(styles.createMaterial, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    string defaultPath = "New Terrain Material.mat";
                    if (materialTemplate == null)
                    {
                        ObjectSelector.get.Show(null, typeof(Shader), null, false, null,
                            selection =>
                            {
                                if (selection == null)
                                    return;

                                var mat = new Material(selection as Shader);
                                AssetDatabase.CreateAsset(mat, AssetDatabase.GetUniquePathNameAtSelectedPath(defaultPath));
                                Undo.RecordObject(m_Terrain, "Terrain property change");
                                m_Terrain.materialTemplate = mat;
                                MarkDirty(m_Terrain);
                            }, null);
                    }
                    else
                    {
                        var mat = new Material(materialTemplate);
                        var createTerrainMat = CreateInstance<DoCreateTerrainMaterial>();
                        createTerrainMat.terrain = m_Terrain;
                        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(mat.GetInstanceID(), createTerrainMat, defaultPath, AssetPreview.GetMiniThumbnail(mat), null);
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Warn if shader needs tangent basis
                TerrainInspectorUtility.TerrainShaderValidationGUI(materialTemplate);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Terrain, "Terrain property change");

                    m_Terrain.groupingID = groupingID;
                    m_Terrain.allowAutoConnect = allowAutoConnect;
                    m_Terrain.drawHeightmap = drawHeightmap;
                    m_Terrain.drawInstanced = drawInstanced;
                    m_Terrain.enableHeightmapRayTracing = enableHeightmapRayTracing;
                    m_Terrain.heightmapPixelError = heightmapPixelError;
                    m_Terrain.basemapDistance = basemapDistance;
                    m_Terrain.shadowCastingMode = shadowCastingMode;
                    m_Terrain.materialTemplate = materialTemplate;
                    m_Terrain.reflectionProbeUsage = reflectionProbeUsage;

                    m_Terrain.GetClosestReflectionProbes(m_BlendInfoList);
                    MarkDirty(m_Terrain);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            m_ShowTreeAndDetailSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTreeAndDetailSettings, styles.treeAndDetails);

            if (m_ShowTreeAndDetailSettings)
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();

                var drawTreesAndFoliage = EditorGUILayout.Toggle(styles.drawTrees, m_Terrain.drawTreesAndFoliage);
                var bakeLightProbesForTrees = EditorGUILayout.Toggle(styles.bakeLightProbesForTrees, m_Terrain.bakeLightProbesForTrees);
                var deringLightProbesForTrees = m_Terrain.deringLightProbesForTrees;
                using (new EditorGUI.DisabledScope(!bakeLightProbesForTrees))
                    deringLightProbesForTrees = EditorGUILayout.Toggle(styles.deringLightProbesForTrees, deringLightProbesForTrees);
                var preserveTreePrototypeLayers = EditorGUILayout.Toggle(styles.preserveTreePrototypeLayers, m_Terrain.preserveTreePrototypeLayers);

                TreeMotionVectorModeOverride treeMotionVectorOverrideMode = m_Terrain.treeMotionVectorModeOverride;
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                    treeMotionVectorOverrideMode = (TreeMotionVectorModeOverride)EditorGUILayout.EnumPopup(styles.treeMotionVectorOverrideMode, m_Terrain.treeMotionVectorModeOverride);

                float detailObjectDistance = m_Terrain.detailObjectDistance;
                bool overrideDetailDistance = overrideFlags.HasFlag(TerrainQualityOverrides.DetailDistance) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideDetailDistance))
                {
                    detailObjectDistance = EditorGUILayout.Slider(styles.detailObjectDistance, m_Terrain.detailObjectDistance, 0, 1000); // former string formatting: ""
                    if (overrideDetailDistance)
                        ShowOverrideBar();
                }

                float detailObjectDensity = m_Terrain.detailObjectDensity;
                bool overrideDetailDensity = overrideFlags.HasFlag(TerrainQualityOverrides.DetailDensity) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideDetailDensity))
                {
                    detailObjectDensity = EditorGUILayout.Slider(styles.detailObjectDensity, m_Terrain.detailObjectDensity, 0.0f, 1.0f);
                    if (overrideDetailDensity)
                        ShowOverrideBar();
                }

                var treeDistance = m_Terrain.treeDistance;
                bool overrideTreeDistance = overrideFlags.HasFlag(TerrainQualityOverrides.TreeDistance) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideTreeDistance))
                {
                    treeDistance = EditorGUILayout.Slider(styles.treeDistance, m_Terrain.treeDistance, 0, 5000); // former string formatting: ""
                    if (overrideTreeDistance)
                        ShowOverrideBar();
                }

                var treeBillboardDistance = m_Terrain.treeBillboardDistance;
                bool overrideTreeBillboardDistance = overrideFlags.HasFlag(TerrainQualityOverrides.BillboardStart) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideTreeBillboardDistance))
                {
                    treeBillboardDistance = EditorGUILayout.Slider(styles.treeBillboardDistance, m_Terrain.treeBillboardDistance, 5, 2000); // former string formatting: ""
                    if (overrideTreeBillboardDistance)
                        ShowOverrideBar();
                }

                var treeCrossFadeLength = m_Terrain.treeCrossFadeLength;
                bool overrideCrossFadeLength = overrideFlags.HasFlag(TerrainQualityOverrides.FadeLength) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideCrossFadeLength))
                {
                    treeCrossFadeLength = EditorGUILayout.Slider(styles.treeCrossFadeLength, m_Terrain.treeCrossFadeLength, 0, 200); // former string formatting: ""
                    if (overrideCrossFadeLength)
                        ShowOverrideBar();
                }

                var treeMaximumFullLODCount = m_Terrain.treeMaximumFullLODCount;
                bool overrideTreeMaximumFullLODCount = overrideFlags.HasFlag(TerrainQualityOverrides.MaxTrees) && !m_Terrain.ignoreQualitySettings;
                using (new EditorGUI.DisabledScope(overrideTreeMaximumFullLODCount))
                {
                    treeMaximumFullLODCount = EditorGUILayout.IntSlider(styles.treeMaximumFullLODCount, m_Terrain.treeMaximumFullLODCount, 0, 10000);
                    if (overrideTreeMaximumFullLODCount)
                        ShowOverrideBar();
                }

                var detailScatterMode = (DetailScatterMode)EditorGUILayout.EnumPopup(styles.detailScatterMode, m_Terrain.terrainData.detailScatterMode);

                // Only do this check once per frame.
                if (Event.current.type == EventType.Layout)
                {
                    m_LODTreePrototypePresent = false;
                    for (int i = 0; i < terrainData.treePrototypes.Length; ++i)
                    {
                        if (TerrainEditorUtility.IsLODTreePrototype(terrainData.treePrototypes[i].prefab))
                        {
                            m_LODTreePrototypePresent = true;
                            break;
                        }
                    }
                }

                if (m_LODTreePrototypePresent)
                    EditorGUILayout.HelpBox("Tree Distance, Billboard Start, Fade Length and Max Mesh Trees have no effect on SpeedTree trees. Please use the LOD Group component on the tree prefab to control LOD settings.", MessageType.Info);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Terrain, "Terrain property change");

                    m_Terrain.drawTreesAndFoliage = drawTreesAndFoliage;
                    m_Terrain.bakeLightProbesForTrees = bakeLightProbesForTrees;
                    m_Terrain.deringLightProbesForTrees = deringLightProbesForTrees;
                    m_Terrain.preserveTreePrototypeLayers = preserveTreePrototypeLayers;
                    m_Terrain.detailObjectDistance = detailObjectDistance;
                    m_Terrain.detailObjectDensity = detailObjectDensity;
                    m_Terrain.treeDistance = treeDistance;
                    m_Terrain.treeBillboardDistance = treeBillboardDistance;
                    m_Terrain.treeCrossFadeLength = treeCrossFadeLength;
                    m_Terrain.treeMaximumFullLODCount = treeMaximumFullLODCount;
                    m_Terrain.treeMotionVectorModeOverride = treeMotionVectorOverrideMode;

                    if (m_Terrain.terrainData.detailScatterMode != detailScatterMode)
                    {
                        if (detailScatterMode == DetailScatterMode.CoverageMode)
                        {
                            ResampleDetailResolution(terrainData, terrainData.detailResolution, terrainData.detailResolutionPerPatch, detailScatterMode);
                        }
                        else if (EditorUtility.DisplayDialog(
                            L10n.Tr("Change Detail Scatter Mode"),
                            L10n.Tr("Changing detail scatter mode to \"Instance count\" will erase existing detail placements. This action is undoable. Do you wish to continue?"),
                            L10n.Tr("Yes"),
                            L10n.Tr("No"))
                            )
                        {
                            m_Terrain.terrainData.SetDetailScatterMode(detailScatterMode);
                        }
                    }

                    MarkDirty(m_Terrain);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            m_ShowGrassWindSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowGrassWindSettings, styles.grassWindSettings);

            if (m_ShowGrassWindSettings)
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();

                var wavingGrassStrength = EditorGUILayout.Slider(styles.wavingGrassStrength, terrainData.wavingGrassStrength, 0, 1); // former string formatting: "%"
                var wavingGrassSpeed = EditorGUILayout.Slider(styles.wavingGrassSpeed, terrainData.wavingGrassSpeed, 0, 1); // former string formatting: "%"
                var wavingGrassAmount = EditorGUILayout.Slider(styles.wavingGrassAmount, terrainData.wavingGrassAmount, 0, 1); // former string formatting: "%"
                var wavingGrassTint = EditorGUILayout.ColorField(styles.wavingGrassTint, terrainData.wavingGrassTint);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(terrainData, "TerrainData property change");

                    terrainData.wavingGrassStrength = wavingGrassStrength;
                    terrainData.wavingGrassSpeed = wavingGrassSpeed;
                    terrainData.wavingGrassAmount = wavingGrassAmount;
                    terrainData.wavingGrassTint = wavingGrassTint;

                    MarkTerrainDataDirty();
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            ShowResolution(terrainData);
            EditorGUILayout.Space();

            ShowHolesSettings(terrainData);
            EditorGUILayout.Space();

            ShowTextures();
            EditorGUILayout.Space();

            RenderLightingFields();

            ShowRenderingLayerMask();

            m_ShowTerrainQualitySettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTerrainQualitySettings, styles.qualitySettings);
            if (m_ShowTerrainQualitySettings)
            {
                ++EditorGUI.indentLevel;
                EditorGUI.BeginChangeCheck();
                var ignoreQualitySettings = EditorGUILayout.Toggle(styles.ignoreQualitySettings, m_Terrain.ignoreQualitySettings);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Terrain, "Terrain property change");
                    m_Terrain.ignoreQualitySettings = ignoreQualitySettings;
                    MarkDirty(m_Terrain);
                }
                --EditorGUI.indentLevel;
            }
        }

        // this is a non-serializedProperty version of RendererEditorBase.DrawRenderingLayer()
        // if we switch to serializedProperty multi-edit, we can just use that function directly instead
        private void ShowRenderingLayerMask(bool useMiniStyle = false)
        {
            RenderPipelineAsset srpAsset = GraphicsSettings.currentRenderPipeline;
            bool usingSRP = srpAsset != null;
            if (!usingSRP || target == null)
                return;

            var mask = (int)m_Terrain.renderingLayerMask;
            var layerNames = srpAsset.prefixedRenderingLayerMaskNames;
            if (layerNames == null)
                layerNames = RendererEditorBase.defaultPrefixedRenderingLayerNames;

            EditorGUI.BeginChangeCheck();

            var rect = EditorGUILayout.GetControlRect();
            if (useMiniStyle)
            {
                rect = ModuleUI.PrefixLabel(rect, Styles.renderingLayerMask);
                mask = EditorGUI.MaskField(rect, GUIContent.none, mask, layerNames,
                    ParticleSystemStyles.Get().popup);
            }
            else
                mask = EditorGUI.MaskField(rect, Styles.renderingLayerMask, mask, layerNames);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Terrain, "Set Terrain rendering layer mask");
                m_Terrain.renderingLayerMask = (UInt32)mask;
                EditorUtility.SetDirty(m_Terrain);
            }
        }

        internal static event Action BrushSizeChanged;
        internal static event Action BrushStrengthChanged;
        public void ShowBrushes(int spacing, bool showBrushes, bool showBrushEditor, bool showBrushSize, bool showBrushStrength, int textureResolutionPerTile)
        {
            EditorGUI.BeginDisabledGroup(s_activeTerrainInspector != GetInstanceID() || s_activeTerrainInspectorInstance != this);

            GUILayout.Space(spacing);
            bool repaint = false;
            if (showBrushes)
                repaint = brushList.ShowGUI();

            if (showBrushSize)
            {
                float minBrushSize, maxBrushSize;
                GetBrushSizeLimits(out minBrushSize, out maxBrushSize, textureResolutionPerTile);
                brushSize = TerrainInspectorUtility.PowerSlider(styles.brushSize, brushSize, minBrushSize, maxBrushSize, 4.0f);
                if (!Mathf.Approximately(brushSize, prevBrushSize))
                {
                    if (GetActiveTerrainTool() is ITerrainPaintToolWithOverlays && BrushSizeChanged != null) BrushSizeChanged();
                    prevBrushSize = brushSize;
                }
            }

            if (showBrushStrength)
            {
                brushStrength = TerrainInspectorUtility.ScaledSliderWithRounding(styles.opacity, brushStrength, kMinBrushStrength, kMaxBrushStrength, 100.0f, 0.1f);
                if (!Mathf.Approximately(brushStrength, prevBrushStrength))
                {
                    if (GetActiveTerrainTool() is ITerrainPaintToolWithOverlays && BrushStrengthChanged != null) BrushStrengthChanged();
                    prevBrushStrength = brushStrength;
                }
            }

            if (showBrushEditor)
            {
                if (showBrushes || showBrushSize || showBrushStrength)
                    EditorGUILayout.Space();
                brushList.ShowEditGUI();
            }

            if (repaint)
                Repaint();

            EditorGUI.EndDisabledGroup();
        }

        void ResizeControlTexture(int newResolution)
        {
            RenderTexture oldRT = RenderTexture.active;

            TerrainData terrainData = m_Terrain.terrainData;

            // we record the terrainData because we change terrainData.alphamapResolution
            // we also store a complete copy of the alphamap -- because each alphamap is a separate asset, these are separate
            var undoObjects = new List<UnityEngine.Object>();
            undoObjects.Add(terrainData);
            undoObjects.AddRange(terrainData.alphamapTextures);
            Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Resize Alphamap");

            Material blitMaterial = TerrainPaintUtility.GetCopyTerrainLayerMaterial();      // special blit that forces copy from highest mip only

            int targetRezU = newResolution;
            int targetRezV = newResolution;

            float invTargetRezU = 1.0f / targetRezU;
            float invTargetRezV = 1.0f / targetRezV;

            RenderTexture[] resizedAlphaMaps = new RenderTexture[terrainData.alphamapTextureCount];
            for (int i = 0; i < resizedAlphaMaps.Length; i++)
            {
                Texture2D oldAlphamap = terrainData.alphamapTextures[i];

                int sourceRezU = oldAlphamap.width;
                int sourceRezV = oldAlphamap.height;
                float invSourceRezU = 1.0f / sourceRezU;
                float invSourceRezV = 1.0f / sourceRezV;

                resizedAlphaMaps[i] = RenderTexture.GetTemporary(newResolution, newResolution, 0, oldAlphamap.graphicsFormat);

                float scaleU = (1.0f - invSourceRezU) / (1.0f - invTargetRezU);
                float scaleV = (1.0f - invSourceRezV) / (1.0f - invTargetRezV);
                float offsetU = 0.5f * (invSourceRezU - scaleU * invTargetRezU);
                float offsetV = 0.5f * (invSourceRezV - scaleV * invTargetRezV);

                Vector2 scale = new Vector2(scaleU, scaleV);
                Vector2 offset = new Vector2(offsetU, offsetV);

                blitMaterial.mainTexture = oldAlphamap;
                blitMaterial.mainTextureScale = scale;
                blitMaterial.mainTextureOffset = offset;

                // custom blit
                oldAlphamap.filterMode = FilterMode.Bilinear;
                RenderTexture.active = resizedAlphaMaps[i];
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, newResolution, 0, newResolution);
                blitMaterial.SetPass(2);
                TerrainPaintUtility.DrawQuad(new RectInt(0, 0, newResolution, newResolution), new RectInt(0, 0, sourceRezU, sourceRezV), oldAlphamap);
                GL.PopMatrix();
            }

            terrainData.alphamapResolution = newResolution;
            for (int i = 0; i < resizedAlphaMaps.Length; i++)
            {
                RenderTexture.active = resizedAlphaMaps[i];
                terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, i, new RectInt(0, 0, newResolution, newResolution), Vector2Int.zero, false);
            }
            terrainData.SetBaseMapDirty();
            RenderTexture.active = oldRT;
            for (int i = 0; i < resizedAlphaMaps.Length; i++)
                RenderTexture.ReleaseTemporary(resizedAlphaMaps[i]);
            Repaint();
        }

        void ResizeHeightmap(int newResolution)
        {
            RenderTexture oldRT = RenderTexture.active;

            RenderTexture oldHeightmap = RenderTexture.GetTemporary(m_Terrain.terrainData.heightmapTexture.descriptor);
            Graphics.Blit(m_Terrain.terrainData.heightmapTexture, oldHeightmap);

            // TODO: Can this be optimized if there is no hole?
            RenderTexture oldHoles = RenderTexture.GetTemporary(m_Terrain.terrainData.holesRenderTexture.descriptor);
            Graphics.Blit(m_Terrain.terrainData.holesRenderTexture, oldHoles);

            Undo.RegisterCompleteObjectUndo(m_Terrain.terrainData, "Resize Heightmap");

            int dWidth = m_Terrain.terrainData.heightmapResolution;
            int sWidth = newResolution;

            Vector3 oldSize = m_Terrain.terrainData.size;
            m_Terrain.terrainData.heightmapResolution = newResolution;
            m_Terrain.terrainData.size = oldSize;

            oldHeightmap.filterMode = FilterMode.Bilinear;

            // Make sure textures are offset correctly when resampling
            // tsuv = (suv * swidth - 0.5) / (swidth - 1)
            // duv = (tsuv(dwidth - 1) + 0.5) / dwidth
            // duv = (((suv * swidth - 0.5) / (swidth - 1)) * (dwidth - 1) + 0.5) / dwidth
            // k = (dwidth - 1) / (swidth - 1) / dwidth
            // duv = suv * (swidth * k)     + 0.5 / dwidth - 0.5 * k

            float k = (dWidth - 1.0f) / (sWidth - 1.0f) / dWidth;
            float scaleX = (sWidth * k);
            float offsetX = (float)(0.5 / dWidth - 0.5 * k);
            Vector2 scale = new Vector2(scaleX, scaleX);
            Vector2 offset = new Vector2(offsetX, offsetX);

            Graphics.Blit(oldHeightmap, m_Terrain.terrainData.heightmapTexture, scale, offset);
            RenderTexture.ReleaseTemporary(oldHeightmap);

            oldHoles.filterMode = FilterMode.Point;
            Graphics.Blit(oldHoles, (RenderTexture)m_Terrain.terrainData.holesRenderTexture);
            RenderTexture.ReleaseTemporary(oldHoles);

            RenderTexture.active = oldRT;

            m_Terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, m_Terrain.terrainData.heightmapTexture.width, m_Terrain.terrainData.heightmapTexture.height), TerrainHeightmapSyncControl.HeightAndLod);
            m_Terrain.terrainData.DirtyTextureRegion(TerrainData.HolesTextureName, new RectInt(0, 0, m_Terrain.terrainData.holesRenderTexture.width, m_Terrain.terrainData.holesRenderTexture.height), false);

            Repaint();
        }

        private bool m_ShowTextures = true;
        public void ShowTextures()
        {
            m_ShowTextures = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowTextures, styles.textures);

            if (!m_ShowTextures)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            ++EditorGUI.indentLevel;

            EditorGUILayout.HelpBox(styles.requireResampling.text, MessageType.Info);

            GUILayout.BeginVertical();

            // heightmap texture resolution
            int newHeightmapResolution = EditorGUILayout.IntPopup(Styles.heightmapResolution, m_Terrain.terrainData.heightmapResolution, Styles.heightmapResolutionStrings, Styles.heightmapResolutionInts);
            if (newHeightmapResolution != m_Terrain.terrainData.heightmapResolution)
            {
                ResizeHeightmap(newHeightmapResolution);
                MarkTerrainDataDirty();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // calling functions in the middle of GUI rendering code can cause errors with GUI clips popping
            // so set it to a bool and call the function at the end
            bool importRaw = GUILayout.Button(styles.importRaw);
            bool exportRaw = GUILayout.Button(styles.exportRaw);

            GUILayout.EndHorizontal();

            // alphamap resolution
            int newAlphamapResolution = EditorGUILayout.IntPopup(Styles.alphamapResolution, m_Terrain.terrainData.alphamapResolution, Styles.alphamapResolutionStrings, Styles.alphamapResolutionInts);
            if (newAlphamapResolution != m_Terrain.terrainData.alphamapResolution)
            {
                ResizeControlTexture(newAlphamapResolution);
                MarkTerrainDataDirty();
            }

            // base texture resolution
            int newBasemapResolution = EditorGUILayout.IntPopup(Styles.basemapResolution, m_Terrain.terrainData.baseMapResolution, Styles.basemapResolutionStrings, Styles.basemapResolutionInts);
            if (newBasemapResolution != m_Terrain.terrainData.baseMapResolution)
            {
                Undo.RecordObject(m_Terrain.terrainData, "Resize Basemap");
                m_Terrain.terrainData.baseMapResolution = newBasemapResolution;
            }

            GUILayout.EndVertical();

            --EditorGUI.indentLevel;

            EditorGUILayout.EndFoldoutHeaderGroup();

            // calling the appropriate functions at the end
            if (importRaw) TerrainMenus.ImportRaw();
            if (exportRaw) TerrainMenus.ExportHeightmapRaw();
        }

        private bool m_ShowResolution = true;

        public void ShowResolution(TerrainData terrainData)
        {
            m_ShowResolution = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowResolution, styles.meshResolution);

            if (!m_ShowResolution)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            ++EditorGUI.indentLevel;

            float terrainWidth = terrainData.size.x;
            float terrainHeight = terrainData.size.y;
            float terrainLength = terrainData.size.z;
            int detailResolution = terrainData.detailResolution;
            int detailResolutionPerPatch = terrainData.detailResolutionPerPatch;

            EditorGUI.BeginChangeCheck();

            const int kMaxTerrainSize = 100000;
            const int kMaxTerrainHeight = 10000;
            terrainWidth = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TrTextContent("Terrain Width", $"Size of the terrain object in its X axis (in world units). Value range [1, {kMaxTerrainSize}]"), terrainWidth);
            if (terrainWidth <= 0) terrainWidth = 1;
            if (terrainWidth > kMaxTerrainSize) terrainWidth = kMaxTerrainSize;

            terrainLength = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TrTextContent("Terrain Length", $"Size of the terrain object in its Z axis (in world units). Value range [1, {kMaxTerrainSize}]"), terrainLength);
            if (terrainLength <= 0) terrainLength = 1;
            if (terrainLength > kMaxTerrainSize) terrainLength = kMaxTerrainSize;

            terrainHeight = EditorGUILayout.DelayedFloatField(EditorGUIUtility.TrTextContent("Terrain Height", $"Difference in Y coordinate between the lowest possible heightmap value and the highest (in world units). Value range [1, {kMaxTerrainHeight}]"), terrainHeight);
            if (terrainHeight <= 0) terrainHeight = 1;
            if (terrainHeight > kMaxTerrainHeight) terrainHeight = kMaxTerrainHeight;

            const int kMinDetailResolutionPerPatch = 8;
            const int kMaxDetailResolutionPerPatch = 128;
            detailResolutionPerPatch = EditorGUILayout.DelayedIntField(EditorGUIUtility.TrTextContent("Detail Resolution Per Patch", $"The number of cells in a single patch (mesh). This value is squared to form a grid of cells, and must be a divisor of the detail resolution. Value range [{kMinDetailResolutionPerPatch}, {kMaxDetailResolutionPerPatch}]"), detailResolutionPerPatch);
            detailResolutionPerPatch = Mathf.Clamp(detailResolutionPerPatch, kMinDetailResolutionPerPatch, kMaxDetailResolutionPerPatch);

            const int kMinDetailResolution = 0;
            const int kMaxDetailResolution = 4048;
            detailResolution = EditorGUILayout.DelayedIntField(EditorGUIUtility.TrTextContent("Detail Resolution", $"The number of cells available for placing details onto the terrain tile. This value is squared to make a grid of cells. Value range [{kMinDetailResolution}, {kMaxDetailResolution}]"), detailResolution);
            detailResolution = Mathf.Clamp(detailResolution, kMinDetailResolution, kMaxDetailResolution);

            ShowDetailStats();

            if (EditorGUI.EndChangeCheck())
            {
                var undoObjects = new List<UnityEngine.Object>();
                undoObjects.Add(terrainData);
                undoObjects.AddRange(terrainData.alphamapTextures);

                Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Set Resolution");

                terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

                bool resolutionChanged = terrainData.detailResolution != detailResolution;
                if (resolutionChanged)
                {
                    detailResolutionPerPatch = Mathf.Min(detailResolutionPerPatch, detailResolution);
                }

                bool resolutionPerPatchChanged = terrainData.detailResolutionPerPatch != detailResolutionPerPatch;
                if (resolutionPerPatchChanged)
                {
                    detailResolution = Mathf.Max(detailResolution, detailResolutionPerPatch);
                }

                if (resolutionChanged || resolutionPerPatchChanged)
                    ResampleDetailResolution(terrainData, detailResolution, detailResolutionPerPatch);

                MarkTerrainDataDirty();
                m_Terrain.Flush();
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private bool m_ShowHolesSettings = true;

        public void ShowHolesSettings(TerrainData terrainData)
        {
            m_ShowHolesSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowHolesSettings, styles.holesSettings);
            if (!m_ShowHolesSettings)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            ++EditorGUI.indentLevel;

            EditorGUI.BeginChangeCheck();

            bool enableHolesTextureCompression = EditorGUILayout.Toggle(styles.holesCompressionToggle, terrainData.enableHolesTextureCompression);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(terrainData, "TerrainData property change");

                terrainData.enableHolesTextureCompression = enableHolesTextureCompression;

                MarkTerrainDataDirty();
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void ResampleDetailResolution(TerrainData terrainData, int resolution, int resolutionPerPatch, DetailScatterMode? scatterMode = null)
        {
            var layers = new List<byte[]>();
            int originalResolution = terrainData.detailResolution;
            int oldWidth = terrainData.detailWidth;
            int oldHeight = terrainData.detailHeight;
            // Store texture versions of outdated detail map
            for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
            {
                byte[] detailArray = terrainData
                    .GetDetailLayer(0, 0, oldWidth, oldHeight, i)
                    .Cast<int>().Select<int, byte>(v =>
                        terrainData.detailScatterMode == DetailScatterMode.InstanceCountMode
                        ? (byte)(Math.Min(v * 16, 255))
                        : (byte)v)
                    .ToArray();
                layers.Add(detailArray);
            }

            // resize (and clear) detail maps
            terrainData.SetDetailResolution(resolution, resolutionPerPatch);
            if (scatterMode.HasValue)
                terrainData.SetDetailScatterMode(scatterMode.Value);

            for (int i = 0; i < layers.Count; i++)
            {
                Texture2D detailTexture = new Texture2D(oldWidth, oldHeight, TextureFormat.R8, false);
                detailTexture.filterMode = FilterMode.Bilinear;
                detailTexture.SetPixelData(layers[i], 0);
                detailTexture.Apply(false);

                int width = terrainData.detailWidth;
                int height = terrainData.detailHeight;

                // blit old detail map textures to target sized RT
                RenderTexture rt = RenderTexture.GetTemporary(width, height);
                rt.filterMode = FilterMode.Bilinear;
                RenderTexture.active = rt;
                Graphics.Blit(detailTexture, rt);
                Texture2D resizedTex = new Texture2D(width, height);
                resizedTex.ReadPixels(new Rect(0, 0, width, height), 0,0);
                resizedTex.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                float resampleRatio = terrainData.detailScatterMode == DetailScatterMode.CoverageMode
                ? 1.0f
                : (float)originalResolution / resolution;
                resampleRatio *= resampleRatio;

                // Get pixel data from resulting blit and copy to detail maps
                int[] values = resizedTex.GetPixels()
                    .Select<Color, int>(c =>
                    {
                        int detailAmt = Mathf.CeilToInt(terrainData.maxDetailScatterPerRes * c.r);
                        return Mathf.CeilToInt(detailAmt * resampleRatio);
                    })
                    .ToArray<int>();

                var detailArray = new int[width, height];
                Buffer.BlockCopy(values, 0, detailArray, 0, width * height * sizeof(int));
                terrainData.SetDetailLayer(0, 0, i, detailArray);

                DestroyImmediate(detailTexture);
                DestroyImmediate(resizedTex);
            }
        }

        internal static void ShowRefreshPrototypes()
        {
            if (GUILayout.Button(styles.refresh, styles.largeSquare))
            {
                TerrainMenus.RefreshPrototypes();
            }
        }

        private void LightingDataUpdatedRepaint()
        {
            if (m_Lighting.showLightmapSettings)
            {
                Repaint();
            }
        }

        public void RenderLightingFields()
        {
            m_Lighting.RenderTerrainSettings();
        }

        void OnInspectorUpdate()
        {
            if (AssetPreview.HasAnyNewPreviewTexturesAvailable())
                Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Initialize();

            if (styles == null)
            {
                styles = new Styles();
            }

            if (!m_Terrain.terrainData)
            {
                GUI.enabled = false;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Toolbar(-1, styles.toolIcons, styles.command);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Terrain Asset Missing");
                m_Terrain.terrainData = EditorGUILayout.ObjectField("Assign:", m_Terrain.terrainData, typeof(TerrainData), false) as TerrainData;
                GUILayout.EndVertical();
                return;
            }

            var isAsset = IsTerrainPartOfPrefabOrAsset(target as Terrain);
            var isEditorActive = IsEditorActive(this) && !isAsset;

            if (!isEditorActive && !isAsset)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(styles.duplicateTab, EditorStyles.boldLabel);
                if (GUILayout.Button(styles.makeMeActive))
                {
                    SetActiveEditor(this);
                }
                GUILayout.EndVertical();
            }

            if (Event.current.type == EventType.Layout)
                m_TerrainCollider = m_Terrain.gameObject.GetComponent<TerrainCollider>();

            if (m_TerrainCollider && m_TerrainCollider.terrainData != m_Terrain.terrainData)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(styles.mismatchedTerrainData, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
                if (GUILayout.Button(styles.assign, GUILayout.ExpandWidth(false)))
                {
                    Undo.RecordObject(m_TerrainCollider, "Assign TerrainData");
                    m_TerrainCollider.terrainData = m_Terrain.terrainData;
                }
                GUILayout.Space(3);
                GUILayout.EndVertical();
            }

            EditorGUI.BeginDisabledGroup(s_activeTerrainInspector != GetInstanceID() || s_activeTerrainInspectorInstance != this);

            int tool = (int)selectedCategory;
            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();          // flexible space on either end centers the toolbar
            GUI.changed = false;
            int newlySelectedTool = GUILayout.Toolbar(tool, styles.toolIcons, styles.command);

            if (newlySelectedTool != tool)
            {
                SelectCategory((TerrainTool) newlySelectedTool);

                // Need to repaint other terrain inspectors as their previously selected tool is now deselected.
                InspectorWindow.RepaintAllInspectors();

                if (Toolbar.get != null)
                    Toolbar.get.Repaint();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();


            switch ((TerrainTool)tool)
            {
                case TerrainTool.Paint:
                case TerrainTool.CreateNeighbor:
                case TerrainTool.PlaceTree:
                case TerrainTool.PaintDetail:
                    var activeTool = GetActiveTerrainTool();
                    if (activeTool != null)
                    {
                        if (selectedCategory == TerrainTool.Paint && m_PaintToolNames != null && m_PaintToolNames.Length > 0)
                        {
                            EditorGUI.BeginChangeCheck();
                            int index = EditorGUILayout.Popup(m_ActivePaintToolIndex, m_PaintToolNames);
                            if (EditorGUI.EndChangeCheck() && index != m_ActivePaintToolIndex)
                            {
                                SelectToolByName(m_PaintToolNames[index]);
                            }
                        }

                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        if (selectedCategory != TerrainTool.Paint)
                            GUILayout.Label(activeTool.GetName(), EditorStyles.boldLabel);
                        GUILayout.Label(activeTool.GetDescription(), EditorStyles.wordWrappedMiniLabel);
                        GUILayout.EndVertical();
                        EditorGUILayout.Space();
                        activeTool.OnInspectorGUI(m_Terrain, onInspectorGUIEditContext);
                    }
                    break;
                case TerrainTool.TerrainSettings:
                    ShowSettings();
                    break;
                default:
                    // TODO: Fix these somehow sensibly
                    GUILayout.Label("No tool selected");
                    GUILayout.Label("Please select a tool", EditorStyles.wordWrappedMiniLabel);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        public bool Raycast(out Vector2 uv, out Vector3 pos)
        {
            Ray mouseRay = GUIPointToWorldRayPrecise(Event.current.mousePosition);

            RaycastHit hit;
            if (m_Terrain.GetComponent<TerrainCollider>().Raycast(mouseRay, out hit, Mathf.Infinity, true))
            {
                uv = hit.textureCoord;
                pos = hit.point;
                return true;
            }

            uv = Vector2.zero;
            pos = Vector3.zero;
            return false;
        }

        public bool HasFrameBounds()
        {
            // When selecting terrains using Scene search, they may be framed right after selecting, when the editor
            // is not initialized yet. Just return empty bounds in that case.
            return m_Terrain != null;
        }

        public Bounds OnGetFrameBounds()
        {
            Vector2 uv;
            Vector3 pos;

            // It's possible that something other the scene view invoked OnGetFrameBounds (e.g. double clicking the terrain in the hierarchy)
            // In this case we can't do a raycast, because there is no active camera.
            if (Camera.current && m_Terrain.terrainData && Raycast(out uv, out pos))
            {
                // Avoid locking when framing terrain (case 527647)
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.viewIsLockedToObject = false;

                // Use height editing tool for calculating the bounds by default
                Bounds bounds = new Bounds();
                var activeTool = GetActiveTerrainTool();
                float frameSize = activeTool != null
                    ? (activeTool is PaintTreesTool ? ((PaintTreesTool)activeTool).brushSize : brushSize)
                    : Mathf.Max(m_Terrain.terrainData.size.x, m_Terrain.terrainData.size.z);
                Vector3 size;
                size.x = frameSize / m_Terrain.terrainData.heightmapResolution * m_Terrain.terrainData.size.x;
                size.z = frameSize / m_Terrain.terrainData.heightmapResolution * m_Terrain.terrainData.size.z;
                size.y = (size.x + size.z) * 0.5F;
                bounds.center = pos;
                bounds.size = size;
                // detail painting needs to be much closer
                if (selectedCategory == TerrainTool.PaintDetail && m_Terrain.terrainData.detailWidth != 0)
                {
                    size.x = frameSize / m_Terrain.terrainData.detailWidth * m_Terrain.terrainData.size.x;
                    size.z = frameSize / m_Terrain.terrainData.detailHeight * m_Terrain.terrainData.size.z;
                    size.y = 0;
                    bounds.size = size;
                }

                return bounds;
            }
            else
            {
                // We don't return bounds from the collider, because apparently they are not immediately
                // updated after changing the position. So if terrain is repositioned, then bounds will
                // still be wrong until after some times PhysX updates them (it gets them through some
                // pruning interface and not directly from heightmap shape).
                //return m_Terrain.collider.bounds;

                Vector3 position = m_Terrain.transform.position;

                if (m_Terrain.terrainData == null)
                {
                    return new Bounds(position, Vector3.zero);
                }

                Vector3 size = m_Terrain.terrainData.size;

                float[,] heights = m_Terrain.terrainData.GetHeights(0, 0, m_Terrain.terrainData.heightmapResolution, m_Terrain.terrainData.heightmapResolution);

                float maxHeight = float.MinValue;
                for (int y = 0; y < m_Terrain.terrainData.heightmapResolution; y++)
                    for (int x = 0; x < m_Terrain.terrainData.heightmapResolution; x++)
                        maxHeight = Mathf.Max(maxHeight, heights[x, y]);

                size.y = maxHeight * size.y;

                return new Bounds(position + size * 0.5f, size);
            }
        }

        private bool IsModificationToolActive()
        {
            if (!m_Terrain)
                return false;
            TerrainTool st = selectedCategory;
            if (st == TerrainTool.TerrainSettings)
                return false;
            if ((int)st < 0 || st >= TerrainTool.TerrainToolCount)
                return false;
            return true;
        }

        bool IsBrushPreviewVisible(Terrain overTerrain)
        {
            if (!IsModificationToolActive())
                return false;

            return (overTerrain != null);
        }

        private static Ray GUIPointToWorldRayPrecise(Vector2 guiPoint, float startZ = float.NegativeInfinity)
        {
            Camera camera = Camera.current;
            if (!camera)
            {
                Debug.LogError("Unable to convert GUI point to world ray if a camera has not been set up!");
                return new Ray(Vector3.zero, Vector3.forward);
            }

            if (float.IsNegativeInfinity(startZ))
            {
                startZ = camera.nearClipPlane;
            }

            Vector2 screenPixelPos = HandleUtility.GUIPointToScreenPixelCoordinate(guiPoint);
            Rect viewport = camera.pixelRect;

            Matrix4x4 camToWorld = camera.cameraToWorldMatrix;
            Matrix4x4 camToClip = camera.projectionMatrix;
            Matrix4x4 clipToCam = camToClip.inverse;

            // calculate ray origin and direction in world space
            Vector3 rayOriginWorldSpace;
            Vector3 rayDirectionWorldSpace;

            // first construct an arbitrary point that is on the ray through this screen pixel (remap screen pixel point to clip space [-1, 1])
            Vector3 rayPointClipSpace = new Vector3(
                (screenPixelPos.x - viewport.x) * 2.0f / viewport.width - 1.0f,
                (screenPixelPos.y - viewport.y) * 2.0f / viewport.height - 1.0f,
                0.95f
            );

            // and convert that point to camera space
            Vector3 rayPointCameraSpace = clipToCam.MultiplyPoint(rayPointClipSpace);

            if (camera.orthographic)
            {
                // ray direction is always 'camera forward' in orthographic mode
                Vector3 rayDirectionCameraSpace = new Vector3(0.0f, 0.0f, -1.0f);
                rayDirectionWorldSpace = camToWorld.MultiplyVector(rayDirectionCameraSpace);
                rayDirectionWorldSpace.Normalize();

                // in camera space, the ray origin has the same XY coordinates as ANY point on the ray
                // so we just need to override the Z coordinate to startZ to get the correct starting point
                // (assuming camToWorld is a pure rotation/offset, with no scale)
                Vector3 rayOriginCameraSpace = rayPointCameraSpace;
                rayOriginCameraSpace.z = startZ;

                // move it to world space
                rayOriginWorldSpace = camToWorld.MultiplyPoint(rayOriginCameraSpace);
            }
            else
            {
                // in projective mode, the ray passes through the origin in camera space
                // so the ray direction is just (ray point - origin) == (ray point)
                Vector3 rayDirectionCameraSpace = rayPointCameraSpace;
                rayDirectionCameraSpace.Normalize();

                rayDirectionWorldSpace = camToWorld.MultiplyVector(rayDirectionCameraSpace);

                // calculate the correct startZ offset from the camera by moving a distance along the ray direction
                // this assumes camToWorld is a pure rotation/offset, with no scale, so we can use rayDirection.z to calculate how far we need to move
                Vector3 cameraPositionWorldSpace = camToWorld.MultiplyPoint(Vector3.zero);
                Vector3 originOffsetWorldSpace = rayDirectionWorldSpace * Mathf.Abs(startZ / rayDirectionCameraSpace.z);
                rayOriginWorldSpace = cameraPositionWorldSpace + originOffsetWorldSpace;
            }

            return new Ray(rayOriginWorldSpace, rayDirectionWorldSpace);
        }

        internal static bool RaycastAllTerrains(out Terrain hitTerrain, out RaycastHit raycastHit)
        {
            Ray mouseRay = GUIPointToWorldRayPrecise(Event.current.mousePosition);

            float minDist = float.MaxValue;
            hitTerrain = null;
            raycastHit = new RaycastHit();

            if (mouseRay.direction == Vector3.zero) return false;

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                RaycastHit hit;
                if (terrain.GetComponent<TerrainCollider>().Raycast(mouseRay, out hit, Mathf.Infinity, true))
                {
                    if (hit.distance < minDist)
                    {
                        minDist = hit.distance;
                        hitTerrain = terrain;
                        raycastHit = hit;
                    }
                }
            }
            return (hitTerrain != null);
        }

        public void OnSceneGUICallback(SceneView sceneView)
        {
            if (selectedCategory == TerrainTool.None)
            {
                // if we switch the active scene tool while painting we still need to update the terrains. ( case 1394295 )
                PaintContext.ApplyDelayedActions();
            }

            Initialize();

            if (selectedCategory == TerrainTool.None || GetActiveTerrainTool() == null)
            {
                return;
            }

            ITerrainPaintTool activeTool = GetActiveTerrainTool();

            Event e = Event.current;

            Terrain hitTerrain = null;

            RaycastHit raycastHit = new RaycastHit();

            // If this is not the active terrain inspector, we shouldn't be affecting the SceneGUI
            if (s_activeTerrainInspector != GetInstanceID() || s_activeTerrainInspectorInstance != this)
                return;

            if (selectedCategory == TerrainTool.Paint       ||
                selectedCategory == TerrainTool.PaintDetail ||
                selectedCategory == TerrainTool.PlaceTree)
            {
                RaycastAllTerrains(out hitTerrain, out raycastHit);
            }

            Texture brushTexture = selectedCategory == TerrainTool.PlaceTree ? brushList.GetCircleBrush().texture : brushList.GetActiveBrush().texture;

            Vector2 uv = raycastHit.textureCoord;

            bool hitValidTerrain = (hitTerrain != null && hitTerrain.terrainData != null);
            if (hitValidTerrain)
            {
                HotkeyApply(raycastHit.distance);
            }

            int id = GUIUtility.GetControlID(s_TerrainEditorHash, FocusType.Passive);
            Terrain lastActiveTerrain = hitValidTerrain ? hitTerrain : s_LastActiveTerrain;
            if (lastActiveTerrain)
            {
                activeTool.OnSceneGUI(lastActiveTerrain, onSceneGUIEditContext.Set(sceneView, hitValidTerrain, raycastHit, brushTexture, brushStrength, brushSize, id));

                var mousePos = Event.current.mousePosition;
                var cameraRect = sceneView.cameraViewport;
                cameraRect.y = 0;
                var isMouseInSceneView = cameraRect.Contains(mousePos);
                if (EditorGUIUtility.hotControl == id || (isMouseInSceneView && EditorGUIUtility.hotControl == 0))
                {
                    activeTool.OnRenderBrushPreview(lastActiveTerrain, onSceneGUIEditContext);
                }
            }

            var eventType = e.GetTypeForControl(id);
            if (!hitValidTerrain)
            {
                // if we release the mouse button outside the terrain we still need to update the terrains. ( case 1089947 )
                if (eventType == EventType.MouseUp)
                    PaintContext.ApplyDelayedActions();

                return;
            }

            s_LastActiveTerrain = hitTerrain;

            bool changeSelection = false;

            switch (eventType)
            {
                case EventType.Layout:
                    if (!IsModificationToolActive())
                        return;
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseMove:
                    if (IsBrushPreviewVisible(hitTerrain))
                        HandleUtility.Repaint();
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag:
                {
                    // Don't do anything at all if someone else owns the hotControl. Fixes case 677541.
                    if (EditorGUIUtility.hotControl != 0 && EditorGUIUtility.hotControl != id)
                        return;

                    // Don't do anything on MouseDrag if we don't own the hotControl.
                    if (eventType == EventType.MouseDrag && EditorGUIUtility.hotControl != id)
                        return;

                    // If user is ALT-dragging, we want to return to main routine
                    if (e.alt)
                        return;

                    // Allow painting with LMB only
                    if (e.button != 0)
                        return;

                    if (!IsModificationToolActive())
                        return;

                    HandleUtility.AddDefaultControl(id);
                    if (HandleUtility.nearestControl != id)
                        return;

                    if (e.type == EventType.MouseDown)
                        EditorGUIUtility.hotControl = id;

                    if (activeTool.OnPaint(hitTerrain, onPaintEditContext.Set(hitValidTerrain, raycastHit, brushTexture, uv, brushStrength, brushSize)))
                    {
                        if (selectedCategory == TerrainTool.Paint)
                        {
                            // height map modification modes
                            hitTerrain.editorRenderFlags = TerrainRenderFlags.Heightmap;
                        }
                    }

                    if (m_Terrain != hitTerrain && e.type == EventType.MouseDown)
                    {
                        changeSelection = true;
                    }

                    e.Use();
                }
                break;

                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != id)
                    {
                        return;
                    }

                    // Release hot control
                    GUIUtility.hotControl = 0;

                    if (!IsModificationToolActive())
                        return;

                    if (m_Terrain != hitTerrain)
                    {
                        changeSelection = true;
                    }

                    PaintContext.ApplyDelayedActions();

                    e.Use();
                }
                break;
            }

            if (changeSelection)
            {
                Selection.activeObject = hitTerrain;
            }
        }
    }
} //namespace
