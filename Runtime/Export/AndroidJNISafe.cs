// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    internal class AndroidJNISafe
    {
        public static void CheckException()
        {
            IntPtr jthrowable = AndroidJNI.ExceptionOccurred();
            if (jthrowable != IntPtr.Zero)
            {
                AndroidJNI.ExceptionClear();
                IntPtr jthrowableClass = AndroidJNI.FindClass("java/lang/Throwable");
                IntPtr androidUtilLogClass = AndroidJNI.FindClass("android/util/Log");
                try
                {
                    IntPtr toStringMethodId = AndroidJNI.GetMethodID(jthrowableClass, "toString", "()Ljava/lang/String;");
                    IntPtr getStackTraceStringMethodId = AndroidJNI.GetStaticMethodID(androidUtilLogClass, "getStackTraceString", "(Ljava/lang/Throwable;)Ljava/lang/String;");
                    string exceptionMessage = AndroidJNI.CallStringMethod(jthrowable, toStringMethodId, new jvalue[] {});
                    // Pass jthrowable as a param to getStackTraceString()
                    jvalue[] jniArgs = new jvalue[1];
                    jniArgs[0].l = jthrowable;
                    string exceptionCallStack = AndroidJNI.CallStaticStringMethod(androidUtilLogClass, getStackTraceStringMethodId, jniArgs);
                    throw new AndroidJavaException(exceptionMessage, exceptionCallStack);
                }
                finally
                {
                    AndroidJNISafe.DeleteLocalRef(jthrowable);
                    AndroidJNISafe.DeleteLocalRef(jthrowableClass);
                    AndroidJNISafe.DeleteLocalRef(androidUtilLogClass);
                }
            }
        }

        public static void DeleteGlobalRef(IntPtr globalref)
        {
            if (globalref != IntPtr.Zero) AndroidJNI.DeleteGlobalRef(globalref);
        }

        public static void DeleteLocalRef(IntPtr localref)
        {
            if (localref != IntPtr.Zero) AndroidJNI.DeleteLocalRef(localref);
        }

        public static IntPtr NewStringUTF(string bytes)
        {
            try { return AndroidJNI.NewStringUTF(bytes); } finally { CheckException(); }
        }

        public static string GetStringUTFChars(IntPtr str)
        {
            try { return AndroidJNI.GetStringUTFChars(str); } finally { CheckException(); }
        }

        public static IntPtr GetObjectClass(IntPtr ptr)
        {
            try { return AndroidJNI.GetObjectClass(ptr); } finally { CheckException(); }
        }

        public static IntPtr GetStaticMethodID(IntPtr clazz, string name, string sig)
        {
            try { return AndroidJNI.GetStaticMethodID(clazz, name, sig); } finally { CheckException(); }
        }

        public static IntPtr GetMethodID(IntPtr obj, string name, string sig)
        {
            try { return AndroidJNI.GetMethodID(obj, name, sig); } finally { CheckException(); }
        }

        public static IntPtr GetFieldID(IntPtr clazz, string name, string sig)
        {
            try { return AndroidJNI.GetFieldID(clazz, name, sig); } finally { CheckException(); }
        }

        public static IntPtr GetStaticFieldID(IntPtr clazz, string name, string sig)
        {
            try { return AndroidJNI.GetStaticFieldID(clazz, name, sig); } finally { CheckException(); }
        }

        public static IntPtr FromReflectedMethod(IntPtr refMethod)
        {
            try { return AndroidJNI.FromReflectedMethod(refMethod); } finally { CheckException(); }
        }

        public static IntPtr FromReflectedField(IntPtr refField)
        {
            try { return AndroidJNI.FromReflectedField(refField); } finally { CheckException(); }
        }

        public static IntPtr FindClass(string name)
        {
            try { return AndroidJNI.FindClass(name); } finally { CheckException(); }
        }

        public static IntPtr NewObject(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.NewObject(clazz, methodID, args); } finally { CheckException(); }
        }

        public static void SetStaticObjectField(IntPtr clazz, IntPtr fieldID, IntPtr val)
        {
            try { AndroidJNI.SetStaticObjectField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticStringField(IntPtr clazz, IntPtr fieldID, string val)
        {
            try { AndroidJNI.SetStaticStringField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticCharField(IntPtr clazz, IntPtr fieldID, Char val)
        {
            try { AndroidJNI.SetStaticCharField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticDoubleField(IntPtr clazz, IntPtr fieldID, double val)
        {
            try { AndroidJNI.SetStaticDoubleField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticFloatField(IntPtr clazz, IntPtr fieldID, float val)
        {
            try { AndroidJNI.SetStaticFloatField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticLongField(IntPtr clazz, IntPtr fieldID, Int64 val)
        {
            try { AndroidJNI.SetStaticLongField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticShortField(IntPtr clazz, IntPtr fieldID, Int16 val)
        {
            try { AndroidJNI.SetStaticShortField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticByteField(IntPtr clazz, IntPtr fieldID, Byte val)
        {
            try { AndroidJNI.SetStaticByteField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticBooleanField(IntPtr clazz, IntPtr fieldID, bool val)
        {
            try { AndroidJNI.SetStaticBooleanField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStaticIntField(IntPtr clazz, IntPtr fieldID, Int32 val)
        {
            try { AndroidJNI.SetStaticIntField(clazz, fieldID, val); } finally { CheckException(); }
        }

        public static IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticObjectField(clazz, fieldID); } finally { CheckException(); }
        }

        public static string GetStaticStringField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticStringField(clazz, fieldID); } finally { CheckException(); }
        }

        public static Char GetStaticCharField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticCharField(clazz, fieldID); } finally { CheckException(); }
        }

        public static double GetStaticDoubleField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticDoubleField(clazz, fieldID); } finally { CheckException(); }
        }

        public static float GetStaticFloatField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticFloatField(clazz, fieldID); } finally { CheckException(); }
        }

        public static Int64 GetStaticLongField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticLongField(clazz, fieldID); } finally { CheckException(); }
        }

        public static Int16 GetStaticShortField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticShortField(clazz, fieldID); } finally { CheckException(); }
        }

        public static Byte GetStaticByteField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticByteField(clazz, fieldID); } finally { CheckException(); }
        }

        public static bool GetStaticBooleanField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticBooleanField(clazz, fieldID); } finally { CheckException(); }
        }

        public static Int32 GetStaticIntField(IntPtr clazz, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStaticIntField(clazz, fieldID); } finally { CheckException(); }
        }

        public static void CallStaticVoidMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { AndroidJNI.CallStaticVoidMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticObjectMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static string CallStaticStringMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticStringMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static Char CallStaticCharMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticCharMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static double CallStaticDoubleMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticDoubleMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static float CallStaticFloatMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticFloatMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static Int64 CallStaticLongMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticLongMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static Int16 CallStaticShortMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticShortMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static Byte CallStaticByteMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticByteMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static bool CallStaticBooleanMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticBooleanMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static Int32 CallStaticIntMethod(IntPtr clazz, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStaticIntMethod(clazz, methodID, args); } finally { CheckException(); }
        }

        public static void SetObjectField(IntPtr obj, IntPtr fieldID, IntPtr val)
        {
            try { AndroidJNI.SetObjectField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetStringField(IntPtr obj, IntPtr fieldID, string val)
        {
            try { AndroidJNI.SetStringField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetCharField(IntPtr obj, IntPtr fieldID, Char val)
        {
            try { AndroidJNI.SetCharField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetDoubleField(IntPtr obj, IntPtr fieldID, double val)
        {
            try { AndroidJNI.SetDoubleField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetFloatField(IntPtr obj, IntPtr fieldID, float val)
        {
            try { AndroidJNI.SetFloatField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetLongField(IntPtr obj, IntPtr fieldID, Int64 val)
        {
            try { AndroidJNI.SetLongField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetShortField(IntPtr obj, IntPtr fieldID, Int16 val)
        {
            try { AndroidJNI.SetShortField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetByteField(IntPtr obj, IntPtr fieldID, Byte val)
        {
            try { AndroidJNI.SetByteField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetBooleanField(IntPtr obj, IntPtr fieldID, bool val)
        {
            try { AndroidJNI.SetBooleanField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static void SetIntField(IntPtr obj, IntPtr fieldID, Int32 val)
        {
            try { AndroidJNI.SetIntField(obj, fieldID, val); } finally { CheckException(); }
        }

        public static IntPtr GetObjectField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetObjectField(obj, fieldID); } finally { CheckException(); }
        }

        public static string GetStringField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetStringField(obj, fieldID); } finally { CheckException(); }
        }

        public static Char GetCharField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetCharField(obj, fieldID); } finally { CheckException(); }
        }

        public static double GetDoubleField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetDoubleField(obj, fieldID); } finally { CheckException(); }
        }

        public static float GetFloatField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetFloatField(obj, fieldID); } finally { CheckException(); }
        }

        public static Int64 GetLongField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetLongField(obj, fieldID); } finally { CheckException(); }
        }

        public static Int16 GetShortField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetShortField(obj, fieldID); } finally { CheckException(); }
        }

        public static Byte GetByteField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetByteField(obj, fieldID); } finally { CheckException(); }
        }

        public static bool GetBooleanField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetBooleanField(obj, fieldID); } finally { CheckException(); }
        }

        public static Int32 GetIntField(IntPtr obj, IntPtr fieldID)
        {
            try { return AndroidJNI.GetIntField(obj, fieldID); } finally { CheckException(); }
        }

        public static void CallVoidMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { AndroidJNI.CallVoidMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallObjectMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static string CallStringMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallStringMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static Char CallCharMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallCharMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static double CallDoubleMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallDoubleMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static float CallFloatMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallFloatMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static Int64 CallLongMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallLongMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static Int16 CallShortMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallShortMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static Byte CallByteMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallByteMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static bool CallBooleanMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallBooleanMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static Int32 CallIntMethod(IntPtr obj, IntPtr methodID, jvalue[] args)
        {
            try { return AndroidJNI.CallIntMethod(obj, methodID, args); } finally { CheckException(); }
        }

        public static IntPtr[] FromObjectArray(IntPtr array)
        {
            try { return AndroidJNI.FromObjectArray(array); } finally { CheckException(); }
        }

        public static Char[] FromCharArray(IntPtr array)
        {
            try { return AndroidJNI.FromCharArray(array); } finally { CheckException(); }
        }

        public static double[] FromDoubleArray(IntPtr array)
        {
            try { return AndroidJNI.FromDoubleArray(array); } finally { CheckException(); }
        }

        public static float[] FromFloatArray(IntPtr array)
        {
            try { return AndroidJNI.FromFloatArray(array); } finally { CheckException(); }
        }

        public static Int64[] FromLongArray(IntPtr array)
        {
            try { return AndroidJNI.FromLongArray(array); } finally { CheckException(); }
        }

        public static Int16[] FromShortArray(IntPtr array)
        {
            try { return AndroidJNI.FromShortArray(array); } finally { CheckException(); }
        }

        public static Byte[] FromByteArray(IntPtr array)
        {
            try { return AndroidJNI.FromByteArray(array); } finally { CheckException(); }
        }

        public static bool[] FromBooleanArray(IntPtr array)
        {
            try { return AndroidJNI.FromBooleanArray(array); } finally { CheckException(); }
        }

        public static Int32[] FromIntArray(IntPtr array)
        {
            try { return AndroidJNI.FromIntArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToObjectArray(IntPtr[] array)
        {
            try { return AndroidJNI.ToObjectArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToObjectArray(IntPtr[] array, IntPtr type)
        {
            try { return AndroidJNI.ToObjectArray(array, type); } finally { CheckException(); }
        }

        public static IntPtr ToCharArray(Char[] array)
        {
            try { return AndroidJNI.ToCharArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToDoubleArray(double[] array)
        {
            try { return AndroidJNI.ToDoubleArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToFloatArray(float[] array)
        {
            try { return AndroidJNI.ToFloatArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToLongArray(Int64[] array)
        {
            try { return AndroidJNI.ToLongArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToShortArray(Int16[] array)
        {
            try { return AndroidJNI.ToShortArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToByteArray(Byte[] array)
        {
            try { return AndroidJNI.ToByteArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToBooleanArray(bool[] array)
        {
            try { return AndroidJNI.ToBooleanArray(array); } finally { CheckException(); }
        }

        public static IntPtr ToIntArray(Int32[] array)
        {
            try { return AndroidJNI.ToIntArray(array); } finally { CheckException(); }
        }

        public static IntPtr GetObjectArrayElement(IntPtr array, int index)
        {
            try { return AndroidJNI.GetObjectArrayElement(array, index); } finally { CheckException(); }
        }

        public static int GetArrayLength(IntPtr array)
        {
            try { return AndroidJNI.GetArrayLength(array); } finally { CheckException(); }
        }
    }
}
