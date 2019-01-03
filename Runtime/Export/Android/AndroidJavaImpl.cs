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


    public class AndroidJavaProxy
    {
        public readonly AndroidJavaClass javaInterface;
        public AndroidJavaProxy(string javaInterface) {}
        public AndroidJavaProxy(AndroidJavaClass javaInterface) {}
        public virtual AndroidJavaObject Invoke(string methodName, object[] args) { return null; }
        public virtual AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs) { return null; }
        public virtual bool equals(AndroidJavaObject obj) { return false; }
        public virtual int hashCode() { return 0; }
        public virtual string toString() { return "<c# proxy java object>"; }
    }

    public partial class AndroidJavaObject
    {
        protected void DebugPrint(string msg) {}
        protected void DebugPrint(string call, string methodName, string signature, object[] args) {}
        ~AndroidJavaObject() {}
        protected virtual void Dispose(bool disposing) {}
        protected void _Dispose() {}
        protected void _Call(string methodName, params object[] args) {}
        protected ReturnType _Call<ReturnType>(string methodName, params object[] args) { return default(ReturnType); }
        protected FieldType _Get<FieldType>(string fieldName) { return default(FieldType); }
        protected void _Set<FieldType>(string fieldName, FieldType val) {}
        protected void _CallStatic(string methodName, params object[] args) {}
        protected ReturnType _CallStatic<ReturnType>(string methodName, params object[] args) { return default(ReturnType); }
        protected FieldType _GetStatic<FieldType>(string fieldName) { return default(FieldType); }
        protected void _SetStatic<FieldType>(string fieldName, FieldType val) {}
        protected IntPtr _GetRawObject() { return IntPtr.Zero; }
        protected IntPtr _GetRawClass() { return IntPtr.Zero; }
        protected static AndroidJavaObject FindClass(string name) { return null; }
        protected static AndroidJavaClass JavaLangClass { get { return null; } }
    }

}
