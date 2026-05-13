// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    [UxmlElement]
    internal partial class OverlayManager : VisualElement, IOverlayManager
    {
        public OverlayManager()
        {
            pickingMode = PickingMode.Ignore;
            style.overflow = Overflow.Hidden;
        }

        public void AddOverlay(Overlay overlay)
        {
            Add(overlay);
        }

        public void RemoveOverlay(Overlay overlay)
        {
            Remove(overlay);
        }
    }
}
