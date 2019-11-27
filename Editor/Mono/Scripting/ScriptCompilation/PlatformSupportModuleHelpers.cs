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

            scriptAssembly.Defines = AddAdditionalToArray(scriptAssembly.Defines, compilationExtension.GetAdditionalDefines().ToArray());
            scriptAssembly.References = AddAdditionalToArray(scriptAssembly.References, compilationExtension.GetAdditionalAssemblyReferences()
                .Concat(compilationExtension.GetWindowsMetadataReferences()).ToArray());
            scriptAssembly.Files = AddAdditionalToArray(scriptAssembly.Files, compilationExtension.GetAdditionalSourceFiles().ToArray());
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
