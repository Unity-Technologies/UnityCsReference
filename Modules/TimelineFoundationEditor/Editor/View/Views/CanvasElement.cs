// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    abstract class CanvasElement : SequenceElement
    {
        protected CanvasElement()
        {
            usageHints = UsageHints.DynamicTransform;
        }

        public abstract void PositionInCanvas(CanvasTransform canvasTransform);
    }
}
