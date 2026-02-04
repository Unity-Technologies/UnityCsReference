// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExCSS;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using ParserStyleSheet = ExCSS.Stylesheet;
using UnityStyleSheet = UnityEngine.UIElements.StyleSheet;

namespace UnityEditor.UIElements.StyleSheets
{
    abstract class StyleValueImporter
    {
        private static StyleSheetImportGlossary s_Glossary;

        internal static StyleSheetImportGlossary glossary => s_Glossary ?? (s_Glossary = new StyleSheetImportGlossary());

        static readonly Dictionary<string, Dimension.Unit> s_UnitNameToDimensionUnit = new()
        {
            { UnitNames.Px, Dimension.Unit.Pixel },
            { UnitNames.Percent, Dimension.Unit.Percent },
            { UnitNames.S, Dimension.Unit.Second },
            { UnitNames.Ms, Dimension.Unit.Millisecond },
            { UnitNames.Deg, Dimension.Unit.Degree },
            { UnitNames.Grad, Dimension.Unit.Gradian },
            { UnitNames.Rad, Dimension.Unit.Radian },
            { UnitNames.Turn, Dimension.Unit.Turn },
        };

        static Dictionary<string, StyleValueKeyword> s_NameCache;

        const string k_ResourcePathFunctionName = "resource";

        protected readonly AssetImportContext m_Context;
        protected readonly UnityStylesheetParser m_Parser;
        protected readonly StyleSheetBuilder m_Builder;
        internal readonly StyleSheetImportErrors m_Errors;
        protected readonly StyleValidator m_Validator;
        protected string m_AssetPath;
        protected int m_CurrentLine;

        readonly StringBuilder m_StringBuilder = new StringBuilder();

        public StyleValueImporter(AssetImportContext context)
        {
            if (context == null)
                throw new System.ArgumentNullException(nameof(context));

            m_Context = context;
            m_AssetPath = context.assetPath;
            m_Parser = new UnityStylesheetParser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors()
            {
                assetPath = context.assetPath
            };
            m_Validator = new StyleValidator();
        }

        internal StyleValueImporter()
        {
            m_Context = null;
            m_AssetPath = null;
            m_Parser = new UnityStylesheetParser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
            m_Validator = new StyleValidator();
        }

        static StyleValueImporter()
        {
            // Add custom psuedo class support to our validation
            PseudoClassSelectorFactory.Selectors["selected"] = PseudoClassSelector.Create("selected");
        }

        /// <summary>
        /// ExCSS StylesheetParser with error capturing.
        /// </summary>
        protected class UnityStylesheetParser : StylesheetParser
        {
            public readonly List<TokenizerError> errors = new List<TokenizerError>();

            public UnityStylesheetParser() : base(
                // So we can parse unknown rules and declarations, supports our variables etc.
                includeUnknownRules: true,
                includeUnknownDeclarations: true,
                tolerateInvalidValues: true,

                // Supports unknown psuedo class and psuedo element names
                tolerateInvalidSelectors: true,

                // Preserve duplicate properties. We may want to remove this support in the future.
                preserveDuplicateProperties: true,

                // We added this support
                // Prevents ExCSS from expanding properties like "margin: 1px 2px" into "margin-top: 1px; margin-right: 2px; margin-bottom: 1px; margin-left: 2px"
                expandShorthandProperties: false)
            {
                ErrorHandler = HandleError;
            }

            public override Stylesheet Parse(string content)
            {
                errors.Clear();
                return base.Parse(content);
            }

            void HandleError(object sender, TokenizerError tokenizerError)
            {
                errors.Add(tokenizerError);
            }
        }

        public bool disableValidation { get; set; }

        // Used by test
        public StyleSheetImportErrors importErrors { get { return m_Errors; } }

        public string assetPath => m_AssetPath;

        public virtual UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            return DeclareDependencyAndLoad(path, null);
        }

        private static readonly string kThemePrefix = $"{ThemeRegistry.kThemeScheme}://";

        // Allow overriding this in tests
        public virtual UnityEngine.Object DeclareDependencyAndLoad(string path, string subAssetPath)
        {
            if (path.StartsWith(kThemePrefix))
            {
                var themeName = path.Substring(kThemePrefix.Length);

                if (!ThemeRegistry.themes.TryGetValue(themeName, out var themePath))
                    return null;

                var themeAssetToCopy = EditorGUIUtility.Load(themePath);
                Debug.Assert(themeAssetToCopy != null, $"Theme not found searching for '{themeName}' at <{themePath}>.");

                if (themeAssetToCopy != null)
                {
                    var clonedAssets = DeepCopyAsset(themeAssetToCopy);

                    if (clonedAssets.Count > 0)
                    {
                        clonedAssets[0].name = themeName;
                        int assetIndex = 0;
                        foreach (var clonedAsset in clonedAssets)
                            m_Context.AddObjectToAsset($"asset {assetIndex++}: clonedAsset.name", clonedAsset);

                        return clonedAssets[0];
                    }
                }
                return null;
            }

            m_Context?.DependsOnSourceAsset(path);

            if (string.IsNullOrEmpty(subAssetPath))
                return AssetDatabase.LoadMainAssetAtPath(path);

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o == mainAsset)
                    continue; // We're looking for a sub-asset here

