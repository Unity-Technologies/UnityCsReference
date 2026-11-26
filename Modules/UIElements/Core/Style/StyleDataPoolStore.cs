// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // The native style data pools, one per StyleDataType; a block shared by several elements is counted once
    sealed class StyleDataPoolStore : IPooledNativeStore
    {
        public string storeName => "StyleData";

        public long GetBytesReserved()
        {
            var bytes = 0L;
            for (var i = 0; i < StyleDataAllocator.k_StyleDataTypeCount; ++i)
                bytes += StyleDataAllocator.GetPoolBytesReserved((StyleDataType)i);
            return bytes;
        }

        public long GetBytesUsed()
        {
            var bytes = 0L;
            for (var i = 0; i < StyleDataAllocator.k_StyleDataTypeCount; ++i)
                bytes += StyleDataAllocator.GetPoolBytesUsed((StyleDataType)i);
            return bytes;
        }

        public void ReleasePendingSlots()
        {
            // A block is freed with its last reference
        }
    }
}
