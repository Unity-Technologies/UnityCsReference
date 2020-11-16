using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorWindow : BuilderPaneWindow
    {
        BuilderInspector m_Inspector;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " Inspector")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderInspectorWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder Inspector");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var selection = document.primaryViewportWindow?.selection;
            if (selection == null)
                return;

            m_Inspector = new BuilderInspector(this, selection);

            selection.AddNotifier(m_Inspector);

            root.Add(m_Inspector);
        }

        public override void ClearUI()
        {
            if (m_Inspector == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection == null)
                return;

            selection.RemoveNotifier(m_Inspector);

            m_Inspector.RemoveFromHierarchy();
            m_Inspector = null;
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            m_Inspector.OnAfterBuilderDeserialize();
        }
    }
}
