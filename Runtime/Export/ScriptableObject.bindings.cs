// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // A class you can derive from if you want to create objects that don't need to be attached to game objects.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeClass(null)]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    public class ScriptableObject : Object
    {
        public ScriptableObject()
        {
            CreateScriptableObject(this);
        }

        [NativeConditional("ENABLE_MONO")]
        [Obsolete("Use EditorUtility.SetDirty instead")]
        public extern void SetDirty();

        // Creates an instance of a scriptable object with /className/.
        public static ScriptableObject CreateInstance(string className)
        {
            return CreateScriptableObjectInstanceFromName(className);
        }

        // Creates an instance of a scriptable object with /type/.
        public static ScriptableObject CreateInstance(Type type)
        {
            return CreateScriptableObjectInstanceFromType(type);
        }

        // Creates an instance of a scriptable object with /T/.
        public static T CreateInstance<T>() where T : ScriptableObject
        {
            return (T)CreateInstance(typeof(T));
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static void CreateScriptableObject([Writable] ScriptableObject self);

        [FreeFunction("Scripting::CreateScriptableObject")]
        extern static ScriptableObject CreateScriptableObjectInstanceFromName(string className);

        [FreeFunction("Scripting::CreateScriptableObjectWithType")]
        extern static ScriptableObject CreateScriptableObjectInstanceFromType(Type type);
    }
}
