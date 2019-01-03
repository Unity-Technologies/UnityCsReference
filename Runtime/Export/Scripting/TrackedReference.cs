// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    //This class should be internal. Next time we can break backwardscompatibility we should do it.
    public class TrackedReference
    {
        internal IntPtr m_Ptr;

        protected TrackedReference() {}

        public static bool operator==(TrackedReference x, TrackedReference y)
        {
            object xo = x;
            object yo = y;

            if (yo == null && xo == null) return true;
            if (yo == null) return x.m_Ptr == IntPtr.Zero;
            if (xo == null) return y.m_Ptr == IntPtr.Zero;
            return x.m_Ptr == y.m_Ptr;
        }

        public static bool operator!=(TrackedReference x, TrackedReference y) { return !(x == y); }

        public override bool Equals(object o) { return (o as TrackedReference) == this; }
        public override int GetHashCode() { return (int)m_Ptr; }

        public static implicit operator bool(TrackedReference exists)
        {
            return exists != null;
        }
    }
}
