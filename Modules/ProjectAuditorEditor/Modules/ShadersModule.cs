// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum ShaderProperty
    {
        Size = 0,
        MaxVariants,
        NumBuiltVariants,
        NumPasses,
        NumKeywords,
        NumProperties,
        NumTextureProperties,
        RenderQueue,
        Instancing,
        SrpBatcher,
        AlwaysIncluded,
        Num
    }

    enum ShaderVariantProperty
    {
        Compiled = 0,
        Platform,
        Tier,
        Stage,
        PassType,
        PassName,
        Keywords,
        PlatformKeywords,
        Requirements,
        Num
    }

    enum ComputeShaderVariantProperty
    {
        Platform = 0,
        Tier,
        Kernel,
        KernelThreadCount,
        Keywords,
        PlatformKeywords,
        Num
    }

    enum ShaderMessageProperty
    {
        ShaderName = 0,
        Platform,
        Num
    }

    enum ParseLogResult
    {
        Success,
        NoCompiledVariants,
        ReadError
    }

    class ShaderVariantData
    {
        public PassType PassType;
        public string PassName;
        public ShaderType ShaderType;
        public string[] Keywords;
        public string[] PlatformKeywords;
        public ShaderRequirements[] Requirements;
        public GraphicsTier GraphicsTier;
        public BuildTarget BuildTarget;
        public ShaderCompilerPlatform CompilerPlatform;
    }

    class ComputeShaderVariantData
    {
        public string KernelName;
        public string KernelThreadCount;
        public string[] Keywords;
        public string[] PlatformKeywords;
        public GraphicsTier GraphicsTier;
        public BuildTarget BuildTarget;
        public ShaderCompilerPlatform CompilerPlatform;
    }

    class CompiledVariantData
    {
        public string Pass;
        public string Stage;
        public string[] Keywords;
    }

    class ShadersModule : ModuleWithAnalyzers<ShaderModuleAnalyzer>
        , IPreprocessShaders
        , IPreprocessComputeShaders
    {
        internal static readonly IssueLayout k_ShaderIssueLayout = new IssueLayout
        {
            Category = IssueCategory.AssetIssue,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Format = PropertyFormat.String, Name = "Areas", LongName = "Impacted Areas" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            Category = IssueCategory.Shader,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Shader Name"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size of the variants in the build" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.MaxVariants), Format = PropertyFormat.ULong, Name = "Max Variants", LongName = "Number of potential shader variants for a single stage (e.g. fragment), per shader platform (e.g. GLES30)" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.NumBuiltVariants), Format = PropertyFormat.Integer, Name = "Built Fragment Variants", LongName = "Number of fragment shader variants in the build for a single stage (e.g. fragment), per shader platform (e.g. GLES30)" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.NumPasses), Format = PropertyFormat.Integer, Name = "Num Passes", LongName = "Number of Passes" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.NumKeywords), Format = PropertyFormat.Integer, Name = "Num Keywords", LongName = "Number of Keywords" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.NumProperties), Format = PropertyFormat.Integer, Name = "Num Properties", LongName = "Number of Properties" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.NumTextureProperties), Format = PropertyFormat.Integer, Name = "Num Tex Properties", LongName = "Number of Texture Properties" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.RenderQueue), Format = PropertyFormat.Integer, Name = "Render Queue" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.Instancing), Format = PropertyFormat.Bool, Name = "Instancing", LongName = "GPU Instancing Support" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.SrpBatcher), Format = PropertyFormat.Bool, Name = "SRP Batcher", LongName = "SRP Batcher Compatible" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderProperty.AlwaysIncluded), Format = PropertyFormat.Bool, Name = "Always Included", LongName = "Always Included in Build" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        static readonly IssueLayout k_ShaderVariantLayout = new IssueLayout
        {
            Category = IssueCategory.ShaderVariant,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Shader Name", IsDefaultGroup = true },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Compiled), Format = PropertyFormat.Bool,
                    Name = "Compiled", LongName = "Compiled at runtime by the player"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Platform), Format = PropertyFormat.String,
                    Name = "Graphics API"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Tier), Format = PropertyFormat.String,
                    Name = "Tier"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Stage), Format = PropertyFormat.String,
                    Name = "Stage"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PassType), Format = PropertyFormat.String,
                    Name = "Pass Type"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PassName), Format = PropertyFormat.String,
                    Name = "Pass Name"
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Keywords), Format = PropertyFormat.String,
                    Name = "Keywords",
                    MaxAutoWidth = 500
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PlatformKeywords),
                    Format = PropertyFormat.String, Name = "Platform Keywords",
                    MaxAutoWidth = 500
                },
                new PropertyDefinition
                {
                    Type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Requirements),
                    Format = PropertyFormat.String, Name = "Requirements"
                }
            }
        };

        static readonly IssueLayout k_ComputeShaderVariantLayout = new IssueLayout
        {
            Category = IssueCategory.ComputeShaderVariant,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Shader Name", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Platform), Format = PropertyFormat.String, Name = "Graphics API" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Tier), Format = PropertyFormat.String, Name = "Tier" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Kernel), Format = PropertyFormat.String, Name = "Kernel" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.KernelThreadCount), Format = PropertyFormat.Integer, Name = "Kernel Thread Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Keywords), Format = PropertyFormat.String, Name = "Keywords", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.PlatformKeywords), Format = PropertyFormat.String, Name = "Platform Keywords", MaxAutoWidth = 500 },
            }
        };

        static readonly IssueLayout k_ShaderCompilerMessageLayout = new IssueLayout
        {
            Category = IssueCategory.ShaderCompilerMessage,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Message", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderMessageProperty.ShaderName), Format = PropertyFormat.String, Name = "Shader Name", IsDefaultGroup = true},
                //new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ShaderMessageProperty.Platform), Format = PropertyFormat.String, Name = "Platform"},
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 },
            }
        };

        // k_NoPassNames and k_NoKeywords must be consistent with values assigned in SubProgram::Compile()
        internal static readonly string[] k_NoPassNames = new[] { "unnamed", "<unnamed>"}; // 2019.x uses: <unnamed>, whilst 2020.x uses unnamed
        internal static readonly Dictionary<string, string> k_StageNameMap = new Dictionary<string, string>()
        {
            { "all", "vertex" },       // GLES* / OpenGLCore
            { "pixel", "fragment" }    // Metal
        };
        internal const string k_NoKeywords = "<no keywords>";
        internal const string k_UnnamedPassPrefix = "Pass ";
        internal const string k_NoRuntimeData = "This feature requires runtime data.";
        internal const string k_NotAvailable = "This feature requires a build.";
        internal const string k_Unknown = "Unknown";
        internal const string k_ComputeShaderMayHaveBadVariants = "Compute shader may have bad (but unused) variants preventing this from being evaluated.";

        static Dictionary<Shader, List<ShaderVariantData>> s_ShaderVariantData =
            new Dictionary<Shader, List<ShaderVariantData>>();
        static Dictionary<ComputeShader, List<ComputeShaderVariantData>> s_ComputeShaderVariantData =
            new Dictionary<ComputeShader, List<ComputeShaderVariantData>>();

        public override string Name => "Shaders";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_ShaderIssueLayout,
            k_ShaderLayout,
            k_ShaderVariantLayout,
            k_ComputeShaderVariantLayout,
            k_ShaderCompilerMessageLayout
        };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            var shaderPathMap = CollectShaders(context);
            ProcessShaders(analysisParams, shaderPathMap);

            ProcessComputeShaders(analysisParams);

            // clear collected variants before next build
            ClearBuildData();

            return AnalysisResult.Success;
        }

        Dictionary<Shader, string> CollectShaders(AnalysisContext context)
        {
            var shaderPathMap = new Dictionary<Shader, string>();
            var assetPaths = GetAssetPathsByFilter("t:shader", context);
            foreach (var assetPath in assetPaths)
            {
                // skip editor shaders
                if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) != -1)
                    continue;
                if (assetPath.IndexOf("/editor default resources/", StringComparison.OrdinalIgnoreCase) != -1)
                    continue;

                // vfx shaders are not currently supported
                if (Path.HasExtension(assetPath) && Path.GetExtension(assetPath).Equals(".vfx"))
                    continue;

                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;
                if (shader == null)
                {
                    Debug.LogError(assetPath + " is not a Shader.");
                    continue;
                }

                shaderPathMap.Add(shader, assetPath);
            }

            var builtShaderPaths = GetBuiltShaderPaths();

            foreach (var builtShader in builtShaderPaths)
            {
                if (!shaderPathMap.ContainsKey(builtShader.Key))
                {
                    shaderPathMap.Add(builtShader.Key, builtShader.Value);
                }
            }

            return shaderPathMap;
        }

        static Dictionary<Shader, string> GetBuiltShaderPaths()
        {
            // note this will find hidden shaders too
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return s_ShaderVariantData.Select(variant => variant.Key)
                .Where(shader => shader != null) // skip shader if it's been removed since the last build
                .ToDictionary(s => s, AssetDatabase.GetAssetPath);
#pragma warning restore RS0030
        }

        static HashSet<Shader> GetAlwaysIncludedShaders()
        {
            var alwaysIncludedShaders = new HashSet<Shader>();
            var graphicsSettings = Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings");
            var graphicsSettingsSerializedObject = new SerializedObject(graphicsSettings);
            var alwaysIncludedShadersSerializedProperty =
                graphicsSettingsSerializedObject.FindProperty("m_AlwaysIncludedShaders");

            for (var i = 0; i < alwaysIncludedShadersSerializedProperty.arraySize; i++)
            {
                var shader = (Shader)alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(i)
                    .objectReferenceValue;

                // sanity check, maybe the shader was removed/deleted
                if (shader == null)
                    continue;

                if (!alwaysIncludedShaders.Contains(shader))
                {
                    alwaysIncludedShaders.Add(shader);
                }
            }

            return alwaysIncludedShaders;
        }

        void ProcessShaders(AnalysisParams analysisParams, Dictionary<Shader, string> shaderPathMap)
        {
            var platform = analysisParams.Platform;
            var alwaysIncludedShaders = GetAlwaysIncludedShaders();
            var buildReportInfoAvailable = false;

            var packetAssetInfos = new PackedAssetInfo[0];
            var buildReport = BuildReportModule.BuildReportProvider.GetBuildReport(platform);
            if (buildReport != null)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                packetAssetInfos = buildReport.packedAssets.SelectMany(packedAsset => packedAsset.contents)
#pragma warning restore RS0030
                    .Where(c => c.type == typeof(UnityEngine.Shader)).ToArray();
            }

            buildReportInfoAvailable = packetAssetInfos.Length > 0;

            var sortedShaders = new List<Shader>(shaderPathMap.Keys);
            sortedShaders.Sort((s1, s2) => string.Compare(s1.name, s2.name));

            var analyzers = GetCompatibleAnalyzers(analysisParams);
            foreach (var shader in sortedShaders)
            {
                var assetPath = shaderPathMap[shader];
                var assetSize = buildReportInfoAvailable ? k_Unknown : k_NotAvailable;

                if (!assetPath.Equals("Resources/unity_builtin_extra"))
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var builtAssets = packetAssetInfos.Where(p => p.sourceAssetPath.Equals(assetPath)).ToArray();
#pragma warning restore RS0030
                    if (builtAssets.Length > 0)
                    {
                        assetSize = builtAssets[0].packedSize.ToString();
                    }
                    else if (!s_ShaderVariantData.ContainsKey(shader))
                    {
                        // if not processed, it was not built into either player data or AssetBundles.
                        assetSize = "0";
                    }
                }

                var shaderAnalysisContext = new ShaderAnalysisContext()
                {
                    AssetPath = assetPath,
                    Shader = shader,
                    Params = analysisParams
                };

                analysisParams.OnIncomingIssues(ProcessShader(shaderAnalysisContext, assetSize, alwaysIncludedShaders.Contains(shader)));
                analysisParams.OnIncomingIssues(ProcessVariants(shaderAnalysisContext));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(shaderAnalysisContext));
                }
            }
        }

        void ProcessComputeShaders(AnalysisParams analysisParams)
        {
            var context = new AnalysisContext()
            {
                Params = analysisParams
            };
            var issues = new List<ReportItem>();

            foreach (var shaderCompilerData in s_ComputeShaderVariantData)
            {
                var computeShaderName = shaderCompilerData.Key.name;
                foreach (var shaderVariantData in shaderCompilerData.Value)
                {
                    if (shaderVariantData.BuildTarget != BuildTarget.NoTarget && shaderVariantData.BuildTarget != analysisParams.Platform)
                        continue;

                    issues.Add(context.CreateInsight(k_ComputeShaderVariantLayout.Category, computeShaderName)
                        .WithCustomProperties(
                        [
                            shaderVariantData.CompilerPlatform,
                            shaderVariantData.GraphicsTier,
                            shaderVariantData.KernelName,
                            shaderVariantData.KernelThreadCount,
                            CombineKeywords(shaderVariantData.Keywords),
                            CombineKeywords(shaderVariantData.PlatformKeywords)
                        ]));
                }
            }
            if (issues.Count > 0)
                analysisParams.OnIncomingIssues(issues);
        }

        IEnumerable<ReportItem> ProcessShader(ShaderAnalysisContext context, string assetSize, bool isAlwaysIncluded)
        {
            // set initial state (-1: info not available)
            var variantCountPerCompilerPlatform = s_ShaderVariantData.Count > 0 ? 0 : -1;

            // add variants first
            if (s_ShaderVariantData.ContainsKey(context.Shader))
            {
                var variants = s_ShaderVariantData[context.Shader];
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var numCompilerPlatforms = variants.Select(v => v.CompilerPlatform).Distinct().Count();
                variantCountPerCompilerPlatform = variants.Count(v => ShaderTypeIsFragment(v.ShaderType, v.CompilerPlatform)) / numCompilerPlatforms;
#pragma warning restore RS0030
            }

            var shaderName = context.Shader.name;
            var shaderHasError = false;
            var severity = Severity.None;

            var shaderMessages = ShaderUtil.GetShaderMessages(context.Shader);
            foreach (var shaderMessage in shaderMessages)
            {
                var message = shaderMessage.message;
                if (message.EndsWith("\n"))
                    message = message.Substring(0, message.Length - 2);
                yield return context.CreateInsight(IssueCategory.ShaderCompilerMessage, message)
                    .WithCustomProperties(
                    [
                        shaderName,
                        shaderMessage.platform
                    ])
                    .WithLocation(context.AssetPath, shaderMessage.line)
                    .WithSeverity(shaderMessage.severity == ShaderCompilerMessageSeverity.Error
                        ? Severity.Error
                        : Severity.Warning);
            }

            shaderHasError = ShaderUtil.ShaderHasError(context.Shader);

            if (shaderHasError)
                severity = Severity.Error;
            else if (shaderMessages.Length > 0)
                severity = Severity.Warning;

            if (shaderHasError)
            {
                yield return context.CreateInsight(IssueCategory.Shader, Path.GetFileNameWithoutExtension(context.AssetPath))
                    .WithCustomProperties((int)ShaderProperty.Num, k_NotAvailable)
                    .WithLocation(context.AssetPath)
                    .WithSeverity(severity);
            }
            else
            {
/*
                var usedBySceneOnly = false;
                if (m_GetShaderVariantCountMethod != null)
                {
                    var value = (ulong)m_GetShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
                    variantCount = value.ToString();
                }
*/
                var passCount = context.Shader.passCount;
                var globalKeywords = ShaderUtilProxy.GetShaderGlobalKeywords(context.Shader);
                var localKeywords = ShaderUtilProxy.GetShaderLocalKeywords(context.Shader);
                var hasInstancing = ShaderUtilProxy.HasInstancing(context.Shader);
                var subShaderIndex = ShaderUtilProxy.GetShaderActiveSubshaderIndex(context.Shader);
                var isSrpBatcherCompatible = ShaderUtilProxy.GetSRPBatcherCompatibilityCode(context.Shader, subShaderIndex) == 0;
                var propertyCount = ShaderUtilProxy.GetPropertyCount(context.Shader);
                var texturePropertyCount = ShaderUtilProxy.GetTexturePropertyCount(context.Shader);

                yield return context.CreateInsight(IssueCategory.Shader, shaderName)
                    .WithCustomProperties(
                    [
                        assetSize,
                        ShaderUtilProxy.GetVariantCount(context.Shader),
                        variantCountPerCompilerPlatform == -1 ? k_NotAvailable : variantCountPerCompilerPlatform.ToString(),
                        passCount == -1 ? k_NotAvailable : passCount.ToString(),
                        globalKeywords == null || localKeywords == null ? k_NotAvailable : (globalKeywords.Length + localKeywords.Length).ToString(),
                        propertyCount,
                        texturePropertyCount,
                        context.Shader.renderQueue,
                        hasInstancing,
                        isSrpBatcherCompatible,
                        isAlwaysIncluded
                    ])
                    .WithLocation(context.AssetPath)
                    .WithSeverity(severity);
            }
        }

        IEnumerable<ReportItem> ProcessVariants(ShaderAnalysisContext context)
        {
            if (s_ShaderVariantData.ContainsKey(context.Shader))
            {
                var shaderVariants = s_ShaderVariantData[context.Shader];

                foreach (var shaderVariantData in shaderVariants)
                {
                    if (shaderVariantData.BuildTarget != BuildTarget.NoTarget && shaderVariantData.BuildTarget != context.Params.Platform)
                        continue;

                    yield return context.CreateInsight(IssueCategory.ShaderVariant, context.Shader.name)
                        .WithLocation(context.AssetPath)
                        .WithCustomProperties(
                        [
                            k_NoRuntimeData,
                            shaderVariantData.CompilerPlatform,
                            shaderVariantData.GraphicsTier,
                            shaderVariantData.ShaderType,
                            shaderVariantData.PassType,
                            shaderVariantData.PassName,
                            CombineKeywords(shaderVariantData.Keywords),
                            CombineKeywords(shaderVariantData.PlatformKeywords),
                            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            CombineKeywords(shaderVariantData.Requirements.Select(r => r.ToString()).ToArray())
#pragma warning restore RS0030
                        ]);
                }
            }
        }

        internal static void ClearBuildData()
        {
            s_ShaderVariantData.Clear();
            s_ComputeShaderVariantData.Clear();

            var playerDataCachePath = Path.Combine("Library", "PlayerDataCache");
            if (Directory.Exists(playerDataCachePath))
            {
                Directory.Delete(playerDataCachePath, true);
            }
        }

        internal static int NumBuiltVariants()
        {
            return s_ShaderVariantData.Count;
        }

        public int callbackOrder => Int32.MaxValue;

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (data.Count == 0)
                return; // no variants

            if (!s_ComputeShaderVariantData.ContainsKey(shader))
            {
                s_ComputeShaderVariantData.Add(shader, new List<ComputeShaderVariantData>());
            }

            var buildTargetPropertyInfo = typeof(ShaderCompilerData).GetRuntimeProperty("buildTarget");
            foreach (var shaderCompilerData in data)
            {
                int kernelThreadCount = 0;
                if (shader.HasKernel(kernelName))
                {
                    var kernelIndex = shader.FindKernel(kernelName);
                    // This is gross and it deserves some explaination.
                    // Unlike raster shaders, it is possible for this callback to give you a compute kernel that's invalid for the keyword state.
                    // This seems to only happen when you have a multi_compile without a leading _ default entry, but it's not guaranteed for that situation to cause a problem.
                    // We care because calling GetKernelThreadGroupSizes for a "bad" kernel puts spurious errors in the console and we don't want that.
                    // As it currently exists the check prevents all intended error scenarios but does also skip some perfectly valid kernels.
                    // For now it's an ok compromise but the goal is to get the false positives down to zero.
                    // In service of that, here's the current thinking behind the check.
                    // 1) A variant can't have problems if the base shader defines no keywords.
                    // 2) A variant can't have problems if it has every defined or enabled keyword of the base shader.
                    // 3) A variant can have problems if it has keywords but the base shader has enabled no keywords.
                    bool keywordSpaceValid =
                        (shaderCompilerData.shaderKeywordSet.GetShaderKeywords().Length == shader.shaderKeywords.Length) ||
                        (shaderCompilerData.shaderKeywordSet.GetShaderKeywords().Length == shader.keywordSpace.keywordCount) ||
                        !((shader.shaderKeywords.Length == 0 && shaderCompilerData.shaderKeywordSet.GetShaderKeywords().Length != 0) && shader.keywordSpace.keywordCount > 0);
                    if (keywordSpaceValid && shader.IsSupported(kernelIndex))
                    {
                        shader.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
                        kernelThreadCount = (int)(x * y * z);
                    }
                }

                s_ComputeShaderVariantData[shader].Add(new ComputeShaderVariantData
                {
                    KernelName = kernelName,
                    KernelThreadCount = kernelThreadCount == 0 ? k_ComputeShaderMayHaveBadVariants : kernelThreadCount.ToString(),
                    Keywords = GetShaderKeywords(shader, shaderCompilerData.shaderKeywordSet.GetShaderKeywords()),
                    PlatformKeywords = PlatformKeywordSetToStrings(shaderCompilerData.platformKeywordSet),
                    GraphicsTier = shaderCompilerData.graphicsTier,
                    BuildTarget = (buildTargetPropertyInfo != null) ? (BuildTarget)buildTargetPropertyInfo.GetValue(shaderCompilerData) : BuildTarget.NoTarget,
                    CompilerPlatform = shaderCompilerData.shaderCompilerPlatform
                });
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (data.Count == 0)
                return; // no variants

            if (!s_ShaderVariantData.ContainsKey(shader))
            {
                s_ShaderVariantData.Add(shader, new List<ShaderVariantData>());
            }

            // the buildTarget property is only available as of 2020_3_35 so we need to use reflection to get the value
            var buildTargetPropertyInfo = typeof(ShaderCompilerData).GetRuntimeProperty("buildTarget");
            foreach (var shaderCompilerData in data)
            {
                var shaderRequirements = shaderCompilerData.shaderRequirements;
                var shaderRequirementsList = new List<ShaderRequirements>();
                foreach (ShaderRequirements value in Enum.GetValues(shaderRequirements.GetType()))
                    if ((shaderRequirements & value) != 0)
                        shaderRequirementsList.Add(value);

                if (shaderRequirementsList.Count > 1)
                    shaderRequirementsList.Remove(ShaderRequirements.None);

                s_ShaderVariantData[shader].Add(new ShaderVariantData
                {
                    PassType = snippet.passType,
                    PassName =  snippet.passName,
                    ShaderType = snippet.shaderType,
                    Keywords = GetShaderKeywords(shader, shaderCompilerData.shaderKeywordSet.GetShaderKeywords()),
                    PlatformKeywords = PlatformKeywordSetToStrings(shaderCompilerData.platformKeywordSet),
                    Requirements = shaderRequirementsList.ToArray(),
                    GraphicsTier = shaderCompilerData.graphicsTier,
                    BuildTarget = (buildTargetPropertyInfo != null) ? (BuildTarget)buildTargetPropertyInfo.GetValue(shaderCompilerData) : BuildTarget.NoTarget,
                    CompilerPlatform = shaderCompilerData.shaderCompilerPlatform
                });
            }
        }

        public static void ExportVariantsToSvc(string svcName, string path, ReportItem[] variants)
        {
            var svc = new ShaderVariantCollection();
            svc.name = svcName;

            foreach (var issue in variants)
            {
                var shader = Shader.Find(issue.GetProperty(PropertyType.Description));
                var passType = issue.GetCustomProperty(ShaderVariantProperty.PassType);
                var keywords = SplitKeywords(issue.GetCustomProperty(ShaderVariantProperty.Keywords));

                if (shader != null && !passType.Equals(string.Empty))
                {
                    var shaderVariant = new ShaderVariantCollection.ShaderVariant();
                    shaderVariant.shader = shader;
                    shaderVariant.passType = (UnityEngine.Rendering.PassType)Enum.Parse(typeof(UnityEngine.Rendering.PassType), passType);
                    shaderVariant.keywords = keywords;
                    svc.Add(shaderVariant);
                }
            }
            AssetDatabase.CreateAsset(svc, path);
        }

        public static ParseLogResult ParsePlayerLog(string logFile, ReportItem[] builtVariants, IProgress progress = null)
        {
            var compiledVariants = new Dictionary<string, List<CompiledVariantData>>();
            var lines = GetCompiledShaderLines(logFile);
            if (lines == null)
                return ParseLogResult.ReadError;

            foreach (var line in lines)
            {
                var parts = line.Split(new[] {" (instance ", ", pass: ", ", stage: ", ", keywords ", ", time"}, StringSplitOptions.None);

                if (parts.Length != 4 && parts.Length != 6)
                {
                    Debug.LogError("Malformed shader compilation log info: " + line);
                    continue;
                }

                var shaderName = parts[0];
                var pass = parts[1];
                var stage = parts[2];
                var keywordsString = parts[3];

                if (parts.Length == 6)
                {
                    pass = parts[2];
                    stage = parts[3];
                    keywordsString = parts[4];
                }

                var keywords = SplitKeywords(keywordsString, " ");

                // fix-up stage to be consistent with built variants stage
                if (k_StageNameMap.ContainsKey(stage))
                    stage = k_StageNameMap[stage];

                if (!compiledVariants.ContainsKey(shaderName))
                {
                    compiledVariants.Add(shaderName, new List<CompiledVariantData>());
                }
                compiledVariants[shaderName].Add(new CompiledVariantData
                {
                    Pass = pass,
                    Stage = stage,
                    Keywords = keywords
                });
            }

            if (compiledVariants.Count == 0)
                return ParseLogResult.NoCompiledVariants;

            var sortedBuiltVariants = new ReportItem[builtVariants.Length];
            builtVariants.CopyTo(sortedBuiltVariants, 0);
            Array.Sort(sortedBuiltVariants, (v1, v2) => string.Compare(v1.Description, v2.Description));

            var shader = (Shader)null;
            foreach (var builtVariant in sortedBuiltVariants)
            {
                if (shader == null || !shader.name.Equals(builtVariant.Description))
                {
                    shader = Shader.Find(builtVariant.Description);
                }

                if (shader == null)
                {
                    builtVariant.SetCustomProperty(ShaderVariantProperty.Compiled, "?");
                    continue;
                }

                var shaderName = shader.name;
                var stage = builtVariant.GetCustomProperty(ShaderVariantProperty.Stage);
                var passName = builtVariant.GetCustomProperty(ShaderVariantProperty.PassName);
                var keywordsString = builtVariant.GetCustomProperty(ShaderVariantProperty.Keywords);
                var keywords = SplitKeywords(keywordsString);
                var isVariantCompiled = false;

                if (compiledVariants.ContainsKey(shaderName))
                {
                    // note that we are not checking pass name since there is an inconsistency regarding "unnamed" passes between build vs compiled
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var matchingVariants = compiledVariants[shaderName].Where(cv => ShaderVariantsMatch(cv, stage, passName, keywords)).ToArray();
#pragma warning restore RS0030
                    isVariantCompiled = matchingVariants.Length > 0;
                }

                builtVariant.SetCustomProperty(ShaderVariantProperty.Compiled, isVariantCompiled);
            }

            return ParseLogResult.Success;
        }

        // Older Unity versions use the first string in their log, new versions use the second.
        // Rather than trying to identify the specific version when the change occurred, we'll just check both.
        static readonly string[] k_CompiledShaderPrefixes = { "Compiled shader: ", "Uploaded shader variant to the GPU driver: " };

        static string[] GetCompiledShaderLines(string logFile)
        {
            var compilationLines = new List<string>();
            try
            {
                using (var file = new StreamReader(logFile))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        for (int i = 0; i < k_CompiledShaderPrefixes.Length; ++i)
                        {
                            var compilationLogIndex = line.IndexOf(k_CompiledShaderPrefixes[i], StringComparison.Ordinal);
                            if (compilationLogIndex >= 0)
                            {
                                compilationLines.Add(
                                    line.Substring(compilationLogIndex + k_CompiledShaderPrefixes[i].Length));
                                break;
                            }
                        }
                    }
                }
                return compilationLines.ToArray();
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return null;
            }
        }

        static bool ShaderVariantsMatch(CompiledVariantData cv, string stage, string passName, string[] secondSet)
        {
            if (!cv.Stage.Equals(stage, StringComparison.InvariantCultureIgnoreCase))
                return false;

            var passMatch = cv.Pass.Equals(passName);
            if (!passMatch && string.IsNullOrEmpty(passName))
                passMatch = Array.IndexOf(k_NoPassNames, cv.Pass) != -1 || cv.Pass.StartsWith("<Unnamed Pass ");

            if (!passMatch)
                return false;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return cv.Keywords.OrderBy(e => e).SequenceEqual(secondSet.OrderBy(e => e));
#pragma warning restore RS0030
        }

        static string[] GetShaderKeywords(Shader shader, ShaderKeyword[] shaderKeywords)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var keywords = shaderKeywords.Select(keyword => keyword.name);
            return keywords.ToArray();
