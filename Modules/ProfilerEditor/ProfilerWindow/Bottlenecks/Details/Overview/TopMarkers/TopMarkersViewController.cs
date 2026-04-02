// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    // Displays a leaderboard-like list of profiler markers, as defined by a TopMarkersModel.
    class TopMarkersViewController : ViewController
    {
        static class Content
        {
            public static readonly string k_FrameHeaderLabel = L10n.Tr("Frame");
        }

        // Model.
        readonly string m_Title;
        readonly ProfilerWindow m_ProfilerWindow;
        readonly Action m_Action;
        readonly IResponder m_Responder;
        private readonly IDetailsElementBinder m_DetailsBinder;

        // View.
        readonly List<TopMarkerItem> m_Items = new();
        Label m_TitleLabel;
        Label m_HeaderFrameLabel;
        VisualElement m_ItemsContainer;
        Label m_NoDataLabel;
        ActivityIndicatorOverlay m_ActivityOverlay;

        public TopMarkersViewController(string title, ProfilerWindow profilerWindow, Action action, IResponder responder, IDetailsElementBinder detailsBinder)
        {
            m_Title = title;
            m_ProfilerWindow = profilerWindow;
            m_Action = action;
            m_Responder = responder;
            m_DetailsBinder = detailsBinder;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in m_Items)
                {
                    m_DetailsBinder.UnbindDetailsElement(item);
                }
            }
            base.Dispose(disposing);
        }

        public void RefreshView(TopMarkersModel model)
        {
            if (IsViewLoaded == false)
                throw new InvalidOperationException("Cannot refresh view; view has not been loaded.");

            var markers = model.Markers;
            var itemIndex = 0;
            var hasData = markers.Length > 0;
            if (hasData)
            {
                var topMarkerValueAsFloat = Convert.ToSingle(markers[0].Value);
                foreach (var marker in markers)
                {
                    // Reuse an existing item, if possible, or create a new one. This allows for
                    // transition animations to happen on the items when the view is refreshed.
                    TopMarkerItem item;
                    if (itemIndex < m_Items.Count)
                    {
                        item = m_Items[itemIndex];
                    }
                    else
                    {
                        item = new TopMarkerItem();
                        m_ItemsContainer.Add(item);
                        m_Items.Add(item);
                    }

                    var markerValueNormalized = marker.Value / topMarkerValueAsFloat;
                    var title = m_Action switch
                    {
                        Action.ChangeSelectedFrame => FrameIndexFormatterUtility.DisplayStringForFrameIndex(marker.FrameIndex),
                        Action.SwitchToCpuModule => null,
                        _ => throw new ArgumentOutOfRangeException("Unknown action type."),
                    };
                    item.Configure(marker, markerValueNormalized, title);

                    m_DetailsBinder.BindDetailsElement(item, new TopMarkerDetailsProvider(m_ProfilerWindow, marker, m_Responder));

                    itemIndex++;
                }
            }

            // Destroy any unused items.
            while (itemIndex < m_Items.Count)
            {
                var item = m_Items[itemIndex];
                m_Items.RemoveAt(itemIndex);
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
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("TopMarkersView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "top-markers-view__dark";
            const string k_UssClass_Light = "top-markers-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_TitleLabel.text = m_Title;
            m_NoDataLabel.text = "No markers found";

            // Set localized text for frame header label
            if (m_HeaderFrameLabel != null)
            {
                m_HeaderFrameLabel.text = Content.k_FrameHeaderLabel;
                // Hide frame header label when displaying single frame summary
                UIUtility.SetElementDisplay(m_HeaderFrameLabel, m_Action == Action.ChangeSelectedFrame);
            }
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>("top-markers-view__title-label");
            m_HeaderFrameLabel = view.Q<Label>("top-markers-view__frame-label");
            m_ItemsContainer = view.Q<VisualElement>("top-markers-view__items-container");
            m_NoDataLabel = view.Q<Label>("top-markers-view__no-data-label");
            m_ActivityOverlay = view.Q<ActivityIndicatorOverlay>("top-markers-view__activity-overlay");
        }

        void SetNoDataLabelVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_NoDataLabel, visible);
        }

        public enum Action
        {
            ChangeSelectedFrame,
            SwitchToCpuModule,
        }

        public interface IResponder
        {
            void OnMarkerSelected(Marker marker, Action action);
        }
    }
}
