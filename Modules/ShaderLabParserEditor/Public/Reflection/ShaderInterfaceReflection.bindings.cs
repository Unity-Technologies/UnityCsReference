// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    [NativeHeader("Modules/ShaderLabParserEditor/Public/Reflection/ShaderInterfaceReflection.h")]
    [NativeClass("ShaderApiReflection::ShaderInterfaceReflection", PersistentTypeId = 0x675047D5)]
    internal class ShaderInterfaceReflection : UnityEngine.Object
    {
        // Public API

        public Shader BaseShader => GetBaseShader();
        public ReadOnlyCollection<VirtualFunctionDeclaration> OverridableFunctions
            => GetVirtualFunctionDeclarations().AsReadOnly();
        public ReadOnlyCollection<PartialStruct> PartialStructs => GetOrLoadPartialStructs().AsReadOnly();

        // Internal API

        internal ReadOnlyCollection<VirtualFunction> VirtualFunctions => GetOrLoadAllVirtualFunctions().AsReadOnly();

        // Private API

        private List<VirtualFunction> m_AllVirtualFunctions = null;
        private List<PartialStruct> m_PartialStructs = null;

        [NativeName("GetVirtualFunctions")]
        private extern VirtualFunction.MarshalledType[] GetVirtualFunctionsFromNative();
        [NativeName("GetPartialStructs")]
        private extern PartialStruct.MarshalledType[] GetPartialStructsFromNative();
        private extern Shader GetBaseShader();

        private List<VirtualFunction> GetOrLoadAllVirtualFunctions()
        {
            if (m_AllVirtualFunctions == null)
            {
                VirtualFunction.MarshalledType[] marshalledArray = GetVirtualFunctionsFromNative();
                m_AllVirtualFunctions = new(marshalledArray.Length);
                foreach (VirtualFunction.MarshalledType nativeData in marshalledArray)
                    m_AllVirtualFunctions.Add(new(nativeData));
            }
            return m_AllVirtualFunctions;
        }

        private List<PartialStruct> GetOrLoadPartialStructs()
        {
            if (m_PartialStructs == null)
            {
                PartialStruct.MarshalledType[] marshalledArray = GetPartialStructsFromNative();
                m_PartialStructs = new(marshalledArray.Length);
                foreach (PartialStruct.MarshalledType nativeData in marshalledArray)
                    m_PartialStructs.Add(new(nativeData));
            }
            return m_PartialStructs;
        }

        private List<VirtualFunctionDeclaration> GetVirtualFunctionDeclarations()
        {
            List<VirtualFunctionDeclaration> result = new();
            foreach (VirtualFunction func in VirtualFunctions)
                result.Add(func.Declaration);
            return result;
        }
    }
}
