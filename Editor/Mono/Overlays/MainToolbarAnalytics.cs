// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class MainToolbarAnalytics : IDisposable
    {
        [Serializable]
        class PresetChangedData : IAnalytic.IData
        {
            [SerializeField] public string presetName;
        }

        [AnalyticInfo(eventName: "toolbarPresetChanged", vendorKey: k_VendorKey)]
        class PresetChangedEvent : IAnalytic
        {
            PresetChangedData m_data = null;

            public PresetChangedEvent(string presetName)
            {
                m_data = new PresetChangedData
                {
                    presetName = presetName,
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
        }

        [Serializable]
        class ElementChangedData : IAnalytic.IData
        {
            [SerializeField] public string elementId;
        }

        [Serializable]
        class NewElementEventData : IAnalytic.IData
        {
            [SerializeField] public NewElementData[] elements;
        }

        [Serializable]
        class NewElementData : ElementChangedData
        {
            [SerializeField] public string sourceAssembly;
        }

        [AnalyticInfo(eventName: "toolbarNewElement", vendorKey: k_VendorKey, version: 4)]
        class NewElementEvent : IAnalytic
        {
            NewElementEventData m_data = null;

            public NewElementEvent(NewElementData[] elements)
            {
                m_data = new NewElementEventData
                {
                    elements = elements
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
        }

        [Serializable]
        class ElementOrderChangedData : ElementChangedData
        {
            [SerializeField] public string container; // {left, middle, right}
            [SerializeField] public int order; // Left to right
        }

        [AnalyticInfo(eventName: "toolbarElementOrderChanged", vendorKey: k_VendorKey)]
        class ElementOrderChangedEvent : IAnalytic
        {
            ElementOrderChangedData m_data = null;

            public ElementOrderChangedEvent(string elementId, string container, int order)
            {
                m_data = new ElementOrderChangedData
                {
                    elementId = elementId,
                    container = container,
                    order = order
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
        }

        [AnalyticInfo(eventName: "toolbarElementInteraction", vendorKey: k_VendorKey)]
        class ElementInteractionEvent : IAnalytic
        {
            ElementChangedData m_data = null;

            public ElementInteractionEvent(string elementId)
            {
                m_data = new ElementChangedData
                {
                    elementId = elementId
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
        }

        [Serializable]
        class ElementVisibilityChangedData : IAnalytic.IData
        {
            [SerializeField] public string[] shown;
            [SerializeField] public string[] hidden;
        }

        [AnalyticInfo(eventName: "toolbarElementVisibilityChanged", vendorKey: k_VendorKey, version: 2)]
        class ElementVisibilityChangedEvent : IAnalytic
        {
            ElementVisibilityChangedData m_data = null;

            public ElementVisibilityChangedEvent(string[] shown, string[] hidden)
            {
                m_data = new ElementVisibilityChangedData
                {
                    shown = shown,
                    hidden = hidden
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_data;
                return data != null;
            }
        }

        class OverlayData
        {
            public Action<bool> displayedChanged;
            public Action<VisualElement> contentRebuilt;
        }

        class OverlayVisibilityBatch
        {
            public List<string> shown = new List<string>(32);
            public List<string> hidden = new List<string>(32);

            public void Clear()
            {
                shown.Clear();
                hidden.Clear();
            }

            public bool HasAny()
            {
                return shown.Count > 0 || hidden.Count > 0;
            }
        }

        const string k_VendorKey = "unity.main-toolbar";
        const string k_NewElementKey = "unity.main-toolbar.analytics.new-element-sent";

        readonly OverlayCanvas m_TargetCanvas;
        readonly Dictionary<MainToolbarOverlay, OverlayData> m_OverlayData = new Dictionary<MainToolbarOverlay, OverlayData>();
        readonly OverlayVisibilityBatch m_VisibilityBatch = new OverlayVisibilityBatch();
        readonly List<NewElementData> m_NewElementBatch = new List<NewElementData>();

        public MainToolbarAnalytics(MainToolbarWindow window)
        {
            m_TargetCanvas = window.overlayCanvas;
            m_TargetCanvas.presetChanged += OnPresetChanged;
            foreach (var rawOverlay in m_TargetCanvas.overlays)
                if (rawOverlay is MainToolbarOverlay overlay)
                    RegisterOverlayEvents(overlay);

            foreach (var container in m_TargetCanvas.containers)
                if (container is MainToolbarOverlayContainer mtContainer)
                   for (int i = 0; i < mtContainer.sectionCount; ++i)
                       RegisterContainerSectionEvents(mtContainer.GetContainerSection(i));

            // Sending during onEnable seem to cause issues so we delay to the next frame
            EditorApplication.delayCall += UpdateNewMainToolbarOverlays;
        }

        public void Dispose()
        {
            m_TargetCanvas.presetChanged -= OnPresetChanged;
            foreach (var keyValue in m_OverlayData)
                UnregisterOverlayEvents(keyValue.Key, keyValue.Value);

            foreach (var container in m_TargetCanvas.containers)
                if (container is MainToolbarOverlayContainer mtContainer)
                    for (int i = 0; i < mtContainer.sectionCount; ++i)
                        UnregisterContainerSectionEvents(mtContainer.GetContainerSection(i));
        }

        void UpdateNewMainToolbarOverlays()
        {
            foreach (var overlay in m_TargetCanvas.GetNewMainToolbarOverlaysBuffer())
                SendNewElementAdded(overlay);

            m_TargetCanvas.ClearNewMainToolbarOverlaysBuffer();
        }

        void OnPresetChanged()
        {
            SendPresetChanged(m_TargetCanvas.lastAppliedPresetName);
            UpdateNewMainToolbarOverlays();
        }

        void RegisterOverlayEvents(MainToolbarOverlay overlay)
        {
            Action<bool> displayedChanged = (display) => OnOverlayDisplayChanged(overlay, display);
            Action<VisualElement> contentRebuilt = (toolbar) => OnOverlayContentRebuilt(overlay, toolbar);

            m_OverlayData.Add(overlay, new OverlayData
            {
                displayedChanged = displayedChanged,
                contentRebuilt = contentRebuilt
            });

            overlay.displayedChanged += displayedChanged;
            overlay.afterContentRebuilt += contentRebuilt;
        }

        void UnregisterOverlayEvents(MainToolbarOverlay overlay, OverlayData data)
        {
            overlay.displayedChanged -= data.displayedChanged;
            overlay.afterContentRebuilt -= data.contentRebuilt;

            var toolbar = overlay.rootVisualElement.Q<OverlayToolbar>();
            if (toolbar != null)
            {
                toolbar.Query().ForEach((ve) =>
                {
                    ve.UnregisterCallback<MouseDownEvent, MainToolbarOverlay>(OnMainToolbarElementInteracted, useTrickleDown: TrickleDown.TrickleDown);
                });
            }
        }

        void RegisterContainerSectionEvents(ContainerSection section)
        {
            section.overlayInserted += OnOverlayOrderChanged;
        }

        void UnregisterContainerSectionEvents(ContainerSection section)
        {
            section.overlayInserted -= OnOverlayOrderChanged;
        }

        void OnOverlayContentRebuilt(MainToolbarOverlay overlay, VisualElement toolbar)
        {
            toolbar.Query().ForEach((ve) =>
            {
                ve.RegisterCallback<MouseDownEvent, MainToolbarOverlay>(OnMainToolbarElementInteracted, overlay, useTrickleDown: TrickleDown.TrickleDown);
            });
        }

        void OnMainToolbarElementInteracted(MouseDownEvent evt, MainToolbarOverlay overlay)
        {
            // Ensure we only hit this event twice for the root element
            if (evt.currentTarget is not VisualElement target || target.parent is not OverlayToolbar)
                return;

            // Left click tracking only
            if (evt.button != 0)
                return;

            SendElementInteractionTriggered(overlay);
        }

        void OnOverlayDisplayChanged(MainToolbarOverlay overlay, bool displayed)
        {
            SendElementVisibilityChanged(overlay);
        }

        void OnOverlayOrderChanged(Overlay overlay, int index, DockingHint hint)
        {
            SendElementOrderChanged((MainToolbarOverlay)overlay);
        }

        void SendElementInteractionTriggered(MainToolbarOverlay overlay)
        {
            EditorAnalytics.SendAnalytic(new ElementInteractionEvent(overlay.id));
        }

        void SendPresetChanged(string newPreset)
        {
            EditorAnalytics.SendAnalytic(new PresetChangedEvent(newPreset));
        }

        void SendElementVisibilityChanged(MainToolbarOverlay overlay)
        {
            if (!m_VisibilityBatch.HasAny())
            {
                EditorApplication.delayCall += () =>
                {
                    EditorAnalytics.SendAnalytic(new ElementVisibilityChangedEvent(m_VisibilityBatch.shown.ToArray(), m_VisibilityBatch.hidden.ToArray()));
                    m_VisibilityBatch.Clear();
                };
            }

            if (overlay.displayed)
            {
                m_VisibilityBatch.shown.Add(overlay.id);
            }
            else
            {
                m_VisibilityBatch.hidden.Add(overlay.id);
            }
        }

        void SendNewElementAdded(MainToolbarOverlay overlay)
        {
            // Ensure that new elements are only sent once per session
            var key = $"{k_NewElementKey}.{overlay.id}";
            if (!SessionState.GetBool(key, false))
            {
                if (m_NewElementBatch.Count == 0)
                {
                    EditorApplication.delayCall += () =>
                    {
                        EditorAnalytics.SendAnalytic(new NewElementEvent(m_NewElementBatch.ToArray()));
                        m_NewElementBatch.Clear();
                    };
                }

                SessionState.SetBool(key, true);

                var assemblyName = overlay.createElementMethod != null
                    ? overlay.createElementMethod.DeclaringType.Assembly.FullName
                    : "MenuItemPin";
                m_NewElementBatch.Add(new NewElementData { elementId = overlay.id, sourceAssembly = assemblyName });
            }
        }

        void SendElementOrderChanged(MainToolbarOverlay overlay)
        {
            int sectionIdx = 0;
            if (!overlay.displayed || !overlay.container.GetOverlayIndex(overlay, out sectionIdx, out _))
                return;

            var section = overlay.container.GetContainerSection(sectionIdx);
            var currentCount = 0;
            var visibleIndex = -1;
            for (int i = 0; i < section.overlayCount; ++i)
            {
                var target = section.GetOverlay(i);
                if (target.displayed)
                {
                    if (overlay == target)
                    {
                        visibleIndex = currentCount;
                        break;
                    }
                    ++currentCount;
                }
            }

            // Right align toolbar is in inverted order
            if (sectionIdx == (int)OverlayContainerSection.AfterSpacer)
                visibleIndex = section.GetVisibleCount() - visibleIndex - 1;

            string container = "";
            switch (sectionIdx)
            {
                case 0: container = "Left"; break;
                case 1: container = "Right"; break;
                case 2: container = "Middle"; break;
            }

            EditorAnalytics.SendAnalytic(new ElementOrderChangedEvent(overlay.id, container, visibleIndex));
        }
    }
}
