// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marks APIs visibility as restricted to Unity internal developers.
    ///
    /// This attribute can be applied to all attribute targets.
    ///
    /// Elements marked with [UnityRestricted] are accessible to Unity's internal developers but are not available in the
    /// public API for external users.
    /// </summary>
    /// <remarks>
    /// Note: This attribute is currently informative only and doesn't yet affect API access.
    /// The functionality to enforce this visibility level is under development.
    /// </remarks>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [UnityRestricted]
    internal class UnityRestrictedAttribute : Attribute
    {
    }
}
