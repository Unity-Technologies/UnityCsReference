// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static partial class LiveReloadTrackerCreator
    {
        // These factory slots are cleared on code reload, so they have to be reinstalled on every load.
        // A static constructor would only run once per domain, after which UXML live reload silently stops
        // working for UIDocument and PanelRenderer — and UIDocument invokes the factory without a null
        // check, so it would throw rather than degrade.
        [OnCodeLoaded]
        static void Initialize()
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
