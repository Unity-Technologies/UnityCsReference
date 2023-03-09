// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    internal readonly struct ArrayHandleOnStack
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate void* CreateArrayDelegate(void* targetRef, int size);
    }

    [VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ArrayHandleOnStack<TT> where TT : unmanaged
    {
        private unsafe readonly void* _arrayRefPtr;
        private readonly IntPtr _allocArrayCallbackPtr;

        // GetFunctionPointerForDelegate can not target a generic method on CoreCLR
        // For CoreCLR we use function pointers from emitted methods instead
        static ArrayHandleOnStack.CreateArrayDelegate s_createArrayDelegate;
        static IntPtr s_createArrayFcnPtr;

        unsafe static ArrayHandleOnStack()
        {
            s_createArrayDelegate = AllocArrayManagedCallback;
            s_createArrayFcnPtr = Marshal.GetFunctionPointerForDelegate<ArrayHandleOnStack.CreateArrayDelegate>(s_createArrayDelegate);
        }

        public unsafe ArrayHandleOnStack(void* arrayRefPtr)
        {
            _arrayRefPtr = arrayRefPtr;
            _allocArrayCallbackPtr = s_createArrayFcnPtr;
        }
        public unsafe ArrayHandleOnStack(void* arrayRefPtr, IntPtr allocArrayCallbackPtr)
        {
            _arrayRefPtr = arrayRefPtr;
            _allocArrayCallbackPtr = allocArrayCallbackPtr;
        }

        [AOT.MonoPInvokeCallback(typeof(ArrayHandleOnStack.CreateArrayDelegate))]
        public static unsafe void* AllocArrayManagedCallback (void* targetRef, int size)
        {
            var retArray = new TT[size];
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.ClassAsRef<TT[]>(targetRef) = retArray;
            if(size<1)
                return null;

            // Typically it is dangerous to return fixed memory as it is not pinned after the return.
            // In this case AllocArrayManagedCallback is called in a specific scenario where assigningthe 
            // new array to targetRef will pin the array. This fixed statement simply allows us toget 
            // the address of the first element of an already pinned array.
            fixed(void* retPtr = retArray)
                return retPtr;
        }
    }
}

