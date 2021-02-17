using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class LiveReloadTrackerCreator
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UIDocument.CreateLiveReloadVisualTreeAssetTracker = CreateVisualTreeAssetTrackerInstance;
            PanelSettings.CreateLiveReloadStyleSheetAssetTracker = CreateStyleSheetAssetTrackerInstance;

            DefaultEditorWindowBackend.SetupLiveReloadPanelTrackers = PanelSettings.SetupLiveReloadPanelTrackers;
        }

        internal static ILiveReloadAssetTracker<VisualTreeAsset> CreateVisualTreeAssetTrackerInstance(UIDocument owner)
        {
            return new UIDocumentVisualTreeAssetTracker(owner);
        }

        internal static ILiveReloadAssetTracker<StyleSheet> CreateStyleSheetAssetTrackerInstance()
        {
            if (DefaultEditorWindowBackend.IsGameViewWindowLiveReloadOn())
                return new LiveReloadStyleSheetAssetTracker();

            return null;
        }
    }

    internal class UIDocumentVisualTreeAssetTracker : BaseLiveReloadVisualTreeAssetTracker
    {
        private UIDocument m_Owner;

        public UIDocumentVisualTreeAssetTracker(UIDocument owner)
        {
            m_Owner = owner;
        }

        internal override void OnVisualTreeAssetChanged()
        {
            if (m_Owner.rootVisualElement != null)
            {
                m_Owner.HandleLiveReload();
            }
        }
    }
}
