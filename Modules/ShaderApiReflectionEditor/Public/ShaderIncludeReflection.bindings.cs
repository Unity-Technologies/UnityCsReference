// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

using Object = UnityEngine.Object;

namespace UnityEditor.ShaderApiReflection
{
    [NativeHeader("Modules/ShaderApiReflectionEditor/Public/ShaderIncludeReflection.h")]
    [NativeClass("ShaderApiReflection::ShaderIncludeReflection")]
    public sealed class ShaderIncludeReflection : Object
    {
        // Public API

        public ReadOnlyCollection<LogMessage> LogMessages => GetOrLoadLogMessages().AsReadOnly();
        public ReadOnlyCollection<ReflectedFunction> ReflectedFunctions => GetOrLoadFunctions().AsReadOnly();

        // Private API

        [NativeName("GetLogMessages")]
        private extern LogMessage.MarshalledType[] GetMessagesFromNative();

        private List<LogMessage> m_LogMessages;

        private List<LogMessage> GetOrLoadLogMessages()
        {
            if (m_LogMessages == null)
            {
                LogMessage.MarshalledType[] nativeMessages = GetMessagesFromNative();
                m_LogMessages = new List<LogMessage>(nativeMessages.Length);
                foreach (LogMessage.MarshalledType nativeMessage in nativeMessages)
                    m_LogMessages.Add(new LogMessage(nativeMessage));
            }
            return m_LogMessages;
        }

        [NativeName("GetFunctions")]
        private extern ReflectedFunction.MarshalledType[] GetFunctionsFromNative();

        private List<ReflectedFunction> m_Functions;

        private List<ReflectedFunction> GetOrLoadFunctions()
        {
            if (m_Functions == null)
            {
                ReflectedFunction.MarshalledType[] nativeFunctions = GetFunctionsFromNative();
                m_Functions = new List<ReflectedFunction>(nativeFunctions.Length);
                foreach (ReflectedFunction.MarshalledType nativeFunction in nativeFunctions)
                    m_Functions.Add(new ReflectedFunction(nativeFunction));
            }
            return m_Functions;
        }

        // This class is read-only and should not be constructed by users.
        internal ShaderIncludeReflection()
        {
        }
    }
}
