// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class DraggablePlayHeadOverlay : PlayHeadOverlay
    {
        public DraggablePlayHeadOverlay(ICanvas canvas)
            : base(PickingMode.Position)
        {
            usageHints = UsageHints.DynamicTransform;

            var timeDragManipulator = new TimeDragManipulator(canvas);
            timeDragManipulator.SetTime += SetTime;
            m_Handle.AddManipulator(timeDragManipulator);
        }

        protected virtual void SetTime(DiscreteTime time)
        {
            this.time = time;
        }
    }
}
