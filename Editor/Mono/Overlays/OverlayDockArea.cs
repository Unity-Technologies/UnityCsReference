// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class OverlayDockArea : VisualElement
    {
        // Used in tests.
        internal const string topToolbarDropZone = "DropZone-TopToolbar";
        internal const string bottomToolbarDropZone = "DropZone-BottomToolbar";
        internal const string leftToolbarDropZone = "DropZone-LeftToolbar";
        internal const string rightToolbarDropZone = "DropZone-RightToolbar";

        // Used in tests.
        internal const string topLeftColumnDropZone = "DropZone-TopLeftColumn";
        internal const string bottomLeftColumnDropZone = "DropZone-BottomLeftColumn";
        internal const string topRightColumnDropZone = "DropZone-TopRightColumn";
        internal const string bottomRightColumnDropZone = "DropZone-BottomRightColumn";

        readonly ToolbarDropZone m_TopToolbar;
        readonly ToolbarDropZone m_BottomToolbar;
        readonly ToolbarDropZone m_LeftToolbar;
        readonly ToolbarDropZone m_RightToolbar;
        readonly OverlayContainerDropZone m_TopLeftColumn;
        readonly OverlayContainerDropZone m_BottomLeftColumn;
        readonly OverlayContainerDropZone m_TopRightColumn;
        readonly OverlayContainerDropZone m_BottomRightColumn;

        public OverlayDockArea(OverlayCanvas canvas)
        {
            this.StretchToParentSize();
            style.position = Position.Absolute;
            pickingMode = PickingMode.Ignore;

            Add(m_TopToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.TopToolbar)) { name = topToolbarDropZone });
            Add(m_BottomToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.BottomToolbar)) { name = bottomToolbarDropZone });
            Add(m_LeftToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.LeftToolbar)) { name = leftToolbarDropZone });
            Add(m_RightToolbar = new ToolbarDropZone(canvas.GetDockZoneContainer(DockZone.RightToolbar)) { name = rightToolbarDropZone });
            Add(m_TopLeftColumn = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_LeftToolbar) { name = topLeftColumnDropZone });
            Add(m_BottomLeftColumn = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.LeftColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_LeftToolbar) { name = bottomLeftColumnDropZone });
            Add(m_TopRightColumn = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.Start, m_TopToolbar, m_RightToolbar) { name = topRightColumnDropZone });
            Add(m_BottomRightColumn = new OverlayContainerDropZone(canvas.GetDockZoneContainer(DockZone.RightColumn), OverlayContainerDropZone.Placement.End, m_BottomToolbar, m_RightToolbar) { name = bottomRightColumnDropZone });
        }

        public IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            yield return m_TopToolbar;
            yield return m_BottomToolbar;
            yield return m_LeftToolbar;
            yield return m_RightToolbar;
            yield return m_TopLeftColumn;
            yield return m_BottomLeftColumn;
            yield return m_TopRightColumn;
            yield return m_BottomRightColumn;
        }
    }
}
