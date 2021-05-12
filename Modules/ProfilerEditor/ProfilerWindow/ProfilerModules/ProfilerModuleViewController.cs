// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    // TODO This type will be made public as part of the Extensibility API.
    internal abstract class ProfilerModuleViewController : IDisposable
    {
        VisualElement m_View;

        protected ProfilerModuleViewController(ProfilerWindow profilerWindow)
        {
            ProfilerWindow = profilerWindow;
        }

        public bool Disposed { get; private set; }

        internal VisualElement View
        {
            get
            {
                if (m_View == null)
                    m_View = CreateView();

                return m_View;
            }
        }

        protected ProfilerWindow ProfilerWindow { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract VisualElement CreateView();

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
                m_View?.RemoveFromHierarchy();

            Disposed = true;
        }
    }
}
