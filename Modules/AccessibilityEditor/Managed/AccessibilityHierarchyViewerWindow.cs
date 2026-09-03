// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Accessibility not yet converted
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace UnityEditor.Accessibility
{
    /// <summary>
    /// A window that displays the active accessibility hierarchy.
    /// </summary>
    internal class AccessibilityHierarchyViewerWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal AccessibilityHierarchyViewerWindow() { }
        #pragma warning restore UAL0015

        private static readonly string s_WindowTitle = "Accessibility Hierarchy Viewer";

        private AccessibilityHierarchyViewModel m_ActiveHierarchyModel;

        [MenuItem("Window/Accessibility/Hierarchy Viewer", false, 3006)]
        public static void ShowWindow()
        {
            GetWindow<AccessibilityHierarchyViewerWindow>();
        }

        private void OnEnable()
        {
            minSize = new Vector2(200, 200);
            titleContent = new GUIContent(L10n.Tr(s_WindowTitle, null));
            AssistiveSupport.activeHierarchyChanged += OnActiveHierarchyChanged;
        }

        private void OnDisable()
        {
            AssistiveSupport.activeHierarchyChanged -= OnActiveHierarchyChanged;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            var viewer = new AccessibilityHierarchyViewer();

            m_ActiveHierarchyModel = new AccessibilityHierarchyViewModel();
            viewer.hierarchyModel = m_ActiveHierarchyModel;

            root.Add(viewer);
            viewer.StretchToParentSize();

            OnActiveHierarchyChanged(AssistiveSupport.activeHierarchy);
        }

        private void OnActiveHierarchyChanged(AccessibilityHierarchy hierarchy)
        {
            if (m_ActiveHierarchyModel == null)
                return;

            m_ActiveHierarchyModel.accessibilityHierarchy = hierarchy;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
