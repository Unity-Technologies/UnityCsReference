// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using Unity.Burst;

namespace Unity.Collections.LowLevel.Unsafe
{
    ///<summary>Provides a collection of low-level, unsafe utility methods for memory operations in Unity.</summary>
    ///<remarks>
    ///  <para>The <c>UnsafeUtility</c> class provides functions for direct memory manipulation. These functions are unsafe because they allow you to bypass the safety restrictions of managed code. You can perform operations such as memory allocation, deallocation, copying, and setting memory directly.
    ///
    ///Use these methods with extreme caution to avoid memory leaks, access violations, or data corruption. The <c>UnsafeUtility</c> class is intended for scenarios where performance is critical, and the overhead of managed memory safety is prohibitive.
    ///
    ///Common use cases include interaction with native code, performance-critical sections in custom collections or game systems, and low-level manipulation required by specific algorithms.</para>
    ///  <para />
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[using Unity.Collections;
    ///using Unity.Collections.LowLevel.Unsafe;
    ///using UnityEngine;
    ///
    ///public class UnsafeUtilityExample : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        unsafe
    ///        {
    ///            // Allocate a block of unmanaged memory to store 10 integers
    ///            int sizeOfInt = UnsafeUtility.SizeOf<int>();
    ///            int length = 10;
    ///            void* memoryBlock = UnsafeUtility.Malloc(sizeOfInt * length, 4, Allocator.Temp);
    ///
    ///            // Write data to the allocated memory
    ///            for (int i = 0; i < length; i++)
    ///            {
    ///                UnsafeUtility.WriteArrayElement<int>(memoryBlock, i, i * 10);
    ///            }
    ///
    ///            // Read and print data from the allocated memory
    ///            for (int i = 0; i < length; i++)
    ///            {
    ///                int value = UnsafeUtility.ReadArrayElement<int>(memoryBlock, i);
    ///                Debug.Log("Value: " + value);
    ///            }
    ///
    ///            // Free the allocated unmanaged memory
    ///            UnsafeUtility.Free(memoryBlock, Allocator.Temp);
    ///        }
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso cref="Unity.Collections.NativeArray{T}" />
    ///<seealso cref="Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility" />
    ///<seealso href="../Manual/job-system.html">Job system overview</seealso>
    [NativeHeader("ManagedKernel/Unsafe/UnsafeUtility.bindings.h")]
    [NativeHeader("Scripting/ScriptingTypeFlags.h")]
    [StaticAccessor("UnsafeUtility", StaticAccessorType.DoubleColon)]
    public static partial class UnsafeUtility
    {
        [NativeMethod(IsThreadSafe = true)]
        extern static int GetFieldOffsetInStruct(FieldInfo field);

        [NativeMethod(IsThreadSafe = true)]
        extern static int GetFieldOffsetInClass(FieldInfo field);

        ///<summary>Returns the offset of the field relative struct or class it is contained in.</summary>
        public static int GetFieldOffset(FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (field.DeclaringType.IsValueType)
                return GetFieldOffsetInStruct(field);
            else if (field.DeclaringType.IsClass)
                return GetFieldOffsetInClass(field);
            else
            {
                throw new ArgumentException("field.DeclaringType must be a struct or class");
            }
        }

        ///<summary>Pins an object in memory, ensuring it remains at a fixed memory location during garbage collection.</summary>
        ///<remarks>
        ///  <para>The <c>PinGCObjectAndGetAddress</c> method pins a managed object, preventing the garbage collector from relocating it. This is particularly useful when you need to maintain stable memory addresses while interfacing with unmanaged code or performing low-level memory operations. Ensure that you release the GC handle using <see cref="UnsafeUtility.ReleaseGCObject" /> to prevent resource leaks.</para>
        ///  <para />
        ///</remarks>
        ///<param name="target">The object to pin. This should be a managed object whose memory location needs to remain fixed during garbage collection.</param>
        ///<param name="gcHandle">The handle associated with the pinned object. Manage this handle properly to ensure it is released when no longer needed.</param>
        ///<returns>A pointer to the memory location of the pinned object. Use this pointer for direct memory access to the object's data.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class PinGCObjectBoxedStructExample : MonoBehaviour
        ///{
        ///    private struct MyStruct
        ///    {
        ///        public int value;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        // Create and box a struct
        ///        object myObject = new MyStruct { value = 20 };
        ///
        ///        // Pin the boxed struct and get the address of its memory location
        ///        unsafe
        ///        {
        ///            void* dataPtr = UnsafeUtility.PinGCObjectAndGetAddress(myObject, out ulong gcHandle);
        ///
        ///            // Move the pointer to skip the object header to get to the struct value
        ///            int objectHeaderSize = UnsafeUtility.SizeOf<long>() * 2; // Typical object header size in bytes
        ///            int* valuePtr = (int*)((byte*)dataPtr + objectHeaderSize);
        ///
        ///            // Log the initial value
        ///            Debug.Log($"Initial struct value: {*valuePtr}"); // Expected output: 20
        ///
        ///            // Modify the value through the pointer
        ///            *valuePtr = 40;
        ///
        ///            // Release the GC handle after manipulation
        ///            UnsafeUtility.ReleaseGCObject(gcHandle);
        ///        }
        ///
        ///        // Re-cast the object to a struct to verify modifications
        ///        MyStruct modifiedStruct = (MyStruct)myObject;
        ///        Debug.Log($"Modified struct value: {modifiedStruct.value}"); // Expected output: 40
        ///
        ///        // Confirm the GC handle is released
        ///        Debug.Log("GC handle released, boxed struct unpinned.");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.PinGCArrayAndGetDataAddress" />
        [Obsolete("Use GCHandle.Alloc with GCHandle.AddrOfPinnedObject instead.")]
        unsafe public static void* PinGCObjectAndGetAddress(System.Object target, out ulong gcHandle)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return PinSystemObjectAndGetAddress(target, out gcHandle);
        }

        ///<summary>Pins a garbage-collected (GC) array and returns the address of its first element, ensuring the array's memory location remains fixed.</summary>
        ///<remarks>
        ///  <para>The <c>PinGCArrayAndGetDataAddress</c> method pins a managed array in memory, preventing the garbage collector from relocating it. This is crucial when interfacing with native code or conducting low-level memory operations involving managed arrays. Use this method to obtain a stable pointer to array elements for performance-critical operations.
        ///
        ///After pinning an array, release the GC handle using <see cref="UnsafeUtility.ReleaseGCObject" /> when the array no longer needs to be pinned to avoid resource leaks.</para>
        ///  <para />
        ///</remarks>
        ///<param name="target">The managed array to pin. It should be a valid array managed by the garbage collector.</param>
        ///<param name="gcHandle">The handle to associate with the pinned object. This should be managed properly to ensure it is correctly released.</param>
        ///<returns>A pointer to the first element of the pinned array. Use this pointer for direct memory access to the array elements.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using System;
        ///
        ///public class PinArrayExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int[] managedArray = { 1, 2, 3, 4, 5 };
        ///
        ///        // Pin the managed array and get the address of its first element
        ///        unsafe
        ///        {
        ///            void* dataPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(managedArray, out ulong gcHandle);
        ///
        ///            Debug.Log($"Address of first element: {(IntPtr)dataPtr}");
        ///
        ///            // Manipulate the array data directly using the pointer
        ///            int* intPtr = (int*)dataPtr;
        ///            for (int i = 0; i < managedArray.Length; i++)
        ///            {
        ///                intPtr[i] *= 2;  // Double each element in the array
        ///            }
        ///
        ///            // Verify changes by logging the updated array
        ///            Debug.Log("Contents of managed array after manipulation:");
        ///            for (int i = 0; i < managedArray.Length; i++)
        ///            {
        ///                Debug.Log(managedArray[i]);  // Expected output: 2, 4, 6, 8, 10
        ///            }
        ///
        ///            // Release the GC handle once done
        ///            UnsafeUtility.ReleaseGCObject(gcHandle);
        ///        }
        ///
        ///        // Confirm that the GC handle is released
        ///        Debug.Log("GC handle released, managed array unpinned.");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.PinGCArrayAndGetDataAddress" />
        [Obsolete("Use GCHandle.Alloc with GCHandle.AddrOfPinnedObject instead.")]
        unsafe public static void* PinGCArrayAndGetDataAddress(System.Array target, out ulong gcHandle)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return PinSystemArrayAndGetAddress(target, out gcHandle);
        }

