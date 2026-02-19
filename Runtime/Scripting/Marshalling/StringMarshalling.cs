// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryMarshalEmptyOrNullString(string s, ref MarshalledArray marshalledArray)
        {
            if (s == null)
            {
                marshalledArray.data = null;
                marshalledArray.size = 0;
                marshalledArray.capacity = 0;
                marshalledArray.dataOwner = MarshalledArray.DataOwner.Null;
                return true;
            }
            if (s.Length == 0)
            {
                // It doesn't matter what we send across here, as long as it is non-null
                marshalledArray.data = (void*)(UIntPtr)1;
                marshalledArray.size = 0;
                marshalledArray.capacity = 0;
                marshalledArray.dataOwner = MarshalledArray.DataOwner.Empty;
                return true;
            }
            return false;
        }

        public static unsafe MarshalledArray AllocateString(string s)
        {
            ManagedSpanWrapper managedSpanWrapper = default;
            if (TryMarshalEmptyOrNullString(s, ref managedSpanWrapper))
                return MarshalledArray.CreateFromPinnedData(managedSpanWrapper.begin, managedSpanWrapper.length);

            void* buffer = BindingsAllocator.Malloc(s.Length * sizeof(char));
            s.AsSpan().CopyTo(new Span<char>(buffer, s.Length));
            return MarshalledArray.CreateFromNativeAllocatedData(buffer, s.Length);
        }

        public static unsafe void FreeAllocatedString(ref MarshalledArray managedSpan)
        {
            // Check for length instead of s.begin == null
            // Because we will pass a non-null, static pointer for empty strings
            if (managedSpan.dataOwner == MarshalledArray.DataOwner.TempAllocated)
                BindingsAllocator.Free(managedSpan.data);
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

        // TODO: Get rid of this hack and make how we pass strings consistent
        public static string GetStringAndDispose(MarshalledArray marshalledArray)
        {
            return GetStringAndDispose(new ManagedSpanWrapper(marshalledArray.data, marshalledArray.size));
        }

        public static void UpdateStringAndDispose(ManagedSpanWrapper inSpanWrapper, ManagedSpanWrapper outSpanWrapper, ref string outString)
        {
            // The span was not updated by native code, nothing to do
            if (inSpanWrapper.begin != outSpanWrapper.begin)
                outString = GetStringAndDispose(outSpanWrapper);
        }
    }

    [VisibleToOtherModules]
    [Il2CppEagerStaticClassConstruction]
    internal static unsafe class StringArrayMarshaller
    {
        private static readonly int sizeOfCoreString = BindingsAllocator.SizeOfCoreString();

        public static void ConvertToUnmanaged<TCollection>(in TCollection array, ref MarshalledArray marshalledArray)
            where TCollection : struct, ICollectionMarshallingAccessor<string>
        {
            MarshalledArray.Allocate<string, TCollection>(array, ref marshalledArray, sizeOfCoreString, elementCleanupRequired: false);

            var span = marshalledArray.AsSpan<byte>();
            byte* ptr = (byte*)marshalledArray.data;
            for (int i = 0; i < span.Length; i++)
            {
                BindingsAllocator.SetCoreStringBuffer(ptr, array[i]);
                ptr += sizeOfCoreString;
            }
        }

        public static void ConvertToManaged<TCollection>(in MarshalledArray marshalled, ref TCollection array)
            where TCollection : struct, ICollectionMarshallingAccessor<string>
        {
            var span = marshalled.GetDataForUnmarshal<string, byte, TCollection>(ref array);

            if (span.Length == 0)
                return;

            fixed (byte* begin = &span[0])
            {
                var ptr = begin;
                for (int i = 0; i < span.Length; i++)
                {
                    array[i] = BindingsAllocator.GetStringForCoreString(ptr);
                    ptr += sizeOfCoreString;
                }
            }
        }

        public static void Free(in MarshalledArray marshalled)
        {
            switch (marshalled.dataOwner)
            {
                case MarshalledArray.DataOwner.ExternallyOwned:
                case MarshalledArray.DataOwner.Empty:
                case MarshalledArray.DataOwner.Null:
                    break;
                case MarshalledArray.DataOwner.PinnedBuffer:
                case MarshalledArray.DataOwner.TempAllocated:
                case MarshalledArray.DataOwner.TempAllocatedCleanupRequired:
                    // The native marshaller did not free the data buffer we passed in
                    BindingsAllocator.FreeCoreStringArray(marshalled.data, marshalled.size);
                    break;
                case MarshalledArray.DataOwner.NativeOwnedMemory:
                    // The native marshaller already freed the data buffer we passed in (or it was null before)
                    // But it returned NativeOwnedMemory, so we need to free it
                    BindingsAllocator.FreeNativeOwnedMemory(marshalled.data);
                    break;
                default:
                    MarshalledArray.ThrowUnimplementedDataOwnerCase(marshalled.dataOwner);
                    break;
            }
        }
    }

    [VisibleToOtherModules]
    internal unsafe ref struct ProxyStringMarshaller
    {
        public string GetString(ManagedSpanWrapper* managedSpan)
        {
            if (managedSpan->length == 0)
            {
                _managedString = managedSpan->begin == null ? null : string.Empty;
                return _managedString;
            }

            _managedString = new string((char*)managedSpan->begin, 0, managedSpan->length);
            return _managedString;
        }

        public void Unmarshal(ManagedSpanWrapper* managedSpan, string currentString)
        {
            if(currentString == null)
            {
                if (managedSpan->length == 0)
                    return;
            }

            else if (Object.ReferenceEquals(currentString, _managedString))
                return;

            // a new core::string is allocated and we write the address to the ManagedSpanWrapper's
            // begin field. Also a -1 length is stored in ManagedSpanWrapper to signal to Native code that
            // the new core::string has been created and needs to be unmarshalled and freed
            *managedSpan = new ManagedSpanWrapper(BindingsAllocator.AllocateCoreString(currentString), -1);
        }

        string _managedString;
    }
}
