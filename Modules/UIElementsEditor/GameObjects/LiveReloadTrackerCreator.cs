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
            PanelRenderer.CreateLiveReloadVisualTreeAssetTracker = CreateVisualTreeAssetTrackerInstance;

            DefaultEditorWindowBackend.SetupLiveReloadPanelTrackers = PanelSettings.SetupLiveReloadPanelTrackers;
        }

        internal static ILiveReloadAssetTracker<VisualTreeAsset> CreateVisualTreeAssetTrackerInstance(IPanelComponent owner)
        {
            return new PanelComponentVisualTreeAssetTracker(owner);
        }
    }

    internal class PanelComponentVisualTreeAssetTracker : BaseLiveReloadVisualTreeAssetTracker 
    {
        IPanelComponent m_Owner;

        public PanelComponentVisualTreeAssetTracker(IPanelComponent owner)
        {
            m_Owner = owner;
        }

        internal override void OnVisualTreeAssetChanged()
        {
            if (m_Owner != null)
            {
                m_Owner.HandleLiveReload();
            }
        }
    }
}