        [NativeMethod(IsThreadSafe = true)]
        unsafe private static extern void* PinSystemArrayAndGetAddress(System.Object target, out ulong gcHandle);

        [NativeMethod(IsThreadSafe = true)]
        unsafe private static extern void* PinSystemObjectAndGetAddress(System.Object target, out ulong gcHandle);

        ///<summary>Releases a GC handle obtained from <see cref="UnsafeUtility.PinGCObjectAndGetAddress" /> or <see cref="UnsafeUtility.PinGCArrayAndGetDataAddress" />.</summary>
        ///<remarks>
        ///  <para>The <c>ReleaseGCObject</c> method unpins an object, allowing the garbage collector to manage its memory freely. Use this method to prevent resource leaks. Ensure you release each handle after finishing direct memory operations.</para>
        ///  <para />
        ///</remarks>
        ///<param name="gcHandle">The handle associated with the pinned object.</param>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections.LowLevel.Unsafe;
        ///using UnityEngine;
        ///
        ///public class ReleaseGCObjectExample : MonoBehaviour
        ///{
        ///    private struct MyStruct
        ///    {
        ///        public int value;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        // Box a struct
        ///        object boxedStruct = new MyStruct { value = 10 };
        ///
        ///        // Pin the object and get a GC handle
        ///        unsafe
        ///        {
        ///            void* dataPtr = UnsafeUtility.PinGCObjectAndGetAddress(boxedStruct, out ulong gcHandle);
        ///
        ///            // Access and modify data
        ///            int objectHeaderSize = UnsafeUtility.SizeOf<long>() * 2;
        ///            int* valuePtr = (int*)((byte*)dataPtr + objectHeaderSize);
        ///            *valuePtr = 20;
        ///
        ///            // Release the GC handle
        ///            UnsafeUtility.ReleaseGCObject(gcHandle);
        ///        }
        ///
        ///        // Verify modification
        ///        MyStruct modifiedStruct = (MyStruct)boxedStruct;
        ///        Debug.Log($"Modified value: {modifiedStruct.value}"); // Outputs: 20
        ///
        ///        // Confirm handle release
        ///        Debug.Log("GC handle released.");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.PinGCObjectAndGetAddress" />
        ///<seealso cref="UnsafeUtility.PinGCArrayAndGetDataAddress" />
        [Obsolete("Use GCHandle.Free instead.")]
        [NativeMethod(IsThreadSafe = true)]
        unsafe public static extern void ReleaseGCObject(ulong gcHandle);

