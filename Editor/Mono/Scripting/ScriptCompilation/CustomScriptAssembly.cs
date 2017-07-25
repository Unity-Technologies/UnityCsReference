// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [System.Serializable]
    class CustomScriptAssemblyData
    {
#pragma warning disable 649
        public string name;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;

        public static CustomScriptAssemblyData FromJson(string json)
        {
            var assemblyData = UnityEngine.JsonUtility.FromJson<CustomScriptAssemblyData>(json);

            if (assemblyData == null)
                throw new System.Exception("Json file does not contain an assembly definition");

            if (string.IsNullOrEmpty(assemblyData.name))
                throw new System.Exception("Required property 'name' not set");

            if ((assemblyData.excludePlatforms != null && assemblyData.excludePlatforms.Length > 0) &&
                (assemblyData.includePlatforms != null && assemblyData.includePlatforms.Length > 0))
                throw new System.Exception("Both 'excludePlatforms' and 'includePlatforms' are set.");

            return assemblyData;
        }
    }

    struct CustomScriptAssemblyPlatform
    {
        public string Name { get; set; }
        public BuildTarget BuildTarget { get; set; }

        public CustomScriptAssemblyPlatform(string name, BuildTarget buildTarget) : this()
        {
            Name = name;
            BuildTarget = buildTarget;
        }
    }

    class CustomScriptAssembly
    {
        public string FilePath { get; set; }
        public string PathPrefix { get; set; }
        public string Name { get; set; }
        public string[] References { get; set; }
        public CustomScriptAssemblyPlatform[] IncludePlatforms { get; set;  }
        public CustomScriptAssemblyPlatform[] ExcludePlatforms { get; set;  }

        public AssemblyFlags AssemblyFlags
        {
            get
            {
                if (IncludePlatforms != null && IncludePlatforms.Length == 1 && IncludePlatforms[0].BuildTarget == BuildTarget.NoTarget)
                    return AssemblyFlags.EditorOnly;

                return AssemblyFlags.None;
            }
        }

        static CustomScriptAssemblyPlatform[] Platforms { get; set; }

        static CustomScriptAssembly()
        {
            Platforms = new CustomScriptAssemblyPlatform[23];

            int i = 0;
            Platforms[i++] = new CustomScriptAssemblyPlatform("Editor", BuildTarget.NoTarget);
            Platforms[i++] = new CustomScriptAssemblyPlatform("OSXtandalone32", BuildTarget.StandaloneOSXIntel);
            Platforms[i++] = new CustomScriptAssemblyPlatform("OSXStandalone64", BuildTarget.StandaloneOSXIntel64);
            Platforms[i++] = new CustomScriptAssemblyPlatform("OSXStandaloneUniversal", BuildTarget.StandaloneOSXUniversal);
            Platforms[i++] = new CustomScriptAssemblyPlatform("WindowsStandalone32", BuildTarget.StandaloneWindows);
            Platforms[i++] = new CustomScriptAssemblyPlatform("WindowsStandalone64", BuildTarget.StandaloneWindows64);
            Platforms[i++] = new CustomScriptAssemblyPlatform("LinuxStandalone32", BuildTarget.StandaloneLinux);
            Platforms[i++] = new CustomScriptAssemblyPlatform("LinuxStandalone64", BuildTarget.StandaloneLinux64);
            Platforms[i++] = new CustomScriptAssemblyPlatform("LinuxStandaloneUniversal", BuildTarget.StandaloneLinuxUniversal);
            Platforms[i++] = new CustomScriptAssemblyPlatform("iOS", BuildTarget.iOS);
            Platforms[i++] = new CustomScriptAssemblyPlatform("Android", BuildTarget.Android);
            Platforms[i++] = new CustomScriptAssemblyPlatform("WebGL", BuildTarget.WebGL);
            Platforms[i++] = new CustomScriptAssemblyPlatform("WSA", BuildTarget.WSAPlayer);
            Platforms[i++] = new CustomScriptAssemblyPlatform("Tizen", BuildTarget.Tizen);
            Platforms[i++] = new CustomScriptAssemblyPlatform("PSVita", BuildTarget.PSP2);
            Platforms[i++] = new CustomScriptAssemblyPlatform("PS4", BuildTarget.PS4);
            Platforms[i++] = new CustomScriptAssemblyPlatform("PSMobile", BuildTarget.PSM);
            Platforms[i++] = new CustomScriptAssemblyPlatform("XboxOne", BuildTarget.XboxOne);
            Platforms[i++] = new CustomScriptAssemblyPlatform("Nintendo3DS", BuildTarget.N3DS);
            Platforms[i++] = new CustomScriptAssemblyPlatform("WiiU", BuildTarget.WiiU);
            Platforms[i++] = new CustomScriptAssemblyPlatform("tvOS", BuildTarget.tvOS);
            Platforms[i++] = new CustomScriptAssemblyPlatform("SamsungTV", BuildTarget.SamsungTV);
            Platforms[i++] = new CustomScriptAssemblyPlatform("Switch", BuildTarget.Switch);

            System.Diagnostics.Debug.Assert(Platforms.Length == i - 1);
        }

        public bool IsCompatibleWithEditor()
        {
            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != BuildTarget.NoTarget);

            if (IncludePlatforms != null)
                return IncludePlatforms.Any(p => p.BuildTarget == BuildTarget.NoTarget);

            return true;
        }

        public bool IsCompatibleWith(BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            // Compatible with editor and all platforms.
            if (IncludePlatforms == null && ExcludePlatforms == null)
                return true;

            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            if (buildingForEditor)
                return IsCompatibleWithEditor();

            if (buildingForEditor)
                buildTarget = BuildTarget.NoTarget; // Editor

            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != buildTarget);

            return IncludePlatforms.Any(p => p.BuildTarget == buildTarget);
        }

        public static CustomScriptAssembly Create(string name, string directory)
        {
            var customScriptAssembly = new CustomScriptAssembly();

            var modifiedDirectory = AssetPath.ReplaceSeparators(directory);

            if (modifiedDirectory.Last() != AssetPath.Separator)
                modifiedDirectory += AssetPath.Separator;

            customScriptAssembly.Name = name;
            customScriptAssembly.FilePath = modifiedDirectory;
            customScriptAssembly.PathPrefix = modifiedDirectory;
            customScriptAssembly.References = new string[0];

            return customScriptAssembly;
        }

        public static CustomScriptAssembly FromCustomScriptAssemblyData(string path, CustomScriptAssemblyData customScriptAssemblyData)
        {
            if (customScriptAssemblyData == null)
                return null;

            var pathPrefix = path.Substring(0, path.Length - AssetPath.GetFileName(path).Length);

            var customScriptAssembly = new CustomScriptAssembly();

            customScriptAssembly.Name = customScriptAssemblyData.name;
            customScriptAssembly.References = customScriptAssemblyData.references;
            customScriptAssembly.FilePath = path;
            customScriptAssembly.PathPrefix = pathPrefix;

            if (customScriptAssemblyData.includePlatforms != null && customScriptAssemblyData.includePlatforms.Length > 0)
                customScriptAssembly.IncludePlatforms = customScriptAssemblyData.includePlatforms.Select(name => GetPlatformFromName(name)).ToArray();

            if (customScriptAssemblyData.excludePlatforms != null && customScriptAssemblyData.excludePlatforms.Length > 0)
                customScriptAssembly.ExcludePlatforms = customScriptAssemblyData.excludePlatforms.Select(name => GetPlatformFromName(name)).ToArray();

            return customScriptAssembly;
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromName(string name)
        {
            foreach (var platform in Platforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return platform;

            var platformNames = Platforms.Select(p => string.Format("'{0}'", p.Name)).ToArray();

            var platformsString = string.Join(",", platformNames);

            throw new System.ArgumentException(string.Format("Platform name '{0}' not supported. Supported platform names: {1}", name, platformsString));
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromBuildTarget(BuildTarget buildTarget)
        {
            foreach (var platform in Platforms)
                if (platform.BuildTarget == buildTarget)
                    return platform;

            throw new System.ArgumentException(string.Format("No CustomScriptAssemblyPlatform setup for BuildTarget '{0}'", buildTarget));
        }
    }
}
