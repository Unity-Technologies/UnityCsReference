// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View.Internals
{
    class CanvasZoomManipulator : ZoomManipulator
    {
        readonly CanvasManager m_CanvasManager;

        public CanvasZoomManipulator(CanvasManager canvasManager)
        {
            m_CanvasManager = canvasManager;
        }

        protected override Rect GetWorldRect() => m_CanvasManager.worldBound;
    }

    class CanvasPanManipulator : PanManipulator
    {
        readonly CanvasManager m_CanvasManager;

        public CanvasPanManipulator(CanvasManager canvasManager)
        {
            m_CanvasManager = canvasManager;
        }

        protected override Rect GetWorldRect() => m_CanvasManager.worldBound;
    }
}
