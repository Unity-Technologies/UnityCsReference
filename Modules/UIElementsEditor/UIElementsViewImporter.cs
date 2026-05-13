// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Unity.Profiling;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using StyleSheet = UnityEngine.UIElements.StyleSheet;

namespace UnityEditor.UIElements
{
    // Make sure UXML is imported after assets than can be addressed in USS
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [HelpURL("UIE-VisualTree-landing")]
    [ScriptedImporter(version: 32, ext: "uxml", importQueueOffset: 1102)]
    [ExcludeFromPreset]
    internal class UIElementsViewImporter : ScriptedImporter
    {
        // Parses the XML file to figure out dependencies to other UXML/USS files
        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(File.ReadAllText(FileUtil.PathToAbsolutePath(assetPath)), LoadOptions.SetLineInfo);
            }
            catch (Exception)
            {
                // We want to be silent here, all XML syntax errors will be reported during the actual import
                return Array.Empty<string>();
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

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class UXMLImporterImpl : StyleValueImporter
    {
        private class LogAuthoringIdConflictScope : IDisposable
        {
            UXMLImporterImpl m_Importer;
            VisualTreeAsset m_VisualTreeAsset;
            IXmlLineInfo m_LineInfo;

            public bool conflictDetected { get; private set; } = false;

            public LogAuthoringIdConflictScope(UXMLImporterImpl importer, VisualTreeAsset vta, IXmlLineInfo lineInfo)
            {
                m_Importer = importer;
                m_VisualTreeAsset = vta;
                m_LineInfo = lineInfo;
                m_VisualTreeAsset.onAuthoringIdConflictResolved += LogError;
            }

            void LogError(UxmlAsset asset, int oldId, int newId)
            {
                conflictDetected = true;
                m_Importer.LogError(asset.visualTreeAsset, ImportErrorType.Semantic, ImportErrorCode.DuplicateAuthoringId, oldId, m_LineInfo);
            }

            public void Dispose()
            {
                m_VisualTreeAsset.onAuthoringIdConflictResolved -= LogError;
                m_Importer = null;
                m_VisualTreeAsset = null;
            }
        }

        const string k_ClassAttr = "class";
        const string k_StyleAttr = "style";
        const string k_GenericPathAttr = "path";
        const string k_GenericSrcAttr = "src";

        #pragma warning disable CS0618 // Type or member is obsolete
        const StringComparison k_Comparison = StringComparison.InvariantCulture;
        public const string k_RootNode = VisualTreeAsset.RootElementName;
        const string k_TemplateNode = "Template";
        const string k_TemplateNameAttr = "name";
        const string k_TemplateInstanceNode = "Instance";
        const string k_TemplateInstanceSourceAttr = "template";
        const string k_StyleReferenceNode = "Style";
        const string k_SlotDefinitionAttr = "slot-name";
        const string k_SlotUsageAttr = "slot";
        const string k_AttributeOverridesNode = "AttributeOverrides";
        const string k_AttributeOverridesElementNameAttr = "element-name";
        #pragma warning restore CS0618 // Type or member is obsolete

        static UxmlAssetAttributeCache s_UxmlAssetAttributeCache = new();

        /// <summary>
        /// Controls whether URL paths are automatically upgraded during import.
        /// When false, the importer will not set importerWithUpdatedUrls flag.
        /// </summary>
        internal bool enableAutomaticUrlUpgrades = true;

        /// <summary>
        /// Controls whether obsolete attribute names are automatically renamed during import.
        /// When false, the importer will not set importedWithObsoleteAttributeNames flag.
        /// </summary>
        internal bool enableAutomaticAttributeRenames = true;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal UXMLImporterImpl()
        {
        }

        public UXMLImporterImpl(AssetImportContext context) : base(context)
        {
        }

        public void Import(out VisualTreeAsset asset)
        {
            ImportXml(assetPath, out asset);
        }

        void LogWarning(VisualTreeAsset asset, ImportErrorType errorType, ImportErrorCode code, object context, IXmlLineInfo xmlLineInfo)
        {
            if (m_Context != null)
            {
                var error = FormatError(code, errorType, xmlLineInfo, context);
                m_Context.LogImportWarning(error);
            }

            if (asset != null)
                asset.importedWithWarnings = true;
        }

        void LogError(VisualTreeAsset asset, ImportErrorType errorType, ImportErrorCode code, object context, IXmlLineInfo xmlLineInfo)
        {
            if (m_Context != null)
            {
                var error = FormatError(code, errorType, xmlLineInfo, context);
                m_Context.LogImportError(error);
            }

            if (asset != null)
                asset.importedWithErrors = true;
        }

        public static string FormatError(ImportErrorCode code, ImportErrorType error, IXmlLineInfo xmlLineInfo, object context)
        {
            string message = ErrorMessage(code);
            string lineInfo = xmlLineInfo == null ? ""
                : string.Format(" ({0},{1})", xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
            return string.Format("{0}: {1} - {2}", lineInfo, error,
                string.Format(message, context == null ? "<null>" : context.ToString()));
        }

        static string ErrorMessage(ImportErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ImportErrorCode.InvalidXml:
                    return "Xml is not valid, exception during parsing: {0}";
                case ImportErrorCode.InvalidRootElement:
                    return "Expected the UXML Root element name to be '" + k_RootNode + "', found '{0}'";
                case ImportErrorCode.MultipleRootElements:
                    return "Multiple UXML elements were found. Expected a single root UXML element to be found";
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
                case ImportErrorCode.DuplicateAuthoringId:
                    return "Uxml object has an authoring-id that is already used by another Uxml object: {0}";
                case ImportErrorCode.AttributeParsing:
                    return "Could not parse attribute value: {0}";
                default:
                    throw new ArgumentOutOfRangeException("Unhandled error code " + errorCode);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static Hash128 GenerateHash(string uxmlPath)
        {
            var h = new Hash128();
            using (var stream = File.OpenRead(FileUtil.PathToAbsolutePath(uxmlPath)))
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
                doc = XDocument.Load(FileUtil.PathToAbsolutePath(xmlPath), LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                LogError(vta, ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, null);
                return;
            }

            LoadXmlRoot(doc, vta);
            TryCreateInlineStyleSheet(vta);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
                foreach (var asset in vta.DepthFirstTraversal())
                {
                    if (asset is VisualElementAsset vea)
                        vea.ruleIndex = -1;
                }
                return;
            }

            StyleSheet inlineSheet = ScriptableObject.CreateInstance<StyleSheet>();
            inlineSheet.name = "inlineStyle";
            m_Builder.BuildTo(inlineSheet);
            vta.inlineSheet = inlineSheet;
            vta.inlineSheet.hideFlags = HideFlags.NotEditable;
        }

        void CreateVisualTreeAsset(out VisualTreeAsset vta, Hash128 contentHash)
        {
            vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            vta.contentHash = contentHash.GetHashCode();
        }

        void LoadXmlRoot(XDocument doc, VisualTreeAsset vta)
        {
            XElement elt = doc.Root;

            // Check UXML as a local name (this was kept to ensure we're not breaking any existing uxml files) or as a fully
            // resolved type name. When the local name is not UXML, the fully resolved namespace must be UnityEngine.UIElements.
            if (!string.Equals(elt.Name.LocalName, k_RootNode, k_Comparison) &&
                !string.Equals(ResolveFullType(elt).fullName, "UnityEngine.UIElements.UXML", k_Comparison))
            {
                LogError(vta,
                    ImportErrorType.Semantic,
                    ImportErrorCode.InvalidRootElement,
                    elt.Name,
                    elt);
                return;
            }

            LoadXml(elt, null, null, vta);
            UxmlSerializer.CreateSerializedDataOverrides(vta);
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
                        if (validationResponse.resolvedUrlChanged)
                            vta.importerWithUpdatedUrls = true;

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
                        // There is no AssetImportContext here so we have to log the error directly
                        var error = FormatError(ImportErrorCode.TemplateHasCircularDependency, ImportErrorType.Semantic, templateNode, projectRelativePath);
                        Debug.LogError(error);
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
                var doc = XDocument.Parse(File.ReadAllText(FileUtil.PathToAbsolutePath(templateAssetPath)), LoadOptions.SetLineInfo);

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

        void LoadXml(XElement elt, UxmlAsset parent, UxmlSerializedData parentSerializedData, VisualTreeAsset vta)
        {
            if (!TryResolveType(elt, parent, vta, out var uxmlAsset, out var uxmlSerializedDataDescription))
                return;

            if (!EnsureValidUxmlObjectChild(elt, uxmlAsset, vta, parent))
                return;

            var vea = uxmlAsset as VisualElementAsset;
            var templateAsset = uxmlAsset as TemplateAsset;
            var uxmlObjectAsset = uxmlAsset as UxmlObjectAsset;

            if (vea is { isRoot: true })
            {
                vta.SetRootAsset(vea);
            }
            else
            {
                var actualParent = parent ?? vta.visualTree;
                actualParent.Add(uxmlAsset);
            }

            var currentSerializedData = vea?.serializedData;

            if (uxmlObjectAsset != null && parentSerializedData != null)
            {
                // If its a field name we do nothing and forward the parentSerializedData data to child UxmlObjects.
                if (!uxmlObjectAsset.isField)
                {
                    parentSerializedData = LoadUxmlObject(uxmlObjectAsset, parentSerializedData, uxmlSerializedDataDescription);
                    currentSerializedData = parentSerializedData;
                }
            }
            else
            {
                parentSerializedData = currentSerializedData;
            }

            ParseAttributes(elt, uxmlAsset, vta, uxmlSerializedDataDescription, currentSerializedData);

            if (currentSerializedData != null)
                currentSerializedData.uxmlAssetId = uxmlAsset.id;

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
                        LoadXml(child, uxmlAsset, parentSerializedData, vta);
                    }
                }
            }
        }

        /// <summary>
        /// Create the UxmlObject serialized data and apply it to the corresponding field in the parent serialized data.
        /// </summary>
        /// <param name="uxmlObjectAsset">The UxmlObject asset</param>
        /// <param name="parentSerializedData">The serialized data of the current parent VisualElement or UxmlObject if we are nested.
        /// Note this may not match the parent of <paramref name="uxmlObjectAsset"/> if it is a UxmlObject field name.</param>
        /// <param name="uxmlSerializedDataDescription">Description of the <paramref name="uxmlObjectAsset"/></param>
        /// <returns></returns>
        UxmlSerializedData LoadUxmlObject(UxmlObjectAsset uxmlObjectAsset, UxmlSerializedData parentSerializedData, UxmlSerializedDataDescription uxmlSerializedDataDescription)
        {
            // We expect the UXML with UxmlObjects to look like this:
            // <visual-element>
            //   <element-field-name>
            //     <field-value/>
            //     <field-value/>
            //   </element-field-name>
            // </visual-element>
            // Legacy fields, such as those found in MultiColumnListView and MultiColumnTreeView,
            // do not have a root element and look like this:
            // <visual-element>
            //   <field-value/>
            //   <field-value/>
            // </visual-element>

            // If its a field name we do nothing and forward the parentSerializedData data to child UxmlObjects
            var serializedData = uxmlObjectAsset.fullTypeName == UxmlAsset.NullNodeType ? null : CreateSerializedData(uxmlSerializedDataDescription);

            // Find a matching UxmlObjectReference field.
            var rootName = uxmlObjectAsset.parentAsset is UxmlObjectAsset parentUxmlObjectAsset && parentUxmlObjectAsset.isField ? parentUxmlObjectAsset.fullTypeName : null;
            var uxmlObjectType = uxmlSerializedDataDescription?.serializedDataType;
            var parentSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(parentSerializedData.GetType().DeclaringType.FullName);

            var uxmlObjectReferenceDescription = FindMatchingUxmlObjectField(parentSerializedDataDescription, rootName, uxmlObjectType, true);
            if (uxmlObjectReferenceDescription != null)
            {
                AssignUxmlObjectToField(serializedData, parentSerializedData, uxmlObjectReferenceDescription);
            }
            else
            {
                var warning = string.Format(UxmlSerializedUxmlObjectAttributeDescription.k_UxmlObjectWithNoFieldWarning, uxmlObjectAsset.fullTypeName, parentSerializedDataDescription.uxmlFullName);

                // Try to find a match by ignoring the root name
                uxmlObjectReferenceDescription = FindMatchingUxmlObjectField(parentSerializedDataDescription, null, uxmlObjectType, false);
                if (uxmlObjectReferenceDescription != null)
                {
                    warning += "\n" + string.Format(UxmlSerializedUxmlObjectAttributeDescription.k_UxmlObjectMismatchFieldHint, uxmlObjectReferenceDescription.name, uxmlObjectReferenceDescription.rootName);
                    AssignUxmlObjectToField(serializedData, parentSerializedData, uxmlObjectReferenceDescription);
                }

                Debug.LogWarning(warning, uxmlObjectAsset.visualTreeAsset);
            }

            return serializedData;
        }

        UxmlSerializedData CreateSerializedData(UxmlSerializedDataDescription description)
        {
            RegisterDependency(description.uxmlFullName);
            return description.CreateSerializedData();
        }

        void RegisterDependency(string fullName)
        {
            if (m_Context != null)
            {
                var dependencyKeyName = UxmlCodeDependencies.instance.FormatSerializedDependencyKeyName(fullName);
                m_Context.DependsOnCustomDependency(dependencyKeyName);
            }
        }

        static UxmlSerializedUxmlObjectAttributeDescription FindMatchingUxmlObjectField(UxmlSerializedDataDescription desc, string rootName, Type uxmlObjectType, bool checkRootName)
        {
            foreach (var attribute in desc.serializedAttributes)
            {
                if (attribute is not UxmlSerializedUxmlObjectAttributeDescription uxmlObjectDescription)
                    continue;

                // Check root name
                if (checkRootName && uxmlObjectDescription.rootName != rootName)
                    continue;

                // Check type is compatible
                var expectedObjectType = uxmlObjectDescription.isList ? uxmlObjectDescription.type.GetArrayOrListElementType() : uxmlObjectDescription.type;
                if (uxmlObjectType == null || expectedObjectType.IsAssignableFrom(uxmlObjectType))
                    return uxmlObjectDescription;
            }
            return null;
        }

        static void AssignUxmlObjectToField(UxmlSerializedData uxmlObjectData, UxmlSerializedData parentSerializedData, UxmlSerializedUxmlObjectAttributeDescription uxmlObjectReferenceDescription)
        {
            object value = null;

            if (uxmlObjectReferenceDescription.isList)
            {
                var listType = uxmlObjectReferenceDescription.type;
                var collectionInstance = uxmlObjectReferenceDescription.GetSerializedValue(parentSerializedData);

                if (listType.IsArray)
                {
                    if (collectionInstance is Array array)
                    {
                        // We dont know the final size of the array until all elements have been parsed so we need to increase it as we go.
                        var newArray = Array.CreateInstance(listType.GetArrayOrListElementType(), array.Length + 1);
                        Array.Copy(array, 0, newArray, 0, array.Length);
                        array = newArray;
                    }
                    else
                    {
                        array = Array.CreateInstance(listType.GetArrayOrListElementType(), 1);
                    }

                    array.SetValue(uxmlObjectData, array.Length - 1);
                    value = array;
                }
                else
                {
                    var list = collectionInstance as IList;
                    list ??= (IList)Activator.CreateInstance(listType);
                    list.Add(uxmlObjectData);
                    value = list;
                }
            }
            else
            {
                value = uxmlObjectData;

                // Display a warning when uxml file contains more than one named UxmlObject of a type defined in a single instance attribute
                if (uxmlObjectReferenceDescription.GetSerializedValueAttributeFlags(parentSerializedData) == UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml)
                {
                    Debug.LogWarning(string.Format(UxmlSerializedUxmlObjectAttributeDescription.k_MultipleUxmlObjectsWarning, uxmlObjectReferenceDescription.name));
                }
            }

            uxmlObjectReferenceDescription.SetSerializedValue(parentSerializedData, value, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
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
                var validationResponse = ValidateAndLoadResource(styleElt, vta, srcAttr.Value).response;
                if (validationResponse.result == URIValidationResult.OK)
                {
                    var asset = DeclareDependencyAndLoad(validationResponse.resolvedProjectRelativePath);
                    if (asset is StyleSheet styleSheet)
                    {
                        if (validationResponse.resolvedUrlChanged)
                            vta.importerWithUpdatedUrls = true;
                        vea.stylesheets.Add(styleSheet);
                    }
                    else
                    {
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidAssetType, validationResponse.resolvedProjectRelativePath, styleElt);
                    }
                }
            }
        }

