// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    // An internal module view controller responsible for drawing legacy IMGUI-based Profiler modules.
    internal class LegacyDetailsViewController : ProfilerModuleViewController
    {
        ProfilerModuleBase m_Module;

        public LegacyDetailsViewController(ProfilerWindow profilerWindow, ProfilerModuleBase module) : base(profilerWindow)
        {
            m_Module = module;
        }

        protected override VisualElement CreateView()
        {
            return new IMGUIContainer(DrawDetailsViewViaLegacyIMGUIMethods)
            {
                style =
                {
                    flexGrow = 1
                }
            };
        }

        void DrawDetailsViewViaLegacyIMGUIMethods()
        {
            using (Markers.drawLegacyDetailsView.Auto())
            {
                var detailsViewContainer = ProfilerWindow.DetailsViewContainer;
                var resolvedStyle = detailsViewContainer.resolvedStyle;
                var detailsViewBounds = new Rect(0, 0, resolvedStyle.width, resolvedStyle.height);
                var detailsViewToolbarBounds = detailsViewBounds;
                detailsViewToolbarBounds.height = EditorStyles.contentToolbar.CalcHeight(GUIContent.none, 10.0f);
                m_Module.DrawToolbar(detailsViewToolbarBounds);

                detailsViewBounds.yMin += detailsViewToolbarBounds.height;
                m_Module.DrawDetailsView(detailsViewBounds);
            }
        }

        static class Markers
        {
            // The name of the marker is "ProfilerWindow.DrawDetailsView" for comparing with old performance tests.
            public static readonly ProfilerMarker drawLegacyDetailsView = new ProfilerMarker("ProfilerWindow.DrawDetailsView");
        }
    }
}
