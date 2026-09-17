// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal static class FrameDebuggerStyles
    {
        // match enum FrameEventType on C++ side!
        internal static readonly string[] s_FrameEventTypeNames = new[]
        {
            "Clear (nothing)",
            "Clear (color)",
            "Clear (Depth)",
            "Clear (color+depth)",
            "Clear (stencil)",
            "Clear (color+stencil)",
            "Clear (depth+stencil)",
            "Clear (color+depth+stencil)",
            "SetRenderTarget",
            "Resolve Color",
            "Resolve Depth",
            "Grab RenderTexture",
            "Static Batch",
            "Dynamic Batch",
            "Draw Mesh",
            "Draw Dynamic",
            "Draw GL",
            "GPU Skinning",
            "Draw Procedural",
            "Draw Procedural Indirect",
            "Draw Procedural Indexed",
            "Draw Procedural Indexed Indirect",
            "Compute",
            "Ray Tracing Dispatch",
            "Plugin Event",
            "Draw Mesh (instanced)",
            "Begin Subpass",
            "SRP Batch",
            "",                 // on purpose empty string for kFrameEventHierarchyLevelBreak
            "Hybrid Batch Group",
            "Configure Foveated Rendering"
        };

        // General settings for the Frame Debugger Window and layout
        internal struct Window
        {
            internal const int k_StartWindowWidth = 1024;
            internal const float k_MinTreeWidth = k_StartWindowWidth * 0.33f;
            internal const float k_ResizerWidth = 5f;
            internal const float k_MinDetailsWidth = 200f;
        }

        // Tree
        internal struct Tree
        {
            internal static readonly GUIStyle s_RowText = new GUIStyle(EditorStyles.label);
            internal static readonly GUIStyle s_RowTextBold = new GUIStyle(EditorStyles.boldLabel);
            internal static readonly GUIStyle s_RowTextRight = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            internal const string k_UnknownScopeString = "<unknown scope>";
        }

        // Top Toolbar
        internal struct TopToolbar
        {
            internal static readonly GUIContent s_RecordButtonEnable = L10n.TextContent("Enable", null, null, null);
            internal static readonly GUIContent s_RecordButtonDisable = L10n.TextContent("Disable", null, null, null);
            internal static readonly GUIContent s_PrevFrame = L10n.IconContent("Profiler.PrevFrame", "Go back one frame", null);
            internal static readonly GUIContent s_NextFrame = L10n.IconContent("Profiler.NextFrame", "Go one frame forwards", null);
            internal static readonly GUIContent s_LevelsHeader = L10n.TextContent("Levels", "Render target display black/white intensity levels", null, null);
        }

        // Event Toolbar in the Event Details window
        internal struct EventToolbar
        {
            private const float k_ToolbarHeight = 22f;
            private const float k_ChannelButtonWidth = 30f;

            internal static readonly GUIStyle s_HorizontalStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = k_ToolbarHeight + 1f
            };
            internal static readonly GUIStyle s_ChannelHeaderStyle = new GUIStyle(EditorStyles.toolbarLabel)
            {
                fixedHeight = k_ToolbarHeight
            };
            internal static readonly GUIStyle s_ChannelStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedWidth = k_ChannelButtonWidth,
                fixedHeight = k_ToolbarHeight,
                border = new RectOffset(1,0,0,0)
            };
            internal static readonly GUIStyle s_PopupLeftStyle = new GUIStyle(EditorStyles.toolbarDropDown)
            {
                fixedHeight = k_ToolbarHeight
            };
            internal static readonly GUIStyle s_LevelsHorizontalStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                margin = new RectOffset(0, 4, 0, 0),
                padding = new RectOffset(0, 4, 0, 0),
                fixedHeight = k_ToolbarHeight
            };

            internal static readonly GUIContent s_DepthLabel = L10n.TextContent("Depth", "Show depth buffer", null, null);
            internal static readonly GUIContent s_StencilLabel = L10n.TextContent("Stencil", "Show stencil buffer", null, null);

            internal static readonly GUIContent[] s_ChannelLabels = new[]
            {
                L10n.TextContent("R", "Shows the selected channel from the Render Target", null, null),
                L10n.TextContent("G", "Shows the selected channel from the Render Target", null, null),
                L10n.TextContent("B", "Shows the selected channel from the Render Target", null, null),
                L10n.TextContent("A", "Shows the selected channel from the Render Target", null, null),
            };

            internal static readonly GUIContent s_LevelsHeader = L10n.TextContent("Levels", "Render target display black/white intensity levels", null, null);
            internal static readonly GUIContent[] s_MRTLabels = new[]
            {
                L10n.TextContent("RT 0", "Show render target #0", null, null),
                L10n.TextContent("RT 1", "Show render target #1", null, null),
                L10n.TextContent("RT 2", "Show render target #2", null, null),
                L10n.TextContent("RT 3", "Show render target #3", null, null),
                L10n.TextContent("RT 4", "Show render target #4", null, null),
                L10n.TextContent("RT 5", "Show render target #5", null, null),
                L10n.TextContent("RT 6", "Show render target #6", null, null),
                L10n.TextContent("RT 7", "Show render target #7", null, null)
            };
        }

        // Event Details Window
        internal struct EventDetails
        {
            private const int k_Indent1 = 5;
            private const int k_Indent2 = 20;

            internal const float k_MaxViewportHeight = 355f;

            internal const float k_VerticalLabelWidth = 150f;
            internal const float k_VerticalValueWidth = 250f;
            internal const float k_MeshNameWidth = k_VerticalLabelWidth + k_VerticalValueWidth;

            internal const int k_PropertyNameMaxChars = 30;
            internal const int k_TextureFormatMaxChars = 19;

            internal const int k_ShaderLabelWidth = 155;
            internal const int k_ShaderObjectFieldWidth = 450;

            internal const float k_MeshBottomToolbarHeight = 21f;
            internal const float k_ArrayValuePopupBtnWidth = 2.0f;

            internal const string k_FloatFormat = "F7";
            internal const string k_IntFormat = "d";
            internal const string k_NotAvailable = "-";

            internal static readonly string s_DashesString = new string('-', 30);
            internal static readonly string s_EqualsString = new string('=', 30);

            // Cached width for two-column format label (matches k_TwoColumnFormat first column width of 22 chars)
            [NoAutoStaticsCleanup] // cached measurement of a fixed GUIStyle; style persists across code reload so value remains valid
            private static float s_TwoColumnLabelWidth = -1f;
            internal static float TwoColumnLabelWidth
            {
                get
                {
                    if (s_TwoColumnLabelWidth < 0f)
                    {
                        // Calculate once after GUI is initialized
                        GUIContent tempContent = new GUIContent(new string(' ', 22));
                        s_TwoColumnLabelWidth = s_MonoLabelStyle.CalcSize(tempContent).x;
                    }
                    return s_TwoColumnLabelWidth;
                }
            }

            internal static readonly GUIStyle s_ArrayFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                margin = new RectOffset(-29, 0, 0, 0),
            };

            internal static readonly GUIStyle s_TitleHorizontalStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 10),
            };

            internal static readonly GUIStyle s_TitleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                padding = new RectOffset(k_Indent1, 0, k_Indent1, 0),
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                fixedHeight = 50,
            };

            internal static readonly GUIStyle s_FoldoutCategoryBoxStyle = new GUIStyle(EditorStyles.helpBox);
            internal static readonly GUIStyle s_FoldoutCategoryHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
            };

            internal static readonly GUIStyle s_MonoLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft
            };

            internal static readonly GUIStyle s_MonoLabelStylePadding = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(25, 0, 0, 0),
            };

            internal static readonly GUIStyle s_MonoLabelBoldPaddingStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(25, 0, 0, 0),
            };

            internal static readonly GUIStyle s_MonoLabelNoWrapStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 2, 0),
            };

            internal static readonly GUIStyle s_MonoLabelBoldStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            internal static readonly GUIStyle s_OutputMeshTabStyle = new GUIStyle("LargeButton")
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(-2, 0, 0, 0),
            };

            internal static readonly GUIStyle s_RenderTargetMeshBackgroundStyle = new GUIStyle();

            internal static readonly GUIStyle s_PropertiesBottomMarginStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 10)
            };

            internal static readonly GUIStyle s_PropertiesLeftMarginStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(k_Indent2, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };

            internal static readonly GUIStyle s_TextureButtonStyle = new GUIStyle()
            {
                fixedWidth = 20f,
                margin = new RectOffset(0, 10, 0, 0),
            };

            internal const string k_WarningMultiThreadedMsg = "The Frame Debugger requires multi-threaded renderer. If this error persists, try starting the Editor with -force-gfx-mt command line argument.";
            internal const string k_WarningLinuxOpenGLMsg = k_WarningMultiThreadedMsg + " On Linux, the editor does not support a multi-threaded renderer when using OpenGL.";
            internal const string k_DescriptionString = "Frame Debugger lets you step through draw calls and see how exactly frame is rendered. Click Enable!";
            internal const string k_PlaymodeViewsErrorStringEditor = "Frame Debugger requires at least one available Game (Play Mode) window.\n" +
                                                               "- If no Game windows are open, please open a Game window to continue.\n" +
                                                               "- If the Frame Debugger is docked in the same tab group as a Game window, undock one of them before proceeding.";
            internal const string k_ErrorInvalidPlayerGUID = "Player GUID is invalid.";
            internal const string k_WarningPlayerNotSendingData = "No response from player. \nTry: Focus the player window, verify it's a Development Build, and check it uses MultiThreaded, LegacyJobified, or NativeGraphicsJobs threading mode.";

            internal static readonly GUIContent s_RenderTargetText = L10n.TextContent("RenderTarget", null, null, null);
            internal static readonly GUIContent s_CopyEventText = L10n.TextContent("Copy Event Info", null, null, null);
            internal static readonly GUIContent s_CopyPropertyText = L10n.TextContent("Copy Property", null, null, null);
            internal static readonly GUIContent[] s_FoldoutCopyText =
            {
                L10n.TextContent("Copy Output", null, null, null),
                L10n.TextContent("Copy All Details", null, null, null),
                L10n.TextContent("Copy All Keyword Properties", null, null, null),
                L10n.TextContent("Copy All Texture Properties", null, null, null),
                L10n.TextContent("Copy All Integer Properties", null, null, null),
                L10n.TextContent("Copy All Float Properties", null, null, null),
                L10n.TextContent("Copy All Vector Properties", null, null, null),
                L10n.TextContent("Copy All Matrix Properties", null, null, null),
                L10n.TextContent("Copy All Buffer Properties", null, null, null),
                L10n.TextContent("Copy All Constant Buffer Properties", null, null, null)
            };
            internal static readonly GUIContent s_RealShaderText = L10n.TextContent("Used Shader", "The shader used in this draw call.", null, null);
            internal static readonly GUIContent s_OriginalShaderText = L10n.TextContent("Original Shader", "The shader originally set to be used in this draw call.", null, null);
            internal static readonly GUIContent s_RayTracingShaderText = L10n.TextContent("Ray Tracing Shader", "", null, null);
            internal static readonly GUIContent s_RayTracingGenerationShaderText = L10n.TextContent("Ray Generation Shader", "", null, null);
            internal static readonly GUIContent s_ComputeShaderText = L10n.TextContent("Compute Shader", "", null, null);
            internal static readonly GUIContent s_ShadingRateImageText = L10n.TextContent("Shading Rate Image", null, null, null);
            internal static readonly GUIContent s_BatchCauseText = L10n.TextContent("Batch cause", null, null, null);
            internal static readonly GUIContent s_PassLightModeText = L10n.TextContent("Pass\nLightMode", null, null, null);
            internal static readonly GUIContent s_ArrayPopupButtonText = L10n.TextContent("...", null, null, null);
            internal static readonly GUIContent s_FoldoutOutputText = L10n.TextContent("Output", null, null, null);
            internal static readonly GUIContent s_FoldoutMeshText = L10n.TextContent("Meshes", null, null, null);
            internal static readonly GUIContent s_FoldoutMeshNotSupportedText = L10n.TextContent("Meshes - Not supported", null, null, null);
            internal static readonly GUIContent s_FoldoutEventDetailsText = L10n.TextContent("Details", null, null, null);
            internal static readonly GUIContent s_FoldoutTexturesText = L10n.TextContent("Textures", null, null, null);
            internal static readonly GUIContent s_FoldoutKeywordsText = L10n.TextContent("Keywords", null, null, null);
            internal static readonly GUIContent s_FoldoutFloatsText = L10n.TextContent("Floats", null, null, null);
            internal static readonly GUIContent s_FoldoutIntsText = L10n.TextContent("Ints", null, null, null);
            internal static readonly GUIContent s_FoldoutVectorsText = L10n.TextContent("Vectors", null, null, null);
            internal static readonly GUIContent s_FoldoutMatricesText = L10n.TextContent("Matrices", null, null, null);
            internal static readonly GUIContent s_FoldoutBuffersText = L10n.TextContent("Buffers", null, null, null);
            internal static readonly GUIContent s_FoldoutCBufferText = L10n.TextContent("Constant Buffers", null, null, null);
            internal struct DetailsSectionInfo
            {
                internal GUIContent header;
                internal string editorPrefsKey;
                internal bool defaultOpenState;

                internal DetailsSectionInfo(GUIContent header, string prefsKey, bool defaultOpen)
                {
                    this.header = header;
                    this.editorPrefsKey = prefsKey;
                    this.defaultOpenState = defaultOpen;
                }
            }

            [NoAutoStaticsCleanup] // immutable config array; GUIContent entries and string keys are code-reload-safe
            internal static readonly DetailsSectionInfo[] s_DetailsSections = new DetailsSectionInfo[]
            {
                new DetailsSectionInfo(
                    L10n.TextContent("Event Info", null, null, null),
                    "FrameDebuggerDetailsEventInfo",
                    false
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Render Target", null, null, null),
                    "FrameDebuggerDetailsRenderTarget",
                    true
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Blending", null, null, null),
                    "FrameDebuggerDetailsBlending",
                    false
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Depth & Culling", null, null, null),
                    "FrameDebuggerDetailsDepthCulling",
                    false
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Stencil", null, null, null),
                    "FrameDebuggerDetailsStencil",
                    false
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Variable Rate Shading", null, null, null),
                    "FrameDebuggerDetailsVariableRateShading",
                    false
                ),
                new DetailsSectionInfo(
                    L10n.TextContent("Shader", null, null, null),
                    "FrameDebuggerDetailsShader",
                    false
                ),
            };
            internal static readonly GUIContent s_NotAvailableText = L10n.TextContent(k_NotAvailable, null, null, null);
            [NoAutoStaticsCleanup] // programmatic Texture2D persists across incremental code reload; lifecycle managed by FrameDebuggerStyles.OnDisable
            internal static Texture2D s_RenderTargetMeshBackgroundTexture = null;
            internal static readonly string[] s_BatchBreakCauses = FrameDebuggerUtility.GetBatchBreakCauseStrings();
        }

        // Constructor
        static FrameDebuggerStyles()
        {
            float greyVal = 0.2196079f;
            EventDetails.s_RenderTargetMeshBackgroundTexture = MakeTex(1, 1, new Color(greyVal, greyVal, greyVal, 1f));
            EventDetails.s_RenderTargetMeshBackgroundStyle.normal.background = EventDetails.s_RenderTargetMeshBackgroundTexture;

            Font monospacedFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
            Font monospacedBoldFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Bold.ttf") as Font;
            EventDetails.s_MonoLabelStyle.font = monospacedFont;
            EventDetails.s_MonoLabelStylePadding.font = monospacedFont;
            EventDetails.s_MonoLabelNoWrapStyle.font = monospacedFont;
            EventDetails.s_ArrayFoldoutStyle.font = monospacedFont;

            EventDetails.s_MonoLabelBoldStyle.font = monospacedBoldFont;
            EventDetails.s_MonoLabelBoldPaddingStyle.font = monospacedBoldFont;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        internal static void OnDisable()
        {
            UnityEngine.Object.DestroyImmediate(EventDetails.s_RenderTargetMeshBackgroundTexture);
            EventDetails.s_RenderTargetMeshBackgroundTexture = null;
        }
    }
}
