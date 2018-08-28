// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ParserStyleSheet = ExCSS.StyleSheet;
using ParserStyleRule = ExCSS.StyleRule;
using UnityStyleSheet = UnityEngine.StyleSheets.StyleSheet;
using UnityEngine.StyleSheets;
using ExCSS;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.StyleSheets
{
    abstract class StyleValueImporter
    {
        const string k_ResourcePathFunctionName = "resource";

        protected readonly AssetImportContext m_Context;
        protected readonly Parser m_Parser;
        protected readonly StyleSheetBuilder m_Builder;
        protected readonly StyleSheetImportErrors m_Errors;

        public StyleValueImporter(AssetImportContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            m_Context = context;
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
        }

        internal StyleValueImporter()
        {
            m_Context = null;
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
        }

        public string assetPath
        {
            get
            {
                Debug.Assert(m_Context != null);
                return m_Context.assetPath;
            }
        }

        // Allow overriding this in tests
        public virtual UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            m_Context.DependsOnSourceAsset(path);
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

        static readonly Uri s_ProjectRootUri = new UriBuilder("project", "").Uri;

        protected void VisitResourceFunction(GenericFunction funcTerm)
        {
            var argTerm = funcTerm.Arguments.FirstOrDefault() as PrimitiveTerm;
            if (argTerm == null)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, funcTerm.Name);
                return;
            }

            string path = argTerm.Value as string;
            m_Builder.AddValue(path, StyleValueType.ResourcePath);
        }

        protected void VisitUrlFunction(PrimitiveTerm term)
        {
            string path = term.Value as string;
            if (string.IsNullOrEmpty(path))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURILocation, "");
                return;
            }

            Uri absoluteUri = null;
            // Always treat URIs starting with "/" as implicit project schemes
            if (path.StartsWith("/"))
            {
                var builder = new UriBuilder(s_ProjectRootUri.Scheme, "", 0, path);
                absoluteUri = builder.Uri;
            }
            else if (Uri.TryCreate(path, UriKind.Absolute, out absoluteUri) == false)
            {
                // Resolve a relative URI compared to current file
                Uri assetPathUri = new Uri(s_ProjectRootUri, assetPath);

                if (Uri.TryCreate(assetPathUri, path, out absoluteUri) == false)
                {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURILocation, path);
                    return;
                }
            }
            else if (absoluteUri.Scheme != s_ProjectRootUri.Scheme)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURIScheme, absoluteUri.Scheme);
                return;
            }

            string projectRelativePath = absoluteUri.AbsolutePath;

            // Remove any leading "/" as this now used as a path relative to the current directory
            if (projectRelativePath.StartsWith("/"))
            {
                projectRelativePath = projectRelativePath.Substring(1);
            }

            if (string.IsNullOrEmpty(projectRelativePath) || !File.Exists(projectRelativePath))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURIProjectAssetPath, projectRelativePath);
            }
            else
            {
                UnityEngine.Object asset = DeclareDependencyAndLoad(projectRelativePath);

                if (asset is Texture2D || asset is Font)
                {
                    m_Builder.AddValue(asset);
                }
                else
                {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURIProjectAssetType, string.Format("Invalid asset type {0}, only Font and Texture2D are supported", asset.GetType().Name));
                }
            }
        }

        protected void VisitValue(Term term)
        {
            var primitiveTerm = term as PrimitiveTerm;
            var colorTerm = term as HtmlColor;
            var funcTerm = term as GenericFunction;
            var termList = term as TermList;

            if (term == PrimitiveTerm.Inherit)
            {
                m_Builder.AddValue(StyleValueKeyword.Inherit);
            }
            else if (primitiveTerm != null)
            {
                string rawStr = term.ToString();

                switch (primitiveTerm.PrimitiveType)
                {
                    case UnitType.Pixel:
                    case UnitType.Number:
                        float? floatValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(floatValue.Value);
                        break;
                    case UnitType.Ident:
                        StyleValueKeyword keyword;
                        if (TryParseKeyword(rawStr, out keyword))
                        {
                            m_Builder.AddValue(keyword);
                        }
                        else
                        {
                            m_Builder.AddValue(rawStr, StyleValueType.Enum);
                        }
                        break;
                    case UnitType.String:
                        string unquotedStr = rawStr.Trim('\'', '\"');
                        m_Builder.AddValue(unquotedStr, StyleValueType.String);
                        break;
                    case UnitType.Uri:
                        VisitUrlFunction(primitiveTerm);
                        break;
                    default:
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedUnit, primitiveTerm.ToString());
                        return;
                }
            }
            else if (colorTerm != null)
            {
                var color = new Color((float)colorTerm.R / 255.0f, (float)colorTerm.G / 255.0f, (float)colorTerm.B / 255.0f, (float)colorTerm.A / 255.0f);
                m_Builder.AddValue(color);
            }
            else if (funcTerm != null)
            {
                if (funcTerm.Name == k_ResourcePathFunctionName)
                {
                    VisitResourceFunction(funcTerm);
                }
                else
                {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedFunction, funcTerm.Name);
                }
            }
            else if (termList != null)
            {
                foreach (Term childTerm in termList)
                {
                    VisitValue(childTerm);
                }
            }
            else
            {
                m_Errors.AddInternalError(term.GetType().Name);
            }
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
    }

    class StyleSheetImporterImpl : StyleValueImporter
    {
        public StyleSheetImporterImpl(AssetImportContext context) : base(context)
        {
        }

        protected void OnImportError(StyleSheetImportErrors errors)
        {
            foreach (string importError in errors.FormatErrors())
            {
                m_Context.LogImportError(importError);
            }
        }

        protected void OnImportSuccess(UnityStyleSheet asset)
        {
            m_Context.AddObjectToAsset("stylesheet", asset);
            m_Context.SetMainObject(asset);
        }

        public void Import(UnityStyleSheet asset, string contents)
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
                    Debug.LogException(exc);
                    m_Errors.AddInternalError(exc.StackTrace);
                }
            }

            if (!m_Errors.hasErrors)
            {
                m_Builder.BuildTo(asset);
                OnImportSuccess(asset);
            }
            else
            {
                OnImportError(m_Errors);
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
                    VisitValue(property.Term);

                    m_Builder.EndProperty();
                }

                m_Builder.EndRule();
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
