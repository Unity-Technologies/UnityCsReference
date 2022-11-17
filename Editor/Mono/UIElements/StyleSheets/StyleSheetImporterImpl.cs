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
using UnityEditor.AssetImporters;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.StyleSheets
{
    abstract class StyleValueImporter
    {
        private static StyleSheetImportGlossary s_Glossary;

        internal static StyleSheetImportGlossary glossary => s_Glossary ?? (s_Glossary = new StyleSheetImportGlossary());

        const string k_ResourcePathFunctionName = "resource";
        const string k_VariableFunctionName = "var";

        protected readonly UnityEditor.AssetImporters.AssetImportContext m_Context;
        protected readonly Parser m_Parser;
        protected readonly StyleSheetBuilder m_Builder;
        protected readonly StyleSheetImportErrors m_Errors;
        protected readonly StyleValidator m_Validator;
        protected string m_AssetPath;
        protected int m_CurrentLine;

        public StyleValueImporter(UnityEditor.AssetImporters.AssetImportContext context)
        {
            if (context == null)
                throw new System.ArgumentNullException(nameof(context));

            m_Context = context;
            m_AssetPath = context.assetPath;
            m_Parser = new Parser();
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
            m_Parser = new Parser();
            m_Builder = new StyleSheetBuilder();
            m_Errors = new StyleSheetImportErrors();
            m_Validator = new StyleValidator();
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
                    newScalableImages.Add(new ScalableImage() {
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

                        var path = clonedStylesheet.strings[value.valueIndex];

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
                                    newScalableImages.Add(new ScalableImage() {
                                        normalImage = clonedAssets[0] as Texture2D,
                                        highResolutionImage = highResClones[0] as Texture2D
                                    });
                                    scalableImagePaths[path] = new StoredAsset() {
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
            var result = cleanAssets.ToList();
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

                {
                    var so = new SerializedObject(font);
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

        protected void VisitResourceFunction(GenericFunction funcTerm)
        {
            var argTerm = funcTerm.Arguments.FirstOrDefault() as PrimitiveTerm;
            if (argTerm == null)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, funcTerm.Name, m_CurrentLine);
                return;
            }

            string path = argTerm.Value as string;
            m_Builder.AddValue(path, StyleValueType.ResourcePath);
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

        protected void VisitUrlFunction(PrimitiveTerm term)
        {
            string path = (string)term.Value;

            var response = URIHelpers.ValidateAssetURL(assetPath, path);

            if (response.hasWarningMessage)
            {
                m_Errors.AddValidationWarning(response.warningMessage, m_CurrentLine);
            }

            if (response.result != URIValidationResult.OK)
            {
                var(_, message) = ConvertErrorCode(response.result);

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

                        if (File.Exists(hiResImageLocation))
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
                        var propertyName = new StylePropertyName(m_Builder.currentProperty.name);

                        // Unknown properties (not custom) should beforehand
                        if (propertyName.id == StylePropertyId.Unknown)
                            return;

                        var allowed = StylePropertyUtil.GetAllowedAssetTypesForProperty(propertyName.id);

                        // If no types were returned, it means this property doesn't support assets.
                        // Normal syntax validation should cover this.
                        if (!allowed.Any())
                            return;

                        Type assetType = assetToStore.GetType();

                        // If none of the allowed types are compatible with the asset type, output a warning
                        if (!allowed.Any(t => t.IsAssignableFrom(assetType)))
                        {
                            string allowedTypes = string.Join(", ", allowed.Select(t => t.Name));
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
                    var(_, message) = ConvertErrorCode(URIValidationResult.InvalidURIProjectAssetPath);

                    // In case of error, we still want to call AddValue, with parameters to indicate the problem, in order
                    // to keep the full layout from being discarded. We also add appropriate warnings to explain to the
                    // user what is wrong.
                    m_Builder.AddValue(path, StyleValueType.MissingAssetReference);
                    m_Errors.AddValidationWarning(string.Format(message, path), m_CurrentLine);
                }
            }
        }

        private bool ValidateFunction(GenericFunction term, out StyleValueFunction func)
        {
            func = StyleValueFunction.Unknown;
            if (term.Arguments.Length == 0)
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.MissingFunctionArgument, string.Format(glossary.missingFunctionArgument, term.Name), m_CurrentLine);
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
            catch (System.Exception)
            {
                var prop = m_Builder.currentProperty;
                m_Errors.AddValidationWarning(string.Format(glossary.unknownFunction, term.Name, prop.name), prop.line);
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
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.missingVariableName, m_CurrentLine);
                        return false;
                    }
                    if (!varName.StartsWith("--"))
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, string.Format(glossary.missingVariablePrefix, varName), m_CurrentLine);
                        return false;
                    }
                    if (varName.Length < 3)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.emptyVariableName, m_CurrentLine);
                        return false;
                    }

                    foundVar = true;
                }
                else if (arg.GetType() == typeof(Comma))
                {
                    if (foundComma)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.tooManyFunctionArguments, m_CurrentLine);
                        return false;
                    }

                    foundComma = true;

                    ++i;
                    if (i >= argc)
                    {
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, glossary.emptyFunctionArgument, m_CurrentLine);
                        return false;
                    }
                }
                else if (!foundComma)
                {
                    string token = "";
                    while (arg.GetType() == typeof(Whitespace) && i + 1 < argc)
                    {
                        arg = term.Arguments[++i];
                    }

                    if (arg.GetType() != typeof(Whitespace))
                    {
                        token = arg.ToString();
                    }

                    m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidVarFunction, string.Format(glossary.unexpectedTokenInFunction, token), m_CurrentLine);
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
                    case UnitType.Second:
                        float? secondValue = primitiveTerm.GetFloatValue(UnitType.Second);
                        m_Builder.AddValue(new Dimension(secondValue.Value, Dimension.Unit.Second));
                        break;
                    case UnitType.Millisecond:
                        float? msValue = primitiveTerm.GetFloatValue(UnitType.Millisecond);
                        m_Builder.AddValue(new Dimension(msValue.Value, Dimension.Unit.Millisecond));
                        break;
                    case UnitType.Degree:
                        float? degValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(degValue.Value, Dimension.Unit.Degree));
                        break;
                    case UnitType.Grad:
                        float? gradValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(gradValue.Value, Dimension.Unit.Gradian));
                        break;
                    case UnitType.Radian:
                        float? radValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(radValue.Value, Dimension.Unit.Radian));
                        break;
                    case UnitType.Turn:
                        float? turnValue = primitiveTerm.GetFloatValue(UnitType.Pixel);
                        m_Builder.AddValue(new Dimension(turnValue.Value, Dimension.Unit.Turn));
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
                        m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedUnit, string.Format(glossary.unsupportedUnit, primitiveTerm.ToString()), m_CurrentLine);
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
                int valueCount = 0;
                foreach (Term childTerm in termList)
                {
                    VisitValue(childTerm);
                    ++valueCount;

                    // Add separator
                    if (valueCount < termList.Length)
                    {
                        var termSeparator = termList.GetSeparatorAt(valueCount - 1);
                        switch (termSeparator)
                        {
                            case TermList.TermSeparator.Comma:
                                m_Builder.AddCommaSeparator();
                                break;
                            case TermList.TermSeparator.Space:
                            case TermList.TermSeparator.Colon:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(termSeparator));
                        }
                    }
                }
            }
            else if (commaTerm != null)
            {
                m_Builder.AddCommaSeparator();
            }
            else if (wsTerm != null)
            {
                // skip
            }
            else
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedTerm, string.Format(glossary.unsupportedTerm, term.GetType().Name), m_CurrentLine);
            }
        }

        static Dictionary<string, StyleValueKeyword> s_NameCache;

        static bool TryParseKeyword(string rawStr, out StyleValueKeyword value)
        {
            if (s_NameCache == null)
            {
                s_NameCache = new Dictionary<string, StyleValueKeyword>();
                foreach (StyleValueKeyword kw in System.Enum.GetValues(typeof(StyleValueKeyword)))
                {
                    s_NameCache[kw.ToString().ToLower()] = kw;
                }
            }
            return s_NameCache.TryGetValue(rawStr.ToLower(), out value);
        }
    }

    internal class StyleSheetImporterImpl : StyleValueImporter
    {
        static readonly Parser s_Parser = new Parser();
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
            var contents = File.ReadAllText(assetPath);

            if (string.IsNullOrEmpty(contents))
                return;

            var styleSheet = s_Parser.Parse(contents);
            var importDirectivesCount = styleSheet.ImportDirectives.Count;

            s_StyleSheetProjectRelativeImportPaths.Clear();
            for (var i = 0; i < importDirectivesCount; ++i)
            {
                var importedPath = styleSheet.ImportDirectives[i].Href;
                var importResult = URIHelpers.ValidAssetURL(assetPath, importedPath, out _, out var projectRelativePath);
                if (importResult == URIValidationResult.OK)
                {
                    if (!s_StyleSheetProjectRelativeImportPaths.Contains(projectRelativePath))
                        s_StyleSheetProjectRelativeImportPaths.Add(projectRelativePath);
                }
            }

            foreach (var projectRelativeImportPath in s_StyleSheetProjectRelativeImportPaths)
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
                    m_Errors.AddSyntaxError(string.Format(glossary.ussParsingError, error.Message), error.Line);
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
                    var importDirectivesCount = styleSheet.ImportDirectives.Count;
                    asset.imports = new UnityStyleSheet.ImportStruct[importDirectivesCount];
                    for (int i = 0; i < importDirectivesCount; ++i)
                    {
                        var importedPath = styleSheet.ImportDirectives[i].Href;

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
                            var(code, message) = ConvertErrorCode(importResult);
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
                            mediaQueries = styleSheet.ImportDirectives[i].Media.ToArray()
                        };
                    }

                    if (importDirectivesCount > 0)
                    {
                        asset.FlattenImportedStyleSheetsRecursive();
                    }
                }
                else
                {
                    asset.imports = new UnityStyleSheet.ImportStruct[0];
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

                m_CurrentLine = rule.Line;

                // Note: we must rely on recursion to correctly handle parser types here
                VisitBaseSelector(rule.Selector);

                foreach (Property property in rule.Declarations)
                {
                    m_CurrentLine = property.Line;

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
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidSelectorListDelimiter, string.Format(glossary.invalidSelectorListDelimiter, selectorList.Delimiter), m_CurrentLine);
            }
        }

        void VisitComplexSelector(ComplexSelector complexSelector)
        {
            int fullSpecificity = CSSSpec.GetSelectorSpecificity(complexSelector.ToString());

            if (fullSpecificity == 0)
            {
                m_Errors.AddInternalError(string.Format(glossary.internalError, "Failed to calculate selector specificity " + complexSelector), m_CurrentLine);
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
                        m_Errors.AddInternalError(string.Format(glossary.internalError, "Expected simple selector inside complex selector " + simpleSelector), m_CurrentLine);
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
                                m_Errors.AddSemanticError(StyleSheetImportErrorCode.InvalidComplexSelectorDelimiter, string.Format(glossary.invalidComplexSelectorDelimiter, complexSelector), m_CurrentLine);
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
                    m_Errors.AddInternalError(string.Format(glossary.internalError, "Failed to calculate selector specificity " + selector), m_CurrentLine);
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
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }
            if (parts.Any(p => p.type == StyleSelectorType.Unknown))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.UnsupportedSelectorFormat, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }
            if (parts.Any(p => p.type == StyleSelectorType.RecursivePseudoClass))
            {
                m_Errors.AddSemanticError(StyleSheetImportErrorCode.RecursiveSelectorDetected, string.Format(glossary.unsupportedSelectorFormat, selector), m_CurrentLine);
                return false;
            }
            return true;
        }
    }
}
