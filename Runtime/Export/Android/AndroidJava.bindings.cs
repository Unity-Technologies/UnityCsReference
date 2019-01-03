// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEngine
{

    public partial class AndroidJavaObject : IDisposable
    {
        internal AndroidJavaObject() {}
        public AndroidJavaObject(string className, params object[] args) {}
        public void Dispose() {}
        public void Call(string methodName, params object[] args) {}
        public void CallStatic(string methodName, params object[] args) {}
        public FieldType Get<FieldType>(string fieldName) { return default(FieldType); }
        public void Set<FieldType>(string fieldName, FieldType val) {}
        public FieldType GetStatic<FieldType>(string fieldName) { return default(FieldType); }
        public void SetStatic<FieldType>(string fieldName, FieldType val) {}
        public IntPtr GetRawObject() { return IntPtr.Zero; }
        public IntPtr GetRawClass() { return IntPtr.Zero; }
        public ReturnType Call<ReturnType>(string methodName, params object[] args) { return default(ReturnType); }
        public ReturnType CallStatic<ReturnType>(string methodName, params object[] args) { return default(ReturnType); }
    }

    public partial class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) {}
    }
}
