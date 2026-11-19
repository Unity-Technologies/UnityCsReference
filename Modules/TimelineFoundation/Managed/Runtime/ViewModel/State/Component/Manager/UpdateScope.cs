// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Timeline.Foundation.ViewModel
{
    readonly struct UpdateScope : IDisposable
    {
        readonly Component m_Component;

        public UpdateScope(Component component)
        {
            m_Component = component;
        }

        public void Dispose()
        {
            m_Component?.MarkAsDirty();
        }
    }
}
