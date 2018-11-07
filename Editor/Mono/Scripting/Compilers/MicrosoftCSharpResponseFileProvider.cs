// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpResponseFileProvider : ResponseFileProvider
    {
        public override string ResponseFileName { get { return CompilerSpecificResponseFiles.MicrosoftCSharpCompiler; } }
        public override string[] ObsoleteResponseFileNames { get { return CompilerSpecificResponseFiles.MicrosoftCSharpCompilerObsolete; } }
    }
}
