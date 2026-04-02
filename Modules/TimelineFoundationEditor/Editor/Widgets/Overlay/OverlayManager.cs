// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class OverlayManager : VisualElement, IOverlayManager
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new OverlayManager();
        }

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
