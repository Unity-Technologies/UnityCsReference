// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// AlwaysSynchronizeSystem can be applied to a SystemBase to force it to synchronize on all of its
    /// dependencies before every update.  This attribute should only be applied when a synchronization point is
    /// necessary every frame.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AlwaysSynchronizeSystemAttribute : Attribute
    {
    }
}
