// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeCharting instead. (UnityUpgradable) -> RealtimeCharting", true)]
        Charting = -12,
        RealtimeCharting = 12,

        Systems = 13,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeAlbedo instead. (UnityUpgradable) -> RealtimeAlbedo", true)]
        Albedo = -14,
        RealtimeAlbedo = 14,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeEmissive instead. (UnityUpgradable) -> RealtimeEmissive", true)]
        Emissive = -15,
        RealtimeEmissive = 15,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeIndirect instead. (UnityUpgradable) -> RealtimeIndirect", true)]
        Irradiance = -16,
        RealtimeIndirect = 16,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Renamed to better distinguish this mode from new Progressive baked modes. Please use RealtimeDirectionality instead. (UnityUpgradable) -> RealtimeDirectionality", true)]
        Directionality = -17,
        RealtimeDirectionality = 17,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
        TextureStreaming = 33,
        BakedLightmapCulling = 34,
        GIContributorsReceivers = 35,
    }

    internal class SceneRenderModeWindow : PopupWindowContent
    {
        static class Styles
        {
            private static GUIStyle s_MenuItem;
            private static GUIStyle s_Separator;

            public static GUIStyle sMenuItem => s_MenuItem ?? (s_MenuItem = "MenuItem");
            public static GUIStyle sSeparator => s_Separator ?? (s_Separator = "sv_iconselector_sep");

            private static readonly string kShadingMode = "Shading Mode";
            private static readonly string kMiscellaneous = "Miscellaneous";
            private static readonly string kDeferred = "Deferred";
            private static readonly string kGlobalIllumination = "Global Illumination";
            private static readonly string kRealtimeGI = "Realtime Global Illumination";
            private static readonly string kBakedGI = "Baked Global Illumination";
            private static readonly string kMaterialValidation = "Material Validation";

            // Map all builtin DrawCameraMode entries
            // This defines the order in which the entries appear in the dropdown menu!
            public static readonly SceneView.CameraMode[] sBuiltinCameraModes =
            {
                new SceneView.CameraMode(DrawCameraMode.Textured, "Shaded", kShadingMode),
                new SceneView.CameraMode(DrawCameraMode.Wireframe, "Wireframe", kShadingMode),
                new SceneView.CameraMode(DrawCameraMode.TexturedWire, "Shaded Wireframe", kShadingMode),

                new SceneView.CameraMode(DrawCameraMode.DeferredDiffuse, "Albedo", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredSpecular, "Specular", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredSmoothness, "Smoothness", kDeferred),
                new SceneView.CameraMode(DrawCameraMode.DeferredNormal, "Normal", kDeferred),

                new SceneView.CameraMode(DrawCameraMode.Systems, "Systems", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.Clustering, "Clustering", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.LitClustering, "Lit Clustering", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.RealtimeCharting, "UV Charts", kGlobalIllumination),
                new SceneView.CameraMode(DrawCameraMode.GIContributorsReceivers, "Contributors / Receivers", kGlobalIllumination),

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
                new SceneView.CameraMode(DrawCameraMode.BakedLightmapCulling, "Baked Lightmap Culling", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.BakedIndices, "Lightmap Indices", kBakedGI),
                new SceneView.CameraMode(DrawCameraMode.LightOverlap, "Light Overlap", kBakedGI),

                new SceneView.CameraMode(DrawCameraMode.ValidateAlbedo, "Validate Albedo", kMaterialValidation),
                new SceneView.CameraMode(DrawCameraMode.ValidateMetalSpecular, "Validate Metal Specular", kMaterialValidation),

                new SceneView.CameraMode(DrawCameraMode.ShadowCascades, "Shadow Cascades", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.RenderPaths, "Render Paths", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.AlphaChannel, "Alpha Channel", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.Overdraw, "Overdraw", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.Mipmaps, "Mipmaps", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.TextureStreaming, "Texture Streaming", kMiscellaneous),
                new SceneView.CameraMode(DrawCameraMode.SpriteMask, "Sprite Mask", kMiscellaneous),
            };

        }

        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        private float windowHeight
        {
            get
            {
                int headers;
                int modes;

                // Hide unsupported items and headers
                headers = Styles.sBuiltinCameraModes.Where(mode => m_SceneView.IsCameraDrawModeSupported(mode))
                              .Select(mode => mode.section).Distinct().Count() +
                          SceneView.userDefinedModes.Where(mode => m_SceneView.IsCameraDrawModeSupported(mode))
                              .Select(mode => mode.section).Distinct().Count();
                modes = Styles.sBuiltinCameraModes.Count(mode => m_SceneView.IsCameraDrawModeSupported(mode)) +
                        SceneView.userDefinedModes.Count(mode => m_SceneView.IsCameraDrawModeSupported(mode));

                return UpdatedHeight(headers, modes, GraphicsSettings.renderPipelineAsset != null);
            }
        }

        const float windowWidth = 205;
        const float kSeparatorHeight = 3;
        const float kHeaderHorizontalPadding = 5f;
        const float kHeaderVerticalPadding = 3f;

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

            Draw(rect.width);

            if (GUI.changed)
            {
                GUIUtility.ExitGUI();
                editorWindow.RepaintImmediately();
                GUI.changed = false; // Reset the changed flag
            }

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

        //Opens render debugger window located at \newSRP\unity\Packages\com.unity.render-pipelines.core\Editor\Debugging\DebugWindow.cs
        private void DrawRenderingDebuggerShortCut(Rect rect)
        {
            GUIContent label = new GUIContent("Rendering Debugger...");
            var labelRect = rect;
            labelRect.y += (kHeaderVerticalPadding * 2f);
            labelRect.x += kHeaderHorizontalPadding + 15f;
            labelRect.width = EditorStyles.foldout.CalcSize(label).x;
            labelRect.height = EditorStyles.foldout.CalcSize(label).y;

            if (GUI.Button(labelRect, label, EditorStyles.label))
            {
                EditorApplication.ExecuteMenuItem("Window/Analysis/Rendering Debugger");
                editorWindow.Close();
            }

            rect.y += EditorGUI.kSingleLineHeight;
        }

        private void Draw(float listElementWidth)
        {
            var drawPos = new Rect(0, 0, listElementWidth, EditorGUI.kSingleLineHeight);
            string lastSection = null;

            foreach (SceneView.CameraMode mode in SceneView.userDefinedModes.OrderBy(mode => mode.section)
                         .Concat(Styles.sBuiltinCameraModes))
            {
                // Draw separators and headers
                if (mode.drawMode != DrawCameraMode.UserDefined && !m_SceneView.IsCameraDrawModeSupported(mode))
                    // Hide unsupported items and headers
                    continue;

                if (lastSection != mode.section)
                {
                    lastSection = mode.section;
                    if (!foldoutStates.ContainsKey(lastSection))
                    {
                        foldoutStates.Add(lastSection, true);
                    }

                    bool previousState = foldoutStates[lastSection];
                    foldoutStates[lastSection] = EditorGUI.Foldout(
                        new Rect(drawPos.x + kHeaderHorizontalPadding, drawPos.y, drawPos.width,
                            EditorGUI.kSingleLineHeight), foldoutStates[lastSection],
                        EditorGUIUtility.TextContent(lastSection), true);
                    drawPos.y += EditorGUI.kSingleLineHeight;

                    if (previousState != foldoutStates[lastSection])
                    {
                        UpdateWindowSize();
                    }
                }

                if (foldoutStates[lastSection])
                {
                    using (new EditorGUI.DisabledScope(!m_SceneView.IsCameraDrawModeEnabled(mode)))
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            DoBuiltinMode(ref drawPos, mode);
                        }
                    }
                }
            }

            if (GraphicsSettings.renderPipelineAsset != null)
            {
                DrawSeparator(ref drawPos);
                DrawRenderingDebuggerShortCut(drawPos);
            }
        }

        private void UpdateWindowSize()
        {
            if (editorWindow != null && Event.current.type != EventType.Layout)
            {
                float newWindowHeight = RecalculateWindowHeight();
                editorWindow.minSize = new Vector2(windowWidth, newWindowHeight);
                editorWindow.maxSize = new Vector2(windowWidth, newWindowHeight);
            }
        }

        private float RecalculateWindowHeight()
        {
            int headers = 0;
            int modes = 0;

            string lastSection = null;
            foreach (SceneView.CameraMode mode in SceneView.userDefinedModes.OrderBy(mode => mode.section)
                         .Concat(Styles.sBuiltinCameraModes))
            {
                if (mode.drawMode != DrawCameraMode.UserDefined && !m_SceneView.IsCameraDrawModeSupported(mode))
                    continue;

                if (lastSection != mode.section)
                {
                    headers++;
                    lastSection = mode.section;
                }

                if (foldoutStates.ContainsKey(lastSection) && foldoutStates[lastSection])
                {
                    modes++;
                }
            }

            return UpdatedHeight(headers, modes, GraphicsSettings.renderPipelineAsset != null);
        }

        private float UpdatedHeight(int headers, int modes, bool isSRP)
        {
            int separators = headers - 1;
            return ((headers + modes + (isSRP ? 1 : 0)) * EditorGUI.kSingleLineHeight) + (kSeparatorHeight * separators);
        }


        bool IsModeEnabled(DrawCameraMode mode)
        {
            return m_SceneView.IsCameraDrawModeEnabled(GetBuiltinCameraMode(mode));
        }

        void DoBuiltinMode(ref Rect rect, SceneView.CameraMode mode)
        {
            using (new EditorGUI.DisabledScope(!m_SceneView.CheckDrawModeForRenderingPath(mode.drawMode)))
            {
                EditorGUI.BeginChangeCheck();

                GUI.Toggle(rect, m_SceneView.cameraMode == mode, EditorGUIUtility.TextContent(mode.name),
                    Styles.sMenuItem);
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
            return EditorGUIUtility.TextContent(Styles.sBuiltinCameraModes
                .Single(mode => mode.drawMode == drawCameraMode).name);
        }

        internal static SceneView.CameraMode GetBuiltinCameraMode(DrawCameraMode drawMode)
        {
            if (drawMode == DrawCameraMode.Normal)
                drawMode = DrawCameraMode.Textured;
            return Styles.sBuiltinCameraModes.Single(mode => mode.drawMode == drawMode);
        }

        internal static bool DrawCameraModeExists(DrawCameraMode drawMode)
        {
            foreach (var mode in Styles.sBuiltinCameraModes)
            {
                if (mode.drawMode == drawMode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
