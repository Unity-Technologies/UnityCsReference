// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UI.Builder
{
    /// <summary>
    ///  Window used to create and edit bindings.
    /// </summary>
    partial class BuilderBindingWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderBindingWindow() {}
        #pragma warning restore UAL0015

        [AutoStaticsCleanupOnCodeReload]
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
        public static BuilderBindingWindow Open(bool openToCreate, string property, BuilderInspector inspector, Rect pos, Vector2 windowSize)
        {
            var title = openToCreate ? BuilderConstants.AddBindingTitle : BuilderConstants.EditBindingTitle;

            s_Window = GetWindow<BuilderBindingWindow>(true, title);
            s_Window.position = new Rect(pos.position, windowSize);

            s_Window.view.StartCreatingOrEditingBinding(property, openToCreate, inspector);
            return s_Window;
        }

        void OnEnable()
        {
            // Ensure this window closes when the UIBuilder window closes.
            if (Builder.ActiveWindow)
            {
                Builder.ActiveWindow.closing += Close;
            }

            AssemblyReloadEvents.beforeAssemblyReload += Close;
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

            AssemblyReloadEvents.beforeAssemblyReload -= Close;
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
