// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    internal static class CalculateReaderWriterDependency
    {
        public static bool Add(ComponentType type, ref UnsafeList<TypeIndex> reading, ref UnsafeList<TypeIndex> writing)
        {
            Assert.IsFalse(type == ComponentType.ReadWrite<Entity>());

            if (type.IsZeroSized && !type.IsEnableable)
                return false;
            if (type.AccessModeType == ComponentType.AccessMode.ReadOnly)
                return AddReaderTypeIndex(type.TypeIndex, ref reading, ref writing);
            else
                return AddWriterTypeIndex(type.TypeIndex, ref reading, ref writing);
        }

        public static bool AddReaderTypeIndex(TypeIndex typeIndex, ref UnsafeList<TypeIndex> reading, ref UnsafeList<TypeIndex> writing)
        {
            if (reading.Contains(typeIndex))
                return false;
            if (writing.Contains(typeIndex))
                return false;

            reading.Add(typeIndex);
            return true;
        }

        public static bool AddWriterTypeIndex(TypeIndex typeIndex, ref UnsafeList<TypeIndex> reading, ref UnsafeList<TypeIndex> writing)
        {
            if (writing.Contains(typeIndex))
                return false;

            var readingIndex = reading.IndexOf(typeIndex);
            if (readingIndex != -1)
                reading.RemoveAtSwapBack(readingIndex);

            writing.Add(typeIndex);
            return true;
        }
    }
}
