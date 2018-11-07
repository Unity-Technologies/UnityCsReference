// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Scripting.Compilers
{
    internal class CompilerSpecificResponseFiles
    {
        public const string MicrosoftCSharpCompiler = "csc.rsp";
        public static string[] MicrosoftCSharpCompilerObsolete = new[] { "mcs.rsp" };

        public const string MonoCSharpCompiler = "mcs.rsp";
        public static string[] MonoCSharpCompilerObsolete = new[] { "smcs.rsp", "gmcs.rsp" };

        public static IEnumerable<string> AllCompilerSpecific()
        {
            return new[]
            {
                MicrosoftCSharpCompiler,
                MonoCSharpCompiler
            }.Concat(MonoCSharpCompilerObsolete);
        }
    }
}
