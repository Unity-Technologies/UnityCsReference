// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Match EditorDrawingMode order in C++ code!
    public enum DrawCameraMode
    {
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
    }

    internal class SceneRenderModeWindow : PopupWindowContent
    {
        class Styles
        {
            public static readonly GUIStyle sMenuItem = "MenuItem";
            public static readonly GUIStyle sSeparator = "sv_iconselector_sep";

            public static readonly GUIContent sShadedHeader                 = EditorGUIUtility.TextContent("Shading Mode");
            public static readonly GUIContent sMiscellaneous                = EditorGUIUtility.TextContent("Miscellaneous");
            public static readonly GUIContent sDeferredHeader               = EditorGUIUtility.TextContent("Deferred");
            public static readonly GUIContent sGlobalIlluminationHeader     = EditorGUIUtility.TextContent("Global Illumination");
            public static readonly GUIContent sRealtimeGIHeader             = EditorGUIUtility.TextContent("Realtime Global Illumination");
            public static readonly GUIContent sBakedGIHeader                = EditorGUIUtility.TextContent("Baked Global Illumination");
            public static readonly GUIContent sMaterialValidationHeader     = EditorGUIUtility.TextContent("Material Validation");
            public static readonly GUIContent sResolutionToggle             = EditorGUIUtility.TextContent("Show Lightmap Resolution");

            // Map all DrawCameraMode entries
            // This array defines the order in which the entries appear in the dropdown menu!
            public static DrawCameraMode[] sRenderModeUIOrder =
            {
                // Shading Mode
                DrawCameraMode.Textured,
                DrawCameraMode.Wireframe,
                DrawCameraMode.TexturedWire,

                // Miscellaneous
                DrawCameraMode.ShadowCascades,
                DrawCameraMode.RenderPaths,
                DrawCameraMode.AlphaChannel,
                DrawCameraMode.Overdraw,
                DrawCameraMode.Mipmaps,
                DrawCameraMode.SpriteMask,

                // Deferred
                DrawCameraMode.DeferredDiffuse,
                DrawCameraMode.DeferredSpecular,
                DrawCameraMode.DeferredSmoothness,
                DrawCameraMode.DeferredNormal,

                // Enlighten Global Illumination
                DrawCameraMode.Systems,
                DrawCameraMode.Clustering,
                DrawCameraMode.LitClustering,
                DrawCameraMode.RealtimeCharting,

                // Realtime GI
                DrawCameraMode.RealtimeAlbedo,
                DrawCameraMode.RealtimeEmissive,
                DrawCameraMode.RealtimeIndirect,
                DrawCameraMode.RealtimeDirectionality,

                // Baked GI
                DrawCameraMode.BakedLightmap,
                DrawCameraMode.BakedDirectionality,
                DrawCameraMode.ShadowMasks,
                DrawCameraMode.BakedAlbedo,
                DrawCameraMode.BakedEmissive,
                DrawCameraMode.BakedCharting,
                DrawCameraMode.BakedTexelValidity,
                DrawCameraMode.BakedIndices,
                DrawCameraMode.LightOverlap,

                // Material Validation
                DrawCameraMode.ValidateAlbedo,
                DrawCameraMode.ValidateMetalSpecular,
            };


            // Match DrawCameraMode order!
            public static readonly GUIContent[] sRenderModeOptions =
            {
                EditorGUIUtility.TextContent("Shaded"),
                EditorGUIUtility.TextContent("Wireframe"),
                EditorGUIUtility.TextContent("Shaded Wireframe"),
                EditorGUIUtility.TextContent("Shadow Cascades"),
                EditorGUIUtility.TextContent("Render Paths"),
                EditorGUIUtility.TextContent("Alpha Channel"),
                EditorGUIUtility.TextContent("Overdraw"),
                EditorGUIUtility.TextContent("Mipmaps"),
                EditorGUIUtility.TextContent("Albedo"),
                EditorGUIUtility.TextContent("Specular"),
                EditorGUIUtility.TextContent("Smoothness"),
                EditorGUIUtility.TextContent("Normal"),
                EditorGUIUtility.TextContent("UV Charts"),
                EditorGUIUtility.TextContent("Systems"),
                EditorGUIUtility.TextContent("Albedo"),
                EditorGUIUtility.TextContent("Emissive"),
                EditorGUIUtility.TextContent("Indirect"),
                EditorGUIUtility.TextContent("Directionality"),
                EditorGUIUtility.TextContent("Baked Lightmap"),
                EditorGUIUtility.TextContent("Clustering"),
                EditorGUIUtility.TextContent("Lit Clustering"),
                EditorGUIUtility.TextContent("Validate Albedo"),
                EditorGUIUtility.TextContent("Validate Metal Specular"),
                EditorGUIUtility.TextContent("Shadowmask"),
                EditorGUIUtility.TextContent("Light Overlap"),
                EditorGUIUtility.TextContent("Albedo"),
                EditorGUIUtility.TextContent("Emissive"),
                EditorGUIUtility.TextContent("Directionality"),
                EditorGUIUtility.TextContent("Texel Validity"),
                EditorGUIUtility.TextContent("Lightmap Indices"),
                EditorGUIUtility.TextContent("UV Charts"),
                EditorGUIUtility.TextContent("Sprite Mask"),
            };
        }

        readonly float m_WindowHeight = (sMenuRowCount * EditorGUI.kSingleLineHeight) + (kSeparatorHeight * 5) + kShowLightmapResolutionHeight;
        const float m_WindowWidth = 205;

        static readonly int sRenderModeCount = Styles.sRenderModeOptions.Length;
        static readonly int sMenuRowCount = sRenderModeCount + kMenuHeaderCount;

        const int kMenuHeaderCount = 7;
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
            return new Vector2(m_WindowWidth, m_WindowHeight);
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
            DrawHeader(ref drawPos, Styles.sShadedHeader);

            // Render modes
            for (var i = 0; i < sRenderModeCount; ++i)
            {
                // Draw separators and headers
                var mode = Styles.sRenderModeUIOrder[i];

                switch (mode)
                {
                    case DrawCameraMode.ShadowCascades:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sMiscellaneous);
                        break;

                    case DrawCameraMode.DeferredDiffuse:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sDeferredHeader);
                        break;

                    case DrawCameraMode.Systems:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sGlobalIlluminationHeader);
                        break;

                    case DrawCameraMode.RealtimeAlbedo:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sRealtimeGIHeader);
                        break;

                    case DrawCameraMode.BakedLightmap:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sBakedGIHeader);
                        break;

                    case DrawCameraMode.ValidateAlbedo:
                        DrawSeparator(ref drawPos);
                        DrawHeader(ref drawPos, Styles.sMaterialValidationHeader);
                        break;
                }

                using (new EditorGUI.DisabledScope(!IsModeEnabled(mode)))
                {
                    DoOneMode(caller, ref drawPos, mode);
                }
            }

            bool disabled = (m_SceneView.renderMode < DrawCameraMode.RealtimeCharting) || !IsModeEnabled(m_SceneView.renderMode);
            DoResolutionToggle(drawPos, disabled);
        }

        bool IsModeEnabled(DrawCameraMode mode)
        {
            return m_SceneView.IsCameraDrawModeEnabled(mode);
        }

        void DoResolutionToggle(Rect rect, bool disabled)
        {
            // Bg
            GUI.Label(new Rect(kFrameWidth, rect.y, m_WindowWidth - kFrameWidth * 2, kShowLightmapResolutionHeight), "", EditorStyles.inspectorBig);

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

        void DoOneMode(EditorWindow caller, ref Rect rect, DrawCameraMode drawCameraMode)
        {
            using (new EditorGUI.DisabledScope(!m_SceneView.CheckDrawModeForRenderingPath(drawCameraMode)))
            {
                EditorGUI.BeginChangeCheck();

                GUI.Toggle(rect, m_SceneView.renderMode == drawCameraMode, GetGUIContent(drawCameraMode), Styles.sMenuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SceneView.renderMode = drawCameraMode;
                    m_SceneView.Repaint();
                    GUIUtility.ExitGUI();
                }

                rect.y += EditorGUI.kSingleLineHeight;
            }
        }

        public static GUIContent GetGUIContent(DrawCameraMode drawCameraMode)
        {
            return Styles.sRenderModeOptions[(int)drawCameraMode];
        }
    }
}
