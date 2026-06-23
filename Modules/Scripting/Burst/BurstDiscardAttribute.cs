// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class BurstDiscardAttribute : Attribute
    {
        // Attribute used to discard entirely a method/property from being compiled by the burst compiler.
    }
}
