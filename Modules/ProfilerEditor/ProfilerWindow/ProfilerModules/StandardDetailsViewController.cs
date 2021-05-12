// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class StandardDetailsViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "StandardDetailsView.uxml";
        const string k_UssSelector_StandardDetailsView__Label = "standard-details-view__label";

        // Data
        ProfilerCounterDescriptor[] m_Counters;

        // UI
        Label m_Label;

        public StandardDetailsViewController(ProfilerWindow profilerWindow, ProfilerCounterDescriptor[] counters) : base(profilerWindow)
        {
            m_Counters = counters;
        }

        protected override VisualElement CreateView()
        {
            var template = EditorGUIUtility.Load(k_UxmlResourceName) as VisualTreeAsset;
            var view = template.Instantiate();
            m_Label = view.Q<Label>(name: k_UssSelector_StandardDetailsView__Label);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            SubscribeToExternalEvents();

            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnsubscribeFromExternalEvents();
            base.Dispose(disposing);
        }

        void SubscribeToExternalEvents()
        {
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
        }

        void UnsubscribeFromExternalEvents()
        {
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            ReloadData(selectedFrameIndex);
        }

        void ReloadData(long selectedFrameIndex)
        {
            m_Label.text = ConstructTextSummaryOfCounters(selectedFrameIndex);
        }

        string ConstructTextSummaryOfCounters(long selectedFrameIndex)
        {
            var selectedFrameIndexInt = System.Convert.ToInt32(selectedFrameIndex);
            var stringBuilder = new System.Text.StringBuilder();
            foreach (var counter in m_Counters)
            {
                var counterValue = UnityEditorInternal.ProfilerDriver.GetFormattedCounterValue(selectedFrameIndexInt, counter.CategoryName, counter.Name);
                stringBuilder.AppendLine($"{counter}: {counterValue}");
            }

            return stringBuilder.ToString();
        }
    }
}
