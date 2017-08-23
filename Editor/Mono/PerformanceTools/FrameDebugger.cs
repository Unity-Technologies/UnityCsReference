// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEditorInternal
{
    // match enum FrameEventType on C++ side!
    // match kFrameEventTypeNames names array!
    internal enum FrameEventType
    {
        // ReSharper disable InconsistentNaming
        ClearNone = 0,
        ClearColor,
        ClearDepth,
        ClearColorDepth,
        ClearStencil,
        ClearColorStencil,
        ClearDepthStencil,
        ClearAll,
        SetRenderTarget,
        ResolveRT,
        ResolveDepth,
        GrabIntoRT,
        StaticBatch,
        DynamicBatch,
        Mesh,
        DynamicGeometry,
        GLDraw,
        SkinOnGPU,
        DrawProcedural,
        ComputeDispatch,
        PluginEvent,
        InstancedMesh
        // ReSharper restore InconsistentNaming
    };

    internal enum ShowAdditionalInfo
    {
        Preview,
        ShaderProperties
    };

    // Match C++ ScriptingShaderFloatInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderFloatInfo
    {
        public string name;
        public int flags;
        public float value;
    }

    // Match C++ ScriptingShaderVectorInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderVectorInfo
    {
        public string name;
        public int flags;
        public Vector4 value;
    }

    // Match C++ ScriptingShaderMatrixInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderMatrixInfo
    {
        public string name;
        public int flags;
        public Matrix4x4 value;
    }

    // Match C++ ScriptingShaderTextureInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderTextureInfo
    {
        public string name;
        public int flags;
        public string textureName;
        public Texture value;
    }

    // Match C++ ScriptingShaderBufferInfo memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderBufferInfo
    {
        public string name;
        public int flags;
    }

    // Match C++ ScriptingShaderProperties memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShaderProperties
    {
        public ShaderFloatInfo[] floats;
        public ShaderVectorInfo[] vectors;
        public ShaderMatrixInfo[] matrices;
        public ShaderTextureInfo[] textures;
        public ShaderBufferInfo[] buffers;
    }

    // Match C++ ScriptingFrameDebuggerEventData memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerEventData
    {
        public int frameEventIndex;
        public int vertexCount;
        public int indexCount;
        public int instanceCount;
        public string shaderName;
        public string passName;
        public string passLightMode;
        public int shaderInstanceID;
        public int subShaderIndex;
        public int shaderPassIndex;
        public string shaderKeywords;
        public int rendererInstanceID;
        public Mesh mesh;
        public int meshInstanceID;
        public int meshSubset;

        // state for compute shader dispatches
        public int csInstanceID;
        public string csName;
        public string csKernel;
        public int csThreadGroupsX;
        public int csThreadGroupsY;
        public int csThreadGroupsZ;

        // active render target info
        public string rtName;
        public int rtWidth;
        public int rtHeight;
        public int rtFormat;
        public int rtDim;
        public int rtFace;
        public short rtCount;
        public short rtHasDepthTexture;

        // shader state and properties
        public FrameDebuggerBlendState blendState;
        public FrameDebuggerRasterState rasterState;
        public FrameDebuggerDepthState depthState;
        public FrameDebuggerStencilState stencilState;
        public int stencilRef;
        public int batchBreakCause;

        public ShaderProperties shaderProperties;
    }

    // Match C++ MonoFrameDebuggerEvent memory layout!
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameDebuggerEvent
    {
        public FrameEventType type;
        public GameObject gameObject;
    }

    // Match C++ ScriptingFrameDebuggerBlendState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FrameDebuggerBlendState
    {
        public uint writeMask;
        public BlendMode srcBlend;
        public BlendMode dstBlend;
        public BlendMode srcBlendAlpha;
        public BlendMode dstBlendAlpha;
        public BlendOp blendOp;
        public BlendOp blendOpAlpha;
    }

    // Match C++ ScriptingFrameDebuggerRasterState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct FrameDebuggerRasterState
    {
        public CullMode cullMode;
        public int depthBias;
        public bool depthClip;
        public float slopeScaledDepthBias;
    };

    // Match C++ ScriptingFrameDebuggerDepthState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct FrameDebuggerDepthState
    {
        public int depthWrite;
        public CompareFunction depthFunc;
    };

    // Match C++ ScriptingFrameDebuggerStencilState memory layout!
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct FrameDebuggerStencilState
    {
        public bool stencilEnable;
        public byte readMask;
        public byte writeMask;
        public byte padding;
        public CompareFunction stencilFuncFront;
        public StencilOp stencilPassOpFront;
        public StencilOp stencilFailOpFront;
        public StencilOp stencilZFailOpFront;
        public CompareFunction stencilFuncBack;
        public StencilOp stencilPassOpBack;
        public StencilOp stencilFailOpBack;
        public StencilOp stencilZFailOpBack;
    };
}

namespace UnityEditor
{
    internal class FrameDebuggerWindow : EditorWindow
    {
        // match enum FrameEventType on C++ side!
        static public readonly string[] s_FrameEventTypeNames = new[]
        {
            "Clear (nothing)",
            "Clear (color)",
            "Clear (Z)",
            "Clear (color+Z)",
            "Clear (stencil)",
            "Clear (color+stencil)",
            "Clear (Z+stencil)",
            "Clear (color+Z+stencil)",
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
            "Compute Shader",
            "Plugin Event",
            "Draw Mesh (instanced)"
        };

        // Cached strings built from FrameDebuggerEventData.
        // Only need to rebuild them when event data actually changes.
        private struct EventDataStrings
        {
            public string shader;
            public string pass;

            public string stencilRef;
            public string stencilReadMask;
            public string stencilWriteMask;
            public string stencilComp;
            public string stencilPass;
            public string stencilFail;
            public string stencilZFail;

            public string[] texturePropertyTooltips;
        }

        const float kScrollbarWidth = 16;
        const float kResizerWidth = 5f;
        const float kMinListWidth = 200f;
        const float kMinDetailsWidth = 200f;
        const float kMinWindowWidth = 240f;
        const float kDetailsMargin = 4f;
        const float kMinPreviewSize = 64f;

        const string kFloatFormat = "g2";
        const string kFloatDetailedFormat = "g7";

        const float kShaderPropertiesIndention = 15.0f;
        const float kNameFieldWidth = 200.0f;
        const float kValueFieldWidth = 200.0f;
        const float kArrayValuePopupBtnWidth = 25.0f;

        // See the comments for BaseParamInfo in FrameDebuggerInternal.h
        const int kShaderTypeBits = 6;
        const int kArraySizeBitMask = 0x3FF;

