// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Utils
{
    // Locates the .NET reference assemblies for opt-in net10.0 compilation. The core BCL ships inside the
    // editor's bundled .NET SDK as a reference pack (Microsoft.NETCore.App.Ref); the net10 BCL extensions
    // ship separately in the BCLExtensions targeting pack, mirroring how netstandard is split (see NetStandardFinder).
    static class NetCoreReferenceFinder
    {
        const string k_ReferencePackName = "Microsoft.NETCore.App.Ref";

        // net10.0 is the target modelled by ApiCompatibilityLevel.NET. Keep in sync with the defines emitted in
        // BuildTargetPlatformSpecific.cpp (kNET) and the BCLExtensions net10.0 targeting pack.
        const int k_ReferencePackMajorVersion = 10;

        [NoAutoStaticsCleanup] // The bundled SDK doesn't change while the editor runs, so the path is safe to persist across code reload.
        static string s_PackDirectory;

        // Returns the ref directory of the bundled net10.0 reference pack, e.g.
        // <scripting>/DotNetSdk/packs/Microsoft.NETCore.App.Ref/<version>/ref/net10.0
        public static string GetReferenceDirectory()
        {
            return Path.Combine(GetPackDirectory(), "ref", $"net{k_ReferencePackMajorVersion}.0");
        }

        // Returns the root of the bundled net10.0 reference pack, which also holds its source generators, e.g.
        // <scripting>/DotNetSdk/packs/Microsoft.NETCore.App.Ref/<version>
        public static string GetPackDirectory()
        {
            if (s_PackDirectory != null)
                return s_PackDirectory;

            var packsDirectory = Path.Combine(Scripting.NetCoreProgram.GetDotNetRuntimePath(), "packs", k_ReferencePackName);
            if (!Directory.Exists(packsDirectory))
                throw new DirectoryNotFoundException($"Could not find the .NET reference pack directory '{packsDirectory}'. It is required to compile for the latest .NET.");

            // The pack is versioned (e.g. 10.0.11) and that version rolls forward independently of the SDK, so resolve
            // it from disk rather than hard-coding it. Only consider net10 packs: the defines and BCLExtensions pack are
            // fixed to net10.0, so a higher-major pack (e.g. net11) must not be selected here. Pick the highest net10.
            var latestVersion = new Version(0, 0);
            string versionDirectory = null;
            foreach (var directory in Directory.GetDirectories(packsDirectory))
            {
                if (Version.TryParse(Path.GetFileName(directory), out var version)
                    && version.Major == k_ReferencePackMajorVersion
                    && version > latestVersion)
                {
                    latestVersion = version;
                    versionDirectory = directory;
                }
            }

            if (versionDirectory == null)
                throw new DirectoryNotFoundException($"The .NET reference pack directory '{packsDirectory}' contains no net{k_ReferencePackMajorVersion}.0 reference pack.");

            s_PackDirectory = versionDirectory;
            return versionDirectory;
        }
    }
}
