// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        static Dictionary<ApiCompatibilityLevel, string[]> cachedApiCompatibilityLevelReferences = new Dictionary<ApiCompatibilityLevel, string[]>();

        [RequiredByNativeCode]
        public static string[] GetSystemLibraryReferences(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            return GetCachedSystemLibraryReferences(apiCompatibilityLevel);
        }

        public static IEnumerable<string> GetEditorExtensionsReferences(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if(apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard)
            {
                return Directory.GetFiles(NetStandardFinder.GetNetStandardEditorExtensionsDirectory(), "*.dll");
            }

#pragma warning disable CS0618
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET)
#pragma warning restore CS0618
                throw new NotImplementedException("CORECLR_FIXME");

            return System.Array.Empty<string>();
        }


        static string[] FindReferencesInDirectories(this IEnumerable<string> references, string[] directories)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return (
#pragma warning restore UA2001
                from reference in references
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                from directory in directories
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                where File.Exists(Path.Combine(directory, reference))
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                select Path.Combine(directory, reference)
#pragma warning restore UA2001
            ).ToArray();
        }

        static string[] GetCachedSystemLibraryReferences(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            // We cache the references because they are computed by getting files in directories on disk,
            // which is very slow.
            if (cachedApiCompatibilityLevelReferences.TryGetValue(apiCompatibilityLevel, out var cachedReferences))
            {
                return cachedReferences;
            }

            var references = new List<string>();


            // CORECLR_FIXME : Once we want to allow compiling against .NET we need to add a separate if case for .NET
#pragma warning disable CS0618
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard || apiCompatibilityLevel == ApiCompatibilityLevel.NET)
#pragma warning restore CS0618
            {
                references.AddRange(GetNetStandardClassLibraries());
            }
            else if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8)
            {
                var monoAssemblyDirectories = GetSystemReferenceDirectories(apiCompatibilityLevel);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var referenceFileNames = GetSystemReferences().Concat(GetNet46SystemReferences()).Concat(GetMonoProfileNetstandardFacadeReferences()).Distinct();
#pragma warning restore UA2001
                references.AddRange(referenceFileNames.FindReferencesInDirectories(monoAssemblyDirectories));
                references.AddRange(Directory.GetFiles(Path.Combine(GetUnityReferenceProfileDirectory(), "Facades"), "*.dll"));
            }
            else
            {
                var monoAssemblyDirectories = GetSystemReferenceDirectories(apiCompatibilityLevel);
                references.AddRange(GetSystemReferences().FindReferencesInDirectories(monoAssemblyDirectories));
            }

            var apiCompatibilityLevelReference = references.ToArray();
            cachedApiCompatibilityLevelReferences[apiCompatibilityLevel] = apiCompatibilityLevelReference;

            return apiCompatibilityLevelReference;
        }

        static string GetSystemReference(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8)
                return GetUnityReferenceProfileDirectory();
#pragma warning disable CS0618
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET)
#pragma warning restore CS0618
                throw new NotImplementedException("CORECLR_FIXME");

            return MonoInstallationFinder.GetProfileDirectory(BuildPipeline.CompatibilityProfileToClassLibFolder(apiCompatibilityLevel), MonoInstallationFinder.MonoBleedingEdgeInstallation);
        }

        private static string GetUnityReferenceProfileDirectory()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "UnityReferenceAssemblies", "unity-4.8-api");
        }

        public static string[] GetSystemReferenceDirectories(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            // CORECLR_FIXME Temporarily treat NET_10 as netstandard.  We are not ready to start compiling against net10 directly
#pragma warning disable CS0618
            if (apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard || apiCompatibilityLevel == ApiCompatibilityLevel.NET)
#pragma warning restore CS0618
            {
                var systemReferenceDirectories = new List<string>();
                systemReferenceDirectories.Add(NetStandardFinder.GetReferenceDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetNetStandardCompatShimsDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetNetStandardExtensionsDirectory());
                systemReferenceDirectories.Add(NetStandardFinder.GetNetStandardEditorExtensionsDirectory());
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
