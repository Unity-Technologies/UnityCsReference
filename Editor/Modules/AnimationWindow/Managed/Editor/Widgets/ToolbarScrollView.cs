// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    [UxmlElement]
    partial class ToolbarScrollView : ScrollView
    {
        public const string mainUssClassName = "animation-toolbar-scroll-view";
        public const string scrollerUssClassName = "animation-toolbar-scroll-view__scroller";

        public ToolbarScrollView() : base(ScrollViewMode.Horizontal)
        {
            AddToClassList(mainUssClassName);
            horizontalScrollerVisibility = ScrollerVisibility.Auto;
            verticalScrollerVisibility = ScrollerVisibility.Hidden;
            SetupScroller(horizontalScroller);
            contentViewport.RegisterCallback<GeometryChangedEvent>(UpdateButtonsOnGeometryChanged);
            contentContainer.RegisterCallback<GeometryChangedEvent>(UpdateButtonsOnGeometryChanged);
        }

        void SetupScroller(Scroller scroller)
        {
            scroller.AddToClassList(scrollerUssClassName);
            scroller.pickingMode = PickingMode.Ignore;
            scroller.valueChanged += (value) => UpdateButtons(scroller);
            UpdateButtons(scroller);
        }

        void UpdateButtonsOnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateButtons(horizontalScroller);
        }

        void UpdateButtons(Scroller scroller)
        {
            scroller.lowButton.style.display = scroller.value <= scroller.lowValue ? DisplayStyle.None : DisplayStyle.Flex;
            scroller.highButton.style.display = scroller.value >= scroller.highValue ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
