// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class FloatingOverlayContainer : OverlayContainer
    {
        public FloatingOverlayContainer()
        {
            CreateSection();

            this.StretchToParentSize();
        }

        public override bool IsOverlayLayoutSupported(Layout requested) => true;
    }
}
