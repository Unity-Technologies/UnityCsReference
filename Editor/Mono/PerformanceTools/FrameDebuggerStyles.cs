// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

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
            "Configure Foveated Rendering",
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
            internal static readonly GUIContent s_RecordButtonEnable = EditorGUIUtility.TrTextContent(L10n.Tr("Enable"));
            internal static readonly GUIContent s_RecordButtonDisable = EditorGUIUtility.TrTextContent(L10n.Tr("Disable"));
            internal static readonly GUIContent s_PrevFrame = EditorGUIUtility.TrIconContent("Profiler.PrevFrame", "Go back one frame");
            internal static readonly GUIContent s_NextFrame = EditorGUIUtility.TrIconContent("Profiler.NextFrame", "Go one frame forwards");
            internal static readonly GUIContent s_LevelsHeader = EditorGUIUtility.TrTextContent("Levels", "Render target display black/white intensity levels");
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
            internal static readonly GUIStyle s_ChannelStyle = new GUIStyle(EditorStyles.miniButtonMid)
            {
                fixedWidth = k_ChannelButtonWidth,
            };
            internal static readonly GUIStyle s_ChannelAllStyle = new GUIStyle(EditorStyles.miniButtonLeft)
            {
                fixedWidth = k_ChannelButtonWidth,
            };
            internal static readonly GUIStyle s_ChannelAStyle = new GUIStyle(EditorStyles.miniButtonRight)
            {
                fixedWidth = k_ChannelButtonWidth,
            };
            internal static readonly GUIStyle s_PopupLeftStyle = new GUIStyle(EditorStyles.toolbarPopupLeft)
            {
                fixedHeight = k_ToolbarHeight
            };
            internal static readonly GUIStyle s_LevelsHorizontalStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                margin = new RectOffset(4, 4, 0, 0),
                padding = new RectOffset(4, 4, 0, 0),
                fixedHeight = k_ToolbarHeight
            };

            internal static readonly GUIContent s_DepthLabel = EditorGUIUtility.TrTextContent("Depth", "Show depth buffer");
            internal static readonly GUIContent s_StencilLabel = EditorGUIUtility.TrTextContent("Stencil", "Show stencil buffer");
            internal static readonly GUIContent s_ChannelHeader = EditorGUIUtility.TrTextContent("Channels", "Which render target color channels to show");
            internal static readonly GUIContent s_ChannelAll = EditorGUIUtility.TrTextContent("All");
            internal static readonly GUIContent s_ChannelR = EditorGUIUtility.TrTextContent("R");
            internal static readonly GUIContent s_ChannelG = EditorGUIUtility.TrTextContent("G");
            internal static readonly GUIContent s_ChannelB = EditorGUIUtility.TrTextContent("B");
            internal static readonly GUIContent s_ChannelA = EditorGUIUtility.TrTextContent("A");
            internal static readonly GUIContent s_LevelsHeader = EditorGUIUtility.TrTextContent("Levels", "Render target display black/white intensity levels");
            internal static readonly GUIContent[] s_MRTLabels = new[]
            {
                EditorGUIUtility.TrTextContent("RT 0", "Show render target #0"),
                EditorGUIUtility.TrTextContent("RT 1", "Show render target #1"),
                EditorGUIUtility.TrTextContent("RT 2", "Show render target #2"),
                EditorGUIUtility.TrTextContent("RT 3", "Show render target #3"),
                EditorGUIUtility.TrTextContent("RT 4", "Show render target #4"),
                EditorGUIUtility.TrTextContent("RT 5", "Show render target #5"),
                EditorGUIUtility.TrTextContent("RT 6", "Show render target #6"),
                EditorGUIUtility.TrTextContent("RT 7", "Show render target #7")
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

            internal static string s_DashesString = new string('-', 30);
            internal static string s_EqualsString = new string('=', 30);


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
            internal const string k_TabbedWithPlaymodeErrorString = "Frame Debugger can not be docked with the Game Window when trying to debug the editor.";
            internal static readonly GUIContent s_RenderTargetText = EditorGUIUtility.TrTextContent("RenderTarget");
            internal static readonly GUIContent s_CopyEventText = EditorGUIUtility.TrTextContent("Copy Event Info");
            internal static readonly GUIContent s_CopyPropertyText = EditorGUIUtility.TrTextContent("Copy Property");
            internal static readonly GUIContent[] s_FoldoutCopyText =
            {
                EditorGUIUtility.TrTextContent("Copy Output"),
                EditorGUIUtility.TrTextContent("Copy All Details"),
                EditorGUIUtility.TrTextContent("Copy All Keyword Properties"),
                EditorGUIUtility.TrTextContent("Copy All Texture Properties"),
                EditorGUIUtility.TrTextContent("Copy All Integer Properties"),
                EditorGUIUtility.TrTextContent("Copy All Float Properties"),
                EditorGUIUtility.TrTextContent("Copy All Vector Properties"),
                EditorGUIUtility.TrTextContent("Copy All Matrix Properties"),
                EditorGUIUtility.TrTextContent("Copy All Buffer Properties"),
                EditorGUIUtility.TrTextContent("Copy All Constant Buffer Properties")
            };
            internal static readonly GUIContent s_RealShaderText = EditorGUIUtility.TrTextContent("Used Shader", "The shader used in this draw call.");
            internal static readonly GUIContent s_OriginalShaderText = EditorGUIUtility.TrTextContent("Original Shader", "The shader originally set to be used in this draw call.");
            internal static readonly GUIContent s_RayTracingShaderText = EditorGUIUtility.TrTextContent("Ray Tracing Shader", "");
            internal static readonly GUIContent s_RayTracingGenerationShaderText = EditorGUIUtility.TrTextContent("Ray Generation Shader", "");
            internal static readonly GUIContent s_ComputeShaderText = EditorGUIUtility.TrTextContent("Compute Shader", "");
            internal static readonly GUIContent s_BatchCauseText = EditorGUIUtility.TrTextContent("Batch cause");
            internal static readonly GUIContent s_PassLightModeText = EditorGUIUtility.TrTextContent("Pass\nLightMode");
            internal static readonly GUIContent s_ArrayPopupButtonText = EditorGUIUtility.TrTextContent("...");
            internal static readonly GUIContent s_FoldoutOutputText = EditorGUIUtility.TrTextContent("Output");
            internal static readonly GUIContent s_FoldoutMeshText = EditorGUIUtility.TrTextContent("Meshes");
            internal static readonly GUIContent s_FoldoutMeshNotSupportedText = EditorGUIUtility.TrTextContent("Meshes - Not supported");
            internal static readonly GUIContent s_FoldoutEventDetailsText = EditorGUIUtility.TrTextContent("Details");
            internal static readonly GUIContent s_FoldoutTexturesText = EditorGUIUtility.TrTextContent("Textures");
            internal static readonly GUIContent s_FoldoutKeywordsText = EditorGUIUtility.TrTextContent("Keywords");
            internal static readonly GUIContent s_FoldoutFloatsText = EditorGUIUtility.TrTextContent("Floats");
            internal static readonly GUIContent s_FoldoutIntsText = EditorGUIUtility.TrTextContent("Ints");
            internal static readonly GUIContent s_FoldoutVectorsText = EditorGUIUtility.TrTextContent("Vectors");
            internal static readonly GUIContent s_FoldoutMatricesText = EditorGUIUtility.TrTextContent("Matrices");
            internal static readonly GUIContent s_FoldoutBuffersText = EditorGUIUtility.TrTextContent("Buffers");
            internal static readonly GUIContent s_FoldoutCBufferText = EditorGUIUtility.TrTextContent("Constant Buffers");
            internal static readonly GUIContent s_NotAvailableText = EditorGUIUtility.TrTextContent(k_NotAvailable);
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
