// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    [RequiredByNativeCode]
    public class FormerlySerializedAsAttribute : Attribute
    {
        private string m_oldName;
        public FormerlySerializedAsAttribute(string oldName)
        {
            m_oldName = oldName;
        }

        public string oldName { get { return m_oldName; } }
    }
}
