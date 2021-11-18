// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal static class ClassLibraryInitializer
    {
        [RequiredByNativeCode]
        static void Init()
        {
            UnityLogWriter.Init();
        }

        [RequiredByNativeCode]
        static void InitStdErrWithHandle(IntPtr fileHandle)
        {
            var sfh = new SafeFileHandle(fileHandle, false);
            if (!sfh.IsInvalid)
            {
                var writer = new StreamWriter(new FileStream(sfh, FileAccess.Write)){ AutoFlush = true };
                Console.SetError(writer);
            }
        }
    }
}
