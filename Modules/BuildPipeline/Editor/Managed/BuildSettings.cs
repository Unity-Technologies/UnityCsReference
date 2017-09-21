// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/AssetBundleBuildInterface.h")]
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
    }
}
