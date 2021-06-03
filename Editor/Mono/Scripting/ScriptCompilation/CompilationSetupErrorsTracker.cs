// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Compilation;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    /// <summary>Classes implementing <c>ICompilationSetupErrorsTracker</c> are responsible for providing an interface for tracking compilation setup errors
    /// within the editor and manage reporting those to the user as they are discovered or fixed.
    /// </summary>
    interface ICompilationSetupErrorsTracker
    {
        void SetCompilationSetupErrors(CompilationSetupErrors errors);
        void ClearCompilationSetupErrors(CompilationSetupErrors errors);
        bool HaveCompilationSetupErrors();
        void LogCompilationSetupErrors(CompilationSetupErrors compilationSetupError, string[] filePaths, string message);
    }

    interface ICompilationSetupWarningTracker
    {
        void AddAssetWarning(string assetPath, string message);
        void ClearAssetWarnings();
    }

    static class CompilationSetupErrorsTrackerExtensions
    {
        public static void ProcessPrecompiledAssemblyException(this ICompilationSetupErrorsTracker tracker, PrecompiledAssemblyException exception)
        {
            tracker.SetCompilationSetupErrors(CompilationSetupErrors.PrecompiledAssemblyError);
            tracker.LogCompilationSetupErrors(CompilationSetupErrors.PrecompiledAssemblyError, exception.filePaths, exception.Message);
        }

        public static bool ProcessException(this ICompilationSetupErrorsTracker tracker, Exception exception)
        {
            var assemblyDefinitionException = exception as AssemblyDefinitionException;
            var precompiledAssemblyException = exception as PrecompiledAssemblyException;

            if (assemblyDefinitionException != null && assemblyDefinitionException.filePaths.Length > 0)
            {
                tracker.LogCompilationSetupErrors(
                    assemblyDefinitionException.errorType == AssemblyDefinitionErrorType.LoadError ?
                    CompilationSetupErrors.LoadError : CompilationSetupErrors.CyclicReferences,
                    assemblyDefinitionException.filePaths, assemblyDefinitionException.Message);
                return true;
            }
            else if (precompiledAssemblyException != null)
            {
                // PrecompiledAssemblyException was potentially processed earlier at the GetPrecompiledAssemblies call site
                // if it was called in the context of script compilation within the editor.
                UnityEngine.Debug.LogException(exception);
                return true;
            }

            return false;
        }
    }

    /// <summary>Class <c>CompilationSetupErrorsTracker</c> is the default implementation of ICompilationSetupErrorsTracker
    /// which uses native state to keep track of present errors and it communicates these errors as sticky console errors.
    /// </summary>
    class CompilationSetupErrorsTracker : ICompilationSetupErrorsTracker
    {
        public void SetCompilationSetupErrors(CompilationSetupErrors errors)
        {
            SetCompilationSetupErrorsNative(errors);
        }

        public void ClearCompilationSetupErrors(CompilationSetupErrors errors)
        {
            ClearCompilationSetupErrorsNative(errors);
        }

        public bool HaveCompilationSetupErrors()
        {
            return HaveCompilationSetupErrorsNative();
        }

        public void LogCompilationSetupErrors(CompilationSetupErrors compilationSetupError, string[] filePaths, string message)
        {
            foreach (var filePath in filePaths)
            {
                var messageWithPath = $"{message} ({filePath})";
                LogCompilationSetupErrorNative(compilationSetupError, messageWithPath, filePath);
            }
        }

        [FreeFunction(Name = "SetCompilationSetupErrors")]
        internal static extern void SetCompilationSetupErrorsNative(CompilationSetupErrors errors);
        [FreeFunction(Name = "ClearCompilationSetupErrors")]
        internal static extern void ClearCompilationSetupErrorsNative(CompilationSetupErrors errors);
        [FreeFunction(Name = "HaveCompilationSetupErrors")]
        internal static extern bool HaveCompilationSetupErrorsNative();
        [FreeFunction(Name = "LogCompilationSetupError")]
        internal static extern void LogCompilationSetupErrorNative(CompilationSetupErrors compilationSetupError, string message, string filePath);
    }

    class CompilationSetupWarningTracker : ICompilationSetupWarningTracker
    {
        public void AddAssetWarning(string assetPath, string message)
        {
            AddAssetWarningNative(assetPath, message);
        }

        public void ClearAssetWarnings()
        {
            ClearCompilationAssetWarningsNative();
        }

        [FreeFunction(Name = "AddCompilationAssetWarning")]
        internal static extern void AddAssetWarningNative(string assetPath, string message);
        [FreeFunction(Name = "ClearCompilationAssetWarnings")]
        internal static extern void ClearCompilationAssetWarningsNative();
    }
}
