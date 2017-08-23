// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;

// Note: this file is externally included included in some tools - SerializationWeaver, AssemblyUpdater, InternalCallReplacer etc.

namespace UnityEditor.Scripting.Compilers
{
    internal sealed class NuGetPackageResolver
    {
        public string PackagesDirectory
        {
            get;
            set;
        }

        public string ProjectLockFile
        {
            get;
            set;
        }

        public string TargetMoniker
        {
            get;
            set;
        }

        public string[] ResolvedReferences
        {
            get;
            private set;
        }

        public NuGetPackageResolver()
        {
            TargetMoniker = "UAP,Version=v10.0";
        }

        private string ConvertToWindowsPath(string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public string[] Resolve()
        {
            var text = File.ReadAllText(ProjectLockFile);
            var lockFile = (Dictionary<string, object>)Json.Deserialize(text);
            var targets = (Dictionary<string, object>)lockFile["targets"];
            var target = FindUWPTarget(targets);

            var references = new List<string>();
            var packagesPath = ConvertToWindowsPath(GetPackagesPath());

            foreach (var packagePair in target)
            {
                var package = (Dictionary<string, object>)packagePair.Value;

                object compileObject;
                if (!package.TryGetValue("compile", out compileObject))
                    continue;
                var compile = (Dictionary<string, object>)compileObject;

                var parts = packagePair.Key.Split('/');
                var packageId = parts[0];
                var packageVersion = parts[1];
                var packagePath = Path.Combine(Path.Combine(packagesPath, packageId), packageVersion);
                if (!Directory.Exists(packagePath))
                    throw new Exception(string.Format("Package directory not found: \"{0}\".", packagePath));

                foreach (var name in compile.Keys)
                {
                    const string emptyFolder = "_._";
                    if (string.Equals(Path.GetFileName(name), emptyFolder, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    var reference = Path.Combine(packagePath, ConvertToWindowsPath(name));
                    if (!File.Exists(reference))
                        throw new Exception(string.Format("Reference not found: \"{0}\".", reference));
                    references.Add(reference);
                }

                if (package.ContainsKey("frameworkAssemblies"))
                    throw new NotImplementedException("Support for \"frameworkAssemblies\" property has not been implemented yet.");
            }

            ResolvedReferences = references.ToArray();
            return ResolvedReferences;
        }

        private Dictionary<string, object> FindUWPTarget(Dictionary<string, object> targets)
        {
            foreach (var target in targets)
            {
                if (target.Key.StartsWith(TargetMoniker) && !target.Key.Contains("/"))
                    return (Dictionary<string, object>)target.Value;
            }

            throw new InvalidOperationException("Could not find suitable target for " + TargetMoniker + " in project.lock.json file.");
        }

        private string GetPackagesPath()
        {
            var value = PackagesDirectory;
            if (!string.IsNullOrEmpty(value))
                return value;
            value = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(value))
                return value;
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            return Path.Combine(Path.Combine(userProfile, ".nuget"), "packages");
        }
    }
}
