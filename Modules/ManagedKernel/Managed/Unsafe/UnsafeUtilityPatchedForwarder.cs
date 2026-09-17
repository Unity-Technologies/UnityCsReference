// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeUtility
    {
        // Copies sizeof(T) bytes from ptr to output
        ///<summary>Copies sizeof(T) bytes from a memory pointer to a struct.</summary>
        ///<param name="ptr">The memory pointer to copy from.</param>
        ///<param name="output">A struct to copy data to.</param>
        [MethodImpl(256)] // AggressiveInlining
        unsafe public static void CopyPtrToStructure<T>(void* ptr, out T output) where T : struct
        {
	    UnsafeUtilityInternal.CopyPtrToStructure<T>(ptr, out output);
        }

        // Copies sizeof(T) bytes from output to ptr
        ///<summary>Copies sizeof(T) bytes from a memory pointer to a struct.</summary>
        ///<param name="ptr">The memory pointer to copy from.</param>
        ///<param name="input">A struct to copy data to.</param>
        [MethodImpl(256)] // AggressiveInlining
        unsafe public static void CopyStructureToPtr<T>(ref T input, void* ptr) where T : struct
        {
            UnsafeUtilityInternal.CopyStructureToPtr<T>(ref input, ptr);
        }


        ///<summary>Read array element.</summary>
        ///<param name="source">Memory pointer.</param>
        ///<param name="index">Array index.</param>
        ///<returns>Array Element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static T ReadArrayElement<T>(void* source, int index)
        {
	    return UnsafeUtilityInternal.ReadArrayElement<T>(source, index);
        }

        ///<summary>Read array element with stride.</summary>
        ///<param name="source">Memory pointer.</param>
        ///<param name="index">Array index.</param>
        ///<param name="stride">Stride.</param>
        ///<returns>Array element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static T ReadArrayElementWithStride<T>(void* source, int index, int stride)
        {
	    return UnsafeUtilityInternal.ReadArrayElementWithStride<T>(source, index, stride);
        }

        ///<summary>Write array element.</summary>
        ///<param name="destination">Memory pointer.</param>
        ///<param name="index">Array index.</param>
        ///<param name="value">Value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void WriteArrayElement<T>(void* destination, int index, T value)
        {
            UnsafeUtilityInternal.WriteArrayElement<T>(destination, index, value);
        }

        ///<summary>Write array element with stride.</summary>
        ///<param name="destination">Memory pointer.</param>
        ///<param name="index">Array index.</param>
        ///<param name="stride">Stride.</param>
        ///<param name="value">Value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void WriteArrayElementWithStride<T>(void* destination, int index, int stride, T value)
        {
            UnsafeUtilityInternal.WriteArrayElementWithStride<T>(destination, index, stride, value);
        }

        // The address of the memory where the struct resides in memory
        ///<summary>Obtains the memory address of the specified object as a pointer.</summary>
        ///<remarks>
        ///  <para>The <c>AddressOf</c> method retrieves the memory address of a managed object. Use this in scenarios that need direct memory access or interaction with unmanaged code, where efficiency is crucial.
        ///
        ///Exercise caution when you use this method. Improper handling of memory addresses may cause memory corruption or application instability. Use this method to enhance performance in areas where managed memory safety has significant overhead.</para>
        ///  <para />
        ///</remarks>
        ///<param name="output">The managed object for which you need the memory address. Ensure the object remains valid and exists throughout the use of its address.</param>
        ///<returns>A void pointer that represents the memory address of the object. This pointer allows direct access to the memory location. Manage this pointer carefully to prevent runtime errors.</returns>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections.LowLevel.Unsafe;
        ///using UnityEngine;
        ///
        ///public class AddressOfExample : MonoBehaviour
        ///{
        ///    struct ExampleStruct
        ///    {
        ///        public int Number;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        ExampleStruct example = new ExampleStruct { Number = 100 };
        ///        unsafe
        ///        {
        ///            // Get the memory address of the struct
        ///            void* address = UnsafeUtility.AddressOf(ref example);
        ///
        ///            // Modify the struct's value directly through the pointer
        ///            ExampleStruct* pointer = (ExampleStruct*)address;
        ///            pointer->Number = 200;
        ///
        ///            Debug.Log($"Modified value: {example.Number}");  // Outputs: Modified value: 200
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void* AddressOf<T>(ref T output) where T : struct
        {
            return UnsafeUtilityInternal.AddressOf<T>(ref output);
        }

        // The size of a struct
        ///<summary>Determines the size, in bytes, of a specified type, including padding for alignment.</summary>
        ///<remarks>
        ///  <para>The <c>SizeOf</c> method calculates the number of bytes needed to store a single instance of a provided type. This includes any padding necessary for proper memory alignment, which ensures efficient data access and hardware compliance.
        ///
        ///Use this method when manually allocating memory or interfacing with lower-level systems in Unity. It is essential for tasks that require precise control over memory use, such as custom data structures or interaction with native plugins.</para>
        ///  <para />
        ///</remarks>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct
        {
            return UnsafeUtilityInternal.SizeOf<T>();
        }


        // minimum alignment of a struct
        ///<summary>Retrieves the minimum memory alignment requirement for a specified struct type.</summary>
        ///<remarks>
        ///  <para>The <c>AlignOf</c> method calculates the minimum alignment required for a struct type in memory. This is crucial for ensuring the proper alignment of data structures, which can improve access speed and comply with hardware requirements.
        ///
        ///Proper alignment can prevent performance penalties by avoiding misaligned data access, which might require multiple memory accesses. This function is particularly useful when you're implementing custom memory allocations or interop scenarios where data alignment is critical.</para>
        ///  <para />
        ///</remarks>
        ///<returns>The alignment size in bytes required for the specified struct type.</returns>
        ///<example>
        ///  <code><![CDATA[using System;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using System.Runtime.InteropServices;
        ///using UnityEngine;
        ///
        ///[StructLayout(LayoutKind.Sequential)]
        ///struct ExampleStruct
        ///{
        ///    public byte ByteValue;  // 1 byte
        ///    public int IntValue;    // 4 bytes
        ///    public byte AnotherByte; // 1 byte
        ///}
        ///
        ///public class StructSizeAndAlignment : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Calculate size and alignment for ExampleStruct
        ///        int size = UnsafeUtility.SizeOf<ExampleStruct>();
        ///        int alignment = UnsafeUtility.AlignOf<ExampleStruct>();
        ///
        ///        Debug.Log($"Size of ExampleStruct: {size} bytes");
        ///        Debug.Log($"Alignment requirement of ExampleStruct: {alignment} bytes");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.SizeOf" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignOf<T>() where T : struct
        {
            return UnsafeUtilityInternal.AlignOf<T>();
        }

        // Reinterprets reference as reference of different type.
        ///<summary>Performs an unsafe cast of a specified object to a different type.</summary>
        ///<remarks>
        ///  <para>The <c>As</c> method converts a reference of one type to another type. Use this method for direct type conversions, such as during low-level memory operations or when you interact with unmanaged code where type safety is not enforced.
        ///
        ///Apply caution when you use this method. Improper casts may lead to runtime exceptions or undefined behavior if the types are incompatible. This method bypasses the usual type safety checks and provides flexibility but less safety.</para>
        ///  <para />
        ///</remarks>
        ///<param name="from">The reference to an object of one type that you want to reinterpret as a reference of another type. Ensure the conversion is between compatible types to prevent runtime errors.</param>
        ///<returns>A reference to the object interpreted as the target type.</returns>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections.LowLevel.Unsafe;
        ///using UnityEngine;
        ///
        ///public class UnsafeUtilityAsExample : MonoBehaviour
        ///{
        ///    enum ExampleEnum
        ///    {
        ///        First = 1,
        ///        Second = 2,
        ///    }
        ///    
        ///    struct SourceStruct
        ///    {
        ///        public int IntegerValue;
        ///    }
        ///
        ///    struct TargetStruct
        ///    {
        ///        public ExampleEnum EnumValue;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        SourceStruct source = new SourceStruct { IntegerValue = 2 };
        ///
        ///        // Use UnsafeUtility.As to convert the source struct to a target struct
        ///        TargetStruct target = UnsafeUtility.As<SourceStruct, TargetStruct>(ref source);
        ///
        ///        // Output the converted value
        ///        Debug.Log($"Converted value: {target.EnumValue}");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf" />
        ///<seealso cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AsRef" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T As<U, T>(ref U from)
        {
            return ref UnsafeUtilityInternal.As<U, T>(ref from);
        }

        // Reinterprets reference type as different reference type.
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.MarshallingModule")]
        internal static T As<T>(object from) where T : class
        {
            return UnsafeUtilityInternal.As<T>(from);
        }

        // The address of the memory where the struct resides in memory
        ///<summary>Gets a reference to a struct at a specific memory location.</summary>
        ///<remarks>
        ///  <para>The <c>AsRef</c> method provides a reference to a struct directly in memory. This allows efficient data manipulation without copying. Use this method when performance is critical, and you must access data in-place. Improper use can lead to memory corruption, so always ensure pointer validity.</para>
        ///  <para />
        ///</remarks>
        ///<param name="ptr">A pointer to the memory location of the struct. Ensure the pointer is valid and correctly allocated.</param>
        ///<returns>A reference to the struct of type <c>T</c>. This reference allows changes to the struct at its current memory location.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///
        ///public class AsRefExample : MonoBehaviour
        ///{
        ///    struct Position
        ///    {
        ///        public float X;
        ///        public float Y;
        ///        public float Z;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        // Allocate memory for Position using stackalloc
        ///        unsafe
        ///        {
        ///            Position* positionPtr = stackalloc Position[1];
        ///
        ///            // Set initial values
        ///            positionPtr->X = 1.0f;
        ///            positionPtr->Y = 2.0f;
        ///            positionPtr->Z = 3.0f;
        ///
        ///            // Use AsRef to get a reference to the struct
        ///            ref Position positionRef = ref UnsafeUtility.AsRef<Position>(positionPtr);
        ///
        ///            // Modify the struct through the reference
        ///            positionRef.X = 10.0f;
        ///            positionRef.Y = 20.0f;
        ///            positionRef.Z = 30.0f;
        ///
        ///            // Output the modified values
        ///            Debug.Log($"Modified Position: X = {positionPtr->X}, Y = {positionPtr->Y}, Z = {positionPtr->Z}");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.As" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static ref T AsRef<T>(void* ptr) where T : struct
        {
            return ref UnsafeUtilityInternal.AsRef<T>(ptr);
        }

        // The address of the memory where the class resides in memory
        unsafe internal static ref T ClassAsRef<T>(void* ptr) where T : class
        {
            return ref UnsafeUtilityInternal.ClassAsRef<T>(ptr);
        }

        // The address of the memory where the struct resides in memory
        ///<summary>Obtains a reference to a value of type <c>T</c> in an array at a specified index in memory.</summary>
        ///<remarks>
        ///  <para>The <c>ArrayElementAsRef</c> method allows direct access to an array element by its index in memory. This method provides a reference to the element, enabling efficient data manipulation without copying. Use this method with care, as incorrect pointer usage can cause memory corruption or application crashes.</para>
        ///  <para />
        ///</remarks>
        ///<param name="ptr">A pointer to the first element of the array. Ensure that this pointer is valid and points to allocated memory.</param>
        ///<param name="index">The zero-based index of the element to reference. Verify that the index is within the bounds of the array to prevent access violations.</param>
        ///<returns>A reference to the value of type T at the specified index within the array. This reference allows direct manipulation of the value at its memory location.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using Unity.Collections.LowLevel.Unsafe;
        ///using System.Runtime.InteropServices;
        ///
        ///public class ArrayElementAsRefExample : MonoBehaviour
        ///{
        ///    [StructLayout(LayoutKind.Sequential)]
        ///    struct CustomStruct
        ///    {
        ///        public int Id;
        ///        public float Value;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        int arrayLength = 5;
        ///
        ///        // Allocate a buffer for CustomStruct using stackalloc
        ///        unsafe
        ///        {
        ///            CustomStruct* structArray = stackalloc CustomStruct[arrayLength];
        ///
        ///            // Initialize the array with values
        ///            for (int i = 0; i < arrayLength; i++)
        ///            {
        ///                structArray[i].Id = i;
        ///                structArray[i].Value = i * 1.5f;
        ///            }
        ///
        ///            // Get a reference to an element using ArrayElementAsRef
        ///            ref CustomStruct element = ref UnsafeUtility.ArrayElementAsRef<CustomStruct>(structArray, 2);
        ///            element.Value = 99.9f;  // Modify the value in place
        ///
        ///            // Output the modified value
        ///            Debug.Log($"Modified element at index 2: Id = {structArray[2].Id}, Value = {structArray[2].Value}");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="UnsafeUtility.ReadArrayElement" />
        ///<seealso cref="UnsafeUtility.WriteArrayElement" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static ref T ArrayElementAsRef<T>(void* ptr, int index) where T : struct
        {
            return ref UnsafeUtilityInternal.ArrayElementAsRef<T>(ptr, index);
        }

        // converts generic enum to int without boxing
        ///<summary>Gets the integer representation of an enum value without boxing.</summary>
        ///<param name="enumValue">Enum value to convert.</param>
        ///<returns>Returns the integer representation of the enum value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnumToInt<T>(T enumValue) where T : struct, IConvertible
        {
            return UnsafeUtilityInternal.EnumToInt<T>(enumValue);
        }

        // generic enum equals check without boxing
        ///<summary>Determines whether specified enums are equal without boxing.</summary>
        ///<param name="lhs">The first enum to compare.</param>
        ///<param name="rhs">The second enum to compare.</param>
        ///<returns>True if equal, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EnumEquals<T>(T lhs, T rhs) where T : struct, IConvertible
        {
            return UnsafeUtilityInternal.EnumEquals<T>(lhs, rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal unsafe static ref T Add<T> (ref T source, int elementOffset) where T : unmanaged
        {
            return ref UnsafeUtilityInternal.Add<T>(ref source, elementOffset);
        }

        // The address of the memory where the struct resides in memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void* AsPointer<T>(ref T output)
        {
            return UnsafeUtilityInternal.AsPointer<T>(ref output);
        }

        // A reference that is null
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T NullRef<T>()
        {
            return ref UnsafeUtilityInternal.NullRef<T>();
        }
    }
}
