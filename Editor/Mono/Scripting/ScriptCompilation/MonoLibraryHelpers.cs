// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine.Scripting;

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

        [RequiredByNativeCode]
        public static string[] GetSystemLibraryReferences(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            return GetCachedSystemLibraryReferences(apiCompatibilityLevel);
        }


        static string[] FindReferencesInDirectories(this IEnumerable<string> references, string[] directories)
        {
            return (
                from reference in references
                from directory in directories
                where File.Exists(Path.Combine(directory, reference))
                select Path.Combine(directory, reference)
            ).ToArray();
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
            var monoAssemblyDirectories = GetSystemReferenceDirectories(apiCompatibilityLevel);

            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard)
            {
                references.AddRange(GetNetStandardClassLibraries());
            }
            else if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8)
            {
                var referenceFileNames = GetSystemReferences().Concat(GetNet46SystemReferences()).Concat(GetMonoProfileNetstandardFacadeReferences()).Distinct();
                references.AddRange(referenceFileNames.FindReferencesInDirectories(monoAssemblyDirectories));
                references.AddRange(Directory.GetFiles(Path.Combine(GetUnityReferenceProfileDirectory(), "Facades"), "*.dll"));
            }
            else
            {
                references.AddRange(GetSystemReferences().FindReferencesInDirectories(monoAssemblyDirectories));
            }

            cachedReferences = new CachedReferences
            {
                ApiCompatibilityLevel = apiCompatibilityLevel,
                References = references.ToArray()
            };


            return cachedReferences.References;
        }

        static string GetSystemReference(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8)
                return GetUnityReferenceProfileDirectory();
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_2_0)
                return MonoInstallationFinder.GetProfileDirectory("2.0-api", MonoInstallationFinder.MonoBleedingEdgeInstallation);

            return MonoInstallationFinder.GetProfileDirectory(BuildPipeline.CompatibilityProfileToClassLibFolder(apiCompatibilityLevel), MonoInstallationFinder.MonoBleedingEdgeInstallation);
        }

        private static string GetUnityReferenceProfileDirectory()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "UnityReferenceAssemblies", "unity-4.8-api");
        }

        public static string[] GetSystemReferenceDirectories(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard)
            {
                var systemReferenceDirectories = new List<string>();
                systemReferenceDirectories.Add(NetStandardFinder.GetReferenceDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetNetStandardCompatShimsDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetNetStandardExtensionsDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetDotNetFrameworkCompatShimsDirectory());
                return systemReferenceDirectories.ToArray();
            }

            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8)
            {
                var systemReferenceDirectories = new List<string>();
                var frameworkDirectory = GetSystemReference(apiCompatibilityLevel);
                systemReferenceDirectories.Add(frameworkDirectory);
                systemReferenceDirectories.Add(Path.Combine(frameworkDirectory, "Facades"));
                return systemReferenceDirectories.ToArray();
            }

            return new[] { GetSystemReference(apiCompatibilityLevel) };
        }

        static string[] GetNetStandardClassLibraries()
        {
            var classLibraries = new List<string>();

            // Add the .NET Standard 2.0 reference assembly
            classLibraries.Add(Path.Combine(NetStandardFinder.GetReferenceDirectory(), "netstandard.dll"));

            // Add the .NET Standard 2.0 compat shims
            classLibraries.AddRange(Directory.GetFiles(NetStandardFinder.GetNetStandardCompatShimsDirectory(), "*.dll"));

            // Add the .NET Standard 2.0 extensions
            classLibraries.AddRange(Directory.GetFiles(NetStandardFinder.GetNetStandardExtensionsDirectory(), "*.dll"));

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

        static string[] GetNet46SystemReferences()
        {
            return new[]
            {
                "System.Numerics.dll",
                "System.Numerics.Vectors.dll",
                "System.Net.Http.dll",
                "System.IO.Compression.dll",
                "Microsoft.CSharp.dll",
                "System.Data.dll",
            };
        }

        static string[] GetMonoProfileNetstandardFacadeReferences()
        {
            return new[]
            {
                "mscorlib.dll",
                "System.Core.dll",
                "System.dll",
                "System.Data.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Drawing.dll",
                "System.IO.Compression.dll",
                "System.IO.Compression.FileSystem.dll",
                "System.ComponentModel.Composition.dll",
                "System.Net.Http.dll",
                "System.Numerics.dll",
                "System.Runtime.Serialization.dll",
                "System.Transactions.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
            };
        }
    }
}
