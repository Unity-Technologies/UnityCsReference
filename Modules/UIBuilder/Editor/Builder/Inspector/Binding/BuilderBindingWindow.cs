// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    /// <summary>
    ///  Window used to create and edit bindings.
    /// </summary>
    class BuilderBindingWindow : EditorWindow
    {
        private static BuilderBindingWindow s_Window;
        private BuilderBindingView m_View;

        public static BuilderBindingWindow activeWindow => s_Window;

        /// <summary>
        /// The main view.
        /// </summary>
        public BuilderBindingView view => m_View;

        /// <summary>
        /// Opens a window with the specified mode, title and geometry.
        /// </summary>
        /// <param name="creationMode">Indicates whether the window will be used for creeating or editing binding</param>
        /// <param name="title">The window title</param>
        /// <param name="pos">The position of the window</param>
        /// <param name="windowSize">The size of the window</param>
        /// <returns></returns>
        public static BuilderBindingWindow Open(string title, Rect pos, Vector2 windowSize)
        {
            s_Window = GetWindow<BuilderBindingWindow>(true, title);
            s_Window.position = new Rect(pos.position, windowSize);
            return s_Window;
        }

        void OnEnable()
        {
            // Ensure this window closes when the UIBuilder window closes.
            if (Builder.ActiveWindow)
            {
                Builder.ActiveWindow.closing += Close;
            }
        }

        private void OnDisable()
        {
            if (Builder.ActiveWindow)
            {
                Builder.ActiveWindow.closing -= Close;
            }
            m_View.closing?.Invoke();
            m_View = null;
            s_Window = null;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // Load assets.
            var mainUSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_BuilderWindow);
            var mainInspectorUSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorWindow);
            var themeUSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_BuilderWindow_Themed);
            var themeInspectorUSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorWindow_Themed);

            // Load styles.
            root.styleSheets.Add(mainUSS);
            root.styleSheets.Add(themeUSS);
            root.styleSheets.Add(mainInspectorUSS);
            root.styleSheets.Add(themeInspectorUSS);

            m_View = new BuilderBindingView();
            rootVisualElement.Add(m_View);
            m_View.closeRequested += Close;
        }
    }
}
