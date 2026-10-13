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
    ///<exclude />
    [StructLayout(LayoutKind.Explicit)]
    [NativeType(CodegenOptions.Custom, "ScriptingJvalue")]
    public struct jvalue
    {
        ///<exclude />
        [FieldOffset(0)]    public bool    z;
        ///<exclude />
        [FieldOffset(0)]    public sbyte   b;
        ///<exclude />
        [FieldOffset(0)]    public char    c;
        ///<exclude />
        [FieldOffset(0)]    public short   s;
        ///<exclude />
        [FieldOffset(0)]    public int     i;
        ///<exclude />
        [FieldOffset(0)]    public long    j;
        ///<exclude />
        [FieldOffset(0)]    public float   f;
        ///<exclude />
        [FieldOffset(0)]    public double  d;
        ///<exclude />
        [FieldOffset(0)]    public IntPtr  l;
    }

    ///<summary>Defines a single method to register with a Java class using <see cref="AndroidJNI.RegisterNatives" />.</summary>
    ///<remarks>This struct stores information about the Java method, such as its name, JNI signature, and the pointer to its corresponding native function. Registering this information using the <see cref="AndroidJNI.RegisterNatives" /> allows you to integrate Android-specific capabilities in your Unity application.
    ///
    ///**Note**: Use of this struct requires knowledge of Android Java Native Interface (JNI) concepts. For more information, refer to &lt;a href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html"&gt;Java Native Interface Specification (Oracle)&lt;/a&gt;</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[
    /// // Delegate definition 
    ///delegate void JavaToCs(IntPtr jniEnv, IntPtr klass, int x);
    ///
    /// // Method definition
    ///[MonoPInvokeCallback(typeof(JavaToCs))]
    ///static void CsMethod(IntPtr jniEnv, IntPtr klass, int x)
    ///{
    ///    Debug.Log("From Java: " + x);
    ///}
    ///
    /// // Array to be passed to AndroidJNI.RegisterNative
    ///var methods = new JNINativeMethod[]
    ///{
    ///    new JNINativeMethod
    ///    {
    ///        name = "csMethod",
    ///        signature = "(I)V",
    ///        fnPtr = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(new JavaToCs(CsMethod)),
    ///    }
    ///};
    ///
    /// // Register method with the specified Java class
    ///AndroidJNI.RegisterNatives(clazz, methods);
    ///]]></code>
    ///</example>
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "ScriptingJNINativeMethod")]
    public struct JNINativeMethod
    {
        ///<summary>The name of the Java method implemented by the corresponding native function.</summary>
        public string name;
        ///<summary>The JNI method signature string that represents the parameters and return type of the Java method.</summary>
        ///<remarks>For example, the signature <c>(I)V</c> indicates that the method takes an integer parameter (<c>I</c>) and returns void (<c>V</c>).</remarks>
        public string signature;
        ///<summary>The pointer to the native function that is called from the Java code.</summary>
        ///<remarks>Obtain this pointer by calling <c>Marshal.GetFunctionPointerForDelegate</c>, where the delegate refers to a method annotated with <c>MonoPInvokeCallback</c> attribute. The method must have the signature: <c>static ReturnType MyMethod(IntPtr jniEnv, IntPtr objectOrClass, ...);</c></remarks>
        public IntPtr fnPtr;
    }

    // Helper interface for JNI interaction; signature creation and method lookups
    ///<summary>Helper interface for JNI interaction; signature creation and method lookups.
    ///
    ///**Note:** Using raw JNI functions requires advanced knowledge of the Android Java Native Interface (JNI). Please take note.</summary>
    [UsedByNativeCode]
    [NativeHeader("Modules/AndroidJNI/Public/AndroidJNIBindingsHelpers.h")]
    [StaticAccessor("AndroidJNIBindingsHelpers", StaticAccessorType.DoubleColon)]
    [NativeConditional("PLATFORM_ANDROID")]
    public static class AndroidJNIHelper
    {
        // Set /debug/ to true to log calls through the AndroidJNIHelper
        ///<summary>Set <c>debug</c> to true to log calls through the AndroidJNIHelper.</summary>
        public static extern bool debug { get; set; }

        // Scans a particular Java class for a constructor method matching a signature.
        ///<summary>Scans a particular Java class for a constructor method matching a signature.</summary>
        ///<remarks>The signature comparison is done to allow for sub-/base-classes of the class types.
        ///If no signature is provided the first constuctor found will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<returns>The constructor's method ID. Throws an exception if no matching constructor is found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetMethodID" />
        ///<seealso cref="AndroidJNIHelper.GetFieldID" />
        public static IntPtr GetConstructorID(IntPtr javaClass)
        {
            return GetConstructorID(javaClass, "");
        }

        // Scans a particular Java class for a constructor method matching a signature.
        ///<summary>Scans a particular Java class for a constructor method matching a signature.</summary>
        ///<remarks>The signature comparison is done to allow for sub-/base-classes of the class types.
        ///If no signature is provided the first constuctor found will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="signature">Constructor method signature (e.g. obtained by calling AndroidJNIHelper.GetSignature).</param>
        ///<returns>The constructor's method ID. Throws an exception if no matching constructor is found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetMethodID" />
        ///<seealso cref="AndroidJNIHelper.GetFieldID" />
        public static IntPtr GetConstructorID(IntPtr javaClass, [DefaultValue("")] string signature)
        {
            return _AndroidJNIHelper.GetConstructorID(javaClass, signature);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        ///<summary>Scans a particular Java class for a method matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first method with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="methodName">Name of the method as declared in Java.</param>
        ///<returns>The method ID. Throws an exception if the method is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetFieldID" />
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName)
        {
            return GetMethodID(javaClass, methodName, "", false);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        ///<summary>Scans a particular Java class for a method matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first method with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="methodName">Name of the method as declared in Java.</param>
        ///<param name="signature">Method signature (e.g. obtained by calling AndroidJNIHelper.GetSignature).</param>
        ///<returns>The method ID. Throws an exception if the method is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetFieldID" />
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [DefaultValue("")] string signature)
        {
            return GetMethodID(javaClass, methodName, signature, false);
        }

        // Scans a particular Java class for a method matching a name and a signature.
        ///<summary>Scans a particular Java class for a method matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first method with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="methodName">Name of the method as declared in Java.</param>
        ///<param name="signature">Method signature (e.g. obtained by calling AndroidJNIHelper.GetSignature).</param>
        ///<param name="isStatic">Set to &lt;tt&gt;true&lt;/tt&gt; for static methods; &lt;tt&gt;false&lt;/tt&gt; for instance (nonstatic) methods.</param>
        ///<returns>The method ID. Throws an exception if the method is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetFieldID" />
        public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [DefaultValue("")] string signature, [DefaultValue("false")] bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID(javaClass, methodName, signature, isStatic);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        ///<summary>Scans a particular Java class for a field matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first field with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="fieldName">Name of the field as declared in Java.</param>
        ///<returns>The field ID. Throws an exception if the field is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetMethodID" />
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName)
        {
            return GetFieldID(javaClass, fieldName, "", false);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        ///<summary>Scans a particular Java class for a field matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first field with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="fieldName">Name of the field as declared in Java.</param>
        ///<param name="signature">Field signature (e.g. obtained by calling AndroidJNIHelper.GetSignature).</param>
        ///<returns>The field ID. Throws an exception if the field is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetMethodID" />
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [DefaultValue("")] string signature)
        {
            return GetFieldID(javaClass, fieldName, signature, false);
        }

        // Scans a particular Java class for a field matching a name and a signature.
        ///<summary>Scans a particular Java class for a field matching a name and a signature.</summary>
        ///<remarks>The signature comparison is done to allow sub-/base-classes of a class types.
        ///If no signature is provided the first field with the correct name will be returned.</remarks>
        ///<param name="javaClass">Raw JNI Java class object (obtained by calling AndroidJNI.FindClass).</param>
        ///<param name="fieldName">Name of the field as declared in Java.</param>
        ///<param name="signature">Field signature (e.g. obtained by calling AndroidJNIHelper.GetSignature).</param>
        ///<param name="isStatic">Set to &lt;tt&gt;true&lt;/tt&gt; for static fields; &lt;tt&gt;false&lt;/tt&gt; for instance (nonstatic) fields.</param>
        ///<returns>The field ID. Throws an exception if the field is not found.</returns>
        ///<seealso cref="AndroidJNIHelper.GetSignature" />
        ///<seealso cref="AndroidJNIHelper.GetConstructorID" />
        ///<seealso cref="AndroidJNIHelper.GetMethodID" />
        public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [DefaultValue("")] string signature, [DefaultValue("false")] bool isStatic)
        {
            return _AndroidJNIHelper.GetFieldID(javaClass, fieldName, signature, isStatic);
        }

        // Creates a UnityJavaRunnable object (implements java.lang.Runnable).
        ///<summary>Creates a UnityJavaRunnable object (implements java.lang.Runnable).</summary>
        ///<param name="jrunnable">A delegate representing the java.lang.Runnable.</param>
        ///<returns>A local reference to the newly created UnityJavaRunnable object.</returns>
        ///<seealso cref="AndroidJavaRunnable" />
        public static IntPtr CreateJavaRunnable(AndroidJavaRunnable jrunnable)
        {
            return _AndroidJNIHelper.CreateJavaRunnable(jrunnable);
        }

        // Creates a UnityJavaProxy object (implements jinterface).
        ///<summary>Creates a java proxy object which connects to the supplied proxy implementation.</summary>
        ///<param name="proxy">An implementatinon of a java interface in c#.</param>
        ///<returns>A local reference to the newly created Java proxy object.</returns>
        ///<seealso cref="AndroidJavaProxy" />
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
        ///<summary>Creates a Java array from a managed array.</summary>
        ///<param name="array">Managed array to be converted into a Java array object.</param>
        ///<returns>A local reference to the newly created Java array.</returns>
        ///<seealso cref="AndroidJNIHelper.ConvertFromJNIArray" />
        public static IntPtr ConvertToJNIArray(Array array)
        {
            return _AndroidJNIHelper.ConvertToJNIArray(array);
        }

        // Creates the parameter array to be used as argument list when invoking Java code through CallMethod() in AndroidJNI.
        ///<summary>Creates the parameter array to be used as argument list when invoking Java code through CallMethod() in AndroidJNI.</summary>
        ///<param name="args">An array of objects that should be converted to Call parameters.</param>
        ///<returns>The converted arguments, ready to pass to a Call method.</returns>
        public static jvalue[] CreateJNIArgArray(object[] args)
        {
            jvalue[] ret = new jvalue[args.Length];
            _AndroidJNIHelper.CreateJNIArgArray(args, ret);
            return ret;
        }

        ///<summary>Creates the parameter array to be used as argument list when invoking Java code through CallMethod() in AndroidJNI, writing into the given buffer instead of allocating a new array.</summary>
        ///<param name="args">An array of objects to convert to Call parameters.</param>
        ///<param name="jniArgs">The buffer to write the converted arguments into.</param>
        public static void CreateJNIArgArray(object[] args, Span<jvalue> jniArgs)
        {
            if (args.Length != jniArgs.Length)
                throw new ArgumentException($"Both arrays must be of the same length, but are {args.Length} and {jniArgs.Length}");
            _AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
        }

        ///<summary>Deletes any local JNI references previously allocated by CreateJNIArgArray.</summary>
        ///<param name="args">The array of arguments used as a parameter to CreateJNIArgArray.</param>
        ///<param name="jniArgs">The array returned by CreateJNIArgArray.</param>
        public static void DeleteJNIArgArray(object[] args, jvalue[] jniArgs)
        {
            _AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
        }

        ///<summary>Deletes any local JNI references previously allocated by CreateJNIArgArray.</summary>
        ///<param name="args">The array of arguments used as a parameter to CreateJNIArgArray.</param>
        ///<param name="jniArgs">The array returned by CreateJNIArgArray.</param>
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
        ///<summary>Get a JNI method ID for a constructor based on calling arguments.</summary>
        ///<remarks>Scans a particular Java class for a constructor method matching a signature based on passed arguments.
        ///The signature comparison is done to allow for sub-/base-classes of the class types.</remarks>
        ///<param name="jclass">The Java class to scan.</param>
        ///<param name="args">Array with parameters to be passed to the constructor when invoked.</param>
        ///<returns>The constructor's method ID. Throws an exception if no matching constructor is found.</returns>
        public static IntPtr GetConstructorID(IntPtr jclass, object[] args)
        {
            return _AndroidJNIHelper.GetConstructorID(jclass, args);
        }

        // Get a JNI method ID based on calling arguments.
        ///<summary>Get a JNI method ID based on calling arguments.</summary>
        ///<remarks>Generic parameter represents the method return type, and the regular method assumes 'void' return type.
        ///Scans a particular Java class for a method matching a signature based on passed arguments.
        ///The signature comparison is done to allow for sub-/base-classes of the class types.</remarks>
        ///<param name="jclass">The Java class to scan.</param>
        ///<param name="methodName">Name of the method as declared in Java.</param>
        ///<param name="args">Array with parameters to be passed to the method when invoked.</param>
        ///<param name="isStatic">Set to &lt;tt&gt;true&lt;/tt&gt; for static methods; &lt;tt&gt;false&lt;/tt&gt; for instance (nonstatic) methods.</param>
        ///<returns>The method ID. Throws an exception if the method is not found.</returns>
        public static IntPtr GetMethodID(IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID(jclass, methodName, args, isStatic);
        }

        // Creates the JNI signature string for particular object type
        ///<summary>Creates the JNI signature string for particular object type.</summary>
        ///<param name="obj">Object for which a signature is to be produced.</param>
        ///<returns>The JNI signature string.</returns>
        public static string GetSignature(object obj)
        {
            return _AndroidJNIHelper.GetSignature(obj);
        }

        // Creates the JNI signature string for an object parameter list.
        ///<summary>Creates the JNI signature string for an object parameter list.</summary>
        ///<param name="args">Array of object for which a signature is to be produced.</param>
        ///<returns>The JNI signature string.</returns>
        public static string GetSignature(object[] args)
        {
            return _AndroidJNIHelper.GetSignature(args);
        }

        //===================================================================

        // Creates a managed array from a Java array
        ///<summary>Creates a managed array from a Java array.</summary>
        ///<param name="array">Java array object to be converted into a managed array.</param>
        ///<returns>The managed array converted from the Java array.</returns>
        ///<seealso cref="AndroidJNIHelper.ConvertToJNIArray" />
        public static ArrayType ConvertFromJNIArray<ArrayType>(IntPtr array)
        {
            return _AndroidJNIHelper.ConvertFromJNIArray<ArrayType>(array);
        }

        // Get a JNI method ID based on calling arguments.
        ///<summary>Get a JNI method ID based on calling arguments.</summary>
        ///<remarks>Generic parameter represents the method return type, and the regular method assumes 'void' return type.
        ///Scans a particular Java class for a method matching a signature based on passed arguments.
        ///The signature comparison is done to allow for sub-/base-classes of the class types.</remarks>
        ///<param name="jclass">The Java class to scan.</param>
        ///<param name="methodName">Name of the method as declared in Java.</param>
        ///<param name="args">Array with parameters to be passed to the method when invoked.</param>
        ///<param name="isStatic">Set to &lt;tt&gt;true&lt;/tt&gt; for static methods; &lt;tt&gt;false&lt;/tt&gt; for instance (nonstatic) methods.</param>
        ///<returns>The method ID. Throws an exception if the method is not found.</returns>
        public static IntPtr GetMethodID<ReturnType>(IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID<ReturnType>(jclass, methodName, args, isStatic);
        }

        // Get a JNI field ID based on type detection. Generic parameter represents the field type.
        ///<summary>Get a JNI field ID based on type detection. Generic parameter represents the field type.</summary>
        ///<remarks>Scans a particular Java class for a field matching a signature of the field type.
        ///The signature comparison is done to allow for sub-/base-classes of the class types.</remarks>
        ///<param name="jclass">The Java class to scan.</param>
        ///<param name="fieldName">Name of the field as declared in Java.</param>
        ///<param name="isStatic">Set to &lt;tt&gt;true&lt;/tt&gt; for static fields; &lt;tt&gt;false&lt;/tt&gt; for instance (nonstatic) fields.</param>
        ///<returns>The field ID. Throws an exception if the field is not found.</returns>
        public static IntPtr GetFieldID<FieldType>(IntPtr jclass, string fieldName, bool isStatic)
        {
            return _AndroidJNIHelper.GetFieldID<FieldType>(jclass, fieldName, isStatic);
        }

        // Creates the JNI signature string for an object parameter list.
        ///<summary>Creates the JNI signature string for an object parameter list.</summary>
        ///<remarks>The generic parameter represents the method return type.</remarks>
        ///<param name="args">Array of object for which a signature is to be produced.</param>
        ///<returns>The JNI signature string.</returns>
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

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(sbyte value)
        {
            jvalue val = default;
            val.b = value;
            return Box(val, "java/lang/Byte", "(B)Ljava/lang/Byte;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(short value)
        {
            jvalue val = default;
            val.s = value;
            return Box(val, "java/lang/Short", "(S)Ljava/lang/Short;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(int value)
        {
            jvalue val = default;
            val.i = value;
            return Box(val, "java/lang/Integer", "(I)Ljava/lang/Integer;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(long value)
        {
            jvalue val = default;
            val.j = value;
            return Box(val, "java/lang/Long", "(J)Ljava/lang/Long;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(float value)
        {
            jvalue val = default;
            val.f = value;
            return Box(val, "java/lang/Float", "(F)Ljava/lang/Float;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(double value)
        {
            jvalue val = default;
            val.d = value;
            return Box(val, "java/lang/Double", "(D)Ljava/lang/Double;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
        public static IntPtr Box(char value)
        {
            jvalue val = default;
            val.c = value;
            return Box(val, "java/lang/Character", "(C)Ljava/lang/Character;");
        }

        ///<summary>Convert primitive to it's object counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.</remarks>
        ///<param name="value">Primitive value to be boxed.</param>
        ///<returns>A local reference to Java object.</returns>
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

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out sbyte value)
        {
            IntPtr method = GetUnboxMethod(obj, "byteValue", "()B");
            value = AndroidJNISafe.CallSByteMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out short value)
        {
            IntPtr method = GetUnboxMethod(obj, "shortValue", "()S");
            value = AndroidJNISafe.CallShortMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out int value)
        {
            IntPtr method = GetUnboxMethod(obj, "intValue", "()I");
            value = AndroidJNISafe.CallIntMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out long value)
        {
            IntPtr method = GetUnboxMethod(obj, "longValue", "()J");
            value = AndroidJNISafe.CallLongMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out float value)
        {
            IntPtr method = GetUnboxMethod(obj, "floatValue", "()F");
            value = AndroidJNISafe.CallFloatMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out double value)
        {
            IntPtr method = GetUnboxMethod(obj, "doubleValue", "()D");
            value = AndroidJNISafe.CallDoubleMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out char value)
        {
            IntPtr method = GetUnboxMethod(obj, "charValue", "()C");
            value = AndroidJNISafe.CallCharMethod(obj, method, new Span<jvalue>());
        }

        ///<summary>Converts Java object of a boxed type to its primitive counterpart.</summary>
        ///<remarks>Each primitive type in Java has a counterpart class, for example java.lang.Integer class is a counterpart for primitive type int.
        ///Note, that this methods is designed Java object to be of the correct type depending of the value you want to get. Fix example, if value is short, then obj is expected to be of type java.lang.Short, though in practice any descendant of java.lang.Number should work for any numeric destination, possibly with rounding or loss.</remarks>
        ///<param name="obj">A Java object that is a counterpart of a primitive type of the value parameter.</param>
        ///<param name="value">Destination for the primitive value of obj.</param>
        public static void Unbox(IntPtr obj, out bool value)
        {
            IntPtr method = GetUnboxMethod(obj, "booleanValue", "()Z");
            value = AndroidJNISafe.CallBooleanMethod(obj, method, new Span<jvalue>());
        }
    }

    // 'Raw' JNI interface to Android Dalvik (Java) VM from Scripting (CS/JS)
    ///<summary>'Raw' JNI interface to Android Java VM from Unity scripting (C#).
    ///
    ///**Note:** Using raw JNI functions requires advanced knowledge of the Android Java Native Interface (JNI). Please take note.</summary>
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

        [NativeMethod(IsThreadSafe = true)]
        private static extern void ReleaseStringChars(JStringBinding str);

        ///<summary>Returns the pointer to JavaVM.</summary>
        ///<returns>The pointer to the JavaVM.</returns>
        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("jni", StaticAccessorType.DoubleColon)]
        public static extern IntPtr GetJavaVM();

        // Attaches the current thread to a Java (Dalvik) VM.
        ///<summary>Attaches the current thread to a Java (Dalvik) VM.</summary>
        ///<remarks>A thread must be attached to the VM before any other JNI calls can be made.</remarks>
        ///<returns>Zero on success. A negative number on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int AttachCurrentThread();

        // Detaches the current thread from a Java (Dalvik) VM.
        ///<summary>Detaches the current thread from a Java (Dalvik) VM.</summary>
        ///<remarks>A thread must be detached from the VM before exiting.</remarks>
        ///<returns>Zero on success. A negative number on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int DetachCurrentThread();

        [RequiredByNativeCode]
        private static void InvokeAction(Action action) => action();

        ///<summary>Calls <see cref="AndroidJNI.AttachCurrentThread" /> (if necessary) before invoking a delegate, and then calls <see cref="AndroidJNI.DetachCurrentThread" />.</summary>
        ///<remarks>**Note:** Use this method to invoke Java methods on threads other than the main thread.</remarks>
        ///<param name="action">The delegate to invoke while the current thread is attached to the Java VM.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[using System;
        ///using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class JavaThreads : MonoBehaviour
        ///{
        ///    public void Start()
        ///    {
        ///        var t = new System.Threading.Thread(() =>
        ///        {
        ///            AndroidJNI.InvokeAttached(() => Debug.Log(AndroidApplication.currentActivity.Call<string>("getLocalClassName")));
        ///        });
        ///        t.Start();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void InvokeAttached(Action action);

        // Returns the version of the native method interface.
        ///<summary>Returns the version of the native method interface.</summary>
        ///<returns>The version of the native method interface.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetVersion();

        // This function loads a locally-defined class.
        ///<summary>This function loads a locally-defined class.</summary>
        ///<param name="name">The fully qualified class name, or the name of an array class.</param>
        ///<returns>A local reference to the class, or null if the class couldn't be found.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr FindClass(string name);

        // Converts a <tt>java.lang.reflect.Method</tt> or <tt>java.lang.reflect.Constructor</tt> object to a method ID.
        ///<summary>Converts a &lt;tt&gt;java.lang.reflect.Method&lt;/tt&gt; or &lt;tt&gt;java.lang.reflect.Constructor&lt;/tt&gt; object to a method ID.</summary>
        ///<param name="refMethod">The java.lang.reflect.Method or java.lang.reflect.Constructor object to convert.</param>
        ///<returns>The corresponding method ID.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr FromReflectedMethod(IntPtr refMethod);
        // Converts a <tt>java.lang.reflect.Field</tt> to a field ID.
        ///<summary>Converts a &lt;tt&gt;java.lang.reflect.Field&lt;/tt&gt; to a field ID.</summary>
        ///<param name="refField">The java.lang.reflect.Field object to convert.</param>
        ///<returns>The corresponding field ID.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr FromReflectedField(IntPtr refField);
        // Converts a method ID derived from clazz to a <tt>java.lang.reflect.Method</tt> or <tt>java.lang.reflect.Constructor</tt> object.
        ///<summary>Converts a method ID derived from clazz to a &lt;tt&gt;java.lang.reflect.Method&lt;/tt&gt; or &lt;tt&gt;java.lang.reflect.Constructor&lt;/tt&gt; object.</summary>
        ///<param name="clazz">The Java class that declares the method.</param>
        ///<param name="methodID">The ID of the method to convert.</param>
        ///<param name="isStatic">Set to true for a static method. Set to false for an instance (nonstatic) method.</param>
        ///<returns>A local reference to the corresponding java.lang.reflect.Method or java.lang.reflect.Constructor object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr ToReflectedMethod(IntPtr clazz, IntPtr methodID, bool isStatic);
        // Converts a field ID derived from cls to a <tt>java.lang.reflect.Field</tt> object.
        ///<summary>Converts a field ID derived from cls to a &lt;tt&gt;java.lang.reflect.Field&lt;/tt&gt; object.</summary>
        ///<param name="clazz">The Java class that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="isStatic">Set to true for a static field. Set to false for an instance (nonstatic) field.</param>
        ///<returns>A local reference to the corresponding java.lang.reflect.Field object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr ToReflectedField(IntPtr clazz, IntPtr fieldID, bool isStatic);

        // If <tt>clazz</tt> represents any class other than the class <tt>Object</tt>, then this function returns the object that represents the superclass of the class specified by <tt>clazz</tt>.
        ///<summary>If &lt;tt&gt;clazz&lt;/tt&gt; represents any class other than the class &lt;tt&gt;Object&lt;/tt&gt;, then this function returns the object that represents the superclass of the class specified by &lt;tt&gt;clazz&lt;/tt&gt;.</summary>
        ///<remarks>If &lt;tt&gt;clazz&lt;/tt&gt; specifies the class &lt;tt&gt;Object&lt;/tt&gt;, or &lt;tt&gt;clazz&lt;/tt&gt; represents an interface, this function returns &lt;tt&gt;NULL&lt;/tt&gt;.</remarks>
        ///<param name="clazz">The class whose superclass is returned.</param>
        ///<returns>A local reference to the superclass, or null.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetSuperclass(IntPtr clazz);
        // Determines whether an object of <tt>clazz1</tt> can be safely cast to <tt>clazz2</tt>.
        ///<summary>Determines whether an object of &lt;tt&gt;clazz1&lt;/tt&gt; can be safely cast to &lt;tt&gt;clazz2&lt;/tt&gt;.</summary>
        ///<param name="clazz1">The class to test for assignability.</param>
        ///<param name="clazz2">The class to test against.</param>
        ///<returns>True if an object of the first class can be safely cast to the second. Otherwise, false.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsAssignableFrom(IntPtr clazz1, IntPtr clazz2);

        // Causes a <tt>java.lang.Throwable</tt> object to be thrown.
        ///<summary>Causes a &lt;tt&gt;java.lang.Throwable&lt;/tt&gt; object to be thrown.</summary>
        ///<param name="obj">The throwable object to throw.</param>
        ///<returns>Zero on success. A negative value on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int Throw(IntPtr obj);
        // Constructs an exception object from the specified class with the <tt>message</tt> specified by message and causes that exception to be thrown.
        ///<summary>Constructs an exception object from the specified class with the &lt;tt&gt;message&lt;/tt&gt; specified by message and causes that exception to be thrown.</summary>
        ///<param name="clazz">The class of the exception to construct and throw.</param>
        ///<param name="message">The exception message.</param>
        ///<returns>Zero on success. A negative value on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int ThrowNew(IntPtr clazz, string message);
        // Determines if an exception is being thrown
        ///<summary>Determines if an exception is being thrown.</summary>
        ///<returns>A local reference to the exception being thrown, or null if no exception is currently being thrown.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr ExceptionOccurred();
        // Prints an exception and a backtrace of the stack to the <tt>logcat</tt>
        ///<summary>Prints an exception and a backtrace of the stack to the &lt;tt&gt;logcat&lt;/tt&gt;</summary>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void ExceptionDescribe();
        // Clears any exception that is currently being thrown.
        ///<summary>Clears any exception that is currently being thrown.</summary>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void ExceptionClear();
        // Raises a fatal error and does not expect the VM to recover. This function does not return.
        ///<summary>Raises a fatal error and does not expect the VM to recover. This function does not return.</summary>
        ///<param name="message">The exception message.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void FatalError(string message);

        // Creates a new local reference frame, in which at least a given number of local references can be created.
        ///<summary>Creates a new local reference frame, in which at least a given number of local references can be created.</summary>
        ///<param name="capacity">The minimum number of local references the new frame can hold.</param>
        ///<returns>Zero on success. A negative value on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int PushLocalFrame(int capacity);
        // Pops off the current local reference frame, frees all the local references, and returns a local reference in the previous local reference frame for the given <tt>result</tt> object.
        ///<summary>Pops off the current local reference frame, frees all the local references, and returns a local reference in the previous local reference frame for the given &lt;tt&gt;result&lt;/tt&gt; object.</summary>
        ///<param name="ptr">The result object to return a local reference to, in the previous local reference frame.</param>
        ///<returns>A local reference to the given result object, valid in the previous local reference frame.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr PopLocalFrame(IntPtr ptr);

        // Creates a new global reference to the object referred to by the <tt>obj</tt> argument.
        ///<summary>Creates a new global reference to the object referred to by the &lt;tt&gt;obj&lt;/tt&gt; argument.</summary>
        ///<param name="obj">The reference to convert.</param>
        ///<returns>The new reference, or null if the object referred to by the original reference has already been garbage collected.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewGlobalRef(IntPtr obj);
        // Deletes the global reference pointed to by <tt>obj</tt>.
        ///<summary>Deletes the global reference pointed to by &lt;tt&gt;obj&lt;/tt&gt;.</summary>
        ///<param name="obj">The reference to delete.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void DeleteGlobalRef(IntPtr obj);
        // Checks that current thread is attached and deletes the global reference pointed to by <tt>obj</tt>.
        [NativeMethod(IsThreadSafe = true)]
        internal static extern void QueueDeleteGlobalRef(IntPtr obj);
        // Returns the number of global references to delete in queue.
        [NativeMethod(IsThreadSafe = true)]
        internal static extern uint GetQueueGlobalRefsCount();
        // Force delete all global references in queue.
        [NativeMethod(IsThreadSafe = true)]
        internal static extern void CleanQueueGlobalRefs();
        // Creates a new global weak reference to the object referred to by the <tt>obj</tt> argument.
        ///<summary>Creates a new global weak reference to the object referred to by the &lt;tt&gt;obj&lt;/tt&gt; argument.</summary>
        ///<param name="obj">The reference to convert.</param>
        ///<returns>The new reference, or null if the object referred to by the original reference has already been garbage collected.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewWeakGlobalRef(IntPtr obj);
        // Deletes the global weak reference pointed to by <tt>obj</tt>.
        ///<summary>Deletes the global weak reference pointed to by &lt;tt&gt;obj&lt;/tt&gt;.</summary>
        ///<param name="obj">The reference to delete.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void DeleteWeakGlobalRef(IntPtr obj);
        // Creates a new local reference that refers to the same object as <tt>obj</tt>.
        ///<summary>Creates a new local reference that refers to the same object as &lt;tt&gt;obj&lt;/tt&gt;.</summary>
        ///<param name="obj">The reference to convert.</param>
        ///<returns>The new reference, or null if the object referred to by the original reference has already been garbage collected.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewLocalRef(IntPtr obj);
        // Deletes the local reference pointed to by <tt>obj</tt>.
        ///<summary>Deletes the local reference pointed to by &lt;tt&gt;obj&lt;/tt&gt;.</summary>
        ///<param name="obj">The reference to delete.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void DeleteLocalRef(IntPtr obj);
        // Tests whether two references refer to the same Java object.
        ///<summary>Tests whether two references refer to the same Java object.</summary>
        ///<param name="obj1">The first reference to compare.</param>
        ///<param name="obj2">The second reference to compare.</param>
        ///<returns>True if both references refer to the same Java object, or are both null. Otherwise, false.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsSameObject(IntPtr obj1, IntPtr obj2);

        // Ensures that at least a given number of local references can be created in the current thread.
        ///<summary>Ensures that at least a given number of local references can be created in the current thread.</summary>
        ///<param name="capacity">The minimum number of local references the current thread can hold.</param>
        ///<returns>Zero on success. A negative value on failure.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int EnsureLocalCapacity(int capacity);

        //-------------------------------------------

        // Allocates a new Java object without invoking any of the constructors for the object.
        ///<summary>Allocates a new Java object without invoking any of the constructors for the object.</summary>
        ///<param name="clazz">The Java class to instantiate.</param>
        ///<returns>A local reference to the newly allocated object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr AllocObject(IntPtr clazz);

        // Constructs a new Java object. The method ID indicates which constructor method to invoke. This ID must be obtained by calling GetMethodID() with <init> as the method name and void (V) as the return type.
        ///<summary>Constructs a new Java object. The method ID indicates which constructor method to invoke. This ID must be obtained by calling GetMethodID() with &lt;init&gt; as the method name and void (V) as the return type.</summary>
        ///<param name="clazz">The Java class to instantiate.</param>
        ///<param name="methodID">The ID of the constructor to invoke.</param>
        ///<param name="args">An array of arguments to pass to the constructor.</param>
        ///<returns>A local reference to the newly constructed object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static IntPtr NewObject(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return NewObject(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Constructs a new Java object. The method ID indicates which constructor method to invoke. This ID must be obtained by calling `GetMethodID` with `init` as the method name and `void (V)` as the return type.</summary>
        ///<param name="clazz">The Java class to instantiate.</param>
        ///<param name="methodID">The ID of the constructor to invoke.</param>
        ///<param name="args">An array of arguments to pass to the constructor.</param>
        ///<returns>A local reference to the newly constructed object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Constructs a new Java object. The method ID indicates which constructor method to invoke. This ID must be obtained by calling GetMethodID() with &lt;init&gt; as the method name and void (V) as the return type.</summary>
        ///<param name="clazz">The Java class to instantiate.</param>
        ///<param name="methodID">The ID of the constructor to invoke.</param>
        ///<param name="args">An array of arguments to pass to the constructor.</param>
        ///<returns>A local reference to the newly constructed object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr NewObjectA(IntPtr clazz, IntPtr methodID, jvalue* args);

        // Returns the class of an object.
        ///<summary>Returns the class of an object.</summary>
        ///<param name="obj">The object whose class is returned.</param>
        ///<returns>A local reference to the object's class.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetObjectClass(IntPtr obj);
        // Tests whether an object is an instance of a class.
        ///<summary>Tests whether an object is an instance of a class.</summary>
        ///<param name="obj">The object to test.</param>
        ///<param name="clazz">The class to test against.</param>
        ///<returns>True if the object is an instance of the class. Otherwise, false.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsInstanceOf(IntPtr obj, IntPtr clazz);

        // Returns the method ID for an instance (nonstatic) method of a class or interface.
        ///<summary>Returns the method ID for an instance (nonstatic) method of a class or interface.</summary>
        ///<param name="clazz">The Java class to scan.</param>
        ///<param name="name">The name of the method, as declared in Java.</param>
        ///<param name="sig">The JNI method signature.</param>
        ///<returns>The method ID, or zero if the method couldn't be found.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetMethodID(IntPtr clazz, string name, string sig);
        // Returns the field ID for an instance (nonstatic) field of a class.
        ///<summary>Returns the field ID for an instance (nonstatic) field of a class.</summary>
        ///<param name="clazz">The Java class to scan.</param>
        ///<param name="name">The name of the field, as declared in Java.</param>
        ///<param name="sig">The JNI field signature.</param>
        ///<returns>The field ID, or zero if the field couldn't be found.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetFieldID(IntPtr clazz, string name, string sig);
        // Returns the method ID for a static method of a class.
        ///<summary>Returns the method ID for a static method of a class.</summary>
        ///<param name="clazz">The Java class to scan.</param>
        ///<param name="name">The name of the method, as declared in Java.</param>
        ///<param name="sig">The JNI method signature.</param>
        ///<returns>The method ID, or zero if the method couldn't be found.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetStaticMethodID(IntPtr clazz, string name, string sig);
        // Returns the field ID for a static field of a class.
        ///<summary>Returns the field ID for a static field of a class.</summary>
        ///<param name="clazz">The Java class to scan.</param>
        ///<param name="name">The name of the field, as declared in Java.</param>
        ///<param name="sig">The JNI field signature.</param>
        ///<returns>The field ID, or zero if the field couldn't be found.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetStaticFieldID(IntPtr clazz, string name, string sig);

        ///<summary>Constructs a new &lt;tt&gt;java.lang.String&lt;/tt&gt; object from an array of Unicode characters.</summary>
        ///<param name="chars">The array of Unicode characters to construct the string from.</param>
        ///<returns>A local reference to the newly constructed java.lang.String object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static IntPtr NewString(string chars)
        {
            return NewStringFromStr(chars);
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern IntPtr NewStringFromStr(string chars);

        // Constructs a new <tt>java.lang.String</tt> object from an array of Unicode characters.
        ///<summary>Constructs a new &lt;tt&gt;java.lang.String&lt;/tt&gt; object from an array of Unicode characters.</summary>
        ///<param name="chars">The array of Unicode characters to construct the string from.</param>
        ///<returns>A local reference to the newly constructed java.lang.String object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewString(char[] chars);

        // Constructs a new <tt>java.lang.String</tt> object from an array of characters in modified UTF-8 encoding.
        ///<summary>Constructs a new &lt;tt&gt;java.lang.String&lt;/tt&gt; object from an array of characters in modified UTF-8 encoding.</summary>
        ///<param name="bytes">The modified UTF-8 encoded string data.</param>
        ///<returns>A local reference to the newly constructed java.lang.String object.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewStringUTF(string bytes);

        ///<summary>Returns a pointer to the array of Unicode characters of the string. This pointer is valid until ReleaseStringchars() is called.</summary>
        ///<remarks>This method is a modification of the original &lt;tt&gt;GetStringChars&lt;/tt&gt;, which returns a pointer to an array of bytes.</remarks>
        ///<param name="str">The Java string.</param>
        ///<returns>The Unicode characters of the string.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static string GetStringChars(IntPtr str)
        {
            using (var jstring = GetStringCharsInternal(str))
                return jstring.ToString();
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern JStringBinding GetStringCharsInternal(IntPtr str);

        // Returns the length (the count of Unicode characters) of a Java string.
        ///<summary>Returns the length (the count of Unicode characters) of a Java string.</summary>
        ///<param name="str">The Java string.</param>
        ///<returns>The number of Unicode characters in the string.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetStringLength(IntPtr str);
        // Returns the length in bytes of the modified UTF-8 representation of a string.
        ///<summary>Returns the length in bytes of the modified UTF-8 representation of a string.</summary>
        ///<param name="str">The Java string.</param>
        ///<returns>The length, in bytes, of the modified UTF-8 representation of the string.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetStringUTFLength(IntPtr str);
        // Returns a managed string object representing the string in modified UTF-8 encoding.
        ///<summary>Returns a managed string object representing the string in modified UTF-8 encoding.</summary>
        ///<remarks>This method is a modification of the original &lt;tt&gt;GetStringUTFChars&lt;/tt&gt;, which returns a pointer to an array of bytes.</remarks>
        ///<param name="str">The Java string.</param>
        ///<returns>The string, converted from its modified UTF-8 representation.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetStringUTFChars(IntPtr str);

        //---------------------------------------------

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<remarks>This is a convenience function that calls CallObjectMethod() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<seealso cref="AndroidJNI.CallObjectMethod" />
        public static string CallStringMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallStringMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<remarks>This is a convenience function that calls `CallObjectMethod` with the same parameters, but creates a managed string from the result.</remarks>
        ///<seealso cref="AndroidJNI.CallObjectMethod" />
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<remarks>This is a convenience function that calls CallObjectMethod() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<seealso cref="AndroidJNI.CallObjectMethodUnsafe" />
        public static unsafe string CallStringMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args)
        {
            using (var jstring = CallStringMethodUnsafeInternal(obj, methodID, args))
                return jstring.ToString();
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern unsafe JStringBinding CallStringMethodUnsafeInternal(IntPtr obj, IntPtr methodID, jvalue* args);

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<remarks>This method returns a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallObjectMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<remarks>This method returns a reference to a `java.lang.Object`, or a subclass thereof.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<remarks>This method returns a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr CallObjectMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int32 CallIntMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallIntMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int32 CallIntMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static bool CallBooleanMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallBooleanMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe bool CallBooleanMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int16 CallShortMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallShortMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int16 CallShortMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.CallByteMethod" /> is obsolete. Use <see cref="AndroidJNI.CallSByteMethod" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.CallByteMethod is obsolete. Use AndroidJNI.CallSByteMethod method instead")]
        public static Byte CallByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return (Byte)CallSByteMethod(obj, methodID, args);
        }

        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static SByte CallSByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallSByteMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe SByte CallSByteMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Char CallCharMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallCharMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Char CallCharMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static float CallFloatMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallFloatMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe float CallFloatMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static double CallDoubleMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallDoubleMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe double CallDoubleMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int64 CallLongMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            return CallLongMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int64 CallLongMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);
        // Calls an instance (nonstatic) Java method defined by <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static void CallVoidMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            CallVoidMethod(obj, methodID, new Span<jvalue>(args));
        }
        ///<summary>Calls a Java instance method defined by `methodID`, optionally passing an array of arguments (`args`) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Calls a Java instance method defined by &lt;tt&gt;methodID&lt;/tt&gt;, optionally passing an array of arguments (&lt;tt&gt;args&lt;/tt&gt;) to the method.</summary>
        ///<param name="obj">The Java object on which to call the instance method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void CallVoidMethodUnsafe(IntPtr obj, IntPtr methodID, jvalue* args);

        //---------------------------------------------

        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<remarks>This is a convenience function that calls GetObjectField() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field, as a managed string.</returns>
        ///<seealso cref="AndroidJNI.GetObjectField" />
        public static string GetStringField(IntPtr obj, IntPtr fieldID)
        {
            using (var jstring = GetStringFieldInternal(obj, fieldID))
                return jstring.ToString();
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern JStringBinding GetStringFieldInternal(IntPtr obj, IntPtr fieldID);

        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<remarks>The result is a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>A local reference to the Java object stored in the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetObjectField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool GetBooleanField(IntPtr obj, IntPtr fieldID);
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.GetByteField" /> is obsolete. Use <see cref="AndroidJNI.GetSByteField" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.GetByteField is obsolete. Use AndroidJNI.GetSByteField method instead")]
        public static Byte GetByteField(IntPtr obj, IntPtr fieldID)
        {
            return (Byte)GetSByteField(obj, fieldID);
        }

        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern SByte GetSByteField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Char GetCharField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int16 GetShortField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int32 GetIntField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int64 GetLongField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern float GetFloatField(IntPtr obj, IntPtr fieldID);
        // This function returns the value of an instance (nonstatic) field of an object.
        ///<summary>Returns the value of an instance (nonstatic) field of an object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern double GetDoubleField(IntPtr obj, IntPtr fieldID);

        //---------------------------------------------

        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<remarks>This is a convenience function that calls SetObjectField() with the same parameters, but performs the necessary marshalling of the string value.</remarks>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso cref="AndroidJNI.SetObjectField" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStringField(IntPtr obj, IntPtr fieldID, string val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<remarks>The value to set is a reference to either a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetObjectField(IntPtr obj, IntPtr fieldID, IntPtr val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetBooleanField(IntPtr obj, IntPtr fieldID, bool val);
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.SetByteField" /> is obsolete. Use <see cref="AndroidJNI.SetSByteField" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.SetByteField is obsolete. Use AndroidJNI.SetSByteField method instead")]
        public static void SetByteField(IntPtr obj, IntPtr fieldID, Byte val)
        {
            SetSByteField(obj, fieldID, (SByte)val);
        }

        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetSByteField(IntPtr obj, IntPtr fieldID, SByte val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetCharField(IntPtr obj, IntPtr fieldID, Char val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetShortField(IntPtr obj, IntPtr fieldID, Int16 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetIntField(IntPtr obj, IntPtr fieldID, Int32 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetLongField(IntPtr obj, IntPtr fieldID, Int64 val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetFloatField(IntPtr obj, IntPtr fieldID, float val);
        // This function sets the value of an instance (nonstatic) field of an object.
        ///<summary>Sets the value of an instance field of the specified object.</summary>
        ///<param name="obj">The Java object that declares the field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetDoubleField(IntPtr obj, IntPtr fieldID, double val);


        //---------------------------------------------

        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<remarks>This is a convenience function that calls CallStaticObjectMethod() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<seealso cref="AndroidJNI.CallStaticObjectMethod" />
        public static string CallStaticStringMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticStringMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<remarks>This is a convenience function that calls `CallStaticObjectMethod()` with the same parameters, but creates a managed string from the result.</remarks>
        ///<seealso cref="AndroidJNI.CallStaticObjectMethod" />
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<remarks>This is a convenience function that calls CallStaticObjectMethod() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The managed string returned by the method.</returns>
        ///<seealso cref="AndroidJNI.CallStaticObjectMethodUnsafe" />
        public static unsafe string CallStaticStringMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args)
        {
            using (var jstring = CallStaticStringMethodUnsafeInternal(clazz, methodID, args))
                return jstring.ToString();
        }
        [NativeMethod(IsThreadSafe = true)]
        private static extern unsafe JStringBinding CallStaticStringMethodUnsafeInternal(IntPtr clazz, IntPtr methodID, jvalue* args);

        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<remarks>This method returns a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticObjectMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<remarks>This method returns a reference to a `java.lang.Object`, or a subclass thereof.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<remarks>This method returns a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>A local reference to the Java object returned by the method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr CallStaticObjectMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int32 CallStaticIntMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticIntMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int32 CallStaticIntMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static bool CallStaticBooleanMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticBooleanMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe bool CallStaticBooleanMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int16 CallStaticShortMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticShortMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int16 CallStaticShortMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.CallStaticByteMethod" /> is obsolete. Use <see cref="AndroidJNI.CallStaticSByteMethod" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.CallStaticByteMethod is obsolete. Use AndroidJNI.CallStaticSByteMethod method instead")]
        public static Byte CallStaticByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return (Byte)CallStaticSByteMethod(clazz, methodID, args);
        }

        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static SByte CallStaticSByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticSByteMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe SByte CallStaticSByteMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Char CallStaticCharMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticCharMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Char CallStaticCharMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static float CallStaticFloatMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticFloatMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe float CallStaticFloatMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static double CallStaticDoubleMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticDoubleMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe double CallStaticDoubleMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static Int64 CallStaticLongMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            return CallStaticLongMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<returns>The value returned by the Java method.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe Int64 CallStaticLongMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);
        // Invokes a static method on a Java object, according to the specified <tt>methodID</tt>, optionally passing an array of arguments (<tt>args</tt>) to the method.
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static void CallStaticVoidMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            CallStaticVoidMethod(clazz, methodID, new Span<jvalue>(args));
        }
        ///<summary>Invokes the specified `methodID` static method on a Java object, optionally passing in an array of arguments (`args`).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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
        ///<summary>Invokes the specified &lt;tt&gt;methodID&lt;/tt&gt; static method on a Java object, optionally passing in an array of arguments (&lt;tt&gt;args&lt;/tt&gt;).</summary>
        ///<param name="clazz">The Java class on which to call the static method.</param>
        ///<param name="methodID">The ID of the method to call.</param>
        ///<param name="args">An array of arguments to pass to the method.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void CallStaticVoidMethodUnsafe(IntPtr clazz, IntPtr methodID, jvalue* args);

        //---------------------------------------------

        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<remarks>This is a convenience function that calls GetStaticObjectField() with the same parameters, but creates a managed string from the result.</remarks>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field, as a managed string.</returns>
        ///<seealso cref="AndroidJNI.GetStaticObjectField" />
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static string GetStaticStringField(IntPtr clazz, IntPtr fieldID)
        {
            using (var jstring = GetStaticStringFieldInternal(clazz, fieldID))
                return jstring.ToString();
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern JStringBinding GetStaticStringFieldInternal(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<remarks>The result is a reference to a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>A local reference to the Java object stored in the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool GetStaticBooleanField(IntPtr clazz, IntPtr fieldID);
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.GetStaticByteField" /> is obsolete. Use <see cref="AndroidJNI.GetStaticSByteField" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.GetStaticByteField is obsolete. Use AndroidJNI.GetStaticSByteField method instead")]
        public static Byte GetStaticByteField(IntPtr clazz, IntPtr fieldID)
        {
            return (Byte)GetStaticSByteField(clazz, fieldID);
        }

        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern SByte GetStaticSByteField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Char GetStaticCharField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int16 GetStaticShortField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int32 GetStaticIntField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int64 GetStaticLongField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern float GetStaticFloatField(IntPtr clazz, IntPtr fieldID);
        // This function returns the value of a static field of an object.
        ///<summary>Returns the value of a static field of an object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<returns>The value of the static field.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern double GetStaticDoubleField(IntPtr clazz, IntPtr fieldID);

        //---------------------------------------------

        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<remarks>This is a convenience function that calls SetStaticObjectField() with the same parameters, but performs the necessary marshalling of the string value.</remarks>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso cref="AndroidJNI.SetStaticObjectField" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticStringField(IntPtr clazz, IntPtr fieldID, string val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<remarks>The value to set is a reference to either a &lt;tt&gt;java.lang.Object&lt;/tt&gt;, or a subclass thereof.</remarks>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticObjectField(IntPtr clazz, IntPtr fieldID, IntPtr val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticBooleanField(IntPtr clazz, IntPtr fieldID, bool val);
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.SetStaticByteField" /> is obsolete. Use <see cref="AndroidJNI.SetStaticSByteField" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.SetStaticByteField is obsolete. Use AndroidJNI.SetStaticSByteField method instead")]
        public static void SetStaticByteField(IntPtr clazz, IntPtr fieldID, Byte val)
        {
            SetStaticSByteField(clazz, fieldID, (SByte)val);
        }

        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticSByteField(IntPtr clazz, IntPtr fieldID, SByte val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticCharField(IntPtr clazz, IntPtr fieldID, Char val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticShortField(IntPtr clazz, IntPtr fieldID, Int16 val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticIntField(IntPtr clazz, IntPtr fieldID, Int32 val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticLongField(IntPtr clazz, IntPtr fieldID, Int64 val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticFloatField(IntPtr clazz, IntPtr fieldID, float val);
        // This function ets the value of a static field of an object.
        ///<summary>Sets the value of a static field in the specified object.</summary>
        ///<param name="clazz">The Java class that declares the static field.</param>
        ///<param name="fieldID">The ID of the field.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetStaticDoubleField(IntPtr clazz, IntPtr fieldID, double val);


        //---------------------------------------
        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr ConvertToBooleanArray(Boolean[] array);
        ///<summary>Converts a managed array of `System.Boolean` to a Java array of `boolean`.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of boolean.</returns>
        ///<seealso cref="AndroidJNI.NewBooleanArray" />
        ///<seealso cref="AndroidJNI.SetBooleanArrayElement" />
        public static IntPtr ToBooleanArray(Boolean[] array)
        {
            return array == null ? IntPtr.Zero : ConvertToBooleanArray(array);
        }

        ///<summary>Converts a managed array of System.Byte to a Java array of &lt;tt&gt;byte&lt;/tt&gt;.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.ToByteArray" /> is obsolete. Use <see cref="AndroidJNI.ToSByteArray" /> instead.</remarks>
        ///<seealso cref="AndroidJNI.NewByteArray" />
        ///<seealso cref="AndroidJNI.SetByteArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        [Obsolete("AndroidJNI.ToByteArray is obsolete. Use AndroidJNI.ToSByteArray method instead")]
        public static extern IntPtr ToByteArray(Byte[] array);
        // Convert a managed array of System.SByte to a Java array of <tt>byte</tt>.
        ///<summary>Converts a managed array of System.SByte to a Java array of &lt;tt&gt;byte&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of sbyte.</returns>
        ///<seealso cref="AndroidJNI.NewSByteArray" />
        ///<seealso cref="AndroidJNI.SetSByteArrayElement" />
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
        ///<summary>Converts a managed array of `System.SByte` to a Java array of `byte`.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of sbyte.</returns>
        ///<seealso cref="AndroidJNI.NewSByteArray" />
        ///<seealso cref="AndroidJNI.SetSByteArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToSByteArray(SByte* array, int length);
        // Convert a managed array of System.Char to a Java array of <tt>char</tt>.
        ///<summary>Converts a managed array of System.Char to a Java array of &lt;tt&gt;char&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of char.</returns>
        ///<seealso cref="AndroidJNI.NewCharArray" />
        ///<seealso cref="AndroidJNI.SetCharArrayElement" />
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
        ///<summary>Converts a managed array of System.Char to a Java array of &lt;tt&gt;char&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of char.</returns>
        ///<seealso cref="AndroidJNI.NewCharArray" />
        ///<seealso cref="AndroidJNI.SetCharArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToCharArray(Char* array, int length);
        // Convert a managed array of System.Int16 to a Java array of <tt>short</tt>.
        ///<summary>Converts a managed array of System.Int16 to a Java array of &lt;tt&gt;short&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of short.</returns>
        ///<seealso cref="AndroidJNI.NewShortArray" />
        ///<seealso cref="AndroidJNI.SetShortArrayElement" />
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
        ///<summary>Converts a managed array of System.Int16 to a Java array of &lt;tt&gt;short&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of short.</returns>
        ///<seealso cref="AndroidJNI.NewShortArray" />
        ///<seealso cref="AndroidJNI.SetShortArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToShortArray(Int16* array, int length);
        // Convert a managed array of System.Int32 to a Java array of <tt>int</tt>.
        ///<summary>Converts a managed array of System.Int32 to a Java array of &lt;tt&gt;int&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of int.</returns>
        ///<seealso cref="AndroidJNI.NewIntArray" />
        ///<seealso cref="AndroidJNI.SetIntArrayElement" />
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
        ///<summary>Converts a managed array of System.Int32 to a Java array of &lt;tt&gt;int&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of int.</returns>
        ///<seealso cref="AndroidJNI.NewIntArray" />
        ///<seealso cref="AndroidJNI.SetIntArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToIntArray(Int32* array, int length);
        // Convert a managed array of System.Int64 to a Java array of <tt>long</tt>.
        ///<summary>Converts a managed array of System.Int64 to a Java array of &lt;tt&gt;long&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of long.</returns>
        ///<seealso cref="AndroidJNI.NewLongArray" />
        ///<seealso cref="AndroidJNI.SetLongArrayElement" />
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
        ///<summary>Converts a managed array of System.Int64 to a Java array of &lt;tt&gt;long&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of long.</returns>
        ///<seealso cref="AndroidJNI.NewLongArray" />
        ///<seealso cref="AndroidJNI.SetLongArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToLongArray(Int64* array, int length);
        // Convert a managed array of System.Single to a Java array of <tt>float</tt>.
        ///<summary>Converts a managed array of System.Single to a Java array of &lt;tt&gt;float&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of float.</returns>
        ///<seealso cref="AndroidJNI.NewFloatArray" />
        ///<seealso cref="AndroidJNI.SetFloatArrayElement" />
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
        ///<summary>Converts a managed array of System.Single to a Java array of &lt;tt&gt;float&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of float.</returns>
        ///<seealso cref="AndroidJNI.NewFloatArray" />
        ///<seealso cref="AndroidJNI.SetFloatArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToFloatArray(float* array, int length);
        // Convert a managed array of System.Double to a Java array of <tt>double</tt>.
        ///<summary>Converts a managed array of System.Double to a Java array of &lt;tt&gt;double&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of double.</returns>
        ///<seealso cref="AndroidJNI.NewDoubleArray" />
        ///<seealso cref="AndroidJNI.SetDoubleArrayElement" />
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
        ///<summary>Converts a managed array of System.Double to a Java array of `double`.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<returns>A local reference to the newly constructed Java array of double.</returns>
        ///<seealso cref="AndroidJNI.NewDoubleArray" />
        ///<seealso cref="AndroidJNI.SetDoubleArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToDoubleArray(double* array, int length);

        // Convert a managed array of System.IntPtr, representing Java objects, to a Java array of <tt>java.lang.Object</tt>.
        ///<summary>Converts a managed array of System.IntPtr, representing Java objects, to a Java array of &lt;tt&gt;java.lang.Object&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="length">The number of elements in the array.</param>
        ///<param name="arrayClass">The class of the array's elements.</param>
        ///<returns>A local reference to the newly constructed Java array of object.</returns>
        ///<seealso cref="AndroidJNI.NewObjectArray" />
        ///<seealso cref="AndroidJNI.SetObjectArrayElement" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr ToObjectArray(IntPtr* array, int length, IntPtr arrayClass);

        ///<summary>Converts a managed array of System.IntPtr, representing Java objects, to a Java array of `java.lang.Object`.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<param name="arrayClass">The class of the array's elements.</param>
        ///<returns>A local reference to the newly constructed Java array of object.</returns>
        ///<seealso cref="AndroidJNI.NewObjectArray" />
        ///<seealso cref="AndroidJNI.SetObjectArrayElement" />
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
        ///<summary>Converts a managed array of System.IntPtr, representing Java objects, to a Java array of &lt;tt&gt;java.lang.Object&lt;/tt&gt;.</summary>
        ///<param name="array">The managed array to convert.</param>
        ///<returns>A local reference to the newly constructed Java array of object.</returns>
        ///<seealso cref="AndroidJNI.NewObjectArray" />
        ///<seealso cref="AndroidJNI.SetObjectArrayElement" />
        public static IntPtr ToObjectArray(IntPtr[] array)
        {
            return ToObjectArray(array, IntPtr.Zero);
        }

        // Convert a Java array of <tt>boolean</tt> to a managed array of System.Boolean.
        ///<summary>Converts a Java array of &lt;tt&gt;boolean&lt;/tt&gt; to a managed array of System.Boolean.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetBooleanArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern Boolean[] FromBooleanArray(IntPtr array);
        ///<summary>Converts a Java array of &lt;tt&gt;byte&lt;/tt&gt; to a managed array of System.Byte.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.FromByteArray" /> is obsolete. Use <see cref="AndroidJNI.FromSByteArray" /> instead.</remarks>
        ///<seealso cref="AndroidJNI.GetByteArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [Obsolete("AndroidJNI.FromByteArray is obsolete. Use AndroidJNI.FromSByteArray method instead")]
        public static extern Byte[] FromByteArray(IntPtr array);
        // Convert a Java array of <tt>byte</tt> to a managed array of System.SByte.
        ///<summary>Converts a Java array of &lt;tt&gt;byte&lt;/tt&gt; to a managed array of System.SByte.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetSByteArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern SByte[] FromSByteArray(IntPtr array);
        // Convert a Java array of <tt>char</tt> to a managed array of System.Char.
        ///<summary>Converts a Java array of &lt;tt&gt;char&lt;/tt&gt; to a managed array of System.Char.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetCharArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern Char[] FromCharArray(IntPtr array);
        // Convert a Java array of <tt>short</tt> to a managed array of System.Int16.
        ///<summary>Converts a Java array of &lt;tt&gt;short&lt;/tt&gt; to a managed array of System.Int16.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetShortArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern Int16[] FromShortArray(IntPtr array);
        // Convert a Java array of <tt>int</tt> to a managed array of System.Int32.
        ///<summary>Converts a Java array of &lt;tt&gt;int&lt;/tt&gt; to a managed array of System.Int32.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetIntArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern Int32[] FromIntArray(IntPtr array);
        // Convert a Java array of <tt>long</tt> to a managed array of System.Int64.
        ///<summary>Converts a Java array of &lt;tt&gt;long&lt;/tt&gt; to a managed array of System.Int64.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetFloatArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern Int64[] FromLongArray(IntPtr array);
        // Convert a Java array of <tt>float</tt> to a managed array of System.Single.
        ///<summary>Converts a Java array of &lt;tt&gt;float&lt;/tt&gt; to a managed array of System.Single.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetFloatArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern float[] FromFloatArray(IntPtr array);
        // Convert a Java array of <tt>double</tt> to a managed array of System.Double.
        ///<summary>Converts a Java array of &lt;tt&gt;double&lt;/tt&gt; to a managed array of System.Double.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetDoubleArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern double[] FromDoubleArray(IntPtr array);
        // Convert a Java array of <tt>java.lang.Object</tt> to a managed array of System.IntPtr, representing Java objects.
        ///<summary>Converts a Java array of &lt;tt&gt;java.lang.Object&lt;/tt&gt; to a managed array of System.IntPtr, representing Java objects.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<returns>A managed array containing a copy of the Java array's elements.</returns>
        ///<seealso cref="AndroidJNI.GetFloatArrayElement" />
        ///<seealso cref="AndroidJNI.GetArrayLength" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr[] FromObjectArray(IntPtr array);

        // Returns the number of elements in the array.
        ///<summary>Returns the number of elements in the array.</summary>
        ///<param name="array">The Java array whose length is returned.</param>
        ///<returns>The number of elements in the array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetArrayLength(IntPtr array);

        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewBooleanArray(int size);
        ///<summary>Constructs a new primitive array object.</summary>
        ///<remarks>
        ///  <see cref="AndroidJNI.NewByteArray" /> is obsolete. Use <see cref="AndroidJNI.NewSByteArray" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.NewByteArray is obsolete. Use AndroidJNI.NewSByteArray method instead")]
        public static IntPtr NewByteArray(int size)
        {
            return NewSByteArray(size);
        }

        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewSByteArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewCharArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewShortArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewIntArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewLongArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewFloatArray(int size);
        // Construct a new primitive array object.
        ///<summary>Constructs a new primitive array object.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewDoubleArray(int size);
        // Constructs a new array holding objects in class <tt>clazz</tt>. All elements are initially set to <tt>obj</tt>.
        ///<summary>Constructs a new array holding objects in class &lt;tt&gt;clazz&lt;/tt&gt;. All elements are initially set to &lt;tt&gt;obj&lt;/tt&gt;.</summary>
        ///<param name="size">The number of elements in the array to construct.</param>
        ///<param name="clazz">The element class of the array to construct.</param>
        ///<param name="obj">The value used to initialize every element of the array.</param>
        ///<returns>A local reference to the newly constructed Java array.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr NewObjectArray(int size, IntPtr clazz, IntPtr obj);

        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetBooleanArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool GetBooleanArrayElement(IntPtr array, int index);
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetByteArrayRegion(), called with region size set to 1.
        ///
        ///<see cref="AndroidJNI.GetByteArrayElement" /> is obsolete. Use <see cref="AndroidJNI.GetSByteArrayElement" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.GetByteArrayElement is obsolete. Use AndroidJNI.GetSByteArrayElement method instead")]
        public static Byte GetByteArrayElement(IntPtr array, int index)
        {
            return (Byte)GetSByteArrayElement(array, index);
        }

        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetByteArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern SByte GetSByteArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetCharArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Char GetCharArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetShortArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int16 GetShortArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetIntArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int32 GetIntArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetLongArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern Int64 GetLongArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetFloatArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern float GetFloatArrayElement(IntPtr array, int index);
        // Returns the value of one element of a primitive array.
        ///<summary>Returns the value of one element of a primitive array.</summary>
        ///<remarks>This function is a special case of GetDoubleArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern double GetDoubleArrayElement(IntPtr array, int index);
        // Returns an element of an <tt>Object</tt> array.
        ///<summary>Returns an element of an &lt;tt&gt;Object&lt;/tt&gt; array.</summary>
        ///<param name="array">The Java array to read from.</param>
        ///<param name="index">The index of the array element.</param>
        ///<returns>The value of the array element.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr GetObjectArrayElement(IntPtr array, int index);

        // Sets the value of one element in a primitive array.
        ///<summary>Sets the boolean value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetBooleanArrayRegion(), called with region size set to 1.
        ///
        ///This overload is obsolete. Use the overload that takes the bool &lt;tt&gt;val&lt;/tt&gt; parameter instead.</remarks>
        ///<param name="array">The array of native booleans.</param>
        ///<param name="index">Index of the array element to set.</param>
        ///<param name="val">The value to set - for 'true' use 1, for 'false' use 0.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.SetBooleanArrayElement(IntPtr, int, byte) is obsolete. Use AndroidJNI.SetBooleanArrayElement(IntPtr, int, bool) method instead")]
        public static void SetBooleanArrayElement(IntPtr array, int index, byte val)
        {
            SetBooleanArrayElement(array, index, val != 0);
        }

        ///<summary>Sets the boolean value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetBooleanArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The array of native booleans.</param>
        ///<param name="index">Index of the array element to set.</param>
        ///<param name="val">The value to set.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetBooleanArrayElement(IntPtr array, int index, bool val);
        ///<summary>Sets the sbyte value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetByteArrayRegion(), called with region size set to 1.
        ///
        ///<see cref="AndroidJNI.SetByteArrayElement" /> is obsolete. Use <see cref="AndroidJNI.SetSByteArrayElement" /> instead.</remarks>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [Obsolete("AndroidJNI.SetByteArrayElement is obsolete. Use AndroidJNI.SetSByteArrayElement method instead")]
        public static void SetByteArrayElement(IntPtr array, int index, sbyte val)
        {
            SetSByteArrayElement(array, index, val);
        }

        // Sets the value of one element in a primitive array.
        ///<summary>Sets the sbyte value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetByteArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetSByteArrayElement(IntPtr array, int index, sbyte val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the char value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetCharArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetCharArrayElement(IntPtr array, int index, Char val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the short value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetShortArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetShortArrayElement(IntPtr array, int index, Int16 val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the int value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetIntArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetIntArrayElement(IntPtr array, int index, Int32 val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the long value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetLongArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetLongArrayElement(IntPtr array, int index, Int64 val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the float value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetFloatArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetFloatArrayElement(IntPtr array, int index, float val);
        // Sets the value of one element in a primitive array.
        ///<summary>Sets the double value of one element in a primitive array.</summary>
        ///<remarks>This function is a special case of SetDoubleArrayRegion(), called with region size set to 1.</remarks>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="val">The value to assign to the field.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetDoubleArrayElement(IntPtr array, int index, double val);
        // Sets an element of an <tt>Object</tt> array.
        ///<summary>Sets an element of an &lt;tt&gt;Object&lt;/tt&gt; array.</summary>
        ///<param name="array">The Java array to modify.</param>
        ///<param name="index">The index of the array element.</param>
        ///<param name="obj">The value to assign to the array element.</param>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetObjectArrayElement(IntPtr array, int index, IntPtr obj);

        // ------------------------------------------
        // ByteBuffer support
        ///<summary>Allocates and returns a direct java.nio.ByteBuffer referring to the block of memory starting at the memory address address and extending capacity bytes.</summary>
        ///<param name="buffer">The block of memory the buffer refers to.</param>
        ///<param name="capacity">The capacity, in bytes, of the memory region.</param>
        ///<returns>A local reference to the newly allocated java.nio.ByteBuffer.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe IntPtr NewDirectByteBuffer(byte* buffer, Int64 capacity);

        ///<summary>Allocates and returns a direct java.nio.ByteBuffer referring to the same block of memory as the given NativeArray.</summary>
        ///<param name="buffer">The block of memory the buffer refers to.</param>
        ///<returns>A local reference to the newly allocated java.nio.ByteBuffer.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static IntPtr NewDirectByteBuffer(NativeArray<byte> buffer)
        {
            return NewDirectByteBufferFromNativeArray(buffer);
        }

        ///<summary>Allocates and returns a direct java.nio.ByteBuffer referring to the same block of memory as the given NativeArray.</summary>
        ///<param name="buffer">The block of memory the buffer refers to.</param>
        ///<returns>A local reference to the newly allocated java.nio.ByteBuffer.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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

        ///<summary>Fetches and returns the starting address of the memory region referenced by the given direct java.nio.Buffer.</summary>
        ///<param name="buffer">The direct java.nio.Buffer.</param>
        ///<returns>The starting address of the memory region, or null if the buffer isn't a direct buffer or direct buffers aren't supported.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        public static unsafe sbyte* GetDirectBufferAddress(IntPtr buffer)
        {
            return null;
        }

        ///<summary>Fetches and returns the capacity of the memory region referenced by the given direct java.nio.Buffer.</summary>
        ///<param name="buffer">The direct java.nio.Buffer.</param>
        ///<returns>The capacity, in bytes, of the memory region, or -1 if the buffer isn't a direct buffer or direct buffers aren't supported.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
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

        ///<summary>Returns the NativeArray backed by the same memory region as the given direct java.nio.Buffer.</summary>
        ///<param name="buffer">The direct java.nio.Buffer.</param>
        ///<returns>The NativeArray backed by the same memory region as the given buffer.</returns>
        ///<seealso cref="AndroidJNI.GetDirectBufferAddress" />
        public static NativeArray<byte> GetDirectByteBuffer(IntPtr buffer)
        {
            return GetDirectBuffer<byte>(buffer);
        }

        ///<summary>Returns the NativeArray backed by the same memory region as the given direct java.nio.Buffer.</summary>
        ///<param name="buffer">The direct java.nio.Buffer.</param>
        ///<returns>The NativeArray backed by the same memory region as the given buffer.</returns>
        ///<seealso cref="AndroidJNI.GetDirectBufferAddress" />
        public static NativeArray<sbyte> GetDirectSByteBuffer(IntPtr buffer)
        {
            return GetDirectBuffer<sbyte>(buffer);
        }


        // ------------------------------------------
        // Native method support
        ///<summary>Registers native methods with the class specified by the clazz argument.</summary>
        ///<remarks>See <see cref="JNINativeMethod.fnPtr" /> for details.</remarks>
        ///<param name="clazz">The Java class to register the native methods with.</param>
        ///<param name="methods">The native methods to register.</param>
        ///<returns>Zero on success, negative value on error. Falsely succeeds in Editor.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
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

        [NativeMethod(IsThreadSafe = true)]
        private static extern IntPtr RegisterNativesAllocate(int length);
        [NativeMethod(IsThreadSafe = true)]
        private static extern void RegisterNativesSet(IntPtr natives, int idx, string name, string signature, IntPtr fnPtr);
        [NativeMethod(IsThreadSafe = true)]
        private static extern int RegisterNativesAndFree(IntPtr clazz, IntPtr natives, int n);
        ///<summary>Unregisters native methods of a class. The class goes back to the state before it was linked or registered with its native method functions.</summary>
        ///<param name="clazz">The Java class to unregister native methods from.</param>
        ///<returns>Zero on success, negative value on error. Falsely succeeds in Editor.</returns>
        ///<seealso href="http://download.oracle.com/javase/1.5.0/docs/guide/jni/spec/jniTOC.html">Java Native Interface Specification (Oracle)</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern int UnregisterNatives(IntPtr clazz);

        ///<summary>Dumps JNI local/global reference tables to logcat.</summary>
        ///<remarks>
        ///  <para>Used to diagnose JNI reference leaks and other reference-table corruption.
        ///
        ///**Note:** This function attempts to invoke the undocumented <c>android.os.Debug.dumpReferenceTables()</c> method. If that method is unavailable, <see cref="AndroidJNI.DumpReferenceTables" /> has no effect.
        ///
        ///Each <see cref="AndroidJavaObject" /> instance creates a global JNI reference, which remains until the object is explicitly disposed or collected by the C# garbage collector. Failing to dispose an <see cref="AndroidJavaObject" /> can lead to <c>java.lang.OutOfMemoryError</c> if the process reaches the maximum number of allowed JNI references.
        ///
        ///Call <see cref="AndroidJNI.DumpReferenceTables" /> before and after the code which you want to test, and compare two outputs to determine if you're leaking.
        ///
        ///</para>
        ///  <para>The sample above will produce the following log:
        ///
        ///
        ///   --- reference table dump ---
        ///
        ///    local reference table dump:
        ///
        ///      Last 1 entries (of 1):
        ///
        ///            0: 0x242c298 java.lang.Class&lt;com.unity3d.player.ReflectionHelper&gt;
        ///
        ///      Summary:
        ///
        ///            1 of java.lang.Class
        ///
        ///    monitors reference table dump:
        ///
        ///      (empty)
        ///
        ///    global reference table dump:
        ///
        ///      Last 10 entries (of 744):
        ///
        ///          743: 0x6faef540 java.lang.Class&lt;com.android.internal.policy.PhoneWindow&gt;
        ///
        ///          742: 0x2348060 com.android.internal.policy.PhoneWindow
        ///
        ///          741: 0x6faef540 java.lang.Class&lt;com.android.internal.policy.PhoneWindow&gt;
        ///
        ///          740: 0x2348060 com.android.internal.policy.PhoneWindow
        ///
        ///          739: 0x6faef540 java.lang.Class&lt;com.android.internal.policy.PhoneWindow&gt;
        ///
        ///          738: 0x2348060 com.android.internal.policy.PhoneWindow
        ///
        ///          737: 0x6faef540 java.lang.Class&lt;com.android.internal.policy.PhoneWindow&gt;
        ///
        ///          736: 0x2348060 com.android.internal.policy.PhoneWindow
        ///
        ///          735: 0x6faef540 java.lang.Class&lt;com.android.internal.policy.PhoneWindow&gt;
        ///
        ///          734: 0x2348060 com.android.internal.policy.PhoneWindow.
        ///
        ///Notice **global reference table dump** contains a lot of <c>com.android.internal.policy.PhoneWindow</c> instances, to fix the leak, you need to properly dispose <c>window</c> object by calling <c>Dispose</c> method.</para>
        ///</remarks>
        ///<returns>Returns true if <c>android.os.Debug.dumpReferenceTables()</c> is accessible and was invoked successfully.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Android;
        ///
        ///public class Sample : MonoBehaviour
        ///{
        ///    void TestLeaks()
        ///    {
        ///        using var motionEventClass = new AndroidJavaClass("android.view.MotionEvent");
        ///        AndroidJNI.DumpReferenceTables();
        ///        for (int i = 0; i < 100; i++)
        ///        {
        ///            var window = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getWindow");
        ///            
        ///            // Do something with window
        ///
        ///            // If you'll fail to call Dispose(), jni reference will be kept alive until C# garbage collector collects 'window' object (which might not happen soon)
        ///            // There's a limited amount of max jni references you can have, it depends on the Android version, it's always better to Dispose or use 'using' keyword to release jni reference.
        ///            // window.Dispose();
        ///        }
        ///
        ///        AndroidJNI.DumpReferenceTables();
        ///    }
        ///
        ///    private void OnGUI()
        ///    {
        ///        GUILayout.Space(100);
        ///        if (GUILayout.Button("TestLeaks", GUILayout.Height(200), GUILayout.Width(Screen.width)))
        ///            TestLeaks();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="https://docs.oracle.com/en/java/javase/21/docs/specs/jni/design.html#:~:text=The%20JNI%20divides%20object%20references,until%20they%20are%20explicitly%20freed."> Local and global JNI references.</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool DumpReferenceTables();
    }
}
