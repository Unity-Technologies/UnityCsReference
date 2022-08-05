// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Lumin
{
    [System.Obsolete("Lumin is no longer supported in Unity 2022.2")]
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public sealed class UsesLuminPlatformLevelAttribute : System.Attribute
    {
        // See the attribute guidelines at
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        private readonly uint m_PlatformLevel;

        // This is a positional argument
        public UsesLuminPlatformLevelAttribute(uint platformLevel)
        {
            this.m_PlatformLevel = platformLevel;
        }

        public uint platformLevel
        {
            get { return m_PlatformLevel; }
        }
    }
}
