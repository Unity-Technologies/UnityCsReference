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

#pragma warning disable 169

namespace UnityEngine
{
    // --------------------------------------------------------------------
    // Primitive tests

    [NativeHeader("MarshallingScriptingClasses.h")]
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class StringTests
    {
        [NativeThrows] public static extern void ParameterICallString(string param);
        [NativeThrows] public static extern void ParameterICallNullString(string param);

        [NativeThrows] public static extern void ParameterCoreString(string param);

        [NativeThrows] public static extern void ParameterConstCharPtr(string param);

        [NativeThrows] public static extern void ParameterCoreStringVector(string[] param);

        [NativeThrows] public static extern void ParameterCoreStringDynamicArray(string[] param);

        [NativeThrows] public static extern void ParameterStructCoreString(StructCoreString param);

        [NativeThrows] public static extern void ParameterStructCoreStringVector(StructCoreStringVector param);

        public static extern string ReturnCoreString();

        public static extern string ReturnConstCharPtr();

        public static extern string[] ReturnCoreStringVector();

        public static extern string[] ReturnCoreStringDynamicArray();

        public static extern string[] ReturnNullStringDynamicArray();

        public static extern StructCoreString ReturnStructCoreString();

        [NativeConditional("FOO")]
        public static extern string FalseConditional();

        public static extern StructCoreStringVector ReturnStructCoreStringVector();
        public static extern void ParameterOutString(out string param);
        [NativeThrows] public static extern void ParameterRefString(ref string param);
    }

    // --------------------------------------------------------------------
    // Blittable tests

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructInt
    {
        public int field;
    }
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructInt2
    {
        public int field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructNestedBlittable
    {
        public StructInt field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    [ExcludeFromDocs]
    internal unsafe struct StructFixedBuffer
    {
        public fixed int SomeInts[4];
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObject
    {
        public MyIntPtrObject field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObjectVector
    {
        public MyIntPtrObject[] field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructIntPtrObjectDynamicArray
    {
        public MyIntPtrObject[] field;
    }

    [ExcludeFromDocs]
    internal class MyIntPtrObject : IDisposable
    {
        public IntPtr m_Ptr;

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
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class IntPtrObjectTests
    {
        [NativeThrows] public static extern void ParameterIntPtrObject(MyIntPtrObject param);

        [NativeThrows] public static extern void ParameterIntPtrObjectDynamicArray(MyIntPtrObject[] param);

        [NativeThrows] public static extern void ParameterStructIntPtrObject(StructIntPtrObject param);

        public static extern MyIntPtrObject[] ReturnIntPtrObjectDynamicArray();

        [NativeThrows] public static extern void ParameterStructIntPtrObjectDynamicArray(StructIntPtrObjectDynamicArray param);
    }

    // --------------------------------------------------------------------
    // UnityEngineObject tests

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

        [NativeWritableSelf]
        public extern int WritableSelfFunction(int a);

        public extern static MarshallingTestObject Create();

        extern private static void Internal_CreateMarshallingTestObject([Writable] MarshallingTestObject notSelf);
    }

    [MarshalUnityObjectAs(typeof(MonoBehaviour))]
    internal class MonoBehaviourDerived1 : MonoBehaviour
    {
    }

    [MarshalUnityObjectAs(typeof(MonoBehaviour))]
    internal class MonoBehaviourDerived2 : MonoBehaviour
    {
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObject
    {
        public MarshallingTestObject field;
        public extern int InstanceMethod([NotNull] System.Object o);
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectPPtr
    {
        public MarshallingTestObject field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectVector
    {
        public MarshallingTestObject[] field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal struct StructUnityObjectDynamicArray
    {
        public MarshallingTestObject[] field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class UnityObjectTests
    {
        [NativeThrows] public static extern void ParameterUnityObject(MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterUnityObjectByRef(ref MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterUnityObjectPPtr(MarshallingTestObject param);

        [NativeThrows] public static extern void ParameterStructUnityObject(StructUnityObject param);

        [NativeThrows] public static extern void ParameterStructUnityObjectPPtr(StructUnityObjectPPtr param);

        public static extern void ParameterMonoBehaviourDerived1(MonoBehaviourDerived1 param);

        public static extern void ParameterMonoBehaviourDerived2(MonoBehaviourDerived2 param);

        [NativeThrows] public static extern void ParameterStructUnityObjectDynamicArray(StructUnityObjectDynamicArray param);

        [NativeThrows] public static extern void ParameterUnityObjectDynamicArray(MarshallingTestObject[] param);

        [NativeThrows] public static extern void ParameterUnityObjectPPtrDynamicArray(MarshallingTestObject[] param);

        public static extern MarshallingTestObject ReturnUnityObject();

        public static extern MarshallingTestObject ReturnUnityObjectFakeNull();

        public static extern MarshallingTestObject ReturnUnityObjectPPtr();

        public static extern MarshallingTestObject[] ReturnUnityObjectDynamicArray();

        public static extern MarshallingTestObject[] ReturnUnityObjectPPtrDynamicArray();

        public static extern StructUnityObject ReturnStructUnityObject();

        public static extern StructUnityObjectPPtr ReturnStructUnityObjectPPtr();

        public static extern StructUnityObject[] ReturnStructUnityObjectDynamicArray();

        public static extern StructUnityObjectPPtr[] ReturnStructUnityObjectPPtrDynamicArray();

        public static extern StructUnityObjectDynamicArray[] ReturnStructUnityObjectDynamicArrayDynamicArray();
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    internal struct StructManagedObject
    {
        public MyManagedObject field;
    }

    [ExcludeFromDocs]
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    internal struct StructManagedObjectVector
    {
        public MyManagedObject[] field;
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/SystemTypeMarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/SystemReflectionFieldInfoMarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/SystemReflectionMethodInfoMarshallingTests.h")]
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
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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
    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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
    [NativeType(Header = "Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class TypedefManagedNameTests
    {
        public static extern void ParameterStructWithTypedefManagedName(StructWithTypedefManagedName param);
    }

    // --------------------------------------------------------------------
    // Field-bound property tests
    [NativeType("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    internal class FieldBoundPropertyTests
    {
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern int StaticProp { get; set; }
        [NativeProperty("foo", false, TargetType.Field)]
        [StaticAccessor("FieldBoundPropertyTests::GetNativeStaticPropContainer()", StaticAccessorType.Dot)]
        public static extern int StaticAccessorProp { get; set; }
    }


    // --------------------------------------------------------------------
    // System.Array tests
    [NativeType("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    internal class SystemArrayTests
    {
        [NativeThrows] public static extern void ParameterIntArray(System.Array param);
        public static extern System.Array ReturnIntArray();
    }

    [NativeHeader("Runtime/Scripting/Marshalling/Test/OutArrayMarshallingTests.h")]
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
    [NativeType(Header = "Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
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

    [NativeHeader("Runtime/Scripting/Marshalling/Test/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class BoolStructTests
    {
        [NativeThrows] public static extern void ParameterStructWith8ByteAndBoolFields(StructWith8ByteAndBoolFields param);
        [NativeThrows] public static extern void ParameterStructWith8ByteAndBoolFieldsArray(StructWith8ByteAndBoolFields[] param);
    }
}
