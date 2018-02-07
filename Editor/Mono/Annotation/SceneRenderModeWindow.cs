// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Match EditorDrawingMode order in C++ code!
    public enum DrawCameraMode
    {
        UserDefined = int.MinValue,
        Normal = -1,
        Textured = 0,
        Wireframe = 1,
        TexturedWire = 2,
        ShadowCascades = 3,
        RenderPaths = 4,
        AlphaChannel = 5,
        Overdraw = 6,
        Mipmaps = 7,
        DeferredDiffuse = 8,
        DeferredSpecular = 9,
        DeferredSmoothness = 10,
        DeferredNormal = 11,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeCharting instead. (UnityUpgradable) -> RealtimeCharting", true)]
        Charting = -12,
        RealtimeCharting = 12,

        Systems = 13,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeAlbedo instead. (UnityUpgradable) -> RealtimeAlbedo", true)]
        Albedo = -14,
        RealtimeAlbedo = 14,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeEmissive instead. (UnityUpgradable) -> RealtimeEmissive", true)]
        Emissive = -15,
        RealtimeEmissive = 15,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeIndirect instead. (UnityUpgradable) -> RealtimeIndirect", true)]
        Irradiance = -16,
        RealtimeIndirect = 16,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeDirectionality instead. (UnityUpgradable) -> RealtimeDirectionality", true)]
        Directionality = -17,
        RealtimeDirectionality = 17,

        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use BakedLightmap instead. (UnityUpgradable) -> BakedLightmap", true)]
        Baked = -18,
        BakedLightmap = 18,

        Clustering = 19,
        LitClustering = 20,
        ValidateAlbedo = 21,
        ValidateMetalSpecular = 22,
        ShadowMasks = 23,
        LightOverlap = 24,
        BakedAlbedo = 25,
        BakedEmissive = 26,
        BakedDirectionality = 27,
        BakedTexelValidity = 28,
        BakedIndices = 29,
        BakedCharting = 30,
        SpriteMask = 31,
        BakedUVOverlap = 32,
    }

    internal class SceneRenderModeWindow : PopupWindowContent
    {
        class Styles
        {
            public static GUIStyle s_MenuItem;
            public static GUIStyle s_Separator;

            public static GUIStyle sMenuItem
            {
                get
                {
                    if (s_MenuItem == null)
                        s_MenuItem = "MenuItem";
                    return s_MenuItem;
                }
            }

            public static GUIStyle sSeparator
            {
                get
                {
                    if (s_Separator == null)
                        s_Separator = "sv_iconselector_sep";
                    return s_Separator;
                }
            }

            public static readonly string kShadingMode = "Shading Mode";
            public static readonly string kMiscellaneous = "Miscellaneous";
            public static readonly string kDeferred = "Deferred";
            public static readonly string kGlobalIllumination = "Global Illumination";
            public static readonly string kRealtimeGI = "Realtime Global Illumination";
            public static readonly string kBakedGI = "Baked Global Illumination";
            public static readonly string kMaterialValidation = "Material Validation";

            public static readonly GUIContent sResolutionToggle =
                EditorGUIUtility.TextContent("Show Lightmap Resolution");

            // Map all builtin DrawCameraMode entries
            // This defines the order in which the entries appear in the dropdown menu!
            public static readonly SceneView.CameraMode[] sBuiltinCameraModes =
            {
                new SceneView.CameraMode(DrawCameraMode.Textured, "Shaded", kShadingMode),
                new SceneView.CameraMode(DrawCameraMode.Wireframe, "Wireframe", kShadingMode),
                new SceneView.CameraMode(DrawCameraMode.TexturedWire, "Shaded Wireframe", kShadingMode),

                new SceneView.CameraMode(DrawCameraMode.ShadowCascades, "Shadow Cascades", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.RenderPaths, "Render Paths", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.AlphaChannel, "Alpha Channel", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.Overdraw, "Overdraw", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.Mipmaps, "Mipmaps", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.SpriteMask, "Sprite Mask", kMiscellaneous),

                new SceneView.CameraMode(DrawCameraMode.DeferredDiffuse, "Albedo", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredSpecular, "Specular", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredSmoothness, "Smoothness", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredNormal, "Normal", kDeferred),

                new SceneView.CameraMode(DrawCameraMode.Systems, "Systems", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.Clustering, "Clustering", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.LitClustering, "Lit Clustering", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.RealtimeCharting, "UV Charts", kGlobalIllumination),

                new SceneView.CameraMode(DrawCameraMode.RealtimeAlbedo, "Albedo", kRealtimeGI),
                new SceneView.CameraMode(DrawCameraMode.RealtimeEmissive, "Emissive", kRealtimeGI),
                new SceneView.CameraMode(DrawCameraMode.RealtimeIndirect, "Indirect", kRealtimeGI),
                new SceneView.CameraMode(DrawCameraMode.RealtimeDirectionality, "Directionality", kRealtimeGI),

                new SceneView.CameraMode(DrawCameraMode.BakedLightmap, "Baked Lightmap", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedDirectionality, "Directionality", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.ShadowMasks, "Shadowmask", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedAlbedo, "Albedo", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedEmissive, "Emissive", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedCharting, "UV Charts", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedTexelValidity, "Texel Validity", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedUVOverlap, "UV Overlap", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedIndices, "Lightmap Indices", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.LightOverlap, "Light Overlap", kBakedGI),

                new SceneView.CameraMode(DrawCameraMode.ValidateAlbedo, "Validate Albedo", kMaterialValidation),
                new SceneView.CameraMode(DrawCameraMode.ValidateMetalSpecular, "Validate Metal Specular", kMaterialValidation),
            };
        }

        private float windowHeight
        {
            get
            {
                int headers;
                int modes;

                // TODO: This needs to be fixed and we need to find a way to dif. between disabled and unsupported
                if (GraphicsSettings.renderPipelineAsset != null)
                {
                    // When using SRP, we completely hide disabled builtin modes, including headers
                    headers = Styles.sBuiltinCameraModes.Where(mode => m_SceneView.IsCameraDrawModeEnabled(mode)).Select(mode => mode.section).Distinct().Count() +
                        SceneView.userDefinedModes.Where(mode => m_SceneView.IsCameraDrawModeEnabled(mode)).Select(mode => mode.section).Distinct().Count();
                    modes = Styles.sBuiltinCameraModes.Count(mode => m_SceneView.IsCameraDrawModeEnabled(mode)) + SceneView.userDefinedModes.Count(mode => m_SceneView.IsCameraDrawModeEnabled(mode));
                }
                else
                {
                    headers = Styles.sBuiltinCameraModes.Select(mode => mode.section).Distinct().Count() + SceneView.userDefinedModes.Where(mode => m_SceneView.IsCameraDrawModeEnabled(mode)).Select(mode => mode.section).Distinct().Count();
                    modes = Styles.sBuiltinCameraModes.Count() + SceneView.userDefinedModes.Count(mode => m_SceneView.IsCameraDrawModeEnabled(mode));
                }

                int separators = headers - 2;
                return ((headers + modes) * EditorGUI.kSingleLineHeight) + (kSeparatorHeight * separators) + kShowLightmapResolutionHeight;
            }
        }

        float windowWidth { get { return 205; } }

        const float kSeparatorHeight = 3;
        const float kFrameWidth = 1f;
        const float kHeaderHorizontalPadding = 5f;
        const float kHeaderVerticalPadding = 1f;
        const float kShowLightmapResolutionHeight = EditorGUI.kSingleLineHeight + kSeparatorHeight * 2;
        const float kTogglePadding = 7f;

        readonly SceneView m_SceneView;

        public SceneRenderModeWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(windowWidth, windowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            if (m_SceneView == null || m_SceneView.sceneViewState == null)
                return;

            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            Draw(editorWindow, rect.width);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSeparator(ref Rect rect)
        {
            var labelRect = rect;
            labelRect.x += kHeaderHorizontalPadding;
            labelRect.y += kSeparatorHeight;
            labelRect.width -= kHeaderHorizontalPadding * 2;
            labelRect.height = kSeparatorHeight;

            GUI.Label(labelRect, GUIContent.none, Styles.sSeparator);
            rect.y += kSeparatorHeight;
        }

        private void DrawHeader(ref Rect rect, GUIContent label)
        {
            var labelRect = rect;
            labelRect.y += kHeaderVerticalPadding;
            labelRect.x += kHeaderHorizontalPadding;
            labelRect.width = EditorStyles.miniLabel.CalcSize(label).x;
            labelRect.height = EditorStyles.miniLabel.CalcSize(label).y;
            GUI.Label(labelRect, label, EditorStyles.miniLabel);
            rect.y += EditorGUI.kSingleLineHeight;
        }

        private void Draw(EditorWindow caller, float listElementWidth)
        {
            var drawPos = new Rect(0, 0, listElementWidth, EditorGUI.kSingleLineHeight);
            bool usingScriptableRenderPipeline = (GraphicsSettings.renderPipelineAsset != null);
            string lastSection = null;

            foreach (SceneView.CameraMode mode in SceneView.userDefinedModes.OrderBy(mode => mode.section).Concat(Styles.sBuiltinCameraModes))
            {
                // Draw separators and headers
                if (usingScriptableRenderPipeline && mode.drawMode != DrawCameraMode.UserDefined && !m_SceneView.IsCameraDrawModeEnabled(mode))
                    // When using SRP, we completely hide disabled builtin modes, including headers
                    continue;

                if (lastSection != mode.section)
                {
                    // Draw header for new section
                    if (lastSection != null)
                        // Don't draw separator before first section
                        DrawSeparator(ref drawPos);
                    DrawHeader(ref drawPos, EditorGUIUtility.TextContent(mode.section));
                    lastSection = mode.section;
                }

                using (new EditorGUI.DisabledScope(!m_SceneView.IsCameraDrawModeEnabled(mode)))
                {
                    DoBuiltinMode(caller, ref drawPos, mode);
                }
            }

            bool disabled = (m_SceneView.cameraMode.drawMode < DrawCameraMode.RealtimeCharting) ||
                !IsModeEnabled(m_SceneView.cameraMode.drawMode);
            DoResolutionToggle(drawPos, disabled);
        }

        bool IsModeEnabled(DrawCameraMode mode)
        {
            return m_SceneView.IsCameraDrawModeEnabled(GetBuiltinCameraMode(mode));
        }

        void DoResolutionToggle(Rect rect, bool disabled)
        {
            // Bg
            GUI.Label(new Rect(kFrameWidth, rect.y, windowWidth - kFrameWidth * 2, kShowLightmapResolutionHeight), "", EditorStyles.inspectorBig);

            rect.y += kSeparatorHeight;
            rect.x += kTogglePadding;

            using (new EditorGUI.DisabledScope(disabled))
            {
                EditorGUI.BeginChangeCheck();
                bool showResolution = GUI.Toggle(rect, LightmapVisualization.showResolution, Styles.sResolutionToggle);
                if (EditorGUI.EndChangeCheck())
                {
                    LightmapVisualization.showResolution = showResolution;
                    SceneView.RepaintAll();
                }
            }
        }

        void DoBuiltinMode(EditorWindow caller, ref Rect rect, SceneView.CameraMode mode)
        {
            using (new EditorGUI.DisabledScope(!m_SceneView.CheckDrawModeForRenderingPath(mode.drawMode)))
            {
                EditorGUI.BeginChangeCheck();

                GUI.Toggle(rect, m_SceneView.cameraMode == mode, EditorGUIUtility.TextContent(mode.name), Styles.sMenuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SceneView.cameraMode = mode;
                    m_SceneView.Repaint();
                    GUIUtility.ExitGUI();
                }

                rect.y += EditorGUI.kSingleLineHeight;
            }
        }

        public static GUIContent GetGUIContent(DrawCameraMode drawCameraMode)
        {
            if (drawCameraMode == DrawCameraMode.UserDefined)
                return GUIContent.none;
            return EditorGUIUtility.TextContent(Styles.sBuiltinCameraModes.Single(mode => mode.drawMode == drawCameraMode).name);
        }

        internal static SceneView.CameraMode GetBuiltinCameraMode(DrawCameraMode drawMode)
        {
            return Styles.sBuiltinCameraModes.Single(mode => mode.drawMode == drawMode);
        }
    }
}
