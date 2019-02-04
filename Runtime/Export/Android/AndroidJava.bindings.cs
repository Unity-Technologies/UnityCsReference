// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEngine
{
    // AndroidJavaObject is the Unity representation of a generic instance of java.lang.Object.
    public partial class AndroidJavaObject : IDisposable
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

        // IDisposable callback
        public void Dispose()
        {
            _Dispose();
        }

        //===================================================================

        // Calls a Java method on an object (non-static).
        public void Call<T>(string methodName, T[] args)
        {
            _Call(methodName, (object)args);
        }

        public void Call(string methodName, params object[] args)
        {
            _Call(methodName, args);
        }

        //===================================================================

        // Call a static Java method on a class.
        public void CallStatic<T>(string methodName, T[] args)
        {
            _CallStatic(methodName, (object)args);
        }

        public void CallStatic(string methodName, params object[] args)
        {
            _CallStatic(methodName, args);
        }

        //===================================================================

        // Get the value of a field in an object (non-static).
        public FieldType Get<FieldType>(string fieldName)
        {
            return _Get<FieldType>(fieldName);
        }

        // Set the value of a field in an object (non-static).
        public void Set<FieldType>(string fieldName, FieldType val)
        {
            _Set<FieldType>(fieldName, val);
        }

        //===================================================================

        // Get the value of a static field in an object type.
        public FieldType GetStatic<FieldType>(string fieldName)
        {
            return _GetStatic<FieldType>(fieldName);
        }

        // Set the value of a static field in an object type.
        public void SetStatic<FieldType>(string fieldName, FieldType val)
        {
            _SetStatic<FieldType>(fieldName, val);
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

        // Call a Java method on an object.
        public ReturnType Call<ReturnType, T>(string methodName, T[] args)
        {
            return _Call<ReturnType>(methodName, (object)args);
        }

        public ReturnType Call<ReturnType>(string methodName, params object[] args)
        {
            return _Call<ReturnType>(methodName, args);
        }

        // Call a static Java method on a class.
        public ReturnType CallStatic<ReturnType, T>(string methodName, T[] args)
        {
            return _CallStatic<ReturnType>(methodName, (object)args);
        }

        public ReturnType CallStatic<ReturnType>(string methodName, params object[] args)
        {
            return _CallStatic<ReturnType>(methodName, args);
        }
    }

    // AndroidJavaClass is the Unity representation of a generic instance of java.lang.Class
    public partial class AndroidJavaClass : AndroidJavaObject
    {
        // Construct an AndroidJavaClass from the class name
        public AndroidJavaClass(string className) : base()
        {
            _AndroidJavaClass(className);
        }
    }
}
