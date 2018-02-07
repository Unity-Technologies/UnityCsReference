// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class MonoLibraryHelpers
    {
        class CachedReferences
        {
            public ApiCompatibilityLevel ApiCompatibilityLevel;
            public string[] References;
        }

        static CachedReferences cachedReferences;

        public static string[] GetSystemLibraryReferences(ApiCompatibilityLevel apiCompatibilityLevel, BuildTarget buildTarget, SupportedLanguage supportedLanguage,
            bool buildingForEditor, string assemblyName)
        {
            if (WSAHelpers.BuildingForDotNet(buildTarget, buildingForEditor, assemblyName))
                return new string[0];

            // The language may not be compatible with these additional references
            if (supportedLanguage != null && !supportedLanguage.CompilerRequiresAdditionalReferences())
                return new string[0];

            return GetCachedSystemLibraryReferences(apiCompatibilityLevel);
        }

        static string[] GetCachedSystemLibraryReferences(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            // We cache the references because they are computed by getting files in directories on disk,
            // which is very slow.
            if (cachedReferences != null && cachedReferences.ApiCompatibilityLevel == apiCompatibilityLevel)
            {
                return cachedReferences.References;
            }

            var references = new List<string>();
            var monoAssemblyDirectory = GetSystemReferenceDirectory(apiCompatibilityLevel);

            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                references.AddRange(GetNetStandardClassLibraries());
            }
            else if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6)
            {
                references.AddRange(GetSystemReferences().Select(dll => Path.Combine(monoAssemblyDirectory, dll)));

                // Look in the mono assembly directory for a facade folder and get a list of all the DLL's to be
                // used later by the language compilers.
                references.AddRange(Directory.GetFiles(Path.Combine(monoAssemblyDirectory, "Facades"), "*.dll"));

                references.AddRange(GetBooAndUsReferences().Select(dll => Path.Combine(MonoInstallationFinder.GetProfileDirectory("unityscript", MonoInstallationFinder.MonoBleedingEdgeInstallation), dll)));
            }
            else
            {
                references.AddRange(GetSystemReferences().Select(dll => Path.Combine(monoAssemblyDirectory, dll)));
                references.AddRange(GetBooAndUsReferences().Select(dll => Path.Combine(monoAssemblyDirectory, dll)));
            }

            cachedReferences = new CachedReferences
            {
                ApiCompatibilityLevel = apiCompatibilityLevel,
                References = references.ToArray()
            };


            return cachedReferences.References;
        }

        public static string GetSystemReferenceDirectory(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0)
                return "";
            else if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6)
                return MonoInstallationFinder.GetProfileDirectory("4.7.1-api", MonoInstallationFinder.MonoBleedingEdgeInstallation);
            else if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_2_0)
                return MonoInstallationFinder.GetProfileDirectory("2.0-api", MonoInstallationFinder.MonoBleedingEdgeInstallation);

            return MonoInstallationFinder.GetProfileDirectory(BuildPipeline.CompatibilityProfileToClassLibFolder(apiCompatibilityLevel), MonoInstallationFinder.MonoBleedingEdgeInstallation);
        }

        static string[] GetNetStandardClassLibraries()
        {
            var classLibraries = new List<string>();

            // Add the .NET Standard 2.0 reference assembly
            classLibraries.Add(Path.Combine(NetStandardFinder.GetReferenceDirectory(), "netstandard.dll"));

            // Add the .NET Standard 2.0 compat shims
            classLibraries.AddRange(Directory.GetFiles(NetStandardFinder.GetNetStandardCompatShimsDirectory(), "*.dll"));

            // Add the .NET Framework compat shims
            classLibraries.AddRange(Directory.GetFiles(NetStandardFinder.GetDotNetFrameworkCompatShimsDirectory(), "*.dll"));

            return classLibraries.ToArray();
        }

        static string[] GetSystemReferences()
        {
            return new[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "System.Runtime.Serialization.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
            };
        }

        static string[] GetBooAndUsReferences()
        {
            return new[]
            {
                "UnityScript.dll",
                "UnityScript.Lang.dll",
                "Boo.Lang.dll",
            };
        }
    }
}
