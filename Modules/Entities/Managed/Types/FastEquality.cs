// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Core;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

[assembly: InternalsVisibleTo("Unity.Entities.Tests")]

namespace Unity.Entities
{
    /// <summary>
    /// Utility to look up equality and hashcode functions for component types.
    /// </summary>
    /// <remarks>The TypeManager uses this type internally and you shouldn't need to create or directly use
    /// this in the majority of use cases. When possible, a Burst-compilable implementation is preferred,
    /// but if an IEquatable{T} method isn't marked as Burst compiled, the managed
    /// version is used instead.</remarks>
    [BurstCompile]
    public partial struct FastEquality
    {
        [NoAutoStaticsCleanup] // TypeManager.Shutdown calls Shutdown.
        static List<Delegate> s_ComponentDelegates;

        internal struct LayoutInfo
        {
            public ushort Size;
            public ushort Offset;
        }

        /// <summary>
        /// Type holding one component's Equals and GetHashCode implementations as well as the size
        /// of the underlying unmanaged component type
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct TypeInfo : IDisposable
        {
            /// <summary>
            /// Equals method delegate for comparing two component instances whose type is not known at compile time.
            /// </summary>
            public unsafe delegate bool CompareEqualDelegate(void* lhs, void* rhs);

            /// <summary>
            /// GetHashCode method delegate for hashing a component whose type is not known at compile time.
            /// </summary>
            public unsafe delegate int GetHashCodeDelegate(void* obj);

            internal NativeArray<LayoutInfo> LayoutInfo;

            /// <summary>
            /// Holds the index for the Equals delegate to use when comparing two instances of a component.
            /// </summary>
            internal ushort EqualsDelegateIndex;

            /// <summary>
            /// Holds the index for the GetHashCode delegate to use for a component.
            /// </summary>
            internal ushort GetHashCodeDelegateIndex;

            /// <summary>
            /// Represents an invalid TypeInfo instance.
            /// </summary>
            public static TypeInfo Null => new TypeInfo();

            /// <summary>
            /// Releases the layout data.
            /// </summary>
            public void Dispose()
            {
                if(LayoutInfo.IsCreated)
                    LayoutInfo.Dispose();
            }
        }

        static ushort AddDelegate(Delegate d)
        {
            int index = s_ComponentDelegates.Count;
            s_ComponentDelegates.Add(d);
            Assert.IsTrue(index < ushort.MaxValue);
            return (ushort)index;
        }

        static Delegate GetDelegate(ushort index)
        {
            return s_ComponentDelegates[index];
        }

        static internal void Initialize()
        {
            s_ComponentDelegates = new List<Delegate>();
            AddDelegate(null); // reserve index 0 as null element
        }

        static internal void Shutdown()
        {
            s_ComponentDelegates.Clear();
        }

        internal static TypeInfo CreateTypeInfo<T>(Dictionary<Type, List<LayoutInfo>> cache = null) where T : struct
        {
            return CreateTypeInfo(typeof(T), cache);
        }

        internal static TypeInfo CreateTypeInfo(Type type, Dictionary<Type, List<LayoutInfo>> cache = null)
        {
            // Only unmanaged types can be components, so there is nothing to compare or hash for anything else.
            if (!UnsafeUtility.IsUnmanaged(type))
                return TypeInfo.Null;

            if (TypeUsesDelegates(type))
                return CreateDelegateTypeInfo(type);
            else
                return CreateTypeInfoBlittable(type, cache);
        }

