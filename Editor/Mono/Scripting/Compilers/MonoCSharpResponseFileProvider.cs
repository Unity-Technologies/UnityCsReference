// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEditor.Scripting.Compilers
{
    internal class MonoCSharpResponseFileProvider : ResponseFileProvider
    {
        public override string ResponseFileName { get { return CompilerSpecificResponseFiles.MonoCSharpCompiler; } }
        public override string[] ObsoleteResponseFileNames { get { return CompilerSpecificResponseFiles.MonoCSharpCompilerObsolete; } }
    }
}
