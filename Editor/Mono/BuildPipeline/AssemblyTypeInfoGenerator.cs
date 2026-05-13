// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DOLOG
using System;
using System.Runtime.InteropServices;
using Mono.Cecil;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    using GenericInstanceTypeMap = System.Collections.Generic.Dictionary<TypeReference, TypeReference>;
    [NativeHeader("Modules/ContentBuild/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FieldInfoManaged
    {
        public string name;
        public string type;
        public int flags;
        public int fixedBufferLength;
        public string fixedBufferTypename;

        [RequiredByNativeCode]
        internal static void DeconstructFieldInfoManagedArrayElement(FieldInfoManaged[] array, int index,
            out string name, out string type, out int flags, out int fixedBufferLength, out string fixedBufferTypename)
        {
            var element = array[index];
            name = element.name;
            type = element.type;
            flags = element.flags;
            fixedBufferLength = element.fixedBufferLength;
            fixedBufferTypename = element.fixedBufferTypename;
        }
    }

    [NativeHeader("Modules/ContentBuild/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeInformationManaged
    {
        public string className;
        public FieldInfoManaged[] fieldInfos;

        // The proxy generator does not support `out T[]` directly, so use `out object`
        // and cast to ScriptingArrayPtr on the native side.
        [RequiredByNativeCode]
        internal static void DeconstructTypeInformationManagedArrayElement(TypeInformationManaged[] array, int index,
            out string className, out object fieldInfos)
        {
            var element = array[index];
            className = element.className;
            fieldInfos = element.fieldInfos;
        }
    }

    [NativeHeader("Modules/ContentBuild/Editor/Public/TypeDB.h")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfoManaged
    {
        public string name;
        public string path;
        public TypeInformationManaged[] types;

        // The proxy generator does not support `out T[]` directly, so use `out object`
        // and cast to ScriptingArrayPtr on the native side.
        [RequiredByNativeCode]
        internal static void DeconstructAssemblyInfoManagedArrayElement(AssemblyInfoManaged[] array, int index,
            out string name, out string path, out object types)
        {
            var element = array[index];
            name = element.name;
            path = element.path;
            types = element.types;
        }
    }

    [Serializable]
    internal class ExtractRoot<T>
    {
        public T[] root;
    }
}
