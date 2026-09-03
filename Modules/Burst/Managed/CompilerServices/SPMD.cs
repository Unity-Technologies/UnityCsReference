// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.CompilerServices.Spmd
{
    /// <summary>
    /// Specifies that multiple calls to a method act as if they are
    /// executing in a Single Program, Multiple Data (SPMD) paradigm.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SpmdAttribute : Attribute
    {
    }
}
