// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    /// <summary>
    /// Produces a stable type hash for a Type based on its layout in memory and how that memory should be interpreted.
    /// </summary>
    /// <remarks>
    /// This hash is NOT expected to remain the same across versions.
    ///
    /// You can rename field members and this hash isn't affected. However, if a field's type changes,
    /// this hash changes, because the interpretation of the underlying memory will have contextually changed.
    ///
    /// The purpose of the stable type hash is to provide a version linking serialized component data to its runtime
    /// representation.
    ///
    /// As such, the type hash has a few requirements:
    ///    - **R0:** TypeHashes apply only to types that Unity.Entities serializes. The internals of a UnityEngine.Object reference
    ///      contained in a component mustn't have an effect on the type hash. You can safely change the internals of the
    ///      UnityEngine.Object reference because they're serialized outside of Unity.Entities.
    ///    - **R1:** Types with the same data layout but are different types should have different hashes.
    ///    - **R2:** If a type's data layout changes, so should the type hash. This includes:
    ///      - A nested field's data layout changes (for example, a new member added)
    ///      - FieldOffsets, explicit size, or pack alignment are changed
    ///      - Different types of the same width swap places (for example if a uint swaps with an int)
    ///      - **Note:** Unity can't detect if fields of the same type swap (for example, mInt1 swaps with mInt2) This is a semantic
    ///        difference which you should increase your component [TypeVersion(1)] attribute. You shouldn't
    ///        try to hash field names to handle this, because you can't control all field names.
    ///        Also, field names have no effect serialization and doesn't affect hashes.
    ///    - **R3:** You should version the hash in case the semantics of a type change, but the data layout is unchanged
    ///    - **R4:** DOTS Runtime relies on hashes generated from the Editor (used in serialized data) and hashes generated
    ///      during compilation. These hashes must match. This rule exists because of the following:
    ///      - Tiny swapa out assemblies for 'tiny' versions, which means you should avoid any hashing using the AssemblyName
    ///        or handle it specially for the known swapped assemblies.
    ///        - This means you should avoid Type.AssemblyQualifiedName in hashes, but as well, closed-form
    ///          generic types include the assembly qualified name for GenericArguments in Type.FullName which
    ///          causes issues. For example, `typeof(ComponentWithGeneric.GenericField).FullName ==
    ///          Unity.Entities.ComponentWithGeneric.GenericField`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]`
    ///      - System.Reflection and Mono.Cecil provide different Type names (they use a different format).
    ///        Generating hashes from System.Reflection to match hashes using Mono.Cecil must account for this difference.
    /// </remarks>
    public class TypeHash
    {
        // http://www.isthe.com/chongo/src/fnv/hash_64a.c
        // with basis and prime:
        const ulong kFNV1A64OffsetBasis = 14695981039346656037;
        const ulong kFNV1A64Prime = 1099511628211;

        /// <summary>
        /// Generates a FNV1A64 hash.
        /// </summary>
        /// <param name="text">Text to hash.</param>
        /// <returns>Hash of input string.</returns>
        public static ulong FNV1A64(string text)
        {
            ulong result = kFNV1A64OffsetBasis;
            foreach (var c in text)
            {
                result = kFNV1A64Prime * (result ^ (byte)(c & 255));
                result = kFNV1A64Prime * (result ^ (byte)(c >> 8));
            }
            return result;
        }

        /// <summary>
        /// Generates a FNV1A64 hash.
        /// </summary>
        /// <param name="text">Text to hash.</param>
        /// <typeparam name="T">Unmanaged IUTF8 type.</typeparam>
        /// <returns>Hash of input string.</returns>
        public static ulong FNV1A64<T>(T text)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ulong result = kFNV1A64OffsetBasis;
            for(int i = 0; i <text.Length; ++i)
            {
                var c = text[i];
                result = kFNV1A64Prime * (result ^ (byte)(c & 255));
                result = kFNV1A64Prime * (result ^ (byte)(c >> 8));
            }
            return result;
        }

        /// <summary>
        /// Generates a FNV1A64 hash.
        /// </summary>
        /// <param name="val">Value to hash.</param>
        /// <returns>Hash of input.</returns>
        public static ulong FNV1A64(int val)
        {
            ulong result = kFNV1A64OffsetBasis;
            unchecked
            {
                result = (((ulong)(val & 0x000000FF) >>  0) ^ result) * kFNV1A64Prime;
                result = (((ulong)(val & 0x0000FF00) >>  8) ^ result) * kFNV1A64Prime;
                result = (((ulong)(val & 0x00FF0000) >> 16) ^ result) * kFNV1A64Prime;
                result = (((ulong)(val & 0xFF000000) >> 24) ^ result) * kFNV1A64Prime;
            }

            return result;
        }

        /// <summary>
        /// Combines a FNV1A64 hash with a value.
        /// </summary>
        /// <param name="hash">Input Hash.</param>
        /// <param name="value">Value to add to the hash.</param>
        /// <returns>A combined FNV1A64 hash.</returns>
        public static ulong CombineFNV1A64(ulong hash, ulong value)
        {
            hash ^= value;
            hash *= kFNV1A64Prime;

            return hash;
        }

        private static ulong HashType(Type type, Dictionary<Type, ulong> cache)
        {
            if (cache.TryGetValue(type, out ulong hash))
                return hash;

            /*
             * We want to be consistent with what the typemanager says, but the typemanager uses results generated by ilpp,
             * which are not guaranteed to be the same as what we compute via reflection. So, first check if the typemanager
             * has an opinion, and go with that if it does.
             */
            if (TypeManager.IsInitialized && TypeManager.TryGetTypeIndex(type, out var ti))
            {
                unsafe
                {
                    var ret = TypeManager.GetTypeInfoNoSharedStatic(ti).StableTypeHash;
                    cache[type] = ret;
                    return ret;
                }
            }

            hash = HashTypeName(type);

            // If we shouldn't walk the type's fields just return the type name hash.
            // UnityEngine objects have their own serialization mechanism so exclude hashing their internals.
            // Platform (BCL) value types are hashed by name only: the ILPP hasher resolves them against
            // reference assemblies whose private fields are replaced with dummies, so field walks can
            // never agree between compile time and runtime. Must stay in sync with CodeGen's TypeHash.
            if (type.IsArray || type.IsPointer || type.IsPrimitive || type.IsEnum || (TypeManager.UnityEngineObjectType?.IsAssignableFrom(type) == true)
                || (type.IsValueType && IsPlatformAssembly(type.Assembly)))
            {
                cache.Add(type, hash);
                return hash;
            }

            if (type.ContainsGenericParameters)
                throw new ArgumentException($"'{type}' contains open generic parameters. Generic types must have all generic parameters specified to closed types when calculating stable type hashes");

            // Only non-pod and non-unityengine types could possibly have a version attribute
            hash = CombineFNV1A64(hash, HashVersionAttribute(type));

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for(ulong fieldIndex = 0;  fieldIndex < (ulong)fields.Length; fieldIndex++)
            {
                var field = fields[fieldIndex];
                // statics have no effect on data layout
                if (field.IsStatic)
                    continue;

                var fieldType = field.FieldType;
                ulong fieldTypeHash = 0;

                // Class types can have circular definitions, so for class fields only hash the type name
                if (fieldType.IsClass)
                    fieldTypeHash = HashTypeName(fieldType);
                else if(!cache.TryGetValue(fieldType, out fieldTypeHash))
                {
                    fieldTypeHash = HashType(fieldType, cache);
                }

                fieldTypeHash = CombineFNV1A64(fieldTypeHash, fieldIndex);

                var offset = field.GetCustomAttribute<FieldOffsetAttribute>();
                if (offset != null)
                    fieldTypeHash = CombineFNV1A64(fieldTypeHash, (ulong)offset.Value);

                hash = CombineFNV1A64(hash, fieldTypeHash);
            }

            // TODO: Enable this. Currently IL2CPP gives totally inconsistent results to Mono.
            /*
            if (type.StructLayoutAttribute != null && !type.StructLayoutAttribute.IsDefaultAttribute())
            {
                var explicitSize = type.StructLayoutAttribute.Size;
                if (explicitSize > 0)
                    hash = CombineFNV1A64(hash, (ulong)explicitSize);

                // Todo: Enable this. We cannot support Pack at the moment since a type's Packing will
                // change based on its field's explicit packing which will fail for Tiny mscorlib
                // as it's not in sync with dotnet
                // var packingSize = type.StructLayoutAttribute.Pack;
                // if (packingSize > 0)
                //     hash = CombineFNV1A64(hash, (ulong)packingSize);
            }
            */

            cache.Add(type, hash);
            return hash;
        }

        private static ulong HashVersionAttribute(Type type)
        {
            int version = 0;

            var versionAttribute = type.GetCustomAttribute<TypeManager.TypeVersionAttribute>(true);
            if (versionAttribute != null)
            {
                version = versionAttribute.TypeVersion;
            }

            return FNV1A64(version);
        }


        private static ulong HashNamespace(Type type)
        {
            var hash = kFNV1A64OffsetBasis;

            // System.Reflection and Cecil don't report namespaces the same way so do an alternative:
            // Find the namespace of an un-nested parent type, then hash each of the nested children names
            if (type.IsNested)
            {
                hash = CombineFNV1A64(hash, HashNamespace(type.DeclaringType));
                hash = CombineFNV1A64(hash, FNV1A64(type.DeclaringType.Name));
            }
            else if (!string.IsNullOrEmpty(type.Namespace))
                hash = CombineFNV1A64(hash, FNV1A64(type.Namespace));

            return hash;
        }

        // Strong-name key tokens of .NET platform assemblies. BCL types relocate between
        // assemblies across runtimes (Mono mscorlib/System.dll vs CoreCLR System.Private.CoreLib
        // and its satellites), so all platform-signed assemblies hash as a single identity to
        // keep editor and player hashes equal. Public BCL type full names are globally unique
        // (netstandard type-forwarding relies on this), so collapsing cannot merge distinct types.
        static readonly ulong[] kPlatformKeyTokens =
        {
            0xb77a5c561934e089, // ECMA (mscorlib, System, System.Core)
            0xb03f5f7f11d50a3a, // Microsoft (most CoreCLR System.* satellites)
            0xcc7b13ffcd2ddd51, // .NET Foundation (netstandard, System.Memory, System.Text.Json)
            0x7cec85d7bea7798e, // .NET Core (System.Private.CoreLib)
            0x31bf3856ad364e35, // WinFX (WindowsBase)
            0x0738eb9f132ed756, // Mono (Mono.Security, Mono.Posix)
        };

        // Required for transition between Mono and CoreCLR since the Editor is building and recording a stable
        // type hash from Mono assemblies, and then comparing to a player type hash made from CoreCLR assemblies
        private static bool IsPlatformAssembly(Assembly assembly)
        {
            var token = assembly.GetName().GetPublicKeyToken();
            if (token == null || token.Length != 8)
                return false;

            ulong tokenValue = 0;
            foreach (var b in token)
                tokenValue = (tokenValue << 8) | b;

            return Array.IndexOf(kPlatformKeyTokens, tokenValue) >= 0;
        }

        private static string GetStableAssemblyName(Assembly assembly)
        {
            return IsPlatformAssembly(assembly) ? "FrameworkAssembly" : assembly.GetName().Name;
        }

        private static ulong HashTypeName(Type type)
        {
            ulong hash = HashNamespace(type);
            hash = CombineFNV1A64(hash, FNV1A64(type.Name));
            hash = CombineFNV1A64(hash, FNV1A64(GetStableAssemblyName(type.Assembly)));
            foreach (var ga in type.GenericTypeArguments)
            {
                Assert.IsTrue(!ga.IsGenericParameter);
                hash = CombineFNV1A64(hash, HashTypeName(ga));
            }

            return hash;
        }


        /// <summary>
        /// Calculates a stable type hash for the input type.
        /// </summary>
        /// <param name="type">Type to hash.</param>
        /// <param name="customAttributes">Custom attributes for the provided type (if any).</param>
        /// <param name="hashCache">Cache for Types and their hashes. Used for quicker lookups when hashing.</param>
        /// <returns>StableTypeHash for the input type.
        /// </returns>
        /// <remarks>This hash is NOT expected to remain the same across versions.</remarks>
        public static ulong CalculateStableTypeHash(Type type, IEnumerable<CustomAttributeData> customAttributes = null, Dictionary<Type, ulong> hashCache = null)
        {
            // This API should not be called anymore but we can't deprecate it in a minor release so we forward to a new one
            if (hashCache == null)
                hashCache = new Dictionary<Type, ulong>();

            return CalculateStableTypeHash(type, hashCache);
        }

        /// <summary>
        /// Calculates a stable type hash for the input type.
        /// </summary>
        /// <param name="type">Type to hash.</param>
        /// <param name="customAttributes">Custom attributes for the provided type (if any).</param>
        /// <param name="hashCache">Cache for Types and their hashes. Used for quicker lookups when hashing.</param>
        /// <returns>StableTypeHash for the input type.</returns>
        /// <remarks>This hash is NOT expected to remain the same across versions.</remarks>
        public static ulong CalculateStableTypeHash(Type type, Dictionary<Type, ulong> hashCache)
        {
            Assert.IsNotNull(hashCache, "The cache used for calculating type hashes must not be null");
            ulong hash = HashType(type, hashCache);
            return hash;
        }

        /// <summary>
        /// Calculates a MemoryOrdering for the input type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <param name="hasCustomMemoryOrder">Out param; set to true if the memory order has been explicitly overriden for the input type.</param>
        /// <param name="hashCache">Cache for Types and their hashes. Used for quicker lookups when hashing.</param>
        /// <returns>MemoryOrdering for the input type.</returns>
        public static ulong CalculateMemoryOrdering(Type type, out bool hasCustomMemoryOrder, Dictionary<Type, ulong> hashCache = null)
        {
            hasCustomMemoryOrder = false;

            if (type == null || type.FullName == "Unity.Entities.Entity")
            {
                return 0;
            }

            var customAttributes = type.CustomAttributes;
            foreach (var customAttribute in customAttributes)
            {
                if (customAttribute.Constructor.DeclaringType != typeof(TypeManager.ForcedMemoryOrderingAttribute))
                    continue;

                foreach (var arg in customAttribute.ConstructorArguments)
                {
                    if (arg.ArgumentType.Name == "UInt64" || arg.ArgumentType.Name == "ulong")
                    {
                        hasCustomMemoryOrder = true;
                        return (ulong)arg.Value;
                    }
                }
            }

            return CalculateStableTypeHash(type, customAttributes, hashCache);
        }
    }
}
