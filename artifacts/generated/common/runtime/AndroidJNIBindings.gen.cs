// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
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


[UsedByNativeCode]
public sealed partial class AndroidJNIHelper
{
    private AndroidJNIHelper() {}
    
    
    public extern static bool debug
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [uei.ExcludeFromDocs]
public static IntPtr GetConstructorID (IntPtr javaClass) {
    string signature = "";
    return GetConstructorID ( javaClass, signature );
}

public static IntPtr GetConstructorID(IntPtr javaClass, [uei.DefaultValue("\"\"")]  string signature )
        {
            return _AndroidJNIHelper.GetConstructorID(javaClass, signature);
        }

    
    
    [uei.ExcludeFromDocs]
public static IntPtr GetMethodID (IntPtr javaClass, string methodName, string signature ) {
    bool isStatic = false;
    return GetMethodID ( javaClass, methodName, signature, isStatic );
}

[uei.ExcludeFromDocs]
public static IntPtr GetMethodID (IntPtr javaClass, string methodName) {
    bool isStatic = false;
    string signature = "";
    return GetMethodID ( javaClass, methodName, signature, isStatic );
}

public static IntPtr GetMethodID(IntPtr javaClass, string methodName, [uei.DefaultValue("\"\"")]  string signature , [uei.DefaultValue("false")]  bool isStatic )
        {
            return _AndroidJNIHelper.GetMethodID(javaClass, methodName, signature, isStatic);
        }

    
    
    [uei.ExcludeFromDocs]
public static IntPtr GetFieldID (IntPtr javaClass, string fieldName, string signature ) {
    bool isStatic = false;
    return GetFieldID ( javaClass, fieldName, signature, isStatic );
}

[uei.ExcludeFromDocs]
public static IntPtr GetFieldID (IntPtr javaClass, string fieldName) {
    bool isStatic = false;
    string signature = "";
    return GetFieldID ( javaClass, fieldName, signature, isStatic );
}

public static IntPtr GetFieldID(IntPtr javaClass, string fieldName, [uei.DefaultValue("\"\"")]  string signature , [uei.DefaultValue("false")]  bool isStatic )
        {
            return _AndroidJNIHelper.GetFieldID(javaClass, fieldName, signature, isStatic);
        }

    
    
