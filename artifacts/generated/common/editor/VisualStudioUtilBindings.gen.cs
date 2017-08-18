// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditorInternal
{


internal static partial class VisualStudioUtil
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] FindVisualStudioDevEnvPaths (int visualStudioVersion, string[] requiredWorkloads) ;

}


}
