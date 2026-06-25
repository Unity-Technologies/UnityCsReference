// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.LowLevel.Unsafe
{
    internal static class BurstUnsafeUtility
    {
        public unsafe static void MemCpy (void* destination, void* source, long size)
        {
            byte* dst = (byte*)destination;
            for (long i = 0; i < size; i++)
            {
                dst[i] = ((byte*)source)[i];
            }
        }
    }
}
