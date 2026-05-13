// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor.Modules;
using Unity.Collections;
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
            // Keep in sync with AsmDefParser.s_RenamedReferences in
            // Editor/Tools/AsmDefToCSProj/AsmDefToCSProjLib/AssemblyDefinition.cs (used by the MSBuild pipeline).
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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    data.references = data.references.Concat(additionalReferences).ToArray();
#pragma warning restore UA2001
            }
        }

        [Serializable]
        private class CustomScriptAssemblyWithLegacyData : CustomScriptAssemblyData
        {
            public string[] optionalUnityReferences;
            public string Tooltip { get; private set; }

            public void UpdateLegacyData()
            {
                if (optionalUnityReferences?.Length > 0)
                {
                    autoReferenced = false;
                    overrideReferences = true;

                    references = references ?? Array.Empty<string>();
                    precompiledReferences = precompiledReferences ?? Array.Empty<string>();
                    defineConstraints = defineConstraints ?? Array.Empty<string>();

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
        public bool HasSubTarget { get; private set; }
        public int SubTarget { get; private set; }

        public CustomScriptAssemblyPlatform(string name, string displayName, BuildTarget buildTarget, bool hasSubTarget = false, int subTarget = 0) : this()
        {
            Name = name;
            DisplayName = displayName;
            BuildTarget = buildTarget;
            HasSubTarget = hasSubTarget;
            SubTarget = subTarget;
        }

        public CustomScriptAssemblyPlatform(string name, BuildTarget buildTarget, bool hasSubTarget = false, int subTarget = 0) : this(name, name, buildTarget, hasSubTarget, subTarget) {}
    }

    [DebuggerDisplay("{Name}")]
    class CustomScriptAssembly
    {
        static readonly bool k_CompilerWarningsForImmutablePackages = Environment.GetEnvironmentVariable("UNITY_INTERNAL_NOSUPPRESSWARNINGS") == "1";

        // Whitelisted packages that should not have analyzer rules applied
        // All package names should have "com.unity." prefix
        static readonly string[] k_WhitelistedPackages = new string[]
        {
            "ide.rider", // UAC0005/6/7/20
            "ide.visualstudio", // UAC0005/6/7 in Testing folder for test runner integration
            "addressables", // UAC0023
            "memoryprofiler", // UAC0006
            "purchasing", // UAC0005
            "cinemachine", // UAC0005
            "learn.iet-framework", // UAC0005
            "polyspatial", // UAC0005/7
            "web.stripping-tool", // UAC0005/7
            "multiplayer.tools", // UAC0005
            "formats.alembic", // UAC0005
            "mars", // to be deprecated
            "barracuda", // to be deprecated
            "xrtools.utils", // to be deprecated
            "test-framework", // UAC0007
            "netcode", // UAC0005
            "scriptablebuildpipeline", // UAC0023
            "package-validation-suite", // UAC0005/7
            "mobile.android-logcat", // UAC0005
            "searcher", // CS0618: obsolete API usage
            "xr.legacyinputhelpers", // CS0618: obsolete FindObjectsOfType
            "inputsystem", // CS0109: unnecessary new keyword
            "ads", // CS0168: unused variable
        };

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

        public string[] ResponseFileDefines { get; set; }

        public bool NoEngineReferences { get; set; }

        private AssemblyFlags assemblyFlags = AssemblyFlags.None;

        public bool IsPredefined { get; set; }

        private static string s_immutablePackageRulesetPath;

        private bool IsInWhitelistedPackage()
        {
            var pathSpan = PathPrefix.AsSpan();
            foreach (var packageName in k_WhitelistedPackages)
            {
                var packagePath = $"com.unity.{packageName}";
                if (pathSpan.Contains(packagePath.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool IsUnityPackage()
        {
            var pathSpan = PathPrefix.AsSpan();
            return pathSpan.Contains("com.unity.".AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetImmutablePackageRulesetPath()
        {
            // When UNITY_INTERNAL_NOSUPPRESSWARNINGS=1, use default analyzer behavior (all warnings enabled)
            // Otherwise, suppress all analyzer warnings except UAC0005/6/7/20/23 rules
            var includeAllLine = k_CompilerWarningsForImmutablePackages ? "" : @"  <IncludeAll Action=""None"" />";

            // Explicitly suppress UAC warnings (not intentional errors like UAC1003)
            // Only applies when UNITY_INTERNAL_NOSUPPRESSWARNINGS is not set
            var suppressedRules = k_CompilerWarningsForImmutablePackages ? "" : @"
    <Rule Id=""UAC1001"" Action=""None"" />
    <Rule Id=""UAC1002"" Action=""None"" />
    <Rule Id=""UAC1004"" Action=""None"" />
    <Rule Id=""UAC1008"" Action=""None"" />
    <Rule Id=""UAC1009"" Action=""None"" />
    <Rule Id=""UAC1010"" Action=""None"" />
    <Rule Id=""UAC1011"" Action=""None"" />";

            var rulesetContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Immutable Package Rules"" Description=""Errors for Forbidden API rules"" ToolsVersion=""16.0"">
{includeAllLine}
  <Rules AnalyzerId=""Unity.Analyzers"" RuleNamespace=""Unity.Analyzers"">
    <Rule Id=""UAC0005"" Action=""Error"" />
    <Rule Id=""UAC0006"" Action=""Error"" />
    <Rule Id=""UAC0007"" Action=""Error"" />
    <Rule Id=""UAC0020"" Action=""Error"" />
    <Rule Id=""UAC0023"" Action=""Error"" />{suppressedRules}
  </Rules>
</RuleSet>";

            var projectPath = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var rulesetDirectory = System.IO.Path.Combine(projectPath, "Library");
            var rulesetPath = System.IO.Path.Combine(rulesetDirectory, "ImmutablePackage.ruleset");

            System.IO.Directory.CreateDirectory(rulesetDirectory);
            System.IO.File.WriteAllText(rulesetPath, rulesetContent);

            if (s_immutablePackageRulesetPath == null)
                s_immutablePackageRulesetPath = rulesetPath;

            return s_immutablePackageRulesetPath;
        }

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
                bool imported = AssetDatabase.TryGetAssetFolderInfo(PathPrefix, out rootFolder, out immutable);

                if (imported && immutable)
                {
                    if (IsUnityPackage() && !IsInWhitelistedPackage())
                    {
                        // Non-whitelisted Unity packages: Apply analyzer rules as errors
                        CompilerOptions.RoslynAnalyzerRulesetPath = GetImmutablePackageRulesetPath();
                    }
                    else if (!k_CompilerWarningsForImmutablePackages)
                    {
                        assemblyFlags |= AssemblyFlags.SuppressCompilerWarnings;
                    }
                }
                // User code uses the default Warning severity from the analyzer

                return assemblyFlags;
            }
        }

        public static List<CustomScriptAssemblyPlatform> Platforms { get; private set; }
        public static CustomScriptAssemblyPlatform[] DeprecatedPlatforms { get; private set; }
        public static string[] RenamedPlatforms { get; private set; }

        static CustomScriptAssembly()
        {
            // When removing a platform from Platforms, please add it to DeprecatedPlatforms.
            DiscoveredTargetInfo[] buildTargetList = BuildTargetDiscovery.GetBuildTargetInfoList();

            // Need extra slots in array for Editor target and subtarget variants which are not included in the build target list
            const int numEditorTargets = 1;

            Platforms = new List<CustomScriptAssemblyPlatform>(buildTargetList.Length + numEditorTargets)
            {
                new ("Editor", BuildTarget.NoTarget)
            };

            for (int i = 0; i < buildTargetList.Length; i++)
            {
                // normal case
                Platforms.Add(
                    new CustomScriptAssemblyPlatform(
                    BuildTargetDiscovery.GetScriptAssemblyName(buildTargetList[i]),
                    buildTargetList[i].niceName,
                    buildTargetList[i].buildTargetPlatformVal)
                );

                var extensionModule =
                    ModuleManager.FindPlatformSupportModule(
                        ModuleManager.GetTargetStringFromBuildTarget(buildTargetList[i].buildTargetPlatformVal));

                // if this build target has extra custom assembly targets, append those
                if(extensionModule != null)
                {
                    var extraScriptAssemblyPlatforms = extensionModule.GetExtraScriptAssemblyPlatforms(buildTargetList[i].buildTargetPlatformVal);
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (extraScriptAssemblyPlatforms != null && extraScriptAssemblyPlatforms.Any())
#pragma warning restore UA2002
                    {
                        foreach(var extraPlatform in extraScriptAssemblyPlatforms)
                        {
                            Platforms.Add(new CustomScriptAssemblyPlatform(
                                BuildTargetDiscovery.GetScriptAssemblyName(buildTargetList[i]) + extraPlatform.AssemblyNamePostfix,
                                extraPlatform.TargetNiceName,
                                    buildTargetList[i].buildTargetPlatformVal,
                                    true,
                                    extraPlatform.Subtarget));
                        }
                    }
                }
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
                new CustomScriptAssemblyPlatform("Lumin", BuildTarget.Lumin),
                new CustomScriptAssemblyPlatform("Stadia", BuildTarget.Stadia),
            };
            RenamedPlatforms = new string[]
            {
                "Bratwurst",
            };
#pragma warning restore 0618
        }

        public bool IsCompatibleWithEditor()
        {
            if (ExcludePlatforms != null)
                return Array.TrueForAll(ExcludePlatforms, p => p.BuildTarget != BuildTarget.NoTarget);

            if (IncludePlatforms != null)
                return Array.Exists(IncludePlatforms, p => p.BuildTarget == BuildTarget.NoTarget);

            return true;
        }

        public bool IsCompatibleWith(BuildTarget buildTarget, int subTarget, EditorScriptCompilationOptions options, EditorBuildRules.SymbolDefinitionContext symbolDefinitionContext)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            var isBuildingWithTestAssemblies = (options & EditorScriptCompilationOptions.BuildingIncludingTestAssemblies) == EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
            var isTestAssembly = DefineConstraints != null && DefineConstraints.Contains("UNITY_INCLUDE_TESTS");
            var isTestFrameworkAssembly = DefineConstraints != null && Array.Exists(DefineConstraints, x => x == "UNITY_TESTS_FRAMEWORK");
            if (!buildingForEditor && (isTestAssembly || isTestFrameworkAssembly) && !isBuildingWithTestAssemblies)
            {
                return false;
            }

            if (symbolDefinitionContext.IsEmpty())
            {
                throw new ArgumentException("Defines cannot be empty", nameof(symbolDefinitionContext));
            }

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
            symbolDefinitionContext.SetResponseFileDefines(ResponseFileDefines);
            if (!DefineConstraintsHelper.IsDefineConstraintsCompatibleContext(symbolDefinitionContext, DefineConstraints))
            {
                return false;
            }

            if (isTestAssembly && AssetPathMetaData != null && !AssetPathMetaData.IsTestable)
            {
                return false;
            }

            // Compatible with editor and all platforms.
            if (IncludePlatforms == null && ExcludePlatforms == null)
            {
                return true;
            }

            if (buildingForEditor)
            {
                return IsCompatibleWithEditor();
            }

            if (ExcludePlatforms != null)
            {
                // build target is different
                // OR build target matches, but subtarget for target assembly is both present and differs
                return Array.TrueForAll(ExcludePlatforms, p =>
                    p.BuildTarget != buildTarget ||
                    (p.BuildTarget == buildTarget &&
                    (p.HasSubTarget && p.SubTarget != subTarget)));
            }

            // build target matches, and if present, subtarget matches
            return Array.Exists(IncludePlatforms, p => p.BuildTarget == buildTarget && (!p.HasSubTarget || p.SubTarget == subTarget));
        }

        public static CustomScriptAssembly Create(string name, string directory)
        {
            var customScriptAssembly = new CustomScriptAssembly();

            var modifiedDirectory = AssetPath.ReplaceSeparators(directory);

            if (modifiedDirectory[^1] != AssetPath.Separator)
                modifiedDirectory += AssetPath.Separator.ToString();

            customScriptAssembly.Name = name;
            customScriptAssembly.RootNamespace = name ?? string.Empty;
            customScriptAssembly.FilePath = modifiedDirectory;
            customScriptAssembly.PathPrefix = modifiedDirectory;
            customScriptAssembly.References = Array.Empty<string>();
            customScriptAssembly.PrecompiledReferences = Array.Empty<string>();
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
            customScriptAssembly.PrecompiledReferences = customScriptAssemblyData.precompiledReferences ?? Array.Empty<string>();
            customScriptAssembly.DefineConstraints = customScriptAssemblyData.defineConstraints ?? Array.Empty<string>();
            customScriptAssembly.VersionDefines = (customScriptAssemblyData.versionDefines ?? Array.Empty<VersionDefine>());

            if (customScriptAssemblyData.includePlatforms != null && customScriptAssemblyData.includePlatforms.Length > 0)
                customScriptAssembly.IncludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.includePlatforms);

            if (customScriptAssemblyData.excludePlatforms != null && customScriptAssemblyData.excludePlatforms.Length > 0)
                customScriptAssembly.ExcludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.excludePlatforms);

            var compilerOptions = new ScriptCompilerOptions
            {
                AllowUnsafeCode = customScriptAssemblyData.allowUnsafeCode
            };
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

            foreach(var platformName in RenamedPlatforms)
                if (string.Equals(platformName, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromName(string name)
        {
            foreach (var platform in Platforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return platform;

            var platformNames = Platforms.ConvertAll(p => string.Format("\"{0}\"", p.Name));
            platformNames.Sort();

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomScriptAssembly) obj);
        }

        public bool Equals(CustomScriptAssembly other)
        {
            return assemblyFlags == other.assemblyFlags
                   && string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
                   && string.Equals(PathPrefix, other.PathPrefix, StringComparison.Ordinal)
                   && string.Equals(Name, other.Name, StringComparison.Ordinal)
                   && string.Equals(RootNamespace, other.RootNamespace, StringComparison.Ordinal)
                   && GUID == other.GUID
                   && Equals(References, other.References)
                   && Equals(AdditionalPrefixes, other.AdditionalPrefixes)
                   && Equals(IncludePlatforms, other.IncludePlatforms)
                   && Equals(ExcludePlatforms, other.ExcludePlatforms)
                   && Equals(AssetPathMetaData, other.AssetPathMetaData)
                   && Equals(CompilerOptions, other.CompilerOptions)
                   && OverrideReferences == other.OverrideReferences
                   && Equals(PrecompiledReferences, other.PrecompiledReferences)
                   && AutoReferenced == other.AutoReferenced
                   && Equals(DefineConstraints, other.DefineConstraints)
                   && Equals(VersionDefines, other.VersionDefines)
                   && Equals(ResponseFileDefines, other.ResponseFileDefines)
                   && NoEngineReferences == other.NoEngineReferences
                   && IsPredefined == other.IsPredefined;
        }

        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }
    }
}
