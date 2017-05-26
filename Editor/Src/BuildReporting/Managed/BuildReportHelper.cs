// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Linq;
using UnityEditor.Utils;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using UnityEditorInternal;
using System;
using System.Text.RegularExpressions;
using Mono.Cecil;
using UnityEditor.Modules;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.BuildReporting
{
    internal static class BuildReportHelper
    {
        private static IBuildAnalyzer m_CachedAnalyzer;
        private static BuildTarget m_CachedAnalyzerTarget;

        private static IBuildAnalyzer GetAnalyzerForTarget(BuildTarget target)
        {
            if (m_CachedAnalyzerTarget == target)
                return m_CachedAnalyzer;

            m_CachedAnalyzer = ModuleManager.GetBuildAnalyzer(target);
            m_CachedAnalyzerTarget = target;
            return m_CachedAnalyzer;
        }

        [RequiredByNativeCode]
        public static void OnAddedExecutable(BuildReport report, int fileIndex)
        {
            var analyzer = GetAnalyzerForTarget(report.buildTarget);
            if (analyzer == null) return;

            analyzer.OnAddedExecutable(report, fileIndex);
        }
    }
}
