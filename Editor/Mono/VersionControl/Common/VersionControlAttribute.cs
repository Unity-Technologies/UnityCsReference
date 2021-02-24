// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.VersionControl
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VersionControlAttribute : Attribute
    {
        readonly string m_DisplayName;

        public string name { get; }
        public virtual string displayName => m_DisplayName;

        public VersionControlAttribute(string name, string displayName = null)
        {
            this.name = name;
            m_DisplayName = displayName ?? name;
        }
    }
}
