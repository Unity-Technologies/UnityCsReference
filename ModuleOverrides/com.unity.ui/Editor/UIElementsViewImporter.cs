// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ExCSS;
using Unity.Profiling;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

using UnityEngine.UIElements;
using StyleSheet = UnityEngine.UIElements.StyleSheet;

namespace UnityEditor.UIElements
{
    // Make sure UXML is imported after assets than can be addressed in USS
    [ScriptedImporter(version: 12, ext: "uxml", importQueueOffset: 1102)]
    [ExcludeFromPreset]
    internal class UIElementsViewImporter : ScriptedImporter
    {
        // Parses the XML file to figure out dependencies to other UXML/USS files
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(File.ReadAllText(assetPath), LoadOptions.SetLineInfo);
            }
            catch (Exception)
            {
                // We want to be silent here, all XML syntax errors will be reported during the actual import
                return new string[] {};
            }
            var dependencies = new List<string>();
            UXMLImporterImpl.PopulateDependencies(assetPath, doc.Root, dependencies);

            return dependencies.ToArray();
        }

        public override void OnImportAsset(AssetImportContext args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            VisualTreeAsset vta;

            var importer = new UXMLImporterImpl(args);

            importer.Import(out vta);
            args.AddObjectToAsset("tree", vta);
            args.SetMainObject(vta);

            if (!vta.inlineSheet)
                vta.inlineSheet = ScriptableObject.CreateInstance<StyleSheet>();

            // Make sure imported objects aren't editable in the Inspector
            vta.hideFlags = HideFlags.NotEditable;
            vta.inlineSheet.hideFlags = HideFlags.NotEditable;

            args.AddObjectToAsset("inlineStyle", vta.inlineSheet);
        }
    }

    internal class UXMLImporterImpl : StyleValueImporter
    {
        internal struct Error
        {
            public readonly Level level;
            public readonly ImportErrorType error;
            public readonly ImportErrorCode code;
            public readonly object context;
            public readonly string filePath;
            public readonly IXmlLineInfo xmlLineInfo;

            public enum Level
            {
                Warning,
                Fatal,
            }

            public Error(ImportErrorType error, ImportErrorCode code, object context, Level level, string filePath,
                         IXmlLineInfo xmlLineInfo)
            {
                this.xmlLineInfo = xmlLineInfo;
                this.error = error;
                this.code = code;
                this.context = context;
                this.level = level;
                this.filePath = filePath;
            }

            private static string ErrorMessage(ImportErrorCode errorCode)
            {
                switch (errorCode)
                {
                    case ImportErrorCode.InvalidXml:
                        return "Xml is not valid, exception during parsing: {0}";
                    case ImportErrorCode.InvalidRootElement:
                        return "Expected the XML Root element name to be '" + k_RootNode + "', found '{0}'";
                    case ImportErrorCode.TemplateHasEmptyName:
                        return "'" + k_TemplateNode + "' declaration requires a non-empty '" + k_TemplateNameAttr +
                            "' attribute";
                    case ImportErrorCode.TemplateInstanceHasEmptySource:
                        return "'" + k_TemplateInstanceNode + "' declaration requires a non-empty '" +
                            k_TemplateInstanceSourceAttr + "' attribute";
                    case ImportErrorCode.TemplateMissingPathOrSrcAttribute:
                        return "'" + k_TemplateNode + "' declaration requires a '" + k_GenericPathAttr + "' or '" + k_GenericSrcAttr +
                            "' attribute referencing another UXML file";
                    case ImportErrorCode.TemplateSrcAndPathBothSpecified:
                        return "'" + k_TemplateNode + "' declaration does not accept both '" + k_GenericSrcAttr + "' and '" + k_GenericPathAttr +
                            "' attributes";
                    case ImportErrorCode.DuplicateTemplateName:
                        return "Duplicate name '{0}'";
                    case ImportErrorCode.UnknownTemplate:
                        return "Unknown template name '{0}'";
                    case ImportErrorCode.UnknownElement:
                        return "Unknown element name '{0}'";
                    case ImportErrorCode.UnknownAttribute:
                        return "Unknown attribute: '{0}'";
                    case ImportErrorCode.InvalidCssInStyleAttribute:
                        return "USS in 'style' attribute is invalid: {0}";
                    case ImportErrorCode.StyleReferenceEmptyOrMissingPathOrSrcAttr:
                        return "'" + k_StyleReferenceNode + "' declaration requires a '" + k_GenericPathAttr + "' or '" + k_GenericSrcAttr +
                            "' attribute referencing a USS file";
                    case ImportErrorCode.StyleReferenceSrcAndPathBothSpecified:
                        return "'" + k_StyleReferenceNode + "' declaration does not accept both '" + k_GenericSrcAttr + "' and '" + k_GenericPathAttr +
                            "' attributes";
                    case ImportErrorCode.SlotsAreExperimental:
                        return "Slot are an experimental feature. Syntax and semantic may change in the future.";
                    case ImportErrorCode.DuplicateSlotDefinition:
                        return "Slot definition '{0}' is defined more than once";
                    case ImportErrorCode.SlotUsageInNonTemplate:
                        return "Element has an assigned slot, but its parent '{0}' is not a template reference";
                    case ImportErrorCode.SlotDefinitionHasEmptyName:
                        return "Slot definition has an empty name";
                    case ImportErrorCode.SlotUsageHasEmptyName:
                        return "Slot usage has an empty name";
                    case ImportErrorCode.DuplicateContentContainer:
                        return "'contentContainer' attribute must be defined once at most";
                    case ImportErrorCode.DeprecatedAttributeName:
                        return "'{0}' attribute name is deprecated";
                    case ImportErrorCode.ReplaceByAttributeName:
                        return "Please use '{0}' instead";
                    case ImportErrorCode.AttributeOverridesMissingElementNameAttr:
                        return "AttributeOverrides node missing 'element-name' attribute.";
                    case ImportErrorCode.AttributeOverridesInvalidAttr:
                        return "AttributeOverrides node cannot override attribute '{0}'.";
                    case ImportErrorCode.ReferenceInvalidURILocation:
                        return "The specified URL is empty or invalid : {0}";
                    case ImportErrorCode.ReferenceInvalidURIScheme:
                        return "The scheme specified for the URI is invalid : {0}";
                    case ImportErrorCode.ReferenceInvalidURIProjectAssetPath:
                        return "The specified URI does not exist in the current project : {0}";
                    case ImportErrorCode.ReferenceInvalidAssetType:
                        return "The specified URI refers to an invalid asset : {0}";
                    case ImportErrorCode.TemplateHasCircularDependency:
                        return "The specified URI contains a circular dependency: {0}";
                    case ImportErrorCode.InvalidUxmlObjectParent:
                        return "Uxml object can only be placed under VisualElements or other UxmlObjects: {0}";
                    case ImportErrorCode.InvalidUxmlObjectChild:
                        return "Uxml object has an invalid child element: {0}";
                    default:
                        throw new ArgumentOutOfRangeException("Unhandled error code " + errorCode);
                }
            }

            public override string ToString()
            {
                string message = ErrorMessage(code);
                string lineInfo = xmlLineInfo == null
                    ? ""
                    : UnityString.Format(" ({0},{1})", xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                return UnityString.Format("{0}{1}: {2} - {3}", filePath, lineInfo, error,
                    UnityString.Format(message, context == null ? "<null>" : context.ToString()));
            }
        }

        internal class DefaultLogger
        {
            protected List<Error> m_Errors = new List<Error>();
            protected string m_Path;

            internal virtual void LogError(ImportErrorType error, ImportErrorCode code, object context,
                Error.Level level, IXmlLineInfo xmlLineInfo)
            {
                m_Errors.Add(new Error(error, code, context, level, m_Path, xmlLineInfo));
            }

            internal virtual void BeginImport(string path)
            {
                m_Path = path;
                // UXML files can be re-imported several times in the same refresh
                // therefore we make sure that previously reported errors are gone.
                // Even though dependency order will eventually be honoured
                // some files are unexpectedly getting imported before their dependencies.
                m_Errors.RemoveAll(e => e.filePath == path);
            }

            private void LogError(VisualTreeAsset obj, Error error)
            {
                try
                {
                    switch (error.level)
                    {
                        case Error.Level.Warning:
                            Debug.LogWarningFormat(obj, error.ToString());
                            break;
                        case Error.Level.Fatal:
                            Debug.LogErrorFormat(obj, error.ToString());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(error));
                    }
                }
                catch (FormatException)
                {
                    switch (error.level)
                    {
                        case Error.Level.Warning:
                            Debug.LogWarning(error.ToString());
                            break;
                        case Error.Level.Fatal:
                            Debug.LogError(error.ToString());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(error));
                    }
                }
            }

            internal virtual void FinishImport()
            {
                Dictionary<string, VisualTreeAsset> cache = new Dictionary<string, VisualTreeAsset>();

                foreach (var error in m_Errors)
                {
                    VisualTreeAsset obj;
                    if (!cache.TryGetValue(error.filePath, out obj))
                        cache.Add(error.filePath, obj = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(error.filePath));

                    LogError(obj, error);
                }

                m_Errors.Clear();
            }
        }

        const string k_ClassAttr = "class";
        const string k_StyleAttr = "style";
        const string k_GenericPathAttr = UxmlGenericAttributeNames.k_PathAttributeName;
        const string k_GenericSrcAttr = UxmlGenericAttributeNames.k_SrcAttributeName;

        const StringComparison k_Comparison = StringComparison.InvariantCulture;
        public const string k_RootNode = UxmlRootElementFactory.k_ElementName;
        const string k_TemplateNode = UxmlTemplateFactory.k_ElementName;
        const string k_TemplateNameAttr = UxmlGenericAttributeNames.k_NameAttributeName;
        const string k_TemplateInstanceNode = TemplateContainer.UxmlFactory.k_ElementName;
        const string k_TemplateInstanceSourceAttr = TemplateContainer.UxmlTraits.k_TemplateAttributeName;
        const string k_StyleReferenceNode = UxmlStyleFactory.k_ElementName;
        const string k_SlotDefinitionAttr = "slot-name";
        const string k_SlotUsageAttr = "slot";
        const string k_AttributeOverridesNode = UxmlAttributeOverridesFactory.k_ElementName;
        const string k_AttributeOverridesElementNameAttr = UxmlAttributeOverridesTraits.k_ElementNameAttributeName;

        static UxmlAssetAttributeCache s_UxmlAssetAttributeCache = new();

        internal UXMLImporterImpl()
        {
        }

        public UXMLImporterImpl(AssetImportContext context) : base(context)
        {
        }

        public void Import(out VisualTreeAsset asset)
        {
            // Errors from this import will only be logged inside a post-processor.
            // This guarantees that all files were imported in the correct order before logging any errors.
            logger.BeginImport(assetPath);
            ImportXml(assetPath, out asset);
        }

        // This variable is overriden during editor tests
        internal static DefaultLogger logger = new DefaultLogger();

        void LogWarning(VisualTreeAsset asset, ImportErrorType errorType, ImportErrorCode code, object context, IXmlLineInfo xmlLineInfo)
        {
            // If we ever want to use the AssetDatabase error reporting APIs, use m_Context.LogImportWarning() here
            logger.LogError(errorType, code, context, Error.Level.Warning, xmlLineInfo);

            if (asset != null)
                asset.importedWithWarnings = true;
        }

        void LogError(VisualTreeAsset asset, ImportErrorType errorType, ImportErrorCode code, object context, IXmlLineInfo xmlLineInfo)
        {
            // If we ever want to use the AssetDatabase error reporting APIs, use m_Context.LogImportError() here
            logger.LogError(errorType, code, context, Error.Level.Fatal, xmlLineInfo);

            if (asset != null)
                asset.importedWithErrors = true;
        }

        internal static Hash128 GenerateHash(string uxmlPath)
        {
            var h = new Hash128();
            using (var stream = File.OpenRead(uxmlPath))
            {
                int readCount = 0;
                byte[] b = new byte[1024 * 16];
                while ((readCount = stream.Read(b, 0, b.Length)) > 0)
                {
                    for (int i = readCount; i < b.Length; i++)
                    {
                        b[i] = 0;
                    }
                    h.Append(b);
                }
            }

            return h;
        }

        void ImportXml(string xmlPath, out VisualTreeAsset vta)
        {
            var h = GenerateHash(xmlPath);

            CreateVisualTreeAsset(out vta, h);

            XDocument doc;

            try
            {
                doc = XDocument.Load(xmlPath, LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                LogError(vta, ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, null);
                return;
            }

            LoadXmlRoot(doc, vta);
            TryCreateInlineStyleSheet(vta);
        }

        internal void ImportXmlFromString(string xml, out VisualTreeAsset vta)
        {
            byte[] b = Encoding.UTF8.GetBytes(xml);
            var h = Hash128.Compute(b);

            CreateVisualTreeAsset(out vta, h);

            XDocument doc;

            try
            {
                doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                LogError(vta, ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, null);
                return;
            }

            LoadXmlRoot(doc, vta);
            TryCreateInlineStyleSheet(vta);
        }

        void TryCreateInlineStyleSheet(VisualTreeAsset vta)
        {
            if (m_Context != null)
            {
                foreach (var e in m_Errors)
                {
                    var msg = e.ToString();
                    if (e.isWarning)
                        m_Context.LogImportWarning(msg, e.assetPath, e.line);
                    else
                        m_Context.LogImportError(msg, e.assetPath, e.line);

                    LogWarning(
                        vta,
                        ImportErrorType.Semantic,
                        ImportErrorCode.InvalidCssInStyleAttribute,
                        msg,
                        null);
                }
            }

            if (m_Errors.hasErrors)
            {
                // in case of errors preventing the creation of the inline stylesheet,
                // reset rule indices
                foreach (var asset in vta.visualElementAssets)
                    asset.ruleIndex = -1;
                return;
            }

            StyleSheet inlineSheet = ScriptableObject.CreateInstance<StyleSheet>();
            inlineSheet.name = "inlineStyle";
            m_Builder.BuildTo(inlineSheet);
            vta.inlineSheet = inlineSheet;
        }

        void CreateVisualTreeAsset(out VisualTreeAsset vta, Hash128 contentHash)
        {
            vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            vta.visualElementAssets = new List<VisualElementAsset>();
            vta.templateAssets = new List<TemplateAsset>();
            vta.contentHash = contentHash.GetHashCode();
        }

        void LoadXmlRoot(XDocument doc, VisualTreeAsset vta)
        {
            XElement elt = doc.Root;
            if (!string.Equals(elt.Name.LocalName, k_RootNode, k_Comparison))
            {
                LogError(vta,
                    ImportErrorType.Semantic,
                    ImportErrorCode.InvalidRootElement,
                    elt.Name,
                    elt);
                return;
            }

            LoadXml(elt, null, vta, 0);
        }

        void LoadTemplateNode(VisualTreeAsset vta, XElement elt, XElement child)
        {
            bool hasPath = false;
            bool hasSrc = false;
            string name = null;
            string path = null;
            string src = null;
            foreach (var xAttribute in child.Attributes())
            {
                switch (xAttribute.Name.LocalName)
                {
                    case k_GenericPathAttr:
                        hasPath = true;
                        path = xAttribute.Value;
                        break;
                    case k_GenericSrcAttr:
                        hasSrc = true;
                        src = xAttribute.Value;
                        break;
                    case k_TemplateNameAttr:
                        name = xAttribute.Value;
                        if (String.IsNullOrEmpty(name))
                        {
                            LogError(vta,
                                ImportErrorType.Semantic,
                                ImportErrorCode.TemplateHasEmptyName,
                                child,
                                child
                            );
                        }
                        break;
                    default:
                        LogError(vta,
                            ImportErrorType.Semantic,
                            ImportErrorCode.UnknownAttribute,
                            xAttribute.Name.LocalName,
                            child
                        );
                        break;
                }
            }

            if (hasPath == hasSrc)
            {
                LogError(vta,
                    ImportErrorType.Semantic,
                    hasPath ? ImportErrorCode.TemplateSrcAndPathBothSpecified : ImportErrorCode.TemplateMissingPathOrSrcAttribute,
                    null,
                    elt
                );
                return;
            }

            if (String.IsNullOrEmpty(name))
                name = Path.GetFileNameWithoutExtension(path);

            if (vta.TemplateExists(name))
            {
                LogError(vta,
                    ImportErrorType.Semantic,
                    ImportErrorCode.DuplicateTemplateName,
                    name,
                    elt
                );
                return;
            }

            if (hasPath)
            {
                vta.RegisterTemplate(name, path);
            }
            else if (hasSrc)
            {
                var (validationResponse, asset) = ValidateAndLoadResource(elt, vta, src);
                if (validationResponse.result == URIValidationResult.OK)
                {
                    if (asset is VisualTreeAsset treeAsset)
                    {
                        vta.RegisterTemplate(name, treeAsset);
                    }
                    else
                    {
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidAssetType, validationResponse.resolvedProjectRelativePath, elt);
                    }
                }
            }
        }

        new static ImportErrorCode ConvertErrorCode(URIValidationResult result)
        {
            switch (result)
            {
                case URIValidationResult.InvalidURILocation:
                    return ImportErrorCode.ReferenceInvalidURILocation;
                case URIValidationResult.InvalidURIScheme:
                    return ImportErrorCode.ReferenceInvalidURIScheme;
                case URIValidationResult.InvalidURIProjectAssetPath:
                    return ImportErrorCode.ReferenceInvalidURIProjectAssetPath;
                default:
                    throw new ArgumentOutOfRangeException(result.ToString());
            }
        }

        static void AddDependency(string assetPath, XElement templateNode, List<string> dependencies)
        {
            bool hasSrc = false;
            string src = null;

            foreach (var xAttribute in templateNode.Attributes())
            {
                switch (xAttribute.Name.LocalName)
                {
                    case k_GenericSrcAttr:
                        hasSrc = true;
                        src = xAttribute.Value;
                        break;
                }
            }

            if (!hasSrc)
            {
                return;
            }

            var result = URIHelpers.ValidAssetURL(assetPath, src, out _, out var projectRelativePath);

            if (result != URIValidationResult.OK)
            {
                return;
            }

            switch (templateNode.Name.LocalName)
            {
                case k_TemplateNode:
                    var templateDependencies = new HashSet<string>();
                    templateDependencies.Add(assetPath);

                    if (!HasTemplateCircularDependencies(projectRelativePath, templateDependencies))
                    {
                        dependencies.Add(projectRelativePath);
                    }
                    else
                    {
                        logger.LogError(ImportErrorType.Semantic, ImportErrorCode.TemplateHasCircularDependency, projectRelativePath, Error.Level.Warning, templateNode);
                    }
                    break;
                default:
                    dependencies.Add(projectRelativePath);
                    break;
            }
        }

        static void AddAssetDependency(string assetPath, string src, List<string> dependencies)
        {
            if (string.IsNullOrEmpty(src))
                return;

            var result = URIHelpers.ValidAssetURL(assetPath, src, out _, out var projectRelativePath);
            if (result != URIValidationResult.OK)
                return;

            dependencies.Add(projectRelativePath);
        }

        internal static bool HasTemplateCircularDependencies(string templateAssetPath, HashSet<string> templateDependencies)
        {
            if (templateDependencies.Contains(templateAssetPath))
            {
                return true;
            }

            templateDependencies.Add(templateAssetPath);

            try
            {
                var doc = XDocument.Parse(File.ReadAllText(templateAssetPath), LoadOptions.SetLineInfo);

                if (doc != null)
                {
                    return HasTemplateCircularDependencies(doc.Root, templateDependencies, templateAssetPath);
                }
                else
                {
                    // If there is errors parsing the file, there is no circular dependencies
                    return false;
                }
            }
            catch (Exception)
            {
                // If there is errors parsing the file, there is no circular dependencies
                return false;
            }
        }

        internal static bool HasTemplateCircularDependencies(XElement templateElement, HashSet<string> templateDependencies, string rootAssetPath)
        {
            bool hasCircularDependencies = false;

            var elements = templateElement.Elements();
            foreach (var child in elements)
            {
                switch (child.Name.LocalName)
                {
                    case k_TemplateNode:
                        var attributes = child.Attributes();

                        foreach (var xAttribute in attributes)
                        {
                            if (xAttribute.Name.LocalName != k_GenericSrcAttr)
                            {
                                continue;
                            }

                            var src = xAttribute.Value;
                            URIHelpers.ValidAssetURL(rootAssetPath, src, out _, out var projectRelativePath);
                            hasCircularDependencies = HasTemplateCircularDependencies(projectRelativePath, templateDependencies);
        
                            if (!hasCircularDependencies)
                            {
                                templateDependencies.Remove(projectRelativePath);
                            }
                        }

                        break;
                    default:
                        hasCircularDependencies = HasTemplateCircularDependencies(child, templateDependencies, rootAssetPath);
                        break;
                }

                if (hasCircularDependencies)
                {
                    break;
                }
            }

            return hasCircularDependencies;
        }

        internal static void PopulateDependencies(string assetPath, XElement elt, List<string> dependencies)
        {
            var elements = elt.Elements();

            foreach (var child in elements)
            {
                switch (child.Name.LocalName)
                {
                    case k_TemplateNode:
                    case k_StyleReferenceNode:
                        AddDependency(assetPath, child, dependencies);
                        break;
                    default:
                        // Find and add any asset attribute dependency.
                        var typeName = ResolveFullType(child);
                        var assetAttributeNames = s_UxmlAssetAttributeCache.GetAssetAttributeNames(typeName.fullName);
                        foreach (var assetAttribute in assetAttributeNames)
                        {
                            AddAssetDependency(assetPath, child.Attribute(assetAttribute)?.Value, dependencies);
                        }
                        PopulateDependencies(assetPath, child, dependencies);
                        continue;
                }
            }
        }

        void LoadXml(XElement elt, UxmlAsset parent, VisualTreeAsset vta, int orderInDocument)
        {
            var uxmlAsset = ResolveType(elt, vta);
            if (uxmlAsset == null)
            {
                return;
            }

            int parentHash;
            if (parent == null)
            {
                uxmlAsset.parentId = 0;
                parentHash = vta.contentHash;
            }
            else
            {
                uxmlAsset.parentId = parent.id;
                parentHash = parent.id;
            }

            if (!EnsureValidUxmlObjectChild(elt, uxmlAsset, vta))
                return;

            // id includes the parent id, meaning it's dependent on the whole direct hierarchy
            uxmlAsset.id = (vta.GetNextChildSerialNumber() + 585386304) * -1521134295 + parentHash;
            uxmlAsset.orderInDocument = orderInDocument;

            ParseAttributes(elt, uxmlAsset, vta, parent);

            var templateAsset = uxmlAsset as TemplateAsset;
            var vea = uxmlAsset as VisualElementAsset;
            if (templateAsset != null)
                vta.templateAssets.Add(templateAsset);
            else if (uxmlAsset is UxmlObjectAsset uxmlObjectAsset)
                vta.RegisterUxmlObject(uxmlObjectAsset);
            else
                vta.visualElementAssets.Add(vea);

            if (elt.HasElements)
            {
                foreach (XElement child in elt.Elements())
                {
                    if (child.Name.LocalName == k_TemplateNode)
                        LoadTemplateNode(vta, elt, child);
                    else if (vea != null && child.Name.LocalName == k_StyleReferenceNode)
                        LoadStyleReferenceNode(vea, child, vta);
                    else if (templateAsset != null && child.Name.LocalName == k_AttributeOverridesNode)
                        LoadAttributeOverridesNode(templateAsset, child, vta);
                    else
                    {
                        ++orderInDocument;
                        LoadXml(child, uxmlAsset, vta, orderInDocument);
                    }
                }
            }
        }

        void LoadStyleReferenceNode(VisualElementAsset vea, XElement styleElt, VisualTreeAsset vta)
        {
            XAttribute pathAttr = styleElt.Attribute(k_GenericPathAttr);
            bool hasPath = pathAttr != null && !String.IsNullOrEmpty(pathAttr.Value);

            XAttribute srcAttr = styleElt.Attribute(k_GenericSrcAttr);
            bool hasSrc = srcAttr != null && !String.IsNullOrEmpty(srcAttr.Value);

            if (hasPath == hasSrc)
            {
                LogWarning(vta,
                    ImportErrorType.Semantic,
                    hasPath ? ImportErrorCode.StyleReferenceSrcAndPathBothSpecified : ImportErrorCode.StyleReferenceEmptyOrMissingPathOrSrcAttr,
                    null,
                    styleElt);
                return;
            }

            if (hasPath)
            {
                vea.stylesheetPaths.Add(pathAttr.Value);
            }
            else if (hasSrc)
            {
                string errorMessage, projectRelativePath;

                URIValidationResult result = URIHelpers.ValidAssetURL(assetPath, srcAttr.Value, out errorMessage, out projectRelativePath);

                if (result != URIValidationResult.OK)
                {
                    LogError(vta, ImportErrorType.Semantic, ConvertErrorCode(result), errorMessage, styleElt);
                }
                else
                {
                    Object asset = DeclareDependencyAndLoad(projectRelativePath);

                    if (asset is StyleSheet)
                    {
                        vea.stylesheets.Add(asset as StyleSheet);
                    }
                    else
                    {
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidAssetType, projectRelativePath, styleElt);
                    }
                }
            }
        }

        static ProfilerMarker s_ResolveAttributeOverrides = new ProfilerMarker("UXMLImport.ResolveAttributeOverrideTargets");

        void LoadAttributeOverridesNode(TemplateAsset templateAsset, XElement attributeOverridesElt, VisualTreeAsset vta)
        {
            var elementNameAttr = attributeOverridesElt.Attribute(k_AttributeOverridesElementNameAttr);
            if (elementNameAttr == null || String.IsNullOrEmpty(elementNameAttr.Value))
            {
                LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.AttributeOverridesMissingElementNameAttr, null, attributeOverridesElt);
                return;
            }

            var resolvedVisualElementAssetsInTemplate = ListPool<VisualElementAsset>.Get();

            if (vta.TemplateIsAssetReference(templateAsset))
            {
                using var _ = s_ResolveAttributeOverrides.Auto();
                vta.FindElementsByNameInTemplate(templateAsset, elementNameAttr.Value, resolvedVisualElementAssetsInTemplate);
            }

            foreach (var attribute in attributeOverridesElt.Attributes())
            {
                var attributeName = attribute.Name.LocalName;
                if (attributeName == k_AttributeOverridesElementNameAttr)
                    continue;

                if (attributeName is k_ClassAttr or k_StyleAttr or nameof(VisualElement.name))
                {
                    LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.AttributeOverridesInvalidAttr, attributeName, attributeOverridesElt);
                    continue;
                }

                if (resolvedVisualElementAssetsInTemplate.Count > 0)
                {
                    if (s_UxmlAssetAttributeCache.GetAssetAttributeType(resolvedVisualElementAssetsInTemplate[0].fullTypeName, attributeName, out var assetType))
                    {
                        var (response, asset) = ValidateAndLoadResource(attributeOverridesElt, vta, attribute.Value, true);

                        if (response.result == URIValidationResult.OK && !vta.AssetEntryExists(attribute.Value, assetType))
                        {
                            if (asset)
                            {
                                // Force loading using correct attribute type to support cases like Texture2D vs Sprite,
                                asset = AssetDatabase.LoadAssetAtPath(response.resolvedProjectRelativePath, assetType);
                            }
                            vta.RegisterAssetEntry(attribute.Value, assetType, asset);
                        }
                    }
                }

                var attributeOverride = new TemplateAsset.AttributeOverride()
                {
                    m_ElementName = elementNameAttr.Value,
                    m_AttributeName = attribute.Name.LocalName,
                    m_Value = attribute.Value
                };

                templateAsset.attributeOverrides.Add(attributeOverride);
            }

            ListPool<VisualElementAsset>.Release(resolvedVisualElementAssetsInTemplate);
        }

        static (string elementNamespaceName, string fullName) ResolveFullType(XElement elt)
        {
            var elementNamespaceName = elt.Name.NamespaceName;

            if (elementNamespaceName.StartsWith("UnityEditor.Experimental.UIElements") ||
                elementNamespaceName.StartsWith("UnityEngine.Experimental.UIElements"))
            {
                elementNamespaceName = elementNamespaceName.Replace(".Experimental.UIElements", ".UIElements");
            }

            var fullName = String.IsNullOrEmpty(elementNamespaceName)
                ? elt.Name.LocalName
                : elementNamespaceName + "." + elt.Name.LocalName;

            return (elementNamespaceName, fullName);
        }

        UxmlAsset ResolveType(XElement elt, VisualTreeAsset visualTreeAsset)
        {
            var (elementNamespaceName, fullName) = ResolveFullType(elt);

            if (UxmlObjectFactoryRegistry.factories.ContainsKey(fullName))
            {
                return new UxmlObjectAsset(fullName);
            }

            if (elt.Name.LocalName == k_TemplateInstanceNode && elementNamespaceName == typeof(TemplateContainer).Namespace)
            {
                XAttribute sourceAttr = elt.Attribute(k_TemplateInstanceSourceAttr);
                if (sourceAttr == null || String.IsNullOrEmpty(sourceAttr.Value))
                {
                    LogError(visualTreeAsset,
                        ImportErrorType.Semantic,
                        ImportErrorCode.TemplateInstanceHasEmptySource,
                        null,
                        elt);
                    return null;
                }

                string templateName = sourceAttr.Value;
                if (!visualTreeAsset.TemplateExists(templateName))
                {
                    LogError(visualTreeAsset,
                        ImportErrorType.Semantic,
                        ImportErrorCode.UnknownTemplate,
                        templateName,
                        elt);
                    return null;
                }

                return new TemplateAsset(templateName, fullName);
            }

            return new VisualElementAsset(fullName);
        }

        bool EnsureValidUxmlObjectChild(XElement elt, UxmlAsset uxmlAsset, VisualTreeAsset vta)
        {
            if (uxmlAsset is UxmlObjectAsset)
            {
                // UxmlObjects can't be at the root of a visual tree or child of style and template nodes.
                if (elt.Parent == null || elt.Parent.Name.LocalName == k_RootNode)
                {
                    LogError(vta, ImportErrorType.Semantic, ImportErrorCode.InvalidUxmlObjectParent, uxmlAsset.fullTypeName, elt);
                    return false;
                }

                // UxmlObjects can be child of other UxmlObjects or VisualElements.
                return true;
            }

            // Other types can't be child of a UxmlObject.
            if (vta.uxmlObjectIds != null)
            {
                var isUxmlObjectChild = vta.uxmlObjectIds.Contains(uxmlAsset.parentId);
                if (isUxmlObjectChild)
                {
                    LogError(vta, ImportErrorType.Semantic, ImportErrorCode.InvalidUxmlObjectChild, uxmlAsset.fullTypeName, elt);
                    return false;
                }
            }

            return true;
        }

        (URIHelpers.URIValidationResponse response, Object asset) ValidateAndLoadResource(XElement elt, VisualTreeAsset vta, string src, bool logErrorsAsWarnings = false)
        {
            var response = URIHelpers.ValidateAssetURL(assetPath, src);
            var result = response.result;
            var projectRelativePath = response.resolvedProjectRelativePath;

            if (response.hasWarningMessage)
            {
                logger.LogError(ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidURIProjectAssetPath,
                    response.warningMessage, Error.Level.Warning, elt);
            }

            if (result != URIValidationResult.OK)
            {
                if (logErrorsAsWarnings)
                    LogWarning(vta, ImportErrorType.Semantic, ConvertErrorCode(result), response.errorToken, elt);
                else
                    LogError(vta, ImportErrorType.Semantic, ConvertErrorCode(result), response.errorToken, elt);
            }
            else
            {
                var asset = response.resolvedQueryAsset;
                if (asset && m_Context != null)
                {
                    m_Context.DependsOnArtifact(projectRelativePath);
                }
                else if (!(asset is Object)) // This check accounts for a missing reference. We don't want to overwrite it.
                {
                    asset = DeclareDependencyAndLoad(projectRelativePath);
                }

                return (response, asset);
            }

            return (default, null);
        }

        void ParseAttributes(XElement elt, UxmlAsset res, VisualTreeAsset vta, UxmlAsset parent)
        {
            // Since the import process depends on the existence of the type and any UXMLAssetAttributeDescription members
            // We must declare a dependency to the type with the Asset Database to cause reimports when the factories change
            // (See comments in UXMLAssetAttributeSet)
            if (m_Context != null)
            {
                string dependencyKeyName = UxmlCodeDependencies.instance.FormatDependencyKeyName(res.fullTypeName);
                m_Context.DependsOnCustomDependency(dependencyKeyName);
            }

            var vea = res as VisualElementAsset;
            foreach (var xattr in elt.Attributes())
            {
                var attrName = xattr.Name.LocalName;

                if (s_UxmlAssetAttributeCache.GetAssetAttributeType(res.fullTypeName, attrName, out var assetType))
                {
                    res.SetAttribute(xattr.Name.LocalName, xattr.Value);

                    var (response, asset) = ValidateAndLoadResource(elt, vta, xattr.Value, true);

                    if (response.result == URIValidationResult.OK && !vta.AssetEntryExists(xattr.Value, assetType))
                    {
                        if (asset)
                        {
                            // Force loading using correct attribute type to support cases like Texture2D vs Sprite,
                            asset = AssetDatabase.LoadAssetAtPath(response.resolvedProjectRelativePath, assetType);
                        }
                        vta.RegisterAssetEntry(xattr.Value, assetType, asset);
                    }
                    continue;
                }

                // Start with VisualElement special cases
                if (vea != null)
                {
                    switch (attrName)
                    {
                        case k_ClassAttr:
                            vea.SetAttribute(xattr.Name.LocalName, xattr.Value);
                            vea.classes = xattr.Value.Split(' ');
                            continue;
                        case "content-container":
                        case "contentContainer":
                            vea.SetAttribute(xattr.Name.LocalName, xattr.Value);
                            if (attrName == "contentContainer")
                            {
                            }
                            if (vta.contentContainerId != 0)
                            {
                                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.DuplicateContentContainer, null, elt);
                                continue;
                            }
                            vta.contentContainerId = vea.id;
                            continue;
                        case k_SlotDefinitionAttr:
                            LogWarning(vta, ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, elt);
                            if (String.IsNullOrEmpty(xattr.Value))
                                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotDefinitionHasEmptyName, null, elt);
                            else if (!vta.AddSlotDefinition(xattr.Value, vea.id))
                                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.DuplicateSlotDefinition, xattr.Value, elt);
                            continue;
                        case k_SlotUsageAttr:
                            LogWarning(vta, ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, elt);
                            var templateAsset = parent as TemplateAsset;
                            if (templateAsset == null)
                            {
                                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotUsageInNonTemplate, parent, elt);
                                continue;
                            }
                            if (string.IsNullOrEmpty(xattr.Value))
                            {
                                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotUsageHasEmptyName, null, elt);
                                continue;
                            }
                            templateAsset.AddSlotUsage(xattr.Value, vea.id);
                            continue;
                        case k_StyleAttr:
                            vea.SetAttribute(xattr.Name.LocalName, xattr.Value);
                            ExCSS.StyleSheet parsed = new Parser().Parse("* { " + xattr.Value + " }");
                            if (parsed.Errors.Count != 0)
                            {
                                LogWarning(
                                    vta,
                                    ImportErrorType.Semantic,
                                    ImportErrorCode.InvalidCssInStyleAttribute,
                                    parsed.Errors.Aggregate("", (s, error) => s + error.ToString() + "\n"),
                                    xattr);
                                continue;
                            }
                            if (parsed.StyleRules.Count != 1)
                            {
                                LogWarning(
                                    vta,
                                    ImportErrorType.Semantic,
                                    ImportErrorCode.InvalidCssInStyleAttribute,
                                    "Expected one style rule, found " + parsed.StyleRules.Count,
                                    xattr);
                                continue;
                            }

                            // Each vea will creates 0 or 1 style rule, with one or more properties
                            // they don't have selectors and are directly referenced by index
                            // it's then applied during tree cloning
                            m_Builder.BeginRule(-1);
                            m_CurrentLine = ((IXmlLineInfo)xattr).LineNumber;
                            foreach (var prop in parsed.StyleRules[0].Declarations)
                            {
                                m_Builder.BeginProperty(prop.Name);
                                VisitValue(prop.Term);
                                m_Builder.EndProperty();
                            }

                            vea.ruleIndex = m_Builder.EndRule();
                            continue;
                    }
                }

                res.SetAttribute(xattr.Name.LocalName, xattr.Value);
            }
        }
    }

    internal enum ImportErrorCode
    {
        InvalidRootElement,
        DuplicateTemplateName,
        UnknownTemplate,
        UnknownElement,
        UnknownAttribute,
        InvalidXml,
        InvalidCssInStyleAttribute,
        TemplateMissingPathOrSrcAttribute,
        TemplateSrcAndPathBothSpecified,
        TemplateHasEmptyName,
        TemplateInstanceHasEmptySource,
        StyleReferenceEmptyOrMissingPathOrSrcAttr,
        StyleReferenceSrcAndPathBothSpecified,
        SlotsAreExperimental,
        DuplicateSlotDefinition,
        SlotUsageInNonTemplate,
        SlotDefinitionHasEmptyName,
        SlotUsageHasEmptyName,
        DuplicateContentContainer,
        DeprecatedAttributeName,
        ReplaceByAttributeName,
        AttributeOverridesMissingElementNameAttr,
        AttributeOverridesInvalidAttr,
        ReferenceInvalidURILocation,
        ReferenceInvalidURIScheme,
        ReferenceInvalidURIProjectAssetPath,
        ReferenceInvalidAssetType,
        TemplateHasCircularDependency,
        InvalidUxmlObjectParent,
        InvalidUxmlObjectChild,
    }

    internal enum ImportErrorType
    {
        Syntax,
        Semantic
    }
}
