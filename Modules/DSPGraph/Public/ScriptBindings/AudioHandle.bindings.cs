// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/DSPGraphHandles.h")]
    internal struct Handle : IHandle<Handle>
    {
        public IntPtr Ptr;
        public int Version;

        public bool Equals(Handle other)
        {
            return Ptr.Equals(other.Ptr) && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Handle && Equals((Handle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Ptr.GetHashCode() * 397) ^ Version;
            }
        }

        public bool Valid => Ptr != IntPtr.Zero;
    }
}

