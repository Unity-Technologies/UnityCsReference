// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Modules
{
    /// <summary>
    /// Composition for <see cref="ICompilationExtension"/>. A derived or multi-target platform shares
    /// its base or selected platform's CompilationExtension, so what it adds is composed onto the base
    /// or selected platform's extension rather than used in place of it.
    /// </summary>
    internal class CompositeCompilationExtension : ICompilationExtension
    {
        readonly ICompilationExtension m_First;
        readonly ICompilationExtension m_Second;

        CompositeCompilationExtension(ICompilationExtension first, ICompilationExtension second)
        {
            m_First = first;
            m_Second = second;
        }

        public static ICompilationExtension Combine(ICompilationExtension first, ICompilationExtension second)
        {
            if (first == null)
                return second;

            if (second == null)
                return first;

            return new CompositeCompilationExtension(first, second);
        }

        public string[] GetCompilerExtraAssemblyPaths(bool isEditor, string assemblyPathName) =>
            Concat(m_First.GetCompilerExtraAssemblyPaths(isEditor, assemblyPathName),
                m_Second.GetCompilerExtraAssemblyPaths(isEditor, assemblyPathName));

        public string[] GetWindowsMetadataReferences() =>
            Concat(m_First.GetWindowsMetadataReferences(), m_Second.GetWindowsMetadataReferences());

        public string[] GetAdditionalAssemblyReferences() =>
            Concat(m_First.GetAdditionalAssemblyReferences(), m_Second.GetAdditionalAssemblyReferences());

        public string[] GetAdditionalDefines() =>
            Concat(m_First.GetAdditionalDefines(), m_Second.GetAdditionalDefines());

        public string[] GetAdditionalEditorDefines() =>
            Concat(m_First.GetAdditionalEditorDefines(), m_Second.GetAdditionalEditorDefines());

        public string[] GetAdditionalSourceFiles() =>
            Concat(m_First.GetAdditionalSourceFiles(), m_Second.GetAdditionalSourceFiles());

        static bool IsEmpty(string[] values) => values == null || values.Length == 0;

        // Each side is left as it is; only the join is deduplicated.
        static string[] Concat(string[] first, string[] second)
        {
            if (IsEmpty(first))
                return second ?? Array.Empty<string>();

            if (IsEmpty(second))
                return first;

            var result = new List<string>(first.Length + second.Length);
            result.AddRange(first);

            foreach (var value in second)
            {
                if (!result.Contains(value))
                    result.Add(value);
            }

            return result.ToArray();
        }
    }
}
