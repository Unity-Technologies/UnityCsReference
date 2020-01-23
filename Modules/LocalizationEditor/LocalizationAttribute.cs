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
    /// An attribute to the assembly for Localization.
    /// </summary>
    [RequiredByNativeCode]
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