        private struct Dummy : IEquatable<Dummy>
        {
            public bool Equals(Dummy other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        private struct CompareImpl<T> where T : struct, IEquatable<T>
        {
            public static unsafe bool CompareFunc(void* lhs, void* rhs)
            {
                return UnsafeUtility.AsRef<T>(lhs).Equals(UnsafeUtility.AsRef<T>(rhs));
            }
        }

        private struct GetHashCodeImpl<T> where T : struct, IEquatable<T>
        {
            public static unsafe int GetHashCodeFunc(void* lhs)
            {
                return UnsafeUtility.AsRef<T>(lhs).GetHashCode();
            }
        }

        private unsafe static TypeInfo CreateDelegateTypeInfo(Type t)
        {
            // Type must override GetHashCode()
            var ghcMethod = t.GetMethod(nameof(GetHashCode), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Array.Empty<Type>(), null);
            if (ghcMethod == null || ghcMethod.DeclaringType != t)
            {
                throw new ArgumentException($"type {t} implements IEquatable<T>, you must also override GetHashCode()");
            }

            var equalsFn = typeof(CompareImpl<>).MakeGenericType(t).GetMethod(nameof(CompareImpl<Dummy>.CompareFunc));
            var getHashFn = typeof(GetHashCodeImpl<>).MakeGenericType(t).GetMethod(nameof(GetHashCodeImpl<Dummy>.GetHashCodeFunc));

            return new TypeInfo
            {
                EqualsDelegateIndex = AddDelegate(Delegate.CreateDelegate(typeof(TypeInfo.CompareEqualDelegate), equalsFn)),
                GetHashCodeDelegateIndex = AddDelegate(Delegate.CreateDelegate(typeof(TypeInfo.GetHashCodeDelegate), getHashFn))
            };
        }

        private static TypeInfo CreateTypeInfoBlittable(Type type, Dictionary<Type, List<LayoutInfo>> cache = null)
        {
            return new TypeInfo
            {
                LayoutInfo = CreateDescriptor(type, cache)
            };
        }

        internal static NativeArray<LayoutInfo> CreateDescriptor(Type type, Dictionary<Type, List<LayoutInfo>> cache = null)
        {
            if (cache == null)
                cache = new Dictionary<Type, List<LayoutInfo>>();

            var layoutInfo = FindFields(type, cache);

            return new NativeArray<LayoutInfo>(layoutInfo.ToArray(), Allocator.Persistent);
        }

        /// <summary>
        /// Writes a mask of <paramref name="size"/> bytes: 0xFF for bytes that belong to a field in
        /// <paramref name="layoutInfo"/>, 0x00 for struct padding.
        /// </summary>
        internal static unsafe void FillFieldMask(NativeArray<LayoutInfo> layoutInfo, byte* mask, int size)
        {
            UnsafeUtility.MemClear(mask, size);
            for (var i = 0; i < layoutInfo.Length; i++)
            {
                if (layoutInfo[i].Offset + layoutInfo[i].Size > size)
                    continue;
                UnsafeUtility.MemSet(mask + layoutInfo[i].Offset, 0xFF, layoutInfo[i].Size);
            }
        }

        private static List<LayoutInfo> FindFields(Type type, Dictionary<Type, List<LayoutInfo>> cache,int fixedSizeArrayLength = 1)
        {
            if (cache.TryGetValue(type, out var cachedInfo))
                return cachedInfo;

            if (!type.IsValueType)
                throw new ArgumentException($"{type} is not allowed: only value types are supported");

            if (type.IsGenericTypeDefinition)
                throw new ArgumentException($"{type} is not allowed: only concrete, fully-closed types are supported");

            var result = new List<LayoutInfo>();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                int fieldOffset = UnsafeUtility.GetFieldOffset(field);

                // If the fields are not tightly packed, make a new start (size is updated below)
                if (result.Count == 0 || result[^1].Offset + result[^1].Size != fieldOffset)
                    result.Add(new LayoutInfo { Size = 0, Offset = (ushort)fieldOffset });

                if (field.FieldType.IsPrimitive || field.FieldType.IsPointer)
                {
                    int sizeOf;
                    if (field.FieldType.IsPointer)
                        sizeOf = UnsafeUtility.SizeOf<IntPtr>();
                    else
                        sizeOf = UnsafeUtility.SizeOf(field.FieldType) * fixedSizeArrayLength;

                    if (fieldOffset + sizeOf > ushort.MaxValue)
                        throw new ArgumentException($"Structures larger than 64k are not supported");

                    // If we are tightly packed this will update the size appropriately, if we aren't it's updating
                    // the size from 0 to the correct run length (the offset is set at the top of the loop)
                    var lastIndex = result.Count - 1;
                    var layoutInfo = result[lastIndex];
                    layoutInfo.Size += (ushort)sizeOf;
                    result[lastIndex] = layoutInfo;
                }
                else
                {
                    // Fixed arrays end up being wrapped in a struct with the FixedBuffer attribute
                    var fixedAttr = field.GetCustomAttribute<FixedBufferAttribute>();
                    if (fixedAttr != null)
                        fixedSizeArrayLength = fixedAttr.Length;

                    var structInfos = FindFields(field.FieldType, cache, fixedSizeArrayLength);
                    fixedSizeArrayLength = 1;
                    cache.TryAdd(field.FieldType, structInfos);

                    if (structInfos.Count >= 1)
                    {
                        // We are tightly packed thus far, so only extend the size of our last contiguous run
                        var lastIndex = result.Count - 1;
                        var layoutInfo = result[lastIndex];
                        layoutInfo.Size += structInfos[0].Size;
                        result[lastIndex] = layoutInfo;

                        for (int i = 1; i < structInfos.Count; i++)
                        {
                            var structInfo = structInfos[i];
                            structInfo.Offset += (ushort) fieldOffset;
                            result.Add(structInfo);
                        }
                    }
                }
            }
            return result;
        }

        private const int FNV_32_PRIME = 0x01000193;

