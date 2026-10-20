// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// By default if no baking version are declared in an assembly, the scene will be re-imported if the assembly has changed.
    /// If you only want to re-trigger scene import if a baker, a baking system or an optimization system has changed and not something else in the assembly, use this attribute and bump its version or username everytime you want to re-trigger the scene import.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BakingVersionAttribute : Attribute
    {
        internal string UserName;
        internal int Version;
        internal bool Excluded;

        /// <summary>
        /// Initializes and returns an instance of BakingVersionAttribute
        /// </summary>
        /// <param name="userName">An identifier for the user that made the last change. Use this to enforce a merge conflict when two different users both try to bump the version at the same time.</param>
        /// <param name="version">The version number of the converter. Increase this to invalidate the cached versions of entity scenes that use the converter that is tagged with this attribute.</param>
        public BakingVersionAttribute(string userName, int version)
        {
            UserName = userName;
            Version = version;
            Excluded = false;
        }

        /// <summary>
        /// Initializes and returns an instance of BakingVersionAttribute that is excluded from changing the Baking behaviour for an assembly.
        /// </summary>
        /// <param name="excluded">Whether or not the Baker or Baking System is excluded from impacting the behaviour of the assembly. This means it does not contribute to an assembly causing re-bakes when changed, but also will not emit warnings if the assembly does use BakingVersion on other Bakers. With this constructor, only true is valid.</param>
        public BakingVersionAttribute(bool excluded)
        {
            if (!excluded)
                throw new ArgumentException("Name and Version must be set if not marked Excluded.");

            Excluded = excluded;
            UserName = string.Empty;
            Version = 0;
        }
    }
}
