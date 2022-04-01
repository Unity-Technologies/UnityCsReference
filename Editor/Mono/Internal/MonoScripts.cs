// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    /// <summary>
    /// Helper factory for <see cref="UnityEditor.MonoScript"/> instances.
    /// </summary>
    public static class MonoScripts
    {
        public static MonoScript CreateMonoScript(string scriptContents, string className, string nameSpace, string assemblyName, bool isEditorScript)
        {
            var script = new MonoScript();
            if (!string.IsNullOrEmpty(scriptContents))
            {
                Debug.LogWarning($"MonoScript {className} was initialized with a non-empty script. This has never worked and should not be attempted. The script contents will be ignored");
            }
            script.Init(className, nameSpace, assemblyName, isEditorScript);
            return script;
        }
    }
}
