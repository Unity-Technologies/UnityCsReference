// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // A class you can derive from if you want to create objects that don't need to be attached to game objects.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [ExtensionOfNativeClass]
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
            return CreateScriptableObjectInstanceFromType(type, true);
        }

        // Creates an instance of a scriptable object with /T/.
        public static T CreateInstance<T>() where T : ScriptableObject
        {
            return (T)CreateInstance(typeof(T));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static ScriptableObject CreateInstance(Type type, Action<ScriptableObject> initialize)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
                throw new ArgumentException("Type must inherit ScriptableObject.", "type");

            var res = CreateScriptableObjectInstanceFromType(type, false);

            try
            {
                initialize(res);
            }
            finally
            {
                ResetAndApplyDefaultInstances(res);
            }

            return res;
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        extern static void CreateScriptableObject([Writable] ScriptableObject self);

        [FreeFunction("Scripting::CreateScriptableObject")]
        extern static ScriptableObject CreateScriptableObjectInstanceFromName(string className);

        [NativeMethod(Name = "Scripting::CreateScriptableObjectWithType", IsFreeFunction = true, ThrowsException = true)]
        extern internal static ScriptableObject CreateScriptableObjectInstanceFromType(Type type, bool applyDefaultsAndReset);

        [FreeFunction("Scripting::ResetAndApplyDefaultInstances")]
        extern internal static void ResetAndApplyDefaultInstances([NotNull] Object obj);
    }
}
