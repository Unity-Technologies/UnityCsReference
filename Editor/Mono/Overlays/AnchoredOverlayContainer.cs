// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Overlays
{
    class AnchoredOverlayContainer : OverlayContainer
    {
        [Serializable]
        public new class UxmlSerializedData : OverlayContainer.UxmlSerializedData
        {
            public override object CreateInstance() => new AnchoredOverlayContainer();
        }

        public AnchoredOverlayContainer()
        {
            CreateDefaultSections(out var _, out var _);
        }
    }
}
