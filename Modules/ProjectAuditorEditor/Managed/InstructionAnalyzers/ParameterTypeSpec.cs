// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    // Lightweight representation of a method-parameter type tree. Decoupled from Mono.Cecil so the
    // ObsoleteDatabase lookup-key formatter can be unit-tested without taking a Cecil dependency
    // in the test asmdef (which does not have access to Mono.Cecil at editor-compile time).
    internal sealed class ParameterTypeSpec
    {
        public enum Kind
        {
            Simple,
            Array,
            Pointer,
            ByReference,
            GenericInstance
        }

        public Kind TypeKind { get; private set; }
        public string FullName { get; private set; }
        public string Name { get; private set; }
        public ParameterTypeSpec Element { get; private set; }
        public IReadOnlyList<ParameterTypeSpec> GenericArguments { get; private set; }

        public static ParameterTypeSpec Simple(string fullName, string name)
        {
            return new ParameterTypeSpec { TypeKind = Kind.Simple, FullName = fullName, Name = name };
        }

        public static ParameterTypeSpec ArrayOf(ParameterTypeSpec element)
        {
            return new ParameterTypeSpec { TypeKind = Kind.Array, Element = element };
        }

        public static ParameterTypeSpec PointerTo(ParameterTypeSpec element)
        {
            return new ParameterTypeSpec { TypeKind = Kind.Pointer, Element = element };
        }

        public static ParameterTypeSpec ByReferenceTo(ParameterTypeSpec element)
        {
            return new ParameterTypeSpec { TypeKind = Kind.ByReference, Element = element };
        }

        public static ParameterTypeSpec GenericInstanceOf(string openFullName, string openName, IReadOnlyList<ParameterTypeSpec> genericArguments)
        {
            return new ParameterTypeSpec
            {
                TypeKind = Kind.GenericInstance,
                FullName = openFullName,
                Name = openName,
                GenericArguments = genericArguments
            };
        }

        public static ParameterTypeSpec FromCecil(TypeReference type)
        {
            if (type.IsArray)
                return ArrayOf(FromCecil(((ArrayType)type).ElementType));

            if (type.IsPointer)
                return PointerTo(FromCecil(((Mono.Cecil.PointerType)type).ElementType));

            if (type.IsByReference)
                return ByReferenceTo(FromCecil(((ByReferenceType)type).ElementType));

            if (type.IsGenericInstance)
            {
                var genericInstance = (GenericInstanceType)type;
                var cecilArgs = genericInstance.GenericArguments;
                var args = new ParameterTypeSpec[cecilArgs.Count];
                for (int i = 0; i < args.Length; i++)
                    args[i] = FromCecil(cecilArgs[i]);
                var open = genericInstance.ElementType;
                return GenericInstanceOf(open.FullName, open.Name, args);
            }

            return Simple(type.FullName, type.Name);
        }
    }
}
