// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Compilation;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    internal enum TargetAssemblyType
    {
        Undefined = 0,
        Predefined = 1,
        Custom = 2
    }

    internal enum EditorCompatibility
    {
        NotCompatibleWithEditor = 0,
        CompatibleWithEditor = 1
    }

    [DebuggerDisplay("{Filename}")]
    internal class TargetAssembly
    {
        public string Filename { get; private set; }
        public SupportedLanguage Language { get; set; }
        public AssemblyFlags Flags { get; private set; }
        public string PathPrefix { get; private set; }
        public string[] AdditionalPrefixes { get; set; }
        public Func<string, int> PathFilter { get; private set; }
        public Func<ScriptAssemblySettings, string[], bool> IsCompatibleFunc { get; private set; }
        public List<TargetAssembly> References { get; set; }
        public List<string> ExplicitPrecompiledReferences { get; set; }
        public TargetAssemblyType Type { get; private set; }
        public string[] Defines { get; set; }
        public string RootNamespace { get; set; }
        public ScriptCompilerOptions CompilerOptions { get; set; }
        public List<VersionDefine> VersionDefines { get; set; }
        public int MaxPathLength { get; private set; }

        public TargetAssembly()
        {
            References = new List<TargetAssembly>();
            Defines = null;
        }

        public TargetAssembly(string name,
                              SupportedLanguage language,
                              AssemblyFlags flags,
                              TargetAssemblyType type,
                              string pathPrefix,
                              string[] additionalPrefixes,
                              Func<string, int> pathFilter,
                              Func<ScriptAssemblySettings, string[], bool> compatFunc,
                              ScriptCompilerOptions compilerOptions) : this()
        {
            Language = language;
            Filename = name;
            Flags = flags;
            PathPrefix = pathPrefix;
            AdditionalPrefixes = additionalPrefixes;
            PathFilter = pathFilter;
            IsCompatibleFunc = compatFunc;
            Type = type;
            CompilerOptions = compilerOptions;
            ExplicitPrecompiledReferences = new List<string>();
            VersionDefines = new List<VersionDefine>();

            if (PathPrefix != null)
                MaxPathLength = PathPrefix.Length;
            if (AdditionalPrefixes != null)
                MaxPathLength = UnityEngine.Mathf.Max(MaxPathLength, AdditionalPrefixes.Max(am => am.Length));
        }

        public string FullPath(string outputDirectory)
        {
            return AssetPath.Combine(outputDirectory, Filename);
        }

        public EditorCompatibility editorCompatibility
        {
            get
            {
                bool isCompatibleWithEditor = IsCompatibleFunc == null ||
                    IsCompatibleFunc(new ScriptAssemblySettings { BuildTarget = BuildTarget.NoTarget, CompilationOptions = EditorScriptCompilationOptions.BuildingForEditor }, null);

                return isCompatibleWithEditor
                    ? EditorCompatibility.CompatibleWithEditor
                    : EditorCompatibility.NotCompatibleWithEditor;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Filename, Type);
        }

        protected bool Equals(TargetAssembly other)
        {
            return string.Equals(Filename, other.Filename) && Flags == other.Flags && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TargetAssembly)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Filename != null ? Filename.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Flags;
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
        }
    }
}
