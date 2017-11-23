// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;
using System;


namespace UnityEditor
{
    [Serializable]
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/ShaderMenu.h")]
    public struct ShaderInfo
    {
        [SerializeField]
        [NativeName("name")]
        internal string m_Name;
        [SerializeField]
        [NativeName("supported")]
        internal bool m_Supported;
        [SerializeField]
        [NativeName("hasErrors")]
        internal bool m_HasErrors;

        public string name
        {
            get { return m_Name; }
        }
        public bool supported
        {
            get { return m_Supported; }
        }
        public bool hasErrors
        {
            get { return m_HasErrors; }
        }
    }

    [NativeHeader("Editor/Src/ShaderMenu.h")]
    public partial class ShaderUtil
    {
        [FreeFunction]
        public static extern ShaderInfo[] GetAllShaderInfo();
    }
}
