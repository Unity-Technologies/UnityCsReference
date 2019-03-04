// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPGraphHandles.h")]
    internal unsafe struct Handle : IEquatable<Handle>
    {
        public System.IntPtr Ptr;
        public System.Int32  Version;

        public bool Equals(Handle other)
        {
            return Ptr == other.Ptr &&
                Version == other.Version;
        }
    }
}

