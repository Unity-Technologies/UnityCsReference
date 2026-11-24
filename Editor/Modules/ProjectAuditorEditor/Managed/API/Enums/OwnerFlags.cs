// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Options for whose code and assets Project Auditor should analyze.
    /// </summary>
    /// <remarks>
    /// Setting the owner mode can affect how long analysis takes, which assemblies are analyzed and which package assets are scanned.
    /// </remarks>
    [Flags]
    internal enum OwnerFlags
    {
        None = 0,
        /// <summary>
        ///   <para>User</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include user code and assets.
        /// </remarks>
        User = 1 << 1,
        /// <summary>
        ///   <para>Unity</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include Unity code, and assets in Unity packages and built-in resources.
        /// </remarks>
        Unity = 1 << 2,
        /// <summary>
        ///   <para>All</para>
        /// </summary>
        /// <remarks>
        /// Project Auditor analyzes everyone's code and assets.
        /// </remarks>
        All = ~None
    }
}