                if (o.name == subAssetPath)
                    return o;
            }

            // We sometimes include the main asset name in the sub-asset name. (UUM-49355)
            if (mainAsset != null && mainAsset.name == subAssetPath)
                return mainAsset;

            return null;
        }

        private static UnityEngine.Object LoadResource(string path)
        {
            return StyleSheetResourceUtil.LoadResource(path, typeof(UnityEngine.Object));
        }

        private struct StoredAsset
        {
            public Object resource;
            public ScalableImage si;
            public int index;
        };

        internal static List<UnityEngine.Object> DeepCopyAsset(UnityEngine.Object original)
        {
            var originalStylesheet = original as UnityEngine.UIElements.StyleSheet;
            if (originalStylesheet == null)
                return new List<UnityEngine.Object>();

            var clonedStylesheet = ScriptableObject.Instantiate(originalStylesheet) as UnityEngine.UIElements.StyleSheet;

            var addedAssets = new Dictionary<UnityEngine.Object, List<UnityEngine.Object>>();
            var newAssets = new List<UnityEngine.Object>();
            var newScalableImages = new List<ScalableImage>();

            // Clone assets
            for (int i = 0; i < clonedStylesheet.assets.Length; ++i)
            {
                var asset = clonedStylesheet.assets[i];

                // The first cloned asset is the "main" asset that should be added to the stylesheet's list
                List<UnityEngine.Object> clonedAssets = null;
                if (!addedAssets.TryGetValue(asset, out clonedAssets))
                {
                    clonedAssets = CloneAsset(asset);
                    if (clonedAssets.Count > 0)
                        addedAssets[asset] = clonedAssets;
                }

                if (clonedAssets?.Count > 0)
                    newAssets.Add(clonedAssets[0]);
            }

            // Clone scalable images
            for (int i = 0; i < clonedStylesheet.scalableImages.Length; ++i)
            {
                var si = clonedStylesheet.scalableImages[i];

                List<UnityEngine.Object> clonedImages = null;
                if (!addedAssets.TryGetValue(si.normalImage, out clonedImages))
                {
                    var tex = CloneAsset(si.normalImage);
                    var texHighRes = CloneAsset(si.highResolutionImage);
                    if (tex.Count > 0 && texHighRes.Count > 0)
                    {
                        clonedImages = new List<UnityEngine.Object> { tex[0], texHighRes[0] };
                        addedAssets[si.normalImage] = clonedImages;
                    }
                }

                if (clonedImages?.Count > 0)
                    newScalableImages.Add(new ScalableImage()
                    {
                        normalImage = clonedImages[0] as Texture2D,
                        highResolutionImage = clonedImages[1] as Texture2D
                    });
            }

            // Scan through resource paths and convert them to asset references
            var assetPaths = new Dictionary<string, StoredAsset>();
            var scalableImagePaths = new Dictionary<string, StoredAsset>();

            foreach (var rule in clonedStylesheet.rules)
            {
                foreach (var prop in rule.properties)
                {
                    for (int valueIndex = 0; valueIndex < prop.values.Length; ++valueIndex)
                    {
                        var value = prop.values[valueIndex];
                        if (value.valueType != StyleValueType.ResourcePath)
                            continue;

                        var resourcePath = clonedStylesheet.ReadResourcePath(value);
                        var path = resourcePath.path;

                        bool isResource = false;
                        int assetIndex = -1;
                        bool isScalableImage = false;
                        int scalableImageIndex = -1;

                        StoredAsset sa;
                        if (scalableImagePaths.TryGetValue(path, out sa))
                        {
                            scalableImageIndex = sa.index;
                            isScalableImage = true;
                        }
                        else if (assetPaths.TryGetValue(path, out sa))
                        {
                            assetIndex = sa.index;
                            isResource = true;
                        }
                        else
                        {
                            var asset = LoadResource(path);
                            var clonedAssets = CloneAsset(asset);
                            addedAssets[asset] = clonedAssets;

                            if (asset is Texture2D)
                            {
                                // Try to load the @2x version
                                var highResPath = Path.Combine(
                                    Path.GetDirectoryName(path),
                                    Path.GetFileNameWithoutExtension(path) + "@2x" + Path.GetExtension(path));
                                var highResTex = LoadResource(highResPath);

                                if (highResTex != null)
                                {
                                    scalableImageIndex = newScalableImages.Count;
                                    var highResClones = CloneAsset(highResTex);
                                    newScalableImages.Add(new ScalableImage()
                                    {
                                        normalImage = clonedAssets[0] as Texture2D,
                                        highResolutionImage = highResClones[0] as Texture2D
                                    });
                                    scalableImagePaths[path] = new StoredAsset()
                                    {
                                        si = newScalableImages[newScalableImages.Count - 1],
                                        index = scalableImageIndex
                                    };
                                    clonedAssets.Add(highResClones[0]);
                                    addedAssets[asset] = clonedAssets;
                                    isScalableImage = true;
                                }
                            }

                            if (!isScalableImage && clonedAssets.Count > 0)
                            {
                                assetIndex = newAssets.Count;
                                newAssets.AddRange(clonedAssets);
                                Object resource = clonedAssets[0];
                                assetPaths[path] = new StoredAsset()
                                {
                                    resource = resource,
                                    index = assetIndex
                                };
                                isResource = true;
                            }
                        }

                        if (isResource)
                        {
                            value.valueType = StyleValueType.AssetReference;
                            value.valueIndex = assetIndex;
                            prop.values[valueIndex] = value;
                        }
                        else if (isScalableImage)
                        {
                            value.valueType = StyleValueType.ScalableImage;
                            value.valueIndex = scalableImageIndex;
                            prop.values[valueIndex] = value;
                        }
                        else
                        {
                            Debug.LogError("ResourcePath was not converted to AssetReference when converting stylesheet :  " + path);
                        }
                    }
                }
            }

            clonedStylesheet.assets = newAssets.ToArray();
            clonedStylesheet.scalableImages = newScalableImages.ToArray();

            // Store all added assets in a hashset to avoid duplicates
            var cleanAssets = new HashSet<UnityEngine.Object>();
            foreach (var assets in addedAssets.Values)
                foreach (var a in assets)
                    cleanAssets.Add(a);

            // The cloned stylesheet should be the first item in the list, since it is the "main" asset
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var result = cleanAssets.ToList();
#pragma warning restore UA2001
            result.Insert(0, clonedStylesheet);

            return result;
        }

        private static List<UnityEngine.Object> CloneAsset(UnityEngine.Object o)
        {
            if (o == null)
                return null;

            var clonedAssets = new List<UnityEngine.Object>();

            if (o is Texture2D)
            {
                var tex = new Texture2D(0, 0);
                EditorUtility.CopySerialized(o, tex);
                clonedAssets.Add(tex);
            }
            else if (o is Font)
            {
                var font = new Font();
                EditorUtility.CopySerialized(o, font);
                font.hideFlags = HideFlags.None;
                clonedAssets.Add(font);


                if (font.material != null)
                {
                    var mat = new Material(font.material.shader);
                    EditorUtility.CopySerialized(font.material, mat);
                    mat.hideFlags = HideFlags.None;
                    font.material = mat;
                    clonedAssets.Add(mat);

                    if (mat.mainTexture != null)
                    {
                        var tex = new Texture2D(0, 0);
                        EditorUtility.CopySerialized(mat.mainTexture, tex);
                        tex.hideFlags = HideFlags.None;
                        mat.mainTexture = tex;
                        clonedAssets.Add(tex);
                    }
                }

                using (var so = new SerializedObject(font))
                {
                    var oldTex = so.FindProperty("m_Texture").objectReferenceValue;
                    if (oldTex != null)
                    {
                        //Reuse the same texture if the reference was equal
                        if (font.material != null && oldTex == (o as Font).material.mainTexture)
                            so.FindProperty("m_Texture").objectReferenceValue = font.material.mainTexture;
                        else
                        {
                            var tex = new Texture2D(0, 0);
                            EditorUtility.CopySerialized(oldTex, tex);
                            tex.hideFlags = HideFlags.None;
                            so.FindProperty("m_Texture").objectReferenceValue = font.material.mainTexture;
                            clonedAssets.Add(tex);
                        }
                        so.ApplyModifiedProperties();
                    }
                }
            }

            return clonedAssets;
        }

        internal static (StyleSheetImportErrorCode, string) ConvertErrorCode(URIValidationResult result)
        {
            switch (result)
            {
                case URIValidationResult.InvalidURILocation:
                    return (StyleSheetImportErrorCode.InvalidURILocation, glossary.invalidUriLocation);
                case URIValidationResult.InvalidURIScheme:
                    return (StyleSheetImportErrorCode.InvalidURIScheme, glossary.invalidUriScheme);
                case URIValidationResult.InvalidURIProjectAssetPath:
                    return (StyleSheetImportErrorCode.InvalidURIProjectAssetPath, glossary.invalidAssetPath);
                default:
                    return (StyleSheetImportErrorCode.Internal, glossary.internalErrorWithStackTrace);
            }
        }

        void VisitUrlFunction(string path)
        {
            var response = URIHelpers.ValidateAssetURL(assetPath, path);

            if (response.hasWarningMessage)
            {
                m_Errors.AddValidationWarning(response.warningMessage, m_CurrentLine);
            }

            if (response.result != URIValidationResult.OK)
            {
                var (_, message) = ConvertErrorCode(response.result);

                m_Builder.AddValue(path, StyleValueType.MissingAssetReference);
                m_Errors.AddValidationWarning(string.Format(message, response.errorToken), m_CurrentLine);
            }
            else
            {
                var projectRelativePath = response.resolvedProjectRelativePath;
                var subAssetPath = response.resolvedSubAssetPath;
                var asset = response.resolvedQueryAsset;

                if (asset)
                {
                    if (response.isLibraryAsset)
                    {
                        // do not add path dependencies on assets in the Library folder (e.g. built-in resources)
                        m_Builder.AddValue(asset);
                        return;
                    }

                    // explicit asset reference already loaded
                    m_Context?.DependsOnArtifact(projectRelativePath);

                    // Necessary to avoid the warning "Import of asset setup artifact dependency to but dependency isn't used
                    // and therefore not registered in the asset database". (UUM-68160)
                    AssetDatabase.LoadAssetAtPath(projectRelativePath, typeof(Object));
                }
                else
                {
                    asset = DeclareDependencyAndLoad(projectRelativePath, subAssetPath);
                }

                bool isTexture = asset is Texture2D;
                Sprite spriteAsset = asset as Sprite;

                if (isTexture && string.IsNullOrEmpty(subAssetPath))
                {
                    // Try to load a sprite sub-asset associated with this texture.
                    // Sprites have extra data, such as slices and tight-meshes that
                    // aren't stored in plain textures.
                    spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(projectRelativePath);
                }

                if (asset != null)
                {
                    // Looking suffixed images files only
                    if (isTexture)
                    {
                        string hiResImageLocation = URIHelpers.InjectFileNameSuffix(projectRelativePath, "@2x");

                        if (File.Exists(FileUtil.PathToAbsolutePath(hiResImageLocation)))
                        {
                            UnityEngine.Object hiResImage = DeclareDependencyAndLoad(hiResImageLocation);

                            if (hiResImage is Texture2D)
                            {
                                m_Builder.AddValue(new ScalableImage() { normalImage = asset as Texture2D, highResolutionImage = hiResImage as Texture2D });
                            }
                            else
                            {
                                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidHighResolutionImage, string.Format(glossary.invalidHighResAssetType, asset.GetType().Name, projectRelativePath), m_CurrentLine);
                            }
                            return;
                        }
                        // If we didn't find an high res variant, tell ADB we depend on that potential file existing
                        if (spriteAsset != null)
                            DeclareDependencyAndLoad(hiResImageLocation);
                    }

                    Object assetToStore = spriteAsset != null ? spriteAsset : asset;

                    m_Builder.AddValue(assetToStore);

                    if (!disableValidation)
                    {
                        // Unknown properties (not custom) should beforehand
                        if (m_Builder.currentProperty.id == StylePropertyId.Unknown)
                            return;

                        var allowed = StylePropertyUtil.GetAllowedAssetTypesForProperty(m_Builder.currentProperty.id);

                        // If no types were returned, it means this property doesn't support assets.
                        // Normal syntax validation should cover this.
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        if (!allowed.Any())
#pragma warning restore UA2002
                            return;

                        var assetType = assetToStore.GetType();

                        // If none of the allowed types are compatible with the asset type, output a warning
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        if (!allowed.Any(t => t.IsAssignableFrom(assetType)))
#pragma warning restore UA2001
                        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            string allowedTypes = string.Join(", ", allowed.Select(t => t.Name));
#pragma warning restore UA2001
                            m_Errors.AddValidationWarning(
                                string.Format(glossary.invalidAssetType, assetType.Name, projectRelativePath, allowedTypes),
                                m_CurrentLine);

                        }
                    }
                }
                else
                {
                    // Asset is actually missing OR we couldn't load it for some reason; this should result in
                    // response.result != URIValidationResult.OK (above) but if assets are deleted while Unity is
                    // already open, we fall in here instead.
                    var (_, message) = ConvertErrorCode(URIValidationResult.InvalidURIProjectAssetPath);

                    // In case of error, we still want to call AddValue, with parameters to indicate the problem, in order
                    // to keep the full layout from being discarded. We also add appropriate warnings to explain to the
                    // user what is wrong.
                    m_Builder.AddValue(path, StyleValueType.MissingAssetReference);
                    m_Errors.AddValidationWarning(string.Format(message, path), m_CurrentLine);
                }
            }
        }

        bool ValidateFunction(FunctionToken functionToken, out StyleValueFunction func)
        {
            func = StyleValueFunction.Unknown;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (functionToken.ArgumentTokens.Count() == 0)
#pragma warning restore UA2001
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, string.Format(glossary.missingFunctionArgument, functionToken.Data), functionToken.Position.Line, functionToken.Position.Column);
                return false;
            }

            if (functionToken.Data == StyleValueFunctionExtension.k_Var)
            {
                func = StyleValueFunction.Var;
                return ValidateVarFunction(functionToken);
            }

            try
            {
                func = StyleValueFunctionExtension.FromUssString(functionToken.Data);
            }
            catch (Exception)
            {
                var prop = m_Builder.currentProperty;
                m_Errors.AddValidationWarning(string.Format(glossary.unknownFunction, functionToken.Data, prop.name), functionToken.Position.Line, functionToken.Position.Column);
                return false;
            }

            return true;
        }

        bool ValidateVarFunction(FunctionToken functionToken)
        {
            bool foundVar = false;
            bool foundComma = false;

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var args = functionToken.ArgumentTokens.ToList<Token>();
#pragma warning restore UA2001
            args.Trim();

            for (int i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg.Type == TokenType.Whitespace)
                    continue;

                if (!foundVar)
                {
                    var varName = arg.ToValue();
                    if (string.IsNullOrEmpty(varName))
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.missingVariableName, arg.Position.Line, arg.Position.Column);
                        return false;
                    }
                    if (!varName.StartsWith("--"))
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, string.Format(glossary.missingVariablePrefix, varName), arg.Position.Line, arg.Position.Column);
                        return false;
                    }
                    if (varName.Length < 3)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.emptyVariableName, arg.Position.Line, arg.Position.Column);
                        return false;
                    }

                    foundVar = true;
                    continue;
                }

                if (arg.Type == TokenType.Comma)
                {
                    if (foundComma)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.tooManyFunctionArguments, arg.Position.Line, arg.Position.Column);
                        return false;
                    }

                    foundComma = true;

                    ++i;
                    if (i >= args.Count)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.emptyFunctionArgument, arg.Position.Line, arg.Position.Column);
                        return false;
                    }
                }
                else if (!foundComma)
                {
                    string token = "";
                    while (arg.Type == TokenType.Whitespace && i + 1 < args.Count)
                    {
                        arg = args[++i];
                    }

                    if (arg.Type != TokenType.Whitespace)
                    {
                        token = arg.Data;
                    }

                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, string.Format(glossary.unexpectedTokenInFunction, token), arg.Position.Line, arg.Position.Column);
                    return false;
                }
            }

            return true;
        }

        protected void VisitValue(Property property)
        {
            // First we need to determine if the tokens are actually one long string.
            // Some deliminators such as dot(.) will cause it to be split into multiple strings.
            if (IsTokenString(property.DeclaredValue.Original))
            {
                var generatedString = BuildStringFromTokens(property.DeclaredValue.Original);
                if (!string.IsNullOrEmpty(generatedString))
                {
                    m_Builder.AddValue(generatedString, StyleValueType.String);
                    return;
                }
            }

            foreach (var t in property.DeclaredValue.Original)
            {
                VisitToken(t);
            }
        }

        void VisitToken(Token token)
        {
            switch (token)
            {
                // Hex colors
                case ColorToken colorToken:
                    if (ColorUtility.TryParseHtmlString("#" + colorToken.Data, out var color))
                        m_Builder.AddValue(color);
                    else
                        m_Errors.AddSyntaxError("Could not parse color token: " + colorToken.Data, colorToken.Position.Line, colorToken.Position.Column);
                    break;

                // Anything in the format func(args)
                case FunctionToken functionToken:
                    VisitFunctionToken(functionToken);
                    break;

                case KeywordToken keywordToken:
                {
                    // A keyword can be a color name such as "red"
                    if (keywordToken.Type == TokenType.Ident)
                    {
                        if (TryParseKeyword(keywordToken.Data, out var keyword))
                        {
                            m_Builder.AddValue(keyword);
                        }
                        else if (keywordToken.Data.StartsWith("--"))
                        {
                            m_Builder.AddValue(keywordToken.Data, StyleValueType.Variable);
                        }
                        else
                        {
                            m_Builder.AddValue(keywordToken.Data, StyleValueType.Enum);
                        }
                    }
                    else
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedTerm, string.Format(glossary.unsupportedTerm, keywordToken.Data, keywordToken.Type), keywordToken.Position.Line, keywordToken.Position.Column);
                    }
                    break;
                }

                case NumberToken numberToken:
                    m_Builder.AddValue(numberToken.Value);
                    break;

                case StringToken stringToken:
                    m_Builder.AddValue(stringToken.Data, StyleValueType.String);
                    break;

                case UnitToken unitToken:
                    if (s_UnitNameToDimensionUnit.TryGetValue(unitToken.Unit, out var dimensionUnit))
                    {
                        m_Builder.AddValue(new Dimension(unitToken.Value, dimensionUnit));
                    }
                    else
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedUnit, string.Format(glossary.unsupportedUnit, unitToken.ToValue()), unitToken.Position.Line, unitToken.Position.Column);
                    }
                    break;

                case UrlToken urlToken:
                    VisitUrlFunction(urlToken.Data);
                    break;

                default:

                    switch (token.Type)
                    {
                        case TokenType.Whitespace:
                        case TokenType.Colon:
                            // skip
                            break;

                        case TokenType.Comma:
                            m_Builder.AddCommaSeparator();
                            break;

                        default:
                            m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedTerm, string.Format(glossary.unsupportedTerm, token.Data, token.Type), token.Position.Line, token.Position.Column);
                            break;
                    }
                    break;
            }
        }

        void VisitFunctionToken(FunctionToken functionToken)
        {
            switch (functionToken.Data)
            {
                case "rgb":
                    if (TryCreateColorFromFunctionToken(functionToken, 3, out var colorRgb))
                        m_Builder.AddValue(colorRgb);
                    break;

                case "rgba":
                    if (TryCreateColorFromFunctionToken(functionToken, 4, out var colorRgba))
                        m_Builder.AddValue(colorRgba);
                    break;

                case k_ResourcePathFunctionName:
                    string resourcePath = null;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (functionToken.ArgumentTokens.FirstOrDefault() is StringToken stringToken)
#pragma warning restore UA2001
                        resourcePath = stringToken.Data;
                    else
                    {
                        var generatedPath = BuildStringFromTokens(functionToken.ArgumentTokens);
                        if (!string.IsNullOrEmpty(generatedPath))
                            resourcePath = generatedPath;
                        else
                        {
                            m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, functionToken.Data, functionToken.Position.Line, functionToken.Position.Column);
                        }
                    }

                    var resolvedResourcePath = new ResolvedResourcePath(resourcePath);
                    if (resolvedResourcePath.isPathValid)
                        m_Builder.AddResourcePath(resolvedResourcePath);
                    break;

                case StyleValueFunctionExtension.k_NoneFilter:
                    m_Builder.AddValue(StyleValueFunction.NoneFilter);
                    VisitCustomFilter(functionToken);
                    break;

                case StyleValueFunctionExtension.k_CustomFilter:
                    m_Builder.AddValue(StyleValueFunction.CustomFilter);
                    VisitCustomFilter(functionToken);
                    break;

                // env, var etc
                default:
                    if (ValidateFunction(functionToken, out var func))
                    {
                        m_Builder.AddValue(func);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        m_Builder.AddValue(functionToken.ArgumentTokens.Count(t => t.Type != TokenType.Whitespace));
#pragma warning restore UA2001
                        foreach (var token in functionToken.ArgumentTokens)
                        {
                            VisitToken(token);
                        }
                    }
                    break;
            }
        }

        bool IsTokenString(IEnumerable<Token> tokens)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (tokens.Count() > 1)
