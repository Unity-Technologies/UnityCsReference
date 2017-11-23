// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    //*undocumented*
    [NativeType(Header = "Editor/Platform/Windows/VisualStudioUtilities.h")]
    internal static class VisualStudioUtil
    {
        public class VisualStudio
        {
            public readonly string DevEnvPath;
            public readonly string Edition;
            public readonly Version Version;
            public readonly string[] Workloads;

            internal VisualStudio(string devEnvPath, string edition, Version version, string[] workloads)
            {
                DevEnvPath = devEnvPath;
                Edition = edition;
                Version = version;
                Workloads = workloads;
            }
        }

        public static IEnumerable<VisualStudio> ParseRawDevEnvPaths(string[] rawDevEnvPaths)
        {
            if (rawDevEnvPaths != null)
            {
                for (int i = 0; i < rawDevEnvPaths.Length / 4; i++)
                {
                    yield return new VisualStudio(
                        devEnvPath: rawDevEnvPaths[i * 4],
                        edition: rawDevEnvPaths[i * 4 + 1],
                        version: new Version(rawDevEnvPaths[i * 4 + 2]),
                        workloads: rawDevEnvPaths[i * 4 + 3].Split('|'));
                }
            }
        }

        [FreeFunction("VisualStudioUtilities::FindVisualStudioDevEnvPaths")]
        [NativeConditional("UNITY_WIN")]
        internal extern static string[] FindVisualStudioDevEnvPaths(int visualStudioVersion, string[] requiredWorkloads);
    }
}
