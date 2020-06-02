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
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.StyleSheets;
using UnityEngine;
using Object = UnityEngine.Object;

using UnityEngine.UIElements;
using StyleSheet = UnityEngine.UIElements.StyleSheet;

namespace UnityEditor.UIElements
{
    // Make sure UXML is imported after assets than can be addressed in USS
    [ScriptedImporter(version: 7, ext: "uxml", importQueueOffset: 1100)]
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
                Info,
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
                    case ImportErrorCode.ReferenceInvalidURILocation:
                        return "The specified URL is empty or invalid : {0}";
                    case ImportErrorCode.ReferenceInvalidURIScheme:
                        return "The scheme specified for the URI is invalid : {0}";
                    case ImportErrorCode.ReferenceInvalidURIProjectAssetPath:
                        return "The specified URI does not exist in the current project : {0}";
                    case ImportErrorCode.ReferenceInvalidAssetType:
                        return "The specified URI refers to an invalid asset : {0}";
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
                        case Error.Level.Info:
                            Debug.LogFormat(obj, error.ToString());
                            break;
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
                        case Error.Level.Info:
                            Debug.Log(error.ToString());
                            break;
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

        void ImportXml(string xmlPath, out VisualTreeAsset vta)
        {
            var h = new Hash128();
            using (var stream = File.OpenRead(xmlPath))
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

            CreateVisualTreeAsset(out vta, h);

            XDocument doc;

            try
            {
                doc = XDocument.Load(xmlPath, LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                logger.LogError(ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, Error.Level.Fatal, null);
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
                logger.LogError(ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, Error.Level.Fatal, null);
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
                    if (e.isWarning)
                        m_Context.LogImportWarning(e.ToString(), e.assetPath, e.line);
                    else
                        m_Context.LogImportError(e.ToString(), e.assetPath, e.line);
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
                logger.LogError(ImportErrorType.Semantic,
                    ImportErrorCode.InvalidRootElement,
                    elt.Name,
                    Error.Level.Fatal,
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
                            logger.LogError(ImportErrorType.Semantic,
                                ImportErrorCode.TemplateHasEmptyName,
                                child,
                                Error.Level.Fatal,
                                child
                            );
                        }
                        break;
                    default:
                        logger.LogError(ImportErrorType.Semantic,
                            ImportErrorCode.UnknownAttribute,
                            xAttribute.Name.LocalName,
                            Error.Level.Fatal,
                            child
                        );
                        break;
                }
            }

            if (hasPath == hasSrc)
            {
                logger.LogError(ImportErrorType.Semantic,
                    hasPath ? ImportErrorCode.TemplateSrcAndPathBothSpecified : ImportErrorCode.TemplateMissingPathOrSrcAttribute,
                    null,
                    Error.Level.Fatal,
                    elt
                );
                return;
            }

            if (String.IsNullOrEmpty(name))
                name = Path.GetFileNameWithoutExtension(path);

            if (vta.TemplateExists(name))
            {
                logger.LogError(ImportErrorType.Semantic,
                    ImportErrorCode.DuplicateTemplateName,
                    name,
                    Error.Level.Fatal,
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
                string errorMessage, projectRelativePath;

                URIValidationResult result = URIHelpers.ValidAssetURL(assetPath, src, out errorMessage, out projectRelativePath);

                if (result != URIValidationResult.OK)
                {
                    logger.LogError(ImportErrorType.Semantic, ConvertErrorCode(result), errorMessage, Error.Level.Fatal, elt);
                }
                else
                {
                    Object asset = DeclareDependencyAndLoad(projectRelativePath);

                    if (asset is VisualTreeAsset)
                    {
                        vta.RegisterTemplate(name, asset as VisualTreeAsset);
                    }
                    else
                    {
                        logger.LogError(ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidAssetType, projectRelativePath, Error.Level.Fatal, elt);
                    }
                }
            }
        }

        static ImportErrorCode ConvertErrorCode(URIValidationResult result)
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

            if (hasSrc)
            {
                string errorMessage, projectRelativePath;

                URIValidationResult result = URIHelpers.ValidAssetURL(assetPath, src, out errorMessage, out projectRelativePath);

                if (result == URIValidationResult.OK)
                {
                    dependencies.Add(projectRelativePath);
                }
            }
        }

