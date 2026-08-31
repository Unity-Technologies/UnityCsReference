// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class BuildAnalysisTabHost
    {
        static readonly ProfilerMarker s_SetSelectionMarker = new ProfilerMarker("BuildAnalysisTabHost.SetSelection");

        private readonly TabView m_TabView;
        private readonly List<TabRegistration> m_TabRegistrations = new List<TabRegistration>();

        private struct TabRegistration
        {
            public Tab Tab;
            public IBuildAnalysisTabView TabView;
        }

        public BuildAnalysisTabHost(TabView tabView)
        {
            m_TabView = tabView;

            if (m_TabView != null)
                m_TabView.activeTabChanged += OnActiveTabChanged;
        }

        public void Register(Tab tab, IBuildAnalysisTabView tabView)
        {
            if (tab == null)
                throw new ArgumentNullException(nameof(tab));
            if (tabView == null)
                throw new ArgumentNullException(nameof(tabView));

            var targetContainer = tab.contentContainer;
            if (targetContainer == null)
                throw new InvalidOperationException($"Tab '{tab.name}' does not expose a content container.");

            tabView.Initialize();
            targetContainer.Clear();
            targetContainer.Add(tabView.Root);

            m_TabRegistrations.Add(new TabRegistration
            {
                Tab = tab,
                TabView = tabView,
            });
        }

        public void RegisterShortcuts(VisualElement root)
        {
            if (root == null)
                return;

            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Tab || !evt.ctrlKey)
                    return;

                SelectNextTab(evt.shiftKey);

                // Consume it so the tab character isn't typed into a focused field.
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        public void SetSelection(BuildEntry selection, BuildAnalysis analysis)
        {
            using (s_SetSelectionMarker.Auto())
            {
                foreach (var registration in m_TabRegistrations)
                    registration.TabView.SetSelection(selection, analysis);
            }
        }

        public void NotifyCurrentTabVisibility()
        {
            OnActiveTabChanged(null, m_TabView?.activeTab);
        }

        public void SetInspectorOpen(bool isOpen)
        {
            foreach (var registration in m_TabRegistrations)
                registration.TabView.OnInspectorVisibilityChanged(isOpen);
        }

        private void OnActiveTabChanged(Tab previousTab, Tab newTab)
        {
            foreach (var registration in m_TabRegistrations)
            {
                var isVisible = ReferenceEquals(registration.Tab, newTab);
                registration.TabView.OnTabVisibilityChanged(isVisible);
            }
        }

        private void SelectNextTab(bool reverse)
        {
            if (m_TabView == null || m_TabRegistrations.Count == 0)
                return;

            var count = m_TabRegistrations.Count;
            var current = m_TabRegistrations.FindIndex(r => ReferenceEquals(r.Tab, m_TabView.activeTab));
            if (current < 0)
                current = 0;

            var next = current + (reverse ? -1 : 1);
            if (next < 0)
                next = count - 1;
            else if (next >= count)
                next = 0;

            m_TabView.activeTab = m_TabRegistrations[next].Tab;
        }
    }
}
