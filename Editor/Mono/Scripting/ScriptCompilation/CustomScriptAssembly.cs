// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditorInternal;
using DiscoveredTargetInfo = UnityEditor.BuildTargetDiscovery.DiscoveredTargetInfo;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // https://docs.microsoft.com/en-us/cpp/windows/changing-a-symbol-or-symbol-name-id
    class SymbolNameRestrictions
    {
        private const int k_MaxLength = 247;

        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Length > k_MaxLength)
            {
                return false;
            }

            // Invalid if the first character is a number.
            if (char.IsNumber(name[0]))
            {
                return false;
            }

            foreach (var chr in name)
            {
                // Skip if it's a letter.
                if (char.IsLetter(chr))
                    continue;

                // Skip if it's a number.
                if (char.IsNumber(chr))
                    continue;

                // Skip if it's an underscore.
                if (chr == '_')
                    continue;

                // Invalid for unsupported characters.
                return false;
            }

            return true;
        }
    }

#pragma warning disable 649
    [Serializable]
    class VersionDefine
    {
        public string name;
        public string expression;
        public string define;
    }

    [System.Serializable]
    class CustomScriptAssemblyData
    {
        public string name;
        public string rootNamespace;
        public string[] references;

        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public VersionDefine[] versionDefines;
        public bool noEngineReferences;

        static Dictionary<string, string> renamedReferences = new Dictionary<string, string>(StringComparer.Ordinal);

        static CustomScriptAssemblyData()
        {
            renamedReferences["Unity.RenderPipelines.Lightweight.Editor"] = "Unity.RenderPipelines.Universal.Editor";
            renamedReferences["Unity.RenderPipelines.Lightweight.Runtime"] = "Unity.RenderPipelines.Universal.Runtime";
        }

        public static CustomScriptAssemblyData FromJson(string json)
        {
            var assemblyData = FromJsonNoFieldValidation(json);

            assemblyData.ValidateFields();

            return assemblyData;
        }

        public static CustomScriptAssemblyData FromJsonNoFieldValidation(string json)
        {
            var assemblyData = new CustomScriptAssemblyWithLegacyData();
            assemblyData.autoReferenced = true;
            UnityEngine.JsonUtility.FromJsonOverwrite(json, assemblyData);

            UpdateRenamedReferences(assemblyData);
            assemblyData.UpdateLegacyData();

            if (assemblyData == null)
                throw new System.Exception("Json file does not contain an assembly definition");

            return assemblyData;
        }

        public void ValidateFields()
        {
            if (string.IsNullOrEmpty(name))
                throw new System.Exception("Required property 'name' not set");

            if ((excludePlatforms != null && excludePlatforms.Length > 0) &&
                (includePlatforms != null && includePlatforms.Length > 0))
                throw new System.Exception("Both 'excludePlatforms' and 'includePlatforms' are set.");

            if (autoReferenced && UnityCodeGenHelpers.IsCodeGen(name))
            {
                throw new Exception($"Assembly '{name}' is a CodeGen assembly and cannot be Auto Referenced");
            }
        }

        public static string ToJson(CustomScriptAssemblyData data)
        {
            return UnityEngine.JsonUtility.ToJson(data, true);
        }

        static void UpdateRenamedReferences(CustomScriptAssemblyData data)
        {
            if (data.references == null || data.references.Length == 0)
                return;

            HashSet<string> additionalReferences = null;

            for (int i = 0; i < data.references.Length; ++i)
            {
                var reference = data.references[i];
                string newReference;

                if (!renamedReferences.TryGetValue(reference, out newReference))
                    continue;

                if (additionalReferences == null)
                    additionalReferences = new HashSet<string>();

                additionalReferences.Add(newReference);
            }

            if (additionalReferences != null && additionalReferences.Count > 0)
            {
                for (int i = 0; i < data.references.Length; ++i)
                {
                    var reference = data.references[i];

                    if (additionalReferences.Contains(reference))
                        additionalReferences.Remove(reference);
                }

                if (additionalReferences.Count > 0)
                    data.references = data.references.Concat(additionalReferences).ToArray();
            }
        }

        [Serializable]
        private class CustomScriptAssemblyWithLegacyData : CustomScriptAssemblyData
        {
            public string[] optionalUnityReferences;
            public string Tooltip { get; private set; }

            public void UpdateLegacyData()
            {
                if (optionalUnityReferences != null && optionalUnityReferences.Any())
                {
                    autoReferenced = false;
                    overrideReferences = true;

                    references = references ?? new string[0];
                    precompiledReferences = precompiledReferences ?? new string[0];
                    defineConstraints = defineConstraints ?? new string[0];

                    AddTo(ref references, "UnityEngine.TestRunner", "UnityEditor.TestRunner");
                    AddTo(ref precompiledReferences, "nunit.framework.dll");
                    AddTo(ref defineConstraints, "UNITY_INCLUDE_TESTS");
                }
            }

            private void AddTo(ref string[] array, params string[] additionalValues)
            {
                var z = new string[array.Length + additionalValues.Length];
                array.CopyTo(z, 0);
                additionalValues.CopyTo(z, array.Length);
                array = z;
            }
        }
    }

    struct CustomScriptAssemblyPlatform
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public BuildTarget BuildTarget { get; private set; }

        public CustomScriptAssemblyPlatform(string name, string displayName, BuildTarget buildTarget) : this()
        {
            Name = name;
            DisplayName = displayName;
            BuildTarget = buildTarget;
        }

        public CustomScriptAssemblyPlatform(string name, BuildTarget buildTarget) : this(name, name, buildTarget) {}
    }

    [DebuggerDisplay("{Name}")]
    class CustomScriptAssembly
    {
        public string FilePath { get; set; }
        public string PathPrefix { get; set; }
        public string Name { get; set; }
        public string RootNamespace { get; set; }
        public string GUID { get; set; }
        public string[] References { get; set; }
        public string[] AdditionalPrefixes { get; set; }
        public CustomScriptAssemblyPlatform[] IncludePlatforms { get; set; }
        public CustomScriptAssemblyPlatform[] ExcludePlatforms { get; set; }

        public AssetPathMetaData AssetPathMetaData { get; set; }
        public ScriptCompilerOptions CompilerOptions { get; set; } = new ScriptCompilerOptions();

        public bool OverrideReferences { get; set; }
        public string[] PrecompiledReferences { get; set; }
        public bool AutoReferenced { get; set; }
        public string[] DefineConstraints { get; set; }
        public VersionDefine[] VersionDefines { get; set; }
        public bool NoEngineReferences { get; set; }

        private AssemblyFlags assemblyFlags = AssemblyFlags.None;

        public AssemblyFlags AssemblyFlags
        {
            get
            {
                if (assemblyFlags != AssemblyFlags.None)
                    return assemblyFlags;

                assemblyFlags = AssemblyFlags.UserAssembly;

                if (IncludePlatforms != null && IncludePlatforms.Length == 1 && IncludePlatforms[0].BuildTarget == BuildTarget.NoTarget)
                    assemblyFlags |= AssemblyFlags.EditorOnly;

                if (OverrideReferences)
                {
                    assemblyFlags |= AssemblyFlags.ExplicitReferences;
                }

                if (!AutoReferenced)
                {
                    assemblyFlags |= AssemblyFlags.ExplicitlyReferenced;
                }

                if (NoEngineReferences)
                {
                    assemblyFlags |= AssemblyFlags.NoEngineReferences;
                }

                bool rootFolder, immutable;
                bool imported = AssetDatabase.GetAssetFolderInfo(PathPrefix, out rootFolder, out immutable);

                // Do not emit warnings for immutable (package) folders,
                // as the user cannot do anything to fix them.
                if (imported && immutable)
                {
                    assemblyFlags |= AssemblyFlags.SuppressCompilerWarnings;
                }

                return assemblyFlags;
            }
        }

        public static CustomScriptAssemblyPlatform[] Platforms { get; private set; }
        public static CustomScriptAssemblyPlatform[] DeprecatedPlatforms { get; private set; }

        static CustomScriptAssembly()
        {
            // When removing a platform from Platforms, please add it to DeprecatedPlatforms.
            DiscoveredTargetInfo[] buildTargetList = BuildTargetDiscovery.GetBuildTargetInfoList();

            // Need extra slot for Editor which is not included in the build target list
            Platforms = new CustomScriptAssemblyPlatform[buildTargetList.Length + 1];
            Platforms[0] = new CustomScriptAssemblyPlatform("Editor", BuildTarget.NoTarget);
            for (int i = 1; i < Platforms.Length; i++)
            {
                Platforms[i] = new CustomScriptAssemblyPlatform(
                    BuildTargetDiscovery.GetScriptAssemblyName(buildTargetList[i - 1]),
                    buildTargetList[i - 1].niceName,
                    buildTargetList[i - 1].buildTargetPlatformVal);
            }

#pragma warning disable 0618
            DeprecatedPlatforms = new CustomScriptAssemblyPlatform[]
            {
                new CustomScriptAssemblyPlatform("PSMobile", BuildTarget.PSM),
                new CustomScriptAssemblyPlatform("Tizen", BuildTarget.Tizen),
                new CustomScriptAssemblyPlatform("WiiU", BuildTarget.WiiU),
                new CustomScriptAssemblyPlatform("Nintendo3DS", BuildTarget.N3DS),
                new CustomScriptAssemblyPlatform("PSVita", BuildTarget.PSP2),
                new CustomScriptAssemblyPlatform("LinuxStandalone32", BuildTarget.StandaloneLinux),
                new CustomScriptAssemblyPlatform("LinuxStandaloneUniversal", BuildTarget.StandaloneLinuxUniversal),
            };
#pragma warning restore 0618
        }

        public bool IsCompatibleWithEditor()
        {
            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != BuildTarget.NoTarget);

            if (IncludePlatforms != null)
                return IncludePlatforms.Any(p => p.BuildTarget == BuildTarget.NoTarget);

            return true;
        }

        public bool IsCompatibleWith(BuildTarget buildTarget, EditorScriptCompilationOptions options, string[] defines)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            var isBuildingWithTestAssemblies = (options & EditorScriptCompilationOptions.BuildingIncludingTestAssemblies) == EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
            var isTestAssembly = DefineConstraints != null && DefineConstraints.Any(x => x == "UNITY_INCLUDE_TESTS");
            if (!buildingForEditor && isTestAssembly && !isBuildingWithTestAssemblies)
            {
                return false;
            }

            if (defines != null && defines.Length == 0)
                throw new ArgumentException("Defines cannot be empty", "defines");

            // Log invalid define constraints
            if (DefineConstraints != null)
            {
                for (var i = 0; i < DefineConstraints.Length; ++i)
                {
                    if (!DefineConstraintsHelper.IsDefineConstraintValid(DefineConstraints[i]))
                    {
                        throw new AssemblyDefinitionException($"Invalid Define Constraint: \"{DefineConstraints[i]}\" at line {(i+1).ToString()}", FilePath);
                    }
                }
            }

            if (!DefineConstraintsHelper.IsDefineConstraintsCompatible(defines, DefineConstraints))
            {
                return false;
            }

            if (isTestAssembly && AssetPathMetaData != null && !AssetPathMetaData.IsTestable)
            {
                return false;
            }

            // Compatible with editor and all platforms.
            if (IncludePlatforms == null && ExcludePlatforms == null)
                return true;

            if (buildingForEditor)
                return IsCompatibleWithEditor();

            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != buildTarget);

            return IncludePlatforms.Any(p => p.BuildTarget == buildTarget);
        }

        public static CustomScriptAssembly Create(string name, string directory)
        {
            var customScriptAssembly = new CustomScriptAssembly();

            var modifiedDirectory = AssetPath.ReplaceSeparators(directory);

            if (modifiedDirectory.Last() != AssetPath.Separator)
                modifiedDirectory += AssetPath.Separator.ToString();

            customScriptAssembly.Name = name;
            customScriptAssembly.RootNamespace = name ?? string.Empty;
            customScriptAssembly.FilePath = modifiedDirectory;
            customScriptAssembly.PathPrefix = modifiedDirectory;
            customScriptAssembly.References = new string[0];
            customScriptAssembly.PrecompiledReferences = new string[0];
            customScriptAssembly.CompilerOptions = new ScriptCompilerOptions();
            customScriptAssembly.AutoReferenced = true;

            return customScriptAssembly;
        }

        public static CustomScriptAssembly FromCustomScriptAssemblyData(string path, string guid, CustomScriptAssemblyData customScriptAssemblyData)
        {
            if (customScriptAssemblyData == null)
                return null;

            var pathPrefix = path.Substring(0, path.Length - AssetPath.GetFileName(path).Length);

            var customScriptAssembly = new CustomScriptAssembly();

            customScriptAssembly.Name = customScriptAssemblyData.name;
            customScriptAssembly.RootNamespace = customScriptAssemblyData.rootNamespace;
            customScriptAssembly.GUID = guid;
            customScriptAssembly.References = customScriptAssemblyData.references;
            customScriptAssembly.FilePath = path;
            customScriptAssembly.PathPrefix = pathPrefix;
            customScriptAssembly.AutoReferenced = customScriptAssemblyData.autoReferenced;
            customScriptAssembly.OverrideReferences = customScriptAssemblyData.overrideReferences;
            customScriptAssembly.NoEngineReferences = customScriptAssemblyData.noEngineReferences;
            customScriptAssembly.PrecompiledReferences = customScriptAssemblyData.precompiledReferences ?? new string[0];
            customScriptAssembly.DefineConstraints = customScriptAssemblyData.defineConstraints ?? new string[0];
            customScriptAssembly.VersionDefines = (customScriptAssemblyData.versionDefines ?? new VersionDefine[0]);

            if (customScriptAssemblyData.includePlatforms != null && customScriptAssemblyData.includePlatforms.Length > 0)
                customScriptAssembly.IncludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.includePlatforms);

            if (customScriptAssemblyData.excludePlatforms != null && customScriptAssemblyData.excludePlatforms.Length > 0)
                customScriptAssembly.ExcludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.excludePlatforms);

            var compilerOptions = new ScriptCompilerOptions();

            compilerOptions.AllowUnsafeCode = customScriptAssemblyData.allowUnsafeCode;

            customScriptAssembly.CompilerOptions = compilerOptions;

            return customScriptAssembly;
        }

        public static CustomScriptAssemblyPlatform[] GetPlatformsFromNames(string[] names)
        {
            var platforms = new List<CustomScriptAssemblyPlatform>();

            foreach (var name in names)
            {
                // Ignore deprecated platforms.
                if (IsDeprecatedPlatformName(name))
                    continue;

                platforms.Add(GetPlatformFromName(name));
            }

            return platforms.ToArray();
        }

        public static bool IsDeprecatedPlatformName(string name)
        {
            foreach (var platform in DeprecatedPlatforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromName(string name)
        {
            foreach (var platform in Platforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return platform;

            var platformNames = Platforms.Select(p => string.Format("\"{0}\"", p.Name)).ToArray();
            System.Array.Sort(platformNames);

            var platformsString = string.Join(",\n", platformNames);

            throw new System.ArgumentException(string.Format("Platform name '{0}' not supported.\nSupported platform names:\n{1}\n", name, platformsString));
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
