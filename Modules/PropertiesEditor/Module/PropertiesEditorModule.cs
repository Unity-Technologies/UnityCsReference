// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties.Editor
{
    [UnityEditor.InitializeOnLoad]
    static class PropertiesEditorModule
    {
        static PropertiesEditorModule()
        {
            UnityEditor.Build.BuildDefines.getScriptCompilationDefinesDelegates += AddPropertiesBuildDefines;
        }

        static void AddPropertiesBuildDefines(UnityEditor.BuildTarget target,
            System.Collections.Generic.HashSet<string> defines)
        {
            defines.Add("USE_PROPERTIES_MODULE");
        }
    }
}