        // Sometimes when disabling the frame debugger, the UI does not update automatically -
        // the repaint happens, but we still think there's zero events present
        // (on Mac at least). Haven't figured out why, so whenever changing the
        // enable/limit state, just repaint a couple of times. Yeah...
        const int kNeedToRepaintFrames = 4;


        [SerializeField]
        float   m_ListWidth = kMinListWidth * 1.5f;

        private int m_RepaintFrames = kNeedToRepaintFrames;

        // Mesh preview
        PreviewRenderUtility m_PreviewUtility;
        public Vector2 m_PreviewDir = new Vector2(120, -20);
        private Material m_Material;
        private Material m_WireMaterial;

        // Frame events tree view
        [SerializeField]
        TreeViewState m_TreeViewState;
        [NonSerialized]
        FrameDebuggerTreeView m_Tree;
        [NonSerialized] private int m_FrameEventsHash;

        // Render target view options
        [NonSerialized] int m_RTIndex;
        [NonSerialized] int m_RTChannel;

        [NonSerialized] private float m_RTBlackLevel;
        [NonSerialized] private float m_RTWhiteLevel = 1.0f;

        private int m_PrevEventsLimit = 0;
        private int m_PrevEventsCount = 0;

        private FrameDebuggerEventData  m_CurEventData;
        private uint                    m_CurEventDataHash = 0;
        private EventDataStrings        m_CurEventDataStrings;

        // Shader Properties
        private Vector2 m_ScrollViewShaderProps = Vector2.zero;

        private ShowAdditionalInfo m_AdditionalInfo = ShowAdditionalInfo.ShaderProperties;
        private GUIContent[] m_AdditionalInfoGuiContents = Enum.GetNames(typeof(ShowAdditionalInfo)).Select(m => new GUIContent(m)).ToArray();

        static List<FrameDebuggerWindow> s_FrameDebuggers = new List<FrameDebuggerWindow>();

        private AttachProfilerUI m_AttachProfilerUI = new AttachProfilerUI();

        [MenuItem("Window/Frame Debugger", false, 2100)]
        public static FrameDebuggerWindow ShowFrameDebuggerWindow()
        {
            var wnd = GetWindow(typeof(FrameDebuggerWindow)) as FrameDebuggerWindow;
            if (wnd != null)
            {
                wnd.titleContent = EditorGUIUtility.TextContent("Frame Debug");
            }
            return wnd;
        }

        internal static void RepaintAll()
        {
            foreach (var fd in s_FrameDebuggers)
            {
                fd.Repaint();
            }
        }

        public FrameDebuggerWindow()
        {
            position = new Rect(50, 50, (kMinListWidth + kMinDetailsWidth) * 1.5f, 350f);
            minSize = new Vector2(kMinListWidth + kMinDetailsWidth, 200);
        }

        internal void ChangeFrameEventLimit(int newLimit)
        {
            if (newLimit <= 0 || newLimit > FrameDebuggerUtility.count)
            {
                return;
            }

            if (newLimit != FrameDebuggerUtility.limit && newLimit > 0)
            {
                GameObject go = FrameDebuggerUtility.GetFrameEventGameObject(newLimit - 1);
                if (go != null)
                    EditorGUIUtility.PingObject(go);
            }

            FrameDebuggerUtility.limit = newLimit;
            if (m_Tree != null)
                m_Tree.SelectFrameEventIndex(newLimit);
        }

        static void DisableFrameDebugger()
        {
            if (FrameDebuggerUtility.IsLocalEnabled())
            {
                // if it was true before, we disabled and ask the game scene to repaint
                EditorApplication.SetSceneRepaintDirty();
            }

            FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());
        }

        internal void OnDidOpenScene()
        {
            DisableFrameDebugger();
        }

        void OnPauseStateChanged(PauseState state)
        {
            RepaintOnLimitChange();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RepaintOnLimitChange();
        }

        internal void OnEnable()
        {
            autoRepaintOnSceneChange = true;
            s_FrameDebuggers.Add(this);
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            m_RepaintFrames = kNeedToRepaintFrames;
        }

        internal void OnDisable()
        {
            if (m_WireMaterial != null)
            {
                DestroyImmediate(m_WireMaterial, true);
            }
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            s_FrameDebuggers.Remove(this);
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DisableFrameDebugger();
        }

        public void EnableIfNeeded()
        {
            if (FrameDebuggerUtility.IsLocalEnabled() || FrameDebuggerUtility.IsRemoteEnabled())
                return;
            m_RTChannel = 0;
            m_RTIndex = 0;
            m_RTBlackLevel = 0.0f;
            m_RTWhiteLevel = 1.0f;
            ClickEnableFrameDebugger();
            RepaintOnLimitChange();
        }

