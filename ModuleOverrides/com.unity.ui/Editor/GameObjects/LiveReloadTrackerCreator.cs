// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal static class LiveReloadTrackerCreator
    {
        static LiveReloadTrackerCreator()
        {
            UIDocument.CreateLiveReloadVisualTreeAssetTracker = CreateVisualTreeAssetTrackerInstance;

            DefaultEditorWindowBackend.SetupLiveReloadPanelTrackers = PanelSettings.SetupLiveReloadPanelTrackers;
        }

        internal static ILiveReloadAssetTracker<VisualTreeAsset> CreateVisualTreeAssetTrackerInstance(UIDocument owner)
        {
            return new UIDocumentVisualTreeAssetTracker(owner);
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
