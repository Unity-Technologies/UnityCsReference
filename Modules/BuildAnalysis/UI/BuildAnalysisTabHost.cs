// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class BuildAnalysisTabHost
    {
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

        public void SetSelection(BuildEntry selection, BuildAnalysis analysis)
        {
            foreach (var registration in m_TabRegistrations)
                registration.TabView.SetSelection(selection, analysis);
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
    }
}
