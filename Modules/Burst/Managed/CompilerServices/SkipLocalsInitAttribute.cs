// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Skip zero-initialization of local variables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipLocalsInitAttribute : Attribute
    {
    }
}
