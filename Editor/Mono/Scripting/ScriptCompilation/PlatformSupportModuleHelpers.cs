// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Modules;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class PlatformSupportModuleHelpers
    {
        public static void AddAdditionalPlatformSupportData(ICompilationExtension compilationExtension, ref ScriptAssembly scriptAssembly)
        {
            if (compilationExtension == null)
            {
                return;
            }

            scriptAssembly.Defines = AddAdditionalToArray(scriptAssembly.Defines, compilationExtension.GetAdditionalDefines());
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            scriptAssembly.References = AddAdditionalToArray(scriptAssembly.References, compilationExtension.GetAdditionalAssemblyReferences()
#pragma warning restore UA2001
                .Concat(compilationExtension.GetWindowsMetadataReferences()).ToArray());
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            scriptAssembly.Files = AddAdditionalToArray(scriptAssembly.Files, compilationExtension.GetAdditionalSourceFiles().ToArray());
#pragma warning restore UA2001
        }

        private static string[] AddAdditionalToArray(string[] source, string[] extras)
        {
            if (extras == null)
            {
                return source;
            }
            var destinationArray = new string[source.Length + extras.Length];
            Array.Copy(source, destinationArray, source.Length);
            Array.Copy(extras, 0, destinationArray, source.Length, extras.Length);
            return destinationArray;
        }
    }
}
