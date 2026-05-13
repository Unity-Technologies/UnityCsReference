// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(object obj, string parameterName)
        {
            if (obj is UnityEngine.Object unityObj)
                UnityEngine.Object.MarshalledUnityObject.TryThrowEditorNullExceptionObject(unityObj, parameterName);
            throw new ArgumentNullException(parameterName);
        }

        [DoesNotReturn]
        public static void ThrowNullReferenceException(object obj)
        {
            if (obj is UnityEngine.Object unityObj)
                UnityEngine.Object.MarshalledUnityObject.TryThrowEditorNullExceptionObject(unityObj, null);
            throw new NullReferenceException();
        }
    }
}
