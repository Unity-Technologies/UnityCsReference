// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// NOTE
// This file is STRICTLY for test purposes only. The point is to test the managed->native call through the
// BindingsGenerator. There is currently no alternative way to test this, than to have test classes lying around.


using System;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using System.Collections.Generic;

#pragma warning disable 169

namespace UnityEngine
{
    // --------------------------------------------------------------------
    // Primitive tests

    [NativeHeader("MarshallingScriptingClasses.h")]
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class PrimitiveTests
    {
        [NativeThrows] public static extern void ParameterBool(bool param1, bool param2, int param3);

        [NativeThrows] public static extern void ParameterInt(int param);

        public static extern void ParameterOutInt(out int param);

        public static extern void ParameterRefInt(ref int param);

        public static extern int ReturnInt();

        [NativeThrows] public static extern void ParameterIntDynamicArray(int[] param);

        [NativeThrows] public static extern void ParameterIntNullableDynamicArray(int[] param);

        public static extern int[] ReturnIntDynamicArray();

        public static extern int[] ReturnNullIntDynamicArray();

        public static extern bool[] ReturnBoolDynamicArray();

        public static extern char[] ReturnCharDynamicArray();
    }

    // --------------------------------------------------------------------
    // String tests

    [ExcludeFromDocs]
    [RequiredByNativeCode(GenerateProxy = true, Name = "StructCoreStringManaged", Optional = true)]
    [NativeClass("StructCoreString", "struct StructCoreString;")]
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal struct StructCoreString
    {
        public string field;
        public extern string GetField();
        public extern void SetField(string value);
    }

