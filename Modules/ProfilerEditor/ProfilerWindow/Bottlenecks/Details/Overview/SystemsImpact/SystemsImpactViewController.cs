// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class SystemsImpactViewController : ViewController
    {
        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly string m_Title;
        readonly ProfilerWindow m_ProfilerWindow;
        readonly IDetailsElementBinder m_DetailsBinder;
        SystemsImpactModel? m_Model;

        // View.
        readonly List<SystemImpactItem> m_Items = new();
        Label m_TitleLabel;
        VisualElement m_ItemsContainer;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public SystemsImpactViewController(
            IProfilerCaptureDataService dataService,
            string title,
            ProfilerWindow profilerWindow,
            IDetailsElementBinder detailsBinder)
        {
            m_DataService = dataService;
            m_Title = title;
            m_ProfilerWindow = profilerWindow;
            m_DetailsBinder = detailsBinder;
            UserAccessiblitySettings.colorBlindConditionChanged += OnColorBlindSettingChanged;
        }

        public void ReloadData(SystemsImpactModel model)
        {
            m_Model = model;
            if (IsViewLoaded)
                RefreshView();
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("SystemsImpactView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "systems-impact-view__dark";
            const string k_UssClass_Light = "systems-impact-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();
            m_TitleLabel.text = m_Title;

            if (m_Model.HasValue)
                RefreshView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UserAccessiblitySettings.colorBlindConditionChanged -= OnColorBlindSettingChanged;
                foreach (var item in m_Items)
                    m_DetailsBinder.UnbindDetailsElement(item);
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("systems-impact-view__title-label");
            m_ItemsContainer = view.Q<VisualElement>("systems-impact-view__items-container");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("systems-impact-view__activity-overlay");
        }

        void RefreshView()
        {
            if (m_Model.HasValue == false)
                return;

            var model = m_Model.Value;
            var systemImpacts = model.Data;
            if (systemImpacts.Length == 0)
                return;

            var highestSystemImpactAsFloat = Convert.ToSingle(systemImpacts[0].DurationNs);
            if (highestSystemImpactAsFloat <= 0f)
                return;

            var itemIndex = 0;
            foreach (var systemImpact in systemImpacts)
            {
                // Reuse an existing item, if possible, or create a new one. This allows for
                // transition animations to happen on the items when the view is refreshed.
                SystemImpactItem item;
                if (itemIndex < m_Items.Count)
                {
                    item = m_Items[itemIndex];
                    // Unbind before rebinding to avoid retaining a stale provider on reused items.
                    m_DetailsBinder.UnbindDetailsElement(item);
                }
                else
                {
                    item = new SystemImpactItem();
                    m_ItemsContainer.Add(item);
                    m_Items.Add(item);
                }

                var normalizedDuration = Convert.ToSingle(systemImpact.DurationNs) / highestSystemImpactAsFloat;
                item.Configure(systemImpact, normalizedDuration);
                m_DetailsBinder.BindDetailsElement(item, new SystemImpactDetailsProvider(m_ProfilerWindow, systemImpact, model.FrameRange));

                itemIndex++;
            }

            // Destroy any unused items.
            while (itemIndex < m_Items.Count)
            {
                var item = m_Items[itemIndex];
                m_Items.RemoveAt(itemIndex);
                item.RemoveFromHierarchy();
            }

            SetActivityIndicatorVisible(false);
        }

        void OnColorBlindSettingChanged()
        {
            if (IsViewLoaded == false)
                return;

            RefreshView();
        }
    }
}
