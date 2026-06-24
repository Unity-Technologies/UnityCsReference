// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.VectorGraphics.Editor
{
    internal static class VectorImageUtils
    {
        internal static Texture2D RenderToTexture2D(VectorImage vi, int width, int height, int antiAliasing = 1)
        {
            if (vi == null)
                return null;

            if (width <= 0 || height <= 0)
                return null;

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0) {
                msaaSamples = antiAliasing,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };

            var oldActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(desc);
            ThemeStyleSheet theme = null;
            PanelSettings panelSettings = null;
            BaseRuntimePanel panel = null;
            try
            {
                RenderTexture.active = rt;

                theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
                theme.hideFlags = HideFlags.HideAndDontSave;

                // Suppress PanelSettings.Reset()'s default theme lookup which calls
                // AssetDatabase.Refresh() (disallowed during asset import).
                var savedThemeLookup = PanelSettings.GetOrCreateDefaultTheme;
                PanelSettings.GetOrCreateDefaultTheme = null;
                try
                {
                    panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                }
                finally
                {
                    PanelSettings.GetOrCreateDefaultTheme = savedThemeLookup;
                }
                panelSettings.hideFlags = HideFlags.HideAndDontSave;
                panelSettings.themeStyleSheet = theme;
                panelSettings.clearColor = true;
                panelSettings.clearDepthStencil = true;
                panelSettings.isTransient = true;
                panelSettings.targetTexture = rt;

                panel = panelSettings.panel;
                var root = panel.visualTree;
                root.StretchToParentSize();
                root.style.backgroundImage = new StyleBackground(vi);

                GL.PushMatrix();
                panel.Repaint(new Event() { type = EventType.Repaint });
                panel.Render();
                GL.PopMatrix();

                Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.hideFlags = HideFlags.HideAndDontSave;
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                copy.Apply();
                return copy;
            }
            finally
            {
                // A transient panel is not registered in UIElementsRuntimeUtility's panel cache,
                // so dispose of it explicitly so the RenderTreeManager tear down their GPU resources.
                if (panel != null)
                    panel.Dispose();
                if (panelSettings != null)
                    Object.DestroyImmediate(panelSettings);
                if (theme != null)
                    Object.DestroyImmediate(theme);

                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
