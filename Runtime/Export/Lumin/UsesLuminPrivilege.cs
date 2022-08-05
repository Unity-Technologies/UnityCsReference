// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Lumin
{
    [System.Obsolete("Lumin is no longer supported in Unity 2022.2")]
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed class UsesLuminPrivilegeAttribute : System.Attribute
    {
        private readonly string m_Privilege;

        // This is a positional argument
        public UsesLuminPrivilegeAttribute(string privilege)
        {
            this.m_Privilege = privilege;
        }

        public string privilege
        {
            get { return m_Privilege; }
        }
    }
}
