// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Explicit)]
    public struct jvalue
    {
        [FieldOffset(0)]    public bool    z;
        [FieldOffset(0)]    public byte    b;
        [FieldOffset(0)]    public char    c;
        [FieldOffset(0)]    public short   s;
        [FieldOffset(0)]    public int     i;
        [FieldOffset(0)]    public long    j;
        [FieldOffset(0)]    public float   f;
        [FieldOffset(0)]    public double  d;
        [FieldOffset(0)]    public System.IntPtr  l;
    }

    public class AndroidJNIHelper
    {
        private AndroidJNIHelper() {}
        public static bool debug { get { return false; } set {} }
        public static IntPtr GetConstructorID(IntPtr javaClass) { return IntPtr.Zero; }
        public static IntPtr GetConstructorID(IntPtr javaClass, [UnityEngine.Internal.DefaultValue("")] string signature) { return IntPtr.Zero; }
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName) { return IntPtr.Zero; }
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [UnityEngine.Internal.DefaultValue("")] string signature) { return IntPtr.Zero; }
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [UnityEngine.Internal.DefaultValue("")] string signature, [UnityEngine.Internal.DefaultValue("false")] bool isStatic) { return IntPtr.Zero; }
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName) { return IntPtr.Zero; }
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [UnityEngine.Internal.DefaultValue("")] string signature) { return IntPtr.Zero; }
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [UnityEngine.Internal.DefaultValue("")] string signature, [UnityEngine.Internal.DefaultValue("false")] bool isStatic) { return IntPtr.Zero; }
        public static IntPtr CreateJavaRunnable(AndroidJavaRunnable jrunnable) { return IntPtr.Zero; }
        public static IntPtr CreateJavaProxy(AndroidJavaProxy proxy) { return IntPtr.Zero; }
        public static IntPtr ConvertToJNIArray(System.Array array) { return IntPtr.Zero; }
        public static jvalue[] CreateJNIArgArray(object[] args) { return null; }
        public static void DeleteJNIArgArray(object[] args, jvalue[] jniArgs) {}
        public static System.IntPtr GetConstructorID(System.IntPtr jclass, object[] args) { return IntPtr.Zero; }
        public static System.IntPtr GetMethodID(System.IntPtr jclass, string methodName, object[] args, bool isStatic) { return IntPtr.Zero; }
        public static string GetSignature(object obj) { return ""; }
        public static string GetSignature(object[] args) { return ""; }
        public static ArrayType ConvertFromJNIArray<ArrayType>(IntPtr array) { return default(ArrayType); }
        public static System.IntPtr GetMethodID<ReturnType>(System.IntPtr jclass, string methodName, object[] args, bool isStatic) { return IntPtr.Zero; }
        public static System.IntPtr GetFieldID<FieldType>(System.IntPtr jclass, string fieldName, bool isStatic) { return IntPtr.Zero; }
        public static string GetSignature<ReturnType>(object[] args) { return ""; }
    }

    public class AndroidJNI
    {
        private AndroidJNI() {}
        public static int AttachCurrentThread() { return 0; }
        public static int DetachCurrentThread() { return 0; }
        public static int GetVersion() { return 0; }
        public static IntPtr FindClass(string name) { return IntPtr.Zero; }
        public static IntPtr FromReflectedMethod(IntPtr refMethod) { return IntPtr.Zero; }
        public static IntPtr FromReflectedField(IntPtr refField) { return IntPtr.Zero; }
        public static IntPtr ToReflectedMethod(IntPtr clazz, IntPtr methodID, bool isStatic) { return IntPtr.Zero; }
        public static IntPtr ToReflectedField(IntPtr clazz, IntPtr fieldID, bool isStatic) { return IntPtr.Zero; }
        public static IntPtr GetSuperclass(IntPtr clazz) { return IntPtr.Zero; }
        public static bool IsAssignableFrom(IntPtr clazz1, IntPtr clazz2) { return false; }
        public static int Throw(IntPtr obj) { return 0; }
        public static int ThrowNew(IntPtr clazz, string message) { return 0; }
        public static IntPtr ExceptionOccurred() { return IntPtr.Zero; }
        public static void ExceptionDescribe() {}
        public static void ExceptionClear() {}
        public static void FatalError(string message) {}
        public static int PushLocalFrame(int capacity) { return 0; }
        public static IntPtr PopLocalFrame(IntPtr ptr) { return IntPtr.Zero; }
        public static IntPtr NewGlobalRef(IntPtr obj) { return IntPtr.Zero; }
        public static void DeleteGlobalRef(IntPtr obj) {}
        public static IntPtr NewLocalRef(IntPtr obj) { return IntPtr.Zero; }
        public static void DeleteLocalRef(IntPtr obj) {}
        public static bool IsSameObject(IntPtr obj1, IntPtr obj2) { return false; }
        public static int EnsureLocalCapacity(int capacity) { return 0; }
        public static IntPtr AllocObject(IntPtr clazz) { return IntPtr.Zero; }
        public static IntPtr NewObject(IntPtr clazz, IntPtr methodID, jvalue[] args) { return IntPtr.Zero; }
        public static IntPtr GetObjectClass(IntPtr obj) { return IntPtr.Zero; }
        public static bool IsInstanceOf(IntPtr obj, IntPtr clazz) { return false; }
        public static IntPtr GetMethodID(IntPtr clazz, string name, string sig) { return IntPtr.Zero; }
        public static IntPtr GetFieldID(IntPtr clazz, string name, string sig) { return IntPtr.Zero; }
        public static IntPtr GetStaticMethodID(IntPtr clazz, string name, string sig) { return IntPtr.Zero; }
        public static IntPtr GetStaticFieldID(IntPtr clazz, string name, string sig) { return IntPtr.Zero; }
        public static IntPtr NewStringUTF(string bytes) { return IntPtr.Zero; }
        public static int GetStringUTFLength(IntPtr str) { return 0; }
        public static string GetStringUTFChars(IntPtr str) { return ""; }
        public static string CallStringMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return ""; }
        public static IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return IntPtr.Zero; }
        public static Int32 CallIntMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static bool CallBooleanMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return false; }
        public static Int16 CallShortMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static Byte CallByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static Char CallCharMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return '0'; }
        public static float CallFloatMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static double CallDoubleMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static Int64 CallLongMethod(IntPtr obj, IntPtr methodID, jvalue[] args) { return 0; }
        public static void CallVoidMethod(IntPtr obj, IntPtr methodID, jvalue[] args) {}
        public static string GetStringField(IntPtr obj, IntPtr fieldID) { return ""; }
        public static IntPtr GetObjectField(IntPtr obj, IntPtr fieldID) { return IntPtr.Zero; }
        public static bool GetBooleanField(IntPtr obj, IntPtr fieldID) { return false; }
        public static Byte GetByteField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static Char GetCharField(IntPtr obj, IntPtr fieldID) { return '0'; }
        public static Int16 GetShortField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static Int32 GetIntField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static Int64 GetLongField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static float GetFloatField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static double GetDoubleField(IntPtr obj, IntPtr fieldID) { return 0; }
        public static void SetStringField(IntPtr obj, IntPtr fieldID, string val) {}
        public static void SetObjectField(IntPtr obj, IntPtr fieldID, IntPtr val) {}
        public static void SetBooleanField(IntPtr obj, IntPtr fieldID, bool val) {}
        public static void SetByteField(IntPtr obj, IntPtr fieldID, Byte val) {}
        public static void SetCharField(IntPtr obj, IntPtr fieldID, Char val) {}
        public static void SetShortField(IntPtr obj, IntPtr fieldID, Int16 val) {}
        public static void SetIntField(IntPtr obj, IntPtr fieldID, Int32 val) {}
        public static void SetLongField(IntPtr obj, IntPtr fieldID, Int64 val) {}
        public static void SetFloatField(IntPtr obj, IntPtr fieldID, float val) {}
        public static void SetDoubleField(IntPtr obj, IntPtr fieldID, double val) {}
        public static string CallStaticStringMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return ""; }
        public static IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return IntPtr.Zero; }
        public static Int32 CallStaticIntMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static bool CallStaticBooleanMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return false; }
        public static Int16 CallStaticShortMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static Byte CallStaticByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static Char CallStaticCharMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return '0'; }
        public static float CallStaticFloatMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static double CallStaticDoubleMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static Int64 CallStaticLongMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) { return 0; }
        public static void CallStaticVoidMethod(IntPtr clazz, IntPtr methodID, jvalue[] args) {}
        public static string GetStaticStringField(IntPtr clazz, IntPtr fieldID) { return ""; }
        public static IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID) { return IntPtr.Zero; }
        public static bool GetStaticBooleanField(IntPtr clazz, IntPtr fieldID) { return false; }
        public static Byte GetStaticByteField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static Char GetStaticCharField(IntPtr clazz, IntPtr fieldID) { return '0'; }
        public static Int16 GetStaticShortField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static Int32 GetStaticIntField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static Int64 GetStaticLongField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static float GetStaticFloatField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static double GetStaticDoubleField(IntPtr clazz, IntPtr fieldID) { return 0; }
        public static void SetStaticStringField(IntPtr clazz, IntPtr fieldID, string val) {}
        public static void SetStaticObjectField(IntPtr clazz, IntPtr fieldID, IntPtr val) {}
        public static void SetStaticBooleanField(IntPtr clazz, IntPtr fieldID, bool val) {}
        public static void SetStaticByteField(IntPtr clazz, IntPtr fieldID, Byte val) {}
        public static void SetStaticCharField(IntPtr clazz, IntPtr fieldID, Char val) {}
        public static void SetStaticShortField(IntPtr clazz, IntPtr fieldID, Int16 val) {}
        public static void SetStaticIntField(IntPtr clazz, IntPtr fieldID, Int32 val) {}
        public static void SetStaticLongField(IntPtr clazz, IntPtr fieldID, Int64 val) {}
        public static void SetStaticFloatField(IntPtr clazz, IntPtr fieldID, float val) {}
        public static void SetStaticDoubleField(IntPtr clazz, IntPtr fieldID, double val) {}
        public static IntPtr ToBooleanArray(Boolean[] array)  { return IntPtr.Zero; }
        public static IntPtr ToByteArray(Byte[] array) { return IntPtr.Zero; }
        public static IntPtr ToCharArray(Char[] array) { return IntPtr.Zero; }
        public static IntPtr ToShortArray(Int16[] array) { return IntPtr.Zero; }
        public static IntPtr ToIntArray(Int32[] array) { return IntPtr.Zero; }
        public static IntPtr ToLongArray(Int64[] array) { return IntPtr.Zero; }
        public static IntPtr ToFloatArray(float[] array) { return IntPtr.Zero; }
        public static IntPtr ToDoubleArray(double[] array) { return IntPtr.Zero; }
        public static IntPtr ToObjectArray(IntPtr[] array, IntPtr arrayClass) { return IntPtr.Zero; }
        public static IntPtr ToObjectArray(IntPtr[] array) { return IntPtr.Zero; }
        public static Boolean[] FromBooleanArray(IntPtr array) { return null; }
        public static Byte[] FromByteArray(IntPtr array) { return null; }
        public static Char[] FromCharArray(IntPtr array) { return null; }
        public static Int16[] FromShortArray(IntPtr array) { return null; }
        public static Int32[] FromIntArray(IntPtr array) { return null; }
        public static Int64[] FromLongArray(IntPtr array) { return null; }
        public static float[] FromFloatArray(IntPtr array) { return null; }
        public static double[] FromDoubleArray(IntPtr array) { return null; }
        public static IntPtr[] FromObjectArray(IntPtr array) { return null; }
        public static int GetArrayLength(IntPtr array) { return 0; }
        public static IntPtr NewBooleanArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewByteArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewCharArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewShortArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewIntArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewLongArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewFloatArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewDoubleArray(int size) { return IntPtr.Zero; }
        public static IntPtr NewObjectArray(int size, IntPtr clazz, IntPtr obj) { return IntPtr.Zero; }
        public static bool GetBooleanArrayElement(IntPtr array, int index) { return false; }
        public static Byte GetByteArrayElement(IntPtr array, int index) { return 0; }
        public static Char GetCharArrayElement(IntPtr array, int index) { return '0'; }
        public static Int16 GetShortArrayElement(IntPtr array, int index) { return 0; }
        public static Int32 GetIntArrayElement(IntPtr array, int index) { return 0; }
        public static Int64 GetLongArrayElement(IntPtr array, int index) { return 0; }
        public static float GetFloatArrayElement(IntPtr array, int index) { return 0; }
        public static double GetDoubleArrayElement(IntPtr array, int index) { return 0; }
        public static IntPtr GetObjectArrayElement(IntPtr array, int index) { return IntPtr.Zero; }
        public static void SetBooleanArrayElement(IntPtr array, int index, byte val) {}
        public static void SetByteArrayElement(IntPtr array, int index, sbyte val) {}
        public static void SetCharArrayElement(IntPtr array, int index, Char val) {}
        public static void SetShortArrayElement(IntPtr array, int index, Int16 val) {}
        public static void SetIntArrayElement(IntPtr array, int index, Int32 val) {}
        public static void SetLongArrayElement(IntPtr array, int index, Int64 val) {}
        public static void SetFloatArrayElement(IntPtr array, int index, float val) {}
        public static void SetDoubleArrayElement(IntPtr array, int index, double val) {}
        public static void SetObjectArrayElement(IntPtr array, int index, IntPtr obj) {}
    }
}
