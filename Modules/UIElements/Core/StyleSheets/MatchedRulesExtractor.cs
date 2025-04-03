// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal struct MatchedRule
    {
        public readonly SelectorMatchRecord matchRecord;
        public readonly string displayPath;
        public readonly int lineNumber;
        public readonly string fullPath;

        public MatchedRule(SelectorMatchRecord matchRecord, string path)
            : this()
        {
            this.matchRecord = matchRecord;
            fullPath = path;
            lineNumber = matchRecord.complexSelector.rule.line;
            if (string.IsNullOrEmpty(fullPath))
            {
                displayPath = matchRecord.sheet.name + ":" + lineNumber;
            }
            else
            {
                if (fullPath == "Library/unity editor resources")
                    displayPath = matchRecord.sheet.name + ":" + lineNumber;
                else
                    displayPath = Path.GetFileName(fullPath) + ":" + lineNumber;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = matchRecord.GetHashCode();
                hashCode = (hashCode * 397) ^ (displayPath != null ? displayPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ lineNumber;
                hashCode = (hashCode * 397) ^ (fullPath != null ? fullPath.GetHashCode() : 0);
                return hashCode;
            }
        }

        private sealed class LineNumberFullPathEqualityComparer : IEqualityComparer<MatchedRule>
        {
            public bool Equals(MatchedRule x, MatchedRule y)
            {
                return x.lineNumber == y.lineNumber && string.Equals(x.fullPath, y.fullPath) && string.Equals(x.displayPath, y.displayPath);
            }

            public int GetHashCode(MatchedRule obj)
            {
                return obj.GetHashCode();
            }
        }

        public static IEqualityComparer<MatchedRule> lineNumberFullPathComparer = new LineNumberFullPathEqualityComparer();
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class MatchedRulesExtractor
    {
        private static readonly Func<StyleSheet, string> k_defaultGetPath = ss => ss.name;

        private Func<StyleSheet, string> m_GetStyleSheetPath;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal HashSet<MatchedRule> selectedElementRules = new HashSet<MatchedRule>(MatchedRule.lineNumberFullPathComparer);
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal HashSet<string> selectedElementStylesheets = new HashSet<string>();
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal List<SelectorMatchRecord> matchRecords = new List<SelectorMatchRecord>();

        public IEnumerable<MatchedRule> GetMatchedRules() => selectedElementRules;

        public Func<StyleSheet, string> getStyleSheetPath
        {
            get => m_GetStyleSheetPath ?? k_defaultGetPath;
            set => m_GetStyleSheetPath = value;
        }

        public MatchedRulesExtractor(Func<StyleSheet, string> getAssetPath)
        {
            getStyleSheetPath = getAssetPath;
        }

        private void SetupParents(VisualElement target, StyleMatchingContext matchingContext)
        {
            if (target.hierarchy.parent != null)
                SetupParents(target.hierarchy.parent, matchingContext);

            // We populate the ancestor filter in order for the Bloom filter detection to work.
            matchingContext.ancestorFilter.PushElement(target);

            if (target.styleSheetList == null)
                return;

            foreach (StyleSheet sheet in target.styleSheetList)
            {
                // Skip deleted style sheets
                if (sheet == null)
                    continue;

                string name = getStyleSheetPath(sheet);
                if (string.IsNullOrEmpty(name) || sheet.isDefaultStyleSheet)
                    name = sheet.name;

                void RecursivePrintStyleSheetNames(StyleSheet importedSheet)
                {
                    for (int i = 0; i < importedSheet.imports.Length; i++)
                    {
                        var thisImportedSheet = importedSheet.imports[i].styleSheet;
                        if (thisImportedSheet != null)
                        {
                            name += "\n(" + thisImportedSheet.name + ")";
                            matchingContext.AddStyleSheet(thisImportedSheet);
                            RecursivePrintStyleSheetNames(thisImportedSheet);
                        }
                    }
                }

                RecursivePrintStyleSheetNames(sheet);

                selectedElementStylesheets.Add(name);
                matchingContext.AddStyleSheet(sheet);
            }
        }

        public void FindMatchingRules(VisualElement target)
        {
            var matchingContext = new StyleMatchingContext((element, info) => {}) { currentElement = target };
            SetupParents(target, matchingContext);

            matchRecords.Clear();
            StyleSelectorHelper.FindMatches(matchingContext, matchRecords);

            matchRecords.Sort(SelectorMatchRecord.Compare);

            foreach (var record in matchRecords)
            {
                selectedElementRules.Add(new MatchedRule(record, getStyleSheetPath(record.sheet)));
            }
        }

        public void Clear()
        {
            selectedElementRules.Clear();
            selectedElementStylesheets.Clear();
            matchRecords.Clear();
        }
    }
}
