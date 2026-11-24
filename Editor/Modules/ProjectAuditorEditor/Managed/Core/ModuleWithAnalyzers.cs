// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal abstract class ModuleWithAnalyzers<T> : Module where T : ModuleAnalyzer
    {
        T[] m_Analyzers;

        protected T[] GetAnalyzers()
        {
            return m_Analyzers;
        }

        protected T[] GetCompatibleAnalyzers(AnalysisParams analysisParams)
        {
            foreach (var analyzer in m_Analyzers)
                analyzer.CacheParameters(analysisParams.DiagnosticParams);
            return m_Analyzers;
        }

        public override void Initialize()
        {
            base.Initialize();

            var analyzers = new List<T>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(T)))
            {
                if (type.IsAbstract)
                    continue;

                // Skip analyzers that have been migrated to the rules package, once an installed package version provides them
                var migratedAttribute = type.GetCustomAttribute<MigratedToRulesPackageAttribute>();
                if (migratedAttribute != null && migratedAttribute.IsProvidedByInstalledPackage())
                    continue;

                var moduleAnalyzer = (ModuleAnalyzer)Activator.CreateInstance(type);
                moduleAnalyzer.Initialize(RegisterDescriptor);
                analyzers.Add((T)moduleAnalyzer);
            }
            m_Analyzers = analyzers.ToArray();
        }
    }
}
