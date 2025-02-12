// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A SplitView that contains two resizable panes. One pane is fixed-size while the other pane has flex-grow style set to 1 to take all remaining space. The border between the panes is draggable to resize both panes. Both horizontal and vertical modes are supported. Requires exactly two child elements to operate.
    /// </summary>
    public class TwoPaneSplitView : VisualElement
    {
        static readonly string s_UssClassName = "unity-two-pane-split-view";
        static readonly string s_ContentContainerClassName = "unity-two-pane-split-view__content-container";
        static readonly string s_HandleDragLineClassName = "unity-two-pane-split-view__dragline";
        static readonly string s_HandleDragLineVerticalClassName = s_HandleDragLineClassName + "--vertical";
        static readonly string s_HandleDragLineHorizontalClassName = s_HandleDragLineClassName + "--horizontal";
        static readonly string s_HandleDragLineAnchorClassName = "unity-two-pane-split-view__dragline-anchor";
        static readonly string s_HandleDragLineAnchorVerticalClassName = s_HandleDragLineAnchorClassName + "--vertical";
        static readonly string s_HandleDragLineAnchorHorizontalClassName = s_HandleDragLineAnchorClassName + "--horizontal";
        static readonly string s_VerticalClassName = "unity-two-pane-split-view--vertical";
        static readonly string s_HorizontalClassName = "unity-two-pane-split-view--horizontal";

        /// <summary>
        /// Instantiates a <see cref="TwoPaneSplitView"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<TwoPaneSplitView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TwoPaneSplitView"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlIntAttributeDescription m_FixedPaneIndex = new UxmlIntAttributeDescription { name = "fixed-pane-index", defaultValue = 0 };
            UxmlIntAttributeDescription m_FixedPaneInitialDimension = new UxmlIntAttributeDescription { name = "fixed-pane-initial-dimension", defaultValue = 100 };
            UxmlEnumAttributeDescription<TwoPaneSplitViewOrientation> m_Orientation = new UxmlEnumAttributeDescription<TwoPaneSplitViewOrientation> { name = "orientation", defaultValue = TwoPaneSplitViewOrientation.Horizontal };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var fixedPaneIndex = m_FixedPaneIndex.GetValueFromBag(bag, cc);
                var fixedPaneInitialSize = m_FixedPaneInitialDimension.GetValueFromBag(bag, cc);
                var orientation = m_Orientation.GetValueFromBag(bag, cc);

                ((TwoPaneSplitView)ve).Init(fixedPaneIndex, fixedPaneInitialSize, orientation);
            }
        }

        VisualElement m_LeftPane;
        VisualElement m_RightPane;

        VisualElement m_FixedPane;
        VisualElement m_FlexedPane;

        [SerializeField] float m_FixedPaneDimension = -1;

        /// <summary>
        /// The child element that is set as the fixed size pane.
        /// </summary>
        public VisualElement fixedPane => m_FixedPane;
        /// <summary>
        /// The child element that is set as the flexable size pane.
        /// </summary>
        public VisualElement flexedPane => m_FlexedPane;

        VisualElement m_DragLine;
        VisualElement m_DragLineAnchor;

        /// <summary>
        /// The actual drag line, which is a child of the anchor.
        /// </summary>
        internal VisualElement dragLine => m_DragLine;

        bool m_CollapseMode;
        bool m_CollapseChildCalledBeforeSetupComplete;
        int m_CollapsedChildIndex = -1;

        VisualElement m_Content;

        TwoPaneSplitViewOrientation m_Orientation;
        int m_FixedPaneIndex;
        float m_FixedPaneInitialDimension;

        /// <summary>
        /// 0 for setting first child as the fixed pane, 1 for the second child element.
        /// </summary>
        public int fixedPaneIndex
        {
            get => m_FixedPaneIndex;
            set
            {
                if (value == m_FixedPaneIndex)
                    return;

                Init(value, m_FixedPaneInitialDimension, m_Orientation);
            }
        }

        /// <summary>
        /// The initial width or height for the fixed pane.
        /// </summary>
        public float fixedPaneInitialDimension
        {
            get => m_FixedPaneInitialDimension;
            set
            {
                if (value == m_FixedPaneInitialDimension)
                    return;

                Init(m_FixedPaneIndex, value, m_Orientation);
            }
        }

        /// <summary>
        /// Orientation of the split view.
        /// </summary>
        public TwoPaneSplitViewOrientation orientation
        {
            get => m_Orientation;
            set
            {
                if (value == m_Orientation)
                    return;

                Init(m_FixedPaneIndex, m_FixedPaneInitialDimension, value);
            }
        }

        internal float fixedPaneDimension
        {
            get => string.IsNullOrEmpty(viewDataKey)
            ? m_FixedPaneInitialDimension
            : m_FixedPaneDimension;

            set
            {
                if (value == m_FixedPaneDimension)
                    return;
                m_FixedPaneDimension = value;
                SaveViewData();
            }
        }

        internal TwoPaneSplitViewResizer m_Resizer;

        public TwoPaneSplitView()
        {
            AddToClassList(s_UssClassName);

            m_Content = new VisualElement();
            m_Content.name = "unity-content-container";
            m_Content.AddToClassList(s_ContentContainerClassName);
            hierarchy.Add(m_Content);

            // Create drag anchor line.
            m_DragLineAnchor = new VisualElement();
            m_DragLineAnchor.name = "unity-dragline-anchor";
            m_DragLineAnchor.AddToClassList(s_HandleDragLineAnchorClassName);
            hierarchy.Add(m_DragLineAnchor);

            // Create drag
            m_DragLine = new VisualElement();
            m_DragLine.name = "unity-dragline";
            m_DragLine.AddToClassList(s_HandleDragLineClassName);
            m_DragLineAnchor.Add(m_DragLine);
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="fixedPaneIndex">0 for setting first child as the fixed pane, 1 for the second child element.</param>
        /// <param name="fixedPaneStartDimension">Set an inital width or height for the fixed pane.</param>
        /// <param name="orientation">Orientation of the split view.</param>
        public TwoPaneSplitView(
            int fixedPaneIndex,
            float fixedPaneStartDimension,
            TwoPaneSplitViewOrientation orientation) : this()
        {
            Init(fixedPaneIndex, fixedPaneStartDimension, orientation);
        }

        /// <summary>
        /// Collapse one of the panes of the split view. This will hide the resizer and make the other child take up all available space.
        /// </summary>
        /// <param name="index">Index of child to collapse.</param>
        public void CollapseChild(int index)
        {
            if (index != 0 && index != 1)
                return;

            if (m_LeftPane == null)
            {
                m_CollapseChildCalledBeforeSetupComplete = true;
                m_CollapsedChildIndex = index;
                return;
            }

            m_DragLine.style.display = DisplayStyle.None;
            m_DragLineAnchor.style.display = DisplayStyle.None;
            if (index == 0)
            {
                m_RightPane.style.width = StyleKeyword.Initial;
                m_RightPane.style.height = StyleKeyword.Initial;
                m_RightPane.style.flexGrow = 1;
                m_LeftPane.style.display = DisplayStyle.None;
            }
            else
            {
                m_LeftPane.style.width = StyleKeyword.Initial;
                m_LeftPane.style.height = StyleKeyword.Initial;
                m_LeftPane.style.flexGrow = 1;
                m_RightPane.style.display = DisplayStyle.None;
            }

            m_CollapseMode = true;
        }

        /// <summary>
        /// Un-collapse the split view. This will restore the split view to the state it was before the previous collapse.
        /// </summary>
        public void UnCollapse()
        {
            if (m_LeftPane == null)
                return;

            VisualElement collapsedPane = null;

            if (m_LeftPane.style.display == DisplayStyle.None)
                collapsedPane = m_LeftPane;
            else if (m_RightPane.style.display == DisplayStyle.None)
                collapsedPane = m_RightPane;

            if (collapsedPane == null)
                return;

            m_LeftPane.style.display = DisplayStyle.Flex;
            m_RightPane.style.display = DisplayStyle.Flex;

            m_DragLine.style.display = DisplayStyle.Flex;
            m_DragLineAnchor.style.display = DisplayStyle.Flex;

            m_LeftPane.style.flexGrow = 0;
            m_RightPane.style.flexGrow = 0;
            m_CollapseMode = false;
            m_CollapseChildCalledBeforeSetupComplete = false;
            m_CollapsedChildIndex = -1;

            Init(m_FixedPaneIndex, m_FixedPaneInitialDimension, m_Orientation);

            // Update the position of the drag line anchor after one of the pane gets uncollapsed.
            // However, the computation of the position requires the resolved style of the panes to be computed.
            collapsedPane.RegisterCallback<GeometryChangedEvent>(OnUncollapsedPaneResized);
        }

        void OnUncollapsedPaneResized(GeometryChangedEvent evt)
        {
            UpdateDragLineAnchorOffset();
            (evt.target as VisualElement).UnregisterCallback<GeometryChangedEvent>(OnUncollapsedPaneResized);
        }

        internal void Init(int fixedPaneIndex, float fixedPaneInitialDimension, TwoPaneSplitViewOrientation orientation)
        {
            m_Orientation = orientation;
            m_FixedPaneIndex = fixedPaneIndex;
            m_FixedPaneInitialDimension = fixedPaneInitialDimension;

            m_Content.RemoveFromClassList(s_HorizontalClassName);
            m_Content.RemoveFromClassList(s_VerticalClassName);
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_Content.AddToClassList(s_HorizontalClassName);
            else
                m_Content.AddToClassList(s_VerticalClassName);

            // Create drag anchor line.
            m_DragLineAnchor.RemoveFromClassList(s_HandleDragLineAnchorHorizontalClassName);
            m_DragLineAnchor.RemoveFromClassList(s_HandleDragLineAnchorVerticalClassName);
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_DragLineAnchor.AddToClassList(s_HandleDragLineAnchorHorizontalClassName);
            else
                m_DragLineAnchor.AddToClassList(s_HandleDragLineAnchorVerticalClassName);

            // Create drag
            m_DragLine.RemoveFromClassList(s_HandleDragLineHorizontalClassName);
            m_DragLine.RemoveFromClassList(s_HandleDragLineVerticalClassName);
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_DragLine.AddToClassList(s_HandleDragLineHorizontalClassName);
            else
                m_DragLine.AddToClassList(s_HandleDragLineVerticalClassName);

            if (m_Resizer != null)
            {
                m_DragLineAnchor.RemoveManipulator(m_Resizer);
                m_Resizer = null;
            }

            if (m_Content.childCount != 2)
                RegisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
            else
                PostDisplaySetup();

            m_DragLineAnchor.RegisterCallback<GeometryChangedEvent>(OnAnchorPostDisplaySetup);
        }

        void OnPostDisplaySetup(GeometryChangedEvent evt)
        {
            if (m_Content.childCount != 2)
            {
                Debug.LogError("TwoPaneSplitView needs exactly 2 children.");
                return;
            }

            var postSetupWithEmptyLeftPane = m_LeftPane == null;
            PostDisplaySetup();

            // If CollapseChild was called before the setup was complete, we need to call it again.
            if (postSetupWithEmptyLeftPane && m_CollapseChildCalledBeforeSetupComplete)
            {
                CollapseChild(m_CollapsedChildIndex);
                m_CollapseChildCalledBeforeSetupComplete = false;
            }

            UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);

            // Initially, consider the size of the anchor in the placement of the second pane, no matter if fixed or not.
            ReplacePanesBasedOnAnchor();
        }

        void ReplacePanesBasedOnAnchor()
        {
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_RightPane.style.left = m_DragLineAnchor.worldBound.width;
            else
                m_RightPane.style.top = m_DragLineAnchor.worldBound.height;
        }

        void OnAnchorPostDisplaySetup(GeometryChangedEvent evt)
        {
            if (Mathf.Approximately(evt.newRect.width, evt.oldRect.width) && Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            IdentifyLeftAndRightPane();
            ReplacePanesBasedOnAnchor();
        }

        void IdentifyLeftAndRightPane()
        {
            m_LeftPane = m_Content[0];
            if (m_FixedPaneIndex == 0)
                m_FixedPane = m_LeftPane;
            else
                m_FlexedPane = m_LeftPane;

            m_RightPane = m_Content[1];
            if (m_FixedPaneIndex == 1)
                m_FixedPane = m_RightPane;
            else
                m_FlexedPane = m_RightPane;
        }

        void PostDisplaySetup()
        {
            if (m_Content.childCount != 2)
            {
                Debug.LogError("TwoPaneSplitView needs exactly 2 children.");
                return;
            }

            m_DragLineAnchor.UnregisterCallback<GeometryChangedEvent>(OnAnchorPostDisplaySetup);

            if (fixedPaneDimension < 0)
                fixedPaneDimension = m_FixedPaneInitialDimension;

            var dimension = fixedPaneDimension;

            IdentifyLeftAndRightPane();

            m_FixedPane.style.flexBasis = StyleKeyword.Null;
            m_FixedPane.style.flexShrink = StyleKeyword.Null;
            m_FixedPane.style.flexGrow = StyleKeyword.Null;
            m_FlexedPane.style.flexGrow = StyleKeyword.Null;
            m_FlexedPane.style.flexShrink = StyleKeyword.Null;
            m_FlexedPane.style.flexBasis = StyleKeyword.Null;

            m_FixedPane.style.width = StyleKeyword.Null;
            m_FixedPane.style.height = StyleKeyword.Null;
            m_FlexedPane.style.width = StyleKeyword.Null;
            m_FlexedPane.style.height = StyleKeyword.Null;

            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
            {
                m_FixedPane.style.width = dimension;
                m_FixedPane.style.height = StyleKeyword.Null;
            }
            else
            {
                m_FixedPane.style.width = StyleKeyword.Null;
                m_FixedPane.style.height = dimension;
            }

            m_FixedPane.style.flexShrink = 0;
            m_FixedPane.style.flexGrow = 0;
            m_FlexedPane.style.flexGrow = 1;
            m_FlexedPane.style.flexShrink = 0;
            m_FlexedPane.style.flexBasis = 0;

            m_DragLineAnchor.style.left = 0;
            m_DragLineAnchor.style.top = 0;

            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
            {
                var fixedPaneMargins = m_FixedPane.resolvedStyle.marginLeft + m_FixedPane.resolvedStyle.marginRight;
                if (m_FixedPaneIndex == 0)
                    m_DragLineAnchor.style.left = fixedPaneMargins + dimension;
                else
                    m_DragLineAnchor.style.left = resolvedStyle.width - fixedPaneMargins - dimension - m_DragLineAnchor.resolvedStyle.width;
            }
            else
            {
                var fixedPaneMargins = m_FixedPane.resolvedStyle.marginTop + m_FixedPane.resolvedStyle.marginBottom;
                if (m_FixedPaneIndex == 0)
                    m_DragLineAnchor.style.top = fixedPaneMargins + dimension;
                else
                    m_DragLineAnchor.style.top = resolvedStyle.height - fixedPaneMargins - dimension - m_DragLineAnchor.resolvedStyle.height;
            }

            int direction = 1;
            if (m_FixedPaneIndex == 0)
                direction = 1;
            else
                direction = -1;

            if (m_Resizer != null)
                m_DragLineAnchor.RemoveManipulator(m_Resizer);
            m_Resizer = new TwoPaneSplitViewResizer(this, direction);

            m_DragLineAnchor.AddManipulator(m_Resizer);

            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
            m_DragLineAnchor.RegisterCallback<GeometryChangedEvent>(OnAnchorPostDisplaySetup);
        }

        void OnSizeChange(GeometryChangedEvent evt)
        {
            UpdateLayout(true, true);
        }

        void UpdateDragLineAnchorOffset()
        {
            UpdateLayout(false, true);
        }

        void UpdateLayout(bool updateFixedPane, bool updateDragLine)
        {
            if (m_CollapseMode)
                return;

            // Don't try to update the layout if the split view is not displayed. This is because the resolved width and height
            // will be 0, which will effectively reset the layout.
            if (resolvedStyle.display == DisplayStyle.None ||
                resolvedStyle.visibility == Visibility.Hidden)
                return;

            var maxLength = resolvedStyle.width;
            var fixedPaneLength = m_FixedPane.resolvedStyle.width;
            var fixedPaneMargins = m_FixedPane.resolvedStyle.marginLeft + m_FixedPane.resolvedStyle.marginRight;
            var fixedPaneMinLength = m_FixedPane.resolvedStyle.minWidth.value;
            var flexedPaneMargins = m_FlexedPane.resolvedStyle.marginLeft + m_FlexedPane.resolvedStyle.marginRight;
            var flexedPaneMinLength = m_FlexedPane.resolvedStyle.minWidth.value;

            if (m_Orientation == TwoPaneSplitViewOrientation.Vertical)
            {
                maxLength = resolvedStyle.height;
                fixedPaneLength = m_FixedPane.resolvedStyle.height;
                fixedPaneMargins = m_FixedPane.resolvedStyle.marginTop + m_FixedPane.resolvedStyle.marginBottom;
                fixedPaneMinLength = m_FixedPane.resolvedStyle.minHeight.value;
                flexedPaneMargins = m_FlexedPane.resolvedStyle.marginTop + m_FlexedPane.resolvedStyle.marginBottom;
                flexedPaneMinLength = m_FlexedPane.resolvedStyle.minHeight.value;
            }

            // Big enough to account for current fixed pane size and flexed pane minimum size, so we let the layout
            // dictates where the dragger should be.
            if (maxLength >= fixedPaneLength + fixedPaneMargins + flexedPaneMinLength + flexedPaneMargins)
            {
                if (updateDragLine)
                    SetDragLineOffset(m_FixedPaneIndex == 0 ? fixedPaneLength + fixedPaneMargins : maxLength - fixedPaneLength - fixedPaneMargins);
            }
            // Big enough to account for fixed and flexed pane minimum sizes, so we resize the fixed pane and adjust
            // where the dragger should be.
            else if (maxLength >= fixedPaneMinLength + fixedPaneMargins + flexedPaneMinLength + flexedPaneMargins)
            {
                var newDimension = maxLength - flexedPaneMinLength - flexedPaneMargins - fixedPaneMargins;
                var dimensionToAnchorOffset = 0f;
                dimensionToAnchorOffset = (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                ? Math.Abs(m_DragLineAnchor.worldBound.width - (m_DragLine.resolvedStyle.width - Math.Abs(m_DragLine.resolvedStyle.left)))
                : Math.Abs(m_DragLineAnchor.worldBound.height - (m_DragLine.resolvedStyle.height - Math.Abs(m_DragLine.resolvedStyle.top)));
                newDimension -= dimensionToAnchorOffset;

                if (updateFixedPane)
                    SetFixedPaneDimension(newDimension);

                if (updateDragLine)
                    SetDragLineOffset(m_FixedPaneIndex == 0 ? newDimension + fixedPaneMargins + dimensionToAnchorOffset: flexedPaneMinLength + flexedPaneMargins);
            }
            // Not big enough for fixed and flexed pane minimum sizes
            else
            {
                if (updateFixedPane)
                    SetFixedPaneDimension(fixedPaneMinLength);

                if (updateDragLine)
                    SetDragLineOffset(m_FixedPaneIndex == 0 ? fixedPaneMinLength + fixedPaneMargins : flexedPaneMinLength + flexedPaneMargins);
            }
        }

        public override VisualElement contentContainer
        {
            get { return m_Content; }
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            var key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            PostDisplaySetup();
        }

        void SetDragLineOffset(float offset)
        {
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_DragLineAnchor.style.left = offset;
            else
                m_DragLineAnchor.style.top = offset;
        }

        void SetFixedPaneDimension(float dimension)
        {
            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                m_FixedPane.style.width = dimension;
            else
                m_FixedPane.style.height = dimension;
        }
    }

    /// <summary>
    /// Determines the orientation of the two resizable panes.
    /// </summary>
    public enum TwoPaneSplitViewOrientation
    {
        /// <summary>
        /// Split view panes layout is left/right with vertical resizable split.
        /// </summary>
        Horizontal,
        /// <summary>
        /// Split view panes layout is top/bottom with horizontal resizable split.
        /// </summary>
        Vertical
    }
}