    public static IntPtr CreateJavaRunnable(AndroidJavaRunnable jrunnable)
        {
            return _AndroidJNIHelper.CreateJavaRunnable(jrunnable);
        }
    
    
    [ThreadAndSerializationSafe ()]
    public static IntPtr CreateJavaProxy (AndroidJavaProxy proxy) {
        IntPtr result;
        INTERNAL_CALL_CreateJavaProxy ( proxy, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreateJavaProxy (AndroidJavaProxy proxy, out IntPtr value);
    public static IntPtr ConvertToJNIArray(System.Array array)
        {
            return _AndroidJNIHelper.ConvertToJNIArray(array);
        }
    
    
    public static jvalue[] CreateJNIArgArray(object[] args)
        {
            return _AndroidJNIHelper.CreateJNIArgArray(args);
        }
    
    
    public static void DeleteJNIArgArray(object[] args, jvalue[] jniArgs)
        {
            _AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
        }
    
    
    public static System.IntPtr GetConstructorID(System.IntPtr jclass, object[] args)
        {
            return _AndroidJNIHelper.GetConstructorID(jclass, args);
        }
    
    
    public static System.IntPtr GetMethodID(System.IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID(jclass, methodName, args, isStatic);
        }
    
    
    public static string GetSignature(object obj)
        {
            return _AndroidJNIHelper.GetSignature(obj);
        }
    
    
    public static string GetSignature(object[] args)
        {
            return _AndroidJNIHelper.GetSignature(args);
        }
    
    
    
    public static ArrayType ConvertFromJNIArray<ArrayType>(IntPtr array)
        {
            return _AndroidJNIHelper.ConvertFromJNIArray<ArrayType>(array);
        }
    
    
    public static System.IntPtr GetMethodID<ReturnType>(System.IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return _AndroidJNIHelper.GetMethodID<ReturnType>(jclass, methodName, args, isStatic);
        }
    
    
    public static System.IntPtr GetFieldID<FieldType>(System.IntPtr jclass, string fieldName, bool isStatic)
        {
            return _AndroidJNIHelper.GetFieldID<FieldType>(jclass, fieldName, isStatic);
        }
    
    
    public static string GetSignature<ReturnType>(object[] args)
        {
            return _AndroidJNIHelper.GetSignature<ReturnType>(args);
        }
    
    
}

public sealed partial class AndroidJNI
{
    private AndroidJNI() {}
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int AttachCurrentThread () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int DetachCurrentThread () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetVersion () ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr FindClass (string name) {
        IntPtr result;
        INTERNAL_CALL_FindClass ( name, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FindClass (string name, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr FromReflectedMethod (IntPtr refMethod) {
        IntPtr result;
        INTERNAL_CALL_FromReflectedMethod ( refMethod, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FromReflectedMethod (IntPtr refMethod, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr FromReflectedField (IntPtr refField) {
        IntPtr result;
        INTERNAL_CALL_FromReflectedField ( refField, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FromReflectedField (IntPtr refField, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToReflectedMethod (IntPtr clazz, IntPtr methodID, bool isStatic) {
        IntPtr result;
        INTERNAL_CALL_ToReflectedMethod ( clazz, methodID, isStatic, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToReflectedMethod (IntPtr clazz, IntPtr methodID, bool isStatic, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToReflectedField (IntPtr clazz, IntPtr fieldID, bool isStatic) {
        IntPtr result;
        INTERNAL_CALL_ToReflectedField ( clazz, fieldID, isStatic, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToReflectedField (IntPtr clazz, IntPtr fieldID, bool isStatic, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr GetSuperclass (IntPtr clazz) {
        IntPtr result;
        INTERNAL_CALL_GetSuperclass ( clazz, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSuperclass (IntPtr clazz, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsAssignableFrom (IntPtr clazz1, IntPtr clazz2) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int Throw (IntPtr obj) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int ThrowNew (IntPtr clazz, string message) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr ExceptionOccurred () {
        IntPtr result;
        INTERNAL_CALL_ExceptionOccurred ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ExceptionOccurred (out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExceptionDescribe () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExceptionClear () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void FatalError (string message) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int PushLocalFrame (int capacity) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr PopLocalFrame (IntPtr ptr) {
        IntPtr result;
        INTERNAL_CALL_PopLocalFrame ( ptr, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_PopLocalFrame (IntPtr ptr, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewGlobalRef (IntPtr obj) {
        IntPtr result;
        INTERNAL_CALL_NewGlobalRef ( obj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewGlobalRef (IntPtr obj, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DeleteGlobalRef (IntPtr obj) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr NewLocalRef (IntPtr obj) {
        IntPtr result;
        INTERNAL_CALL_NewLocalRef ( obj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewLocalRef (IntPtr obj, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DeleteLocalRef (IntPtr obj) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsSameObject (IntPtr obj1, IntPtr obj2) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int EnsureLocalCapacity (int capacity) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr AllocObject (IntPtr clazz) {
        IntPtr result;
        INTERNAL_CALL_AllocObject ( clazz, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AllocObject (IntPtr clazz, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewObject (IntPtr clazz, IntPtr methodID, jvalue[] args) {
        IntPtr result;
        INTERNAL_CALL_NewObject ( clazz, methodID, args, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewObject (IntPtr clazz, IntPtr methodID, jvalue[] args, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr GetObjectClass (IntPtr obj) {
        IntPtr result;
        INTERNAL_CALL_GetObjectClass ( obj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetObjectClass (IntPtr obj, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsInstanceOf (IntPtr obj, IntPtr clazz) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr GetMethodID (IntPtr clazz, string name, string sig) {
        IntPtr result;
        INTERNAL_CALL_GetMethodID ( clazz, name, sig, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetMethodID (IntPtr clazz, string name, string sig, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr GetFieldID (IntPtr clazz, string name, string sig) {
        IntPtr result;
        INTERNAL_CALL_GetFieldID ( clazz, name, sig, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetFieldID (IntPtr clazz, string name, string sig, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr GetStaticMethodID (IntPtr clazz, string name, string sig) {
        IntPtr result;
        INTERNAL_CALL_GetStaticMethodID ( clazz, name, sig, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetStaticMethodID (IntPtr clazz, string name, string sig, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr GetStaticFieldID (IntPtr clazz, string name, string sig) {
        IntPtr result;
        INTERNAL_CALL_GetStaticFieldID ( clazz, name, sig, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetStaticFieldID (IntPtr clazz, string name, string sig, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewStringUTF (string bytes) {
        IntPtr result;
        INTERNAL_CALL_NewStringUTF ( bytes, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewStringUTF (string bytes, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetStringUTFLength (IntPtr str) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetStringUTFChars (IntPtr str) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CallStringMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr CallObjectMethod (IntPtr obj, IntPtr methodID, jvalue[] args) {
        IntPtr result;
        INTERNAL_CALL_CallObjectMethod ( obj, methodID, args, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CallObjectMethod (IntPtr obj, IntPtr methodID, jvalue[] args, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32 CallIntMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CallBooleanMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16 CallShortMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte CallByteMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char CallCharMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float CallFloatMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double CallDoubleMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64 CallLongMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CallVoidMethod (IntPtr obj, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetStringField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr GetObjectField (IntPtr obj, IntPtr fieldID) {
        IntPtr result;
        INTERNAL_CALL_GetObjectField ( obj, fieldID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetObjectField (IntPtr obj, IntPtr fieldID, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetBooleanField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte GetByteField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char GetCharField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16 GetShortField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32 GetIntField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64 GetLongField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetFloatField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double GetDoubleField (IntPtr obj, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStringField (IntPtr obj, IntPtr fieldID, string val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetObjectField (IntPtr obj, IntPtr fieldID, IntPtr val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetBooleanField (IntPtr obj, IntPtr fieldID, bool val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetByteField (IntPtr obj, IntPtr fieldID, Byte val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetCharField (IntPtr obj, IntPtr fieldID, Char val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetShortField (IntPtr obj, IntPtr fieldID, Int16 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetIntField (IntPtr obj, IntPtr fieldID, Int32 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLongField (IntPtr obj, IntPtr fieldID, Int64 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetFloatField (IntPtr obj, IntPtr fieldID, float val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetDoubleField (IntPtr obj, IntPtr fieldID, double val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CallStaticStringMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr CallStaticObjectMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) {
        IntPtr result;
        INTERNAL_CALL_CallStaticObjectMethod ( clazz, methodID, args, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CallStaticObjectMethod (IntPtr clazz, IntPtr methodID, jvalue[] args, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32 CallStaticIntMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CallStaticBooleanMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16 CallStaticShortMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte CallStaticByteMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char CallStaticCharMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float CallStaticFloatMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double CallStaticDoubleMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64 CallStaticLongMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CallStaticVoidMethod (IntPtr clazz, IntPtr methodID, jvalue[] args) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetStaticStringField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr GetStaticObjectField (IntPtr clazz, IntPtr fieldID) {
        IntPtr result;
        INTERNAL_CALL_GetStaticObjectField ( clazz, fieldID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetStaticObjectField (IntPtr clazz, IntPtr fieldID, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetStaticBooleanField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte GetStaticByteField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char GetStaticCharField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16 GetStaticShortField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32 GetStaticIntField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64 GetStaticLongField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetStaticFloatField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double GetStaticDoubleField (IntPtr clazz, IntPtr fieldID) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticStringField (IntPtr clazz, IntPtr fieldID, string val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticObjectField (IntPtr clazz, IntPtr fieldID, IntPtr val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticBooleanField (IntPtr clazz, IntPtr fieldID, bool val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticByteField (IntPtr clazz, IntPtr fieldID, Byte val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticCharField (IntPtr clazz, IntPtr fieldID, Char val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticShortField (IntPtr clazz, IntPtr fieldID, Int16 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticIntField (IntPtr clazz, IntPtr fieldID, Int32 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticLongField (IntPtr clazz, IntPtr fieldID, Int64 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticFloatField (IntPtr clazz, IntPtr fieldID, float val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStaticDoubleField (IntPtr clazz, IntPtr fieldID, double val) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr ToBooleanArray (Boolean[] array) {
        IntPtr result;
        INTERNAL_CALL_ToBooleanArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToBooleanArray (Boolean[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToByteArray (Byte[] array) {
        IntPtr result;
        INTERNAL_CALL_ToByteArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToByteArray (Byte[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToCharArray (Char[] array) {
        IntPtr result;
        INTERNAL_CALL_ToCharArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToCharArray (Char[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToShortArray (Int16[] array) {
        IntPtr result;
        INTERNAL_CALL_ToShortArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToShortArray (Int16[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToIntArray (Int32[] array) {
        IntPtr result;
        INTERNAL_CALL_ToIntArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToIntArray (Int32[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToLongArray (Int64[] array) {
        IntPtr result;
        INTERNAL_CALL_ToLongArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToLongArray (Int64[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToFloatArray (float[] array) {
        IntPtr result;
        INTERNAL_CALL_ToFloatArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToFloatArray (float[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToDoubleArray (double[] array) {
        IntPtr result;
        INTERNAL_CALL_ToDoubleArray ( array, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToDoubleArray (double[] array, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr ToObjectArray (IntPtr[] array, IntPtr arrayClass) {
        IntPtr result;
        INTERNAL_CALL_ToObjectArray ( array, arrayClass, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ToObjectArray (IntPtr[] array, IntPtr arrayClass, out IntPtr value);
    public static IntPtr ToObjectArray(IntPtr[] array)
        {
            return ToObjectArray(array, IntPtr.Zero);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Boolean[] FromBooleanArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte[] FromByteArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char[] FromCharArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16[] FromShortArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32[] FromIntArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64[] FromLongArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float[] FromFloatArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double[] FromDoubleArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  IntPtr[] FromObjectArray (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetArrayLength (IntPtr array) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr NewBooleanArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewBooleanArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewBooleanArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewByteArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewByteArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewByteArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewCharArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewCharArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewCharArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewShortArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewShortArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewShortArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewIntArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewIntArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewIntArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewLongArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewLongArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewLongArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewFloatArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewFloatArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewFloatArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewDoubleArray (int size) {
        IntPtr result;
        INTERNAL_CALL_NewDoubleArray ( size, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewDoubleArray (int size, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    public static IntPtr NewObjectArray (int size, IntPtr clazz, IntPtr obj) {
        IntPtr result;
        INTERNAL_CALL_NewObjectArray ( size, clazz, obj, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_NewObjectArray (int size, IntPtr clazz, IntPtr obj, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetBooleanArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Byte GetByteArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Char GetCharArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int16 GetShortArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int32 GetIntArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Int64 GetLongArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetFloatArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  double GetDoubleArrayElement (IntPtr array, int index) ;

    [ThreadAndSerializationSafe ()]
    public static IntPtr GetObjectArrayElement (IntPtr array, int index) {
        IntPtr result;
        INTERNAL_CALL_GetObjectArrayElement ( array, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetObjectArrayElement (IntPtr array, int index, out IntPtr value);
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetBooleanArrayElement (IntPtr array, int index, byte val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetByteArrayElement (IntPtr array, int index, sbyte val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetCharArrayElement (IntPtr array, int index, Char val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetShortArrayElement (IntPtr array, int index, Int16 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetIntArrayElement (IntPtr array, int index, Int32 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLongArrayElement (IntPtr array, int index, Int64 val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetFloatArrayElement (IntPtr array, int index, float val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetDoubleArrayElement (IntPtr array, int index, double val) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetObjectArrayElement (IntPtr array, int index, IntPtr obj) ;

}


}
