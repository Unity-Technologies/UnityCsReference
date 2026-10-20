// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// Obsolete. Systems now update by default. 
    /// </summary>
    /// <remarks>
    /// **Obsolete. Systems now update by default. Use <see cref="RequireMatchingQueriesForUpdateAttribute"/> if you want a
    /// system to only update if one of its queries is non-empty.** 
    ///
    /// This attribute causes a system to update even if none of its registered queries matched any entities.
    /// </remarks>
    [Obsolete("AlwaysUpdateSystem is deprecated. Systems now always update by default. Use RequireMatchingQueriesForUpdate if you want a System to only update if one of its queries matches. (RemovedAfter Entities 1.0)")]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class AlwaysUpdateSystemAttribute : Attribute
    {
    }
}
