// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ExCSS;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.UIElements.StyleSheets;
using ParserStyleSheet = ExCSS.StyleSheet;
using ParserStyleRule = ExCSS.StyleRule;
using UnityStyleSheet = UnityEngine.StyleSheets.StyleSheet;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    [ScriptedImporter(3, "uss", -20)]
    class StyleSheetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var importer = new StyleSheetImporter();
            string contents = File.ReadAllText(ctx.assetPath);
            UnityStyleSheet asset = ScriptableObject.CreateInstance<UnityStyleSheet>();
            asset.hideFlags = HideFlags.NotEditable;
            importer.Import(asset, contents, ctx.assetPath);
            ctx.AddObjectToAsset("stylesheet", asset);

            // Force the pre processor to rebuild its list of referenced asset paths
            // as paths may have changed with this import.
            StyleSheetAssetPostprocessor.ClearReferencedAssets();

            // Clear the style cache to force all USS rules to recompute
            // and subsequently reload/reimport all images.
            StyleContext.ClearStyleCache();
        }


        Parser m_Parser;
        const string k_ResourcePathFunctionName = "resource";

        StyleSheetBuilder m_Builder;
        StyleSheetImportErrors m_Errors;

        public StyleSheetImporter()
        {
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
        }

        public void Import(UnityStyleSheet asset, string contents, string contextAssetPath)
        {
            ParserStyleSheet styleSheet = m_Parser.Parse(contents);

            if (styleSheet.Errors.Count > 0)
            {
                foreach (StylesheetParseError error in styleSheet.Errors)
                {
                    m_Errors.AddSyntaxError(error.ToString());
                }
            }
            else
            {
                try
                {
                    VisitSheet(styleSheet);
                }
                catch (Exception exc)
                {
                    m_Errors.AddInternalError(exc.Message);
                }
            }

            if (m_Errors.hasErrors)
            {
                foreach (string importError in m_Errors.FormatErrors())
                {
                    Debug.LogErrorFormat(importError);
                }
            }
            else
            {
                m_Builder.BuildTo(asset);
            }
        }

        void VisitSheet(ParserStyleSheet styleSheet)
        {
            foreach (ParserStyleRule rule in styleSheet.StyleRules)
            {
                m_Builder.BeginRule(rule.Line);

                // Note: we must rely on recursion to correctly handle parser types here
                VisitBaseSelector(rule.Selector);

                foreach (Property property in  rule.Declarations)
                {
                    m_Builder.BeginProperty(property.Name);

                    // Note: we must rely on recursion to correctly handle parser types here
                    VisitValue(m_Errors, m_Builder, property.Term);

                    m_Builder.EndProperty();
                }

                m_Builder.EndRule();
            }
        }

        internal static void VisitValue(StyleSheetImportErrors errors, StyleSheetBuilder ssb, Term term)
        {
            var primitiveTerm = term as PrimitiveTerm;
            var colorTerm = term as HtmlColor;
            var funcTerm = term as GenericFunction;
            var termList = term as TermList;

            if (term == PrimitiveTerm.Inherit)
            {
                ssb.AddValue(StyleValueKeyword.Inherit);
            }
            else if (primitiveTerm != null)
            {
                string rawStr = term.ToString();

                switch (primitiveTerm.PrimitiveType)
                {
                    case UnitType.Pixel:
                    case UnitType.Number:
                        float? floatValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        ssb.AddValue(floatValue.Value);
                        break;
                    case UnitType.Ident:
                        StyleValueKeyword keyword;
                        if (TryParseKeyword(rawStr, out keyword))
                        {
                            ssb.AddValue(keyword);
                        }
                        else
                        {
                            ssb.AddValue(rawStr, StyleValueType.Enum);
                        }
                        break;
                    case UnitType.String:
                        string unquotedStr = rawStr.Trim('\'', '\"');
                        ssb.AddValue(unquotedStr, StyleValueType.String);
                        break;
                    default:
                        errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedUnit, primitiveTerm.ToString());
                        return;
                }
            }
            else if (colorTerm != null)
            {
                var color = new Color((float)colorTerm.R / 255.0f, (float)colorTerm.G / 255.0f, (float)colorTerm.B / 255.0f, (float)colorTerm.A / 255.0f);
                ssb.AddValue(color);
            }
            else if (funcTerm != null)
            {
                primitiveTerm = funcTerm.Arguments.FirstOrDefault() as PrimitiveTerm;
                if (funcTerm.Name == k_ResourcePathFunctionName && primitiveTerm != null)
                {
                    string path = primitiveTerm.Value as string;
                    ssb.AddValue(path, StyleValueType.ResourcePath);
                }
                else
                {
                    errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedFunction, funcTerm.Name);
                }
            }
            else if (termList != null)
            {
                foreach (Term childTerm in termList)
                {
                    VisitValue(errors, ssb, childTerm);
                }
                return;
            }
            else
            {
                errors.AddInternalError(term.GetType().Name);
            }
        }

        void VisitBaseSelector(BaseSelector selector)
        {
            var selectorList = selector as AggregateSelectorList;
            if (selectorList != null)
            {
                VisitSelectorList(selectorList);
                return;
            }

            var complexSelector = selector as ComplexSelector;
            if (complexSelector != null)
            {
                VisitComplexSelector(complexSelector);
                return;
            }

            var simpleSelector = selector as SimpleSelector;
            if (simpleSelector != null)
            {
                VisitSimpleSelector(simpleSelector.ToString());
            }
        }

        void VisitSelectorList(AggregateSelectorList selectorList)
        {
            // OR selectors, just create an entry for each of them
            if (selectorList.Delimiter == ",")
            {
                foreach (BaseSelector selector in selectorList)
                {
                    VisitBaseSelector(selector);
                }
            }
            // Work around a strange parser issue where sometimes simple selectors
            // are wrapped inside SelectorList with no delimiter
            else if (selectorList.Delimiter == string.Empty)
            {
                VisitSimpleSelector(selectorList.ToString());
            }
            else
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidSelectorListDelimiter, selectorList.Delimiter);
            }
        }

        void VisitComplexSelector(ComplexSelector complexSelector)
        {
            int fullSpecificity = CSSSpec.GetSelectorSpecificity(complexSelector.ToString());

            if (fullSpecificity == 0)
            {
                m_Errors.AddInternalError("Failed to calculate selector specificity " + complexSelector);
                return;
            }

            using (m_Builder.BeginComplexSelector(fullSpecificity))
            {
                StyleSelectorRelationship relationShip = StyleSelectorRelationship.None;

                foreach (CombinatorSelector selector in complexSelector)
                {
                    StyleSelectorPart[] parts;

                    string simpleSelector = ExtractSimpleSelector(selector.Selector);

                    if (string.IsNullOrEmpty(simpleSelector))
                    {
                        m_Errors.AddInternalError("Expected simple selector inside complex selector " + simpleSelector);
                        return;
                    }

                    if (CheckSimpleSelector(simpleSelector, out parts))
                    {
                        m_Builder.AddSimpleSelector(parts, relationShip);

                        // Read relation for next element
                        switch (selector.Delimiter)
                        {
                            case Combinator.Child:
                                relationShip = StyleSelectorRelationship.Child;
                                break;
                            case Combinator.Descendent:
                                relationShip = StyleSelectorRelationship.Descendent;
                                break;
                            default:
                                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidComplexSelectorDelimiter, complexSelector.ToString());
                                return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        void VisitSimpleSelector(string selector)
        {
            StyleSelectorPart[] parts;
            if (CheckSimpleSelector(selector, out parts))
            {
                int specificity = CSSSpec.GetSelectorSpecificity(parts);

                if (specificity == 0)
                {
                    m_Errors.AddInternalError("Failed to calculate selector specificity " + selector);
                    return;
                }

                using (m_Builder.BeginComplexSelector(specificity))
                {
                    m_Builder.AddSimpleSelector(parts, StyleSelectorRelationship.None);
                }
            }
        }

        string ExtractSimpleSelector(BaseSelector selector)
        {
            SimpleSelector simpleSelector = selector as SimpleSelector;

            if (simpleSelector != null)
            {
                return selector.ToString();
            }

            AggregateSelectorList selectorList = selector as AggregateSelectorList;

            // Work around a strange parser issue where sometimes simple selectors
            // are wrapped inside SelectorList with no delimiter
            if (selectorList != null && selectorList.Delimiter == string.Empty)
            {
                return selectorList.ToString();
            }

            return string.Empty;
        }

        static Dictionary<string, StyleValueKeyword> s_NameCache;

        static bool TryParseKeyword(string rawStr, out StyleValueKeyword value)
        {
            if (s_NameCache == null)
            {
                s_NameCache = new Dictionary<string, StyleValueKeyword>();
                foreach (StyleValueKeyword kw in Enum.GetValues(typeof(StyleValueKeyword)))
                {
                    s_NameCache[kw.ToString().ToLower()] = kw;
                }
            }
            return s_NameCache.TryGetValue(rawStr.ToLower(), out value);
        }

        bool CheckSimpleSelector(string selector, out StyleSelectorPart[] parts)
        {
            if (!CSSSpec.ParseSelector(selector, out parts))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, selector);
                return false;
            }
            if (parts.Any(p => p.type == StyleSelectorType.Unknown))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, selector);
                return false;
            }
            if (parts.Any(p => p.type == StyleSelectorType.RecursivePseudoClass))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.RecursiveSelectorDetected, selector);
                return false;
            }
            return true;
        }

    }
}
