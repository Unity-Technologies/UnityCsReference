// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [Obsolete("[ShaderIncludePath] attribute is no longer supported. Your shader library must be under the Assets folder or in a package. To include shader headers directly from a package, use #include \"Packages/<package name>/<path to your header file>\"", true)]
    [AttributeUsage(AttributeTargets.Method)]
    public class ShaderIncludePathAttribute : Attribute
    {
        [RequiredSignature]
        static string[] GetIncludePaths() { throw new InvalidOperationException(); }
    }
}
