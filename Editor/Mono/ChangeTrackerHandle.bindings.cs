// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/ChangeTracker.h")]
    [RequiredByNativeCode]
    internal struct ChangeTrackerHandle
    {
        IntPtr m_Handle;

        internal static ChangeTrackerHandle AcquireTracker(UnityEngine.Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("Not a valid unity engine object");
            return new ChangeTrackerHandle() { m_Handle = Internal_AcquireTracker(obj) };
        }

        [FreeFunction("ChangeTrackerRegistry::AcquireTracker")]
        private static extern IntPtr Internal_AcquireTracker(UnityEngine.Object o);

        internal void ReleaseTracker()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");

            Internal_ReleaseTracker(m_Handle);
            m_Handle = IntPtr.Zero;
        }

        [FreeFunction("ChangeTrackerRegistry::ReleaseTracker")]
        private static extern void Internal_ReleaseTracker(IntPtr handle);

        // returns true if object changed since last poll
        internal bool PollForChanges()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");
            return Internal_PollChanges(m_Handle);
        }

        [FreeFunction("ChangeTrackerRegistry::PollChanges")]
        private static extern bool Internal_PollChanges(IntPtr handle);

        internal void ForceDirtyNextPoll()
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");
            Internal_ForceUpdate(m_Handle);
        }

        [FreeFunction("ChangeTrackerRegistry::ForceUpdate")]
        private static extern void Internal_ForceUpdate(IntPtr handle);
    }
}
