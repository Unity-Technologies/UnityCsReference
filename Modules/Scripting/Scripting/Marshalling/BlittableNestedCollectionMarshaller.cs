// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Bindings
{
    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "Marshalling::NestedCollectionData")]
    [NativeHeader("Scripting/Marshalling/BlittableNestedCollectionMarshaller.h")]
    internal unsafe struct NestedCollectionData
    {
        public void* Data;
        public int Length;
    }

    [VisibleToOtherModules]
    /// <summary>
    /// Marshals a collection of collections (e.g. List<int[]>, int[][]) of blittable types into native
    /// This class only marshals data in - no data will be updated
    /// NOTE: This class does multiple allocations and is not particularly efficient
    ///       Instead consider using a multi-dimensional array (e.g. int[,]) which be marshalled to native as a pinned buffer
    /// </summary>
    internal unsafe struct BlittableNestedCollectionMarshaller<T> where T : unmanaged
    {
        private static readonly int AlignOfT;

        static BlittableNestedCollectionMarshaller()
        {
            AlignOfT = UnsafeUtilityInternal.AlignOf<T>();
        }

        public static NestedCollectionData ConvertToUnmanaged(IList outerCollection)
        {
            if (outerCollection == null)
                return default;

            int outerElementCount = outerCollection.Count;  
            int totalElementCount = 0;

            for (int i = 0; i < outerElementCount; i++)
            {
                totalElementCount += (outerCollection[i] as ICollection<T>)?.Count ?? 0;
            }

            if (totalElementCount == 0)
                return default;

            var innerCollectionData = (NestedCollectionData*)BindingsAllocator.Malloc(checked(outerCollection.Count * sizeof(NestedCollectionData) + AlignOfT + totalElementCount * sizeof(T)));

            NestedCollectionData marshalled;
            marshalled.Length = outerCollection.Count;
            marshalled.Data = innerCollectionData;

            var dataStart = (nuint)(innerCollectionData + marshalled.Length);
            dataStart += (nuint)AlignOfT - (dataStart % (nuint)AlignOfT);
            T* vectorDataBuffer = (T*)dataStart;

            for (int i = 0; i < outerElementCount; i++)
            {
                var innerCollection = (IList<T>)outerCollection[i];
                var length = innerCollection?.Count ?? 0;
                innerCollectionData->Length = length;

                if (length == 0)
                {
                    innerCollectionData->Data = null;
                }
                else
                {
                    innerCollectionData->Data = vectorDataBuffer;
                    switch (innerCollection)
                    {
                        case T[] t:
                            new Span<T>(t).CopyTo(new Span<T>(vectorDataBuffer, length));
                            vectorDataBuffer += length;
                            break;
                        case List<T> l:
                            NoAllocHelpers.CreateReadOnlySpan(l).CopyTo(new Span<T>(vectorDataBuffer, length));
                            vectorDataBuffer += length;
                            break;
                        default:
                            for (int j = 0; j < length; j++)
                                *vectorDataBuffer++ = innerCollection[j];
                            break;
                    }
                }

                innerCollectionData++;
             }

            return marshalled;
        }
    }
}
