// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class ILPostProcessingProgram
    {
        public virtual string EnsureRunningAndGetSocketOrNamedPipe()
        {
            var name = EnsureRunningAndGetSocketOrNamedPipeImpl();
            if (string.IsNullOrEmpty(name))
            {
                UnityEngine.Debug.LogWarning("IL Post Processing agent failed to start. Any IL Post Processing task will fail");
            }
            return name;
        }

        [NativeHeader("Editor/Src/ScriptCompilation/ILPPExternalProcess.h")]
        [FreeFunction("ILPPExternalProcess::EnsureRunningAndGetSocketOrNamedPipe", IsThreadSafe = true)]
        private static extern string EnsureRunningAndGetSocketOrNamedPipeImpl();

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void KillLingeringILPPRunner()
        {
            var pidFilePath = Path.Combine("Library", "ilpp.pid");
            if (!File.Exists(pidFilePath))
            {
                return;
            }
            int processId;
            try
            {
                processId = int.Parse(File.ReadAllText(pidFilePath), CultureInfo.InvariantCulture);
            }
            catch(FormatException) {
                // corrupted pid file, return
                return;
            }
            try
            {
                var process = Process.GetProcessById(processId);
                if(process.ProcessName == "Unity.ILPP.Runner" || process.ProcessName == "Unity.ILPP.Runner.exe")
                {
                    UnityEngine.Debug.Log($"Found a lingering IL Post Processing runner process with PID {processId}. Killing it.");
                    process.Kill();
                }
            }
            catch (ArgumentException)
            {
                // no process with this ID
                return;
            }
            catch (InvalidOperationException)
            {
                // process exitted while the Process instance is manipulated
            }
        }
    }
}
