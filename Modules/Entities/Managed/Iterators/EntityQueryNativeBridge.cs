// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    // The native EntityQuery (Modules/Entities/Public/EntityQuery.h) calls these to take and drop its reference to a
    // world's query cache entry. Iterating reads C# memory through native mirrors of the C# structs, without calling C#.
    [NativeHeader("Modules/Entities/Public/EntityQuery.bindings.h")]
    static unsafe class EntityQueryNativeBridge
    {
        [FreeFunction("EntityQueryBindings::CheckManagedLayout", IsThreadSafe = true)]
        static extern int CheckManagedLayout(int* values, int count);

        // Sends the sizes, offsets and constants that EntityQueryBindings::CheckManagedLayout compares, in its order,
        // and returns the index of the first one that differs, or -1. NativeLayoutTests calls it.
        internal static int FindLayoutMismatch()
        {
            Span<int> values = stackalloc int[16];
            var i = 0;

            var record = default(MatchingArchetype);
            var recordStart = (byte*)&record;
            values[i++] = sizeof(MatchingArchetype);
            values[i++] = (int)((byte*)&record.Archetype - recordStart);
            values[i++] = (int)((byte*)&record.RequiredComponentCount - recordStart);
            values[i++] = (int)((byte*)&record.EnableableComponentsCount_All - recordStart);
            values[i++] = (int)((byte*)&record.EnableableComponentsCount_None - recordStart);
            values[i++] = (int)((byte*)&record.EnableableComponentsCount_Any - recordStart);
            values[i++] = (int)((byte*)&record.EnableableComponentsCount_Disabled - recordStart);
            values[i++] = (int)((byte*)record.IndexInArchetype - recordStart);

            var list = default(UnsafeList<IntPtr>);
            var listStart = (byte*)&list;
            values[i++] = (int)((byte*)&list.Ptr - listStart);
            values[i++] = (int)((byte*)&list.m_length - listStart);

            var type = default(ComponentType);
            var typeStart = (byte*)&type;
            values[i++] = sizeof(ComponentType);
            values[i++] = (int)((byte*)&type.TypeIndex - typeStart);
            values[i++] = (int)((byte*)&type.AccessModeType - typeStart);
            values[i++] = (int)ComponentType.AccessMode.Exclude;

            fixed (int* pointer = values)
            {
                return CheckManagedLayout(pointer, i);
            }
        }

        // Takes a reference to the cache entry and returns its index, or -1. A type id is a TypeIndex without its flag
        // bits; kinds are the values of the native EntityQueryTypeKind. The entry's record list and required types stay
        // where they are while the entry lives. outEnableableTypeIds has room for count ids, and gets the ids of the
        // enableable types.
        [RequiredByNativeCode]
        internal static int AcquireCacheEntry(IntPtr world, IntPtr typeIds, IntPtr kinds, int count, int options,
            IntPtr outRecords, IntPtr outRequiredTypes, IntPtr outRequiredTypeCount, IntPtr outEnableableTypeIds, IntPtr outEnableableTypeCount,
            IntPtr outWorldSequenceNumber)
        {
            try
            {
                var builder = new EntityQueryBuilder(Allocator.Temp);
                var all = new NativeList<ComponentType>(count, Allocator.Temp);
                var any = new NativeList<ComponentType>(count, Allocator.Temp);
                var absent = new NativeList<ComponentType>(count, Allocator.Temp);
                var disabled = new NativeList<ComponentType>(count, Allocator.Temp);
                var present = new NativeList<ComponentType>(count, Allocator.Temp);
                var enableableCount = 0;
                for (var i = 0; i < count; ++i)
                {
                    var type = ComponentType.ReadOnly(ToTypeIndex(((uint*)typeIds)[i]));
                    if (type.IsEnableable)
                        ((uint*)outEnableableTypeIds)[enableableCount++] = (uint)type.TypeIndex.Index;
                    switch (((byte*)kinds)[i])
                    {
                        case 0: all.Add(type); break;
                        case 1: any.Add(type); break;
                        case 2: absent.Add(type); break;
                        case 3: disabled.Add(type); break;
                        case 4: present.Add(type); break;
                        default: throw new ArgumentException($"{((byte*)kinds)[i]} isn't a query type kind.");
                    }
                }
                builder.WithAll(ref all).WithAny(ref any).WithAbsent(ref absent).WithDisabled(ref disabled).WithPresent(ref present)
                    .WithOptions((EntityQueryOptions)options);

                var impl = (WorldUnmanagedImpl*)world;
                var access = impl->m_EntityManager.GetCheckedEntityDataAccess();
                var queries = access->EntityQueryManager;
                var index = queries->AcquireCacheEntry(builder, access->EntityComponentStore);
                var entry = queries->GetCacheEntry(index);
                *(UnsafeList<IntPtr>**)outRecords = entry->Records.List;
                *(ComponentType**)outRequiredTypes = entry->RequiredTypes;
                *(int*)outRequiredTypeCount = entry->RequiredTypeCount;
                *(int*)outEnableableTypeCount = enableableCount;
                *(ulong*)outWorldSequenceNumber = impl->SequenceNumber;
                return index;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return -1;
            }
        }

        // Does nothing when the world is gone, since its cache went with it
        [RequiredByNativeCode]
        internal static void ReleaseCacheEntry(IntPtr world, ulong worldSequenceNumber, int index)
        {
            foreach (var candidate in World.s_AllWorlds)
            {
                var impl = candidate.Unmanaged.GetImplPtr();
                if (impl == (WorldUnmanagedImpl*)world && impl->SequenceNumber == worldSequenceNumber)
                {
                    impl->m_EntityManager.GetCheckedEntityDataAccess()->EntityQueryManager->ReleaseCacheEntry(index);
                    return;
                }
            }
        }

        // Type ids are sparse, and an id without a registered type finds the null type's TypeInfo
        static TypeIndex ToTypeIndex(uint typeId)
        {
            var typeIndex = TypeManager.GetTypeInfo(new TypeIndex { Value = (int)typeId }).TypeIndex;
            if (typeId == 0 || typeIndex.Index != (int)typeId)
                throw new ArgumentException($"{typeId} isn't a registered component type id.");
            return typeIndex;
        }
    }
}
