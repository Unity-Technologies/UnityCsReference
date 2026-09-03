// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderInspectorWindow() {}
        #pragma warning restore UAL0015

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
