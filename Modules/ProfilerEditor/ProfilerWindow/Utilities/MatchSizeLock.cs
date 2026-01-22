// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class MatchWidthLock
    {
        VisualElement m_SizeReceiver;

        public MatchWidthLock(VisualElement sizeReceiver)
        {
            m_SizeReceiver = sizeReceiver;
        }

        public void SetSource(VisualElement source)
        {
            source.RegisterCallback<GeometryChangedEvent>(SizeSourceCallback);
        }

        void SizeSourceCallback(GeometryChangedEvent evt)
        {
            m_SizeReceiver.style.width = evt.newRect.width;
        }
    }
}
