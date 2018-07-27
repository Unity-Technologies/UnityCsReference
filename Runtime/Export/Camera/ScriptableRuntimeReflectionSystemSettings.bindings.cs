// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Camera/ScriptableRuntimeReflectionSystem.h")]
    public static class ScriptableRuntimeReflectionSystemSettings
    {
        public static IScriptableRuntimeReflectionSystem system
        {
            get { return Internal_ScriptableRuntimeReflectionSystemSettings_system; }
            set
            {
                if (value == null || value.Equals(null))
                {
                    Debug.LogError("'null' cannot be assigned to ScriptableRuntimeReflectionSystemSettings.system");
                    return;
                }
                // We always allow the BuiltinRuntimeReflectionSystem, it is set by Unity on domain reload
                // However, we issue a warning when multiple different IScriptableRuntimeReflectionSystem have been assigned.
                else if (!(system is BuiltinRuntimeReflectionSystem)
                         && !(value is BuiltinRuntimeReflectionSystem)
                         && system != value
                )
                    Debug.LogWarningFormat("ScriptableRuntimeReflectionSystemSettings.system is assigned more than once. Only a the last instance will be used. (Last instance {0}, New instance {1})", system, value);

                Internal_ScriptableRuntimeReflectionSystemSettings_system = value;
            }
        }

        static IScriptableRuntimeReflectionSystem Internal_ScriptableRuntimeReflectionSystemSettings_system
        {
            get { return s_Instance.implementation; }
            [RequiredByNativeCode]
            set
            {
                if (s_Instance.implementation != value)
                {
                    if (s_Instance.implementation != null)
                        s_Instance.implementation.Dispose();
                }
                s_Instance.implementation = value;
            }
        }

        static ScriptableRuntimeReflectionSystemWrapper s_Instance = new ScriptableRuntimeReflectionSystemWrapper();

        static ScriptableRuntimeReflectionSystemWrapper Internal_ScriptableRuntimeReflectionSystemSettings_instance
        {
            [RequiredByNativeCode]
            get { return s_Instance; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        [StaticAccessor("ScriptableRuntimeReflectionSystem", StaticAccessorType.DoubleColon)]
        static extern void ScriptingDirtyReflectionSystemInstance();
    }
}
