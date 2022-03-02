// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal  struct SafeHandleAccess
    {
        private  IntPtr m_Handle;

        public SafeHandleAccess(IntPtr ptr)
        {
            m_Handle = ptr;
        }

        public static implicit operator IntPtr(SafeHandleAccess a)
        {
            if (a.m_Handle == IntPtr.Zero)
                throw new ArgumentNullException();
            return a.m_Handle;
        }
    }
}
