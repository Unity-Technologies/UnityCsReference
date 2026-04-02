// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class ViewComponent : Component<ViewData>
    {
        public TimeRange displayRange { get; set; }
        public float verticalScrollOffset { get; set; }
        public float headerWidth { get; set; }

        protected override ViewData GenerateReadOnlyData()
        {
            return new ViewData(displayRange,
                verticalScrollOffset,
                headerWidth);
        }

        public void CopyFrom(ViewData viewData)
        {
            displayRange = viewData.displayRange;
            verticalScrollOffset = viewData.verticalScrollOffset;
            headerWidth = viewData.headerWidth;
        }
    }
}
