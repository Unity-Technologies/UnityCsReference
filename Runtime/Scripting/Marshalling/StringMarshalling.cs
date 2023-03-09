// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class StringMarshaller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryMarshalEmptyOrNullString(string s, ref ManagedSpanWrapper managedSpanWrapper)
        {
            if (s == null)
            {
                managedSpanWrapper = default;
                return true;
            }
            if (s.Length == 0)
            {
                // It doesn't matter what we send across here, as long as it is non-null
                managedSpanWrapper = new ManagedSpanWrapper((void*)(UIntPtr)1, 0);
                return true;
            }
            return false;
        }
    }

    [VisibleToOtherModules]
    internal unsafe ref struct OutStringMarshaller 
    {
        public static string GetStringAndDispose(ManagedSpanWrapper managedSpan)
        {
            if (managedSpan.length == 0)
            {
                // null and 0 length strings are not allocated, no need to free
                return managedSpan.begin == null ? null : string.Empty;
            }

            var outString = new string((char*)managedSpan.begin, 0, managedSpan.length);
            BindingsAllocator.Free(managedSpan.begin);
            return outString;
        }

        public static void UpdateStringAndDispose(ManagedSpanWrapper inSpanWrapper, ManagedSpanWrapper outSpanWrapper, ref string outString)
        {
            // The span was not updated by native code, nothing to do
            if (inSpanWrapper.begin != outSpanWrapper.begin)
                outString = GetStringAndDispose(outSpanWrapper);
        }
    }
}
