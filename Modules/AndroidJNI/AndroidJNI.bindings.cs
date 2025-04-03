// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Android;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Explicit)]
    [NativeType(CodegenOptions.Custom, "ScriptingJvalue")]
    public struct jvalue
    {
        [FieldOffset(0)]    public bool    z;
        [FieldOffset(0)]    public sbyte   b;
        [FieldOffset(0)]    public char    c;
        [FieldOffset(0)]    public short   s;
        [FieldOffset(0)]    public int     i;
        [FieldOffset(0)]    public long    j;
        [FieldOffset(0)]    public float   f;
        [FieldOffset(0)]    public double  d;
        [FieldOffset(0)]    public IntPtr  l;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "ScriptingJNINativeMethod")]
    public struct JNINativeMethod
    {
        public string name;
        public string signature;
        public IntPtr fnPtr;
    }

    // Helper interface for JNI interaction; signature creation and method lookups
    [UsedByNativeCode]
    [NativeHeader("Modules/AndroidJNI/Public/AndroidJNIBindingsHelpers.h")]
    [StaticAccessor("AndroidJNIBindingsHelpers", StaticAccessorType.DoubleColon)]
    [NativeConditional("PLATFORM_ANDROID")]
    public static class AndroidJNIHelper
    {
        // Set /debug/ to true to log calls through the AndroidJNIHelper
        public static extern bool debug { get; set; }

        // Scans a particular Java class for a constructor method matching a signature.
        public static IntPtr GetConstructorID(IntPtr javaClass)
        {
            return GetConstructorID(javaClass, "");
        }

        // Scans a particular Java class for a constructor method matching a signature.
        public static IntPtr GetConstructorID(IntPtr javaClass, [DefaultValue("")] string signature)
        {
            return _AndroidJNIHelper.GetConstructorID(javaClass, signature);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName)
        {
            return GetMethodID(javaClass, methodName, "", false);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [DefaultValue("")] string signature)
        {
            return GetMethodID(javaClass, methodName, signature, false);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [DefaultValue("")] string signature, [DefaultValue("false")] bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID(javaClass, methodName, signature, isStatic);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName)
        {
            return GetFieldID(javaClass, fieldName, "", false);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [DefaultValue("")] string signature)
        {
            return GetFieldID(javaClass, fieldName, signature, false);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [DefaultValue("")] string signature, [DefaultValue("false")] bool isStatic)
        {
            return _AndroidJNIHelper.GetFieldID(javaClass, fieldName, signature, isStatic);
        }

        // Creates a UnityJavaRunnable object (implements java.lang.Runnable).
        public static IntPtr CreateJavaRunnable(AndroidJavaRunnable jrunnable)
        {
            return _AndroidJNIHelper.CreateJavaRunnable(jrunnable);
        }

        // Creates a UnityJavaProxy object (implements jinterface).
        public static IntPtr CreateJavaProxy(AndroidJavaProxy proxy)
        {
            var handle = GCHandle.Alloc(proxy);
            try
            {
                return _AndroidJNIHelper.CreateJavaProxy(AndroidApplication.UnityPlayerRaw, GCHandle.ToIntPtr(handle), proxy);
            }
            catch
            {
                handle.Free();
                throw;
            }
        }

        // Creates a Java array from a managed array
        public static IntPtr ConvertToJNIArray(Array array)
        {
            return _AndroidJNIHelper.ConvertToJNIArray(array);
        }

        // Creates the parameter array to be used as argument list when invoking Java code through CallMethod() in AndroidJNI.
        public static jvalue[] CreateJNIArgArray(object[] args)
        {
            jvalue[] ret = new jvalue[args.Length];
            _AndroidJNIHelper.CreateJNIArgArray(args, ret);
            return ret;
        }

        public static void CreateJNIArgArray(object[] args, Span<jvalue> jniArgs)
        {
            if (args.Length != jniArgs.Length)
                throw new ArgumentException($"Both arrays must be of the same length, but are {args.Length} and {jniArgs.Length}");
            _AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
        }

        // Deletes any local jni references previously allocated by CreateJNIArgArray()
        //
        // @param jniArgs the array returned by CreateJNIArgArray()
        // @param args the array of arguments used as a parameter to CreateJNIArgArray()
        //
        public static void DeleteJNIArgArray(object[] args, jvalue[] jniArgs)
        {
            _AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
        }

        public static void DeleteJNIArgArray(object[] args, Span<jvalue> jniArgs)
        {
            _AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
        }

        // Get a JNI method ID for a constructor based on calling arguments.
        // Scans a particular Java class for a constructor method matching a signature based on passed arguments.
        // The signature comparison is done to allow for sub-/base-classes of the class types.
        //
        // @param javaClass Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).
        // @param args Array with parameters to be passed to the constructor when invoked.
        //
        public static IntPtr GetConstructorID(IntPtr jclass, object[] args)
        {
            return _AndroidJNIHelper.GetConstructorID(jclass, args);
        }

        // Get a JNI method ID based on calling arguments.
        public static IntPtr GetMethodID(IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID(jclass, methodName, args, isStatic);
        }

        // Creates the JNI signature string for particular object type
        public static string GetSignature(object obj)
        {
            return _AndroidJNIHelper.GetSignature(obj);
        }

        // Creates the JNI signature string for an object parameter list.
        public static string GetSignature(object[] args)
        {
            return _AndroidJNIHelper.GetSignature(args);
        }

        //===================================================================

        // Creates a managed array from a Java array
        public static ArrayType ConvertFromJNIArray<ArrayType>(IntPtr array)
        {
            return _AndroidJNIHelper.ConvertFromJNIArray<ArrayType>(array);
        }

        // Get a JNI method ID based on calling arguments.
        public static IntPtr GetMethodID<ReturnType>(IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID<ReturnType>(jclass, methodName, args, isStatic);
        }

        // Get a JNI field ID based on type detection. Generic parameter represents the field type.
        public static IntPtr GetFieldID<FieldType>(IntPtr jclass, string fieldName, bool isStatic)
        {
            return _AndroidJNIHelper.GetFieldID<FieldType>(jclass, fieldName, isStatic);
        }

        // Creates the JNI signature string for an object parameter list.
        public static string GetSignature<ReturnType>(object[] args)
        {
            return _AndroidJNIHelper.GetSignature<ReturnType>(args);
        }

        // Box primitive to a boxed object
        static IntPtr Box(jvalue val, string boxedClass, string signature)
        {
            IntPtr clazz = AndroidJNISafe.FindClass(boxedClass);
            try
            {
                IntPtr method = AndroidJNISafe.GetStaticMethodID(clazz, "valueOf", signature);
                unsafe
                {
                    var args = new Span<jvalue>(&val, 1);
                    return AndroidJNISafe.CallStaticObjectMethod(clazz, method, args);
                }
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(clazz);
            }
        }

        public static IntPtr Box(sbyte value)
        {
            jvalue val = default;
            val.b = value;
            return Box(val, "java/lang/Byte", "(B)Ljava/lang/Byte;");
        }

        public static IntPtr Box(short value)
        {
            jvalue val = default;
            val.s = value;
            return Box(val, "java/lang/Short", "(S)Ljava/lang/Short;");
        }

        public static IntPtr Box(int value)
        {
            jvalue val = default;
            val.i = value;
            return Box(val, "java/lang/Integer", "(I)Ljava/lang/Integer;");
        }

        public static IntPtr Box(long value)
        {
            jvalue val = default;
            val.j = value;
            return Box(val, "java/lang/Long", "(J)Ljava/lang/Long;");
        }

        public static IntPtr Box(float value)
        {
            jvalue val = default;
            val.f = value;
            return Box(val, "java/lang/Float", "(F)Ljava/lang/Float;");
        }

        public static IntPtr Box(double value)
        {
            jvalue val = default;
            val.d = value;
            return Box(val, "java/lang/Double", "(D)Ljava/lang/Double;");
        }

        public static IntPtr Box(char value)
        {
            jvalue val = default;
            val.c = value;
            return Box(val, "java/lang/Character", "(C)Ljava/lang/Character;");
        }

        public static IntPtr Box(bool value)
        {
            jvalue val = default;
            val.z = value;
            return Box(val, "java/lang/Boolean", "(Z)Ljava/lang/Boolean;");
        }

        // Unbox a primitive from boxed counterpart

        static IntPtr GetUnboxMethod(IntPtr obj, string methodName, string signature)
        {
            IntPtr clazz = AndroidJNISafe.GetObjectClass(obj);
            try
            {
                return AndroidJNISafe.GetMethodID(clazz, methodName, signature);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(clazz);
            }
        }

        public static void Unbox(IntPtr obj, out sbyte value)
        {
            IntPtr method = GetUnboxMethod(obj, "byteValue", "()B");
            value = AndroidJNISafe.CallSByteMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out short value)
        {
            IntPtr method = GetUnboxMethod(obj, "shortValue", "()S");
            value = AndroidJNISafe.CallShortMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out int value)
        {
            IntPtr method = GetUnboxMethod(obj, "intValue", "()I");
            value = AndroidJNISafe.CallIntMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out long value)
        {
            IntPtr method = GetUnboxMethod(obj, "longValue", "()J");
            value = AndroidJNISafe.CallLongMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out float value)
        {
            IntPtr method = GetUnboxMethod(obj, "floatValue", "()F");
            value = AndroidJNISafe.CallFloatMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out double value)
        {
            IntPtr method = GetUnboxMethod(obj, "doubleValue", "()D");
            value = AndroidJNISafe.CallDoubleMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out char value)
        {
            IntPtr method = GetUnboxMethod(obj, "charValue", "()C");
            value = AndroidJNISafe.CallCharMethod(obj, method, new Span<jvalue>());
        }

        public static void Unbox(IntPtr obj, out bool value)
        {
            IntPtr method = GetUnboxMethod(obj, "booleanValue", "()Z");
            value = AndroidJNISafe.CallBooleanMethod(obj, method, new Span<jvalue>());
        }
    }

    // 'Raw' JNI interface to Android Dalvik (Java) VM from Scripting (CS/JS)
    [NativeHeader("Modules/AndroidJNI/Public/AndroidJNIBindingsHelpers.h")]
    [StaticAccessor("AndroidJNIBindingsHelpers", StaticAccessorType.DoubleColon)]
    [NativeConditional("PLATFORM_ANDROID")]
    public static class AndroidJNI
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct JStringBinding : IDisposable
        {
            private IntPtr javaString;
            private IntPtr chars;
            private int length;
            private bool ownsRef;

            public override string ToString()
            {
                if (length == 0)
                    return chars == IntPtr.Zero ? null : string.Empty;
                unsafe
                {
                    return new string((char*)chars, 0, length);
                }
            }
            public void Dispose()
            {
                if (length > 0)
                    ReleaseStringChars(this);
            }
        }

        [ThreadSafe]
        private static extern void ReleaseStringChars(JStringBinding str);

        [ThreadSafe]
        [StaticAccessor("jni", StaticAccessorType.DoubleColon)]
        public static extern IntPtr GetJavaVM();

        // Attaches the current thread to a Java (Dalvik) VM.
        [ThreadSafe]
        public static extern int AttachCurrentThread();

        // Detaches the current thread from a Java (Dalvik) VM.
        [ThreadSafe]
        public static extern int DetachCurrentThread();

        [RequiredByNativeCode]
        private static void InvokeAction(Action action) => action();

        [ThreadSafe]
        public static extern void InvokeAttached(Action action);

        // Returns the version of the native method interface.
        [ThreadSafe]
        public static extern int GetVersion();

        // This function loads a locally-defined class.
        [ThreadSafe]
        public static extern IntPtr FindClass(string name);

        // Converts a <tt>java.lang.reflect.Method</tt> or <tt>java.lang.reflect.Constructor</tt> object to a method ID.
        [ThreadSafe]
        public static extern IntPtr FromReflectedMethod(IntPtr refMethod);
        // Converts a <tt>java.lang.reflect.Field</tt> to a field ID.
        [ThreadSafe]
        public static extern IntPtr FromReflectedField(IntPtr refField);
        // Converts a method ID derived from clazz to a <tt>java.lang.reflect.Method</tt> or <tt>java.lang.reflect.Constructor</tt> object.
        [ThreadSafe]
        public static extern IntPtr ToReflectedMethod(IntPtr clazz, IntPtr methodID, bool isStatic);
        // Converts a field ID derived from cls to a <tt>java.lang.reflect.Field</tt> object.
        [ThreadSafe]
        public static extern IntPtr ToReflectedField(IntPtr clazz, IntPtr fieldID, bool isStatic);

        // If <tt>clazz</tt> represents any class other than the class <tt>Object</tt>, then this function returns the object that represents the superclass of the class specified by <tt>clazz</tt>.
        [ThreadSafe]
        public static extern IntPtr GetSuperclass(IntPtr clazz);
        // Determines whether an object of <tt>clazz1</tt> can be safely cast to <tt>clazz2</tt>.
        [ThreadSafe]
        public static extern bool IsAssignableFrom(IntPtr clazz1, IntPtr clazz2);

        // Causes a <tt>java.lang.Throwable</tt> object to be thrown.
        [ThreadSafe]
        public static extern int Throw(IntPtr obj);
        // Constructs an exception object from the specified class with the <tt>message</tt> specified by message and causes that exception to be thrown.
        [ThreadSafe]
        public static extern int ThrowNew(IntPtr clazz, string message);
        // Determines if an exception is being thrown
        [ThreadSafe]
        public static extern IntPtr ExceptionOccurred();
        // Prints an exception and a backtrace of the stack to the <tt>logcat</tt>
        [ThreadSafe]
        public static extern void ExceptionDescribe();
        // Clears any exception that is currently being thrown.
        [ThreadSafe]
        public static extern void ExceptionClear();
        // Raises a fatal error and does not expect the VM to recover. This function does not return.
        [ThreadSafe]
        public static extern void FatalError(string message);

        // Creates a new local reference frame, in which at least a given number of local references can be created.
        [ThreadSafe]
        public static extern int PushLocalFrame(int capacity);
        // Pops off the current local reference frame, frees all the local references, and returns a local reference in the previous local reference frame for the given <tt>result</tt> object.
        [ThreadSafe]
        public static extern IntPtr PopLocalFrame(IntPtr ptr);

        // Creates a new global reference to the object referred to by the <tt>obj</tt> argument.
        [ThreadSafe]
        public static extern IntPtr NewGlobalRef(IntPtr obj);
        // Deletes the global reference pointed to by <tt>obj</tt>.
        [ThreadSafe]
        public static extern void DeleteGlobalRef(IntPtr obj);
        // Checks that current thread is attached and deletes the global reference pointed to by <tt>obj</tt>.
        [ThreadSafe]
        internal static extern void QueueDeleteGlobalRef(IntPtr obj);
        // Returns the number of global references to delete in queue.
        [ThreadSafe]
        internal static extern uint GetQueueGlobalRefsCount();
        // Creates a new global weak reference to the object referred to by the <tt>obj</tt> argument.
        [ThreadSafe]
        public static extern IntPtr NewWeakGlobalRef(IntPtr obj);
        // Deletes the global weak reference pointed to by <tt>obj</tt>.
        [ThreadSafe]
        public static extern void DeleteWeakGlobalRef(IntPtr obj);
        // Creates a new local reference that refers to the same object as <tt>obj</tt>.
        [ThreadSafe]
        public static extern IntPtr NewLocalRef(IntPtr obj);
        // Deletes the local reference pointed to by <tt>obj</tt>.
        [ThreadSafe]
        public static extern void DeleteLocalRef(IntPtr obj);
        // Tests whether two references refer to the same Java object.
        [ThreadSafe]
        public static extern bool IsSameObject(IntPtr obj1, IntPtr obj2);

        // Ensures that at least a given number of local references can be created in the current thread.
        [ThreadSafe]
        public static extern int EnsureLocalCapacity(int capacity);

        //-------------------------------------------

        // Allocates a new Java object without invoking any of the constructors for the object.
        [ThreadSafe]
        public static extern IntPtr AllocObject(IntPtr clazz);

        // Constructs a new Java object. The method ID indicates which constructor method to invoke. This ID must be obtained by calling GetMethodID() with <init> as the method name and void (V) as the return type.
        public static IntPtr NewObject(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return NewObject(clazz, methodID, new Span<jvalue>(args));
        }
        public static IntPtr NewObject(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return NewObjectA(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr NewObjectA(IntPtr clazz, IntPtr methodID, jvalue* args);

        // Returns the class of an object.
        [ThreadSafe]
        public static extern IntPtr GetObjectClass(IntPtr obj);
        // Tests whether an object is an instance of a class.
        [ThreadSafe]
        public static extern bool IsInstanceOf(IntPtr obj, IntPtr clazz);

        // Returns the method ID for an instance (nonstatic) method of a class or interface.
        [ThreadSafe]
        public static extern IntPtr GetMethodID(IntPtr clazz, string name, string sig);
        // Returns the field ID for an instance (nonstatic) field of a class.
        [ThreadSafe]
        public static extern IntPtr GetFieldID(IntPtr clazz, string name, string sig);
        // Returns the method ID for a static method of a class.
        [ThreadSafe]
        public static extern IntPtr GetStaticMethodID(IntPtr clazz, string name, string sig);
        // Returns the field ID for a static field of a class.
        [ThreadSafe]
        public static extern IntPtr GetStaticFieldID(IntPtr clazz, string name, string sig);

        public static IntPtr NewString(string chars)
        {
            return NewStringFromStr(chars);
        }

        [ThreadSafe]
        private static extern IntPtr NewStringFromStr(string chars);

        // Constructs a new <tt>java.lang.String</tt> object from an array of Unicode characters.
        [ThreadSafe]
        public static extern IntPtr NewString(char[] chars);

        // Constructs a new <tt>java.lang.String</tt> object from an array of characters in modified UTF-8 encoding.
        [ThreadSafe]
        public static extern IntPtr NewStringUTF(string bytes);

        public static string GetStringChars(IntPtr str)
        {
            using (var jstring = GetStringCharsInternal(str))
                return jstring.ToString();
        }

        [ThreadSafe]
        private static extern JStringBinding GetStringCharsInternal(IntPtr str);

        // Returns the length (the count of Unicode characters) of a Java string.
        [ThreadSafe]
        public static extern int GetStringLength(IntPtr str);
        // Returns the length in bytes of the modified UTF-8 representation of a string.
        [ThreadSafe]
        public static extern int GetStringUTFLength(IntPtr str);
        // Returns a managed string object representing the string in modified UTF-8 encoding.
        [ThreadSafe]
        public static extern string GetStringUTFChars(IntPtr str);

        //---------------------------------------------

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static string CallStringMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallStringMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static string CallStringMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStringMethodUnsafe(obj, methodID, a);
                }
            }
        }
        public static unsafe string CallStringMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args)
        {
            using (var jstring = CallStringMethodUnsafeInternal(obj, methodID, args))
                return jstring.ToString();
        }

        [ThreadSafe]
        private static extern unsafe JStringBinding CallStringMethodUnsafeInternal(IntPtr obj, IntPtr methodID, jvalue* args);

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallObjectMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallObjectMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr CallObjectMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int32 CallIntMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallIntMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static Int32 CallIntMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallIntMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int32 CallIntMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static bool CallBooleanMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallBooleanMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static bool CallBooleanMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallBooleanMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe bool CallBooleanMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int16 CallShortMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallShortMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static Int16 CallShortMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallShortMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int16 CallShortMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        [Obsolete("AndroidJNI.CallByteMethod is obsolete. Use AndroidJNI.CallSByteMethod method instead")]
        public static Byte CallByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return (Byte)CallSByteMethod(obj, methodID, args);
        }

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static SByte CallSByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallSByteMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static SByte CallSByteMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallSByteMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe SByte CallSByteMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Char CallCharMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallCharMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static Char CallCharMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallCharMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Char CallCharMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static float CallFloatMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallFloatMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static float CallFloatMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallFloatMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe float CallFloatMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static double CallDoubleMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallDoubleMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static double CallDoubleMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallDoubleMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe double CallDoubleMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int64 CallLongMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallLongMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static Int64 CallLongMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallLongMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int64 CallLongMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static void CallVoidMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            CallVoidMethod(obj, methodID, new Span<jvalue>(args));
        }
        public static void CallVoidMethod(IntPtr obj, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    CallVoidMethodUnsafe(obj, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe void CallVoidMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);

        //---------------------------------------------

        // This function returns the value of an instance (nonstatic) field of an object.
        public static string GetStringField(IntPtr obj, IntPtr fieldID)
        {
            using (var jstring = GetStringFieldInternal(obj, fieldID))
                return jstring.ToString();
        }

        [ThreadSafe]
        private static extern JStringBinding GetStringFieldInternal(IntPtr obj, IntPtr fieldID);

        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern IntPtr GetObjectField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern bool GetBooleanField(IntPtr obj, IntPtr fieldID);
        [Obsolete("AndroidJNI.GetByteField is obsolete. Use AndroidJNI.GetSByteField method instead")]
        public static Byte GetByteField(IntPtr obj, IntPtr fieldID)
        {
            return (Byte)GetSByteField(obj, fieldID);
        }

        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern SByte GetSByteField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern Char GetCharField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern Int16 GetShortField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern Int32 GetIntField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern Int64 GetLongField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern float GetFloatField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern double GetDoubleField(IntPtr obj, IntPtr fieldID);

        //---------------------------------------------

        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetStringField(IntPtr obj, IntPtr fieldID, string val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetObjectField(IntPtr obj, IntPtr fieldID, IntPtr val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetBooleanField(IntPtr obj, IntPtr fieldID, bool val);
        [Obsolete("AndroidJNI.SetByteField is obsolete. Use AndroidJNI.SetSByteField method instead")]
        public static void SetByteField(IntPtr obj, IntPtr fieldID, Byte val)
        {
            SetSByteField(obj, fieldID, (SByte)val);
        }

        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetSByteField(IntPtr obj, IntPtr fieldID, SByte val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetCharField(IntPtr obj, IntPtr fieldID, Char val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetShortField(IntPtr obj, IntPtr fieldID, Int16 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetIntField(IntPtr obj, IntPtr fieldID, Int32 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetLongField(IntPtr obj, IntPtr fieldID, Int64 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetFloatField(IntPtr obj, IntPtr fieldID, float val);
        // This function sets the value of an instance (nonstatic) field of an object.
        [ThreadSafe]
        public static extern void SetDoubleField(IntPtr obj, IntPtr fieldID, double val);


        //---------------------------------------------

        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static string CallStaticStringMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticStringMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static string CallStaticStringMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticStringMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        public static unsafe string CallStaticStringMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args)
        {
            using (var jstring = CallStaticStringMethodUnsafeInternal(clazz, methodID, args))
                return jstring.ToString();
        }
        [ThreadSafe]
        private static extern unsafe JStringBinding CallStaticStringMethodUnsafeInternal(IntPtr clazz, IntPtr methodID, jvalue* args);

        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticObjectMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticObjectMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr CallStaticObjectMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int32 CallStaticIntMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticIntMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static Int32 CallStaticIntMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticIntMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int32 CallStaticIntMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static bool CallStaticBooleanMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticBooleanMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static bool CallStaticBooleanMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticBooleanMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe bool CallStaticBooleanMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int16 CallStaticShortMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticShortMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static Int16 CallStaticShortMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticShortMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int16 CallStaticShortMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        [Obsolete("AndroidJNI.CallStaticByteMethod is obsolete. Use AndroidJNI.CallStaticSByteMethod method instead")]
        public static Byte CallStaticByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return (Byte)CallStaticSByteMethod(clazz, methodID, args);
        }

        public static SByte CallStaticSByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticSByteMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static SByte CallStaticSByteMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticSByteMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe SByte CallStaticSByteMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Char CallStaticCharMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticCharMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static Char CallStaticCharMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticCharMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Char CallStaticCharMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static float CallStaticFloatMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticFloatMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static float CallStaticFloatMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticFloatMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe float CallStaticFloatMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static double CallStaticDoubleMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticDoubleMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static double CallStaticDoubleMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticDoubleMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe double CallStaticDoubleMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static Int64 CallStaticLongMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticLongMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static Int64 CallStaticLongMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    return CallStaticLongMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe Int64 CallStaticLongMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        public static void CallStaticVoidMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            CallStaticVoidMethod(clazz, methodID, new Span<jvalue>(args));
        }
        public static void CallStaticVoidMethod(IntPtr clazz, IntPtr methodID, Span<jvalue> args)
        {
            unsafe
            {
                fixed (jvalue* a = args)
                {
                    CallStaticVoidMethodUnsafe(clazz, methodID, a);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe void CallStaticVoidMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);

        //---------------------------------------------

        // This function returns the value of a static field of an object.
        public static string GetStaticStringField(IntPtr clazz, IntPtr fieldID)
        {
            using (var jstring = GetStaticStringFieldInternal(clazz, fieldID))
                return jstring.ToString();
        }

        [ThreadSafe]
        private static extern JStringBinding GetStaticStringFieldInternal(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern bool GetStaticBooleanField(IntPtr clazz, IntPtr fieldID);
        [Obsolete("AndroidJNI.GetStaticByteField is obsolete. Use AndroidJNI.GetStaticSByteField method instead")]
        public static Byte GetStaticByteField(IntPtr clazz, IntPtr fieldID)
        {
            return (Byte)GetStaticSByteField(clazz, fieldID);
        }

        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern SByte GetStaticSByteField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern Char GetStaticCharField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern Int16 GetStaticShortField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern Int32 GetStaticIntField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern Int64 GetStaticLongField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern float GetStaticFloatField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        [ThreadSafe]
        public static extern double GetStaticDoubleField(IntPtr clazz, IntPtr fieldID);

        //---------------------------------------------

        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticStringField(IntPtr clazz, IntPtr fieldID, string val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticObjectField(IntPtr clazz, IntPtr fieldID, IntPtr val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticBooleanField(IntPtr clazz, IntPtr fieldID, bool val);
        [Obsolete("AndroidJNI.SetStaticByteField is obsolete. Use AndroidJNI.SetStaticSByteField method instead")]
        public static void SetStaticByteField(IntPtr clazz, IntPtr fieldID, Byte val)
        {
            SetStaticSByteField(clazz, fieldID, (SByte)val);
        }

        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticSByteField(IntPtr clazz, IntPtr fieldID, SByte val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticCharField(IntPtr clazz, IntPtr fieldID, Char val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticShortField(IntPtr clazz, IntPtr fieldID, Int16 val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticIntField(IntPtr clazz, IntPtr fieldID, Int32 val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticLongField(IntPtr clazz, IntPtr fieldID, Int64 val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticFloatField(IntPtr clazz, IntPtr fieldID, float val);
        // This function ets the value of a static field of an object.
        [ThreadSafe]
        public static extern void SetStaticDoubleField(IntPtr clazz, IntPtr fieldID, double val);


        //---------------------------------------
        [ThreadSafe]
        static extern IntPtr ConvertToBooleanArray(Boolean[] array);
        // Convert a managed array of System.Boolean to a Java array of <tt>boolean</tt>.
        public static IntPtr ToBooleanArray(Boolean[] array)
        {
            return array == null ? IntPtr.Zero : ConvertToBooleanArray(array);
        }

        [ThreadSafe]
        [Obsolete("AndroidJNI.ToByteArray is obsolete. Use AndroidJNI.ToSByteArray method instead")]
        public static extern IntPtr ToByteArray(Byte[] array);
        // Convert a managed array of System.SByte to a Java array of <tt>byte</tt>.
        public static IntPtr ToSByteArray(SByte[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (SByte* ptr = array)
                {
                    return ToSByteArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToSByteArray(SByte* array, int length);
        // Convert a managed array of System.Char to a Java array of <tt>char</tt>.
        public static IntPtr ToCharArray(Char[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (Char* ptr = array)
                {
                    return ToCharArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToCharArray(Char* array, int length);
        // Convert a managed array of System.Int16 to a Java array of <tt>short</tt>.
        public static IntPtr ToShortArray(Int16[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (Int16* ptr = array)
                {
                    return ToShortArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToShortArray(Int16* array, int length);
        // Convert a managed array of System.Int32 to a Java array of <tt>int</tt>.
        public static IntPtr ToIntArray(Int32[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (Int32* ptr = array)
                {
                    return ToIntArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToIntArray(Int32* array, int length);
        // Convert a managed array of System.Int64 to a Java array of <tt>long</tt>.
        public static IntPtr ToLongArray(Int64[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (Int64* ptr = array)
                {
                    return ToLongArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToLongArray(Int64* array, int length);
        // Convert a managed array of System.Single to a Java array of <tt>float</tt>.
        public static IntPtr ToFloatArray(float[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (float* ptr = array)
                {
                    return ToFloatArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToFloatArray(float* array, int length);
        // Convert a managed array of System.Double to a Java array of <tt>double</tt>.
        public static IntPtr ToDoubleArray(double[] array)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (double* ptr = array)
                {
                    return ToDoubleArray(ptr, array.Length);
                }
            }
        }
        [ThreadSafe]
        public static extern unsafe IntPtr ToDoubleArray(double* array, int length);

        // Convert a managed array of System.IntPtr, representing Java objects, to a Java array of <tt>java.lang.Object</tt>.
        [ThreadSafe]
        public static extern unsafe IntPtr ToObjectArray(IntPtr* array, int length, IntPtr arrayClass);

        public static IntPtr ToObjectArray(IntPtr[] array, IntPtr arrayClass)
        {
            unsafe
            {
                if (array == null)
                    return IntPtr.Zero;
                fixed (IntPtr* ptr = array)
                {
                    return ToObjectArray(ptr, array.Length, arrayClass);
                }
            }
        }

        // Convert a managed array of System.IntPtr, representing Java objects, to a Java array of <tt>java.lang.Object</tt>.
        public static IntPtr ToObjectArray(IntPtr[] array)
        {
            return ToObjectArray(array, IntPtr.Zero);
        }

        // Convert a Java array of <tt>boolean</tt> to a managed array of System.Boolean.
        [ThreadSafe]
        public static extern Boolean[] FromBooleanArray(IntPtr array);
        [ThreadSafe]
        [Obsolete("AndroidJNI.FromByteArray is obsolete. Use AndroidJNI.FromSByteArray method instead")]
        public static extern Byte[] FromByteArray(IntPtr array);
        // Convert a Java array of <tt>byte</tt> to a managed array of System.SByte.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern SByte[] FromSByteArray(IntPtr array);
        // Convert a Java array of <tt>char</tt> to a managed array of System.Char.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern Char[] FromCharArray(IntPtr array);
        // Convert a Java array of <tt>short</tt> to a managed array of System.Int16.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern Int16[] FromShortArray(IntPtr array);
        // Convert a Java array of <tt>int</tt> to a managed array of System.Int32.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern Int32[] FromIntArray(IntPtr array);
        // Convert a Java array of <tt>long</tt> to a managed array of System.Int64.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern Int64[] FromLongArray(IntPtr array);
        // Convert a Java array of <tt>float</tt> to a managed array of System.Single.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern float[] FromFloatArray(IntPtr array);
        // Convert a Java array of <tt>double</tt> to a managed array of System.Double.
        [ThreadSafe]
        [return:Unmarshalled]
        public static extern double[] FromDoubleArray(IntPtr array);
        // Convert a Java array of <tt>java.lang.Object</tt> to a managed array of System.IntPtr, representing Java objects.
        [ThreadSafe]
        public static extern IntPtr[] FromObjectArray(IntPtr array);

        // Returns the number of elements in the array.
        [ThreadSafe]
        public static extern int GetArrayLength(IntPtr array);

        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewBooleanArray(int size);
        [Obsolete("AndroidJNI.NewByteArray is obsolete. Use AndroidJNI.NewSByteArray method instead")]
        public static IntPtr NewByteArray(int size)
        {
            return NewSByteArray(size);
        }

        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewSByteArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewCharArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewShortArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewIntArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewLongArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewFloatArray(int size);
        // Construct a new primitive array object.
        [ThreadSafe]
        public static extern IntPtr NewDoubleArray(int size);
        // Constructs a new array holding objects in class <tt>clazz</tt>. All elements are initially set to <tt>obj</tt>.
        [ThreadSafe]
        public static extern IntPtr NewObjectArray(int size, IntPtr clazz, IntPtr obj);

        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern bool GetBooleanArrayElement(IntPtr array, int index);
        [Obsolete("AndroidJNI.GetByteArrayElement is obsolete. Use AndroidJNI.GetSByteArrayElement method instead")]
        public static Byte GetByteArrayElement(IntPtr array, int index)
        {
            return (Byte)GetSByteArrayElement(array, index);
        }

        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern SByte GetSByteArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern Char GetCharArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern Int16 GetShortArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern Int32 GetIntArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern Int64 GetLongArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern float GetFloatArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        [ThreadSafe]
        public static extern double GetDoubleArrayElement(IntPtr array, int index);
        // Returns an element of an <tt>Object</tt> array.
        [ThreadSafe]
        public static extern IntPtr GetObjectArrayElement(IntPtr array, int index);

        // Sets the value of one element in a primitive array.
        [Obsolete("AndroidJNI.SetBooleanArrayElement(IntPtr, int, byte) is obsolete. Use AndroidJNI.SetBooleanArrayElement(IntPtr, int, bool) method instead")]
        public static void SetBooleanArrayElement(IntPtr array, int index, byte val)
        {
            SetBooleanArrayElement(array, index, val != 0);
        }

        [ThreadSafe]
        public static extern void SetBooleanArrayElement(IntPtr array, int index, bool val);
        [Obsolete("AndroidJNI.SetByteArrayElement is obsolete. Use AndroidJNI.SetSByteArrayElement method instead")]
        public static void SetByteArrayElement(IntPtr array, int index, sbyte val)
        {
            SetSByteArrayElement(array, index, val);
        }

        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetSByteArrayElement(IntPtr array, int index, sbyte val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetCharArrayElement(IntPtr array, int index, Char val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetShortArrayElement(IntPtr array, int index, Int16 val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetIntArrayElement(IntPtr array, int index, Int32 val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetLongArrayElement(IntPtr array, int index, Int64 val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetFloatArrayElement(IntPtr array, int index, float val);
        // Sets the value of one element in a primitive array.
        [ThreadSafe]
        public static extern void SetDoubleArrayElement(IntPtr array, int index, double val);
        // Sets an element of an <tt>Object</tt> array.
        [ThreadSafe]
        public static extern void SetObjectArrayElement(IntPtr array, int index, IntPtr obj);

        // ------------------------------------------
        // ByteBuffer support
        [ThreadSafe]
        public static extern unsafe IntPtr NewDirectByteBuffer(byte* buffer, Int64 capacity);

        public static IntPtr NewDirectByteBuffer(NativeArray<byte> buffer)
        {
            return NewDirectByteBufferFromNativeArray(buffer);
        }

        public static IntPtr NewDirectByteBuffer(NativeArray<sbyte> buffer)
        {
            return NewDirectByteBufferFromNativeArray(buffer);
        }

        private static IntPtr NewDirectByteBufferFromNativeArray<T>(NativeArray<T> buffer)
            where T : struct
        {
            if (!buffer.IsCreated || buffer.Length <= 0)
                return IntPtr.Zero;
            unsafe
            {
                return NewDirectByteBuffer((byte*)buffer.GetUnsafePtr(), buffer.Length);
            }
        }

        public static unsafe sbyte* GetDirectBufferAddress(IntPtr buffer)
        {
            return null;
        }

        [ThreadSafe]
        public static extern long GetDirectBufferCapacity(IntPtr buffer);

        private static NativeArray<T> GetDirectBuffer<T>(IntPtr buffer)
            where T : struct
        {
            unsafe
            {
                if (buffer == IntPtr.Zero)
                    return new NativeArray<T>();
                sbyte* address = GetDirectBufferAddress(buffer);
                if (address == null)
                    return new NativeArray<T>();
                long capacity = GetDirectBufferCapacity(buffer);
                if (capacity > Int32.MaxValue)
                    throw new Exception($"Direct buffer is too large ({capacity}) for NativeArray (max {Int32.MaxValue})");
                return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(address, (int)capacity, Allocator.None);
            }
        }

        public static NativeArray<byte> GetDirectByteBuffer(IntPtr buffer)
        {
            return GetDirectBuffer<byte>(buffer);
        }

        public static NativeArray<sbyte> GetDirectSByteBuffer(IntPtr buffer)
        {
            return GetDirectBuffer<sbyte>(buffer);
        }


        // ------------------------------------------
        // Native method support
        public static int RegisterNatives(IntPtr clazz, JNINativeMethod[] methods)
        {
            const int kError = -1;  // RegisterNatives returns negative values for errors, actual values not mentioned in docs

            if (methods == null || methods.Length == 0)
                return kError;
            foreach (var m in methods)
                if (string.IsNullOrEmpty(m.name) || string.IsNullOrEmpty(m.signature) || m.fnPtr == null)
                    return kError;

            return 0;  // false-success in Editor if parameters are correct
        }

        [ThreadSafe]
        private static extern IntPtr RegisterNativesAllocate(int length);
        [ThreadSafe]
        private static extern void RegisterNativesSet(IntPtr natives, int idx, string name, string signature, IntPtr fnPtr);
        [ThreadSafe]
        private static extern int RegisterNativesAndFree(IntPtr clazz, IntPtr natives, int n);
        [ThreadSafe]
        public static extern int UnregisterNatives(IntPtr clazz);
    }
}
