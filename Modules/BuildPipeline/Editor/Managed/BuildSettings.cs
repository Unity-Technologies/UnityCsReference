// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Build.Player;

namespace UnityEditor.Build.Content
{
    [Flags]
    public enum ContentBuildFlags
    {
        None = 0,
        DisableWriteTypeTree = 1 << 0
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildSettings
    {
        [NativeName("typeDB")]
        internal TypeDB m_TypeDB;
        public TypeDB typeDB
        {
            get { return m_TypeDB; }
            set { m_TypeDB = value; }
        }

        [NativeName("target")]
        internal BuildTarget m_Target;
        public BuildTarget target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        [NativeName("group")]
        internal BuildTargetGroup m_Group;
        public BuildTargetGroup group
        {
            get { return m_Group; }
            set { m_Group = value; }
        }

        [NativeName("buildFlags")]
        internal ContentBuildFlags m_BuildFlags;
        public ContentBuildFlags buildFlags
        {
            get { return m_BuildFlags; }
            set { m_BuildFlags = value; }
        }
    }
}