        static ProfilerMarker s_ResolveAttributeOverrides = new ProfilerMarker(ProfilerCategory.UIToolkit, "UXMLImport.ResolveAttributeOverrideTargets");

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

            UxmlSerializedDataDescription uxmlSerializedDataDescription = null;

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
                    Type assetType = null;
                    uxmlSerializedDataDescription ??= UxmlSerializedDataRegistry.GetDescription(resolvedVisualElementAssetsInTemplate[0].fullTypeName);

                    // Extract the asset type from the UxmlSerializedData
                    if (uxmlSerializedDataDescription?.FindAttributeWithUxmlName(attributeName) is UxmlSerializedAttributeDescription attributeDescription &&
                        attributeDescription.isUnityObject)
                        assetType = attributeDescription.type;

                    if (assetType != null || s_UxmlAssetAttributeCache.GetAssetAttributeType(resolvedVisualElementAssetsInTemplate[0].fullTypeName, attributeName, out assetType))
                    {
                        var (response, asset) = ValidateAndLoadResource(attributeOverridesElt, vta, attribute.Value, true);

                        if (response.result == URIValidationResult.OK && !vta.AssetEntryExists(attribute.Value, assetType))
                        {
                            asset = ExtractSubAssetFromParent(asset, assetType, response);
                            vta.RegisterAssetEntry(attribute.Value, assetType, asset);
                        }
                    }
                }

                var attributeOverride = new TemplateAsset.AttributeOverride()
                {
                    m_ElementName = elementNameAttr.Value,
                    m_AttributeName = attribute.Name.LocalName,
                    m_NamesPath = elementNameAttr.Value.Split(),
                    m_Value = attribute.Value
                };

                templateAsset.attributeOverrides.Add(attributeOverride);
            }

            ListPool<VisualElementAsset>.Release(resolvedVisualElementAssetsInTemplate);
        }

        static (string elementNamespaceName, string fullName, UxmlNamespaceDefinition prefix) ResolveFullType(XElement elt)
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

            var prefix = elt.GetPrefixOfNamespace(elt.Name.Namespace);

            if (string.IsNullOrEmpty(prefix))
                return (elementNamespaceName, fullName, new UxmlNamespaceDefinition{ resolvedNamespace = elementNamespaceName });

            return (elementNamespaceName, fullName, new UxmlNamespaceDefinition{ prefix = prefix, resolvedNamespace = elt.GetNamespaceOfPrefix(prefix)?.NamespaceName});
        }

        bool TryResolveType(XElement elt, UxmlAsset parent, VisualTreeAsset visualTreeAsset, out UxmlAsset uxmlAsset, out UxmlSerializedDataDescription uxmlSerializedDataDescription)
        {
            var (elementNamespaceName, fullName, xmlns) = ResolveFullType(elt);
            uxmlAsset = null;
            uxmlSerializedDataDescription = null;

            // Is this a null element?
            if (fullName == UxmlAsset.NullNodeType)
            {
                uxmlAsset = new UxmlObjectAsset(UxmlAsset.NullNodeType, false, xmlns);
                return true;
            }

            uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(fullName);
            if (uxmlSerializedDataDescription?.isEditorOnly == true)
                visualTreeAsset.hasEditorElements = true;

            // Is the element a UxmlObject?
            if (uxmlSerializedDataDescription?.isUxmlObject == true)
            {
                uxmlAsset = new UxmlObjectAsset(fullName, false, xmlns);
                return true;
            }

            // Does the element contain values for a field marked with the UxmlObjectAttribute?
            if (parent != null &&
                UxmlSerializedDataRegistry.GetDescription(parent.fullTypeName) is UxmlSerializedDataDescription descParent &&
                descParent.IsUxmlObjectField(fullName))
            {
                uxmlAsset = new UxmlObjectAsset(fullName, true, xmlns);
                return true;
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
                    return false;
                }

                string templateName = sourceAttr.Value;
                if (!visualTreeAsset.TemplateExists(templateName))
                {
                    LogError(visualTreeAsset,
                        ImportErrorType.Semantic,
                        ImportErrorCode.UnknownTemplate,
                        templateName,
                        elt);
                    return false;
                }

                uxmlAsset = new TemplateAsset(templateName, xmlns) { serializedData = CreateSerializedData(uxmlSerializedDataDescription) };
                return true;
            }

            var vea = new VisualElementAsset(fullName, xmlns);
            uxmlAsset = vea;

            if (uxmlSerializedDataDescription != null)
            {
                vea.serializedData = CreateSerializedData(uxmlSerializedDataDescription);
            }

            return true;
        }

        bool EnsureValidUxmlObjectChild(XElement elt, UxmlAsset uxmlAsset, VisualTreeAsset vta, UxmlAsset parent)
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
            if (parent is UxmlObjectAsset)
            {
                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.InvalidUxmlObjectChild, uxmlAsset.fullTypeName, elt);
                return false;
            }

            if (uxmlAsset is VisualElementAsset { isRoot: true } && parent != null)
            {
                LogError(vta, ImportErrorType.Semantic, ImportErrorCode.MultipleRootElements, uxmlAsset.fullTypeName, elt);
                return false;
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
                LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.ReferenceInvalidURIProjectAssetPath, response.warningMessage, elt);
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
                    // We dont want to declare dependencies on built-in resources
                    if (!IsBuiltinResource(projectRelativePath))
                        m_Context.DependsOnArtifact(projectRelativePath);
                }
                else if (!(asset is Object)) // This check accounts for a missing reference. We don't want to overwrite it.
                {
                    asset = DeclareDependencyAndLoad(projectRelativePath);
                }

                return (response, asset);
            }

            return (response, null);
        }

        static bool IsBuiltinResource(string path)
        {
            return string.Equals(path, "resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "library/unity default resources", StringComparison.OrdinalIgnoreCase);
        }

        static Object ExtractSubAssetFromParent(Object parent, Type assetType, URIHelpers.URIValidationResponse response)
        {
            if (parent)
            {
                // If the type is Object we need to find the asset by name as we can not rely on the type to filter.
                if (typeof(Object) == assetType)
                {
                    if (parent.name == response.resolvedSubAssetPath)
                        return parent;
                }
                else if (assetType.IsAssignableFrom(parent.GetType()))
                {
                    return parent;
                }

                // Force loading using correct attribute type to support cases like Texture2D vs Sprite,
                if (string.IsNullOrEmpty(response.resolvedSubAssetPath))
                {
                    // Force loading using correct attribute type to support cases like Texture2D vs Sprite,
                    return AssetDatabase.LoadAssetAtPath(response.resolvedProjectRelativePath, assetType);
                }

                // Force load the sub assets and find the asset by name and type
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(response.resolvedProjectRelativePath);
                foreach (var subAsset in subAssets)
                {
                    if (subAsset.name == response.resolvedSubAssetPath && assetType.IsAssignableFrom(subAsset.GetType()))
                    {
                        return subAsset;
                    }
                }
            }
            return parent;
        }

        void ParseAttributes(XElement elt, UxmlAsset uxmlAsset, VisualTreeAsset vta, UxmlSerializedDataDescription uxmlSerializedDataDescription, UxmlSerializedData uxmlSerializedData)
        {
            var cc = new CreationContext(vta);
            foreach (var xattr in elt.Attributes())
            {
                var attrName = xattr.Name.LocalName;
                var attrValue = xattr.Value;

                if (TryParseSpecialAttribute(elt, xattr, vta, uxmlAsset, uxmlSerializedData))
                    continue;

                if (uxmlSerializedData != null && uxmlSerializedDataDescription != null)
                {
                    if (uxmlSerializedDataDescription.FindAttributeWithUxmlName(attrName) is { } attributeDescription)
                    {
                        if (attributeDescription.isUnityObject)
                        {
                            var assetType = attributeDescription.type;
                            if (assetType != typeof(VisualTreeAsset) || !vta.TemplateExists(xattr.Value))
                            {
                                // We need to first load the asset and add it to the vta so that the attribute converter can find it.
                                // When importing a UXML which contains relative file paths the converter will not be able to resolve the vta
                                // asset path as it does not yet exist, so it falls back to the path we provide in the register method.
                                // In the future we should consider passing the asset path to the converter through the CreationContext.
                                var (response, asset) = ValidateAndLoadResource(elt, vta, attrValue, true);
                                if (response.result == URIValidationResult.OK && !vta.AssetEntryExists(xattr.Value, assetType))
                                {
                                    asset = ExtractSubAssetFromParent(asset, assetType, response);

                                    // Update the url value so it is correct when saved back to UXML
                                    if (response.resolvedUrlChanged && enableAutomaticUrlUpgrades)
                                    {
                                        attrValue = URIHelpers.MakeAssetUri(asset);
                                        vta.importerWithUpdatedUrls = true;
                                    }

                                    vta.RegisterAssetEntry(attrValue, assetType, asset);
                                }
                            }
                        }

                        var result = UxmlSerializer.TryParseSerializedAttribute(attrValue, uxmlSerializedData, attributeDescription, cc);
                        if (!result.success && result.hasError)
                        {
                            LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.AttributeParsing, result.GenerateFullErrorMessage(attributeDescription.name), elt);
                        }
                    };

                    // Since a deprecated attribute name may be shared by multiple attributes, we apply it to every matching occurrence.
                    if (enableAutomaticAttributeRenames)
                    {
                        foreach (var obsoleteAttribute in uxmlSerializedDataDescription.FindAttributesWithObsoleteUxmlName(attrName))
                        {
                            var result = UxmlSerializer.TryParseSerializedAttribute(attrValue, uxmlSerializedData, obsoleteAttribute, cc);
                            if (result.success)
                            {
                                // Upgrade the attribute name
                                attrName = obsoleteAttribute.name;
                                vta.importedWithObsoleteAttributeNames = true;
                            }
                            else if (result.hasError)
                            {
                                LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.AttributeParsing, result.GenerateFullErrorMessage(attrName), elt);
                            }
                        }
                    }
                }
                uxmlAsset.SetAttribute(attrName, attrValue);
            }

            if (uxmlSerializedDataDescription != null && uxmlSerializedData is IUxmlSerializedDataCustomAttributeHandler customHandler)
            {
                using (HashSetPool<string>.Get(out var handledAttributes))
                {
                    customHandler.SerializeCustomAttributes(uxmlAsset, handledAttributes);

                    // Apply the handled values to the serialized data
                    foreach (var attrName in handledAttributes)
                    {
                        var attributeDesc = uxmlSerializedDataDescription.FindAttributeWithUxmlName(attrName);
                        if (attributeDesc != null)
                        {
                            var result = UxmlSerializer.TryParseSerializedAttribute(uxmlAsset.GetAttributeValue(attrName), uxmlSerializedData, attributeDesc, cc);
                            if (!result.success && result.hasError)
                            {
                                LogWarning(vta, ImportErrorType.Semantic, ImportErrorCode.AttributeParsing, result.GenerateFullErrorMessage(attributeDesc.name), elt);
                            }
                        }
                    }
                }
            }
        }

        bool TryParseSpecialAttribute(XElement elt, XAttribute xattr, VisualTreeAsset vta, UxmlAsset asset, UxmlSerializedData uxmlSerializedData)
        {
            var attrName = xattr.Name.LocalName;

            // To be able to re-export the xmlns back to .uxml, we need to keep the "xmlns:" part.
            // If the xmlns is global (i.e. "xmlns=UnityEngine.UIElements"), then we save it as a
            // normal attribute.
            if (xattr.IsNamespaceDeclaration)
            {
                // Defining a global namespace
                if (attrName == "xmlns")
                {
                    asset.AddUxmlNamespace("", xattr.Value);
                }
                else
                {
                    asset.AddUxmlNamespace(attrName, xattr.Value);
                }
                return true;
            }

            if (attrName == UxmlAsset.AuthoringIdAttribute)
            {
                // If the content container ID was already applied before the authoring ID then we need to update it.
                bool updateContentContainerId = vta.contentContainerId == asset.id;

                // Empty values are silently ignored.
                if (string.IsNullOrEmpty(xattr.Value))
                    return true;

                var success = UxmlUtility.TryParse(xattr.Value, out var parsedId, out var error);
                if (success && parsedId == 0)
                {
                    error = "Authoring Id cannot be zero.";
                    success = false;
                }

                if (!success)
                {
                    error = $"{attrName}=\"{xattr.Value}\": {error}";
                    LogError(vta, ImportErrorType.Syntax, ImportErrorCode.AttributeParsing, error, xattr);
                    return true;
                }

                // Log conflict if there is any.
                using (var conflict = new LogAuthoringIdConflictScope(this, vta, elt))
                {
                    asset.hasAuthoringId = true;
                    asset.id = parsedId;
                    if (conflict.conflictDetected)
                        return true;
                }

                if (uxmlSerializedData != null)
                    uxmlSerializedData.uxmlAssetId = asset.id;
                asset.SetAttribute(attrName, xattr.Value);

                if (updateContentContainerId)
                    vta.contentContainerId = asset.id;

                return true;
            }

            if (asset is not VisualElementAsset vea)
                return false;

            var name = xattr.Name.LocalName;
            var value = xattr.Value;

            switch (name)
            {
                case k_ClassAttr:
                    vea.classes = value.Split(' ');
                    return true;

                case "content-container":
                case "contentContainer":
                    vea.SetAttribute(name, value);
                    if (vta.contentContainerId != 0)
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.DuplicateContentContainer, null, elt);
                    else
                        vta.contentContainerId = vea.id;
                    return true;

                case k_SlotDefinitionAttr:
                    LogWarning(vta, ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, elt);
                    if (string.IsNullOrEmpty(value))
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotDefinitionHasEmptyName, null, elt);
                    else if (!vta.AddSlotDefinition(value, vea.id))
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.DuplicateSlotDefinition, value, elt);
                    return true;

                case k_SlotUsageAttr:
                    LogWarning(vta, ImportErrorType.Syntax, ImportErrorCode.SlotsAreExperimental, null, elt);
                    var templateAsset = vea.parentAsset as TemplateAsset;
                    if (templateAsset == null)
                    {
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotUsageInNonTemplate, vea.parentAsset, elt);
                        return true;
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        LogError(vta, ImportErrorType.Semantic, ImportErrorCode.SlotUsageHasEmptyName, null, elt);
                        return true;
                    }

                    templateAsset.AddSlotUsage(value, vea.id);
                    return true;

                case k_StyleAttr:
                    var parser = new UnityStylesheetParser();
                    var parsed = parser.Parse("* { " + value + " }");
                    if (parser.errors.Count != 0)
                    {
                        LogWarning(
                            vta,
                            ImportErrorType.Semantic,
                            ImportErrorCode.InvalidCssInStyleAttribute,
                            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            parser.errors.Aggregate("", (s, error) => s + error.ToString() + "\n"),
#pragma warning restore UA2001
                            xattr);
                        return true;
                    }

                    #pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (parsed.StyleRules.Count() != 1)
#pragma warning restore UA2005
                    {
                        LogWarning(
                            vta,
                            ImportErrorType.Semantic,
                            ImportErrorCode.InvalidCssInStyleAttribute,
                            #pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            "Expected one style rule, found " + parsed.StyleRules.Count(),
#pragma warning restore UA2005
                            xattr);
                        return true;
                    }

                    // Each vea will creates 0 or 1 style rule, with one or more properties
                    // they don't have selectors and are directly referenced by index
                    // it's then applied during tree cloning
                    m_Builder.BeginRule(-1);
                    m_CurrentLine = ((IXmlLineInfo)xattr).LineNumber;
                    #pragma warning disable UA2010 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var prop in parsed.StyleRules.First().Style.Declarations)
#pragma warning restore UA2010
                    {
                        m_Builder.BeginProperty(prop.Name);
                        VisitValue(prop);
                        m_Builder.EndProperty();
                    }

                    vea.ruleIndex = m_Builder.EndRule();
                    return true;
            }
            return false;
        }
    }

    internal enum ImportErrorCode
    {
        InvalidRootElement,
        MultipleRootElements,
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
        DuplicateAuthoringId,
        AttributeParsing,
    }

    internal enum ImportErrorType
    {
        Syntax,
        Semantic
    }
}
