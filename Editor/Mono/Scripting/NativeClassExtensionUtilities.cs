// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Reflection;

namespace UnityEditor
{
    /// <summary>
    /// What is this : helper class that provides functionality for dealing with managed types that actually are extensions of Native types (MonoBehaviour for example)
    /// Motivation(s): is some rare cases, managed code needs to handle differently types that are a combination of both Native anf Managed code.
    /// </summary>
    class NativeClassExtensionUtilities // ScriptingRuntime
    {
        public static bool ExtendsANativeType(Type type)
        {
            return type.GetCustomAttributes(typeof(ExtensionOfNativeClassAttribute), true).Length != 0;
        }

        public static bool ExtendsANativeType(UnityEngine.Object obj)
        {
            return !object.ReferenceEquals(null, obj) && ExtendsANativeType(obj.GetType());
        }
    }
}
