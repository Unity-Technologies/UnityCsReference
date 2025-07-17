// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // Displays a pie chart alongside its values, as defined by a PieChartModel.
    class PieChartViewController : ViewController
    {
        // Model.
        readonly string m_Title;

        // View.
        Label m_TitleLabel;
        PieChart m_Chart;
        VisualElement m_KeyContainer;
        List<PieChartKeyItem> m_KeyItems = new();
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public PieChartViewController(string title)
        {
            m_Title = title;
        }

        public void RefreshView(PieChartModel model)
        {
            if (IsViewLoaded == false)
                throw new InvalidOperationException("Cannot refresh view; view has not been loaded.");

            var segments = model.Segments;
            var hasData = segments != null && segments.Length > 0;

            // Reload chart.
            if (hasData)
                m_Chart.ReloadData(model);

            // Reload key.
            var itemIndex = 0;
            if (hasData)
            {
                foreach (var segment in segments)
                {
                    // Reuse an existing item if possible, or create a new one.
                    PieChartKeyItem item;
                    if (itemIndex < m_KeyItems.Count)
                    {
                        item = m_KeyItems[itemIndex];
                    }
                    else
                    {
                        item = new PieChartKeyItem();
                        m_KeyContainer.Add(item);
                        m_KeyItems.Add(item);
                    }

                    item.Configure(segment);

                    itemIndex++;
                }
            }

            // Destroy any unused key items.
            while (itemIndex < m_KeyItems.Count)
            {
                var item = m_KeyItems[itemIndex];
                m_KeyItems.RemoveAt(itemIndex);
                item.RemoveFromHierarchy();
            }

            SetNoDataLabelVisible(!hasData);
            SetActivityIndicatorVisible(false);
        }

        public void SetActivityIndicatorVisible(bool visible)
        {
            if (visible)
                m_ActivityOverlay.Show();
            else
                m_ActivityOverlay.Hide();
        }

        public void ShowActivityIndicatorAfterDelay(int delayMs)
        {
            m_ActivityOverlay.ShowAfterDelay(delayMs);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("PieChartView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "pie-chart-view__dark";
            const string k_UssClass_Light = "pie-chart-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_NoDataLabel.text = "No data found";
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("pie-chart-view__title-label");
            m_Chart = view.Q<PieChart>("pie-chart-view__chart");
            m_KeyContainer = view.Q<VisualElement>("pie-chart-view__key");
            m_NoDataLabel = view.Q<Label>("pie-chart-view__no-data-label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("pie-chart-view__activity-overlay");
        }

        void SetNoDataLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_NoDataLabel, visible);
        }
    }
}
