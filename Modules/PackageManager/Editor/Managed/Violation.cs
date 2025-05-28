// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class Violation
    {
        [SerializeField]
        [NativeName("scopePatternExpression")]
        private string m_ScopePatternExpression;

        [SerializeField]
        [NativeName("message")]
        private string m_Message;

        [SerializeField]
        [NativeName("readMoreLink")]
        private string m_ReadMoreLink;

        public string scopePatternExpression => m_ScopePatternExpression;
        public string message => m_Message;
        public string readMoreLink => m_ReadMoreLink;
        
        public Violation() : this("", "", "") {}

        public Violation(string scopePatternExpression = "", string message = "", string readMoreLink = "")
        {
            m_ScopePatternExpression = scopePatternExpression;
            m_Message = message;
            m_ReadMoreLink = readMoreLink;
        }
    }
}
