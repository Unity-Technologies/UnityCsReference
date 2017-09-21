// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FilterResults
    {
#pragma warning disable 414
        internal IntPtr m_CullResults;
#pragma warning restore 414
    }
}
