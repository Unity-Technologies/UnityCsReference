// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomEditor(typeof(VisualTreeAsset))]
    internal class VisualTreeAssetEditor : ScriptableObjectAssetEditor
    {
        private VisualTreeAsset m_VTA;
        private VisualElement m_VisualTree;
        protected Texture2D m_FileTypeIcon;
        protected RenderTexture m_preview_texture;
        private Event m_evt = new Event();//Dummy event to fake rendering, cached to reduce memory allocation.
        private int m_LastDirtyCount;
        private int m_LastContentHash;
        private CanvasTheme m_PreviewThemeOverride = CanvasTheme.ProjectSettings;
        private ThemeStyleSheet m_CustomThemeOverride;
        string m_SelectedThemeDisplayName;

        // Cached dropdown data to avoid allocations every frame
        private List<string> m_CachedThemeChoices;
        private Dictionary<string, (CanvasTheme, ThemeStyleSheet)> m_CachedThemeData;

        private const string k_ThemeDropdownTooltip = "Preview this asset using a specific theme from your project. Actual theme used at runtime is defined by PanelSettings.";
        private const string k_Separator = "|";

        // Used by tests
        internal VisualElement visualTree => m_VisualTree;

        //Currently just uses a fixed size texture to minimize lag/jitter as we are not integrated in the update loop. (instead of the real preview size)
        private Vector2Int m_TextureSize = new Vector2Int(512, 512);

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture(typeof(VisualTreeAsset));
            EditorApplication.update += Update;
            UIToolkitProjectSettings.onThemeChanged += OnProjectThemeChanged;
            ThemeUtility.themeFilesChanged += OnThemeFilesChanged;
            m_VTA = null;//Force redraw;
        }

        protected void OnDisable()
        {
            EditorApplication.update -= Update;
            UIToolkitProjectSettings.onThemeChanged -= OnProjectThemeChanged;
            ThemeUtility.themeFilesChanged -= OnThemeFilesChanged;

            if (m_VisualTree != null)
            {
                m_VisualTree.RemoveFromHierarchy();
                m_VisualTree = null;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabled(true);
            base.OnInspectorGUI();
            EditorGUI.EndDisabled();
        }

        void OnProjectThemeChanged()
        {
            // Force refresh the preview when project theme settings change
            // Reset the override to ProjectSettings so the new project theme is applied
            if (m_PreviewThemeOverride == CanvasTheme.ProjectSettings)
            {
                m_VTA = null;
                Repaint();
            }
        }

        void OnThemeFilesChanged()
        {
            // Invalidate cached dropdown data when theme files change
            m_CachedThemeChoices = null;
            m_CachedThemeData = null;
            Repaint();
        }

        void DrawPreviewThemeDropdown()
        {
            if (m_VTA == null)
                return; // Can't show dropdown without a valid asset

            bool isEditorExtensionMode = m_VTA.IsEditorExtensionMode();

            // Get project default theme for marking with suffix
            var (projectTheme, projectThemeSheet) = ThemeUtility.GetProjectDefaultTheme(isEditorExtensionMode);

            // Build menu options
            if (m_CachedThemeChoices == null || m_CachedThemeData == null)
            {
                m_CachedThemeChoices = new List<string>();
                m_CachedThemeData = new Dictionary<string, (CanvasTheme, ThemeStyleSheet)>();

                var editorThemes = ThemeUtility.GetEditorThemesToDisplayName();
                foreach (var themeKvp in editorThemes)
                {
                    var canvasTheme = themeKvp.Key;
                    var displayName = themeKvp.Value;

                    // Insert project default theme to front
                    if (canvasTheme == projectTheme)
                    {
                        displayName += ThemeUtility.ProjectThemeSuffix;
                        m_CachedThemeChoices.Insert(0, displayName);
                        m_CachedThemeChoices.Insert(1, k_Separator);
                    }
                    else
                        m_CachedThemeChoices.Add(displayName);

                    m_CachedThemeData[displayName] = (canvasTheme, null);
                }

                // Add separator between editor and runtime themes
                m_CachedThemeChoices.Add(k_Separator);

                var runtimeThemes = ThemeUtility.GetRuntimeThemesToDisplayName();
                foreach (var themeKvp in runtimeThemes)
                {
                    var themeSheet = themeKvp.Key;
                    var displayName = themeKvp.Value;

                    // Insert project default to front
                    if (themeSheet == projectThemeSheet)
                    {
                        displayName += ThemeUtility.ProjectThemeSuffix;
                        m_CachedThemeChoices.Insert(0, displayName);
                        m_CachedThemeChoices.Insert(1, k_Separator);
                    }
                    else
                        m_CachedThemeChoices.Add(displayName);

                    m_CachedThemeData[displayName] = (CanvasTheme.Custom, themeSheet);
                }

                // Add separator before settings
                m_CachedThemeChoices.Add(k_Separator);
            }

            // Initialize selection to effective theme if not yet set
            if (m_SelectedThemeDisplayName == null)
            {
                var (effectiveTheme, effectiveThemeSheet) = ThemeUtility.GetEffectiveTheme(isEditorExtensionMode);
                m_SelectedThemeDisplayName = FindDisplayNameForTheme(effectiveTheme, effectiveThemeSheet);
            }

            // Show dropdown in toolbar
            GUILayout.Label(new GUIContent("Theme:", k_ThemeDropdownTooltip), GUILayout.Width(50));
            var dropdownRect = GUILayoutUtility.GetRect(new GUIContent(m_SelectedThemeDisplayName), EditorStyles.popup, GUILayout.MinWidth(150));

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(m_SelectedThemeDisplayName, k_ThemeDropdownTooltip), FocusType.Keyboard, EditorStyles.popup))
            {
                var menu = new GenericMenu();

                foreach (var choice in m_CachedThemeChoices)
                {
                    if (choice == k_Separator)
                    {
                        menu.AddSeparator("");
                        continue;
                    }

                    var (theme, sheet) = m_CachedThemeData[choice];
                    bool isSelected = choice == m_SelectedThemeDisplayName;

                    if (sheet == null)
                        menu.AddItem(new GUIContent(choice), isSelected, () => OnThemeSelected(choice, theme, null));
                    else
                    {
                        var capturedSheet = sheet;
                        menu.AddItem(new GUIContent(choice), isSelected, () => OnThemeSelected(choice, CanvasTheme.Custom, capturedSheet));
                    }
                }

                menu.AddItem(new GUIContent("Preview Theme Settings..."), false, ShowSettingsWindow);

                menu.DropDown(dropdownRect);
            }
        }

        void ShowSettingsWindow()
        {
            var projectSettingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>();
            projectSettingsWindow.Show();
            projectSettingsWindow.SelectProviderByName(UIToolkitSettingsProvider.name);
        }

        void OnThemeSelected(string themeName, CanvasTheme theme, ThemeStyleSheet sheet)
        {
            m_SelectedThemeDisplayName = themeName;
            m_PreviewThemeOverride = theme;
            m_CustomThemeOverride = sheet;
            m_VTA = null; // Force refresh the preview
            Repaint();
        }

        string FindDisplayNameForTheme(CanvasTheme theme, ThemeStyleSheet sheet)
        {
            foreach (var kvp in m_CachedThemeData)
            {
                var (cachedTheme, cachedSheet) = kvp.Value;
                if (cachedTheme == theme && cachedSheet == sheet)
                    return kvp.Key;
            }
            // Fallback to first item
            return m_CachedThemeChoices.Count > 0 ? m_CachedThemeChoices[0] : string.Empty;
        }

        void Update()
        {
            UpdatePreviewTexture(m_TextureSize.x, m_TextureSize.y);
        }

        protected void OnDestroy()
        {
            if (m_preview_texture != null)
            {
                m_preview_texture.Release();
                m_preview_texture.DiscardContents();
                DestroyImmediate(m_preview_texture);
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        protected void RenderIcon(Rect iconRect)
        {
            Debug.Assert(m_FileTypeIcon != null);
            GUI.DrawTexture(iconRect, m_FileTypeIcon, ScaleMode.ScaleToFit);
        }

        private void RenderStaticPreview(int width, int height, ref RenderTexture tex)
        {
            //No backup of the viewport is currently done
            //It is not necessary if rendereing to a rendertexture in the editor update loop but mandatory to do an IM rendering
            //RectInt oldViewport = UnityEngine.UIElements.UIR.Utility.GetActiveViewport();

            m_VisualTree = m_VTA.Instantiate();
            m_VisualTree.StretchToParentSize();

            // Create a transient panel to render the visual tree asset
            var panel = EditorPanel.FindOrCreate(m_VTA);
            var visualTree = panel.visualTree;
            visualTree.Add(m_VisualTree);

            Binding.SetPanelLogLevel(panel, BindingLogLevel.None); // We don't want preview to log errors.
            ApplyThemeToPreview(m_VisualTree);

            var r = new Rect(0, 0, width, height);
            var viewportRect = GUIClip.UnclipToWindow(r); // Still in points, not pixels
            panel.pixelsPerPoint = 1;
            panel.UpdateScalingFromEditorWindow = false;
            panel.visualTree.SetSize(viewportRect.size); // We will draw relative to a viewport covering the preview area, so draw at 0,0
            panel.visualTree.IncrementVersion(VersionChangeType.Repaint);

            var backup = RenderTexture.active;
            GL.PushMatrix();
            var oldState = SavedGUIState.Create();
            PanelClearSettings oldClearSettings = panel.clearSettings;

            try
            {
                if (tex == null || tex.width != width || tex.height != height)
                {
                    if (tex != null)
                    {
                        tex.Release();
                        tex.DiscardContents();
                        DestroyImmediate(tex);
                    }

                    tex = new RenderTexture((int)viewportRect.size.x, (int)viewportRect.size.y, 24);
                }

                RenderTexture.active = tex;
                GL.LoadPixelMatrix();
                GL.Clear(true, true, Color.black, UIRUtility.k_ClearZ);

                int clips = GUIClip.Internal_GetCount();
                while (clips > 0)
                {
                    GUIClip.Pop();
                    clips--;
                }

                panel.clearSettings = new PanelClearSettings();

                //Use a dummy repaint event, otherwise imgui element wont be shown when using event.current and rendered in the editor update loop
                m_evt.type = EventType.Repaint;
                panel.Repaint(m_evt);
                panel.Render();

                panel.Dispose();
                UIElementsUtility.RemoveCachedPanel(m_VTA.GetEntityId());
            }
            finally
            {
                panel.clearSettings = oldClearSettings;
                oldState.ApplyAndForget();
                GL.PopMatrix();
                RenderTexture.active = backup;
            }

            // As stated above, viewport is not saved/restored.
            // Doing  " GL.Viewport(new Rect(oldViewport.xMin, oldViewport.yMin, oldViewport.width, oldViewport.height));" is not ennough
        }

        public void Render(VisualTreeAsset vta, Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint || r.width < 100 && r.height < 100)
                return;


            // Simon.Dufour: was not able to find what setting to revert after rendering the panel so that it does not affect the current rendering.
            // Update of the preview texture has been moved to the editor update loop;
            // In typical IMGUI fashion, updatePreviewTexture((int)r.width, (int)r.height); would be called here

            if (m_preview_texture)
            {
                Vector2 size = Mathf.Min(r.height / (float)m_preview_texture.height, r.width / (float)m_preview_texture.width) * new Vector2(m_preview_texture.width, m_preview_texture.height);

                EditorGUI.DrawPreviewTexture(new Rect(r.center - size / 2, size), m_preview_texture);
            }
        }

        // Also used in tests
        internal bool UpdatePreviewTexture(int width, int height)
        {
            var vta = target as VisualTreeAsset;
            bool dirty = false;
            int currentDirtyCount = EditorUtility.GetDirtyCount(target);
            if (vta != m_VTA || !m_VTA || currentDirtyCount != m_LastDirtyCount || vta.contentHash != m_LastContentHash)
            {
                m_VTA = vta;
                m_LastDirtyCount = currentDirtyCount;
                m_LastContentHash = vta.contentHash;
                dirty = true;
            }

            if (dirty)
            {
                RenderStaticPreview(width, height, ref m_preview_texture);
            }

            return dirty;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            const int k_IconSize = 64;

            base.OnPreviewGUI(r, background);


            if (r.width > k_IconSize || r.height > k_IconSize)
            {
                Render(target as VisualTreeAsset, r, background);
            }
            else
            {
                RenderIcon(r);
            }
        }

        public override void OnPreviewSettings()
        {
            DrawPreviewThemeDropdown();
        }

        void ApplyThemeToPreview(VisualElement previewRoot)
        {
            if (m_VTA == null || previewRoot == null)
                return;

            bool isEditorExtensionMode = m_VTA.IsEditorExtensionMode();
            CanvasTheme canvasTheme;
            ThemeStyleSheet themeSheet;

            // Check if user has overridden the theme
            if (m_PreviewThemeOverride != CanvasTheme.ProjectSettings)
            {
                canvasTheme = m_PreviewThemeOverride;
                themeSheet = m_CustomThemeOverride;
            }
            else
            {
                // Use project settings
                (canvasTheme, themeSheet) = ThemeUtility.GetEffectiveTheme(isEditorExtensionMode);
            }

            // Apply the appropriate stylesheet
            StyleSheet activeThemeStyleSheet = ThemeUtility.GetStyleSheetForTheme(canvasTheme, themeSheet);

            if (activeThemeStyleSheet != null)
            {
                previewRoot.styleSheets.Add(activeThemeStyleSheet);
            }
        }
    }
}
