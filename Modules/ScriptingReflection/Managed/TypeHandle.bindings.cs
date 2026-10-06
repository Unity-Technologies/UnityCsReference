// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;

namespace Unity.Scripting.Reflection
{
    // Bulk queries fill the buffer and return the full count; a capacity of 0 asks for the count alone
    [NativeHeader("ScriptingReflection/TypeHandle.bindings.h")]
    [StaticAccessor("ScriptingReflectionBindings::TypeHandle", StaticAccessorType.DoubleColon)]
    internal partial struct TypeHandle
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsValueType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsEnum(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetParent(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsAbstract(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsInterface(IntPtr klass);

        // Includes interfaces, unlike IsSubclassOf
        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsSubtypeOf(IntPtr klass, IntPtr baseClassOrInterface);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsSubclassOf(IntPtr klass, IntPtr baseClass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsArray(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetDeclaringType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetElementType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsPrimitive(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsSealed(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool HasDefaultConstructor(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsUnmanaged(IntPtr klass);

        // Writes PublicKeyTokenLength bytes; false when the assembly is not strong named
        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetClassAssemblyPubKeyToken(IntPtr klass, IntPtr outToken);

        // Empty unless klass is a closed generic instantiation
        [NativeMethod(IsThreadSafe = true)]
        static extern int GetGenericArguments(IntPtr klass, IntPtr outArguments, int capacity);

        // Writes nothing unless the whole result fits; the first entry starts a block from allocator
        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetCustomAttributes(IntPtr klass, IntPtr attributeKlass, Allocator allocator, IntPtr outAttributes, int capacity);

        // The language defaults, true and false, when AttributeUsage declares neither
        [NativeMethod(IsThreadSafe = true)]
        static extern void GetAttributeUsage(IntPtr attributeKlass, out bool inherited, out bool allowMultiple);

        [NativeMethod(IsThreadSafe = true)]
        static extern string GetClassFullName(IntPtr klass);

        // The display name, with version, culture and public key token
        [NativeMethod(IsThreadSafe = true)]
        static extern string GetClassAssemblyFullName(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsPointer(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsGenericParameter(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsGenericType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsGenericTypeDefinition(IntPtr klass);

        // An open definition answers itself; null for a non-generic type
        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetGenericTypeDefinition(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool ContainsGenericParameters(IntPtr klass);

        // Without the object header, so only meaningful for value types
        [NativeMethod(IsThreadSafe = true)]
        static extern int SizeOf(IntPtr klass);

        // Owned by the backend's metadata: never freed, and outlives the class
        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetClassNameCString(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetClassNamespaceCString(IntPtr klass);

        [NativeMethod("GetClassNameCString", IsThreadSafe = true)]
        static extern string GetClassName(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetClassType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr ClassFromType(IntPtr type);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type GetSystemType(IntPtr klass);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool HasAttribute(IntPtr klass, IntPtr attributeKlass);

        // Includes inherited fields; entries are FieldHandle pairs, not bare pointers
        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetFields(IntPtr klass, IntPtr outFields, int capacity);

        // declaredCount receives how many leading entries belong to klass itself
        [NativeMethod(IsThreadSafe = true)]
        static extern int GetFieldsAndFlags(IntPtr klass, bool declaredOnly, IntPtr outFields, IntPtr outFlags, int capacity, out int declaredCount);

        // Every interface klass implements, including those of its base classes, as Type.GetInterfaces()
        [NativeMethod(IsThreadSafe = true)]
        static extern int GetInterfaces(IntPtr klass, IntPtr outInterfaces, int capacity);

        // Matches an interface in GetInterfaces exactly, or by its generic definition, so IList<> finds IList<int>
        [NativeMethod(IsThreadSafe = true)]
        static extern bool HasInterface(IntPtr klass, IntPtr interfaceKlass);
    }
}
