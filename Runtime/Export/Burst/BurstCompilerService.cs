// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Bindings;

namespace Unity.Burst.LowLevel
{
    internal static partial class BurstCompilerService
    {
        public delegate bool ExtractCompilerFlags(Type jobType, out string flags);

        public static void Initialize(string folderRuntime, ExtractCompilerFlags extractCompilerFlags)
        {
            if (folderRuntime == null) throw new ArgumentNullException(nameof(folderRuntime));
            if (extractCompilerFlags == null) throw new ArgumentNullException(nameof(extractCompilerFlags));

            if (!Directory.Exists(folderRuntime))
            {
                Debug.LogError($"Unable to initialize the burst JIT compiler. The folder `{folderRuntime}` does not exist");
                return;
            }

            var message = InitializeInternal(folderRuntime, extractCompilerFlags);

            if (!String.IsNullOrEmpty(message))
                Debug.LogError($"Unexpected error while trying to initialize the burst JIT compiler: {message}");
        }
    }
}
