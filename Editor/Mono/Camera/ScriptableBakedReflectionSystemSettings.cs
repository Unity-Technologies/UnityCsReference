// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Rendering
{
    [RequiredByNativeCode]
    public static class ScriptableBakedReflectionSystemSettings
    {
        public static IScriptableBakedReflectionSystem system
        {
            get { return Internal_ScriptableBakedReflectionSystemSettings_system; }
            set { Internal_ScriptableBakedReflectionSystemSettings_system = value; }
        }
        static IScriptableBakedReflectionSystem Internal_ScriptableBakedReflectionSystemSettings_system
        {
            [RequiredByNativeCode] get { return s_Instance?.implementation; }
            [RequiredByNativeCode] set
            {
                if (s_Instance == null)
                {
                    Console.WriteLine("[WARNING] ScriptableBakedReflectionSystemWrapper instance must be initialized from CPP before InitializeOnLoad");
                    return;
                }

                if (value == null || value.Equals(null))
                {
                    Debug.LogError("'null' cannot be assigned to ScriptableBakedReflectionSystemSettings.system");
                    return;
                }

                // We always allow the BuiltinBakedReflectionSystem, it is set by Unity on domain reload
                // However, we issue a warning when multiple different IScriptableBakedReflectionSystem have been assigned.
                if (!(system is BuiltinBakedReflectionSystem)
                    && !(value is BuiltinBakedReflectionSystem)
                    && system != value)
                    Debug.LogWarningFormat("ScriptableBakedReflectionSystemSettings.system is assigned more than once. Only a the last instance will be used. (Last instance {0}, New instance {1})", system, value);

                if (s_Instance.implementation != value)
                {
                    if (s_Instance.implementation != null)
                        s_Instance.implementation.Dispose();
                    s_Instance.implementation = value;
                }
            }
        }

        static ScriptableBakedReflectionSystemWrapper s_Instance = null;

        [UsedImplicitly, RequiredByNativeCode]
        static ScriptableBakedReflectionSystemWrapper Internal_ScriptableBakedReflectionSystemSettings_InitializeWrapper(IntPtr ptr)
        {
            s_Instance = new ScriptableBakedReflectionSystemWrapper(ptr);
            return s_Instance;
        }
    }
}
