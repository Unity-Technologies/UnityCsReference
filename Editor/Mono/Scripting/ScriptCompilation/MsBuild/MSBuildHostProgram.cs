// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class MSBuildHostProgram
{
    public string EnsureRunningAndGetSocketOrNamedPipe()
    {
        var name = EnsureRunningAndGetSocketOrNamedPipeImpl();
        if (string.IsNullOrEmpty(name))
        {
            UnityEngine.Debug.LogError("MSBuild Host process failed to start.");
        }
        else
        {
            Directory.CreateDirectory(Path.Combine("Library", "MSBuild"));

            //This is used by tasks in msbuild to be able to talk to the host.
            File.WriteAllText(Path.Combine("Library", "MSBuild", "msbuild.host.txt"), name);
        }
        return name;
    }

    [NativeHeader("Editor/Src/ScriptCompilation/UnityBuildServiceHostProcess.h")]
    [FreeFunction("UnityBuildServiceHost::EnsureRunningAndGetSocketOrNamedPipe", IsThreadSafe = true)]
    private static extern string EnsureRunningAndGetSocketOrNamedPipeImpl();
}
