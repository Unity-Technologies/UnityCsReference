// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Compilation
{
    public class AssemblyDefinitionException : Exception
    {
        public string[] filePaths { get; private set; }

        public AssemblyDefinitionException(string message, params string[] filePaths) : base(message)
        {
            this.filePaths = filePaths;
        }
    }
}
