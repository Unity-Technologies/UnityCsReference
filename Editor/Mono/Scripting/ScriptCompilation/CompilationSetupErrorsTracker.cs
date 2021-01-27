// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Compilation;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    /// <summary>Classes derived from <c>CompilationSetupErrorsTrackerBase</c> are responsible for providing an interface for tracking compilation setup errors
    /// within the editor and manage reporting those to the user as they are discovered or fixed.
    /// </summary>
    abstract class CompilationSetupErrorsTrackerBase
    {
        public abstract void SetCompilationSetupErrors(CompilationSetupErrors errors);
        public abstract void ClearCompilationSetupErrors(CompilationSetupErrors errors);
        public abstract bool HaveCompilationSetupErrors();
        public abstract void LogCompilationSetupErrors(CompilationSetupErrors compilationSetupError, string[] filePaths, string message);

        /// <summary>PrecompiledAssemblyException is treated differently from other compilation setup error signifying exceptions.
        /// If it is discovered in the context of script compilation within the editor, then CompilationSetupErrorsTracker should be allowed to process it
        /// via the ProcessPrecompiledAssemblyException().
        /// If the error was discovered in any other context it should be propagated in the callstack and should not affect the SetupCompilationErrorsFlags state.
        /// The code using the PrecompiledAssemblyProvider should decide which way to handle this exception and can choose to call this method if appropriate.
        /// </summary>
        public virtual void ProcessPrecompiledAssemblyException(PrecompiledAssemblyException exception)
        {
            SetCompilationSetupErrors(CompilationSetupErrors.PrecompiledAssemblyError);
            LogCompilationSetupErrors(CompilationSetupErrors.PrecompiledAssemblyError, exception.filePaths, exception.Message);
        }

        /// <summary>ProcessException is a utility function to call from a code which processes all exceptions thrown by any EditorCompilation action in a generic fashion
        /// (eg. EditorCompilationInterfact). If the exception is one indicating a setup compilation error, this function will take care of the logging needed
        /// when encountering this exception and return true. Otherwise, when false is returned, the caller should take care of processing the exception.
        /// </summary>
        public virtual bool ProcessException(Exception exception)
        {
            var assemblyDefinitionException = exception as AssemblyDefinitionException;
            var precompiledAssemblyException = exception as PrecompiledAssemblyException;

            if (assemblyDefinitionException != null && assemblyDefinitionException.filePaths.Length > 0)
            {
                LogCompilationSetupErrors(
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

    /// <summary>Class <c>CompilationSetupErrorsTracker</c> is the default implementation of CompilationSetupErrorsTrackerBase
    /// which uses native state to keep track of present errors and it communicates these errors as sticky console errors.
    /// </summary>
    class CompilationSetupErrorsTracker : CompilationSetupErrorsTrackerBase
    {
        public override void SetCompilationSetupErrors(CompilationSetupErrors errors)
        {
            SetCompilationSetupErrorsNative(errors);
        }

        public override void ClearCompilationSetupErrors(CompilationSetupErrors errors)
        {
            ClearCompilationSetupErrorsNative(errors);
        }

        public override bool HaveCompilationSetupErrors()
        {
            return HaveCompilationSetupErrorsNative();
        }

        public override void LogCompilationSetupErrors(CompilationSetupErrors compilationSetupError, string[] filePaths, string message)
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
}
