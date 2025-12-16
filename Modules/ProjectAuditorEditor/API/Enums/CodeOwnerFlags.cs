// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Options for whose code Project Auditor should analyze.
    /// </summary>
    /// <remarks>
    /// Setting the code owner mode can affect how long analysis takes and which assemblies are analysed.
    /// </remarks>
    [Flags]
    internal enum CodeOwnerFlags
    {
        None = 0,
        /// <summary>
        ///   <para>User</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include user code.
        /// </remarks>
        User = 1 << 1,
        /// <summary>
        ///   <para>Unity</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include Unity code.
        /// </remarks>
        Unity = 1 << 2,
        /// <summary>
        ///   <para>All</para>
        /// </summary>
        /// <remarks>
        /// Project Auditor analyzes everyone's code.
        /// </remarks>
        All = ~None
    }
}