#pragma warning restore RS0030
        }

        static string[] GetShaderKeywords(ComputeShader shader, ShaderKeyword[] shaderKeywords)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var keywords = shaderKeywords.Select(keyword => keyword.name);
            return keywords.ToArray();
#pragma warning restore RS0030
        }

        static string[] SplitKeywords(string keywordsString, string separator = null)
        {
            if (keywordsString.Equals(k_NoKeywords))
                return Array.Empty<string>();
            return Formatting.SplitStrings(keywordsString, separator);
        }

        static string CombineKeywords(string[] strings, string separator = null)
        {
            if (strings.Length > 0)
                return Formatting.CombineStrings(strings, separator);
            return k_NoKeywords;
        }

        static string[] PlatformKeywordSetToStrings(PlatformKeywordSet platformKeywordSet)
        {
            var builtinShaderDefines = new List<BuiltinShaderDefine>();

            foreach (BuiltinShaderDefine value in Enum.GetValues(typeof(BuiltinShaderDefine)))
                if (platformKeywordSet.IsEnabled(value))
                    builtinShaderDefines.Add(value);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return builtinShaderDefines.Select(d => d.ToString()).ToArray();
#pragma warning restore RS0030
        }

        static bool ShaderTypeIsFragment(ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform)
        {
            switch (shaderCompilerPlatform)
            {
                // On OpenGL and Vulkan, all stages supported by the shader are combined into a single ShaderType (Vertex).
                case ShaderCompilerPlatform.GLES3x:
                case ShaderCompilerPlatform.OpenGLCore:
                case ShaderCompilerPlatform.Vulkan:
                    return true;
                default:
                    return shaderType == ShaderType.Fragment;
            }
        }
    }
}
