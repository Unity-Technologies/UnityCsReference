// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild
{
    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    static partial class MsBuildCompilationInterface
    {
        [NoAutoStaticsCleanup] // editor MSBuild compilation service singleton, safe to persist across reload
        static MsBuildCompilation msBuildCompilation;

        public static MsBuildCompilation Instance
        {
            get
            {
                if (msBuildCompilation == null)
                {
                    msBuildCompilation = new MsBuildCompilation();
                }

                return msBuildCompilation;
            }
        }

        // The singleton outlives code reloads, so state scoped to "since the last domain reload" is reset here.
        [OnCodeLoaded]
        static void ResetPerReloadState()
        {
            msBuildCompilation?.OnCodeLoaded();
        }

        public static bool IsEnabled() => UnityEditor.Compilation.CompilationPipeline.IsUsingMSBuild();

        [RequiredByNativeCode]
        public static void RequestMsBuildScriptCompilation(bool restore, string reason)
        {
            Instance.RequestMsBuildScriptCompilation(restore, reason);
        }

        [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
        internal static Task<MsBuildCompilation.CompilationMessages> RequestAnalysisCompilationAsync(string analysisConfiguration, CancellationToken cancellationToken = default)
            => Instance.RequestAnalysisCompilationAsync(analysisConfiguration, cancellationToken);

        [RequiredByNativeCode]
        public static void InitializeMsBuild(bool createInitCsrpojs, MSBuildCompilationOptions compilationOptions)
        {
            Instance.Initialize(createInitCsrpojs, compilationOptions);
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus TickMsBuildCompilationPipeline(BuildTarget buildTarget, bool allowBlocking, MSBuildCompilationOptions compilationOptions)
        {
            var compileTarget = (compilationOptions & MSBuildCompilationOptions.BuildingWithDebug) != 0 ? CompileTarget.EditorDebug : CompileTarget.EditorRelease;

            return Instance.TickCompilationPipeline(buildTarget, allowBlocking, compileTarget, compilationOptions);
        }

        [RequiredByNativeCode]
        // Blocking call to Compile
        public static EditorCompilation.CompileStatus CompileMsBuild(BuildTarget buildTarget, CompileTarget compileTarget, MSBuildCompilationOptions compilationOptions)
        {
            return Instance.Compile(buildTarget, compileTarget, compilationOptions);
        }

        [RequiredByNativeCode]
        public static bool HaveScriptsForEditorBeenCompiledSinceLastDomainReloadMsBuild()
        {
            return Instance.HaveScriptsForEditorBeenCompiledSinceLastDomainReload();
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyReferenceJsonsMsBuild(string[] allAssemblyReferenceJsons, string[] allAssemblyReferenceJsonContents)
        {
            Instance.SetAllCustomScriptAssemblyReferenceJsonsContents(allAssemblyReferenceJsons, allAssemblyReferenceJsonContents);
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsonContentsMsBuild(string[] allAssemblyJsonPaths, string[] allAssemblyJsonContents, string[] guids)
        {
            Instance.SetAllCustomScriptAssemblyJsonContents(allAssemblyJsonPaths, allAssemblyJsonContents, guids);
        }

        [RequiredByNativeCode]
        public static void ClearCustomScriptAssembliesMsBuild()
        {
            Instance.ClearCustomScriptAssemblies();
        }

        [RequiredByNativeCode]
        public static void SetAssetPathsMetaDataMsBuild(AssetPathMetaData[] assetPathMetaDatas)
        {
            Instance.SetAssetPathsMetaData(assetPathMetaDatas);
        }

        [RequiredByNativeCode]
        public static void SetAdditionalVersionMetaDatasMsBuild(VersionMetaData[] versionMetaDatas)
        {
            Instance.SetAdditionalVersionMetaDatas(versionMetaDatas);
        }

        [RequiredByNativeCode]
        public static void SetAllScriptsMsBuild(string[] allScripts)
        {
            Instance.SetAllScripts(allScripts);
        }
    }
}
