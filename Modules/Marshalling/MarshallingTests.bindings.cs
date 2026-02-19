// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// NOTE
// This file is STRICTLY for test purposes only. The point is to test the managed->native call through the
// BindingsGenerator. There is currently no alternative way to test this, than to have test classes lying around.


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

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
        [NativeMethod(ThrowsException = true)] public static extern void ParameterBool(bool param1, bool param2, int param3);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterInt(int param);

        public static extern void ParameterOutInt(out int param);

        public static extern void ParameterRefInt(ref int param);

        public static extern int ReturnInt();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntVector(int[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntNullableVector(int[] param);

        public static extern int[] ReturnIntVector();

        public static extern int[] ReturnNullIntVector();

        public static extern bool[] ReturnBoolVector();

        public static extern char[] ReturnCharVector();
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

        [NativeMethod(ThrowsException = true)] public static extern void ParameterICallString(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterICallNullString(string param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterCoreString(string param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterConstCharPtr(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterConstCharPtrNull(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterConstCharPtrEmptyString(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNullableString(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNullableStringNull(string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNullableStringEmptyString(string param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterCoreStringVector(string[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructCoreString(StructCoreString param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructCoreStringVector(StructCoreStringVector param);

        [NativeMethod(ThrowsException = true)] public static extern StructCoreString TestCoreStringViaProxy(StructCoreString param);

        public static extern string ReturnCoreString();
        public static extern string ReturnCoreStringRef();
        public static extern string ReturnConstCharPtr();

        public static extern string[] ReturnCoreStringVector();
        public static extern string[] ReturnNullStringVector();

        public static extern StructCoreString ReturnStructCoreString();

        [NativeConditional("FOO")]
        public static extern string FalseConditional();

        public static extern StructCoreStringVector ReturnStructCoreStringVector();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterOutString(out string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterOutStringInNull(out string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterOutStringNotSet(out string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterRefString(ref string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterRefStringInNull(ref string param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterRefStringNotSet(ref string param);
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
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructInt(StructInt param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntByRef(ref StructInt param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntIn(in StructInt param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntOut(out StructInt param);

        public static extern void ParameterStructInt2(StructInt2 param);

        public static extern StructInt ReturnStructInt();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterNestedBlittableStruct(StructNestedBlittable s);

        public static extern StructNestedBlittable ReturnNestedBlittableStruct();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntVector(StructInt[] param);

        public static extern StructInt[] ReturnStructIntVector();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructNestedBlittableVector(StructNestedBlittable[] param);

        public static extern StructNestedBlittable[] ReturnStructNestedBlittableVector();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructFixedBuffer(StructFixedBuffer param);

        public static extern StructFixedBuffer ReturnStructFixedBuffer();

        public static extern StructInt structIntProperty { get; set; }
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class RealWorldTypesTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void GetAccessibilityNodeData();
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
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntPtrObject(MyIntPtrObject param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntPtrObjectVector(MyIntPtrObject[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntPtrObject(StructIntPtrObject param);

        public static extern MyIntPtrObject[] ReturnIntPtrObjectVector();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructIntPtrObjectVector(StructIntPtrObjectVector param);

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
    internal class UnityObjectTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObject(MarshallingTestObject param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectByRef(ref MarshallingTestObject param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectPPtr(MarshallingTestObject param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructUnityObject(StructUnityObject param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructUnityObjectPPtr(StructUnityObjectPPtr param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructUnityObjectVector(StructUnityObjectVector param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructUnityObjectVectorOut(out StructUnityObjectVector param, int expectedLength);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectVector(MarshallingTestObject[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectPPtrVector(MarshallingTestObject[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectNullCoreVector(MarshallingTestObject[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterUnityObjectPPtrNullCoreVector(MarshallingTestObject[] param);

        [NativeMethod("ParameterUnityObjectNullCoreVector", ThrowsException = true)] public static extern void ParameterUnityObjectNullCoreVectorOut([Out] MarshallingTestObject[] param);

        [NativeMethod("ParameterUnityObjectPPtrNullCoreVector", ThrowsException = true)] public static extern void ParameterUnityObjectPPtrNullCoreVectorOut([Out] MarshallingTestObject[] param);

        public static extern MarshallingTestObject ReturnUnityObject();
        public static extern MarshallingTestObject ReturnInUnityObject(MarshallingTestObject obj);

        public static extern MarshallingTestObject ReturnUnityObjectFakeNull();

        public static extern MarshallingTestObject ReturnUnassignedErrorObject();

        public static extern MarshallingTestObject ReturnUnityObjectPPtr();

        public static extern MarshallingTestObject[] ReturnUnityObjectVector();

        public static extern MarshallingTestObject[] ReturnUnityObjectPPtrVector();

        public static extern StructUnityObject ReturnStructUnityObject();

        public static extern StructUnityObjectPPtr ReturnStructUnityObjectPPtr();

        public static extern StructUnityObject[] ReturnStructUnityObjectVector();

        public static extern StructUnityObjectPPtr[] ReturnStructUnityObjectPPtrVector();

        public static extern StructUnityObjectVector[] ReturnStructUnityObjectVectorVector();
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class NullCheckTests
    {
        public static extern void StringParameterNullAllowed(string param);
        public static extern void StringParameterNullNotAllowed([NotNull] string param);

        public static extern void ArrayParameterNullAllowed(int[] param);
        public static extern void ArrayParameterNullNotAllowed([NotNull] int[] param);

        [NativeMethod(ThrowsException = true)] public static extern void ObjectParameterNullAllowed(MarshallingTestObject param);
        public static extern void ObjectParameterNullNotAllowed([NotNull] MarshallingTestObject param);

        public static extern void WritableObjectParameterNullAllowed([Writable] MarshallingTestObject param);
        public static extern void WritableObjectParameterNullNotAllowed([NotNull][Writable] MarshallingTestObject param);

        [NativeMethod(ThrowsException = true)] public static extern void IntPtrObjectParameterNullAllowed(MyIntPtrObject param);
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
        [NativeMethod(ThrowsException = true)] public static extern MyManagedObject ParameterManagedObject(MyManagedObject param);
        [NativeMethod(ThrowsException = true)] public static extern StructManagedObject ParameterStructManagedObject(StructManagedObject param);
        public static extern MyManagedObject[] ReturnNullManagedObjectArray();

        [NativeMethod(ThrowsException = true)] public static extern MyManagedObject[] ParameterManagedObjectVector(MyManagedObject[] param);

        [NativeMethod(ThrowsException = true)] public static extern StructManagedObjectVector ParameterStructManagedObjectVector(StructManagedObjectVector param);

        public static extern void ManagedObjectToGCHandleInNative([UnityMarshalAs(NativeType.ScriptingObjectPtr)] object param);
        public static extern void ManagedObjectMarshalledAsGCHandle([UnityMarshalAs(NativeType.GCHandle, GCHandleOptions = GCHandleOptions.Strong)] object param);

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern object GCHandleReturnsAsManagedObject(GCHandle handle);

        [return: UnityMarshalAs(NativeType.GCHandle)]
        public static extern object GCHandleReturnsMarshalledAsObject(GCHandle handle);

        [RequiredByNativeCode]
        static bool ApplyModificationToManagedObject(MyManagedObject o, int value)
        {
            o.value = value;
            return true;
        }
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
        public static extern string[] CanUnmarshallArrayOfSystemTypeArgumentToVectorOfUnityType(System.Type[] param);
        public static extern string[] CanUnmarshallArrayOfSystemTypeArgumentToVectorOfScriptingClassPtr(System.Type[] param);

        public static extern System.Type CanUnmarshallScriptingSystemTypeObjectPtrToSystemType();
        public static extern System.Type CanUnmarshallUnityTypeToSystemType();
        public static extern System.Type CanUnmarshallScriptingClassPtrToSystemType();

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
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
        public static extern string[] CanMarshallArrayOfFieldInfoArgumentToVectorOfScriptingFieldPtr(System.Reflection.FieldInfo[] param);

        public static extern StructSystemReflectionFieldInfo CanUnmarshallSystemReflectionFieldInfoStructField();
        public static extern StructSystemReflectionFieldInfoArray CanUnmarshallSystemReflectionFieldInfoArrayStructField();

        public static extern System.Reflection.FieldInfo CanUnmarshallScriptingFieldInfoObjectPtrToFieldInfo();
        public static extern System.Reflection.FieldInfo CanUnmarshallScriptingFieldPtrToFieldInfo();

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
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
        public static extern string[] CanMarshallArrayOfMethodInfoArgumentToVectorOfScriptingMethodPtr(System.Reflection.MethodInfo[] param);

        public static extern StructSystemReflectionMethodInfo CanUnmarshallSystemReflectionMethodInfoStructField();
        public static extern StructSystemReflectionMethodInfoArray CanUnmarshallSystemReflectionMethodInfoArrayStructField();

        public static extern System.Reflection.MethodInfo CanUnmarshallScriptingMethodInfoObjectPtrToMethodInfo();
        public static extern System.Reflection.MethodInfo CanUnmarshallScriptingMethodPtrToMethodInfo();

        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
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

        [NativeMethod(ThrowsException = true)] public static extern void ParameterInt(int param);

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
        [NativeMethod(ThrowsException = true)]
        public static extern void VoidReturnStringParameter(string param);

        [NativeMethod(ThrowsException = true)]
        public static extern int NonUnmarshallingReturn();

        [NativeMethod(ThrowsException = true)]
        public static extern string UnmarshallingReturn();

        [NativeMethod(ThrowsException = true)]
        public static extern StructInt BlittableStructReturn();

        [NativeMethod(ThrowsException = true)]
        public static extern StructCoreString NonblittableStructReturn();

        [NativeMethod(ThrowsException = true)]
        public static extern int PropertyThatCanThrow { get; set; }

        public static extern int PropertyGetThatCanThrow
        {
            [NativeMethod(ThrowsException = true)]
            get;
            set;
        }
        public static extern int PropertySetThatCanThrow
        {
            get;
            [NativeMethod(ThrowsException = true)]
            set;
        }
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class ExceptionTypeTests
    {
        [NativeMethod(ThrowsException = true)]
        public static extern void NullReferenceException(string nativeFormat, string values);

        [NativeMethod(ThrowsException = true)]
        public static extern void ArgumentNullException(string argumentName);

        [NativeMethod(ThrowsException = true)]
        public static extern void ArgumentException(string nativeFormat, string values);

        [NativeMethod(ThrowsException = true)]
        public static extern void InvalidOperationException(string nativeFormat, string values);

        [NativeMethod(ThrowsException = true)]
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
        [NativeMethod(ThrowsException = true)] public static extern void ParameterVectorEnum(SomeEnum[] enumArray);
        public static extern void ParameterOutVectorEnum([Out] SomeEnum[] enumArray);
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
                return string.Equals(a, otherStruct.a) && b == otherStruct.b && c == otherStruct.c;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return a.GetHashCode();
        }
    }

    [ExcludeFromDocs]
    internal struct StructWithNonBlittableListField
    {
        public List<StructWithStringIntAndFloat> list;
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

    [ExcludeFromDocs]
    internal struct StructWithNullableString
    {
        public string field;
    }

    [ExcludeFromDocs]
    internal struct StructWithNullableArray
    {
        public string[] field;
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class NonBlittableStructTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithStringIntAndFloat(StructWithStringIntAndFloat param);
        [NativeMethod(ThrowsException = true)] public static extern void RefParameterStructWithStringIntAndFloat(ref StructWithStringIntAndFloat param);
        public static extern void OutParameterStructWithStringIntAndFloat(out StructWithStringIntAndFloat param);
        public static extern void ParameterStructWithStringIntAndFloat2(StructWithStringIntAndFloat2 param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithStringIgnoredIntAndFloat(StructWithStringIgnoredIntAndFloat param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithStringIntAndFloatArray(StructWithStringIntAndFloat[] param);
        public static extern StructWithStringIntAndFloat[] ReturnStructWithStringIntAndFloatArray();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableArrayField(StructWithNonBlittableArrayField param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableArrayFieldOut(out StructWithNonBlittableArrayField param, int expectedArraySize);
        public static extern StructWithNonBlittableArrayField ReturnStructWithNonBlittableArrayField();

        [NativeMethod(ThrowsException = true)] public static extern void CanMarshalManagedObjectToStruct(ClassToStruct param);
        [NativeMethod(ThrowsException = true)] public static extern void CanMarshalOutManagedObjectToStruct([In, Out] ClassToStruct param);
        [NativeMethod(ThrowsException = true)] public static extern void CanMarshalStructWithNativeAsStructField(StructWithClassToStruct param);
        [NativeMethod(ThrowsException = true)] public static extern void CanMarshalNativeAsStructArray(ClassToStruct[] param);
        public static extern ClassToStruct CanUnmarshalManagedObjectFromStruct();
        public static extern StructWithClassToStruct CanUnmarshalStructWithNativeAsStructField();
        public static extern ClassToStruct[] CanUnmarshalNativeAsStructArray();

        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableStringInAndOutNull(StructWithNullableString param, out StructWithNullableString outputParam);
        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableArrayInAndOutNull(StructWithNullableArray param, out StructWithNullableArray outputParam);

        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableStringInAndOutEmpty(StructWithNullableString param, out StructWithNullableString outputParam);
        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableArrayInAndOutEmpty(StructWithNullableArray param, out StructWithNullableArray outputParam);

        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableStringInAndOutNotNullNotEmpty(StructWithNullableString param, out StructWithNullableString outputParam);
        [NativeMethod(ThrowsException = true)] public static extern void ParamStructWithNullableArrayInAndOutNotNullNotEmpty(StructWithNullableArray param, out StructWithNullableArray outputParam);
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
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [ExcludeFromDocs]
    internal class TypedefManagedNameTests
    {
        public static extern void ParameterStructWithTypedefManagedName(StructWithTypedefManagedName param);
    }

    // --------------------------------------------------------------------
    // Field-bound property tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
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
        public static extern void InOutArrayOfStringWhenItemIsDeletedWorks([In, Out] string[] array);

        public static extern void InOutArrayOfBlittableStructTypeWorks([In, Out] StructInt[] array, StructInt value);
        public static extern void InOutArrayOfManagedTypeWorks([In, Out] object[] array, object value);
        public static extern void InOutArrayOfIntPtrObjectTypeWorks([In, Out] MyIntPtrObject[] array, MyIntPtrObject value);
        public static extern void InOutArrayOfIntPtrObjectTypeWhenItemIsDeletedWorks([In, Out] MyIntPtrObject[] array);

        public static extern void InOutArrayOfUnityObjectTypeWorks([In, Out] MarshallingTestObject[] array, MarshallingTestObject value);
        public static extern void InOutArrayOfUnityObjectTypeWhenItemIsDeletedWorks([In, Out] MarshallingTestObject[] array);

        public static extern void InOutArrayOfUnityObjectTypePPtrWorks([In, Out] MarshallingTestObject[] array, MarshallingTestObject value);
        public static extern void InOutArrayOfUnityObjectTypePPtrWhenItemIsDeletedWorks([In, Out] MarshallingTestObject[] array);

        public static extern void InOutArrayOfNestedBlittableStructTypeWorks([In, Out] StructNestedBlittable[] array, StructNestedBlittable value);

        public static extern void InOutArrayOfNonBlittableTypeWorks([In, Out] StructWithStringIntAndFloat[] array, StructWithStringIntAndFloat value);
        public static extern void InOutArrayOfNonBlittableTypeWhenItemIsDeletedWorks([In, Out] StructWithStringIntAndFloat[] array);
    }

    [NativeHeader("Modules/Marshalling/ReturnArrayMarshallingTests.h")]
    [ExcludeFromDocs]
    internal static class ReturnArrayMarshallingTests
    {
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern float[] ReturnArrayOfPrimitiveTypeWorks_Float1D();
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
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
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
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWith8ByteAndBoolFields(StructWith8ByteAndBoolFields param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWith8ByteAndBoolFieldsArray(StructWith8ByteAndBoolFields[] param);
    }

    struct BlittableCornerCases
    {
        public char cVal;
        public bool bVal;
        public SomeEnum eVal;
    }

    struct StructWithBlittableListField
    {
        public List<int> list;
    }

    unsafe struct StructWithSelfPointer
    {
        public int value;
        public StructWithSelfPointer* other;
    }

    // --------------------------------------------------------------------
    // System.Array tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal class ValueTypeArrayTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntArrayReadOnly(int[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntArrayWritable(int[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntArrayEmpty(int[] param, int[] param2);
        public static extern void ParameterIntArrayNullExceptions([NotNull] int[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntMultidimensionalArray(int[,] param);
        public static extern void ParameterIntMultidimensionalArrayNullExceptions([NotNull] int[,] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCharArrayReadOnly(char[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterBlittableCornerCaseStructArrayReadOnly(BlittableCornerCases[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntArrayOutAttr([Out] int[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCharArrayOutAttr([Out]char[] param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterBlittableCornerCaseStructArrayOutAttr([Out]BlittableCornerCases[] param);
        public static extern int[] ParameterIntArrayReturn();
        public static extern int[] ParameterIntArrayReturnEmpty();
        public static extern int[] ParameterIntArrayReturnNull();
        public static extern char[] ParameterCharArrayReturn();
        public static extern BlittableCornerCases[] ParameterBlittableCornerCaseStructArrayReturn();

        // For CreateAndFillArray, passing the array in vs ref doesn't matter for correctness
        // But there is on reason to pass it by ref - we always update the pointer to
        // the array in the OutMarshalledArrayFiller.  With the ref case the custom marshalling
        // code will make an ConvertToManaged call that will just re-read the same pointer
        // But let's make sure we support calling by ref since that will make more sense since in implies read-only

        public static int[] CreateAndFillArray1UsingIn()
        {
            OutArray<int> outArray = new OutArray<int>();
            CreateAndFillArray1In(in outArray);
            return outArray.Value;
        }
        public static int[] CreateAndFillArray1UsingRef()
        {
            OutArray<int> outArray = new OutArray<int>();
            CreateAndFillArray1Ref(ref outArray);
            return outArray.Value;
        }

        public static int[,] CreateAndFillArray2UsingIn()
        {
            OutArray2D<int> outArray = new OutArray2D<int>();
            CreateAndFillArray2In(in outArray);
            return outArray.Value;
        }

        public static int[,] CreateAndFillArray2UsingRef()
        {
            OutArray2D<int> outArray = new OutArray2D<int>();
            CreateAndFillArray2Ref(ref outArray);
            return outArray.Value;
        }

        public static int[,,] CreateAndFillArray3UsingIn()
        {
            OutArray3D<int> outArray = new OutArray3D<int>();
            CreateAndFillArray3In(in outArray);
            return outArray.Value;
        }

        public static int[,,] CreateAndFillArray3UsingRef()
        {
            OutArray3D<int> outArray = new OutArray3D<int>();
            CreateAndFillArray3Ref(ref outArray);
            return outArray.Value;
        }

        [NativeName("CreateAndFillArray1")]
        static extern void CreateAndFillArray1In(in OutArray<int> outArray);
        [NativeName("CreateAndFillArray1")]
        static extern void CreateAndFillArray1Ref(ref OutArray<int> outArray);

        [NativeName("CreateAndFillArray2")]
        static extern void CreateAndFillArray2In(in OutArray2D<int> outArray);
        [NativeName("CreateAndFillArray2")]
        static extern void CreateAndFillArray2Ref(ref OutArray2D<int> outArray);

        [NativeName("CreateAndFillArray3")]
        static extern void CreateAndFillArray3In(in OutArray3D<int> outArray);
        [NativeName("CreateAndFillArray3")]
        static extern void CreateAndFillArray3Ref(ref OutArray3D<int> outArray);
    }

    // --------------------------------------------------------------------
    // System.Span tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal class ValueTypeSpanTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntReadOnlySpan(ReadOnlySpan<int> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterIntSpan(Span<int> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterBoolReadOnlySpan(ReadOnlySpan<bool> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCharReadOnlySpan(ReadOnlySpan<char> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterEnumReadOnlySpan(ReadOnlySpan<SomeEnum> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterBlittableCornerCaseStructReadOnlySpan(ReadOnlySpan<BlittableCornerCases> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithSelfPointerSpan(Span<StructWithSelfPointer> param);
        public static extern Span<int> ReturnsArrayRefWritableAsSpan(int val1, int val2, int val3);
        public static extern Span<int> ReturnsVectorRefAsSpan(int val1, int val2, int val3);
        public static extern Span<int> ReturnsScriptingSpanAsSpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsArrayRefWritableAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsVectorRefAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsArrayRefAsReadOnlySpan(int val1, int val2, int val3);
        public static extern ReadOnlySpan<int> ReturnsScriptingReadOnlySpanAsSpan(int val1, int val2, int val3);
    }

    // --------------------------------------------------------------------
    // Blittable System.Collections.Generic.List tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal class BlittableListOfTTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntRead(List <int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntReadChangeVaules(List <int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntAddNoGrow(List <int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntAddAndGrow(List <int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntPassNullThrow([NotNull] List <int> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntPassNullNoThrow(List<int> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntNativeAllocateSmaller(List<int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntNativeAttachOtherMemoryBlock(List<int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfIntNativeCallsClear(List<int> param, int expectedCapacity);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfBoolReadWrite(List<bool> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfCharReadWrite(List<char> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfEnumReadWrite(List<SomeEnum> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfCornerCaseStructReadWrite(List<BlittableCornerCases> param);
        [NativeMethod(ThrowsException = true)] public static extern void PamameterArrayOfStructsWithListsAddWithCapacity([In, Out] StructWithBlittableListField[] param);
    }

    // --------------------------------------------------------------------
    // Blittable System.Collections.Generic.List tests
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal class NonBlittableListOfTTests
    {
        [NativeMethod(ThrowsException = true)] public static extern void ParameterRead([In] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterReadChangeValues([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterAdd([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterPassNullThrow([NotNull,In] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterPassNullNoThrow([In] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeAllocateSmaller([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeAttachOtherMemoryBlock([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeCallsClear([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeRemovesItem([In,Out] List<StructWithStringIntAndFloat> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterOutOnly([Out] List<StructWithStringIntAndFloat> param, StructWithStringIntAndFloat item1, StructWithStringIntAndFloat item2);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableList(StructWithNonBlittableListField param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableListByRef(ref StructWithNonBlittableListField param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableListIn(in StructWithNonBlittableListField param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterStructWithNonBlittableListOut(out StructWithNonBlittableListField param, int expectedSizeOrCapacity);
        public static extern StructWithNonBlittableListField ReturnStructWithNonBlittableList();

        [NativeMethod(ThrowsException = true)] public static extern void ParameterReadUnityObjectVector([In] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterReadUnityObjectPPtrVector([In] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterReadChangeValuesUnityObjectVector([In,Out] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterReadChangeValuesUnityObjectPPtrVector([In,Out] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterAddUnityObjectVector([In,Out] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterAddUnityObjectPPtrVector([In,Out] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeRemovesItemUnityObjectVector([In,Out] List<MarshallingTestObject> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterNativeRemovesItemUnityObjectPPtrVector([In,Out] List<MarshallingTestObject> param);

        [NativeMethod(ThrowsException = true)] public static extern void ParameterTwoListOfStringAddWithCapacity([In,Out] List<string> param1, [In,Out] List<string> param2);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfStringRefAddWithCapacity([In,Out] List<string> param);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterListOfConstCharPtrAddWithCapacity([In,Out] List<string> param);
        
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCheckNullableWithNull([In] List<string> param1, [Out] List<string> param2, [In,Out] List<string> param3);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCheckNullableWithNotNullEmpty([In] List<string> param1, [Out] List<string> param2, [In,Out] List<string> param3);
        [NativeMethod(ThrowsException = true)] public static extern void ParameterCheckNullableWithNotNullNotEmpty([In] List<string> param1, [Out] List<string> param2, [In,Out] List<string> param3);
    }

    // --------------------------------------------------------------------
    // Invoke tests (calling from native to managed)
    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    internal class InvokeTests
    {
        public static extern bool TestInvokeBool(bool arg);
        public static extern sbyte TestInvokeSByte(sbyte arg);
        public static extern byte TestInvokeByte(byte arg);
        public static extern char TestInvokeChar(char arg);
        public static extern short TestInvokeShort(short arg);
        public static extern ushort TestInvokeUShort(ushort arg);
        public static extern int TestInvokeInt(int arg, ref int refArg1, out int outArg2);
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
        static int InvokeInt(int arg, ref int refArg1, out int outArg2)
        {
            outArg2 = arg + refArg1;
            refArg1 = arg - 1;
            return arg + 1;
        }
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

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [Flags]
    internal enum Test_AccessibilityRole : ushort
    {
        Button = 1 << 0,
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [Flags]
    internal enum Test_AccessibilityState : byte
    {
        Selected = 1 << 1,
    }

    [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Test_AccessibilityNodeData
    {
        public int[] childIds { get; set; }
        public string label { get; set; }
        public string value { get; set; }
        public string hint { get; set; }
        public Rect frame { get; set; }
        public int nodeId { get; set; }
        public int parentId { get; set; }
        public Test_AccessibilityRole role { get; set; }
        public Test_AccessibilityState state { get; set; }
        public bool isActive { get; set; }
        public bool allowsDirectInteraction { get; set; }
        public bool implementsInvoked { get; set; }
        public bool implementsScrolled { get; set; }
        public bool implementsDismissed { get; set; }
    }

    // Test proxy calls taking and returning-by-ref real world non-blittable types
    internal class NonBlittableProxyParameterTests
    {
        static void VerifyEqual(object expectation, object actual, string fieldName)
        {
            if (!actual.Equals(expectation))
                throw new Exception($"Expected '{expectation}' but got '{actual}' for field '{fieldName}'");
        }

        [NativeHeader("Modules/Marshalling/MarshallingTests.h")]
        [FreeFunction("Test_GetAccessibilityNodeData")]
        extern static void Test_GetAccessibilityNodeData(IntPtr nodeDataPtr, Test_AccessibilityNodeData nodeData);

        [RequiredByNativeCode]
        static void GetAccessibilityNodeData(IntPtr nodeDataPtr)
        {
            Test_AccessibilityNodeData nodeData = new Test_AccessibilityNodeData()
            {
                nodeId = 123,
                isActive = true,
                label = "TestLabel",
                value = "TestValue",
                hint = "TestHint",
                allowsDirectInteraction = true,
                frame = new Rect(10, 20, 110, 220),
                parentId = 456,
                role = Test_AccessibilityRole.Button,
                state = Test_AccessibilityState.Selected,
                childIds = new int[] { 101, 102 },
                implementsDismissed = true,
                implementsInvoked = true,
                implementsScrolled = true,
            };

            Test_GetAccessibilityNodeData(nodeDataPtr, nodeData);
        }
    }

    internal static class CustomMarshallingTests
    {
        [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BindingsMarshaller))]
        public class CustomMarshalledClass : ICustomMarshalled
        {
            public string Value { get; set; }

            public static class BindingsMarshaller
            {
                public static int ConvertToUnmanaged(CustomMarshalledClass c) => c == null ? 0 : int.Parse(c.Value);
                public static CustomMarshalledClass ConvertToManaged(int n) => new CustomMarshalledClass { Value = n.ToString() };
            }
        }

        public class CustomMarshalledDerivedClass : CustomMarshalledClass { }

        public interface ICustomMarshalled
        {
            public string Value { get; set; }
        }

        public class CustomMarshaller
        {
            public static int ConvertToUnmanaged(CustomMarshalledClass c) => c == null ? 0 : int.Parse(c.Value) * 2;
            public static CustomMarshalledClass ConvertToManaged(int n) => new CustomMarshalledClass { Value = (n * 2).ToString() };
        }

        public class CustomMarshallerUsingInParameters
        {
            public static int ConvertToUnmanaged(in CustomMarshalledClass c) => c == null ? 0 : int.Parse(c.Value) * 2;
            public static CustomMarshalledClass ConvertToManaged(in int n) => new CustomMarshalledClass { Value = (n * 2).ToString() };
        }

        public class CustomMarshaller_NeeedingMarshalling
        {
            public static string ConvertToUnmanaged(CustomMarshalledClass c) => c == null ? null : c.Value + "_ConvertedToUnmanaged";
            public static CustomMarshalledClass ConvertToManaged(string s) => new CustomMarshalledClass { Value = s + "_ConvertedToManaged" };
        }

        public class CustomMarshaller_WithFree
        {
            private static int _lastFreeValue = int.MinValue;
            public static int GetLastFreeValue() => _lastFreeValue;
            public static int ConvertToUnmanaged(CustomMarshalledClass c) => c == null ? 0 : int.Parse(c.Value) * 3;
            public static CustomMarshalledClass ConvertToManaged(int n) => new CustomMarshalledClass { Value = (n * 3).ToString() };

            public static void Free(int value)
            {
                _lastFreeValue = value;
            }
        }

        public class CustomMarshallerGeneric<T> where T: ICustomMarshalled, new()
        {
            public static int ConvertToUnmanaged(T c) => c == null ? 0 : int.Parse(c.Value) * 2;
            public static T ConvertToManaged(int n) => new T() { Value = (n * 2).ToString() };
        }

        public class CustomMarshallerInterface
        {
            public static int ConvertToUnmanaged(ICustomMarshalled c) => c == null ? 0 : int.Parse(c.Value) * 2;
        }

        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalled(CustomMarshalledClass arg, int expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalledIn(in CustomMarshalledClass arg, int expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalledOut(out CustomMarshalledClass arg, int expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalledRef(ref CustomMarshalledClass arg, int expected);
        public static extern CustomMarshalledClass ParameterCustomMarshalledReturn(int value);

        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_Attribute([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller))] CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledIn", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledIn_Attribute([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller))] in CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledOut", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledOut_Attribute([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller))] out CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledRef", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledRef_Attribute([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller))] ref CustomMarshalledClass arg, int expected);

        [NativeMethod("ParameterCustomMarshalledReturn")]
        [return: UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller))]
        public static extern CustomMarshalledClass ParameterCustomMarshalledReturn_Attribute(int value);

        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_CustomMarshallerUsesInParameters([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerUsingInParameters))] CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledIn", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledIn_CustomMarshallerUsesInParameters([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerUsingInParameters))] in CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledOut", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledOut_CustomMarshallerUsesInParameters([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerUsingInParameters))] out CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledRef", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledRef_CustomMarshallerUsesInParameters([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerUsingInParameters))] ref CustomMarshalledClass arg, int expected);

        [NativeMethod("ParameterCustomMarshalledReturn")]
        [return: UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerUsingInParameters))]
        public static extern CustomMarshalledClass ParameterCustomMarshalledReturn_CustomMarshallerUsesInParameters(int value);

        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_Free([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_WithFree))] CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledIn", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledIn_Free([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_WithFree))] in CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledOut", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledOut_Free([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_WithFree))] out CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledRef", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledRef_Free([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_WithFree))] ref CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledReturn")]
        [return: UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_WithFree))]
        public static extern CustomMarshalledClass ParameterCustomMarshalledReturn_Free(int value);

        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_NeedingMarshalling([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_NeeedingMarshalling))] CustomMarshalledClass arg, string expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_NeedingMarshalling_In([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_NeeedingMarshalling))] in CustomMarshalledClass arg, string expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_NeedingMarshalling_Out([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_NeeedingMarshalling))] out CustomMarshalledClass arg, string expected);
        [NativeMethod(ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_NeedingMarshalling_Ref([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_NeeedingMarshalling))] ref CustomMarshalledClass arg, string expected);
        [return: UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshaller_NeeedingMarshalling))]
        public static extern CustomMarshalledClass ParameterCustomMarshalled_NeedingMarshalling_Return(string value);


        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_GenericMarshaller([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))] CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledIn", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledIn_GenericMarshaller([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))] in CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledOut", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledOut_GenericMarshaller([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))] out CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledRef", ThrowsException = true)]
        public static extern void ParameterCustomMarshalledRef_GenericMarshaller([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))] ref CustomMarshalledClass arg, int expected);
        [NativeMethod("ParameterCustomMarshalledReturn")]
        [return: UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))]
        public static extern CustomMarshalledClass ParameterCustomMarshalledReturn_GenericMarshaller(int value);

        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_DerivedType([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerGeneric<CustomMarshalledClass>))] CustomMarshalledDerivedClass arg, int expected);

        [NativeMethod("ParameterCustomMarshalled", ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_InterfaceMarshaller([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(CustomMarshallerInterface))] CustomMarshalledClass arg, int expected);

        [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BindingsMarshaller))]
        public struct CustomMarshalledAsStruct
        {
            public int field;

            public static class BindingsMarshaller
            {
                public static StructInt ConvertToUnmanaged(in CustomMarshalledAsStruct s)
                {
                    return new StructInt { field = s.field };
                }

                public static CustomMarshalledAsStruct ConvertToManaged(in StructInt s)
                {
                    return new CustomMarshalledAsStruct { field = s.field };
                }
            }
        }

        [NativeMethod(nameof(BlittableStructTests) + "::" + nameof(BlittableStructTests.ParameterStructInt), isFreeFunction: true, ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_AsStruct(CustomMarshalledAsStruct arg);
        [NativeMethod(nameof(BlittableStructTests) + "::" + nameof(BlittableStructTests.ParameterStructIntIn), isFreeFunction: true, ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_AsStruct_In(in CustomMarshalledAsStruct arg);
        [NativeMethod(nameof(BlittableStructTests) + "::" + nameof(BlittableStructTests.ParameterStructIntOut), isFreeFunction: true, ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_AsStruct_Out(out CustomMarshalledAsStruct arg);
        [NativeMethod(nameof(BlittableStructTests) + "::" + nameof(BlittableStructTests.ParameterStructIntByRef), isFreeFunction: true, ThrowsException = true)]
        public static extern void ParameterCustomMarshalled_AsStruct_Ref(ref CustomMarshalledAsStruct arg);
        [NativeMethod(nameof(BlittableStructTests) + "::" + nameof(BlittableStructTests.ReturnStructInt), isFreeFunction: true)]
        public static extern CustomMarshalledAsStruct ParameterCustomMarshalled_AsStruct_Return();

        public class MarshalThisAsStructInt
        {
            public int field;

            static class BindingsMarshaller
            {
                public static StructInt ConvertToUnmanaged(MarshalThisAsStructInt s) => new StructInt { field = s.field };
            }

            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(BindingsMarshaller))]
            public extern int GetField();
        }

        [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(ClassWithPinnableInnerData))]
        public class ClassWithPinnableInnerData
        {
            public StructInt NativeData;

            internal static ref StructInt GetPinnableReference(ClassWithPinnableInnerData c)
            {
                return ref c.NativeData;
            }

            internal static StructInt ConvertToUnmanaged(ClassWithPinnableInnerData data)
            {
                return data.NativeData;
            }
        }

        [NativeMethod("BlittableStructTests::ParameterStructIntByRef", IsFreeFunction = true, ThrowsException = true)]
        public extern static void PassClassWithPinnableInnerData_PinnedRef(ClassWithPinnableInnerData c);

        [NativeMethod("BlittableStructTests::ParameterStructIntVector", IsFreeFunction = true, ThrowsException = true)]
        public extern static void PassClassWithPinnableInnerData_AsArray(ClassWithPinnableInnerData[] arr);
    }
    internal class BlittableNestedCollectionMarshallerTests
    {
        [NativeMethod("BlittableNestedCollectionMarshallerTests::PassInNestedCollection", ThrowsException = true)]
        public extern static void PassInNestedLists([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BlittableNestedCollectionMarshaller<int>))] List<List<int>> nested, int exectedCount, int[] expectedValues1, int[] expectedValues2);

        [NativeMethod("BlittableNestedCollectionMarshallerTests::PassInNestedCollection", ThrowsException = true)]
        public extern static void PassInNestedArrays([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BlittableNestedCollectionMarshaller<int>))] int[][] nested, int exectedCount, int[] expectedValues1, int[] expectedValues2);

        [NativeMethod("BlittableNestedCollectionMarshallerTests::PassInNestedCollection", ThrowsException = true)]
        public extern static void PassInListOfInts([UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BlittableNestedCollectionMarshaller<int>))] List<int[]> nested, int exectedCount, int[] expectedValues1, int[] expectedValues2);
    }

    internal static class ObjectAsGCHandleMarshallingTests
    {
        [NativeName("ObjectAsGCHandleParameter")]
        [NativeMethod(ThrowsException = true)]
        extern static void ObjectAsStrongGCHandleParameter([UnityMarshalAs(NativeType.GCHandle, GCHandleOptions = GCHandleOptions.Strong)] object obj, bool hasTarget);

        [NativeName("ObjectAsGCHandleParameter")]
        [NativeMethod(ThrowsException = true)]
        extern static void ObjectAsWeakGCHandleParameter([UnityMarshalAs(NativeType.GCHandle, GCHandleOptions = GCHandleOptions.Weak)] object obj, bool hasTarget);

        [NativeName("ObjectAsGCHandleParameter")]
        [NativeMethod(ThrowsException = true)]
        extern static void ObjectAsPinnedGCHandleParameter([UnityMarshalAs(NativeType.GCHandle, GCHandleOptions = GCHandleOptions.Pinned)] object obj, bool hasTarget);

        public static void ObjectAsGCHandleParameter(object obj, GCHandleType type)
        {
            switch (type)
            {
                case GCHandleType.Normal:
                    ObjectAsStrongGCHandleParameter(obj, obj != null);
                    break;
                case GCHandleType.Weak:
                    ObjectAsWeakGCHandleParameter(obj, obj != null);
                    break;
                case GCHandleType.Pinned:
                    ObjectAsPinnedGCHandleParameter(obj, obj != null);
                    break;
                default:
                    throw new ArgumentException("Unsupported GCHandleType", nameof(type));
            }
        }

        [return: UnityMarshalAs(NativeType.GCHandle)]
        extern static object ReturnObjectAsGCHandle([UnityMarshalAs(NativeType.ScriptingObjectPtr)] object obj, out IntPtr rawHandle);

        public static object ReturnObjectAsGCHandle(object obj)
        {
            var outObj = ReturnObjectAsGCHandle(obj, out var rawHandle);
            if (rawHandle != IntPtr.Zero)
                GCHandle.FromIntPtr(rawHandle).Free();
            return outObj;
        }
    }

    internal static class GCHandleMarshallingTests
    {
        [NativeMethod(ThrowsException = true)]
        public extern static void GCHandleParameter(GCHandle handle, bool hasTarget);

        public extern static GCHandle GCHandleReturn(GCHandle handle);
    }

    internal static class MarshallingTests
    {
        [FreeFunction("MarshallingTest::DisableMarshallingTestsVerification")]
        public static extern void DisableMarshallingTestsVerification();
    }

    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "CustomNativeMarshallerAlwaysThrows")]
    struct CustomNativeMarshallerAlwaysThrows
    {
    }

    internal class CustomNativeMarshalingTests
    {
        public static extern void CallWithCustomNativeMarshallerAlwaysThrows(CustomNativeMarshallerAlwaysThrows param);
    }
}
