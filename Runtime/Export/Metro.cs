// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Reflection;

namespace UnityEngineInternal
{
    using UnityEngine;

    public delegate void FastCallExceptionHandler(Exception ex);
    public delegate MethodInfo GetMethodDelegate(Type classType, string methodName, bool searchBaseTypes, bool instanceMethod, Type[] methodParamTypes);

    public partial class ScriptingUtils
    {

        public static Delegate CreateDelegate(Type type, MethodInfo methodInfo)
        {
            return Delegate.CreateDelegate(type, methodInfo);
        }
    }
}
