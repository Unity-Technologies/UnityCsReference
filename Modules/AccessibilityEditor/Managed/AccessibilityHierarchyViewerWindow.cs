// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        private static string s_WindowTitle = "Accessibility Hierarchy Viewer";

        private AccessibilityHierarchyViewModel m_ActiveHierarchyModel;

        [MenuItem("Window/Accessibility/Hierarchy Viewer", false, 3006)]
        public static void ShowWindow()
        {
            GetWindow<AccessibilityHierarchyViewerWindow>();
        }

        private void OnEnable()
        {
            minSize = new Vector2(200, 200);
            titleContent = new GUIContent(L10n.Tr(s_WindowTitle));
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
