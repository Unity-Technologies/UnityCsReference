// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine
{
    [Conditional("UNITY_EDITOR"), AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class IconAttribute : Attribute
    {
        string m_IconPath;

        public string path => m_IconPath;

        IconAttribute() {}

        public IconAttribute(string path)
        {
            m_IconPath = path;
        }
    }
}
