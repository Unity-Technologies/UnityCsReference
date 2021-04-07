// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DOLOG
using System;
using System.Runtime.InteropServices;
using Mono.Cecil;
using UnityEngine.Bindings;

namespace UnityEditor
{
    using GenericInstanceTypeMap = System.Collections.Generic.Dictionary<TypeReference, TypeReference>;
    [NativeHeader("Modules/BuildPipeline/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FieldInfoManaged
    {
        public string name;
        public string type;
        public int flags;
        public int fixedBufferLength;
        public string fixedBufferTypename;
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeInformationManaged
    {
        public string className;
        public FieldInfoManaged[] fieldInfos;
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfoManaged
    {
        public string name;
        public string path;
        public TypeInformationManaged[] types;
    }

    [Serializable]
    internal class ExtractRoot<T>
    {
        public T[] root;
    }
}
