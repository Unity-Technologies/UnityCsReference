// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class TrackCell : VisualElement
    {
        const int k_HeaderIndex = 0;
        const int k_ContentIndex = 1;

        const string k_Controls = "cellControls";
        const string k_Contents = "cellContents";

        const string k_StyleName = "tree-view__track-cell";
        const string k_ControlsStyleName = "tree-view__track-cell-controls";
        const string k_ContentsStyleName = "tree-view__track-cell-contents";

        public TrackCell()
        {
            name = nameof(TrackCell);
            this.AddToTimelineClassList(k_StyleName);

            var controls = new VisualElement { name = k_Controls };
            controls.AddToTimelineClassList(k_ControlsStyleName);
            Add(controls);

            var contents = new VisualElement { name = k_Contents };
            contents.AddToTimelineClassList(k_ContentsStyleName);
            Add(contents);
        }

        public static void ResizeAllCells(VisualElement container, float contentWidth)
        {
            UQueryBuilder<TrackCell> query = container.Query<TrackCell>();
            foreach (TrackCell cell in query.Build())
            {
                cell.ResizeContents(contentWidth);
            }
        }

        public void BindTo(ITrackHeaderElement header) => BindTo(this[k_HeaderIndex], header);
        public void BindTo(ITrackElement content) => BindTo(this[k_ContentIndex], content);

        public void ResizeContents(float contentsWidth)
        {
            this[k_ContentIndex].style.width = contentsWidth;
        }

        static void BindTo(VisualElement target, ITrackElement trackElement)
        {
            if (GetBoundTrackElement(target) == trackElement)
                return;

            target.Clear();

            if (trackElement is VisualElement t)
                target.Add(t);
        }

        static ITrackElement GetBoundTrackElement(VisualElement element)
        {
            return GetFirstChild(element) as ITrackElement;
        }

        static VisualElement GetFirstChild(VisualElement element)
        {
            return element.childCount > 0 ? element[0] : null;
        }
    }
}
