// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine
{
    public delegate void AndroidJavaRunnable();

    public sealed class AndroidJavaException : Exception
    {
        private string mJavaStackTrace;
        internal AndroidJavaException(string message, string javaStackTrace) : base(message)
        {
            mJavaStackTrace = javaStackTrace;
        }

        public override string StackTrace
        {
            get
            {
                return mJavaStackTrace + base.StackTrace;
            }
        }
    }

    internal class GlobalJavaObjectRef
    {
        public GlobalJavaObjectRef(IntPtr jobject)
        {
            m_jobject = (jobject == IntPtr.Zero) ? IntPtr.Zero : AndroidJNI.NewGlobalRef(jobject);
        }

        ~GlobalJavaObjectRef()
        {
            Dispose();
        }

        public static implicit operator IntPtr(GlobalJavaObjectRef obj)
        {
            return obj.m_jobject;
        }

        private bool m_disposed = false;
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            if (m_jobject != IntPtr.Zero)
            {
                AndroidJNISafe.QueueDeleteGlobalRef(m_jobject);
            }
        }

        protected IntPtr m_jobject;
    }

    internal class AndroidJavaRunnableProxy : AndroidJavaProxy
    {
        private AndroidJavaRunnable mRunnable;
        public AndroidJavaRunnableProxy(AndroidJavaRunnable runnable) : base("java/lang/Runnable") { mRunnable = runnable; }
        public void run() { mRunnable(); }

        public override IntPtr Invoke(string methodName, IntPtr javaArgs)
        {
            int arrayLen = 0;
            if (javaArgs != IntPtr.Zero)
                arrayLen = AndroidJNISafe.GetArrayLength(javaArgs);
            if (arrayLen == 0 && methodName == "run")
            {
                run();
                return IntPtr.Zero;
            }

            return base.Invoke(methodName, javaArgs);
        }
    }

    public class AndroidJavaProxy
    {
        public readonly AndroidJavaClass javaInterface;
        public AndroidJavaProxy(string javaInterface) : this(new AndroidJavaClass(javaInterface)) {}
        public AndroidJavaProxy(AndroidJavaClass javaInterface)
        {
            this.javaInterface = javaInterface;
        }

        ~AndroidJavaProxy()
        {
            AndroidJNISafe.DeleteWeakGlobalRef(proxyObject);
        }

        public virtual AndroidJavaObject Invoke(string methodName, object[] args)
        {
            Exception error = null;
            BindingFlags binderFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            int NullArgs = 0;
            Type[] argTypes = new Type[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == null)
                {
                    argTypes[i] = null;
                    NullArgs ++;
                }
                else
                {
                    argTypes[i] = args[i].GetType();
                }
            }

            try
            {
                MethodInfo methodResult = null;
                if (NullArgs > 0)
                {
                    MethodInfo[] methods = this.GetType().GetMethods(binderFlags);

                    var matches = 0;

                    foreach (var method in methods)
                    {
                        //exit loop if dont have same name
                        if (methodName != method.Name)
                            continue;

                        ParameterInfo[] methodParameters = method.GetParameters();

                        //exit loop of dont have same number of paramters
                        if (methodParameters.Length != args.Length)
                            continue;

                        var isOk = true;

                        for (int j = 0; j < methodParameters.Length; j++)
                        {
                            if (argTypes[j] == null)
                            {
                                if (methodParameters[j].ParameterType.IsValueType)
                                {
                                    isOk = false;
                                    break;
                                }
                            }
                            else if (!methodParameters[j].ParameterType.IsAssignableFrom(argTypes[j]))
                            {
                                isOk = false;
                                break;
                            }
                        }

                        if (!isOk)
                            continue;
                        else
                            matches++;

                        methodResult = method;
                    }

                    if (matches > 1)
                        throw new Exception($"Ambiguous overloads found for {methodName} with given parameters");

                }
                else
                    methodResult = GetType().GetMethod(methodName, binderFlags, null, argTypes, null);

                if (methodResult != null)
                    return _AndroidJNIHelper.Box(methodResult.Invoke(this, args));

            }
            catch (TargetInvocationException invocationError)
            {
                error = invocationError.InnerException;
            }
            catch (Exception invocationError)
            {
                error = invocationError;
            }

            //Log Error
            string[] argTypeNames = new string[args.Length];
            for (int i = 0; i < argTypeNames.Length; ++i)
            {
                if (argTypes[i] == null)
                    argTypeNames[i] = "null";
                else
                    argTypeNames[i] = argTypes[i].ToString();
            }

            if (error != null)
                throw new TargetInvocationException(GetType() + "." + methodName + "(" + string.Join(",", argTypeNames) + ")", error);

            var ex = new Exception("No such proxy method: " + GetType() + "." + methodName + "(" + string.Join(",", argTypeNames) + ")");

            var nativeError = AndroidReflection.CreateInvocationError(ex, methodNotFound: true);
            return nativeError == IntPtr.Zero ? null : new AndroidJavaObject(nativeError);
        }

        public virtual AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
        {
            object[] args =  new object[javaArgs.Length];
            for (int i = 0; i < javaArgs.Length; ++i)
            {
                args[i] = _AndroidJNIHelper.Unbox(javaArgs[i]);
                if (!(args[i] is AndroidJavaObject))
                {
                    // If we're not passing a AndroidJavaObject/Class to the proxy, we can safely dispose
                    // Otherwise the GC would do it eventally, but it might be too slow and hit the global ref limit
                    if (javaArgs[i] != null)
                        javaArgs[i].Dispose();
                }
            }
            return Invoke(methodName, args);
        }

        public virtual IntPtr Invoke(string methodName, IntPtr javaArgs)
        {
            int arrayLen = 0;
            if (javaArgs != IntPtr.Zero)
                arrayLen = AndroidJNISafe.GetArrayLength(javaArgs);

            if (arrayLen == 1 && methodName == "equals")
            {
                IntPtr o = AndroidJNISafe.GetObjectArrayElement(javaArgs, 0);
                AndroidJavaObject obj = o == IntPtr.Zero ? null : new AndroidJavaObject(o);
                return AndroidJNIHelper.Box(equals(obj));
            }
            else if (arrayLen == 0 && methodName == "hashCode")
            {
                return AndroidJNIHelper.Box(hashCode());
            }

            AndroidJavaObject[] args = new AndroidJavaObject[arrayLen];
            for (int i = 0; i < arrayLen; ++i)
            {
                IntPtr objectRef = AndroidJNISafe.GetObjectArrayElement(javaArgs, i);
                args[i] = objectRef != IntPtr.Zero ? AndroidJavaObject.AndroidJavaObjectDeleteLocalRef(objectRef) : null;
            }
            using (AndroidJavaObject result = Invoke(methodName, args))
            {
                if (result == null)
                    return IntPtr.Zero;

                return AndroidJNI.NewLocalRef(result.GetRawObject());
            }
        }

        // implementing equals, hashCode and toString which should be implemented by all java objects
        // these methods must be in camel case, because that's how they are defined in java.
        public virtual bool equals(AndroidJavaObject obj)
        {
            IntPtr anotherObject = (obj == null) ? System.IntPtr.Zero : obj.GetRawObject();
            return AndroidJNI.IsSameObject(proxyObject, anotherObject);
        }

        public virtual int hashCode()
        {
            Span<jvalue> jniArgs = stackalloc jvalue[1];
            jniArgs[0].l = GetRawProxy();
            return AndroidJNISafe.CallStaticIntMethod(s_JavaLangSystemClass, s_HashCodeMethodID, jniArgs);
        }

        public virtual string toString()
        {
            return this + " <c# proxy java object>";
        }

        internal IntPtr proxyObject = IntPtr.Zero;
        internal AndroidJavaObject GetProxyObject()
        {
            return AndroidJavaObject.AndroidJavaObjectDeleteLocalRef(GetRawProxy());
        }

        internal IntPtr GetRawProxy()
        {
            IntPtr ret = IntPtr.Zero;
            if (proxyObject != IntPtr.Zero)
            {
                ret = AndroidJNI.NewLocalRef(proxyObject);
                if (ret == IntPtr.Zero)
                {
                    AndroidJNI.DeleteWeakGlobalRef(proxyObject);
                    proxyObject = IntPtr.Zero;
                }
            }
            if (ret == IntPtr.Zero)
            {
                ret = AndroidJNIHelper.CreateJavaProxy(this);
                proxyObject = AndroidJNI.NewWeakGlobalRef(ret);
            }
            return ret;
        }

        private static readonly GlobalJavaObjectRef s_JavaLangSystemClass = new GlobalJavaObjectRef(AndroidJNISafe.FindClass("java/lang/System"));
        private static readonly IntPtr s_HashCodeMethodID = AndroidJNIHelper.GetMethodID(s_JavaLangSystemClass, "identityHashCode", "(Ljava/lang/Object;)I", true);
    }

    public class AndroidJavaObject : IDisposable
    {
        // Construct an AndroidJavaObject based on the name of the class.
        public AndroidJavaObject(string className, string[] args) : this()
        {
            _AndroidJavaObject(className, (object)args);
        }

        public AndroidJavaObject(string className, AndroidJavaObject[] args) : this()
        {
            _AndroidJavaObject(className, (object)args);
        }

        public AndroidJavaObject(string className, AndroidJavaClass[] args) : this()
        {
            _AndroidJavaObject(className, (object)args);
        }

        public AndroidJavaObject(string className, AndroidJavaProxy[] args) : this()
        {
            _AndroidJavaObject(className, (object)args);
        }

        public AndroidJavaObject(string className, AndroidJavaRunnable[] args) : this()
        {
            _AndroidJavaObject(className, (object)args);
        }

        public AndroidJavaObject(string className, params object[] args) : this()
        {
            _AndroidJavaObject(className, args);
        }

        public AndroidJavaObject(IntPtr jobject) : this()
        {
            if (jobject == IntPtr.Zero)
            {
                throw new Exception("JNI: Init'd AndroidJavaObject with null ptr!");
            }

            IntPtr jclass = AndroidJNISafe.GetObjectClass(jobject);
            m_jobject = new GlobalJavaObjectRef(jobject);
            m_jclass = new GlobalJavaObjectRef(jclass);
            AndroidJNISafe.DeleteLocalRef(jclass);
        }

        public AndroidJavaObject(IntPtr clazz, IntPtr constructorID, params object[] args)
        {
            m_jclass = new GlobalJavaObjectRef(clazz);
            _AndroidJavaObject(constructorID, args);
        }

        //===================================================================

        // IDisposable callback
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //===================================================================

        // Calls a Java method on an object (non-static).
        public void Call<T>(string methodName, T[] args)
        {
            _Call(methodName, (object)args);
        }

        public void Call<T>(IntPtr methodID, T[] args)
        {
            _Call(methodID, (object)args);
        }

        public void Call(string methodName, params object[] args)
        {
            _Call(methodName, args);
        }

        public void Call(IntPtr methodID, params object[] args)
        {
            _Call(methodID, args);
        }

        //===================================================================

        // Call a static Java method on a class.
        public void CallStatic<T>(string methodName, T[] args)
        {
            _CallStatic(methodName, (object)args);
        }

        public void CallStatic<T>(IntPtr methodID, T[] args)
        {
            _CallStatic(methodID, (object)args);
        }

        public void CallStatic(string methodName, params object[] args)
        {
            _CallStatic(methodName, args);
        }

        public void CallStatic(IntPtr methodID, params object[] args)
        {
            _CallStatic(methodID, args);
        }

        //===================================================================

        // Get the value of a field in an object (non-static).
        public FieldType Get<FieldType>(string fieldName)
        {
            return _Get<FieldType>(fieldName);
        }

        public FieldType Get<FieldType>(IntPtr fieldID)
        {
            return _Get<FieldType>(fieldID);
        }

        // Set the value of a field in an object (non-static).
        public void Set<FieldType>(string fieldName, FieldType val)
        {
            _Set<FieldType>(fieldName, val);
        }

        public void Set<FieldType>(IntPtr fieldID, FieldType val)
        {
            _Set<FieldType>(fieldID, val);
        }

        //===================================================================

        // Get the value of a static field in an object type.
        public FieldType GetStatic<FieldType>(string fieldName)
        {
            return _GetStatic<FieldType>(fieldName);
        }

        public FieldType GetStatic<FieldType>(IntPtr fieldID)
        {
            return _GetStatic<FieldType>(fieldID);
        }

        // Set the value of a static field in an object type.
        public void SetStatic<FieldType>(string fieldName, FieldType val)
        {
            _SetStatic<FieldType>(fieldName, val);
        }

        public void SetStatic<FieldType>(IntPtr fieldID, FieldType val)
        {
            _SetStatic<FieldType>(fieldID, val);
        }

        //===================================================================

        // Retrieve the <i>raw</i> jobject pointer to the Java object.
        public IntPtr GetRawObject()
        {
            return _GetRawObject();
        }

        // Retrieve the <i>raw</i> jclass pointer to the Java class;
        public IntPtr GetRawClass()
        {
            return _GetRawClass();
        }

        //===================================================================

        public AndroidJavaObject CloneReference()
        {
            if (m_jclass == null)
                throw new Exception("Cannot clone a disposed reference");
            // if no object, assume this is AndroidJavaClass
            if (m_jobject != null)
            {
                AndroidJavaObject clone = new AndroidJavaObject();
                clone.m_jobject = new GlobalJavaObjectRef(m_jobject);
                clone.m_jclass = new GlobalJavaObjectRef(m_jclass);
                return clone;
            }
            else
                return new AndroidJavaClass(m_jclass);
        }

        //===================================================================

        // Call a Java method on an object.
        public ReturnType Call<ReturnType, T>(string methodName, T[] args)
        {
            return _Call<ReturnType>(methodName, (object)args);
        }

        public ReturnType Call<ReturnType, T>(IntPtr methodID, T[] args)
        {
            return _Call<ReturnType>(methodID, (object)args);
        }

        public ReturnType Call<ReturnType>(string methodName, params object[] args)
        {
            return _Call<ReturnType>(methodName, args);
        }

        public ReturnType Call<ReturnType>(IntPtr methodID, params object[] args)
        {
            return _Call<ReturnType>(methodID, args);
        }

        // Call a static Java method on a class.
        public ReturnType CallStatic<ReturnType, T>(string methodName, T[] args)
        {
            return _CallStatic<ReturnType>(methodName, (object)args);
        }

        public ReturnType CallStatic<ReturnType, T>(IntPtr methodID, T[] args)
        {
            return _CallStatic<ReturnType>(methodID, (object)args);
        }

        public ReturnType CallStatic<ReturnType>(string methodName, params object[] args)
        {
            return _CallStatic<ReturnType>(methodName, args);
        }

        public ReturnType CallStatic<ReturnType>(IntPtr methodID, params object[] args)
        {
            return _CallStatic<ReturnType>(methodID, args);
        }

        //===================================================================

        private static bool enableDebugPrints = false;

        protected void DebugPrint(string msg)
        {
            if (!enableDebugPrints)
                return;
            Debug.Log(msg);
        }

        protected void DebugPrint(string call, string methodName, string signature, object[] args)
        {
            if (!enableDebugPrints)
                return;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (object obj in args)
            {
                sb.Append(", ");
                sb.Append(obj == null ? "<null>" : obj.GetType().ToString());
            }
            Debug.Log(call + "(\"" + methodName + "\"" + sb + ") = " + signature);
        }

        //===================================================================

        private void _AndroidJavaObject(string className, params object[] args)
        {
            DebugPrint("Creating AndroidJavaObject from " + className);
            var clazz = AndroidJNISafe.FindClass(className.Replace('.', '/'));
            m_jclass = new GlobalJavaObjectRef(clazz);
            AndroidJNISafe.DeleteLocalRef(clazz);
            IntPtr constructorID = AndroidJNIHelper.GetConstructorID(m_jclass, args);
            _AndroidJavaObject(constructorID, args);
        }

        private void _AndroidJavaObject(IntPtr constructorID, params object[] args)
        {
            Span<jvalue> jniArgs = (args == null || args.Length == 0) ? new Span<jvalue>() : stackalloc jvalue[args.Length];
            AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
            try
            {
                IntPtr jobject = AndroidJNISafe.NewObject(m_jclass, constructorID, jniArgs);
                m_jobject = new GlobalJavaObjectRef(jobject);
                AndroidJNISafe.DeleteLocalRef(jobject);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
            }
        }

        internal AndroidJavaObject()
        {
        }

        ~AndroidJavaObject()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_jobject != null)
            {
                m_jobject.Dispose();
                m_jobject = null;
            }
            if (m_jclass != null)
            {
                m_jclass.Dispose();
                m_jclass = null;
            }
        }

        //===================================================================

        protected void _Call(string methodName, params object[] args)
        {
            IntPtr methodID = AndroidJNIHelper.GetMethodID(m_jclass, methodName, args, false);
            _Call(methodID, args);
        }

        protected void _Call(IntPtr methodID, params object[] args)
        {
            Span<jvalue> jniArgs = (args == null || args.Length == 0) ? new Span<jvalue>() : stackalloc jvalue[args.Length];
            AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
            try
            {
                AndroidJNISafe.CallVoidMethod(m_jobject, methodID, jniArgs);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
            }
        }

        protected ReturnType _Call<ReturnType>(string methodName, params object[] args)
        {
            IntPtr methodID = AndroidJNIHelper.GetMethodID<ReturnType>(m_jclass, methodName, args, false);
            return _Call<ReturnType>(methodID, args);
        }

        protected ReturnType _Call<ReturnType>(IntPtr methodID, params object[] args)
        {
            Span<jvalue> jniArgs = (args == null || args.Length == 0) ? new Span<jvalue>() : stackalloc jvalue[args.Length];
            AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
            try
            {
                if (AndroidReflection.IsPrimitive(typeof(ReturnType)))
                {
                    if (typeof(ReturnType) == typeof(Int32))
                        return (ReturnType)(object)AndroidJNISafe.CallIntMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Boolean))
                        return (ReturnType)(object)AndroidJNISafe.CallBooleanMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Byte))
                    {
                        Debug.LogWarning("Return type <Byte> for Java method call is obsolete, use return type <SByte> instead");
                        return (ReturnType)(object)(Byte)AndroidJNISafe.CallSByteMethod(m_jobject, methodID, jniArgs);
                    }
                    else if (typeof(ReturnType) == typeof(SByte))
                        return (ReturnType)(object)AndroidJNISafe.CallSByteMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Int16))
                        return (ReturnType)(object)AndroidJNISafe.CallShortMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Int64))
                        return (ReturnType)(object)AndroidJNISafe.CallLongMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Single))
                        return (ReturnType)(object)AndroidJNISafe.CallFloatMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Double))
                        return (ReturnType)(object)AndroidJNISafe.CallDoubleMethod(m_jobject, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Char))
                        return (ReturnType)(object)AndroidJNISafe.CallCharMethod(m_jobject, methodID, jniArgs);
                }
                else if (typeof(ReturnType) == typeof(String))
                    return (ReturnType)(object)AndroidJNISafe.CallStringMethod(m_jobject, methodID, jniArgs);
                else if (typeof(ReturnType) == typeof(AndroidJavaClass))
                {
                    IntPtr jclass = AndroidJNISafe.CallObjectMethod(m_jobject, methodID, jniArgs);
                    return (jclass == IntPtr.Zero) ? default(ReturnType) : (ReturnType)(object)AndroidJavaClassDeleteLocalRef(jclass);
                }
                else if (typeof(ReturnType) == typeof(AndroidJavaObject))
                {
                    IntPtr jobject = AndroidJNISafe.CallObjectMethod(m_jobject, methodID, jniArgs);
                    return (jobject == IntPtr.Zero) ? default(ReturnType) : (ReturnType)(object)AndroidJavaObjectDeleteLocalRef(jobject);
                }
                else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(ReturnType)))
                {
                    IntPtr jobject = AndroidJNISafe.CallObjectMethod(m_jobject, methodID, jniArgs);
                    return FromJavaArrayDeleteLocalRef<ReturnType>(jobject);
                }
                else
                {
                    throw new Exception("JNI: Unknown return type '" + typeof(ReturnType) + "'");
                }
                return default(ReturnType);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
            }
        }

        //===================================================================

        protected FieldType _Get<FieldType>(string fieldName)
        {
            IntPtr fieldID = AndroidJNIHelper.GetFieldID<FieldType>(m_jclass, fieldName, false);
            return _Get<FieldType>(fieldID);
        }

        protected FieldType _Get<FieldType>(IntPtr fieldID)
        {
            if (AndroidReflection.IsPrimitive(typeof(FieldType)))
            {
                if (typeof(FieldType) == typeof(Int32))
                    return (FieldType)(object)AndroidJNISafe.GetIntField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Boolean))
                    return (FieldType)(object)AndroidJNISafe.GetBooleanField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Byte))
                {
                    Debug.LogWarning("Field type <Byte> for Java get field call is obsolete, use field type <SByte> instead");
                    return (FieldType)(object)(Byte)AndroidJNISafe.GetSByteField(m_jobject, fieldID);
                }
                else if (typeof(FieldType) == typeof(SByte))
                    return (FieldType)(object)AndroidJNISafe.GetSByteField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Int16))
                    return (FieldType)(object)AndroidJNISafe.GetShortField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Int64))
                    return (FieldType)(object)AndroidJNISafe.GetLongField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Single))
                    return (FieldType)(object)AndroidJNISafe.GetFloatField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Double))
                    return (FieldType)(object)AndroidJNISafe.GetDoubleField(m_jobject, fieldID);
                else if (typeof(FieldType) == typeof(Char))
                    return (FieldType)(object)AndroidJNISafe.GetCharField(m_jobject, fieldID);
            }
            else if (typeof(FieldType) == typeof(String))
                return (FieldType)(object)AndroidJNISafe.GetStringField(m_jobject, fieldID);
            else if (typeof(FieldType) == typeof(AndroidJavaClass))
            {
                IntPtr jclass = AndroidJNISafe.GetObjectField(m_jobject, fieldID);
                return (jclass == IntPtr.Zero) ? default(FieldType) : (FieldType)(object)AndroidJavaClassDeleteLocalRef(jclass);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaObject))
            {
                IntPtr jobject = AndroidJNISafe.GetObjectField(m_jobject, fieldID);
                return (jobject == IntPtr.Zero) ? default(FieldType) : (FieldType)(object)AndroidJavaObjectDeleteLocalRef(jobject);
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(FieldType)))
            {
                IntPtr jobject = AndroidJNISafe.GetObjectField(m_jobject, fieldID);
                return FromJavaArrayDeleteLocalRef<FieldType>(jobject);
            }
            else
            {
                throw new Exception("JNI: Unknown field type '" + typeof(FieldType) + "'");
            }
            return default(FieldType);
        }

        protected void _Set<FieldType>(string fieldName, FieldType val)
        {
            IntPtr fieldID = AndroidJNIHelper.GetFieldID<FieldType>(m_jclass, fieldName, false);
            _Set(fieldID, val);
        }

        protected void _Set<FieldType>(IntPtr fieldID, FieldType val)
        {
            if (AndroidReflection.IsPrimitive(typeof(FieldType)))
            {
                if (typeof(FieldType) == typeof(Int32))
                    AndroidJNISafe.SetIntField(m_jobject, fieldID, (Int32)(object)val);
                else if (typeof(FieldType) == typeof(Boolean))
                    AndroidJNISafe.SetBooleanField(m_jobject, fieldID, (Boolean)(object)val);
                else if (typeof(FieldType) == typeof(Byte))
                {
                    Debug.LogWarning("Field type <Byte> for Java set field call is obsolete, use field type <SByte> instead");
                    AndroidJNISafe.SetSByteField(m_jobject, fieldID, (SByte)(Byte)(object)val);
                }
                else if (typeof(FieldType) == typeof(SByte))
                    AndroidJNISafe.SetSByteField(m_jobject, fieldID, (SByte)(object)val);
                else if (typeof(FieldType) == typeof(Int16))
                    AndroidJNISafe.SetShortField(m_jobject, fieldID, (Int16)(object)val);
                else if (typeof(FieldType) == typeof(Int64))
                    AndroidJNISafe.SetLongField(m_jobject, fieldID, (Int64)(object)val);
                else if (typeof(FieldType) == typeof(Single))
                    AndroidJNISafe.SetFloatField(m_jobject, fieldID, (Single)(object)val);
                else if (typeof(FieldType) == typeof(Double))
                    AndroidJNISafe.SetDoubleField(m_jobject, fieldID, (Double)(object)val);
                else if (typeof(FieldType) == typeof(Char))
                    AndroidJNISafe.SetCharField(m_jobject, fieldID, (Char)(object)val);
            }
            else if (typeof(FieldType) == typeof(String))
            {
                AndroidJNISafe.SetStringField(m_jobject, fieldID, (String)(object)val);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaClass))
            {
                AndroidJNISafe.SetObjectField(m_jobject, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaClass)(object)val).m_jclass);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaObject))
            {
                AndroidJNISafe.SetObjectField(m_jobject, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaObject)(object)val).m_jobject);
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(AndroidJavaProxy), typeof(FieldType)))
            {
                AndroidJNISafe.SetObjectField(m_jobject, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaProxy)(object)val).GetRawProxy());
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(FieldType)))
            {
                IntPtr jobject = AndroidJNIHelper.ConvertToJNIArray((Array)(object)val);
                AndroidJNISafe.SetObjectField(m_jobject, fieldID, jobject);
            }
            else
            {
                throw new Exception("JNI: Unknown field type '" + typeof(FieldType) + "'");
            }
        }

        //===================================================================

        protected void _CallStatic(string methodName, params object[] args)
        {
            IntPtr methodID = AndroidJNIHelper.GetMethodID(m_jclass, methodName, args, true);
            _CallStatic(methodID, args);
        }

        protected void _CallStatic(IntPtr methodID, params object[] args)
        {
            Span<jvalue> jniArgs = (args == null || args.Length == 0) ? new Span<jvalue>() : stackalloc jvalue[args.Length];
            AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
            try
            {
                AndroidJNISafe.CallStaticVoidMethod(m_jclass, methodID, jniArgs);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
            }
        }

        protected ReturnType _CallStatic<ReturnType>(string methodName, params object[] args)
        {
            IntPtr methodID = AndroidJNIHelper.GetMethodID<ReturnType>(m_jclass, methodName, args, true);
            return _CallStatic<ReturnType>(methodID, args);
        }

        protected ReturnType _CallStatic<ReturnType>(IntPtr methodID, params object[] args)
        {
            Span<jvalue> jniArgs = (args == null || args.Length == 0) ? new Span<jvalue>() : stackalloc jvalue[args.Length];
            AndroidJNIHelper.CreateJNIArgArray(args, jniArgs);
            try
            {
                if (AndroidReflection.IsPrimitive(typeof(ReturnType)))
                {
                    if (typeof(ReturnType) == typeof(Int32))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticIntMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Boolean))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticBooleanMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Byte))
                    {
                        Debug.LogWarning("Return type <Byte> for Java method call is obsolete, use return type <SByte> instead");
                        return (ReturnType)(object)(Byte)AndroidJNISafe.CallStaticSByteMethod(m_jclass, methodID, jniArgs);
                    }
                    else if (typeof(ReturnType) == typeof(SByte))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticSByteMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Int16))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticShortMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Int64))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticLongMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Single))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticFloatMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Double))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticDoubleMethod(m_jclass, methodID, jniArgs);
                    else if (typeof(ReturnType) == typeof(Char))
                        return (ReturnType)(object)AndroidJNISafe.CallStaticCharMethod(m_jclass, methodID, jniArgs);
                }
                else if (typeof(ReturnType) == typeof(String))
                    return (ReturnType)(object)AndroidJNISafe.CallStaticStringMethod(m_jclass, methodID, jniArgs);
                else if (typeof(ReturnType) == typeof(AndroidJavaClass))
                {
                    IntPtr jclass = AndroidJNISafe.CallStaticObjectMethod(m_jclass, methodID, jniArgs);
                    return (jclass == IntPtr.Zero) ? default(ReturnType) : (ReturnType)(object)AndroidJavaClassDeleteLocalRef(jclass);
                }
                else if (typeof(ReturnType) == typeof(AndroidJavaObject))
                {
                    IntPtr jobject = AndroidJNISafe.CallStaticObjectMethod(m_jclass, methodID, jniArgs);
                    return (jobject == IntPtr.Zero) ? default(ReturnType) : (ReturnType)(object)AndroidJavaObjectDeleteLocalRef(jobject);
                }
                else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(ReturnType)))
                {
                    IntPtr jobject = AndroidJNISafe.CallStaticObjectMethod(m_jclass, methodID, jniArgs);
                    return FromJavaArrayDeleteLocalRef<ReturnType>(jobject);
                }
                else
                {
                    throw new Exception("JNI: Unknown return type '" + typeof(ReturnType) + "'");
                }

                return default(ReturnType);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(args, jniArgs);
            }
        }

        //===================================================================

        protected FieldType _GetStatic<FieldType>(string fieldName)
        {
            IntPtr fieldID = AndroidJNIHelper.GetFieldID<FieldType>(m_jclass, fieldName, true);
            return _GetStatic<FieldType>(fieldID);
        }

        protected FieldType _GetStatic<FieldType>(IntPtr fieldID)
        {
            if (AndroidReflection.IsPrimitive(typeof(FieldType)))
            {
                if (typeof(FieldType) == typeof(Int32))
                    return (FieldType)(object)AndroidJNISafe.GetStaticIntField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Boolean))
                    return (FieldType)(object)AndroidJNISafe.GetStaticBooleanField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Byte))
                {
                    Debug.LogWarning("Field type <Byte> for Java get field call is obsolete, use field type <SByte> instead");
                    return (FieldType)(object)(Byte)AndroidJNISafe.GetStaticSByteField(m_jclass, fieldID);
                }
                else if (typeof(FieldType) == typeof(SByte))
                    return (FieldType)(object)AndroidJNISafe.GetStaticSByteField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Int16))
                    return (FieldType)(object)AndroidJNISafe.GetStaticShortField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Int64))
                    return (FieldType)(object)AndroidJNISafe.GetStaticLongField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Single))
                    return (FieldType)(object)AndroidJNISafe.GetStaticFloatField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Double))
                    return (FieldType)(object)AndroidJNISafe.GetStaticDoubleField(m_jclass, fieldID);
                else if (typeof(FieldType) == typeof(Char))
                    return (FieldType)(object)AndroidJNISafe.GetStaticCharField(m_jclass, fieldID);
            }
            else if (typeof(FieldType) == typeof(String))
                return (FieldType)(object)AndroidJNISafe.GetStaticStringField(m_jclass, fieldID);
            else if (typeof(FieldType) == typeof(AndroidJavaClass))
            {
                IntPtr jclass = AndroidJNISafe.GetStaticObjectField(m_jclass, fieldID);
                return (jclass == IntPtr.Zero) ? default(FieldType) : (FieldType)(object)AndroidJavaClassDeleteLocalRef(jclass);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaObject))
            {
                IntPtr jobject = AndroidJNISafe.GetStaticObjectField(m_jclass, fieldID);
                return (jobject == IntPtr.Zero) ? default(FieldType) : (FieldType)(object)AndroidJavaObjectDeleteLocalRef(jobject);
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(FieldType)))
            {
                IntPtr jobject = AndroidJNISafe.GetStaticObjectField(m_jclass, fieldID);
                return FromJavaArrayDeleteLocalRef<FieldType>(jobject);
            }
            else
            {
                throw new Exception("JNI: Unknown field type '" + typeof(FieldType) + "'");
            }
            return default(FieldType);
        }

        protected void _SetStatic<FieldType>(string fieldName, FieldType val)
        {
            IntPtr fieldID = AndroidJNIHelper.GetFieldID<FieldType>(m_jclass, fieldName, true);
            _SetStatic(fieldID, val);
        }

        protected void _SetStatic<FieldType>(IntPtr fieldID, FieldType val)
        {
            if (AndroidReflection.IsPrimitive(typeof(FieldType)))
            {
                if (typeof(FieldType) == typeof(Int32))
                    AndroidJNISafe.SetStaticIntField(m_jclass, fieldID, (Int32)(object)val);
                else if (typeof(FieldType) == typeof(Boolean))
                    AndroidJNISafe.SetStaticBooleanField(m_jclass, fieldID, (Boolean)(object)val);
                else if (typeof(FieldType) == typeof(Byte))
                {
                    Debug.LogWarning("Field type <Byte> for Java set field call is obsolete, use field type <SByte> instead");
                    AndroidJNISafe.SetStaticSByteField(m_jclass, fieldID, (SByte)(Byte)(object)val);
                }
                else if (typeof(FieldType) == typeof(SByte))
                    AndroidJNISafe.SetStaticSByteField(m_jclass, fieldID, (SByte)(object)val);
                else if (typeof(FieldType) == typeof(Int16))
                    AndroidJNISafe.SetStaticShortField(m_jclass, fieldID, (Int16)(object)val);
                else if (typeof(FieldType) == typeof(Int64))
                    AndroidJNISafe.SetStaticLongField(m_jclass, fieldID, (Int64)(object)val);
                else if (typeof(FieldType) == typeof(Single))
                    AndroidJNISafe.SetStaticFloatField(m_jclass, fieldID, (Single)(object)val);
                else if (typeof(FieldType) == typeof(Double))
                    AndroidJNISafe.SetStaticDoubleField(m_jclass, fieldID, (Double)(object)val);
                else if (typeof(FieldType) == typeof(Char))
                    AndroidJNISafe.SetStaticCharField(m_jclass, fieldID, (Char)(object)val);
            }
            else if (typeof(FieldType) == typeof(String))
            {
                AndroidJNISafe.SetStaticStringField(m_jclass, fieldID, (String)(object)val);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaClass))
            {
                AndroidJNISafe.SetStaticObjectField(m_jclass, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaClass)(object)val).m_jclass);
            }
            else if (typeof(FieldType) == typeof(AndroidJavaObject))
            {
                AndroidJNISafe.SetStaticObjectField(m_jclass, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaObject)(object)val).m_jobject);
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(AndroidJavaProxy), typeof(FieldType)))
            {
                AndroidJNISafe.SetStaticObjectField(m_jclass, fieldID, val == null ? IntPtr.Zero : ((AndroidJavaProxy)(object)val).GetRawProxy());
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), typeof(FieldType)))
            {
                IntPtr jobject = AndroidJNIHelper.ConvertToJNIArray((Array)(object)val);
                AndroidJNISafe.SetStaticObjectField(m_jclass, fieldID, jobject);
            }
            else
            {
                throw new Exception("JNI: Unknown field type '" + typeof(FieldType) + "'");
            }
        }

        internal static AndroidJavaObject AndroidJavaObjectDeleteLocalRef(IntPtr jobject)
        {
            try { return new AndroidJavaObject(jobject); } finally { AndroidJNISafe.DeleteLocalRef(jobject); }
        }

        internal static AndroidJavaClass AndroidJavaClassDeleteLocalRef(IntPtr jclass)
        {
            try { return new AndroidJavaClass(jclass); } finally { AndroidJNISafe.DeleteLocalRef(jclass); }
        }

        internal static ReturnType FromJavaArrayDeleteLocalRef<ReturnType>(IntPtr jobject)
        {
            if (jobject == IntPtr.Zero)
                return default(ReturnType);
            try
            {
                return (ReturnType)(object)AndroidJNIHelper.ConvertFromJNIArray<ReturnType>(jobject);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jobject);
            }
        }

        //===================================================================
        protected IntPtr _GetRawObject() { return m_jobject == null ? IntPtr.Zero : m_jobject; }
        protected IntPtr _GetRawClass() { return m_jclass; }

        internal GlobalJavaObjectRef m_jobject;
        internal GlobalJavaObjectRef m_jclass;          // use this for static lookups; reset in subclases
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        // Construct an AndroidJavaClass from the class name
        public AndroidJavaClass(string className) : base()
        {
            _AndroidJavaClass(className);
        }

        private void _AndroidJavaClass(string className)
        {
            DebugPrint("Creating AndroidJavaClass from " + className);
            var clazz = AndroidJNISafe.FindClass(className.Replace('.', '/'));
            m_jclass = new GlobalJavaObjectRef(clazz /*.GetRawObject()*/);
            m_jobject = null;
            AndroidJNISafe.DeleteLocalRef(clazz);
        }

        internal AndroidJavaClass(IntPtr jclass)  // should be protected and friends with AndroidJNIHelper..
        {
            if (jclass == IntPtr.Zero)
            {
                throw new Exception("JNI: Init'd AndroidJavaClass with null ptr!");
            }

            m_jclass = new GlobalJavaObjectRef(jclass);
            m_jobject = null;
        }
    }

    internal class AndroidReflection
    {
        public static bool IsPrimitive(System.Type t)
        {
            return t.IsPrimitive;
        }

        public static bool IsAssignableFrom(System.Type t, System.Type from)
        {
            return t.IsAssignableFrom(from);
        }

        private static IntPtr GetStaticMethodID(string clazz, string methodName, string signature)
        {
            IntPtr jclass = AndroidJNISafe.FindClass(clazz);
            try
            {
                return AndroidJNISafe.GetStaticMethodID(jclass, methodName, signature);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jclass);
            }
        }

        private static IntPtr GetMethodID(string clazz, string methodName, string signature)
        {
            IntPtr jclass = AndroidJNISafe.FindClass(clazz);
            try
            {
                return AndroidJNISafe.GetMethodID(jclass, methodName, signature);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jclass);
            }
        }

        private const string RELECTION_HELPER_CLASS_NAME = "com/unity3d/player/ReflectionHelper";
        private static readonly GlobalJavaObjectRef s_ReflectionHelperClass  = new GlobalJavaObjectRef(AndroidJNISafe.FindClass(RELECTION_HELPER_CLASS_NAME));
        private static readonly IntPtr s_ReflectionHelperGetConstructorID    = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "getConstructorID", "(Ljava/lang/Class;Ljava/lang/String;)Ljava/lang/reflect/Constructor;");
        private static readonly IntPtr s_ReflectionHelperGetMethodID         = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "getMethodID", "(Ljava/lang/Class;Ljava/lang/String;Ljava/lang/String;Z)Ljava/lang/reflect/Method;");
        private static readonly IntPtr s_ReflectionHelperGetFieldID          = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "getFieldID", "(Ljava/lang/Class;Ljava/lang/String;Ljava/lang/String;Z)Ljava/lang/reflect/Field;");
        private static readonly IntPtr s_ReflectionHelperGetFieldSignature   = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "getFieldSignature", "(Ljava/lang/reflect/Field;)Ljava/lang/String;");
        private static readonly IntPtr s_ReflectionHelperNewProxyInstance    = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "newProxyInstance", "(Lcom/unity3d/player/UnityPlayer;JLjava/lang/Class;)Ljava/lang/Object;");
        private static readonly IntPtr s_ReflectionHelperCeateInvocationError = GetStaticMethodID(RELECTION_HELPER_CLASS_NAME, "createInvocationError", "(JZ)Ljava/lang/Object;");
        private static readonly IntPtr s_FieldGetDeclaringClass              = GetMethodID("java/lang/reflect/Field", "getDeclaringClass", "()Ljava/lang/Class;");

        public static IntPtr GetConstructorMember(IntPtr jclass, string signature)
        {
            jvalue[] jniArgs = new jvalue[2];
            try
            {
                jniArgs[0].l = jclass;
                jniArgs[1].l = AndroidJNISafe.NewString(signature);
                return AndroidJNISafe.CallStaticObjectMethod(s_ReflectionHelperClass, s_ReflectionHelperGetConstructorID, jniArgs);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jniArgs[1].l);
            }
        }

        public static IntPtr GetMethodMember(IntPtr jclass, string methodName, string signature, bool isStatic)
        {
            jvalue[] jniArgs = new jvalue[4];
            try
            {
                jniArgs[0].l = jclass;
                jniArgs[1].l = AndroidJNISafe.NewString(methodName);
                jniArgs[2].l = AndroidJNISafe.NewString(signature);
                jniArgs[3].z = isStatic;
                return AndroidJNISafe.CallStaticObjectMethod(s_ReflectionHelperClass, s_ReflectionHelperGetMethodID, jniArgs);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jniArgs[1].l);
                AndroidJNISafe.DeleteLocalRef(jniArgs[2].l);
            }
        }

        public static IntPtr GetFieldMember(IntPtr jclass, string fieldName, string signature, bool isStatic)
        {
            jvalue[] jniArgs = new jvalue[4];
            try
            {
                jniArgs[0].l = jclass;
                jniArgs[1].l = AndroidJNISafe.NewString(fieldName);
                jniArgs[2].l = AndroidJNISafe.NewString(signature);
                jniArgs[3].z = isStatic;
                return AndroidJNISafe.CallStaticObjectMethod(s_ReflectionHelperClass, s_ReflectionHelperGetFieldID, jniArgs);
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(jniArgs[1].l);
                AndroidJNISafe.DeleteLocalRef(jniArgs[2].l);
            }
        }

        public static IntPtr GetFieldClass(IntPtr field)
        {
            return AndroidJNISafe.CallObjectMethod(field, s_FieldGetDeclaringClass, null);
        }

        public static string GetFieldSignature(IntPtr field)
        {
            jvalue[] jniArgs = new jvalue[1];
            jniArgs[0].l = field;
            return AndroidJNISafe.CallStaticStringMethod(s_ReflectionHelperClass, s_ReflectionHelperGetFieldSignature, jniArgs);
        }

        public static IntPtr NewProxyInstance(IntPtr player, IntPtr delegateHandle, IntPtr interfaze)
        {
            jvalue[] jniArgs = new jvalue[3];
            jniArgs[0].l = player;
            jniArgs[1].j = delegateHandle.ToInt64();
            jniArgs[2].l = interfaze;
            return AndroidJNISafe.CallStaticObjectMethod(s_ReflectionHelperClass, s_ReflectionHelperNewProxyInstance, jniArgs);
        }

        internal static IntPtr CreateInvocationError(Exception ex, bool methodNotFound)
        {
            jvalue[] jniArgs = new jvalue[2];
            jniArgs[0].j = GCHandle.ToIntPtr(GCHandle.Alloc(ex)).ToInt64();
            jniArgs[1].z = methodNotFound;
            return AndroidJNISafe.CallStaticObjectMethod(s_ReflectionHelperClass, s_ReflectionHelperCeateInvocationError, jniArgs);
        }
    }

    [UsedByNativeCode]
    sealed class _AndroidJNIHelper
    {
        public static IntPtr CreateJavaProxy(IntPtr player, IntPtr delegateHandle, AndroidJavaProxy proxy)
        {
            return AndroidReflection.NewProxyInstance(player, delegateHandle, proxy.javaInterface.GetRawClass());
        }

        public static IntPtr CreateJavaRunnable(AndroidJavaRunnable jrunnable)
        {
            return AndroidJNIHelper.CreateJavaProxy(new AndroidJavaRunnableProxy(jrunnable));
        }

        [RequiredByNativeCode]
        public static IntPtr InvokeJavaProxyMethod(AndroidJavaProxy proxy, IntPtr jmethodName, IntPtr jargs)
        {
            try
            {
                return proxy.Invoke(AndroidJNI.GetStringChars(jmethodName), jargs);
            }
            catch (Exception e)
            {
                return AndroidReflection.CreateInvocationError(e, methodNotFound: false);
            }
        }

        public static void CreateJNIArgArray(object[] args, Span<jvalue> ret)
        {
            int i = 0;
            foreach (object obj in args)
            {
                if (obj == null)
                    ret[i].l = System.IntPtr.Zero;
                else if (AndroidReflection.IsPrimitive(obj.GetType()))
                {
                    if (obj is System.Int32)
                        ret[i].i = (System.Int32)obj;
                    else if (obj is System.Boolean)
                        ret[i].z = (System.Boolean)obj;
                    else if (obj is System.Byte)
                    {
                        Debug.LogWarning("Passing Byte arguments to Java methods is obsolete, pass SByte parameters instead");
                        ret[i].b = (System.SByte)(System.Byte) obj;
                    }
                    else if (obj is System.SByte)
                        ret[i].b = (System.SByte)obj;
                    else if (obj is System.Int16)
                        ret[i].s = (System.Int16)obj;
                    else if (obj is System.Int64)
                        ret[i].j = (System.Int64)obj;
                    else if (obj is System.Single)
                        ret[i].f = (System.Single)obj;
                    else if (obj is System.Double)
                        ret[i].d = (System.Double)obj;
                    else if (obj is System.Char)
                        ret[i].c = (System.Char)obj;
                }
                else if (obj is System.String)
                {
                    ret[i].l = AndroidJNISafe.NewString((System.String)obj);
                }
                else if (obj is AndroidJavaClass)
                {
                    ret[i].l = ((AndroidJavaClass)obj).GetRawClass();
                }
                else if (obj is AndroidJavaObject)
                {
                    ret[i].l = ((AndroidJavaObject)obj).GetRawObject();
                }
                else if (obj is System.Array)
                {
                    ret[i].l = ConvertToJNIArray((System.Array)obj);
                }
                else if (obj is AndroidJavaProxy)
                {
                    ret[i].l = ((AndroidJavaProxy)obj).GetRawProxy();
                }
                else if (obj is AndroidJavaRunnable)
                {
                    ret[i].l = AndroidJNIHelper.CreateJavaRunnable((AndroidJavaRunnable)obj);
                }
                else
                {
                    throw new Exception("JNI; Unknown argument type '" + obj.GetType() + "'");
                }
                ++i;
            }
        }

        public static object UnboxArray(AndroidJavaObject obj)
        {
            if (obj == null)
                return null;

            AndroidJavaClass arrayUtil  = new AndroidJavaClass("java/lang/reflect/Array");
            AndroidJavaObject objClass  = obj.Call<AndroidJavaObject>("getClass");
            AndroidJavaObject compClass = objClass.Call<AndroidJavaObject>("getComponentType");
            string className            = compClass.Call<string>("getName");

            int arrayLength = arrayUtil.CallStatic<int>("getLength", obj);
            Array array;
            if (compClass.Call<bool>("isPrimitive")) // need to setup primitive array
            {
                if ("int" == className)
                    array = new int[arrayLength];
                else if ("boolean" == className)
                    array = new bool[arrayLength];
                else if ("byte" == className)
                    array = new sbyte[arrayLength];
                else if ("short" == className)
                    array = new short[arrayLength];
                else if ("long" == className)
                    array = new long[arrayLength];
                else if ("float" == className)
                    array = new float[arrayLength];
                else if ("double" == className)
                    array = new double[arrayLength];
                else if ("char" == className)
                    array = new char[arrayLength];
                else
                    throw new Exception("JNI; Unknown argument type '" + className + "'");
            }
            else if ("java.lang.String" == className)
                array = new string[arrayLength];
            else if ("java.lang.Class" == className)
                array = new AndroidJavaClass[arrayLength];
            else
                array = new AndroidJavaObject[arrayLength];

            for (int i = 0; i < arrayLength; ++i)
                array.SetValue(Unbox(arrayUtil.CallStatic<AndroidJavaObject>("get", obj, i)), i);

            arrayUtil.Dispose();
            return array;
        }

        public static object Unbox(AndroidJavaObject obj)
        {
            if (obj == null)
                return null;

            using (AndroidJavaObject clazz = obj.Call<AndroidJavaObject>("getClass"))
            {
                string className        = clazz.Call<string>("getName");
                if ("java.lang.Integer" == className)
                    return obj.Call<System.Int32>("intValue");
                else if ("java.lang.Boolean" == className)
                    return obj.Call<System.Boolean>("booleanValue");
                else if ("java.lang.Byte" == className)
                    return obj.Call<System.SByte>("byteValue");
                else if ("java.lang.Short" == className)
                    return obj.Call<System.Int16>("shortValue");
                else if ("java.lang.Long" == className)
                    return obj.Call<System.Int64>("longValue");
                else if ("java.lang.Float" == className)
                    return obj.Call<System.Single>("floatValue");
                else if ("java.lang.Double" == className)
                    return obj.Call<System.Double>("doubleValue");
                else if ("java.lang.Character" == className)
                    return obj.Call<System.Char>("charValue");
                else if ("java.lang.String" == className)
                    return obj.Call<System.String>("toString"); // um, can obvoiusly be performed in a better fasion
                else if ("java.lang.Class" == className)
                    return new AndroidJavaClass(obj.GetRawObject());
                else if (clazz.Call<bool>("isArray"))
                    return UnboxArray(obj);
                else
                    return obj;
            }
        }

        public static AndroidJavaObject Box(object obj)
        {
            if (obj == null)
                return null;
            else if (AndroidReflection.IsPrimitive(obj.GetType()))
            {
                if (obj is System.Int32)
                    return new AndroidJavaObject("java.lang.Integer", (System.Int32)obj);
                else if (obj is System.Boolean)
                    return new AndroidJavaObject("java.lang.Boolean", (System.Boolean)obj);
                else if (obj is System.Byte)
                    return new AndroidJavaObject("java.lang.Byte", (System.SByte)obj);
                else if (obj is System.SByte)
                    return new AndroidJavaObject("java.lang.Byte", (System.SByte)obj);
                else if (obj is System.Int16)
                    return new AndroidJavaObject("java.lang.Short", (System.Int16)obj);
                else if (obj is System.Int64)
                    return new AndroidJavaObject("java.lang.Long", (System.Int64)obj);
                else if (obj is System.Single)
                    return new AndroidJavaObject("java.lang.Float", (System.Single)obj);
                else if (obj is System.Double)
                    return new AndroidJavaObject("java.lang.Double", (System.Double)obj);
                else if (obj is System.Char)
                    return new AndroidJavaObject("java.lang.Character", (System.Char)obj);
                else
                    throw new Exception("JNI; Unknown argument type '" + obj.GetType() + "'");
            }
            else if (obj is System.String)
            {
                return new AndroidJavaObject("java.lang.String", (System.String)obj);
            }
            else if (obj is AndroidJavaClass)
            {
                return new AndroidJavaObject(((AndroidJavaClass)obj).GetRawClass());
            }
            else if (obj is AndroidJavaObject)
            {
                return (AndroidJavaObject)obj;
            }
            else if (obj is System.Array)
            {
                return AndroidJavaObject.AndroidJavaObjectDeleteLocalRef(ConvertToJNIArray((System.Array)obj));
            }
            else if (obj is AndroidJavaProxy)
            {
                return ((AndroidJavaProxy)obj).GetProxyObject();
            }
            else if (obj is AndroidJavaRunnable)
            {
                return AndroidJavaObject.AndroidJavaObjectDeleteLocalRef(AndroidJNIHelper.CreateJavaRunnable((AndroidJavaRunnable)obj));
            }
            else
            {
                throw new Exception("JNI; Unknown argument type '" + obj.GetType() + "'");
            }
        }

        public static void DeleteJNIArgArray(object[] args, Span<jvalue> jniArgs)
        {
            if (args == null)
                return;
            int i = 0;
            foreach (object obj in args)
            {
                if (obj is System.String || obj is AndroidJavaRunnable || obj is AndroidJavaProxy || obj is System.Array)
                    AndroidJNISafe.DeleteLocalRef(jniArgs[i].l);

                ++i;
            }
        }

        public static IntPtr ConvertToJNIArray(System.Array array)
        {
            Type type = array.GetType().GetElementType();
            if (AndroidReflection.IsPrimitive(type))
            {
                if (type == typeof(Int32))
                    return AndroidJNISafe.ToIntArray((Int32[])array);
                else if (type == typeof(Boolean))
                    return AndroidJNISafe.ToBooleanArray((Boolean[])array);
                else if (type == typeof(Byte))
                {
                    Debug.LogWarning("AndroidJNIHelper: converting Byte array is obsolete, use SByte array instead");
#pragma warning disable 0618
                    return AndroidJNISafe.ToByteArray((Byte[])array);
#pragma warning restore 0618
                }
                else if (type == typeof(SByte))
                    return AndroidJNISafe.ToSByteArray((SByte[])array);
                else if (type == typeof(Int16))
                    return AndroidJNISafe.ToShortArray((Int16[])array);
                else if (type == typeof(Int64))
                    return AndroidJNISafe.ToLongArray((Int64[])array);
                else if (type == typeof(Single))
                    return AndroidJNISafe.ToFloatArray((Single[])array);
                else if (type == typeof(Double))
                    return AndroidJNISafe.ToDoubleArray((Double[])array);
                else if (type == typeof(Char))
                    return AndroidJNISafe.ToCharArray((Char[])array);
            }
            else if (type == typeof(String))
            {
                String[] strArray = (string[])array;
                int arrayLen = array.GetLength(0);
                IntPtr arrayType = AndroidJNISafe.FindClass("java/lang/String");
                IntPtr res = AndroidJNI.NewObjectArray(arrayLen, arrayType, IntPtr.Zero);
                for (int i = 0; i < arrayLen; ++i)
                {
                    IntPtr jstring = AndroidJNISafe.NewString(strArray[i]);
                    AndroidJNI.SetObjectArrayElement(res, i, jstring);
                    AndroidJNISafe.DeleteLocalRef(jstring);
                }
                AndroidJNISafe.DeleteLocalRef(arrayType);
                return res;
            }
            else if (type == typeof(AndroidJavaObject))
            {
                AndroidJavaObject[] objArray = (AndroidJavaObject[])array;
                int arrayLen = array.GetLength(0);
                IntPtr[] jniObjs = new IntPtr[arrayLen];
                IntPtr fallBackType = AndroidJNISafe.FindClass("java/lang/Object");
                IntPtr arrayType = IntPtr.Zero;
                for (int i = 0; i < arrayLen; ++i)
                {
                    if (objArray[i] != null)
                    {
                        jniObjs[i] = objArray[i].GetRawObject();
                        IntPtr objectType = objArray[i].GetRawClass();
                        if (arrayType == IntPtr.Zero)
                        {
                            arrayType = objectType;
                        }
                        else if (arrayType != fallBackType && !AndroidJNI.IsSameObject(arrayType, objectType))
                        {
                            arrayType = fallBackType; // java/lang/Object
                        }
                    }
                    else
                    {
                        jniObjs[i] = IntPtr.Zero;
                    }
                }
                // zero sized array will call this with IntPtr.Zero type translated into java/lang/Object
                IntPtr res = AndroidJNISafe.ToObjectArray(jniObjs, arrayType);
                AndroidJNISafe.DeleteLocalRef(fallBackType);
                return res;
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(AndroidJavaProxy), type))
            {
                AndroidJavaProxy[] objArray = (AndroidJavaProxy[])array;
                int arrayLen = array.GetLength(0);
                IntPtr[] jniObjs = new IntPtr[arrayLen];
                IntPtr fallBackType = AndroidJNISafe.FindClass("java/lang/Object");
                IntPtr arrayType = IntPtr.Zero;

                for (int i = 0; i < arrayLen; ++i)
                {
                    if (objArray[i] != null)
                    {
                        jniObjs[i] = objArray[i].GetRawProxy();
                        IntPtr objectType = objArray[i].javaInterface.GetRawClass();
                        if (arrayType == IntPtr.Zero)
                        {
                            arrayType = objectType;
                        }
                        else if (arrayType != fallBackType && !AndroidJNI.IsSameObject(arrayType, objectType))
                        {
                            arrayType = fallBackType; // java/lang/Object
                        }
                    }
                    else
                    {
                        jniObjs[i] = IntPtr.Zero;
                    }
                }
                // zero sized array will call this with IntPtr.Zero type translated into java/lang/Object
                IntPtr res = AndroidJNISafe.ToObjectArray(jniObjs, arrayType);
                AndroidJNISafe.DeleteLocalRef(fallBackType);
                return res;
            }
            else
            {
                throw new Exception("JNI; Unknown array type '" + type + "'");
            }
            return IntPtr.Zero;
        }

        public static ArrayType ConvertFromJNIArray<ArrayType>(IntPtr array)
        {
            Type type = typeof(ArrayType).GetElementType();
            if (AndroidReflection.IsPrimitive(type))
            {
                if (type == typeof(Int32))
                    return (ArrayType)(object)AndroidJNISafe.FromIntArray(array);
                else if (type == typeof(Boolean))
                    return (ArrayType)(object)AndroidJNISafe.FromBooleanArray(array);
                else if (type == typeof(Byte))
                {
                    Debug.LogWarning("AndroidJNIHelper: converting from Byte array is obsolete, use SByte array instead");
#pragma warning disable 0618
                    return (ArrayType)(object)AndroidJNISafe.FromByteArray(array);
#pragma warning restore 0618
                }
                else if (type == typeof(SByte))
                    return (ArrayType)(object)AndroidJNISafe.FromSByteArray(array);
                else if (type == typeof(Int16))
                    return (ArrayType)(object)AndroidJNISafe.FromShortArray(array);
                else if (type == typeof(Int64))
                    return (ArrayType)(object)AndroidJNISafe.FromLongArray(array);
                else if (type == typeof(Single))
                    return (ArrayType)(object)AndroidJNISafe.FromFloatArray(array);
                else if (type == typeof(Double))
                    return (ArrayType)(object)AndroidJNISafe.FromDoubleArray(array);
                else if (type == typeof(Char))
                    return (ArrayType)(object)AndroidJNISafe.FromCharArray(array);
            }
            else if (type == typeof(String))
            {
                int arrayLen = AndroidJNISafe.GetArrayLength(array);
                string[] strArray = new string[arrayLen];
                for (int i = 0; i < arrayLen; ++i)
                {
                    IntPtr jstring = AndroidJNI.GetObjectArrayElement(array, i);
                    strArray[i] = AndroidJNISafe.GetStringChars(jstring);
                    AndroidJNISafe.DeleteLocalRef(jstring);
                }
                return (ArrayType)(object)strArray;
            }
            else if (type == typeof(AndroidJavaObject))
            {
                int arrayLen = AndroidJNISafe.GetArrayLength(array);
                AndroidJavaObject[] objArray = new AndroidJavaObject[arrayLen];
                for (int i = 0; i < arrayLen; ++i)
                {
                    IntPtr jobject = AndroidJNI.GetObjectArrayElement(array, i);
                    objArray[i] = new AndroidJavaObject(jobject);
                    AndroidJNISafe.DeleteLocalRef(jobject);
                }
                return (ArrayType)(object)objArray;
            }
            else
            {
                throw new Exception("JNI: Unknown generic array type '" + type + "'");
            }
            return default(ArrayType);
        }

        public static System.IntPtr GetConstructorID(System.IntPtr jclass, object[] args)
        {
            return AndroidJNIHelper.GetConstructorID(jclass, GetSignature(args));
        }

        public static System.IntPtr GetMethodID(System.IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return AndroidJNIHelper.GetMethodID(jclass, methodName, GetSignature(args), isStatic);
        }

        public static System.IntPtr GetMethodID<ReturnType>(System.IntPtr jclass, string methodName, object[] args, bool isStatic)
        {
            return AndroidJNIHelper.GetMethodID(jclass, methodName, GetSignature<ReturnType>(args), isStatic);
        }

        public static System.IntPtr GetFieldID<ReturnType>(System.IntPtr jclass, string fieldName, bool isStatic)
        {
            return AndroidJNIHelper.GetFieldID(jclass, fieldName, GetSignature(typeof(ReturnType)), isStatic);
        }

        public static IntPtr GetConstructorID(IntPtr jclass, string signature)
        {
            IntPtr constructor = IntPtr.Zero;
            try
            {
                constructor = AndroidReflection.GetConstructorMember(jclass, signature);
                return AndroidJNISafe.FromReflectedMethod(constructor);
            }
            catch (Exception e)
            {
                IntPtr memberID = AndroidJNISafe.GetMethodID(jclass, "<init>", signature);
                if (memberID != IntPtr.Zero)
                    return memberID;
                throw e;
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(constructor);
            }
        }

        public static IntPtr GetMethodID(IntPtr jclass, string methodName, string signature, bool isStatic)
        {
            IntPtr method = IntPtr.Zero;
            try
            {
                method = AndroidReflection.GetMethodMember(jclass, methodName, signature, isStatic);
                return AndroidJNISafe.FromReflectedMethod(method);
            }
            catch (Exception e)
            {
                // Make sure this method does not throw to keep e intact
                IntPtr memberID = GetMethodIDFallback(jclass, methodName, signature, isStatic);
                if (memberID != IntPtr.Zero)
                    return memberID;
                throw e;
            }
            finally
            {
                AndroidJNISafe.DeleteLocalRef(method);
            }
        }

        private static IntPtr GetMethodIDFallback(IntPtr jclass, string methodName, string signature, bool isStatic)
        {
            try
            {
                return isStatic ?
                    AndroidJNISafe.GetStaticMethodID(jclass, methodName, signature) :
                    AndroidJNISafe.GetMethodID(jclass, methodName, signature);
            }
            catch (Exception)
            {
                // We don't want this exception to override the initial exception from AndroidReflection
            }
            return IntPtr.Zero;
        }

        public static IntPtr GetFieldID(IntPtr jclass, string fieldName, string signature, bool isStatic)
        {
            IntPtr memberID = IntPtr.Zero;
            Exception reflectEx = null;
            AndroidJNI.PushLocalFrame(10);
            try
            {
                IntPtr field = AndroidReflection.GetFieldMember(jclass, fieldName, signature, isStatic);
                if (!isStatic)
                    jclass = AndroidReflection.GetFieldClass(field);
                signature = AndroidReflection.GetFieldSignature(field);
            }
            catch (Exception e)
            {
                reflectEx = e;
            }

            try
            {
                memberID = isStatic
                    ? AndroidJNISafe.GetStaticFieldID(jclass, fieldName, signature)
                    : AndroidJNISafe.GetFieldID(jclass, fieldName, signature);
                if (memberID == IntPtr.Zero)
                {
                    if (reflectEx != null)
                        throw reflectEx;
                    throw new Exception(string.Format("Field {0} or type signature {1} not found", fieldName, signature));
                }
                return memberID;
            }
            finally
            {
                AndroidJNI.PopLocalFrame(IntPtr.Zero);
            }
        }

        public static string GetSignature(object obj)
        {
            if (obj == null)
                return "Ljava/lang/Object;";
            System.Type type = (obj is System.Type) ? (System.Type)obj : obj.GetType();
            if (AndroidReflection.IsPrimitive(type))
            {
                if (type.Equals(typeof(System.Int32)))
                    return "I";
                else if (type.Equals(typeof(System.Boolean)))
                    return "Z";
                else if (type.Equals(typeof(System.Byte)))
                {
                    Debug.LogWarning("AndroidJNIHelper.GetSignature: using Byte parameters is obsolete, use SByte parameters instead");
                    return "B";
                }
                else if (type.Equals(typeof(System.SByte)))
                    return "B";
                else if (type.Equals(typeof(System.Int16)))
                    return "S";
                else if (type.Equals(typeof(System.Int64)))
                    return "J";
                else if (type.Equals(typeof(System.Single)))
                    return "F";
                else if (type.Equals(typeof(System.Double)))
                    return "D";
                else if (type.Equals(typeof(System.Char)))
                    return "C";
            }
            else if (type.Equals(typeof(System.String)))
            {
                return "Ljava/lang/String;";
            }
            else if (obj is AndroidJavaProxy)
            {
                using (var javaClass = new AndroidJavaObject(((AndroidJavaProxy)obj).javaInterface.GetRawClass()))
                {
                    return "L" + javaClass.Call<System.String>("getName") + ";";
                }
            }
            else if (obj == (object)type && AndroidReflection.IsAssignableFrom(typeof(AndroidJavaProxy), type))
            {
                // underlying Java class/interface can be found only for an actual object of 'type',
                // and we cannot use Activator.CreateInstance(type) here because default constructor might not exist for 'type',
                // returning empty string, this way ReflectionHelper finds field by name only.
                return "";
            }
            else if (type.Equals(typeof(AndroidJavaRunnable)))
            {
                return "Ljava/lang/Runnable;";
            }
            else if (type.Equals(typeof(AndroidJavaClass)))
            {
                return "Ljava/lang/Class;";
            }
            else if (type.Equals(typeof(AndroidJavaObject)))
            {
                if (obj == (object)type)
                {
                    return "Ljava/lang/Object;";
                }
                AndroidJavaObject javaObject = (AndroidJavaObject)obj;
                using (AndroidJavaObject javaClass = javaObject.Call<AndroidJavaObject>("getClass"))
                {
                    return "L" + javaClass.Call<System.String>("getName") + ";";
                }
            }
            else if (AndroidReflection.IsAssignableFrom(typeof(System.Array), type))
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new Exception("JNI: System.Array in n dimensions is not allowed");
                }
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append('[');
                sb.Append(GetSignature(type.GetElementType()));
                return sb.Length > 1 ? sb.ToString() : "";
            }
            else
            {
                throw new Exception("JNI: Unknown signature for type '" + type + "' (obj = " + obj + ") " + ((object)type == obj ? "equal" : "instance"));
            }
            return "";
        }

        public static string GetSignature(object[] args)
        {
            if (args == null || args.Length == 0)
                return "()V";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('(');
            foreach (object obj in args)
            {
                sb.Append(GetSignature(obj));
            }
            sb.Append(")V");
            return sb.ToString();
        }

        public static string GetSignature<ReturnType>(object[] args)
        {
            if (args == null || args.Length == 0)
                return "()" + GetSignature(typeof(ReturnType));
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('(');
            foreach (object obj in args)
            {
                sb.Append(GetSignature(obj));
            }
            sb.Append(')');
            sb.Append(GetSignature(typeof(ReturnType)));
            return sb.ToString();
        }
    }
}
