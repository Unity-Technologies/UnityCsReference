// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal static class FrameDebuggerStyles
    {
        // match enum FrameEventType on C++ side!
        public static readonly string[] frameEventTypeNames = new[]
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
        public struct Window
        {
            public const int k_StartWindowWidth = 1024;
            public const float k_MinTreeWidth = k_StartWindowWidth * 0.33f;
            public const float k_ResizerWidth = 5f;
            public const float k_MinDetailsWidth = 200f;
        }

        // Tree
        public struct Tree
        {
            public static readonly GUIStyle rowText = "OL Label";
            public static readonly GUIStyle rowTextRight = "OL RightLabel";
            public const string k_UnknownScopeString = "<unknown scope>";
        }

        // Top Toolbar
        public struct TopToolbar
        {
            public static readonly GUIContent recordButtonEnable = EditorGUIUtility.TrTextContent(L10n.Tr("Enable"));
            public static readonly GUIContent recordButtonDisable = EditorGUIUtility.TrTextContent(L10n.Tr("Disable"));
            public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Profiler.PrevFrame", "Go back one frame");
            public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Profiler.NextFrame", "Go one frame forwards");
            public static readonly GUIContent levelsHeader = EditorGUIUtility.TrTextContent("Levels", "Render target display black/white intensity levels");
        }

        // Event Toolbar in the Event Details window
        public struct EventToolbar
        {
            private const float k_ToolbarHeight = 22f;
            private const float k_ToolbarButtondWidth = 30f;

            public static readonly GUIStyle toolbarHorizontalStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = k_ToolbarHeight + 1f
            };
            public static readonly GUIStyle channelHeaderStyle = new GUIStyle(EditorStyles.toolbarLabel)
            {
                fixedHeight = k_ToolbarHeight
            };
            public static readonly GUIStyle channelStyle = new GUIStyle(EditorStyles.miniButtonMid)
            {
                fixedWidth = k_ToolbarButtondWidth,
            };
            public static readonly GUIStyle channelAllStyle = new GUIStyle(EditorStyles.miniButtonLeft)
            {
                fixedWidth = k_ToolbarButtondWidth,
            };
            public static readonly GUIStyle channelAStyle = new GUIStyle(EditorStyles.miniButtonRight)
            {
                fixedWidth = k_ToolbarButtondWidth,
            };
            public static readonly GUIStyle popupLeftStyle = new GUIStyle(EditorStyles.toolbarPopupLeft)
            {
                fixedHeight = k_ToolbarHeight
            };
            public static readonly GUIStyle levelsHorizontalStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                margin = new RectOffset(4, 4, 0, 0),
                padding = new RectOffset(4, 4, 0, 0),
                fixedHeight = k_ToolbarHeight
            };

            public static readonly GUIContent depthLabel = EditorGUIUtility.TrTextContent("Depth", "Show depth buffer");
            public static readonly GUIContent channelHeader = EditorGUIUtility.TrTextContent("Channels", "Which render target color channels to show");
            public static readonly GUIContent channelAll = EditorGUIUtility.TrTextContent("All");
            public static readonly GUIContent channelR = EditorGUIUtility.TrTextContent("R");
            public static readonly GUIContent channelG = EditorGUIUtility.TrTextContent("G");
            public static readonly GUIContent channelB = EditorGUIUtility.TrTextContent("B");
            public static readonly GUIContent channelA = EditorGUIUtility.TrTextContent("A");
            public static readonly GUIContent levelsHeader = EditorGUIUtility.TrTextContent("Levels", "Render target display black/white intensity levels");
            public static readonly GUIContent[] MRTLabels = new[]
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
        public struct EventDetails
        {
            private const int k_Indent1 = 5;
            private const int k_Indent2 = 20;

            public const float k_MaxViewportHeight = 355f;
            public const float k_MaxViewportWidth = 125f;

            public const int k_PropertyNameMaxChars = 35;
            public const int k_TextureFormatMaxChars = 16;


            public const float k_MeshBottomToolbarHeight = 21f;
            public const float k_ArrayValuePopupBtnWidth = 2.0f;

            public const string k_FloatFormat = "g7";
            public const string k_IntFormat = "d";
            public const string k_NotAvailable = "-";



            public static readonly GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                padding = new RectOffset(k_Indent1, 0, k_Indent1, 0),
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                fixedHeight = 50,
            };
            public static readonly GUIStyle foldoutCategoryBoxStyle = new GUIStyle(EditorStyles.helpBox);

            public static readonly GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
            };
            public static readonly GUIStyle titleHorizontalStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 10),
            };

            public static readonly GUIStyle verticalLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 175,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            public static readonly GUIStyle verticalValueStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 250,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            public static readonly GUIStyle outputMeshTabStyle = new GUIStyle("LargeButton")
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(-2, 0, 0, 0),
            };
            public static readonly GUIStyle outputMeshTextureStyle = new GUIStyle();
            public static readonly GUIStyle meshPreToolbarStyle = "toolbar";
            public static readonly GUIStyle meshPreToolbarLabelStyle = EditorStyles.toolbarButton;

            public static readonly GUIStyle propertiesVerticalStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(k_Indent2, 0, 0, 7)
            };
            public static readonly GUIStyle propertiesNameStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 215,
            };
            public static readonly GUIStyle propertiesFlagsStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 55f,
            };

            public static readonly GUIStyle textureButtonStyle = new GUIStyle()
            {
                fixedWidth = 12f,
                fixedHeight = 12f,
            };
            public static readonly GUIStyle textureDimensionsStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 65f,
            };
            public static readonly GUIStyle textureSizeStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 100f,
            };
            public static readonly GUIStyle textureFormatStyle = new GUIStyle(EditorStyles.label)
            {
                fixedWidth = 110f,
            };

            public const string warningMultiThreadedMsg = "The Frame Debugger requires multi-threaded renderer. If this error persists, try starting the Editor with -force-gfx-mt command line argument.";
            public const string warningLinuxOpenGLMsg = warningMultiThreadedMsg + " On Linux, the editor does not support a multi-threaded renderer when using OpenGL.";
            public const string descriptionString = "Frame Debugger lets you step through draw calls and see how exactly frame is rendered. Click Enable!";
            public static readonly GUIContent copyValueText = EditorGUIUtility.TrTextContent("Copy value");
            public static readonly GUIContent shaderText = EditorGUIUtility.TrTextContent("Shader");
            public static readonly GUIContent batchCauseText = EditorGUIUtility.TrTextContent("Batch cause");
            public static readonly GUIContent passLightModeText = EditorGUIUtility.TrTextContent("Pass\nLightMode");
            public static readonly GUIContent arrayPopupButtonText = EditorGUIUtility.TrTextContent("...");
            public static readonly GUIContent foldoutOutputOrMeshText = EditorGUIUtility.TrTextContent("Output / Mesh");
            public static readonly GUIContent foldoutEventDetailsText = EditorGUIUtility.TrTextContent("Details");
            public static readonly GUIContent foldoutTexturesText = EditorGUIUtility.TrTextContent("Textures");
            public static readonly GUIContent foldoutKeywordsText = EditorGUIUtility.TrTextContent("Keywords");
            public static readonly GUIContent foldoutFloatsText = EditorGUIUtility.TrTextContent("Floats");
            public static readonly GUIContent foldoutIntsText = EditorGUIUtility.TrTextContent("Ints");
            public static readonly GUIContent foldoutVectorsText = EditorGUIUtility.TrTextContent("Vectors");
            public static readonly GUIContent foldoutMatricesText = EditorGUIUtility.TrTextContent("Matrices");
            public static readonly GUIContent foldoutBuffersText = EditorGUIUtility.TrTextContent("Buffers");
            public static readonly GUIContent foldoutCBufferText = EditorGUIUtility.TrTextContent("CBuffer");

            public static Texture2D outputMeshTexture = null;
            public static readonly string[] batchBreakCauses = FrameDebuggerUtility.GetBatchBreakCauseStrings();
        }

        // Constructor
        static FrameDebuggerStyles()
        {
            float greyVal = 0.2196079f;
            EventDetails.outputMeshTexture = MakeTex(1, 1, new Color(greyVal, greyVal, greyVal, 1f));
            EventDetails.outputMeshTextureStyle.normal.background = EventDetails.outputMeshTexture;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        public static void OnDisable()
        {
            UnityEngine.Object.DestroyImmediate(EventDetails.outputMeshTexture);
            EventDetails.outputMeshTexture = null;
        }

        public class ArrayValuePopup : PopupWindowContent
        {
            public delegate string GetValueStringDelegate(int index, bool highPrecision);
            private GetValueStringDelegate GetValueString;
            private Vector2 m_ScrollPos = Vector2.zero;
            private int m_StartIndex;
            private int m_NumValues;
            private float m_WindowWidth;
            private int m_RowCount;
            private static readonly GUIStyle m_Style = EditorStyles.miniLabel;

            public ArrayValuePopup(int startIndex, int numValues, int rowCount, float windowWidth, GetValueStringDelegate getValueString)
            {
                m_StartIndex = startIndex;
                m_NumValues = numValues;
                m_WindowWidth = windowWidth;
                m_RowCount = rowCount;
                GetValueString = getValueString;
            }

            public override Vector2 GetWindowSize()
            {
                float lineHeight = m_Style.lineHeight + m_Style.padding.vertical + m_Style.margin.top;
                return new Vector2(m_WindowWidth, Math.Min(lineHeight * m_NumValues * m_RowCount, 250.0f));
            }

            public override void OnGUI(Rect rect)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

                for (int i = 0; i < m_NumValues; ++i)
                {
                    string text = $"[{i}]\t{GetValueString(m_StartIndex + i, false)}";
                    GUILayout.Label(text, m_Style);
                }

                EditorGUILayout.EndScrollView();

                // Right click to copy the values to clipboard.
                var e = Event.current;
                if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
                {
                    e.Use();

                    string allText = string.Empty;
                    for (int i = 0; i < m_NumValues; ++i)
                    {
                        allText += $"[{i}]\t{GetValueString(m_StartIndex + i, true)}";
                    }

                    var menu = new GenericMenu();
                    menu.AddItem(FrameDebuggerStyles.EventDetails.copyValueText, false, delegate
                    {
                        EditorGUIUtility.systemCopyBuffer = allText;
                    });
                    menu.ShowAsContext();
                }
            }
        }
    }
}