        ///<summary>Assigns an object reference to a struct or pinned class.</summary>
        ///<seealso cref="UnsafeUtility.PinGCObjectAndGetAddress" />
        [Obsolete("The garbage collector cannot track object references stored in unmanaged memory, leading to undefined behavior.")]
        unsafe public static void CopyObjectAddressToPtr(object target, void* dstPtr)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            ClassAsRef<object>(dstPtr) = target;
        }

        ///<summary>Gets whether a struct is blittable.</summary>
        ///<returns>True if struct is blittable, otherwise false.</returns>
        public static unsafe bool IsBlittable<T>() where T : struct
        {
            return IsBlittable(typeof(T));
        }

        ///<summary>Gets a list of memory leaks.</summary>
        ///<remarks>Any memory allocated before this call that hasn't already been freed, is assumed to have leaked.</remarks>
        ///<returns>The number of leaks found.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int CheckForLeaks();

        ///<summary>Tells the leak checking system to ignore any memory allocations made up to that point.</summary>
        ///<returns>The number of leaks forgiven.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int ForgiveLeaks();

        ///<summary>Gets the mode of memory leak detection.</summary>
        ///<returns>The mode of leak detection:
        ///                
        ///- 1: Disabled
        ///- 2: Enabled
        ///- 3: Enabled with callstacks</returns>
        [NativeMethod(IsThreadSafe = true)]
        [BurstAuthorizedExternalMethod]
        public static extern NativeLeakDetectionMode GetLeakDetectionMode();

        ///<summary>Set the leak detection mode.</summary>
        ///<param name="value">The mode of leak detection. 1 = disabled, 2 = enabled, or 3 = enabled with callstacks.</param>
        [NativeMethod(IsThreadSafe = true)]
        [BurstAuthorizedExternalMethod]
        public static extern void SetLeakDetectionMode(NativeLeakDetectionMode value);

        [NativeMethod(IsThreadSafe = true)]
        [BurstAuthorizedExternalMethod]
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.AIModule")]
        unsafe internal static extern int LeakRecord(IntPtr handle, LeakCategory category, int callstacksToSkip);

        [NativeMethod(IsThreadSafe = true)]
        [BurstAuthorizedExternalMethod]
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.AIModule")]
        unsafe internal static extern int LeakErase(IntPtr handle, LeakCategory category);

        ///<summary>Allocates a block of memory with specified size, alignment, and tracking information.</summary>
        ///<remarks>
        ///  <para>The <c>MallocTracked</c> method allocates memory while providing options to track memory allocations for debugging and profiling purposes. This function is similar to <see cref="UnsafeUtility.Malloc" />, but with additional tracking that helps identify memory allocations that might contribute to memory leaks or inefficiencies in performance-critical applications.
        ///
        ///Ensure that the allocated memory is properly freed with <see cref="UnsafeUtility.Free" /> when it is no longer required to avoid memory leaks. Balance the <c>callstacksToSkip</c> parameter value to focus on your debugging needs while minimizing overhead.</para>
        ///  <para>See Also: <see cref="UnsafeUtility.FreeTracked" />, <see cref="UnsafeUtility.Malloc" />, <see cref="UnsafeUtility.SetLeakDetectionMode" />.</para>
        ///</remarks>
        ///<param name="size">The size of the memory block to allocate, in bytes. Ensure the size is sufficient for the intended data storage to avoid buffer overflow.</param>
        ///<param name="alignment">Specifies the alignment of the memory block. Alignment must be a power of two to ensure efficient memory access patterns for various processor architectures.</param>
        ///<param name="allocator">The memory allocator used to manage the allocation. Choose an allocator type that matches the intended use case for the allocated memory, such as <see cref="Allocator.Temp" /> or <see cref="Allocator.Persistent" />.</param>
        ///<param name="callstacksToSkip">Specifies the number of call stack frames to skip when tracking memory. Adjust this to refine the granularity of stack traces collected for memory profiling.</param>
        ///<returns>A pointer to the allocated memory block. Manage this pointer diligently, ensuring it is freed appropriately to prevent memory leaks and accessing invalid memory.</returns>
        ///<example>
        ///  <code><![CDATA[using System.Runtime.InteropServices;
        ///using Unity.Collections;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using UnityEngine;
        ///
        ///public class MallocTrackedSimplifiedExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Enable Native Leak Detection with stack trace for debugging
        ///        UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.EnabledWithStackTrace);
        ///        
        ///        // Allocate a block of memory for a CustomNativeArray
        ///        Allocate(10, Allocator.Persistent, out CustomNativeArray<int> array);
        ///
        ///        // NOTE: Not freeing the buffer in this example; this is intentional for demonstration.
        ///        // During a Domain reload, such as entering/exiting Play Mode, a warning will appear
        ///        // if memory is not properly freed, aiding in identifying leaks.
        ///    }
        ///
        ///    [StructLayout(LayoutKind.Sequential)]
        ///    unsafe struct CustomNativeArray<T> where T : struct
        ///    {
        ///        public void* m_Buffer;
        ///        public int m_Length;
        ///        public Allocator m_AllocatorLabel;
        ///    }
        ///
        ///    static void Allocate<T>(int length, Allocator allocator, out CustomNativeArray<T> array) where T : struct
        ///    {
        ///        long totalSize = UnsafeUtility.SizeOf<T>() * length;
        ///        array = default;
        ///
        ///        unsafe
        ///        {
        ///            // Allocate memory with leak detection tracking
        ///            array.m_Buffer = UnsafeUtility.MallocTracked(totalSize, UnsafeUtility.AlignOf<T>(), allocator, 0);
        ///        }
        ///
        ///        // Set the array metadata
        ///        array.m_Length = length;
        ///        array.m_AllocatorLabel = allocator;
        ///    }
        ///}]]></code>
        ///</example>
        unsafe public static void* MallocTracked(long size, int alignment, Allocator allocator, int callstacksToSkip)
        {
            // Preserve existing public API signature for MallocTracked
            // However we do not need to have two binding functions doing exactly the same job
            return MallocTracked(size, alignment, allocator, callstacksToSkip + 1, IntPtr.Zero);
        }

        ///<summary>Allocates a block of memory with specified size, alignment, memory label and tracking information.</summary>
        ///<remarks>
        ///  <para>The <c>MallocTracked</c> method allocates memory while providing options to track memory allocations for debugging and profiling purposes. This function is similar to <see cref="UnsafeUtility.Malloc" />, but with additional tracking that helps identify memory allocations that might contribute to memory leaks or inefficiencies in performance-critical applications.
        ///
        ///Specifying a memory label helps in profiling and debugging memory usage in Unity.
        ///
        ///To avoid memory leaks, free the allocated memory with <see cref="UnsafeUtility.Free" /> when it's no longer required. Balance the <c>callstacksToSkip</c> parameter value to focus on your debugging needs while minimizing overhead.</para>
        ///  <para>See Also: <see cref="Unity.Collections.MemoryLabel" />, <see cref="UnsafeUtility.FreeTracked" />, <see cref="UnsafeUtility.Malloc" />, <see cref="UnsafeUtility.SetLeakDetectionMode" />.</para>
        ///</remarks>
        ///<param name="size">The size of the memory block to allocate, in bytes. Ensure the size is sufficient for the intended data storage to avoid buffer overflow.</param>
        ///<param name="alignment">Specifies the alignment of the memory block. Alignment must be a power of two to ensure efficient memory access patterns for various processor architectures.</param>
        ///<param name="label">The memory label to allocate under.</param>
        ///<param name="callstacksToSkip">Specifies the number of call stack frames to skip when tracking memory. Adjust this to refine the granularity of stack traces collected for memory profiling.</param>
        ///<returns>A pointer to the allocated memory block. Manage this pointer carefully to prevent memory leaks and ensure proper deallocation.</returns>
        unsafe public static void* MallocTracked(long size, int alignment, MemoryLabel label, int callstacksToSkip)
        {
            label.CheckArgument();
            return MallocTracked(size, alignment, label.allocator, callstacksToSkip + 1, label.pointer);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe internal static extern void* MallocTracked(long size, int alignment, Allocator allocator, int callstacksToSkip, IntPtr label);

        ///<summary>Free memory with leak tracking.</summary>
        ///<param name="memory">Memory pointer.</param>
        ///<param name="allocator">Allocator.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void FreeTracked(void* memory, Allocator allocator);

        ///<summary>Free memory with leak tracking, using the specified memory label.</summary>
        ///<param name="memory">Memory pointer.</param>
        ///<param name="label">The memory label that was used to allocate the memory. The memory label must match the one used during allocation to ensure correct deallocation.</param>
        unsafe public static void FreeTracked(void* memory, MemoryLabel label)
        {
            label.CheckArgument();
            FreeTracked(memory, label.allocator);
        }

        ///<summary>Allocates a block of memory of a specified size and alignment.</summary>
        ///<remarks>
        ///  <para>The <c>Malloc</c> method allocates a block of unmanaged memory. It allows developers to specify the size in bytes and the alignment of the memory block. This method is critical in performance-critical applications where precise memory control is required.
        ///
        ///The memory allocated is not initialized to zero. Ensure that you free the allocated memory with <see cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free" /> when it is no longer needed.</para>
        ///  <para />
        ///</remarks>
        ///<param name="size">The number of bytes to allocate. Ensure this size matches the memory required for your data.</param>
        ///<param name="alignment">The alignment for the allocated memory block.</param>
        ///<param name="allocator">The allocator type, such as Allocator.Temp or Allocator.Persistent, indicating how the memory is managed.</param>
        ///<returns>A pointer to the allocated memory block. Manage this pointer carefully to prevent memory leaks and ensure proper deallocation.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MallocExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Specify the number of elements
        ///        int numElements = 10;
        ///
        ///        // Allocate memory for an array of integers
        ///        unsafe
        ///        {
        ///            int* array = (int*)UnsafeUtility.Malloc(numElements * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
        ///
        ///            // Initialize the array with some values
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                array[i] = i * 2;
        ///            }
        ///
        ///            // Output the contents of the array
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                Debug.Log(array[i]);  // Expected output: 0, 2, 4, 6, ..., 18
        ///            }
        ///
        ///            // Free the allocated memory
        ///            UnsafeUtility.Free(array, Allocator.Temp);
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MallocTracked" />
        ///<seealso cref="UnsafeUtility.FreeTracked" />
        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return Malloc(size, alignment, allocator, IntPtr.Zero);
        }

        ///<summary>Allocates a block of memory of a specified size and alignment, using the specified memory label.</summary>
        ///<remarks>
        ///  <para>The <c>Malloc</c> method allocates a block of unmanaged memory. It allows developers to specify the size in bytes and the alignment of the memory block. This method is critical in performance-critical applications where precise memory control is required.
        ///
        ///Specifying a memory label helps in profiling and debugging memory usage in Unity.
        ///
        ///The memory allocated is not initialized to zero. Ensure that you free the allocated memory with <see cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free" /> when it is no longer needed.</para>
        ///  <para />
        ///</remarks>
        ///<param name="size">The number of bytes to allocate. Ensure this size matches the memory required for your data.</param>
        ///<param name="alignment">The alignment for the allocated memory block.</param>
        ///<param name="label">The memory label to allocate under.</param>
        ///<returns>A pointer to the allocated memory block. Manage this pointer carefully to prevent memory leaks and ensure proper deallocation.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MallocMemoryLabelExample : MonoBehaviour
        ///{
        ///    static readonly MemoryLabel myMemoryLabel = new MemoryLabel("MyArea", "MyObject");
        ///
        ///    void Start()
        ///    {
        ///        // Specify the number of elements
        ///        int numElements = 10;
        ///
        ///        // Allocate memory for an array of integers using label
        ///        unsafe
        ///        {
        ///            int* array = (int*)UnsafeUtility.Malloc(numElements * sizeof(int), UnsafeUtility.AlignOf<int>(), myMemoryLabel);
        ///
        ///            // Initialize the array with some values
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                array[i] = i * 2;
        ///            }
        ///
        ///            // Output the contents of the array
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                Debug.Log(array[i]);  // Expected output: 0, 2, 4, 6, ..., 18
        ///            }
        ///
        ///            // Free the allocated memory using label
        ///            UnsafeUtility.Free(array, myMemoryLabel);
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Unity.Collections.MemoryLabel" />
        ///<seealso cref="UnsafeUtility.MallocTracked" />
        ///<seealso cref="UnsafeUtility.FreeTracked" />
        unsafe public static void* Malloc(long size, int alignment, MemoryLabel label)
        {
            label.CheckArgument();
            return Malloc(size, alignment, label.allocator, label.pointer);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe static extern void* Malloc(long size, int alignment, Allocator allocator, IntPtr label);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static unsafe void* Realloc(void* memory, long size, int alignment, MemoryLabel label)
        {
            label.CheckArgument();
            return Realloc(memory, size, alignment, label.allocator, label.pointer);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void* Realloc(void* memory, long size, int alignment, Allocator allocator, IntPtr label);

        ///<summary>Frees a block of memory that was previously allocated.</summary>
        ///<remarks>
        ///  <para>The <c>Free</c> method deallocates memory that was allocated using <see cref="UnsafeUtility.Malloc" />. This is essential to prevent memory leaks, which can lead to reduced application performance or system instability. Always ensure that any allocated memory is freed when it is no longer needed.
        ///
        ///Exercise caution when using this method. Improper deallocation of memory can result in undefined behavior and application crashes. The allocator used in this function should match the one used in the corresponding allocation.</para>
        ///  <para />
        ///</remarks>
        ///<param name="memory">A pointer to the block of memory you want to free. Ensure this pointer is valid and was previously allocated using UnsafeUtility.Malloc to avoid undefined behavior.</param>
        ///<param name="allocator">The allocator type that was originally used to allocate the memory. It is crucial that the allocator matches the one used during allocation to ensure correct deallocation.</param>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using UnityEngine;
        ///
        ///public class UnsafeUtilityFreeExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        unsafe
        ///        {
        ///            // Allocate a block of memory for 10 integers
        ///            int sizeOfInt = UnsafeUtility.SizeOf<int>();
        ///            void* memoryBlock = UnsafeUtility.Malloc(sizeOfInt * 10, 4, Allocator.Temp);
        ///
        ///            // Ensure memory is freed when no longer needed
        ///            UnsafeUtility.Free(memoryBlock, Allocator.Temp);
        ///            Debug.Log("Memory block freed.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void Free(void* memory, Allocator allocator);

        ///<summary>Frees a block of memory that was previously allocated, using the specified memory label.</summary>
        ///<remarks>
        ///  <para>The <c>Free</c> method deallocates memory that was allocated using <see cref="UnsafeUtility.Malloc" />. This is essential to prevent memory leaks, which can lead to reduced application performance or system instability. Always ensure that any allocated memory is freed when it is no longer needed.
        ///
        ///Exercise caution when using this method. Improper deallocation of memory can result in undefined behavior and application crashes. The memory label used in this function must match the one used in the corresponding allocation.</para>
        ///  <para />
        ///</remarks>
        ///<param name="memory">A pointer to the block of memory you want to free. Ensure this pointer is valid and was previously allocated using UnsafeUtility.Malloc to avoid undefined behavior.</param>
        ///<param name="label">The memory label that was used to allocate the memory. The memory label should match the one used during allocation to ensure correct deallocation.</param>
        ///<seealso cref="Unity.Collections.MemoryLabel" />
        ///<seealso cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc" />
        unsafe public static void Free(void* memory, MemoryLabel label)
        {
            label.CheckArgument();
            Free(memory, label.allocator);
        }

        ///<summary>Returns true if the allocator label is valid and can be used to allocate or deallocate memory.</summary>
        public static bool IsValidAllocator(Allocator allocator) { return allocator > Allocator.None; }


        ///<summary>Copies a specified number of bytes from a source memory location to a destination memory location.</summary>
        ///<remarks>
        ///  <para>The <c>MemCpy</c> method efficiently copies data from one memory location to another. It is ideal for quickly duplicating memory content in performance-critical applications. Note that if the source and destination memory regions overlap, the result is not guaranteed to be correct. In such cases, use <see cref="UnsafeUtility.MemMove" /> to handle overlapping data safely.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the destination memory block. Ensure this block is large enough to store the data being copied.</param>
        ///<param name="source">A pointer to the source memory block. This block should contain the data you want to copy.</param>
        ///<param name="size">The number of bytes to copy from the source to the destination.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemCpyExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int numElements = 5;
        ///        int size = numElements * sizeof(float);
        ///
        ///        // Use stackalloc to allocate memory for source and destination buffers
        ///        unsafe
        ///        {
        ///            float* srcBuffer = stackalloc float[numElements];
        ///            float* dstBuffer = stackalloc float[numElements];
        ///
        ///            // Initialize the source buffer with data
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                srcBuffer[i] = i * 1.5f;  // Fills srcBuffer with values 0.0, 1.5, 3.0, 4.5, 6.0
        ///            }
        ///
        ///            // Use MemCpy to copy data from source to destination
        ///            UnsafeUtility.MemCpy(dstBuffer, srcBuffer, size);
        ///
        ///            // Output the contents of the destination buffer to verify the copy
        ///            Debug.Log("Contents of destination buffer after MemCpy:");
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                Debug.Log(dstBuffer[i]);  // Expected output: 0.0, 1.5, 3.0, 4.5, 6.0
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemMove" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemCpy(void* destination, void* source, long size);

        ///<summary>Copies memory from a source to a destination and replicates it multiple times.</summary>
        ///<remarks>
        ///  <para>The <c>MemCpyReplicate</c> method copies a specified block of memory from a source location and replicates that block multiple times into a destination location. This is useful for quickly initializing a large buffer with repeated values, such as setting up constant buffers or initializing large arrays with repeated patterns.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the destination memory block. Ensure this block has enough space to hold the replicated data.</param>
        ///<param name="source">A pointer to the source memory block. This block should contain the data you want to replicate.</param>
        ///<param name="size">The size, in bytes, of the data block to copy and replicate. This specifies the number of bytes that are replicated each time.</param>
        ///<param name="count">The number of times to replicate the source data block in the destination buffer.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemCpyReplicateExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int elementCount = 3;
        ///        int replicateCount = 4;
        ///        int dataSize = elementCount * sizeof(float);
        ///
        ///        // Use stackalloc to allocate memory for source and destination buffers
        ///        unsafe
        ///        {
        ///            float* srcBuffer = stackalloc float[elementCount];
        ///            float* dstBuffer = stackalloc float[elementCount * replicateCount];
        ///
        ///            // Initialize the source buffer with multiple values
        ///            srcBuffer[0] = 1.1f;
        ///            srcBuffer[1] = 2.2f;
        ///            srcBuffer[2] = 3.3f;
        ///
        ///            // Use MemCpyReplicate to copy and replicate the source values into the destination
        ///            UnsafeUtility.MemCpyReplicate(dstBuffer, srcBuffer, dataSize, replicateCount);
        ///
        ///            // Output the contents of the destination buffer to verify replication
        ///            Debug.Log("Contents of destination buffer after MemCpyReplicate:");
        ///            for (int i = 0; i < elementCount * replicateCount; i++)
        ///            {
        ///                Debug.Log(dstBuffer[i]);  // Expected pattern: 1.1, 2.2, 3.3 repeated
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemCpy" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemCpyReplicate(void* destination, void* source, int size, int count);

        ///<summary>Copies data between memory blocks with specified strides, allowing skipped bytes in both source and destination.</summary>
        ///<remarks>
        ///  <para>The <c>MemCpyStride</c> method is similar to <see cref="UnsafeUtility.MemCpy" />, but it supports variable strides for both source and destination memory blocks. This functionality is particularly useful for dealing with non-contiguous memory layouts, such as those found in structures with padding or interleaved data. It allows efficient data transfer when direct element alignment is not feasible.
        ///
        ///Ensure that both source and destination pointers are valid and that their memory regions do not incorrectly overlap, which would lead to undefined behavior. This method does not perform bounds checking, so it is crucial to ensure there is sufficient space for the data transfer.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the start of the destination memory block. The memory block should accommodate the data according to the destination stride and element size requirements.</param>
        ///<param name="destinationStride">The stride in bytes between consecutive elements in the destination memory block. This stride facilitates skipping bytes.</param>
        ///<param name="source">A pointer to the start of the source memory block, containing the data to be copied. This block must be sized based on the source stride and count.</param>
        ///<param name="sourceStride">The stride in bytes between consecutive elements in the source memory block. This allows for correct data retrieval from interleaved or padded layouts.</param>
        ///<param name="elementSize">The size in bytes of each individual element to be copied. Ensure consistency in element size between source and destination for accurate copying.</param>
        ///<param name="count">The number of elements to transfer from the source block to the destination block. Both source and destination must have space for this element count.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using System.Runtime.InteropServices;
        ///
        ///public class MemCpyStrideExample : MonoBehaviour
        ///{
        ///    [StructLayout(LayoutKind.Sequential)]
        ///    struct VertexData
        ///    {
        ///        public float x;
        ///        public float y;
        ///        public float z;
        ///        public int id;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        // Create source NativeArray of VertexData with padding
        ///        NativeArray<VertexData> vertexArray = new NativeArray<VertexData>(5, Allocator.Temp);
        ///        // Create destination NativeArray to hold just the IDs, without extra floats
        ///        NativeArray<int> idArray = new NativeArray<int>(5, Allocator.Temp);
        ///
        ///        // Fill the vertex array with sample vertex data
        ///        for (int i = 0; i < vertexArray.Length; i++)
        ///        {
        ///            vertexArray[i] = new VertexData { x = i, y = i, z = i, id = i + 100 };
        ///        }
        ///
        ///        // Use UnsafeUtility.MemCpyStride to copy only the IDs from the padded structure
        ///        unsafe
        ///        {
        ///            void* vertexPtr = vertexArray.GetUnsafeReadOnlyPtr();
        ///            void* idPtr = idArray.GetUnsafePtr();
        ///
        ///            int elementSize = UnsafeUtility.SizeOf<int>();  // Size of ID element
        ///            int srcStride = UnsafeUtility.SizeOf<VertexData>();  // Complete size of VertexData struct
        ///            int idOffset = UnsafeUtility.AlignOf<float>() * 3;  // Offset bytes past the floats to the ID
        ///
        ///            UnsafeUtility.MemCpyStride(idPtr, elementSize, (byte*)vertexPtr + idOffset, srcStride, elementSize, vertexArray.Length);
        ///        }
        ///
        ///        // Log the copied IDs from the idArray to verify the result
        ///        for (int i = 0; i < idArray.Length; i++)
        ///        {
        ///            Debug.Log($"ID: {idArray[i]}"); // Expected output: 100, 101, 102, 103, 104
        ///        }
        ///
        ///        // Clean up resources
        ///        vertexArray.Dispose();
        ///        idArray.Dispose();
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.SizeOf" />
        ///<seealso cref="UnsafeUtility.MemCpy" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemCpyStride(void* destination, int destinationStride, void* source, int sourceStride, int elementSize, int count);

        ///<summary>Copies a specified number of bytes from a source memory location to a destination, allowing overlapping regions.</summary>
        ///<remarks>
        ///  <para>The <c>MemMove</c> method is designed to transfer a block of memory from one location to another. Unlike standard memory copy operations, MemMove can safely handle overlapping memory regions by ensuring that data is transferred correctly even when source and destination areas overlap.
        ///
        ///This function is essential in scenarios where memory regions may overlap, such as moving elements within an array or buffer. Be cautious when using it to ensure both pointers are valid and the destination block has enough space to accommodate the size of the memory being moved.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the start of the destination memory block. Ensure this block is large enough to hold the specified number of bytes to prevent overflow.</param>
        ///<param name="source">A pointer to the start of the source memory block. This block contains the data to be moved and must be valid for the entire size of the move operation.</param>
        ///<param name="size">The number of bytes to transfer from the source to the destination. This value must be positive and less than or equal to the sizes of both memory blocks.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemMoveOverlapExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int numElements = 10;
        ///
        ///        // Allocate a buffer using stackalloc with space for 20 elements
        ///        unsafe
        ///        {
        ///            byte* buffer = stackalloc byte[numElements * 2];
        ///
        ///            // Initialize the buffer with initial values
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                buffer[i] = (byte)(i + 1);  // Fills buffer with values 1 to 10
        ///            }
        ///
        ///            Debug.Log("Buffer before MemMove:");
        ///            for (int i = 0; i < numElements * 2; i++)
        ///            {
        ///                Debug.Log(buffer[i]);
        ///            }
        ///
        ///            // Perform a MemMove operation to simulate overlapping memory move
        ///            // Move the first 10 bytes (1 to 10) to the next starting position (5)
        ///            UnsafeUtility.MemMove(buffer + 5, buffer, numElements);
        ///
        ///            Debug.Log("Buffer after MemMove:");
        ///            for (int i = 0; i < numElements * 2; i++)
        ///            {
        ///                Debug.Log(buffer[i]);
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemCpy" />
        ///<seealso cref="UnsafeUtility.MemCpyStride" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemMove(void* destination, void* source, long size);

        ///<summary>Swap the content of two memory buffers of the same size.</summary>
        ///<remarks>
        ///  <para>The <c>MemSwap</c> method exchanges the data between two memory buffers. Each buffer must have the same size for the swap to be valid. This method is efficient for reorganizing data or implementing custom shuffle operations.
        ///
        ///When the memory regions overlap, the data from the first buffer is copied to the second, ensuring no data corruption. Be cautious to provide correct pointers and sizes to avoid unintended behavior.</para>
        ///  <para />
        ///</remarks>
        ///<param name="ptr1">A pointer to the first memory buffer. This buffer will be swapped with the second one.</param>
        ///<param name="ptr2">A pointer to the second memory buffer to swap with the first. Ensure both buffers are correctly allocated and are of equal size.</param>
        ///<param name="size">The size, in bytes, of the data each buffer holds. This is the number of bytes that will be swapped between the two buffers.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemSwapExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int numElements = 3;
        ///        int size = numElements * sizeof(float);
        ///
        ///        // Use stackalloc to allocate memory for two buffers
        ///        unsafe
        ///        {
        ///            float* buffer1 = stackalloc float[numElements];
        ///            float* buffer2 = stackalloc float[numElements];
        ///
        ///            // Initialize buffer1 and buffer2 with distinct values
        ///            buffer1[0] = 1.1f; buffer1[1] = 2.2f; buffer1[2] = 3.3f;
        ///            buffer2[0] = 4.4f; buffer2[1] = 5.5f; buffer2[2] = 6.6f;
        ///
        ///            // Output initial contents of the buffers
        ///            Debug.Log("Before MemSwap:");
        ///            Debug.Log($"Buffer1: {buffer1[0]}, {buffer1[1]}, {buffer1[2]}");
        ///            Debug.Log($"Buffer2: {buffer2[0]}, {buffer2[1]}, {buffer2[2]}");
        ///
        ///            // Swap the contents of buffer1 and buffer2
        ///            UnsafeUtility.MemSwap(buffer1, buffer2, size);
        ///
        ///            // Output swapped contents of the buffers
        ///            Debug.Log("After MemSwap:");
        ///            Debug.Log($"Buffer1: {buffer1[0]}, {buffer1[1]}, {buffer1[2]}");  // Expected: 4.4, 5.5, 6.6
        ///            Debug.Log($"Buffer2: {buffer2[0]}, {buffer2[1]}, {buffer2[2]}");  // Expected: 1.1, 2.2, 3.3
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemCpy" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemSwap(void* ptr1, void* ptr2, long size);

        ///<summary>Sets a block of memory to a specified byte value for a defined size.</summary>
        ///<remarks>
        ///  <para>The <c>MemSet</c> method fills a specified block of memory with a given byte value. This operation is useful for initializing memory to a known state, such as setting a buffer to zero or another specific value. Properly initializing memory can prevent data leaks and ensure predictable behavior in memory management.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the start of the memory block that you want to fill. Ensure that the memory region has space equal to or greater than the specified size to avoid overflow.</param>
        ///<param name="value">The byte value used to fill the memory block. This value is duplicated throughout the entire specified size of the block.</param>
        ///<param name="size">The number of bytes to set in the memory block. Ensure that this size does not exceed the capacity of the destination buffer to prevent memory corruption.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemSetExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int bufferSize = 10;
        ///
        ///        // Allocate a buffer using stackalloc
        ///        unsafe
        ///        {
        ///            byte* buffer = stackalloc byte[bufferSize];
        ///
        ///            // Use MemSet to initialize buffer with the value 0xFF
        ///            UnsafeUtility.MemSet(buffer, 0xFF, bufferSize);
        ///
        ///            // Output the initialized buffer to verify the result
        ///            Debug.Log("Buffer contents after MemSet:");
        ///            for (int i = 0; i < bufferSize; i++)
        ///            {
        ///                Debug.Log(buffer[i]);  // Expected output: 255 (0xFF) for each element
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemCpy" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern void MemSet(void* destination, byte value, long size);

        ///<summary>Clears a block of memory, setting all bytes to zero.</summary>
        ///<remarks>
        ///  <para>The <c>MemClear</c> method sets all bytes in a specified memory block to zero. This is useful for initializing memory without any residual data that might cause unexpected behavior. Use this method to ensure memory safety when reusing or reallocating memory blocks.
        ///
        ///This method provides a fast and efficient way to clear data, which is important in scenarios where performance and reliability are paramount. It is often used when you need to reset arrays or buffers before further processing.</para>
        ///  <para />
        ///</remarks>
        ///<param name="destination">A pointer to the start of the memory block you want to clear. Ensure this pointer is valid and that the memory region is correctly allocated.</param>
        ///<param name="size">The number of bytes to clear in the memory block. Ensure this size does not exceed the allocated memory to avoid data corruption.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemClearExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int bufferSize = 10;
        ///
        ///        // Allocate a buffer using stackalloc
        ///        unsafe
        ///        {
        ///            byte* buffer = stackalloc byte[bufferSize];
        ///
        ///            // Initialize the buffer with some data
        ///            for (int i = 0; i < bufferSize; i++)
        ///            {
        ///                buffer[i] = (byte)(i + 1);
        ///            }
        ///
        ///            // Output the buffer contents before clearing
        ///            Debug.Log("Buffer contents before MemClear:");
        ///            for (int i = 0; i < bufferSize; i++)
        ///            {
        ///                Debug.Log(buffer[i]);
        ///            }
        ///
        ///            // Clear the buffer using MemClear
        ///            UnsafeUtility.MemClear(buffer, bufferSize);
        ///
        ///            // Output the buffer contents after clearing
        ///            Debug.Log("Buffer contents after MemClear:");
        ///            for (int i = 0; i < bufferSize; i++)
        ///            {
        ///                Debug.Log(buffer[i]);  // Expected output: 0 for each element
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemSet" />
        unsafe public static void MemClear(void* destination, long size)
        {
            MemSet(destination, 0, size);
        }

        ///<summary>Checks whether two memory regions are identical.</summary>
        ///<remarks>
        ///  <para>The <c>MemCmp</c> method compares two memory regions to determine if they contain identical data. It returns zero if the memory contents are the same and a non-zero value if they differ. This method can be used to compare data blocks efficiently in performance-critical applications.</para>
        ///  <para />
        ///</remarks>
        ///<param name="ptr1">A pointer to the first memory buffer. This buffer contains the first block of data to compare.</param>
        ///<param name="ptr2">A pointer to the second memory buffer to compare against the first. Ensure this buffer is the same size as the first for an accurate comparison.</param>
        ///<param name="size">The number of bytes to compare between the two memory regions.</param>
        ///<returns>Returns 0 if the contents of the two memory regions are identical; returns a non-zero value if they differ.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class MemCmpExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        int numElements = 5;
        ///        int size = numElements * sizeof(int);
        ///    
        ///        unsafe
        ///        {
        ///            // Use stackalloc to allocate memory for three buffers
        ///            int* buffer1 = stackalloc int[numElements];
        ///            int* buffer2 = stackalloc int[numElements];
        ///            int* buffer3 = stackalloc int[numElements];
        ///
        ///            // Initialize buffer1 with data
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                buffer1[i] = i * 10;  // 0, 10, 20, 30, 40
        ///            }
        ///
        ///            // Copy contents from buffer1 to buffer2
        ///            UnsafeUtility.MemCpy(buffer2, buffer1, size);
        ///
        ///            // Modify buffer3 differently
        ///            for (int i = 0; i < numElements; i++)
        ///            {
        ///                buffer3[i] = buffer1[i] + 1;  // 1, 11, 21, 31, 41
        ///            }
        ///
        ///            // Compare buffer1 and buffer2
        ///            int result1 = UnsafeUtility.MemCmp(buffer1, buffer2, size);
        ///            Debug.Log($"Comparing buffer1 and buffer2, Result: {result1}");  // Expected output: 0 (identical)
        ///
        ///            // Compare buffer1 and buffer3
        ///            int result2 = UnsafeUtility.MemCmp(buffer1, buffer3, size);
        ///            Debug.Log($"Comparing buffer1 and buffer3, Result: {result2}");  // Expected output: non-zero (different)
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MemSet" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        unsafe public static extern int MemCmp(void* ptr1, void* ptr2, long size);

        ///<summary>Determines the size, in bytes, of a specified type, including padding for alignment.</summary>
        ///<remarks>
        ///  <para>The <c>SizeOf</c> method calculates the number of bytes needed to store a single instance of a provided type. This includes any padding necessary for proper memory alignment, which ensures efficient data access and hardware compliance.
        ///
        ///Use this method when manually allocating memory or interfacing with lower-level systems in Unity. It is essential for tasks that require precise control over memory use, such as custom data structures or interaction with native plugins.</para>
        ///  <para />
        ///</remarks>
        ///<param name="type">The type whose byte size is to be determined.</param>
        ///<returns>The total size in bytes of the specified type, including any alignment padding required for efficient memory access.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using System.Runtime.InteropServices;
        ///using Unity.Collections;
        ///
        ///public class SizeOfExample : MonoBehaviour
        ///{
        ///    [StructLayout(LayoutKind.Sequential)]
        ///    struct ComplexStruct
        ///    {
        ///        public byte SmallValue;
        ///        public double LargeValue;
        ///        public short MediumValue;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        // Calculate and log the size of ComplexStruct
        ///        int structSize = UnsafeUtility.SizeOf<ComplexStruct>();
        ///        Debug.Log($"Size of ComplexStruct: {structSize} bytes");
        ///
        ///        // Using SizeOf to allocate memory for an array of ComplexStruct
        ///        unsafe
        ///        {
        ///            void* structBuffer = UnsafeUtility.Malloc(structSize * 5, UnsafeUtility.AlignOf<ComplexStruct>(), Allocator.Temp);
        ///
        ///            // Cast the buffer to work with the ComplexStruct type
        ///            ComplexStruct* structArray = (ComplexStruct*)structBuffer;
        ///            for (int i = 0; i < 5; i++)
        ///            {
        ///                structArray[i].SmallValue = (byte)i;
        ///                structArray[i].LargeValue = i * 2.2;
        ///                structArray[i].MediumValue = (short)(i * 10);
        ///                Debug.Log($"Struct[{i}]: SmallValue = {structArray[i].SmallValue}, LargeValue = {structArray[i].LargeValue}, MediumValue = {structArray[i].MediumValue}");
        ///            }
        ///
        ///            // Free the allocated memory
        ///            UnsafeUtility.Free(structBuffer, Allocator.Temp);
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.MallocTracked" />
        ///<seealso cref="UnsafeUtility.AlignOf" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern int SizeOf(Type type);

        ///<summary>Gets whether a struct is blittable.</summary>
        ///<param name="type">The <c>System.Type</c> of a struct.</param>
        ///<returns>True if struct is blittable, otherwise false.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsBlittable(Type type);

        ///<summary>Checks whether the struct or type is unmanaged.</summary>
        ///<remarks>An unmanaged type contains no managed fields, and can be freely copied in memory.</remarks>
        ///<param name="type">The <c>System.Type</c> of a struct.</param>
        ///<returns>True if <c>type</c> is unmanaged, otherwise false.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsUnmanaged(Type type);

        ///<summary>Checks whether the type is acceptable as an element type in a native container.</summary>
        ///<param name="type">The <c>System.Type</c> to check.</param>
        ///<returns>True if type is acceptable as a native container element.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsValidNativeContainerElementType(Type type);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetScriptingTypeFlags(Type type);

        // @TODO : This is probably not the ideal place to have this?
        [NativeMethod(IsThreadSafe = true)]
        internal static extern void LogError(string msg, string filename, int linenumber);
    }
}
