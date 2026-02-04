// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

using Object = UnityEngine.Object;

namespace UnityEditor.ShaderApiReflection
{
    [NativeHeader("Modules/ShaderApiReflectionEditor/Public/ShaderIncludeReflection.h")]
    [NativeClass("ShaderApiReflection::ShaderIncludeReflection")]
    internal sealed class ShaderIncludeReflection : Object
    {
        // Public API

        public List<ReflectedFunction> ReflectedFunctions => GetOrLoadFunctions();

        // Private API

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
    }
}
