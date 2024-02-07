// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    class EditorMonitor
    {
        public static EditorMonitor instance { get; } = new EditorMonitor();

        public EditorMonitor()
        {
            Panel.beforeAnyRepaint += OnBeforeRepaint;
        }

        void OnBeforeRepaint(Panel source)
        {
            bool colorSpaceChanged = CheckForColorSpaceChange();
            bool importCountChanged = CheckForImportCountChange();
            bool renderTexturesTrashed = CheckForRenderTexturesTrashed();

            if (colorSpaceChanged || importCountChanged || renderTexturesTrashed)
            {
                var it = UIElementsUtility.GetPanelsIterator();
                while (it.MoveNext())
                {
                    Panel panel = it.Current.Value;
                    if (colorSpaceChanged || (importCountChanged && panel.resetPanelRenderingOnAssetChange) || renderTexturesTrashed)
                        panel.ResetRendering();
                }
            }
        }

        // Sprites, SVGs or textures imports require dynamic atlases and render data to be invalidated
        uint m_LastVersion;
        bool CheckForImportCountChange()
        {
            uint currentVersion = AssetDatabase.GlobalArtifactDependencyVersion;
            if (m_LastVersion == currentVersion)
                return false;

            m_LastVersion = currentVersion;
            return true;
        }

        ColorSpace m_LastColorSpace;
        bool CheckForColorSpaceChange()
        {
            ColorSpace activeColorSpace = QualitySettings.activeColorSpace;
            if (m_LastColorSpace == activeColorSpace)
                return false;

            m_LastColorSpace = activeColorSpace;
            return true;
        }

        // This check isn't required for the "legacy" atlas manager, since it could detect this condition and rebuild
        // the texture. However, the use done by the shader info allocator would not allow this and requires a rebuild.
        RenderTexture m_RenderTexture;
        bool CheckForRenderTexturesTrashed()
        {
            if (m_RenderTexture == null || !m_RenderTexture.IsCreated())
            {
                m_RenderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) { name = "EditorAtlasMonitorRT" };
                m_RenderTexture.Create();
                return true;
            }

            return false;
        }
    }

    namespace Experimental
    {
        static class AssetMonitoringExtensions
        {
            /// <summary>Gets the state that controls if the panel of the EditorWindow should reset its rendering data when an asset changes.</summary>
            public static bool GetResetPanelRenderingOnAssetChange(EditorWindow window) => window.resetPanelRenderingOnAssetChange;

            /// <summary>Sets the state that controls if the panel of the EditorWindow should reset its rendering data when an asset changes.</summary>
            public static void SetResetPanelRenderingOnAssetChange(EditorWindow window, bool reset) => window.resetPanelRenderingOnAssetChange = reset;
        }
    }
}
