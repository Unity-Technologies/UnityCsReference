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
    ///<summary>The container for reflection data extracted from a shader include.</summary>
    ///<remarks>This class exposes information on any functions or structs declared in a shader include which were exposed using <c>UNITY_EXPORT_REFLECTION</c>.</remarks>
    ///<example nocheck="true">
    /// In the following example, information from <see cref="ReflectedStruct"/>s and <see cref="ReflectedFunction"/>s
    /// is printed to the console.
    ///<code source="../Tests/UTFTests/FunctionalTests/Editor/Documentation/ReflectedFunctionExample.cs"/>
    ///</example>
    ///<seealso cref="ShaderInclude.Reflection" />
    [NativeHeader("Modules/ShaderApiReflectionEditor/Public/ShaderIncludeReflection.h")]
    [NativeClass("ShaderApiReflection::ShaderIncludeReflection", PersistentTypeId = 0x0C78A93D)]
    public sealed class ShaderIncludeReflection : Object
    {
        internal ShaderIncludeReflection(global::UnityEngine.EntityId id) : base(id) {}
        // Public API

        ///<summary>The errors or warnings generated while reflecting the shader include.</summary>
        public ReadOnlyCollection<LogMessage> LogMessages => GetOrLoadLogMessages().AsReadOnly();
        ///<summary>The shader include's exposed functions.</summary>
        public ReadOnlyCollection<ReflectedFunction> ReflectedFunctions => GetOrLoadFunctions().AsReadOnly();
        /// <summary>The shader include's exposed structs.</summary>
        public ReadOnlyCollection<ReflectedStruct> ReflectedStructs => GetOrLoadStructs().AsReadOnly();

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

        [NativeName("GetStructs")]
        extern ReflectedStruct.MarshalledType[] GetStructsFromNative();

        List<ReflectedStruct> m_Structs;

        List<ReflectedStruct> GetOrLoadStructs()
        {
            if (m_Structs == null)
            {
                ReflectedStruct.MarshalledType[] nativeStructs = GetStructsFromNative();
                m_Structs = new List<ReflectedStruct>(nativeStructs.Length);
                foreach (ReflectedStruct.MarshalledType nativeStruct in nativeStructs)
                    m_Structs.Add(new ReflectedStruct(nativeStruct));
            }
            return m_Structs;
        }

        // This class is read-only and should not be constructed by users.
        ///<exclude />
        internal ShaderIncludeReflection()
        {
        }
    }
}
