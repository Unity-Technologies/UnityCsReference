// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ParserStyleSheet = ExCSS.StyleSheet;
using ParserStyleRule = ExCSS.StyleRule;
using UnityStyleSheet = UnityEngine.UIElements.StyleSheet;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using ExCSS;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.StyleSheets
{
    abstract class StyleValueImporter
    {
        const string k_ResourcePathFunctionName = "resource";
        const string k_VariableFunctionName = "var";

        protected readonly AssetImportContext m_Context;
        protected readonly Parser m_Parser;
        protected readonly StyleSheetBuilder m_Builder;
        protected readonly StyleSheetImportErrors m_Errors;
        protected readonly StyleValidator m_Validator;
        protected string m_AssetPath;

        public StyleValueImporter(AssetImportContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            m_Context = context;
            m_AssetPath = context.assetPath;
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
            m_Validator = new StyleValidator();
        }

        internal StyleValueImporter()
        {
            m_Context = null;
            m_AssetPath = null;
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
            m_Validator = new StyleValidator();
        }

        public bool disableValidation { get; set; }

        // Used by test
        public StyleSheetImportErrors importErrors { get { return m_Errors; } }

        public string assetPath => m_AssetPath;

        // Allow overriding this in tests
        public virtual UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            m_Context.DependsOnSourceAsset(path);
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

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

        static StyleSheetImportErrorCode ConvertErrorCode(URIValidationResult result)
        {
            switch (result)
            {
                case URIValidationResult.InvalidURILocation:
                    return StyleSheetImportErrorCode.InvalidURILocation;
                case URIValidationResult.InvalidURIScheme:
                    return StyleSheetImportErrorCode.InvalidURIScheme;
                case URIValidationResult.InvalidURIProjectAssetPath:
                    return StyleSheetImportErrorCode.InvalidURIProjectAssetPath;
                default:
                    return StyleSheetImportErrorCode.Internal;
            }
        }

        protected void VisitUrlFunction(PrimitiveTerm term)
        {
            string path = (string)term.Value;

            string projectRelativePath, errorMessage;

            URIValidationResult result = URIHelpers.ValidAssetURL(assetPath, path, out errorMessage, out projectRelativePath);

            if (result != URIValidationResult.OK)
            {
                m_Builder.AddValue(path, StyleValueType.MissingAssetReference);
                m_Errors.AddValidationWarning(errorMessage, m_Builder.currentProperty.line);
            }
            else
            {
                UnityEngine.Object asset = DeclareDependencyAndLoad(projectRelativePath);

                bool isTexture = asset is Texture2D;

                if (isTexture || asset is Font || asset is VectorImage)
                {
                    // Looking suffixed images files only
                    if (isTexture)
                    {
                        string hiResImageLocation = URIHelpers.InjectFileNameSuffix(projectRelativePath, "@2x");

                        if (File.Exists(hiResImageLocation))
                        {
                            UnityEngine.Object hiResImage = DeclareDependencyAndLoad(hiResImageLocation);

                            if (hiResImage is Texture2D)
                            {
                                m_Builder.AddValue(new ScalableImage() { normalImage = asset as Texture2D, highResolutionImage = hiResImage as Texture2D });
                            }
                            else
                            {
                                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidHighResolutionImage, string.Format("Invalid asset type {0}, only Texture2D is supported for variants with @2x suffix", asset.GetType().Name));
                            }
                            return;
                        }
                        // If we didn't find an high res variant, tell ADB we depend on that potential file existing
                        DeclareDependencyAndLoad(hiResImageLocation);
                    }
                    m_Builder.AddValue(asset);
                }
                else
                {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidURIProjectAssetType, string.Format("Invalid asset type {0}, only Font, Texture2D and VectorImage are supported", asset.GetType().Name));
                }
            }
        }

        private bool ValidateFunction(GenericFunction term, out StyleValueFunction func)
        {
            func = StyleValueFunction.Unknown;
            if (term.Arguments.Length == 0)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, term.Name);
                return false;
            }

            if (term.Name == k_VariableFunctionName)
            {
                func = StyleValueFunction.Var;
                return ValidateVarFunction(term);
            }

            try
            {
                func = StyleValueFunctionExtension.FromUssString(term.Name);
            }
            catch (Exception)
            {
                var prop = m_Builder.currentProperty;
                m_Errors.AddValidationWarning($"Unknown function {term.Name} in declaration {prop.name}: {term.Name}", prop.line);
                return false;
            }

            return true;
        }

        private bool ValidateVarFunction(GenericFunction term)
        {
            var argc = term.Arguments.Length;
            var arg = term.Arguments[0];

            bool foundVar = false;
            bool foundComma = false;
            for (int i = 0; i < argc; i++)
            {
                arg = term.Arguments[i];
                if (arg.GetType() == typeof(Whitespace))
                    continue;

                // First arg is always a variable
                if (!foundVar)
                {
                    var variableTerm = term.Arguments[i] as PrimitiveTerm;
                    string varName = variableTerm?.Value as string;
                    if (string.IsNullOrEmpty(varName))
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, "Variable name is missing");
                        return false;
                    }
                    else if (!varName.StartsWith("--"))
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, $"Variable {varName} is missing '--' prefix");
                        return false;
                    }
                    if (varName.Length < 3)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, "Variable name is empty");
                        return false;
                    }

                    foundVar = true;
                }
                else if (arg.GetType() == typeof(Comma))
                {
                    if (foundComma)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, "Too many function arguments");
                        return false;
                    }

                    foundComma = true;

                    ++i;
                    if (i >= argc)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, "Empty function argument");
                        return false;
                    }
                }
                else if (!foundComma)
                {
                    string msg = "Expected ','";
                    while (arg.GetType() == typeof(Whitespace) && i + 1 < argc)
                    {
                        arg = term.Arguments[++i];
                    }

                    if (arg.GetType() != typeof(Whitespace))
                        msg = $"{msg} got {arg}";

                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, msg);
                    return false;
                }
            }

            return true;
        }

        protected void VisitValue(Term term)
        {
            var primitiveTerm = term as PrimitiveTerm;
            var colorTerm = term as HtmlColor;
            var funcTerm = term as GenericFunction;
            var termList = term as TermList;
            var commaTerm = term as Comma;
            var wsTerm = term as Whitespace;

            if (term == PrimitiveTerm.Inherit)
            {
                m_Builder.AddValue(StyleValueKeyword.Inherit);
            }
            else if (primitiveTerm != null)
            {
                string rawStr = term.ToString();

                switch (primitiveTerm.PrimitiveType)
                {
                    case UnitType.Number:
                        float? floatValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(floatValue.Value);
                        break;
                    case UnitType.Pixel:
                        float? pixelValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(pixelValue.Value, Dimension.Unit.Pixel));
                        break;
                    case UnitType.Percentage:
                        float? percentValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(percentValue.Value, Dimension.Unit.Percent));
                        break;
                    case UnitType.Ident:
                        StyleValueKeyword keyword;
                        if (TryParseKeyword(rawStr, out keyword))
                        {
                            m_Builder.AddValue(keyword);
                        }
                        else if (rawStr.StartsWith("--"))
                        {
                            m_Builder.AddValue(rawStr, StyleValueType.Variable);
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
                    StyleValueFunction func;
                    if (!ValidateFunction(funcTerm, out func))
                        return;

                    m_Builder.AddValue(func);
                    m_Builder.AddValue(funcTerm.Arguments.Count(a => !(a is Whitespace)));
                    foreach (var arg in funcTerm.Arguments)
                        VisitValue(arg);
                }
            }
            else if (termList != null)
            {
                foreach (Term childTerm in termList)
                {
                    VisitValue(childTerm);
                }
            }
            else if (commaTerm != null)
            {
                m_Builder.AddValue(commaTerm.ToString(), StyleValueType.FunctionSeparator);
            }
            else if (wsTerm != null)
            {
                // skip
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

    internal class StyleSheetImporterImpl : StyleValueImporter
    {
        public StyleSheetImporterImpl(AssetImportContext context) : base(context)
        {
        }

        public StyleSheetImporterImpl() : base()
        {
        }

        protected virtual void OnImportError(StyleSheetImportErrors errors)
        {
            if (m_Context == null)
                return;

            foreach (var e in errors)
            {
                if (e.isWarning)
                {
                    m_Context.LogImportWarning(e.ToString(), e.assetPath, e.line);
                }
                else
                {
                    m_Context.LogImportError(e.ToString(), e.assetPath, e.line);
                }
            }
        }

        protected virtual void OnImportSuccess(UnityStyleSheet asset)
        {
        }

        public void Import(UnityStyleSheet asset, string contents)
        {
            ParserStyleSheet styleSheet = m_Parser.Parse(contents);
            ImportParserStyleSheet(asset, styleSheet);

            var h = new Hash128();
            byte[] b = Encoding.UTF8.GetBytes(contents);
            if (b.Length > 0)
            {
                HashUtilities.ComputeHash128(b, ref h);
            }
            asset.contentHash = h.GetHashCode();
        }

        protected void ImportParserStyleSheet(UnityStyleSheet asset, ParserStyleSheet styleSheet)
        {
            m_Errors.assetPath = assetPath;

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

            bool success = !m_Errors.hasErrors;
            if (success)
            {
                m_Builder.BuildTo(asset);
                OnImportSuccess(asset);
            }

            if (!success || m_Errors.hasWarning)
            {
                OnImportError(m_Errors);
            }
        }

        void ValidateProperty(Property property)
        {
            if (!disableValidation)
            {
                var name = property.Name;
                var value = property.Term.ToString();
                var result = m_Validator.ValidateProperty(name, value);
                if (!result.success)
                {
                    string msg = $"{result.message}\n    {name}: {value}";
                    if (!string.IsNullOrEmpty(result.hint))
                        msg = $"{msg} -> {result.hint}";

                    m_Errors.AddValidationWarning(msg, property.Line);
                }
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
                    ValidateProperty(property);

                    m_Builder.BeginProperty(property.Name, property.Line);

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
