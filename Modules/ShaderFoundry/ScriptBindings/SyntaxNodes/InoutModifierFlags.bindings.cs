// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN InoutModifierFlags.h
    [Flags]
    internal enum InoutModifierFlags : byte
    {
        None = 0,
        In = 1 << 0,
        Out = 1 << 1,
        InOut = In | Out,
    }
    static class InoutModifierFlagsUtils
    {
        internal static void Set(ref InoutModifierFlags flags, InoutModifierFlags flag, bool state)
        {
            if (state)
                flags |= flag;
            else
                flags &= ~flag;
        }
    }
}
