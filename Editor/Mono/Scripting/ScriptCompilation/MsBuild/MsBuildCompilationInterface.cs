// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Net;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild
{
    static class MsBuildCompilationInterface
    {
        static MsBuildCompilation msBuildCompilation;
        static MSBuildCompilationStatus mSBuildCompilationStatus = MSBuildCompilationStatus.Uninitialized;

        private enum MSBuildCompilationStatus
        {
            Uninitialized,
            Legacy,
            NewCsproj,
        }

        static MsBuildCompilationInterface()
        {
            if (!IsEnabled())
                return;
        }

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

        public static bool IsEnabled()
        {
            if (mSBuildCompilationStatus == MSBuildCompilationStatus.Uninitialized)
                mSBuildCompilationStatus = System.IO.File.Exists("ProjectSettings/enablemsbuild.config") ? MSBuildCompilationStatus.NewCsproj : MSBuildCompilationStatus.Legacy;

            return mSBuildCompilationStatus == MSBuildCompilationStatus.NewCsproj;
        }

        [RequiredByNativeCode]
        public static void RequestMsBuildScriptCompilation(bool restore, string reason)
        {
            Instance.RequestMsBuildScriptCompilation(restore, reason);
        }

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
    }
}
