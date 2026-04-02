// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    // ViewController for displaying general marker details (frame, occurrences)
    class MarkerGeneralDetailsViewController : ViewController
    {
        const string k_UxmlResourceName = "GeneralMarkerDetailsView.uxml";
        const string k_UssClass_Dark = "general-marker-details-view__dark";
        const string k_UssClass_Light = "general-marker-details-view__light";

        static class Content
        {
            public static readonly string k_FrameLabel = L10n.Tr("Frame");
            public static readonly string k_OccurrencesLabel = L10n.Tr("Occurrences");
        }

        readonly Marker m_Marker;
        readonly IProfilerCaptureDataService m_DataService;

        // View elements
        ReadOnlyFloatField m_FrameField;
        ReadOnlyFloatField m_CountField;
        TextElement m_DescriptionElement;

        public MarkerGeneralDetailsViewController(
            Marker marker,
            IProfilerCaptureDataService dataService)
        {
            m_Marker = marker;
            m_DataService = dataService;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeClass = EditorGUIUtility.isProSkin ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            PopulateFields();
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_FrameField = view.Q<ReadOnlyFloatField>("general-marker-details-view__frame-field");
            m_CountField = view.Q<ReadOnlyFloatField>("general-marker-details-view__count-field");
            m_DescriptionElement = view.Q<TextElement>("general-marker-details-view__description");
        }

        void PopulateFields()
        {
            // Set frame field
            if (m_FrameField != null)
            {
                m_FrameField.Label.text = Content.k_FrameLabel;
                m_FrameField.ValueLabel.text = FrameIndexFormatterUtility.DisplayStringForFrameIndex(m_Marker.FrameIndex);
            }

            // Set count field
            if (m_CountField != null)
            {
                m_CountField.Label.text = Content.k_OccurrencesLabel;
                m_CountField.ValueLabel.text = $"{m_Marker.NumberOfInstances:N0}";
            }

            // Set description
            if (m_DescriptionElement != null)
            {
                var description = MarkersInformationProvider.GetMarkerInfo(m_Marker.Name);
                if (!string.IsNullOrEmpty(description))
                {
                    m_DescriptionElement.text = description;
                    UIUtility.SetElementDisplay(m_DescriptionElement, true);
                }
                else
                {
                    UIUtility.SetElementDisplay(m_DescriptionElement, false);
                }
            }
        }
    }
}