        [BurstDiscard]
        static unsafe void GetHashCodeUsingDelegate(void* dataPtr, TypeInfo typeInfo, ref bool didWork, ref int hash)
        {
            if (typeInfo.GetHashCodeDelegateIndex != TypeInfo.Null.GetHashCodeDelegateIndex)
            {
                var fn = (TypeInfo.GetHashCodeDelegate) GetDelegate(typeInfo.GetHashCodeDelegateIndex);
                hash = fn(dataPtr);
                didWork = true;
            }
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe int GetHashCode(void* dataPtr, in TypeInfo typeInfo)
        {
            int hash = 0;
            bool didWork = false;
            GetHashCodeUsingDelegate(dataPtr, typeInfo, ref didWork, ref hash);
            if(didWork)
                return hash;

            return GetHashCodeBlittable(dataPtr, in typeInfo);
        }

        [BurstCompile]
        private static unsafe int GetHashCodeBlittable(void* dataPtr, in TypeInfo typeInfo)
        {
            int hash = 0;
            var layoutInfo = typeInfo.LayoutInfo;
            int len = layoutInfo.Length;
            for (int i = 0; i < len; i++)
            {
                var info = layoutInfo[i];
                hash ^= Hash32((byte*)dataPtr + info.Offset, info.Size, hash);
            }

            return hash;
        }

        [BurstCompile]
        internal static unsafe int Hash32(byte* ptr, int length, int seed = 0)
        {
            int hash = seed;
            for (int i = 0; i < length; ++i)
            {
                hash *= FNV_32_PRIME;
                hash ^= ptr[i];
            }

            return hash;
        }

        [BurstDiscard]
        static unsafe void EqualsUsingDelegate(void* lhsPtr, void* rhsPtr, TypeInfo typeInfo, ref bool didWork, ref int result)
        {
            if (typeInfo.EqualsDelegateIndex != TypeInfo.Null.EqualsDelegateIndex)
            {
                var fn = (TypeInfo.CompareEqualDelegate) GetDelegate(typeInfo.EqualsDelegateIndex);
                // Note, a match returns 0 to mirror the Equals code below for the unmanaged case where we memcmp
                result = fn(lhsPtr, rhsPtr) ? 0 : 1;
                didWork = true;
            }
        }

        /// <summary>
        /// Compares two component types.
        /// </summary>
        /// <param name="lhsPtr">Pointer to the component on the left-side of the comparison.</param>
        /// <param name="rhsPtr">Pointer to the component on the right-side of the comparison.</param>
        /// <param name="typeInfo">TypeInfo to provide the Equals method to invoke.</param>
        /// <returns>Returns true if the components are equal. False otherwise.</returns>
        [GenerateTestsForBurstCompatibility]
        public static unsafe bool Equals(void* lhsPtr, void* rhsPtr, in TypeInfo typeInfo)
        {
            int result = 0;
            bool didWork = false;
            EqualsUsingDelegate(lhsPtr, rhsPtr, typeInfo, ref didWork, ref result);
            if (didWork)
                return result == 0;
            return EqualsBlittable(lhsPtr, rhsPtr, in typeInfo);
        }

        [BurstCompile]
        private static unsafe bool EqualsBlittable(void* lhsPtr, void* rhsPtr, in TypeInfo typeInfo)
        {
            int result = 0;
            var layoutInfo = typeInfo.LayoutInfo;
            int len = layoutInfo.Length;
            for (int i = 0; i < len; i++)
            {
                var info = layoutInfo[i];
                var leftPtr = (byte*)lhsPtr + info.Offset;
                var rightPtr = (byte*)rhsPtr + info.Offset;
                result |= UnsafeUtility.MemCmp(leftPtr, rightPtr, info.Size);
            }

            return result == 0;

        }

        private static bool TypeUsesDelegates(Type t)
        {
            // If an unmanaged shared component but custom equality methods have been provided then use them
            return typeof(ISharedComponentData).IsAssignableFrom(t) && typeof(IEquatable<>).MakeGenericType(t).IsAssignableFrom(t);
        }

        /// <summary>
        /// Internal method used for populating closed forms of generic equality methods to ensure IL2CPP
        /// can generate transpiled implementations and ensure they are not-stripped from builds.
        /// </summary>
        /// <param name="type">Type to close</param>
        /// <param name="output">Set of fully qualified closed generic type names to preserve</param>
        public static void AddExtraAOTTypes(Type type, HashSet<String> output)
        {
            if (!UnsafeUtility.IsUnmanaged(type) || !TypeUsesDelegates(type))
                return;

            output.Add(typeof(CompareImpl<>).MakeGenericType(type).ToString());
            output.Add(typeof(GetHashCodeImpl<>).MakeGenericType(type).ToString());
        }
    }
}
