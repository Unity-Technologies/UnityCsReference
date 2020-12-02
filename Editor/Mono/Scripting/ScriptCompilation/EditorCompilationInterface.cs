// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CompilerMessageType = UnityEditor.Scripting.Compilers.CompilerMessageType;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class EditorCompilationInterface
    {
        static EditorCompilation editorCompilation;

        public static EditorCompilation Instance
        {
            get
            {
                if (editorCompilation == null)
                {
                    editorCompilation = new EditorCompilation();
                    editorCompilation.setupErrorFlagsChanged += ClearErrors;

                    // Clear the errors after creating editorCompilation,
                    // as it is accessed in the CompilationPipeline cctor.
                    CompilationPipeline.ClearEditorCompilationErrors(); // Clear all errors on domain reload.
                }

                return editorCompilation;
            }
        }

        static void ClearErrors(EditorCompilation.CompilationSetupErrorFlags flags)
        {
            if (flags == EditorCompilation.CompilationSetupErrorFlags.none)
                CompilationPipeline.ClearEditorCompilationErrors();
        }

        static void LogException(Exception exception)
        {
            var assemblyDefinitionException = exception as AssemblyDefinitionException;
            var precompiledAssemblyException = exception as PrecompiledAssemblyException;

            if (assemblyDefinitionException != null && assemblyDefinitionException.filePaths.Length > 0)
            {
                foreach (var filePath in assemblyDefinitionException.filePaths)
                {
                    var message = string.Format("{0} ({1})", exception.Message, filePath);

                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                    var instanceID = asset.GetInstanceID();

                    CompilationPipeline.LogEditorCompilationError(message, instanceID);
                }
            }
            else if (precompiledAssemblyException != null)
            {
                foreach (var filePath in precompiledAssemblyException.filePaths)
                {
                    var message = string.Format(exception.Message, filePath);
                    var loadAssetAtPath = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);

                    if (loadAssetAtPath != null)
                    {
                        CompilationPipeline.LogEditorCompilationError(message, loadAssetAtPath.GetInstanceID());
                    }
                    else
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            else
            {
                Debug.LogException(exception);
            }
        }

        static void EmitExceptionsAsErrors(Exception[] exceptions)
        {
            if (exceptions == null || exceptions.Length == 0)
                return;

            foreach (var exception in exceptions)
                LogException(exception);
        }

        static T EmitExceptionAsError<T>(Func<T> func, T returnValue)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                LogException(e);
                return returnValue;
            }
        }

        static void LogWarning(string warning, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Debug.LogWarning(warning, asset);
        }

        [RequiredByNativeCode]
        public static void SetAssetPathsMetaData(AssetPathMetaData[] assetPathMetaDatas)
        {
            Instance.SetAssetPathsMetaData(assetPathMetaDatas);
        }

        [RequiredByNativeCode]
        public static void SetAdditionalVersionMetaDatas(VersionMetaData[] versionMetaDatas)
        {
            Instance.SetAdditionalVersionMetaDatas(versionMetaDatas);
        }

        [RequiredByNativeCode]
        public static void SetAllScripts(string[] allScripts, string[] assemblyFilenames)
        {
            Instance.SetAllScripts(allScripts, assemblyFilenames);
        }

        [RequiredByNativeCode]
        public static bool HaveScriptsForEditorBeenCompiledSinceLastDomainReload()
        {
            return Instance.HaveScriptsForEditorBeenCompiledSinceLastDomainReload();
        }

        [RequiredByNativeCode]
        public static void RequestScriptCompilation(string reason)
        {
            Instance.RequestScriptCompilation(reason);
        }

        [RequiredByNativeCode]
        public static void SkipCustomScriptAssemblyGraphValidation(bool skipValidation)
        {
            Instance.SkipCustomScriptAssemblyGraphValidation(skipValidation);
        }

        [RequiredByNativeCode]
        public static void ClearCustomScriptAssemblies()
        {
            Instance.ClearCustomScriptAssemblies();
        }

        [RequiredByNativeCode]
        public static void DeleteScriptAssemblies()
        {
            Instance.DeleteScriptAssemblies();
        }

        [RequiredByNativeCode]
        public static void SetAllUnityAssemblies(PrecompiledAssembly[] unityAssemblies)
        {
            Instance.SetAllUnityAssemblies(unityAssemblies);
        }

        // Burst package depends on this method, so we can't remove it.
        [RequiredByNativeCode]
        public static void SetCompileScriptsOutputDirectory(string directory)
        {
            Instance.SetCompileScriptsOutputDirectory(directory);
        }

        [RequiredByNativeCode]
        public static string GetCompileScriptsOutputDirectory()
        {
            return EmitExceptionAsError(() => Instance.GetCompileScriptsOutputDirectory(), string.Empty);
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyReferenceJsons(string[] allAssemblyReferenceJsons, string[] allAssemblyReferenceJsonContents)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyReferenceJsonsContents(allAssemblyReferenceJsons, allAssemblyReferenceJsonContents));
        }

        [RequiredByNativeCode]
        public static void SetAllCustomScriptAssemblyJsonContents(string[] allAssemblyJsonPaths, string[] allAssemblyJsonContents, string[] guids)
        {
            EmitExceptionsAsErrors(Instance.SetAllCustomScriptAssemblyJsonContents(allAssemblyJsonPaths, allAssemblyJsonContents, guids));
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetAllCompiledAndResolvedTargetAssemblies(EditorScriptCompilationOptions options, BuildTarget buildTarget)
        {
            EditorCompilation.CustomScriptAssemblyAndReference[] assembliesWithMissingReference = null;

            var result = EmitExceptionAsError(() => Instance.GetAllCompiledAndResolvedTargetAssemblies(options, buildTarget, out assembliesWithMissingReference), new EditorCompilation.TargetAssemblyInfo[0]);

            if (assembliesWithMissingReference != null && assembliesWithMissingReference.Length > 0)
            {
                foreach (var assemblyAndReference in assembliesWithMissingReference)
                {
                    LogWarning(string.Format("The assembly for Assembly Definition File '{0}' will not be loaded. Because the assembly for its reference '{1}'' does not exist on the file system. " +
                        "This can be caused by the reference assembly not being compiled due to errors or not having any scripts associated with it.",
                        assemblyAndReference.Assembly.FilePath,
                        assemblyAndReference.Reference.FilePath),
                        assemblyAndReference.Assembly.FilePath);
                }
            }

            // Check we do not have any assembly definition references (asmref) without matching assembly definitions (asmdef)
            List<CustomScriptAssemblyReference> referencesWithMissingAssemblies;
            Instance.GetAssemblyDefinitionReferencesWithMissingAssemblies(out referencesWithMissingAssemblies);
            if (referencesWithMissingAssemblies.Count > 0)
            {
                foreach (var asmref in referencesWithMissingAssemblies)
                {
                    var warning = $"The Assembly Definition Reference file '{asmref.FilePath}' will not be used. ";
                    if (string.IsNullOrEmpty(asmref.Reference))
                        warning += "It does not contain a reference to an Assembly Definition File.";
                    else
                        warning += $"The reference to the Assembly Definition File with the name '{asmref.Reference}' could not be found.";
                    LogWarning(warning, asmref.FilePath);
                }
            }

            return result;
        }

        [RequiredByNativeCode]
        public static bool HaveSetupErrors()
        {
            return Instance.HaveSetupErrors();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus CompileScripts(EditorScriptCompilationOptions definesOptions, BuildTargetGroup platformGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            return EmitExceptionAsError(() => Instance.CompileScripts(definesOptions, platformGroup, platform, extraScriptingDefines),
                EditorCompilation.CompileStatus.CompilationFailed);
        }

        [RequiredByNativeCode]
        public static bool DoesProjectFolderHaveAnyScripts()
        {
            return Instance.DoesProjectFolderHaveAnyScripts();
        }

        [RequiredByNativeCode]
        public static bool IsCompilationPending()
        {
            return Instance.IsScriptCompilationRequested();
        }

        [RequiredByNativeCode]
        public static bool IsCompiling()
        {
            return Instance.IsCompiling();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.CompileStatus TickCompilationPipeline(EditorScriptCompilationOptions options, BuildTargetGroup platformGroup, BuildTarget platform, string[] extraScriptingDefines)
        {
            try
            {
                return Instance.TickCompilationPipeline(options, platformGroup, platform, extraScriptingDefines);
            }
            catch (Exception e)
            {
                LogException(e);
                return EditorCompilation.CompileStatus.CompilationFailed;
            }
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo[] GetTargetAssemblyInfos()
        {
            return Instance.GetTargetAssemblyInfos();
        }

        [RequiredByNativeCode]
        public static EditorCompilation.TargetAssemblyInfo GetTargetAssembly(string scriptPath)
        {
            return Instance.GetTargetAssembly(scriptPath);
        }

        public static EditorScriptCompilationOptions GetAdditionalEditorScriptCompilationOptions(
            AssembliesType assembliesType)
        {
            var options = GetAdditionalEditorScriptCompilationOptions();
            if (EditorUserBuildSettings.development && (assembliesType == AssembliesType.Player || assembliesType == AssembliesType.PlayerWithoutTestAssemblies))
                options |= EditorScriptCompilationOptions.BuildingDevelopmentBuild;
            return options;
        }

        public static EditorScriptCompilationOptions GetAdditionalEditorScriptCompilationOptions()
        {
            var options = EditorScriptCompilationOptions.BuildingEmpty;

            if (PlayerSettings.allowUnsafeCode)
                options |= EditorScriptCompilationOptions.BuildingPredefinedAssembliesAllowUnsafeCode;

            if (PlayerSettings.UseDeterministicCompilation)
                options |= EditorScriptCompilationOptions.BuildingUseDeterministicCompilation;

            return options;
        }
    }
}