        internal static void PopulateDependencies(string assetPath, XElement elt, List<string> dependencies)
        {
            foreach (var child in elt.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case k_TemplateNode:
                    case k_StyleReferenceNode:
                        AddDependency(assetPath, child, dependencies);
                        break;
                    default:
                        PopulateDependencies(assetPath, child, dependencies);
                        continue;
                }
            }
        }

        void LoadXml(XElement elt, VisualElementAsset parent, VisualTreeAsset vta, int orderInDocument)
        {
            VisualElementAsset vea = ResolveType(elt, vta);
            if (vea == null)
            {
                return;
            }

            int parentHash;
            if (parent == null)
            {
                vea.parentId = 0;
                parentHash = vta.contentHash;
            }
            else
            {
                vea.parentId = parent.id;
                parentHash = parent.id;
            }

            // id includes the parent id, meaning it's dependent on the whole direct hierarchy
            vea.id = (vta.GetNextChildSerialNumber() + 585386304) * -1521134295 + parentHash;
            vea.orderInDocument = orderInDocument;

            bool startedRule = ParseAttributes(elt, vea, vta, parent);

            // each vea will creates 0 or 1 style rule, with one or more properties
            // they don't have selectors and are directly referenced by index
            // it's then applied during tree cloning
            vea.ruleIndex = startedRule ? m_Builder.EndRule() : -1;
            var templateAsset = vea as TemplateAsset;
            if (templateAsset != null)
                vta.templateAssets.Add(templateAsset);
            else
                vta.visualElementAssets.Add(vea);

            if (elt.HasElements)
            {
                foreach (XElement child in elt.Elements())
                {
                    if (child.Name.LocalName == k_TemplateNode)
                        LoadTemplateNode(vta, elt, child);
                    else if (child.Name.LocalName == k_StyleReferenceNode)
                        LoadStyleReferenceNode(vea, child);
                    else if (templateAsset != null && child.Name.LocalName == k_AttributeOverridesNode)
                        LoadAttributeOverridesNode(templateAsset, child);
                    else
                    {
                        ++orderInDocument;
                        LoadXml(child, vea, vta, orderInDocument);
                    }
                }
            }
        }

        void LoadStyleReferenceNode(VisualElementAsset vea, XElement styleElt)
        {
            XAttribute pathAttr = styleElt.Attribute(k_GenericPathAttr);
            bool hasPath = pathAttr != null && !String.IsNullOrEmpty(pathAttr.Value);

            XAttribute srcAttr = styleElt.Attribute(k_GenericSrcAttr);
            bool hasSrc = srcAttr != null && !String.IsNullOrEmpty(srcAttr.Value);

            if (hasPath == hasSrc)
            {
                logger.LogError(ImportErrorType.Semantic, hasPath ? ImportErrorCode.StyleReferenceSrcAndPathBothSpecified : ImportErrorCode.StyleReferenceEmptyOrMissingPathOrSrcAttr, null, Error.Level.Warning, styleElt);
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
                    logger.LogError(ImportErrorType.Semantic, ConvertErrorCode(result), errorMessage, Error.Level.Fatal, styleElt);
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
                        logger.LogError(ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidAssetType, projectRelativePath, Error.Level.Fatal, styleElt);
                    }
                }
            }
        }

        void LoadAttributeOverridesNode(TemplateAsset templateAsset, XElement attributeOverridesElt)
        {
            var elementNameAttr = attributeOverridesElt.Attribute(k_AttributeOverridesElementNameAttr);
            if (elementNameAttr == null || String.IsNullOrEmpty(elementNameAttr.Value))
            {
                logger.LogError(ImportErrorType.Semantic, ImportErrorCode.AttributeOverridesMissingElementNameAttr, null, Error.Level.Warning, attributeOverridesElt);
                return;
            }

            foreach (var attribute in attributeOverridesElt.Attributes())
            {
                if (attribute.Name.LocalName == k_AttributeOverridesElementNameAttr)
                    continue;

                var attributeOverride = new TemplateAsset.AttributeOverride()
                {
                    m_ElementName = elementNameAttr.Value,
                    m_AttributeName = attribute.Name.LocalName,
                    m_Value = attribute.Value
                };

                templateAsset.attributeOverrides.Add(attributeOverride);
            }
        }

        VisualElementAsset ResolveType(XElement elt, VisualTreeAsset visualTreeAsset)
        {
            string elementNamespaceName = elt.Name.NamespaceName;

            if (elementNamespaceName.StartsWith("UnityEditor.Experimental.UIElements") ||
                elementNamespaceName.StartsWith("UnityEngine.Experimental.UIElements"))
            {
                elementNamespaceName = elementNamespaceName.Replace(".Experimental.UIElements", ".UIElements");
            }

            string fullName = String.IsNullOrEmpty(elementNamespaceName)
                ? elt.Name.LocalName
                : elementNamespaceName + "." + elt.Name.LocalName;

            if (elt.Name.LocalName == k_TemplateInstanceNode && elementNamespaceName == typeof(TemplateContainer).Namespace)
            {
                XAttribute sourceAttr = elt.Attribute(k_TemplateInstanceSourceAttr);
                if (sourceAttr == null || String.IsNullOrEmpty(sourceAttr.Value))
                {
                    logger.LogError(ImportErrorType.Semantic, ImportErrorCode.TemplateInstanceHasEmptySource, null,
                        Error.Level.Fatal, elt);
                    return null;
                }

                string templateName = sourceAttr.Value;
                if (!visualTreeAsset.TemplateExists(templateName))
                {
                    logger.LogError(ImportErrorType.Semantic, ImportErrorCode.UnknownTemplate, templateName,
                        Error.Level.Fatal, elt);
                    return null;
                }

                return new TemplateAsset(templateName, fullName);
            }

            return new VisualElementAsset(fullName);
        }

        bool ParseAttributes(XElement elt, VisualElementAsset res, VisualTreeAsset vta, VisualElementAsset parent)
        {
            bool startedRule = false;

            foreach (XAttribute xattr in elt.Attributes())
            {
                string attrName = xattr.Name.LocalName;

                // start with special cases
                switch (attrName)
                {
                    case "class":
                        res.AddProperty(xattr.Name.LocalName, xattr.Value);
                        res.classes = xattr.Value.Split(' ');
                        continue;
                    case "content-container":
                    case "contentContainer":
                        res.AddProperty(xattr.Name.LocalName, xattr.Value);
                        if (attrName == "contentContainer")
                        {
                        }
                        if (vta.contentContainerId != 0)
                        {
                            logger.LogError(ImportErrorType.Semantic, ImportErrorCode.DuplicateContentContainer, null, Error.Level.Fatal, elt);
                            continue;
                        }
                        vta.contentContainerId = res.id;
                        continue;
                    case k_SlotDefinitionAttr:
                        logger.LogError(ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, Error.Level.Warning, elt);
                        if (String.IsNullOrEmpty(xattr.Value))
                            logger.LogError(ImportErrorType.Semantic, ImportErrorCode.SlotDefinitionHasEmptyName, null, Error.Level.Fatal, elt);
                        else if (!vta.AddSlotDefinition(xattr.Value, res.id))
                            logger.LogError(ImportErrorType.Semantic, ImportErrorCode.DuplicateSlotDefinition, xattr.Value, Error.Level.Fatal, elt);
                        continue;
                    case k_SlotUsageAttr:
                        logger.LogError(ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, Error.Level.Warning, elt);
                        var templateAsset = parent as TemplateAsset;
                        if (templateAsset == null)
                        {
                            logger.LogError(ImportErrorType.Semantic, ImportErrorCode.SlotUsageInNonTemplate, parent, Error.Level.Fatal, elt);
                            continue;
                        }
                        if (string.IsNullOrEmpty(xattr.Value))
                        {
                            logger.LogError(ImportErrorType.Semantic, ImportErrorCode.SlotUsageHasEmptyName, null, Error.Level.Fatal, elt);
                            continue;
                        }
                        templateAsset.AddSlotUsage(xattr.Value, res.id);
                        continue;
                    case "style":
                        res.AddProperty(xattr.Name.LocalName, xattr.Value);

                        ExCSS.StyleSheet parsed = new Parser().Parse("* { " + xattr.Value + " }");
                        if (parsed.Errors.Count != 0)
                        {
                            logger.LogError(
                                ImportErrorType.Semantic,
                                ImportErrorCode.InvalidCssInStyleAttribute,
                                parsed.Errors.Aggregate("", (s, error) => s + error.ToString() + "\n"),
                                Error.Level.Warning,
                                xattr);
                            continue;
                        }
                        if (parsed.StyleRules.Count != 1)
                        {
                            logger.LogError(
                                ImportErrorType.Semantic,
                                ImportErrorCode.InvalidCssInStyleAttribute,
                                "Expected one style rule, found " + parsed.StyleRules.Count,
                                Error.Level.Warning,
                                xattr);
                            continue;
                        }
                        m_Builder.BeginRule(-1);
                        startedRule = true;
                        foreach (Property prop in parsed.StyleRules[0].Declarations)
                        {
                            m_Builder.BeginProperty(prop.Name);
                            VisitValue(prop.Term);
                            m_Builder.EndProperty();
                        }

                        // Don't call m_Builder.EndRule() here, it's done in LoadXml to get the rule index at the same time !
                        continue;
                }

                res.AddProperty(xattr.Name.LocalName, xattr.Value);
            }
            return startedRule;
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
        ReferenceInvalidURILocation,
        ReferenceInvalidURIScheme,
        ReferenceInvalidURIProjectAssetPath,
        ReferenceInvalidAssetType
    }

    internal enum ImportErrorType
    {
        Syntax,
        Semantic
    }
}
