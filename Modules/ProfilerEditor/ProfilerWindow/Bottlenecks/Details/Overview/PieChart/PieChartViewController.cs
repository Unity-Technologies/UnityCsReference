// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Accessibility;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // Displays a pie chart alongside its values, as defined by a PieChartModel.
    class PieChartViewController : ViewController
    {
        // Model.
        readonly string m_Title;
        // Optional resolver invoked on the main thread to re-resolve a segment's colour from its
        // DataSeriesIndex when the colour-blind accessibility setting changes. When null, segment
        // colours are taken straight from the model.
        readonly Func<int, Color> m_ColorResolver;
        PieChartModel? m_LastModel;

        // View.
        Label m_TitleLabel;
        PieChart m_Chart;
        VisualElement m_KeyContainer;
        List<PieChartKeyItem> m_KeyItems = new();
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public PieChartViewController(string title, Func<int, Color> colorResolver = null)
        {
            m_Title = title;
            m_ColorResolver = colorResolver;
            if (m_ColorResolver != null)
                UserAccessiblitySettings.colorBlindConditionChanged += OnColorBlindSettingChanged;
        }

        public void RefreshView(PieChartModel model)
        {
            if (IsViewLoaded == false)
                throw new InvalidOperationException("Cannot refresh view; view has not been loaded.");

            // Re-resolve any palette-driven colours from the current colour-blind setting
            // before caching, so a setting change between async builds is reflected.
            if (m_ColorResolver != null)
                model = WithCurrentPaletteColors(model);
            m_LastModel = model;

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

        protected override void Dispose(bool disposing)
        {
            if (disposing && m_ColorResolver != null)
                UserAccessiblitySettings.colorBlindConditionChanged -= OnColorBlindSettingChanged;

            base.Dispose(disposing);
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

        void OnColorBlindSettingChanged()
        {
            if (IsViewLoaded == false || m_LastModel.HasValue == false)
                return;

            RefreshView(m_LastModel.Value);
        }

        PieChartModel WithCurrentPaletteColors(PieChartModel model)
        {
            var segments = model.Segments;
            if (segments == null || segments.Length == 0)
                return model;

            var updated = new PieChartModel.Segment[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var color = segment.DataSeriesIndex.HasValue
                    ? m_ColorResolver(segment.DataSeriesIndex.Value)
                    : segment.Color;
                updated[i] = new PieChartModel.Segment(color, segment.Name, segment.Percentage, segment.DataSeriesIndex);
            }
            return new PieChartModel(updated);
        }
    }
}