#pragma warning restore UA2001
            {
                foreach (var t in tokens)
                {
                    if (t.Type != TokenType.String &&
                        t.Type != TokenType.Delim &&
                        t.Type != TokenType.Ident)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        string BuildStringFromTokens(IEnumerable<Token> tokens)
        {
            m_StringBuilder.Clear();
            foreach (var token in tokens)
            {
                if (token.Type != TokenType.Whitespace)
                    m_StringBuilder.Append(token.Data);
            }
            return m_StringBuilder.ToString();
        }

        void VisitCustomFilter(FunctionToken functionToken)
        {
            // Used typed ToList to prevent extenion method ToList from being used.
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var args = functionToken.ArgumentTokens.ToList<Token>();
#pragma warning restore UA2001

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_Builder.AddValue(args.Count(a => a.Type != TokenType.Whitespace));
#pragma warning restore UA2001

            // First arg is a url
            if (args.Count > 0)
            {
                VisitUrlFunction(args[0].Data);
            }

            // Process remaining args
            for (int i = 1; i < args.Count; ++i)
                VisitToken(args[i]);
        }

        bool TryCreateColorFromFunctionToken(FunctionToken functionToken, int expectedChannels, out Color color)
        {
            // We expect colors in the form rgba(int, int, int, float) or rgba(float, float, float, float)

            // Extract the channels as float values and determine if the rgb format is int or float
            bool rgbIsInteger = true;

            color = new Color(0, 0, 0, 1);
            int channelsProcessed = 0;
            foreach (var arg in functionToken.ArgumentTokens)
            {
                if (arg is NumberToken numberToken)
                {
                    color[channelsProcessed++] = numberToken.Value;

                    // We have a decimal value and can be certain that rgb is in float format
                    if (!numberToken.IsInteger && channelsProcessed != 4)
                    {
                        rgbIsInteger = false;
                    }

                   if (channelsProcessed == 4)
                        break;
                }
            }

            if (channelsProcessed != expectedChannels)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, string.Format(glossary.missingFunctionArgument, functionToken.Data), functionToken.Position.Line, functionToken.Position.Column);
                return false;
            }

            // Convert valuues
            if (rgbIsInteger)
            {
                // Convert to float
                for (int i = 0; i < Mathf.Min(3, expectedChannels); ++i)
                {
                    color[i] /= 255.0f;
                }
            }

            return true;
        }

        static bool TryParseKeyword(string rawStr, out StyleValueKeyword value)
        {
            if (s_NameCache == null)
            {
                s_NameCache = new Dictionary<string, StyleValueKeyword>();
                foreach (StyleValueKeyword kw in System.Enum.GetValues(typeof(StyleValueKeyword)))
                {
                    s_NameCache[kw.ToString().ToLowerInvariant()] = kw;
                }
            }
            return s_NameCache.TryGetValue(rawStr.ToLowerInvariant(), out value);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleSheetImporterImpl : StyleValueImporter
    {
        static readonly StylesheetParser s_Parser = new StylesheetParser();
        static readonly HashSet<string> s_StyleSheetsWithCircularImportDependencies = new HashSet<string>();
        static readonly HashSet<string> s_StyleSheetsUnsortedDependencies = new HashSet<string>();
        static readonly List<string> s_StyleSheetProjectRelativeImportPaths = new List<string>();

        public StyleSheetImporterImpl(AssetImportContext context) : base(context)
        {
        }

        public StyleSheetImporterImpl() : base()
        {
        }

        internal static string[] PopulateDependencies(string assetPath)
        {
            s_StyleSheetsUnsortedDependencies.Clear();
            s_StyleSheetsUnsortedDependencies.Add(assetPath);
            s_StyleSheetsWithCircularImportDependencies.Remove(assetPath);

            var dependencies = new List<string>();
            PopulateDependencies(assetPath, dependencies);
            return dependencies.ToArray();
        }

        internal static void PopulateDependencies(string assetPath, List<string> dependencies)
        {
            var contents = File.ReadAllText(FileUtil.PathToAbsolutePath(assetPath));

            if (string.IsNullOrEmpty(contents))
                return;

            var styleSheet = s_Parser.Parse(contents);

            s_StyleSheetProjectRelativeImportPaths.Clear();
            foreach (var import in styleSheet.ImportRules)
            {
                var importResult = URIHelpers.ValidAssetURL(assetPath, import.Href, out _, out var projectRelativePath);
                if (importResult == URIValidationResult.OK)
                {
                    if (!s_StyleSheetProjectRelativeImportPaths.Contains(projectRelativePath))
                        s_StyleSheetProjectRelativeImportPaths.Add(projectRelativePath);
                }
            }

            // Array copy to iterate over the paths. This avoids in-place editing in the recursive call
            foreach (var projectRelativeImportPath in s_StyleSheetProjectRelativeImportPaths.ToArray())
            {
                if (s_StyleSheetsUnsortedDependencies.Contains(projectRelativeImportPath))
                {
                    s_StyleSheetsWithCircularImportDependencies.Add(projectRelativeImportPath);
                    throw new InvalidDataException("Circular @import dependencies");
                }

                s_StyleSheetsUnsortedDependencies.Add(projectRelativeImportPath);
                PopulateDependencies(projectRelativeImportPath, dependencies);
                dependencies.Add(projectRelativeImportPath);
            }
        }

        protected virtual void OnImportError(StyleSheetImportErrors errors)
        {
            if (m_Context == null)
                return;

            foreach (var e in errors)
            {
                var errorContext = string.IsNullOrEmpty(e.assetPath)
                    ? null
                    : AssetDatabase.LoadMainAssetAtPath(e.assetPath);

                if (e.isWarning)
                {
                    m_Context.LogImportWarning(e.ToString(glossary), e.assetPath, e.line, errorContext);
                }
                else
                {
                    m_Context.LogImportError(e.ToString(glossary), e.assetPath, e.line, errorContext);
                }
            }
        }

        protected virtual void OnImportSuccess(UnityStyleSheet asset)
        {
        }

        public void Import(UnityStyleSheet asset, string contents)
        {
            var styleSheet = m_Parser.Parse(contents);
            ImportParserStyleSheet(asset, styleSheet, m_Parser.errors);

            var h = new Hash128();
            byte[] b = Encoding.UTF8.GetBytes(contents);
            if (b.Length > 0)
            {
                HashUtilities.ComputeHash128(b, ref h);
            }
            asset.contentHash = h.GetHashCode();
        }

        void AddUssParserError(TokenizerError error)
        {
            // Currntly ExCSS 4.3 has the same info in error.Message, we will try to add more detail:
            var code = (ParseError)error.Code;
            string errorMessage = error.Message;
            if (code == ParseError.InvalidBlockStart)
                errorMessage = "Invalid block start, no selector found before the opening curly bracket.";

            var errorMsg = $"{(ParseError)error.Code} : {errorMessage}";
            m_Errors.AddSyntaxError(string.Format(glossary.ussParsingError, errorMsg), error.Position.Line, error.Position.Column);
        }

        protected void ImportParserStyleSheet(UnityStyleSheet asset, ParserStyleSheet styleSheet, List<TokenizerError> errors)
        {
            m_Errors.assetPath = assetPath;

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    AddUssParserError(error);
                }
            }
            else
            {
                try
                {
                    VisitSheet(styleSheet);
                }
                catch (System.Exception exc)
                {
                    m_Errors.AddInternalError(string.Format(glossary.internalErrorWithStackTrace, exc.Message, exc.StackTrace), m_CurrentLine);
                }
            }

            bool hasErrors = m_Errors.hasErrors;
            if (!hasErrors)
            {
                m_Builder.BuildTo(asset);

                if (!s_StyleSheetsWithCircularImportDependencies.Contains(assetPath))
                {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var importRules = styleSheet.ImportRules.ToList();
#pragma warning restore UA2001
                    var importDirectivesCount = importRules.Count;
                    asset.imports = new UnityStyleSheet.ImportStruct[importDirectivesCount];
                    for (int i = 0; i < importDirectivesCount; ++i)
                    {
                        var importedPath = importRules[i].Href;

                        var response = URIHelpers.ValidateAssetURL(assetPath, importedPath);
                        var importResult = response.result;
                        var errorToken = response.errorToken;
                        var projectRelativePath = response.resolvedProjectRelativePath;

                        if (response.hasWarningMessage)
                        {
                            m_Errors.AddValidationWarning(response.warningMessage, m_CurrentLine);
                        }

                        UnityStyleSheet importedStyleSheet = null;
                        if (importResult != URIValidationResult.OK)
                        {
                            var (code, message) = ConvertErrorCode(importResult);
                            m_Errors.AddSemanticWarning(code, string.Format(message, errorToken), m_CurrentLine);
                        }
                        else
                        {
                            importedStyleSheet = response.resolvedQueryAsset as UnityStyleSheet;
                            if (importedStyleSheet)
                            {
                                m_Context.DependsOnArtifact(projectRelativePath);
                            }
                            else
                            {
                                importedStyleSheet = DeclareDependencyAndLoad(projectRelativePath) as UnityStyleSheet;
                            }

                            if (!response.isLibraryAsset)
                            {
                                m_Context.DependsOnImportedAsset(projectRelativePath);
                            }
                        }

                        asset.imports[i] = new UnityStyleSheet.ImportStruct
                        {
                            styleSheet = importedStyleSheet,
                            mediaQueries = new string[importRules[i].Media.Length]
                        };
                        for (int j = 0; j < importRules[i].Media.Length; ++j)
                        {
                            asset.imports[i].mediaQueries[j] = importRules[i].Media[j];
                        }
                    }

                    if (importDirectivesCount > 0)
                    {
                        asset.FlattenImportedStyleSheetsRecursive();
                    }
                }
                else
                {
                    asset.imports = Array.Empty<UnityStyleSheet.ImportStruct>();
                    m_Errors.AddValidationWarning(glossary.circularImport, -1);
                }

                OnImportSuccess(asset);
            }

            bool hasWarnings = m_Errors.hasWarning;
            asset.importedWithErrors = hasErrors;
            asset.importedWithWarnings = hasWarnings;

            if (hasErrors || hasWarnings)
            {
                OnImportError(m_Errors);
            }
        }

        void ValidateProperty(Property property)
        {
            if (!disableValidation)
            {
                var name = property.Name;
                var value = property.Value;
                var result = m_Validator.ValidateProperty(name, value);
                if (!result.success)
                {
                    string msg = $"{result.message}\n    {name}: {value}";
                    if (!string.IsNullOrEmpty(result.hint))
                        msg = $"{msg} -> {result.hint}";

                    m_Errors.AddValidationWarning(msg, GetPropertyLine(property));
                }
            }
        }

        int GetPropertyLine(Property property)
        {
            // Property doesnt seem to have a position. StylesheetText is always null.
            // Grab it from the first token
            return property.DeclaredValue.Original[0].Position.Line;
        }

        void VisitSheet(ParserStyleSheet styleSheet)
        {
            foreach (var rule in styleSheet.StyleRules)
            {
                m_Builder.BeginRule(rule.StylesheetText.Range.Start.Line);

                m_CurrentLine = rule.StylesheetText.Range.Start.Line;

                // Note: we must rely on recursion to correctly handle parser types here
                 VisitBaseSelector(rule.Selector);

                foreach (var property in rule.Style.Declarations)
                {
                    // Property doesnt seem to have a position. StylesheetText is always null.
                    // Grab it from the first token
                    var propertyLine = GetPropertyLine(property);
                    m_CurrentLine = propertyLine;

                    ValidateProperty(property);

                    m_Builder.BeginProperty(property.Name, propertyLine);

                    // Note: we must rely on recursion to correctly handle parser types here
                    VisitValue(property);

                    m_Builder.EndProperty();
                }

                m_Builder.EndRule();
            }
        }

        void VisitBaseSelector(ISelector selector)
        {
            switch (selector)
            {
                case AllSelector allSelector:
                    VisitSelectorParts(new[] { StyleSelectorPart.CreateWildCard() }, allSelector);
                    break;

                case ClassSelector classSelector:
                    VisitSelectorParts(new[] { StyleSelectorPart.CreateClass(classSelector.Class) }, classSelector);
                    break;

                case ComplexSelector complexSelector:
                    VisitComplexSelector(complexSelector);
                    break;

                case CompoundSelector compoundSelector:
                    if (TryExtractSelectorsParts(compoundSelector, out var compoundParts))
                        VisitSelectorParts(compoundParts, compoundSelector);
                    break;

                case IdSelector idSelector:
                    VisitSelectorParts(new[] { StyleSelectorPart.CreateId(idSelector.Id) }, idSelector);
                    break;

                case ListSelector listSelector:
                    foreach (var s in listSelector)
                    {
                        VisitBaseSelector(s);
                    }
                    break;

                case PseudoClassSelector pseudoClassSelector:
                    ValidatePsuedoClassName(pseudoClassSelector.Class, pseudoClassSelector.Text);
                    VisitSelectorParts(new[] { StyleSelectorPart.CreatePseudoClass(pseudoClassSelector.Class) }, pseudoClassSelector);
                    break;

                case TypeSelector typeSelector:
                    VisitSelectorParts(new[] { StyleSelectorPart.CreateType(typeSelector.Name) }, typeSelector);
                    break;

                case UnknownSelector unknownSelector:
                    VisitUnknownSelector(unknownSelector);
                    break;

                default:
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, $"{selector.GetType().Name}: `{selector.Text}`"), m_CurrentLine);
                    break;
            }
        }

        void ValidatePsuedoClassName(string name, string selector)
        {
            // We produce a warning but we still let the selector pass. We may want to change this into an error in the future.
            if (!disableValidation && !PseudoClassSelectorFactory.Selectors.ContainsKey(name))
            {
                m_Errors.AddValidationWarning(string.Format(glossary.unknownPsuedoClass, name, selector), m_CurrentLine);
            }
        }

        void VisitUnknownSelector(UnknownSelector unknownSelector)
        {
            // We try to handle some of the non-standard selectors here.
            var selectorText = unknownSelector.Text;

            // We do not support class selectors that start with a digit, it is invalid css. Here we provide a better error message to explain why. (UUM-102246)
            if (selectorText.StartsWith(".") && selectorText.Length > 1)
            {
                // Check the name doesnt start with a digit
                if (char.IsDigit(selectorText[1]) ||
                    selectorText.Length >= 2 && selectorText[1] == '-' && char.IsDigit(selectorText[2]))
                {
                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.selectorStartsWithDigitFormat, unknownSelector.Text), m_CurrentLine);
                    return;
                }
            }

            m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, unknownSelector.Text), m_CurrentLine);
        }

        void VisitSelectorParts(StyleSelectorPart[] parts, ISelector selector)
        {
            int specificity = CSSSpec.GetSelectorSpecificity(parts);
            if (specificity == CSSSpec.InvalidSpecificityScore)
            {
                m_Errors.AddInternalError(string.Format(glossary.internalError, "Failed to calculate selector specificity " + selector.Text), m_CurrentLine);
                return;
            }

            using (m_Builder.BeginComplexSelector(specificity))
            {
                m_Builder.AddSimpleSelector(parts, StyleSelectorRelationship.None);
            }
        }

        bool TryExtractSelectorsParts(Selectors selectors, out StyleSelectorPart[] parts)
        {
            parts = new StyleSelectorPart[selectors.Length];
            for (int i = 0; i < selectors.Length; ++i)
            {
                switch (selectors[i])
                {
                    case AllSelector allSelector:
                        parts[i] = StyleSelectorPart.CreateWildCard();
                        break;

                    case IdSelector idSelector:
                        parts[i] = StyleSelectorPart.CreateId(idSelector.Id);
                        break;

                    case ClassSelector classSelector:
                        parts[i] = StyleSelectorPart.CreateClass(classSelector.Class);
                        break;

                    case PseudoClassSelector pseudoClassSelector:
                    {
                        // Check for is() and has() which we dont support
                        if (pseudoClassSelector.Class.Contains("("))
                        {
                            m_Errors.AddSemanticError(StyleSheetImportErrorCode.RecursiveSelectorDetected, string.Format(glossary.unsupportedSelectorFormat, selectors.Text), m_CurrentLine);
                            return false;
                        }
                        else
                            parts[i] = StyleSelectorPart.CreatePseudoClass(pseudoClassSelector.Class);
                        break;
                    }

                    case TypeSelector typeSelector:
                        parts[i] = StyleSelectorPart.CreateType(typeSelector.Name);
                        break;

                    case FirstChildSelector firstChildSelector:
                        parts[i] = new StyleSelectorPart { type = StyleSelectorType.RecursivePseudoClass };
                        break;

                    default:
                        parts[i] = new StyleSelectorPart { type = StyleSelectorType.Unknown };
                        break;
                }
            }
            return true;
        }

        void VisitComplexSelector(ComplexSelector complexSelector)
        {
            int fullSpecificity = CSSSpec.GetSelectorSpecificity(complexSelector.Text);

            if (fullSpecificity == CSSSpec.InvalidSpecificityScore)
            {
                m_Errors.AddInternalError(string.Format(glossary.internalError, "Failed to calculate selector specificity " + complexSelector), m_CurrentLine);
                return;
            }

            using (m_Builder.BeginComplexSelector(fullSpecificity))
            {
                StyleSelectorRelationship relationShip = StyleSelectorRelationship.None;
                var lastSelectorIndex = complexSelector.Length - 1;
                int currentSelectorIndex = -1;
                foreach (CombinatorSelector selector in complexSelector)
                {
                    currentSelectorIndex++;
                    StyleSelectorPart[] parts;

                    string simpleSelector = selector.Selector.Text;

                    if (string.IsNullOrEmpty(simpleSelector))
                    {
                        m_Errors.AddInternalError(string.Format(glossary.internalError, "Expected simple selector inside complex selector " + simpleSelector), m_CurrentLine);
                        return;
                    }

                    if (CheckSimpleSelector(simpleSelector, out parts))
                    {
                        m_Builder.AddSimpleSelector(parts, relationShip);

                        // Read relation for next element
                        if (currentSelectorIndex != lastSelectorIndex)
                        {
                            if (selector.Delimiter == Combinators.Child)
                                relationShip = StyleSelectorRelationship.Child;
                            else if (selector.Delimiter == Combinators.Descendent)
                                relationShip = StyleSelectorRelationship.Descendent;
                            else
                            {
                                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidComplexSelectorDelimiter, string.Format(glossary.invalidComplexSelectorDelimiter, complexSelector.Text), m_CurrentLine);
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        bool CheckSimpleSelector(string selector, out StyleSelectorPart[] parts)
        {
            if (!CSSSpec.ParseSelector(selector, out parts))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (parts.Any(p => p.type == StyleSelectorType.Unknown))
#pragma warning restore UA2001
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (parts.Any(p => p.type == StyleSelectorType.RecursivePseudoClass))
#pragma warning restore UA2001
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.RecursiveSelectorDetected, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }

            if (!disableValidation)
            {
                foreach (var p in parts)
                {
                    if (p.type == StyleSelectorType.PseudoClass)
                    {
                        // We allow them but produce a warning.
                        ValidatePsuedoClassName(p.value, selector);
                    }
                }
            }

            return true;
        }
    }
}
