// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    internal class MarkerDetailsPanelViewController : ViewController
    {
        static class Content
        {
            public static readonly string k_TabLabel_General = L10n.Tr("General");
            public static readonly string k_TabLabel_Instances = L10n.Tr("Instances");
            public static readonly string k_ViewButtonLabel = L10n.Tr("View");
            public static readonly string k_ViewButtonTooltip = L10n.Tr("Show the selected marker in the Hierarchy View of CPU Profiler module");
        }

        private readonly Marker m_Marker;
        private readonly IProfilerCaptureDataService m_ProfilerCaptureDataService;
        private readonly Action m_OnViewButtonClicked;

        // Child view controllers
        private MarkerGeneralDetailsViewController m_GeneralViewController;
        private MarkerInstancesDetailsViewController m_InstancesViewController;

        public MarkerDetailsPanelViewController(
            Marker marker,
            IProfilerCaptureDataService dataService,
            Action onViewButtonClicked)
        {
            m_Marker = marker;
            m_ProfilerCaptureDataService = dataService;
            m_OnViewButtonClicked = onViewButtonClicked;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("MarkerDetailsPanel.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "marker-details-panel__dark";
            const string k_UssClass_Light = "marker-details-panel__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            SetupHeader();
            SetupTabs();

            UpdateInstancesView();
        }

        void SetupHeader()
        {
            // Set marker name
            var markerNameLabel = View.Q<Label>("marker-details-panel-header-markername");
            if (markerNameLabel != null)
                markerNameLabel.text = m_Marker.Name;

            // Set marker value
            var markerValueLabel = View.Q<Label>("marker-details-panel-header-value");
            if (markerValueLabel != null)
                markerValueLabel.text = m_Marker.FormatValue();

            // Set up view button
            var viewButton = View.Q<Button>("marker-details-panel-header-view-button");
            if (viewButton != null)
            {
                viewButton.text = Content.k_ViewButtonLabel;
                viewButton.tooltip = Content.k_ViewButtonTooltip;
                viewButton.clicked += OnViewButtonClicked;
            }
        }

        void SetupTabs()
        {
            var tabView = View.Q<TabView>("marker-details-panel-tabs");
            if (tabView == null)
                return;

            // Set view-data-key on TabView to enable automatic state persistence
            tabView.viewDataKey = "marker-details-panel-tabs-state";

            // Create General tab
            m_GeneralViewController = CreateTab(
                tabView,
                "marker-details-panel-tabs__general",
                Content.k_TabLabel_General,
                () => new MarkerGeneralDetailsViewController(m_Marker, m_ProfilerCaptureDataService)
            );

            // Create Instances tab
            m_InstancesViewController = CreateTab(
                tabView,
                "marker-details-panel-tabs__instances",
                Content.k_TabLabel_Instances,
                () => new MarkerInstancesDetailsViewController(m_ProfilerCaptureDataService)
            );
        }

        T CreateTab<T>(TabView tabView, string tabName, string tabLabel, Func<T> createViewController) where T : ViewController
        {
            var tabViewDataKey = $"{tabName}-tab";
            var contentContainerName = $"{tabName}-content";

            var tab = new Tab(tabLabel)
            {
                name = tabName,
                viewDataKey = tabViewDataKey
            };

            var contentContainer = new VisualElement
            {
                name = contentContainerName
            };
            contentContainer.AddToClassList("marker-details-panel-tabs__content");

            tab.Add(contentContainer);
            tabView.Add(tab);

            var viewController = createViewController();
            AddChild(viewController);
            contentContainer.Add(viewController.View);

            return viewController;
        }

        void UpdateInstancesView()
        {
            if (m_InstancesViewController == null || !m_InstancesViewController.IsViewLoaded)
                return;

            m_InstancesViewController.ReloadData(m_Marker);
        }

        void OnViewButtonClicked()
        {
            m_OnViewButtonClicked?.Invoke();
        }
    }
}
