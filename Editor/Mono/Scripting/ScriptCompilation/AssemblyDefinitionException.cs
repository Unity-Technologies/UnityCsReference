// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Compilation
{
    internal enum AssemblyDefinitionErrorType
    {
        loadError,
        cyclicReferences
    }

    public class AssemblyDefinitionException : Exception
    {
        internal AssemblyDefinitionErrorType errorType { get; private set; }
        public string[] filePaths { get; private set; }

        internal AssemblyDefinitionException(string message, AssemblyDefinitionErrorType errorType, params string[] filePaths) : base(message)
        {
            this.errorType = errorType;
            this.filePaths = filePaths;
        }

        public AssemblyDefinitionException(string message, params string[] filePaths) : base(message)
        {
            this.errorType = AssemblyDefinitionErrorType.loadError;
            this.filePaths = filePaths;
        }
    }

    public class PrecompiledAssemblyException : Exception
    {
        public string[] filePaths { get; private set; }

        public PrecompiledAssemblyException(string message, params string[] filePaths) : base(message)
        {
            this.filePaths = filePaths;
        }
    }
}