        private void ClickEnableFrameDebugger()
        {
            bool isEnabled = FrameDebuggerUtility.IsLocalEnabled() || FrameDebuggerUtility.IsRemoteEnabled();

            bool enablingLocally = !isEnabled && m_AttachProfilerUI.IsEditor();

            if (enablingLocally && !FrameDebuggerUtility.locallySupported)
                return;

            if (enablingLocally)
            {
                // pause play mode if needed
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                    EditorApplication.isPaused = true;
            }

            if (!isEnabled)
                FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);
            else
                FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());

            // Make sure game view is visible when enabling frame debugger locally
            if (FrameDebuggerUtility.IsLocalEnabled())
            {
                GameView gameView = (GameView)WindowLayout.FindEditorWindowOfType(typeof(GameView));
                if (gameView)
                {
                    gameView.ShowTab();
                }
            }

            m_PrevEventsLimit = FrameDebuggerUtility.limit;
            m_PrevEventsCount = FrameDebuggerUtility.count;
        }

        // Only call this when m_CurEventData changes.
        void BuildCurEventDataStrings()
        {
            // shader name & subshader index
            m_CurEventDataStrings.shader = string.Format("{0}, SubShader #{1}", m_CurEventData.shaderName, m_CurEventData.subShaderIndex.ToString());

            // pass name & LightMode tag
            string passName = string.IsNullOrEmpty(m_CurEventData.passName) ? "#" + m_CurEventData.shaderPassIndex.ToString() : m_CurEventData.passName;
            string lightMode = string.IsNullOrEmpty(m_CurEventData.passLightMode) ? "" : string.Format(" ({0})", m_CurEventData.passLightMode);
            m_CurEventDataStrings.pass = passName + lightMode;

            // stencil states
            if (m_CurEventData.stencilState.stencilEnable)
            {
                m_CurEventDataStrings.stencilRef = m_CurEventData.stencilRef.ToString();

                if (m_CurEventData.stencilState.readMask != 255)
                    m_CurEventDataStrings.stencilReadMask = m_CurEventData.stencilState.readMask.ToString();

                if (m_CurEventData.stencilState.writeMask != 255)
                    m_CurEventDataStrings.stencilWriteMask = m_CurEventData.stencilState.writeMask.ToString();

                // Only show *Front states when CullMode is set to Back.
                // Only show *Back states when CullMode is set to Front.
                // Show both *Front and *Back states for two-sided geometry.
                if (m_CurEventData.rasterState.cullMode == CullMode.Back)
                {
                    m_CurEventDataStrings.stencilComp = m_CurEventData.stencilState.stencilFuncFront.ToString();
                    m_CurEventDataStrings.stencilPass = m_CurEventData.stencilState.stencilPassOpFront.ToString();
                    m_CurEventDataStrings.stencilFail = m_CurEventData.stencilState.stencilFailOpFront.ToString();
                    m_CurEventDataStrings.stencilZFail = m_CurEventData.stencilState.stencilZFailOpFront.ToString();
                }
                else if (m_CurEventData.rasterState.cullMode == CullMode.Front)
                {
                    m_CurEventDataStrings.stencilComp = m_CurEventData.stencilState.stencilFuncBack.ToString();
                    m_CurEventDataStrings.stencilPass = m_CurEventData.stencilState.stencilPassOpBack.ToString();
                    m_CurEventDataStrings.stencilFail = m_CurEventData.stencilState.stencilFailOpBack.ToString();
                    m_CurEventDataStrings.stencilZFail = m_CurEventData.stencilState.stencilZFailOpBack.ToString();
                }
                else
                {
                    m_CurEventDataStrings.stencilComp =
                        string.Format("{0} {1}", m_CurEventData.stencilState.stencilFuncFront.ToString(), m_CurEventData.stencilState.stencilFuncBack.ToString());

                    m_CurEventDataStrings.stencilPass =
                        string.Format("{0} {1}", m_CurEventData.stencilState.stencilPassOpFront.ToString(), m_CurEventData.stencilState.stencilPassOpBack.ToString());

                    m_CurEventDataStrings.stencilFail =
                        string.Format("{0} {1}", m_CurEventData.stencilState.stencilFailOpFront.ToString(), m_CurEventData.stencilState.stencilFailOpBack.ToString());

                    m_CurEventDataStrings.stencilZFail =
                        string.Format("{0} {1}", m_CurEventData.stencilState.stencilZFailOpFront.ToString(), m_CurEventData.stencilState.stencilZFailOpBack.ToString());
                }
            }

            // texture property tooltips
            ShaderTextureInfo[] textureInfoArray = m_CurEventData.shaderProperties.textures;
            m_CurEventDataStrings.texturePropertyTooltips = new string[textureInfoArray.Length];
            StringBuilder tooltip = new StringBuilder();

            for (int i = 0; i < textureInfoArray.Length; ++i)
            {
                Texture texture = textureInfoArray[i].value;
                if (texture == null)
                    continue;

                tooltip.Clear();
                tooltip.AppendFormat("Size: {0} x {1}", texture.width.ToString(), texture.height.ToString());
                tooltip.AppendFormat("\nDimension: {0}", texture.dimension.ToString());

                string formatFormat = "\nFormat: {0}";
                string depthFormat = "\nDepth: {0}";

                if (texture is Texture2D)
                    tooltip.AppendFormat(formatFormat, (texture as Texture2D).format.ToString());
                else if (texture is Cubemap)
                    tooltip.AppendFormat(formatFormat, (texture as Cubemap).format.ToString());
                else if (texture is Texture2DArray)
                {
                    tooltip.AppendFormat(formatFormat, (texture as Texture2DArray).format.ToString());
                    tooltip.AppendFormat(depthFormat, (texture as Texture2DArray).depth.ToString());
                }
                else if (texture is Texture3D)
                {
                    tooltip.AppendFormat(formatFormat, (texture as Texture3D).format.ToString());
                    tooltip.AppendFormat(depthFormat, (texture as Texture3D).depth.ToString());
                }
                else if (texture is CubemapArray)
                {
                    tooltip.AppendFormat(formatFormat, (texture as CubemapArray).format.ToString());
                    tooltip.AppendFormat("\nCubemap Count: {0}", (texture as CubemapArray).cubemapCount.ToString());
                }
                else if (texture is RenderTexture)
                {
                    tooltip.AppendFormat("\nRT Format: {0}", (texture as RenderTexture).format.ToString());
                }

                tooltip.Append("\n\nCtrl + Click to show preview");

                m_CurEventDataStrings.texturePropertyTooltips[i] = tooltip.ToString();
            }
        }

        // Return true if should repaint
        private bool DrawToolbar(FrameDebuggerEvent[] descs)
        {
            bool repaint = false;

            bool isSupported = !m_AttachProfilerUI.IsEditor() || FrameDebuggerUtility.locallySupported;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            // enable toggle
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(!isSupported))
            {
                GUILayout.Toggle(FrameDebuggerUtility.IsLocalEnabled() || FrameDebuggerUtility.IsRemoteEnabled(), styles.recordButton, EditorStyles.toolbarButton, GUILayout.MinWidth(80));
            }
            if (EditorGUI.EndChangeCheck())
            {
                ClickEnableFrameDebugger();
                repaint = true;
            }

            m_AttachProfilerUI.OnGUILayout(this);

            bool isAnyEnabled = FrameDebuggerUtility.IsLocalEnabled() || FrameDebuggerUtility.IsRemoteEnabled();
            if (isAnyEnabled && ProfilerDriver.connectedProfiler != FrameDebuggerUtility.GetRemotePlayerGUID())
            {
                // Switch from local to remote debugger or vice versa
                FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());
                FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);
            }

            GUI.enabled = isAnyEnabled;

            // event limit slider
            EditorGUI.BeginChangeCheck();
            int newLimit;
            using (new EditorGUI.DisabledScope(FrameDebuggerUtility.count <= 1))
            {
                newLimit = EditorGUILayout.IntSlider(FrameDebuggerUtility.limit, 1, FrameDebuggerUtility.count);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ChangeFrameEventLimit(newLimit);
            }
            GUILayout.Label(" of " + FrameDebuggerUtility.count, EditorStyles.miniLabel);
            // prev/next buttons
            using (new EditorGUI.DisabledScope(newLimit <= 1))
            {
                if (GUILayout.Button(styles.prevFrame, EditorStyles.toolbarButton))
                {
                    ChangeFrameEventLimit(newLimit - 1);
                }
            }
            using (new EditorGUI.DisabledScope(newLimit >= FrameDebuggerUtility.count))
            {
                if (GUILayout.Button(styles.nextFrame, EditorStyles.toolbarButton))
                {
                    ChangeFrameEventLimit(newLimit + 1);
                }
                // If we had last event selected, and something changed in the scene so that
                // number of events is different - then try to keep the last event selected.
                if (m_PrevEventsLimit == m_PrevEventsCount)
                {
                    if (FrameDebuggerUtility.count != m_PrevEventsCount && FrameDebuggerUtility.limit == m_PrevEventsLimit)
                    {
                        ChangeFrameEventLimit(FrameDebuggerUtility.count);
                    }
                }
                m_PrevEventsLimit = FrameDebuggerUtility.limit;
                m_PrevEventsCount = FrameDebuggerUtility.count;
            }

            GUILayout.EndHorizontal();

            return repaint;
        }

        private void DrawMeshPreview(Rect previewRect, Rect meshInfoRect, Mesh mesh, int meshSubset)
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_PreviewUtility.camera.fieldOfView = 30.0f;
            }
            if (m_Material == null)
                m_Material = EditorGUIUtility.GetBuiltinExtraResource(typeof(Material), "Default-Material.mat") as Material;
            if (m_WireMaterial == null)
            {
                m_WireMaterial = ModelInspector.CreateWireframeMaterial();
            }

            m_PreviewUtility.BeginPreview(previewRect, "preBackground");

            ModelInspector.RenderMeshPreview(mesh, m_PreviewUtility, m_Material, m_WireMaterial, m_PreviewDir, meshSubset);

            m_PreviewUtility.EndAndDrawPreview(previewRect);

            // mesh info
            string meshName = mesh.name;
            if (string.IsNullOrEmpty(meshName))
                meshName = "<no name>";
            string info = meshName + " subset " + meshSubset + "\n" + m_CurEventData.vertexCount + " verts, " + m_CurEventData.indexCount + " indices";
            if (m_CurEventData.instanceCount > 1)
                info += ", " + m_CurEventData.instanceCount + " instances";

            EditorGUI.DropShadowLabel(meshInfoRect, info);
        }

        // Draw any mesh associated with the draw event
        // Return false if no mesh.
        private bool DrawEventMesh()
        {
            Mesh mesh = m_CurEventData.mesh;
            if (mesh == null)
                return false;

            Rect previewRect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandHeight(true));
            if (previewRect.width < kMinPreviewSize || previewRect.height < kMinPreviewSize)
                return true;

            GameObject go = FrameDebuggerUtility.GetFrameEventGameObject(m_CurEventData.frameEventIndex);

            // Info display at bottom (and pings object when clicked)
            Rect meshInfoRect = previewRect;
            meshInfoRect.yMin = meshInfoRect.yMax - EditorGUIUtility.singleLineHeight * 2;
            Rect goInfoRect = meshInfoRect;

            meshInfoRect.xMin = meshInfoRect.center.x;
            goInfoRect.xMax = goInfoRect.center.x;
            if (Event.current.type == EventType.MouseDown)
            {
                if (meshInfoRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(mesh);
                    Event.current.Use();
                }
                if (go != null && goInfoRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.PingObject(go.GetInstanceID());
                    Event.current.Use();
                }
            }

            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, previewRect);

            if (Event.current.type == EventType.Repaint)
            {
                int meshSubset = m_CurEventData.meshSubset;
                DrawMeshPreview(previewRect, meshInfoRect, mesh, meshSubset);
                if (go != null)
                {
                    EditorGUI.DropShadowLabel(goInfoRect, go.name);
                }
            }

            return true;
        }

        private void DrawRenderTargetControls()
        {
            FrameDebuggerEventData cur = m_CurEventData;

            if (cur.rtWidth <= 0 || cur.rtHeight <= 0)
                return;

            var isDepthOnlyRT = (cur.rtFormat == (int)RenderTextureFormat.Depth || cur.rtFormat == (int)RenderTextureFormat.Shadowmap);
            var hasShowableDepth = (cur.rtHasDepthTexture != 0);
            var showableRTCount = cur.rtCount;
            if (hasShowableDepth)
                showableRTCount++;

            EditorGUILayout.LabelField("RenderTarget", cur.rtName);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // MRT to show
            bool rtWasClamped;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(showableRTCount <= 1))
            {
                var rtNames = new GUIContent[showableRTCount];
                for (var i = 0; i < cur.rtCount; ++i)
                {
                    rtNames[i] = Styles.mrtLabels[i];
                }
                if (hasShowableDepth)
                    rtNames[cur.rtCount] = Styles.depthLabel;

                var clampedIndex = Mathf.Clamp(m_RTIndex, 0, showableRTCount - 1);
                rtWasClamped = (clampedIndex != m_RTIndex);
                m_RTIndex = clampedIndex;
                m_RTIndex = EditorGUILayout.Popup(m_RTIndex, rtNames, EditorStyles.toolbarPopup, GUILayout.Width(70));
            }

            // color channels
            GUILayout.Space(10);

            using (new EditorGUI.DisabledScope(isDepthOnlyRT))
            {
                GUILayout.Label(Styles.channelHeader, EditorStyles.miniLabel);
                m_RTChannel = GUILayout.Toolbar(m_RTChannel, Styles.channelLabels, EditorStyles.toolbarButton);
            }

            // levels
            GUILayout.Space(10);
            GUILayout.Label(Styles.levelsHeader, EditorStyles.miniLabel);
            EditorGUILayout.MinMaxSlider(ref m_RTBlackLevel, ref m_RTWhiteLevel, 0.0f, 1.0f, GUILayout.MaxWidth(200.0f));
            if (EditorGUI.EndChangeCheck() || rtWasClamped)
            {
                Vector4 mask = Vector4.zero;
                if (m_RTChannel == 1)
                    mask.x = 1f;
                else if (m_RTChannel == 2)
                    mask.y = 1f;
                else if (m_RTChannel == 3)
                    mask.z = 1f;
                else if (m_RTChannel == 4)
                    mask.w = 1f;
                else
                    mask = Vector4.one;
                int rtIndexToSet = m_RTIndex;
                if (rtIndexToSet >= cur.rtCount)
                    rtIndexToSet = -1;
                FrameDebuggerUtility.SetRenderTargetDisplayOptions(rtIndexToSet, mask, m_RTBlackLevel, m_RTWhiteLevel);
                RepaintAllNeededThings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label(string.Format("{0}x{1} {2}",
                    cur.rtWidth,
                    cur.rtHeight,
                    (RenderTextureFormat)cur.rtFormat));
            if (cur.rtDim == (int)UnityEngine.Rendering.TextureDimension.Cube)
                GUILayout.Label("Rendering into cubemap");
        }

        private void DrawEventDrawCallInfo()
        {
            // shader, pass & keyword information
            EditorGUILayout.LabelField("Shader", m_CurEventDataStrings.shader);

            if (GUI.Button(GUILayoutUtility.GetLastRect(), Styles.selectShaderTooltip, GUI.skin.label))
            {
                EditorGUIUtility.PingObject(m_CurEventData.shaderInstanceID);
                Event.current.Use();
            }

            EditorGUILayout.LabelField("Pass", m_CurEventDataStrings.pass);

            if (!string.IsNullOrEmpty(m_CurEventData.shaderKeywords))
            {
                EditorGUILayout.LabelField("Keywords", m_CurEventData.shaderKeywords);

                if (GUI.Button(GUILayoutUtility.GetLastRect(), Styles.copyToClipboardTooltip, GUI.skin.label))
                    EditorGUIUtility.systemCopyBuffer = m_CurEventDataStrings.shader + System.Environment.NewLine + m_CurEventData.shaderKeywords;
            }

            DrawStates();

            // Show why this draw call can't batch with the previous one.
            if (m_CurEventData.batchBreakCause > 1)   // Valid batch break cause enum value on the C++ side starts at 2.
            {
                GUILayout.Space(10.0f);
                GUILayout.Label(Styles.causeOfNewDrawCallLabel, EditorStyles.boldLabel);
                GUILayout.Label(styles.batchBreakCauses[m_CurEventData.batchBreakCause], EditorStyles.wordWrappedLabel);
            }

            GUILayout.Space(15.0f);

            // preview / properties
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_AdditionalInfo = (ShowAdditionalInfo)GUILayout.Toolbar((int)m_AdditionalInfo, m_AdditionalInfoGuiContents, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            switch (m_AdditionalInfo)
            {
                case ShowAdditionalInfo.Preview:
                    // Show mesh preview if possible
                    if (!DrawEventMesh())
                    {
                        // If no mesh preview, then show vertex/index count at least
                        EditorGUILayout.LabelField("Vertices", m_CurEventData.vertexCount.ToString());
                        EditorGUILayout.LabelField("Indices", m_CurEventData.indexCount.ToString());
                    }
                    break;
                case ShowAdditionalInfo.ShaderProperties:
                    DrawShaderProperties(m_CurEventData.shaderProperties);
                    break;
            }
        }

        private void DrawEventComputeDispatchInfo()
        {
            // compute shader & kernel information
            EditorGUILayout.LabelField("Compute Shader", m_CurEventData.csName);
            if (GUI.Button(GUILayoutUtility.GetLastRect(), GUIContent.none, GUI.skin.label))
            {
                EditorGUIUtility.PingObject(m_CurEventData.csInstanceID);
                Event.current.Use();
            }

            EditorGUILayout.LabelField("Kernel", m_CurEventData.csKernel);

            // dispatch size
            string threadGroupsText;
            if (m_CurEventData.csThreadGroupsX != 0 || m_CurEventData.csThreadGroupsY != 0 || m_CurEventData.csThreadGroupsZ != 0)
                threadGroupsText = string.Format("{0}x{1}x{2}", m_CurEventData.csThreadGroupsX, m_CurEventData.csThreadGroupsY, m_CurEventData.csThreadGroupsZ);
            else
                threadGroupsText = "indirect dispatch";

            EditorGUILayout.LabelField("Thread Groups", threadGroupsText);
        }

        private void DrawCurrentEvent(Rect rect, FrameDebuggerEvent[] descs)
        {
            int curEventIndex = FrameDebuggerUtility.limit - 1;
            if (curEventIndex < 0 || curEventIndex >= descs.Length)
                return;

            GUILayout.BeginArea(rect);

            uint eventDataHash = FrameDebuggerUtility.eventDataHash;
            bool isFrameEventDataValid = curEventIndex == m_CurEventData.frameEventIndex;

            if (eventDataHash != 0 && m_CurEventDataHash != eventDataHash)
            {
                isFrameEventDataValid = FrameDebuggerUtility.GetFrameEventData(curEventIndex, out m_CurEventData);
                m_CurEventDataHash = eventDataHash;
                BuildCurEventDataStrings();
            }

            // render target
            if (isFrameEventDataValid)
                DrawRenderTargetControls();

            // event type and draw call info
            FrameDebuggerEvent cur = descs[curEventIndex];
            GUILayout.Label(string.Format("Event #{0}: {1}", (curEventIndex + 1), s_FrameEventTypeNames[(int)cur.type]), EditorStyles.boldLabel);

            if (FrameDebuggerUtility.IsRemoteEnabled() && FrameDebuggerUtility.receivingRemoteFrameEventData)
            {
                GUILayout.Label("Receiving frame event data...");
            }
            else if (isFrameEventDataValid)     // Is this a draw call?
            {
                if (m_CurEventData.vertexCount > 0 || m_CurEventData.indexCount > 0)
                {
                    // a draw call, display extra info
                    DrawEventDrawCallInfo();
                }
                else if (cur.type == FrameEventType.ComputeDispatch)
                {
                    // a compute dispatch, display extra info
                    DrawEventComputeDispatchInfo();
                }
            }

            GUILayout.EndArea();
        }

        void DrawShaderPropertyFlags(int flags)
        {
            // lowest bits of flags are set for each shader stage that property is used in; matching ShaderType C++ enum
            var str = string.Empty;
            if ((flags & (1 << 1)) != 0)
                str += 'v';
            if ((flags & (1 << 2)) != 0)
                str += 'f';
            if ((flags & (1 << 3)) != 0)
                str += 'g';
            if ((flags & (1 << 4)) != 0)
                str += 'h';
            if ((flags & (1 << 5)) != 0)
                str += 'd';

            GUILayout.Label(str, EditorStyles.miniLabel, GUILayout.MinWidth(20.0f));
        }

        void ShaderPropertyCopyValueMenu(Rect valueRect, System.Object value)
        {
            var e = Event.current;
            if (e.type == EventType.ContextClick && valueRect.Contains(e.mousePosition))
            {
                e.Use();
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy value"), false, delegate
                    {
                        var str = string.Empty;
                        if (value is Vector4)
                            str = ((Vector4)value).ToString(kFloatDetailedFormat);
                        else if (value is Matrix4x4)
                            str = ((Matrix4x4)value).ToString(kFloatDetailedFormat);
                        else if (value is System.Single)
                            str = ((System.Single)value).ToString(kFloatDetailedFormat);
                        else
                            str = value.ToString();
                        EditorGUIUtility.systemCopyBuffer = str;
                    });
                menu.ShowAsContext();
            }
        }

        private void OnGUIShaderPropFloats(ShaderFloatInfo[] floats, int startIndex, int numValues)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kShaderPropertiesIndention);

            ShaderFloatInfo t = floats[startIndex];

            if (numValues == 1)
            {
                GUILayout.Label(t.name, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);
                GUILayout.Label(t.value.ToString(kFloatFormat), EditorStyles.miniLabel, GUILayout.MinWidth(kValueFieldWidth));
                ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
            }
            else
            {
                string arrayName = String.Format("{0} [{1}]", t.name, numValues);
                GUILayout.Label(arrayName, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);

                Rect buttonRect = GUILayoutUtility.GetRect(Styles.arrayValuePopupButton, GUI.skin.button, GUILayout.MinWidth(kValueFieldWidth));
                buttonRect.width = kArrayValuePopupBtnWidth;
                if (GUI.Button(buttonRect, Styles.arrayValuePopupButton))
                {
                    ArrayValuePopup.GetValueStringDelegate getValueString =
                        (int index, bool highPrecision) => floats[index].value.ToString(highPrecision ? kFloatDetailedFormat : kFloatFormat);

                    PopupWindowWithoutFocus.Show(
                        buttonRect,
                        new ArrayValuePopup(startIndex, numValues, 100.0f, getValueString),
                        new[] { PopupLocationHelper.PopupLocation.Left, PopupLocationHelper.PopupLocation.Below, PopupLocationHelper.PopupLocation.Right });
                }
            }

            GUILayout.EndHorizontal();
        }

        private void OnGUIShaderPropVectors(ShaderVectorInfo[] vectors, int startIndex, int numValues)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kShaderPropertiesIndention);

            ShaderVectorInfo t = vectors[startIndex];

            if (numValues == 1)
            {
                GUILayout.Label(t.name, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);
                GUILayout.Label(t.value.ToString(kFloatFormat), EditorStyles.miniLabel, GUILayout.MinWidth(kValueFieldWidth));
                ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
            }
            else
            {
                string arrayName = String.Format("{0} [{1}]", t.name, numValues);
                GUILayout.Label(arrayName, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);

                Rect buttonRect = GUILayoutUtility.GetRect(Styles.arrayValuePopupButton, GUI.skin.button, GUILayout.MinWidth(kValueFieldWidth));
                buttonRect.width = kArrayValuePopupBtnWidth;
                if (GUI.Button(buttonRect, Styles.arrayValuePopupButton))
                {
                    ArrayValuePopup.GetValueStringDelegate getValueString =
                        (int index, bool highPrecision) => vectors[index].value.ToString(highPrecision ? kFloatDetailedFormat : kFloatFormat);

                    PopupWindowWithoutFocus.Show(
                        buttonRect,
                        new ArrayValuePopup(startIndex, numValues, 200.0f, getValueString),
                        new[] { PopupLocationHelper.PopupLocation.Left, PopupLocationHelper.PopupLocation.Below, PopupLocationHelper.PopupLocation.Right });
                }
            }

            GUILayout.EndHorizontal();
        }

        private void OnGUIShaderPropMatrices(ShaderMatrixInfo[] matrices, int startIndex, int numValues)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kShaderPropertiesIndention);

            ShaderMatrixInfo t = matrices[startIndex];

            if (numValues == 1)
            {
                GUILayout.Label(t.name, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);
                GUILayout.Label(t.value.ToString(kFloatFormat), EditorStyles.miniLabel, GUILayout.MinWidth(kValueFieldWidth));
                ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
            }
            else
            {
                string arrayName = String.Format("{0} [{1}]", t.name, numValues);
                GUILayout.Label(arrayName, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
                DrawShaderPropertyFlags(t.flags);

                Rect buttonRect = GUILayoutUtility.GetRect(Styles.arrayValuePopupButton, GUI.skin.button, GUILayout.MinWidth(kValueFieldWidth));
                buttonRect.width = kArrayValuePopupBtnWidth;
                if (GUI.Button(buttonRect, Styles.arrayValuePopupButton))
                {
                    ArrayValuePopup.GetValueStringDelegate getValueString =
                        (int index, bool highPrecision) => '\n' + matrices[index].value.ToString(highPrecision ? kFloatDetailedFormat : kFloatFormat);

                    PopupWindowWithoutFocus.Show(
                        buttonRect,
                        new ArrayValuePopup(startIndex, numValues, 200.0f, getValueString),
                        new[] { PopupLocationHelper.PopupLocation.Left, PopupLocationHelper.PopupLocation.Below, PopupLocationHelper.PopupLocation.Right });
                }
            }

            GUILayout.EndHorizontal();
        }

        private void OnGUIShaderPropBuffer(ShaderBufferInfo t)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kShaderPropertiesIndention);

            GUILayout.Label(t.name, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
            DrawShaderPropertyFlags(t.flags);
            GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel, GUILayout.MinWidth(kValueFieldWidth));

            GUILayout.EndHorizontal();
        }

        private void OnGUIShaderPropTexture(int idx, ShaderTextureInfo t)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kShaderPropertiesIndention);

            GUILayout.Label(t.name, EditorStyles.miniLabel, GUILayout.MinWidth(kNameFieldWidth));
            DrawShaderPropertyFlags(t.flags);

            Rect valueRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.MinWidth(kValueFieldWidth));

            // display texture similar to ObjectField, just without much of interaction or styling:
            Event evt = Event.current;
            Rect previewRect = valueRect;
            previewRect.width = previewRect.height;

            // Tooltip
            if (t.value != null && previewRect.Contains(evt.mousePosition))
                GUI.Label(previewRect, GUIContent.Temp(string.Empty, m_CurEventDataStrings.texturePropertyTooltips[idx]));

            if (evt.type == EventType.Repaint)
            {
                // preview and a label
                Rect textRect = valueRect;
                textRect.xMin += previewRect.width;

                if (t.value != null)
                {
                    // for 2D textures, we want to display them directly as a preview (this will make render textures display their contents);
                    // but for cube maps and other non-2D types DrawPreview does not do anything useful right now, so get their asset type
                    // icon at least
                    Texture previewTexture = t.value;
                    if (previewTexture.dimension != TextureDimension.Tex2D)
                        previewTexture = AssetPreview.GetMiniThumbnail(previewTexture);
                    EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
                }

                GUI.Label(textRect, t.value != null ? t.value.name : t.textureName);
            }
            else if (evt.type == EventType.MouseDown)
            {
                // ping or show preview of texture when clicked
                if (valueRect.Contains(evt.mousePosition))
                {
                    EditorGUI.PingObjectOrShowPreviewOnClick(t.value, valueRect);
                    evt.Use();
                }
            }

            GUILayout.EndHorizontal();
        }

        void DrawShaderProperties(ShaderProperties props)
        {
            m_ScrollViewShaderProps = GUILayout.BeginScrollView(m_ScrollViewShaderProps);

            if (props.textures.Count() > 0)
            {
                GUILayout.Label("Textures", EditorStyles.boldLabel);

                for (int i = 0; i < props.textures.Length; ++i)
                {
                    OnGUIShaderPropTexture(i, props.textures[i]);
                }
            }

            if (props.floats.Count() > 0)
            {
                GUILayout.Label("Floats", EditorStyles.boldLabel);

                for (int i = 0; i < props.floats.Length;)
                {
                    int arraySize = (props.floats[i].flags >> kShaderTypeBits) & kArraySizeBitMask;
                    OnGUIShaderPropFloats(props.floats, i, arraySize);
                    i += arraySize;
                }
            }

            if (props.vectors.Count() > 0)
            {
                GUILayout.Label("Vectors", EditorStyles.boldLabel);

                for (int i = 0; i < props.vectors.Length;)
                {
                    int arraySize = (props.vectors[i].flags >> kShaderTypeBits) & kArraySizeBitMask;
                    OnGUIShaderPropVectors(props.vectors, i, arraySize);
                    i += arraySize;
                }
            }

            if (props.matrices.Count() > 0)
            {
                GUILayout.Label("Matrices", EditorStyles.boldLabel);

                for (int i = 0; i < props.matrices.Length;)
                {
                    int arraySize = (props.matrices[i].flags >> kShaderTypeBits) & kArraySizeBitMask;
                    OnGUIShaderPropMatrices(props.matrices, i, arraySize);
                    i += arraySize;
                }
            }

            if (props.buffers.Count() > 0)
            {
                GUILayout.Label("Buffers", EditorStyles.boldLabel);

                foreach (var d in props.buffers)
                {
                    OnGUIShaderPropBuffer(d);
                }
            }

            GUILayout.EndScrollView();
        }

        void DrawStates()
        {
            FrameDebuggerBlendState blendState = m_CurEventData.blendState;
            FrameDebuggerRasterState rasterState = m_CurEventData.rasterState;
            FrameDebuggerDepthState depthState = m_CurEventData.depthState;

            // blend state
            string blendText = string.Format("{0} {1}", blendState.srcBlend, blendState.dstBlend);
            // only add alpha blend mode if different from RGB one
            if (blendState.srcBlendAlpha != blendState.srcBlend || blendState.dstBlendAlpha != blendState.dstBlend)
                blendText += string.Format(", {0} {1}", blendState.srcBlendAlpha, blendState.dstBlendAlpha);

            EditorGUILayout.LabelField("Blend", blendText);

            // only add blend op if non-Add
            if (blendState.blendOp != BlendOp.Add || blendState.blendOpAlpha != BlendOp.Add)
            {
                string blendOpText;
                if (blendState.blendOp == blendState.blendOpAlpha)
                    blendOpText = blendState.blendOp.ToString();
                else
                    blendOpText = string.Format("{0}, {1}", blendState.blendOp, blendState.blendOpAlpha);

                EditorGUILayout.LabelField("BlendOp", blendOpText);
            }

            // only add color mask if non-RGBA
            if (blendState.writeMask != 15)
            {
                string colorMaskText = "";
                if (blendState.writeMask == 0)
                    colorMaskText += '0';
                else
                {
                    if ((blendState.writeMask & 2) != 0)
                        colorMaskText += 'R';
                    if ((blendState.writeMask & 4) != 0)
                        colorMaskText += 'G';
                    if ((blendState.writeMask & 8) != 0)
                        colorMaskText += 'B';
                    if ((blendState.writeMask & 1) != 0)
                        colorMaskText += 'A';
                }

                EditorGUILayout.LabelField("ColorMask", colorMaskText);
            }

            // depth state
            EditorGUILayout.LabelField("ZClip", rasterState.depthClip.ToString());
            EditorGUILayout.LabelField("ZTest", depthState.depthFunc.ToString());
            EditorGUILayout.LabelField("ZWrite", depthState.depthWrite == 0 ? "Off" : "On");
            EditorGUILayout.LabelField("Cull", rasterState.cullMode.ToString());

            // only add depth offset if non zero
            if (rasterState.slopeScaledDepthBias != 0 || rasterState.depthBias != 0)
            {
                string offsetText = string.Format("{0}, {1}", rasterState.slopeScaledDepthBias, rasterState.depthBias);
                EditorGUILayout.LabelField("Offset", offsetText);
            }

            // Stencil state
            if (m_CurEventData.stencilState.stencilEnable)
            {
                EditorGUILayout.LabelField("Stencil Ref", m_CurEventDataStrings.stencilRef);

                if (m_CurEventData.stencilState.readMask != 255)
                    EditorGUILayout.LabelField("Stencil ReadMask", m_CurEventDataStrings.stencilReadMask);

                if (m_CurEventData.stencilState.writeMask != 255)
                    EditorGUILayout.LabelField("Stencil WriteMask", m_CurEventDataStrings.stencilWriteMask);

                EditorGUILayout.LabelField("Stencil Comp", m_CurEventDataStrings.stencilComp);
                EditorGUILayout.LabelField("Stencil Pass", m_CurEventDataStrings.stencilPass);
                EditorGUILayout.LabelField("Stencil Fail", m_CurEventDataStrings.stencilFail);
                EditorGUILayout.LabelField("Stencil ZFail", m_CurEventDataStrings.stencilZFail);
            }
        }

        internal void OnGUI()
        {
            FrameDebuggerEvent[] descs = FrameDebuggerUtility.GetFrameEvents();
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            if (m_Tree == null)
            {
                m_Tree = new FrameDebuggerTreeView(descs, m_TreeViewState, this, new Rect());
                m_FrameEventsHash = FrameDebuggerUtility.eventsHash;
                m_Tree.m_DataSource.SetExpandedWithChildren(m_Tree.m_DataSource.root, true);
            }

            // captured frame event contents have changed, rebuild the tree data
            if (FrameDebuggerUtility.eventsHash != m_FrameEventsHash)
            {
                m_Tree.m_DataSource.SetEvents(descs);
                m_FrameEventsHash = FrameDebuggerUtility.eventsHash;
            }


            int oldLimit = FrameDebuggerUtility.limit;
            bool repaint = DrawToolbar(descs);

            if (!FrameDebuggerUtility.IsLocalEnabled() && !FrameDebuggerUtility.IsRemoteEnabled() && m_AttachProfilerUI.IsEditor())
            {
                GUI.enabled = true;

                if (!FrameDebuggerUtility.locallySupported)
                {
                    EditorGUILayout.HelpBox("Frame Debugger requires multi-threaded renderer. Usually Unity uses that; if it does not, try starting with -force-gfx-mt command line argument.", MessageType.Warning, true);
                }

                // info box
                EditorGUILayout.HelpBox("Frame Debugger lets you step through draw calls and see how exactly frame is rendered. Click Enable!", MessageType.Info, true);
            }
            else
            {
                float toolbarHeight = EditorStyles.toolbar.fixedHeight;

                var dragRect = new Rect(m_ListWidth, toolbarHeight, kResizerWidth, position.height - toolbarHeight);
                dragRect = EditorGUIUtility.HandleHorizontalSplitter(dragRect, position.width, kMinListWidth, kMinDetailsWidth);
                m_ListWidth = dragRect.x;

                var listRect = new Rect(
                        0,
                        toolbarHeight,
                        m_ListWidth,
                        position.height - toolbarHeight);
                var currentEventRect = new Rect(
                        m_ListWidth + kDetailsMargin,
                        toolbarHeight + kDetailsMargin,
                        position.width - m_ListWidth - kDetailsMargin * 2,
                        position.height - toolbarHeight - kDetailsMargin * 2);


                DrawEventsTree(listRect);
                EditorGUIUtility.DrawHorizontalSplitter(dragRect);
                DrawCurrentEvent(currentEventRect, descs);
            }

            if (repaint || oldLimit != FrameDebuggerUtility.limit)
                RepaintOnLimitChange();

            if (m_RepaintFrames > 0)
            {
                m_Tree.SelectFrameEventIndex(FrameDebuggerUtility.limit);
                RepaintAllNeededThings();
                --m_RepaintFrames;
            }
        }

        private void RepaintOnLimitChange()
        {
            m_RepaintFrames = kNeedToRepaintFrames;
            RepaintAllNeededThings();
        }

        private void RepaintAllNeededThings()
        {
            // indicate that editor needs a redraw (mostly to get offscreen cameras rendered)
            EditorApplication.SetSceneRepaintDirty();
            // Note: do NOT add GameView.RepaintAll here; that would cause really confusing
            // behaviors when there are offscreen (rendering into RTs) cameras.

            // redraw ourselves
            Repaint();
        }

        void DrawEventsTree(Rect rect)
        {
            m_Tree.OnGUI(rect);
        }

        internal class Styles
        {
            public GUIStyle header = "OL title";
            public GUIStyle entryEven = "OL EntryBackEven";
            public GUIStyle entryOdd = "OL EntryBackOdd";
            public GUIStyle rowText = "OL Label";
            public GUIStyle rowTextRight = new GUIStyle("OL Label");
            public GUIContent recordButton = new GUIContent(EditorGUIUtility.TextContent("Record|Record profiling information"));
            public GUIContent prevFrame = new GUIContent(EditorGUIUtility.IconContent("Profiler.PrevFrame", "|Go back one frame"));
            public GUIContent nextFrame = new GUIContent(EditorGUIUtility.IconContent("Profiler.NextFrame", "|Go one frame forwards"));

            public GUIContent[] headerContent;
            public readonly string[] batchBreakCauses;

            public static readonly string[] s_ColumnNames = new[] { "#", "Type", "Vertices", "Indices" };
            public static readonly GUIContent[] mrtLabels = new[]
            {
                EditorGUIUtility.TextContent("RT 0|Show render target #0"),
                EditorGUIUtility.TextContent("RT 1|Show render target #1"),
                EditorGUIUtility.TextContent("RT 2|Show render target #2"),
                EditorGUIUtility.TextContent("RT 3|Show render target #3"),
                EditorGUIUtility.TextContent("RT 4|Show render target #4"),
                EditorGUIUtility.TextContent("RT 5|Show render target #5"),
                EditorGUIUtility.TextContent("RT 6|Show render target #6"),
                EditorGUIUtility.TextContent("RT 7|Show render target #7")
            };
            public static readonly GUIContent depthLabel = EditorGUIUtility.TextContent("Depth|Show depth buffer");
            public static readonly GUIContent[] channelLabels = new[]
            {
                EditorGUIUtility.TextContent("All|Show all (RGB) color channels"),
                EditorGUIUtility.TextContent("R|Show red channel only"),
                EditorGUIUtility.TextContent("G|Show green channel only"),
                EditorGUIUtility.TextContent("B|Show blue channel only"),
                EditorGUIUtility.TextContent("A|Show alpha channel only")
            };
            public static readonly GUIContent channelHeader = EditorGUIUtility.TextContent("Channels|Which render target color channels to show");
            public static readonly GUIContent levelsHeader = EditorGUIUtility.TextContent("Levels|Render target display black/white intensity levels");
            public static readonly GUIContent causeOfNewDrawCallLabel = EditorGUIUtility.TextContent("Why this draw call can't be batched with the previous one");
            public static readonly GUIContent selectShaderTooltip = EditorGUIUtility.TextContent("|Click to select shader");
            public static readonly GUIContent copyToClipboardTooltip = EditorGUIUtility.TextContent("|Click to copy shader and keywords text to clipboard.");
            public static readonly GUIContent arrayValuePopupButton = new GUIContent("...");

            public Styles()
            {
                rowTextRight.alignment = TextAnchor.MiddleRight;
                recordButton.text = "Enable";
                recordButton.tooltip = "Enable Frame Debugging";
                prevFrame.tooltip = "Previous event";
                nextFrame.tooltip = "Next event";
                headerContent = new GUIContent[s_ColumnNames.Length];
                for (int i = 0; i < headerContent.Length; i++)
                    headerContent[i] = EditorGUIUtility.TextContent(s_ColumnNames[i]);

                batchBreakCauses = FrameDebuggerUtility.GetBatchBreakCauseStrings();
            }
        }

        private static Styles ms_Styles;
        public static Styles styles
        {
            get { return ms_Styles ?? (ms_Styles = new Styles()); }
        }

        private class ArrayValuePopup : PopupWindowContent
        {
            public delegate string GetValueStringDelegate(int index, bool highPrecision);
            private GetValueStringDelegate GetValueString;

            private Vector2 m_ScrollPos = Vector2.zero;
            private int m_StartIndex;
            private int m_NumValues;
            private float m_WindowWidth;

            private static readonly GUIStyle m_Style = EditorStyles.miniLabel;

            public ArrayValuePopup(int startIndex, int numValues, float windowWidth, GetValueStringDelegate getValueString)
            {
                m_StartIndex = startIndex;
                m_NumValues = numValues;
                m_WindowWidth = windowWidth;
                GetValueString = getValueString;
            }

            public override Vector2 GetWindowSize()
            {
                float lineHeight = m_Style.lineHeight + m_Style.padding.vertical + m_Style.margin.top;
                return new Vector2(m_WindowWidth, Math.Min(lineHeight * m_NumValues, 250.0f));
            }

            public override void OnGUI(Rect rect)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

                for (int i = 0; i < m_NumValues; ++i)
                {
                    string text = String.Format("[{0}]\t{1}", i, GetValueString(m_StartIndex + i, false));
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
                        allText += String.Format("[{0}]\t{1}\n", i, GetValueString(m_StartIndex + i, true));
                    }

                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Copy value"), false, delegate
                        {
                            EditorGUIUtility.systemCopyBuffer = allText;
                        });
                    menu.ShowAsContext();
                }
            }
        }
    }
}
