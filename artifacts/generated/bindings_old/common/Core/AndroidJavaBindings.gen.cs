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


public partial class AndroidJavaObject : IDisposable
{
    public AndroidJavaObject(string className, params object[] args) : this()
        {
            _AndroidJavaObject(className, args);
        }
    
    
    public void Dispose()
        {
            _Dispose();
        }
    
    
    
    public void Call(string methodName, params object[] args)
        {
            _Call(methodName, args);
        }
    
    
    
    public void CallStatic(string methodName, params object[] args)
        {
            _CallStatic(methodName, args);
        }
    
    
    
    public FieldType Get<FieldType>(string fieldName)
        {
            return _Get<FieldType>(fieldName);
        }
    
    
    public void Set<FieldType>(string fieldName, FieldType val)
        {
            _Set<FieldType>(fieldName, val);
        }
    
    
    
    public FieldType GetStatic<FieldType>(string fieldName)
        {
            return _GetStatic<FieldType>(fieldName);
        }
    
    
    public void SetStatic<FieldType>(string fieldName, FieldType val)
        {
            _SetStatic<FieldType>(fieldName, val);
        }
    
    
    
    public IntPtr GetRawObject() { return _GetRawObject(); }
    public IntPtr GetRawClass() { return _GetRawClass(); }
    
    
    
    public ReturnType Call<ReturnType>(string methodName, params object[] args)
        {
            return _Call<ReturnType>(methodName, args);
        }
    
    
    public ReturnType CallStatic<ReturnType>(string methodName, params object[] args)
        {
            return _CallStatic<ReturnType>(methodName, args);
        }
    
    
}

public partial class AndroidJavaClass : AndroidJavaObject
{
    public AndroidJavaClass(string className) : base()
        {
            _AndroidJavaClass(className);
        }
    
    
}


}
