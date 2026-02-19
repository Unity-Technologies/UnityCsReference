// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    class OverlayManager : VisualElement
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
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
