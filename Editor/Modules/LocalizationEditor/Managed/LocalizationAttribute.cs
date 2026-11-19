// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    /// Marks an assembly as carrying its own editor translations.
    /// </summary>
    /// <remarks>
    /// Put the assembly's `.po` files in a `Localization` folder inside the same assembly definition.
    /// Unity registers them under the group named by this attribute, and looks them up under the same
    /// name when the assembly calls `L10n`.
    /// </remarks>
    /// <seealso cref="L10n"/>
    /// <seealso cref="LocalizationGroup"/>
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class LocalizationAttribute : Attribute
    {
        string m_LocGroupName;

        internal string locGroupName { get { return m_LocGroupName; } }

        /// <summary>
        /// Marks the assembly as carrying its own editor translations.
        /// </summary>
        /// <remarks>
        /// Give a group name to decouple the group from the assembly name, so that renaming the
        /// assembly does not move its translations, or so that several assemblies share one group.
        /// </remarks>
        /// <param name="locGroupName">Group to register and look the assembly's translations up under.
        /// Leave it out to use the assembly's own name.</param>
        public LocalizationAttribute(string locGroupName = null)
        {
            m_LocGroupName = locGroupName;
        }
    }
}

namespace UnityEditor.Localization.Editor
{
    /// <summary>
    /// An attribute to the assembly for Localization.
    /// </summary>
    [System.Obsolete("Please use UnityEditor.LocalizationAttribute instead. (UnityUpgradable) -> UnityEditor.LocalizationAttribute", true)]
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class LocalizationAttribute : Attribute
    {
        string m_LocGroupName;

        internal string locGroupName { get { return m_LocGroupName; } }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LocalizationAttribute(string locGroupName = null)
        {
            m_LocGroupName = locGroupName;
        }
    }
}
