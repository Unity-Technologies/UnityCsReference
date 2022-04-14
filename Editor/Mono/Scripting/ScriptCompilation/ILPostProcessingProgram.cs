// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class ILPostProcessingProgram
    {
        public virtual string NamedPipeOrUnixSocket
        {
            get
            {
                var name = EnsureRunningAndGetSocketOrNamedPipe();
                if (string.IsNullOrEmpty(name))
                {
                    UnityEngine.Debug.LogWarning("IL Post Processing agent failed to start. Any IL Post Processing task will fail");
                }
                return name;
            }
        }

        [NativeHeader("Editor/Src/ScriptCompilation/ILPPExternalProcess.h")]
        [FreeFunction("ILPPExternalProcess::EnsureRunningAndGetSocketOrNamedPipe", IsThreadSafe = true)]
        private static extern string EnsureRunningAndGetSocketOrNamedPipe();
    }
}