    [ExcludeFromDocs]
    internal struct StructCoreStringVector
    {
        public string[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class StringTests
    {
        public static extern void SetTestOutString(string testString);

        [NativeThrows] public static extern void ParameterICallString(string param);
        [NativeThrows] public static extern void ParameterICallNullString(string param);

        [NativeThrows] public static extern void ParameterCoreString(string param);

        [NativeThrows] public static extern void ParameterConstCharPtr(string param);
        [NativeThrows] public static extern void ParameterConstCharPtrNull(string param);
        [NativeThrows] public static extern void ParameterConstCharPtrEmptyString(string param);

        [NativeThrows] public static extern void ParameterCoreStringVector(string[] param);

        [NativeThrows] public static extern void ParameterCoreStringDynamicArray(string[] param);

        [NativeThrows] public static extern void ParameterStructCoreString(StructCoreString param);

        [NativeThrows] public static extern void ParameterStructCoreStringVector(StructCoreStringVector param);

        [NativeThrows] public static extern StructCoreString TestCoreStringViaProxy(StructCoreString param);

        public static extern string ReturnCoreString();
        public static extern string ReturnCoreStringRef();
        public static extern string ReturnConstCharPtr();

        public static extern string[] ReturnCoreStringVector();
        public static extern string[] ReturnCoreStringDynamicArray();
        public static extern string[] ReturnNullStringDynamicArray();

        public static extern StructCoreString ReturnStructCoreString();

        [NativeConditional("FOO")]
        public static extern string FalseConditional();

        public static extern StructCoreStringVector ReturnStructCoreStringVector();

        [NativeThrows] public static extern void ParameterOutString(out string param);
        [NativeThrows] public static extern void ParameterOutStringInNull(out string param);
        [NativeThrows] public static extern void ParameterOutStringNotSet(out string param);
        [NativeThrows] public static extern void ParameterRefString(ref string param);
        [NativeThrows] public static extern void ParameterRefStringInNull(ref string param);
        [NativeThrows] public static extern void ParameterRefStringNotSet(ref string param);
    }

    // --------------------------------------------------------------------
    // Blittable tests

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructInt
    {
        public int field;
    }
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructInt2
    {
        public int field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructNestedBlittable
    {
        public StructInt field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    [ExcludeFromDocs]
    internal unsafe struct StructFixedBuffer
    {
        public fixed int SomeInts[4];
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class BlittableStructTests
    {
        [NativeThrows] public static extern void ParameterStructInt(StructInt param);

        public static extern void ParameterStructInt2(StructInt2 param);

        public static extern StructInt ReturnStructInt();

        [NativeThrows] public static extern void ParameterNestedBlittableStruct(StructNestedBlittable s);

        public static extern StructNestedBlittable ReturnNestedBlittableStruct();

        [NativeThrows] public static extern void ParameterStructIntDynamicArray(StructInt[] param);

        public static extern StructInt[] ReturnStructIntDynamicArray();

        [NativeThrows] public static extern void ParameterStructNestedBlittableDynamicArray(StructNestedBlittable[] param);

        public static extern StructNestedBlittable[] ReturnStructNestedBlittableDynamicArray();

        [NativeThrows] public static extern void ParameterStructFixedBuffer(StructFixedBuffer param);

        public static extern StructFixedBuffer ReturnStructFixedBuffer();

        public static extern StructInt structIntProperty { get; set; }
    }

    // --------------------------------------------------------------------
    // IntPtrObject tests

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObject
    {
        public MyIntPtrObject field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObjectVector
    {
        public MyIntPtrObject[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObjectDynamicArray
    {
        public MyIntPtrObject[] field;
    }

    [ExcludeFromDocs]
    internal class MyIntPtrObject : IDisposable
    {
        public IntPtr m_Ptr;

        internal MyIntPtrObject(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        public MyIntPtrObject()
        {
            m_Ptr = Internal_Create();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public extern static MyIntPtrObject Create();

        public extern int MemberFunction(int a);

        public extern int MemberProperty
        {
            get;
            set;
        }

        private static extern IntPtr Internal_Create();

        private static extern void Internal_Destroy(IntPtr ptr);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(MyIntPtrObject obj) => obj.m_Ptr;
            public static MyIntPtrObject ConvertToManaged(IntPtr ptr) => new MyIntPtrObject(ptr);
        }
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class IntPtrObjectTests
    {
        [NativeThrows] public static extern void ParameterIntPtrObject(MyIntPtrObject param);

        [NativeThrows] public static extern void ParameterIntPtrObjectDynamicArray(MyIntPtrObject[] param);

        [NativeThrows] public static extern void ParameterStructIntPtrObject(StructIntPtrObject param);

        public static extern MyIntPtrObject[] ReturnIntPtrObjectDynamicArray();

        [NativeThrows] public static extern void ParameterStructIntPtrObjectDynamicArray(StructIntPtrObjectDynamicArray param);

        public static extern MyIntPtrObject ReturnIntPtrObject(int value);
    }

    // --------------------------------------------------------------------
    // UnityEngineObject tests

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    [StructLayout(LayoutKind.Sequential)]
    internal class MarshallingTestObject : Object
    {
        public MarshallingTestObject()
        {
            Internal_CreateMarshallingTestObject(this);
        }

        public extern int MemberFunction(int a);

        public extern int MemberProperty
        {
            get;
            set;
        }

        [NativeProperty("m_fieldBoundProp", false, TargetType.Field)]
        public extern int FieldBoundMemberProperty
        {
            get;
            set;
        }

        public extern static MarshallingTestObject Create();

        extern private static void Internal_CreateMarshallingTestObject([Writable] MarshallingTestObject notSelf);

        [RequiredMember, RequiredByNativeCode(Optional = true)]
        private int TestField;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    [StructLayout(LayoutKind.Sequential)]
    internal class DifferentMarshallingTestObject : Object
    {

    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObject
    {
        public MarshallingTestObject field;
        public extern int InstanceMethod([NotNull] System.Object o);
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectPPtr
    {
        public MarshallingTestObject field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectVector
    {
        public MarshallingTestObject[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectDynamicArray
    {
        public MarshallingTestObject[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class UnityObjectTests
    {
        [NativeThrows] public static extern void ParameterUnityObject(MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterUnityObjectByRef(ref MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterUnityObjectPPtr(MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterStructUnityObject(StructUnityObject param);

        [NativeThrows] public static extern void ParameterStructUnityObjectPPtr(StructUnityObjectPPtr param);

        [NativeThrows] public static extern void ParameterStructUnityObjectDynamicArray(StructUnityObjectDynamicArray param);

        [NativeThrows] public static extern void ParameterUnityObjectDynamicArray(MarshallingTestObject[] param);

        [NativeThrows] public static extern void ParameterUnityObjectPPtrDynamicArray(MarshallingTestObject[] param);

        public static extern MarshallingTestObject ReturnUnityObject();
        public static extern MarshallingTestObject ReturnInUnityObject(MarshallingTestObject obj);

        public static extern MarshallingTestObject ReturnUnityObjectFakeNull();

        public static extern MarshallingTestObject ReturnUnassignedErrorObject();

        public static extern MarshallingTestObject ReturnUnityObjectPPtr();

        public static extern MarshallingTestObject[] ReturnUnityObjectDynamicArray();

        public static extern MarshallingTestObject[] ReturnUnityObjectPPtrDynamicArray();

        public static extern StructUnityObject ReturnStructUnityObject();

        public static extern StructUnityObjectPPtr ReturnStructUnityObjectPPtr();

        public static extern StructUnityObject[] ReturnStructUnityObjectDynamicArray();

        public static extern StructUnityObjectPPtr[] ReturnStructUnityObjectPPtrDynamicArray();

        public static extern StructUnityObjectDynamicArray[] ReturnStructUnityObjectDynamicArrayDynamicArray();
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class NullCheckTests
    {
        public static extern void StringParameterNullAllowed(string param);
        public static extern void StringParameterNullNotAllowed([NotNull] string param);

        public static extern void ArrayParameterNullAllowed(int[] param);
        public static extern void ArrayParameterNullNotAllowed([NotNull] int[] param);

        [NativeThrows] public static extern void ObjectParameterNullAllowed(MarshallingTestObject param);
        public static extern void ObjectParameterNullNotAllowed([NotNull] MarshallingTestObject param);

        public static extern void WritableObjectParameterNullAllowed([Writable] MarshallingTestObject param);
        public static extern void WritableObjectParameterNullNotAllowed([NotNull][Writable] MarshallingTestObject param);

        [NativeThrows] public static extern void IntPtrObjectParameterNullAllowed(MyIntPtrObject param);
        public static extern void IntPtrObjectParameterNullNotAllowed([NotNull] MyIntPtrObject param);
    }
    // --------------------------------------------------------------------
    // Managed object tests

    [ExcludeFromDocs]
    [StructLayout(LayoutKind.Sequential)]
    internal class MyManagedObject
    {
        public int value = 42;
    }

    [ExcludeFromDocs]
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal struct StructManagedObject
    {
        public MyManagedObject field;
    }

    [ExcludeFromDocs]
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal struct StructManagedObjectVector
    {
        public MyManagedObject[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class ManagedObjectTests
    {
        [NativeThrows] public static extern MyManagedObject ParameterManagedObject(MyManagedObject param);
        [NativeThrows] public static extern StructManagedObject ParameterStructManagedObject(StructManagedObject param);
        public static extern MyManagedObject[] ReturnNullManagedObjectArray();

        [NativeThrows] public static extern MyManagedObject[] ParameterManagedObjectVector(MyManagedObject[] param);

        [NativeThrows] public static extern StructManagedObjectVector ParameterStructManagedObjectVector(StructManagedObjectVector param);
    }

    // --------------------------------------------------------------------
    // System.Type tests

    [ExcludeFromDocs]
    internal struct StructSystemType
    {
        public System.Type field;
    }

    [ExcludeFromDocs]
    internal struct StructSystemTypeArray
    {
        public System.Type[] field;
    }

    [NativeHeader("Modules/Marshalling/SystemTypeMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class SystemTypeMarshallingTests
    {
        public static extern string CanMarshallSystemTypeArgumentToScriptingClassPtr(System.Type param);

        public static extern string CanMarshallSystemTypeStructField(StructSystemType param);
        public static extern string[] CanMarshallSystemTypeArrayStructField(StructSystemTypeArray param);

        public static extern StructSystemType CanUnmarshallSystemTypeStructField();
        public static extern StructSystemTypeArray CanUnmarshallSystemTypeArrayStructField();

        public static extern string[] CanUnmarshallArrayOfSystemTypeArgumentToDynamicArrayOfScriptingSystemTypeObjectPtr(System.Type[] param);
        public static extern string[] CanUnmarshallArrayOfSystemTypeArgumentToDynamicArrayOfUnityType(System.Type[] param);
        public static extern string[] CanUnmarshallArrayOfSystemTypeArgumentToDynamicArrayOfScriptingClassPtr(System.Type[] param);

        public static extern System.Type CanUnmarshallScriptingSystemTypeObjectPtrToSystemType();
        public static extern System.Type CanUnmarshallUnityTypeToSystemType();
        public static extern System.Type CanUnmarshallScriptingClassPtrToSystemType();

        public static extern System.Type[] CanUnmarshallScriptingArrayPtrToSystemTypeArray();
        public static extern System.Type[] CanUnmarshallArrayOfScriptingSystemTypeObjectPtrToSystemTypeArray();
        public static extern System.Type[] CanUnmarshallArrayOfUnityTypeToSystemTypeArray();
        public static extern System.Type[] CanUnmarshallArrayOfScriptingClassPtrToSystemTypeArray();
    }

    // --------------------------------------------------------------------
    // System.Reflection.FieldInfo tests

    [ExcludeFromDocs]
    internal struct StructSystemReflectionFieldInfo
    {
        public System.Reflection.FieldInfo field;
    }

    [ExcludeFromDocs]
    internal struct StructSystemReflectionFieldInfoArray
    {
        public System.Reflection.FieldInfo[] field;
    }

    [NativeHeader("Modules/Marshalling/SystemReflectionFieldInfoMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class SystemReflectionFieldInfoMarshallingTests
    {
        public static extern string CanMarshallFieldInfoArgumentToScriptingFieldPtr(System.Reflection.FieldInfo param);

        public static extern string CanMarshallSystemReflectionFieldInfoStructField(StructSystemReflectionFieldInfo param);
        public static extern string[] CanMarshallSystemReflectionFieldInfoArrayStructField(StructSystemReflectionFieldInfoArray param);

        public static extern string[] CanMarshallArrayOfFieldInfoArgumentToDynamicArrayOfScriptingFieldInfoObjectPtr(System.Reflection.FieldInfo[] param);
        public static extern string[] CanMarshallArrayOfFieldInfoArgumentToDynamicArrayOfScriptingFieldPtr(System.Reflection.FieldInfo[] param);

        public static extern StructSystemReflectionFieldInfo CanUnmarshallSystemReflectionFieldInfoStructField();
        public static extern StructSystemReflectionFieldInfoArray CanUnmarshallSystemReflectionFieldInfoArrayStructField();

        public static extern System.Reflection.FieldInfo CanUnmarshallScriptingFieldInfoObjectPtrToFieldInfo();
        public static extern System.Reflection.FieldInfo CanUnmarshallScriptingFieldPtrToFieldInfo();

        public static extern System.Reflection.FieldInfo[] CanUnmarshallScriptingArrayPtrToFieldInfoArray();
        public static extern System.Reflection.FieldInfo[] CanUnmarshallArrayOfScriptingFieldInfoObjectPtrToFieldInfoArray();
        public static extern System.Reflection.FieldInfo[] CanUnmarshallArrayOfScriptingFieldPtrToFieldInfoArray();
    }

    // --------------------------------------------------------------------
    // System.Reflection.MethodInfo tests

    [ExcludeFromDocs]
    internal struct StructSystemReflectionMethodInfo
    {
        public System.Reflection.MethodInfo field;
    }

    [ExcludeFromDocs]
    internal struct StructSystemReflectionMethodInfoArray
    {
        public System.Reflection.MethodInfo[] field;
    }

    [NativeHeader("Modules/Marshalling/SystemReflectionMethodInfoMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class SystemReflectionMethodInfoMarshallingTests
    {
        public static extern string CanMarshallMethodInfoArgumentToScriptingMethodPtr(System.Reflection.MethodInfo param);

        public static extern string CanMarshallSystemReflectionMethodInfoStructField(StructSystemReflectionMethodInfo param);
        public static extern string[] CanMarshallSystemReflectionMethodInfoArrayStructField(StructSystemReflectionMethodInfoArray param);

        public static extern string[] CanMarshallArrayOfMethodInfoArgumentToDynamicArrayOfScriptingMethodInfoObjectPtr(System.Reflection.MethodInfo[] param);
        public static extern string[] CanMarshallArrayOfMethodInfoArgumentToDynamicArrayOfScriptingMethodPtr(System.Reflection.MethodInfo[] param);

        public static extern StructSystemReflectionMethodInfo CanUnmarshallSystemReflectionMethodInfoStructField();
        public static extern StructSystemReflectionMethodInfoArray CanUnmarshallSystemReflectionMethodInfoArrayStructField();

        public static extern System.Reflection.MethodInfo CanUnmarshallScriptingMethodInfoObjectPtrToMethodInfo();
        public static extern System.Reflection.MethodInfo CanUnmarshallScriptingMethodPtrToMethodInfo();

        public static extern System.Reflection.MethodInfo[] CanUnmarshallScriptingArrayPtrToMethodInfoArray();
        public static extern System.Reflection.MethodInfo[] CanUnmarshallArrayOfScriptingMethodInfoObjectPtrToMethodInfoArray();
        public static extern System.Reflection.MethodInfo[] CanUnmarshallArrayOfScriptingMethodPtrToMethodInfoArray();
    }

    // --------------------------------------------------------------------
    // Struct w/ icall tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    [StructLayout(LayoutKind.Sequential)]
    internal struct StructWithExternTests
    {
        public int a;

        public extern int GetTimesTwo();

        public extern void SetTimesThree();

        public extern int ParameterWritable([Writable] UnityEngine.Object unityObject);

        [NativeThrows] public static extern void ParameterInt(int param);

        public static extern int ReturnInt();

    }

    // --------------------------------------------------------------------
    // Delegate tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class DelegateTests
    {
        public delegate int SomeDelegate();

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        public delegate int SomeDelegateFunctionPtr();

        public static int A() { return 882; }
        [AOT.MonoPInvokeCallbackAttribute(typeof(SomeDelegateFunctionPtr))]
        public static int B() { return 883; }

        public static extern int ReturnDelegate(SomeDelegate someDelegate);
        public static extern int ReturnDelegateFunctionPtr(SomeDelegateFunctionPtr SomeDelegateFunctionPtr);
    }

    // --------------------------------------------------------------------
    // Exception tests

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class ExceptionTests
    {
        [NativeThrows]
        public static extern void VoidReturnStringParameter(string param);

        [NativeThrows]
        public static extern int NonUnmarshallingReturn();

        [NativeThrows]
        public static extern string UnmarshallingReturn();

        [NativeThrows]
        public static extern StructInt BlittableStructReturn();

        [NativeThrows]
        public static extern StructCoreString NonblittableStructReturn();

        [NativeThrows]
        public static extern int PropertyThatCanThrow { get; set; }

        public static extern int PropertyGetThatCanThrow
        {
            [NativeThrows]
            get;
            set;
        }
        public static extern int PropertySetThatCanThrow
        {
            get;
            [NativeThrows]
            set;
        }
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class ExceptionTypeTests
    {
        [NativeThrows]
        public static extern void NullReferenceException(string nativeFormat, string values);

        [NativeThrows]
        public static extern void ArgumentNullException(string argumentName);

        [NativeThrows]
        public static extern void ArgumentException(string nativeFormat, string values);

        [NativeThrows]
        public static extern void InvalidOperationException(string nativeFormat, string values);

        [NativeThrows]
        public static extern void IndexOutOfRangeException(string nativeFormat, int index);
    }

    // --------------------------------------------------------------------
    // Enum tests

    // Keep in sync with MarshallingTests.bindings.h
    [ExcludeFromDocs]
    internal enum SomeEnum
    {
        A = 0,
        B = 1,
        C = 2,
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class EnumTests
    {
        [NativeThrows] public static extern void ParameterDynamicArrayEnum(SomeEnum[] enumArray);
        public static extern void ParameterOutDynamicArrayEnum([Out] SomeEnum[] enumArray);
    }

    // --------------------------------------------------------------------
    // Non-blittables struct tests

    [ExcludeFromDocs]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal struct StructWithStringIntAndFloat
    {
        public string a;
        public int b;
        public float c;

        public override bool Equals(System.Object other)
        {
            if (other == null)
                return false;
            if (other is StructWithStringIntAndFloat)
            {
                StructWithStringIntAndFloat otherStruct = (StructWithStringIntAndFloat)other;
                return a.Equals(otherStruct.a) && b == otherStruct.b && c == otherStruct.c;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return a.GetHashCode();
        }
    }

    [ExcludeFromDocs]
    internal struct StructWithStringIntAndFloat2
    {
        public string a;
        public int b;
        public float c;
    }

    [ExcludeFromDocs]
    internal struct StructWithStringIgnoredIntAndFloat
    {
        public string a;
        [Ignore]
        public int b;
        public float c;
    }

    [ExcludeFromDocs]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    internal class ClassToStruct
    {
        public int intField;
        public string stringField;
    }

    [ExcludeFromDocs]
    internal struct StructWithClassToStruct
    {
        public ClassToStruct classToStructField;
    }

    [ExcludeFromDocs]
    internal struct StructWithNonBlittableArrayField
    {
        public StructWithStringIntAndFloat[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class NonBlittableStructTests
    {
        [NativeThrows] public static extern void ParameterStructWithStringIntAndFloat(StructWithStringIntAndFloat param);
        [NativeThrows] public static extern void RefParameterStructWithStringIntAndFloat(ref StructWithStringIntAndFloat param);
        public static extern void OutParameterStructWithStringIntAndFloat(out StructWithStringIntAndFloat param);
        public static extern void ParameterStructWithStringIntAndFloat2(StructWithStringIntAndFloat2 param);
        [NativeThrows] public static extern void ParameterStructWithStringIgnoredIntAndFloat(StructWithStringIgnoredIntAndFloat param);

        [NativeThrows] public static extern void ParameterStructWithStringIntAndFloatArray(StructWithStringIntAndFloat[] param);
        public static extern StructWithStringIntAndFloat[] ReturnStructWithStringIntAndFloatArray();

        [NativeThrows] public static extern void ParameterStructWithNonBlittableArrayField(StructWithNonBlittableArrayField param);
        public static extern StructWithNonBlittableArrayField ReturnStructWithNonBlittableArrayField();

        [NativeThrows] public static extern void CanMarshalManagedObjectToStruct(ClassToStruct param);
        [NativeThrows] public static extern void CanMarshalOutManagedObjectToStruct([Out] ClassToStruct param);
        [NativeThrows] public static extern void CanMarshalStructWithNativeAsStructField(StructWithClassToStruct param);
        [NativeThrows] public static extern void CanMarshalNativeAsStructArray(ClassToStruct[] param);
        public static extern ClassToStruct CanUnmarshalManagedObjectFromStruct();
        public static extern StructWithClassToStruct CanUnmarshalStructWithNativeAsStructField();
        public static extern ClassToStruct[] CanUnmarshalNativeAsStructArray();
    }

    // --------------------------------------------------------------------
    // Typedef Managed Name tests - Just being compilable is enough to test

    [NativeType]
    [ExcludeFromDocs]
    internal struct StructWithTypedefManagedName
    {
#pragma warning disable 0169
        bool a;
    }
    [NativeType(Header = "Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class TypedefManagedNameTests
    {
        public static extern void ParameterStructWithTypedefManagedName(StructWithTypedefManagedName param);
    }

    // --------------------------------------------------------------------
    // Field-bound property tests
    [NativeType("Modules/Marshalling/MarshallingTests.h")]
    internal class FieldBoundPropertyTests
    {
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern int StaticProp { get; set; }
        [NativeProperty("foo", false, TargetType.Field)]
        [StaticAccessor("FieldBoundPropertyTests::GetNativeStaticPropContainer()", StaticAccessorType.Dot)]
        public static extern int StaticAccessorProp { get; set; }
    }

    [NativeHeader("Modules/Marshalling/OutArrayMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class OutArrayMarshallingTests
    {
        public static extern void OutArrayOfPrimitiveTypeWorks([Out] int[] array, int value);
        public static extern void OutArrayOfStringTypeWorks([Out] string[] array, string value);

        public static extern void OutArrayOfBlittableStructTypeWorks([Out] StructInt[] array, StructInt value);
        public static extern void OutArrayOfIntPtrObjectTypeWorks([Out] MyIntPtrObject[] array, MyIntPtrObject value);
        public static extern void OutArrayOfNestedBlittableStructTypeWorks([Out] StructNestedBlittable[] array, StructNestedBlittable value);

        public static extern void OutArrayOfNonBlittableTypeWorks([Out] StructWithStringIntAndFloat[] array, StructWithStringIntAndFloat value);
    }

    [NativeHeader("Modules/Marshalling/ReturnArrayMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class ReturnArrayMarshallingTests
    {
        [return: Unmarshalled]
        public static extern float[] ReturnArrayOfPrimitiveTypeWorks_Float1D();

        [return: Unmarshalled]
        public static extern float[,] ReturnArrayOfPrimitiveTypeWorks_Float2D();

        [return: Unmarshalled]
        public static extern float[,,] ReturnArrayOfPrimitiveTypeWorks_Float3D();
    }

    // --------------------------------------------------------------------
    // Nested type tests
    internal class ParentOfNested
    {
        internal class Nested
        {
            public static extern int MethodInNested();
        }
    }

    // --------------------------------------------------------------------
    // Abstract type tests
    [NativeType(Header = "Modules/Marshalling/MarshallingTests.h")]
    internal abstract class AbstractClass
    {
        public static extern int MethodInAbstractClass();
    }

    // --------------------------------------------------------------------
    // BoolStruct tests
    [StructLayout(LayoutKind.Sequential)]
    internal struct StructWith8ByteAndBoolFields
    {
        public Int64 int64;
        public bool bool1;
        public bool bool2;
        public bool bool3;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class BoolStructTests
    {
        [NativeThrows] public static extern void ParameterStructWith8ByteAndBoolFields(StructWith8ByteAndBoolFields param);
        [NativeThrows] public static extern void ParameterStructWith8ByteAndBoolFieldsArray(StructWith8ByteAndBoolFields[] param);
    }

    struct BlittableCornerCases
    {
        public char cVal;
        public bool bVal;
        public SomeEnum eVal;
    }

    // --------------------------------------------------------------------
    // System.Array tests
    [NativeType("Modules/Marshalling/MarshallingTests.h")]
    internal class ValueTypeArrayTests
    {
        [NativeThrows] public static extern void ParameterIntArrayReadOnly(int[] param);
        [NativeThrows] public static extern void ParameterIntArrayWritable(int[] param);
        [NativeThrows] public static extern void ParameterIntArrayEmpty(int[] param, int[] param2);
        public static extern void ParameterIntArrayNullExceptions([NotNull] int[] param);
        [NativeThrows] public static extern void ParameterIntMultidimensionalArray(int[,] param);
        public static extern void ParameterIntMultidimensionalArrayNullExceptions([NotNull] int[,] param);
        [NativeThrows] public static extern void ParameterCharArrayReadOnly(char[] param);
        [NativeThrows] public static extern void ParameterBlittableCornerCaseStructArrayReadOnly(BlittableCornerCases[] param);
        [NativeThrows] public static extern void ParameterIntArrayOutAttr([Out] int[] param);
        [NativeThrows] public static extern void ParameterCharArrayOutAttr([Out]char[] param);
        [NativeThrows] public static extern void ParameterBlittableCornerCaseStructArrayOutAttr([Out]BlittableCornerCases[] param);
        public static extern int[] ParameterIntArrayReturn();
        public static extern int[] ParameterIntArrayReturnEmpty();
        public static extern int[] ParameterIntArrayReturnNull();
        public static extern char[] ParameterCharArrayReturn();
        public static extern BlittableCornerCases[] ParameterBlittableCornerCaseStructArrayReturn();
    }

    // --------------------------------------------------------------------
    // System.Span tests
    [NativeType("Modules/Marshalling/MarshallingTests.h")]
    internal class ValueTypeSpanTests
    {
        [NativeThrows] public static extern void ParameterIntReadOnlySpan(ReadOnlySpan<int> param);
        [NativeThrows] public static extern void ParameterIntSpan(Span<int> param);
        [NativeThrows] public static extern void ParameterBoolReadOnlySpan(ReadOnlySpan<bool> param);
        [NativeThrows] public static extern void ParameterCharReadOnlySpan(ReadOnlySpan<char> param);
        [NativeThrows] public static extern void ParameterEnumReadOnlySpan(ReadOnlySpan<SomeEnum> param);
        [NativeThrows] public static extern void ParameterBlittableCornerCaseStructReadOnlySpan(ReadOnlySpan<BlittableCornerCases> param);
        public static extern Span<int> ReturnsArrayRefWritableAsSpan(int val1, int val2, int val3);
        public static extern Span<int> ReturnsCoreVectorRefAsSpan(int val1, int val2, int val3);
        public static extern Span<int> ReturnsScriptingSpanAsSpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsArrayRefWritableAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsCoreVectorRefAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsArrayRefAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsScriptingReadOnlySpanAsSpan(int val1, int val2, int val3);
    }

    // --------------------------------------------------------------------
    // System.Collections.Generic.List tests
    [NativeType("Modules/Marshalling/MarshallingTests.h")]
    internal class ValueTypeListOfTTests
    {
        [NativeThrows] public static extern void ParameterListOfIntRead(List <int> param);
        [NativeThrows] public static extern void ParameterListOfIntReadChangeVaules(List <int> param);
        [NativeThrows] public static extern void ParameterListOfIntAddNoGrow(List <int> param);
        [NativeThrows] public static extern void ParameterListOfIntAddAndGrow(List <int> param);
        [NativeThrows] public static extern void ParameterListOfIntPassNullThrow([NotNull] List <int> param);
        [NativeThrows] public static extern void ParameterListOfIntPassNullNoThrow(List<int> param);
        [NativeThrows] public static extern void ParameterListOfIntNativeAllocateSmaller(List<int> param);
        [NativeThrows] public static extern void ParameterListOfIntNativeAttachOtherMemoryBlock(List<int> param);
        [NativeThrows] public static extern void ParameterListOfIntNativeCallsClear(List<int> param);
        [NativeThrows] public static extern void ParameterListOfBoolReadWrite(List<bool> param);
        [NativeThrows] public static extern void ParameterListOfCharReadWrite(List<char> param);
        [NativeThrows] public static extern void ParameterListOfEnumReadWrite(List<SomeEnum> param);
        [NativeThrows] public static extern void ParameterListOfCornerCaseStructReadWrite(List<BlittableCornerCases> param);
    }

        // --------------------------------------------------------------------
    // Invoke tests (calling from native to managed)
    [NativeType("Modules/Marshalling/MarshallingTests.h")]
    internal class InvokeTests
    {
        public static extern bool TestInvokeBool(bool arg);
        public static extern sbyte TestInvokeSByte(sbyte arg);
        public static extern byte TestInvokeByte(byte arg);
        public static extern char TestInvokeChar(char arg);
        public static extern short TestInvokeShort(short arg);
        public static extern ushort TestInvokeUShort(ushort arg);
        public static extern int TestInvokeInt(int arg);
        public static extern uint TestInvokeUInt(uint arg);
        public static extern long TestInvokeLong(long arg);
        public static extern ulong TestInvokeULong(ulong arg);
        public static extern IntPtr TestInvokeIntPtr(IntPtr arg);
        public static extern UIntPtr TestInvokeUIntPtr(UIntPtr arg);
        public static extern float TestInvokeFloat(float arg);
        public static extern double TestInvokeDouble(double arg);

        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static bool InvokeBool(bool arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static sbyte InvokeSByte(sbyte arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static byte InvokeByte(byte arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static char InvokeChar(char arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static short InvokeShort(short arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static ushort InvokeUShort(ushort arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static int InvokeInt(int arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static uint InvokeUInt(uint arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static long InvokeLong(long arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static ulong InvokeULong(ulong arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static IntPtr InvokeIntPtr(IntPtr arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static UIntPtr InvokeUIntPtr(UIntPtr arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static float InvokeFloat(float arg) { return arg; }
        [RequiredMember, RequiredByNativeCode(Optional = true)]
        static double InvokeDouble(double arg) { return arg; }
    }

    internal static class MarshallingTests
    {
        [FreeFunction("MarshallingTest::DisableMarshallingTestsVerification")]
        public static extern void DisableMarshallingTestsVerification();
    }
}
