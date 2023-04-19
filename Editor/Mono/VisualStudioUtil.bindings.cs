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
            public readonly string[] WorkloadsAndComponents;

            internal VisualStudio(string devEnvPath, string edition, Version version, string[] workloadsAndComponents)
            {
                DevEnvPath = devEnvPath;
                Edition = edition;
                Version = version;
                WorkloadsAndComponents = workloadsAndComponents;
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
                        workloadsAndComponents: rawDevEnvPaths[i * 4 + 3].Split('|'));
                }
            }
        }

        public static string GetVSVersionYear(Version vsVersion)
        {
            switch (vsVersion.Major)
            {
                case 14:
                    return "2015";

                case 15:
                    return "2017";

                case 16:
                    return "2019";

                case 17:
                    return "2022";

                default:
                    return vsVersion.ToString();
            }
        }

        [FreeFunction("VisualStudioUtilities::FindVisualStudioDevEnvPaths")]
        [NativeConditional("PLATFORM_WIN")]
        internal extern static string[] FindVisualStudioDevEnvPaths(int visualStudioVersion, string[] requiredWorkloadsAndComponents);
    }
}
