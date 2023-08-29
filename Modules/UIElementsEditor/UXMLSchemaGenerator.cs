// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Pool;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Attribute that can be used on an assembly to define an XML namespace prefix for a namespace.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class UxmlNamespacePrefixAttribute : Attribute
    {
        /// <summary>
        /// The namespace name.
        /// </summary>
        public string ns { get; }
        /// <summary>
        /// The namespace prefix.
        /// </summary>
        public string prefix { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ns">The XML/C# namespace to which a prefix will be associated.</param>
        /// <param name="prefix">The prefix to associate to the namespace.</param>
        public UxmlNamespacePrefixAttribute(string ns, string prefix)
        {
            this.ns = ns;
            this.prefix = prefix;
        }
    }

    internal class UxmlSchemaGenerator
    {
        // Folder, relative to the project root.
        internal const string k_SchemaFolder = "UIElementsSchema";

        internal static readonly List<string> k_SkippedAttributeNames = new()
        {
            "content-container",
            "class",
            "style",
        };

        [MenuItem("Assets/Update UXML Schema", false, 800)]
        static void UpdateUXMLSchema()
        {
            if (CommandService.Exists(nameof(UpdateUXMLSchema)))
                CommandService.Execute(nameof(UpdateUXMLSchema), CommandHint.Menu);
            else
            {
                UpdateSchemaFiles();
            }
        }

        public static void UpdateSchemaFiles(bool saveSelectedObject = false)
        {
            var selectedObject = Selection.activeObject;

            Directory.CreateDirectory(k_SchemaFolder);
            using (var it = GenerateSchemaFiles(k_SchemaFolder + "/").GetEnumerator())
            {
                while (it.MoveNext())
                {
                    var fileName = it.Current;
                    if (!it.MoveNext())
                        continue;

                    var action = ScriptableObject.CreateInstance<DoCreateAssetWithContent>();
                    action.filecontent = it.Current;

                    ProjectWindowUtil.EndNameEditAction(action, 0, fileName, null, true);
                    Selection.activeObject = EditorUtility.InstanceIDToObject(0);
                }
            }

            AssetDatabase.Refresh();

            // Ensure that the selected object has not changed. Needed for UIElementsTemplate.CreateUXMLTemplate()
            if (saveSelectedObject)
                Selection.activeObject = selectedObject;
        }

        internal static Dictionary<string, string> GetNamespacePrefixDictionary()
        {
            return SchemaInfo.s_NamespacePrefix;
        }

        sealed class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        class SchemaInfo
        {
            public SchemaInfo(string uxmlNamespace)
            {
                schema = new XmlSchema();
                schema.ElementFormDefault = XmlSchemaForm.Qualified;
                if (uxmlNamespace != string.Empty)
                {
                    schema.TargetNamespace = uxmlNamespace;
                }

                namepacePrefix = GetPrefixForNamespace(uxmlNamespace);

                importNamespaces = new HashSet<string>();
            }

            public XmlSchema schema { get; set; }
            public string namepacePrefix { get; set; }
            public HashSet<string> importNamespaces { get; set; }

            internal static Dictionary<string, string> s_NamespacePrefix;

            static string GetPrefixForNamespace(string ns)
            {
                if (s_NamespacePrefix == null)
                {
                    s_NamespacePrefix = new Dictionary<string, string>();

                    s_NamespacePrefix.Add(string.Empty, "global");
                    s_NamespacePrefix.Add(typeof(VisualElement).Namespace, "engine");
                    s_NamespacePrefix.Add(typeof(UxmlSchemaGenerator).Namespace, "editor");

                    var currentDomain = AppDomain.CurrentDomain;
                    var userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
                    foreach (var assembly in currentDomain.GetAssemblies())
                    {
                        if (!userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                            continue;

                        try
                        {
                            foreach (var nsPrefixAttributeObject in assembly.GetCustomAttributes(typeof(UxmlNamespacePrefixAttribute), false))
                            {
                                var nsPrefixAttribute = (UxmlNamespacePrefixAttribute)nsPrefixAttributeObject;
                                s_NamespacePrefix[nsPrefixAttribute.ns] = nsPrefixAttribute.prefix;
                            }
                        }
                        catch (TypeLoadException e)
                        {
                            Debug.LogWarningFormat("Error while loading types from assembly {0}: {1}", assembly.FullName, e);
                        }
                    }
                }

                if (s_NamespacePrefix.TryGetValue(ns ?? string.Empty, out var prefix))
                {
                    return prefix;
                }

                s_NamespacePrefix[ns] = string.Empty;
                return string.Empty;
            }
        }

        class SerializedDataSchemaInfo
        {
            public SerializedDataSchemaInfo(string uxmlName, Type type)
            {
                fullName = uxmlName;
                this.type = type;
            }

            public string fullName { get; }
            public Type type { get; }

            public string uxmlName => type.Name;

            public string uxmlNamespace => type.Namespace ?? string.Empty;

            public string uxmlQualifiedName => type.FullName;

            public string baseTypeName => type == typeof(VisualElement) ? string.Empty : typeof(VisualElement).Name;

            public string baseTypeNamespace => type == typeof(VisualElement) ? string.Empty : typeof(VisualElement).Namespace ?? string.Empty;

            public string baseTypeQualifiedName => type == typeof(VisualElement) ? string.Empty : typeof(VisualElement).FullName;

            public IEnumerable<UxmlSerializedAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    var description = UxmlSerializedDataRegistry.GetDescription(fullName);
                    Debug.AssertFormat(description != null, "Expected to find a description for {0}", fullName);
                    return description?.serializedAttributes;
                }
            }

            public IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    if (type != typeof(VisualElement))
                        yield break;
                    yield return new UxmlChildElementDescription(typeof(VisualElement));
                }
            }
        }

        const string k_XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        const string k_TypeSuffix = "Type";

        class FactoryProcessingHelper
        {
            public class AttributeRecord
            {
                public XmlQualifiedName name { get; set; }
                public UxmlAttributeDescription desc { get; set; }
            }

            public Dictionary<string, AttributeRecord> attributeTypeNames;

            HashSet<XmlQualifiedName> m_KnownTypes;

            public FactoryProcessingHelper()
            {
                attributeTypeNames = new Dictionary<string, AttributeRecord>();
                m_KnownTypes = new HashSet<XmlQualifiedName>();
            }

            public void RegisterElementType(string elementName, string elementNameSpace)
            {
                m_KnownTypes.Add(new XmlQualifiedName(elementName, elementNameSpace));
            }

            public bool IsKnownElementType(string elementName, string elementNameSpace)
            {
                return m_KnownTypes.Contains(new XmlQualifiedName(elementName, elementNameSpace));
            }
        }

        const string k_SchemaFileExtension = ".xsd";
        const string k_MainSchemaFileName = "UIElements" + k_SchemaFileExtension;
        const string k_GlobalNamespaceSchemaFileName = "GlobalNamespace" + k_SchemaFileExtension;

        internal static IEnumerable<string> GenerateSchemaFiles(string baseDir = null)
        {
            var schemas = new Dictionary<string, SchemaInfo>();
            var processingData = new FactoryProcessingHelper();

            baseDir ??= Application.temporaryCachePath + "/";

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Generating UXML Schema Files", "Please wait...", 0.0f);
            }

            try
            {
                // Convert the factories and serialized data into schemas info.
                ProcessUxmlSerializedData(schemas, processingData);
                ProcessUxmlTraitFactories(schemas, processingData);

                // Compile schemas.
                var schemaSet = new XmlSchemaSet();
                var masterSchema = new XmlSchema();
                masterSchema.ElementFormDefault = XmlSchemaForm.Qualified;

                var nsmgr = new XmlNamespaceManager(new NameTable());
                nsmgr.AddNamespace("xs", k_XmlSchemaNamespace);

                File.Delete(baseDir + k_MainSchemaFileName);

                foreach (var schema in schemas)
                {
                    if (schema.Value.schema.TargetNamespace != null)
                    {
                        nsmgr.AddNamespace(schema.Value.namepacePrefix, schema.Value.schema.TargetNamespace);

                        // Import schema into the master schema.
                        var import = new XmlSchemaImport();
                        import.Namespace = schema.Value.schema.TargetNamespace;
                        var schemaLocation = GetFileNameForNamespace(schema.Value.schema.TargetNamespace);
                        File.Delete(baseDir + schemaLocation);
                        import.SchemaLocation = schemaLocation;
                        masterSchema.Includes.Add(import);
                    }
                    else
                    {
                        var include = new XmlSchemaInclude();
                        var schemaLocation = GetFileNameForNamespace(null);
                        File.Delete(baseDir + schemaLocation);
                        include.SchemaLocation = schemaLocation;
                        masterSchema.Includes.Add(include);
                    }

                    // Import referenced schemas into this XSD
                    foreach (var ns in schema.Value.importNamespaces)
                    {
                        if (ns != schema.Value.schema.TargetNamespace && ns != k_XmlSchemaNamespace)
                        {
                            var import = new XmlSchemaImport();
                            import.Namespace = ns;
                            import.SchemaLocation = GetFileNameForNamespace(ns);
                            schema.Value.schema.Includes.Add(import);
                        }
                    }

                    schemaSet.Add(schema.Value.schema);
                }

                schemaSet.Add(masterSchema);

                try
                {
                    schemaSet.Compile();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    yield break;
                }

                // Now generate the schema textual data.
                foreach (XmlSchema compiledSchema in schemaSet.Schemas())
                {
                    var schemaName = compiledSchema.TargetNamespace;

                    // Three possible cases:
                    // TargetNamespace == null and Items.Count == 0: the main schema, that include/import all other schema files
                    // TargetNamespace == null and Items.Count != 0: the schema file for the global namespace
                    // TargetNamespace != null: the schema file for TargetNamespace
                    if (schemaName == null && compiledSchema.Items.Count == 0)
                    {
                        schemaName = k_MainSchemaFileName;
                    }
                    else
                    {
                        schemaName = GetFileNameForNamespace(compiledSchema.TargetNamespace);
                    }

                    yield return baseDir + schemaName;

                    StringWriter strWriter = new UTF8StringWriter();
                    compiledSchema.Write(strWriter, nsmgr);
                    yield return strWriter.ToString();
                }
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        static void ProcessUxmlSerializedData(Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData)
        {
            var deferredFactories = new List<SerializedDataSchemaInfo>();

            // Convert the UxmlSerializedData into schemas info.
            UxmlSerializedDataRegistry.Register();
            foreach (var serializedDataType in UxmlSerializedDataRegistry.SerializedDataTypes)
            {
                var schemaInfo = new SerializedDataSchemaInfo(serializedDataType.Key, serializedDataType.Value.DeclaringType);
                if (!ProcessSerializedData(schemaInfo, schemas, processingData))
                {
                    // Could not process the serialized data now, because it depends on a yet unprocessed serialized data.
                    // Defer its processing.
                    deferredFactories.Add(schemaInfo);
                }
            }

            ProcessDeferredSchemaInfo(schemas, processingData, deferredFactories);
        }

        static void ProcessUxmlTraitFactories(Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData)
        {
            var deferredFactories = new List<IBaseUxmlFactory>();

            foreach (var factories in VisualElementFactoryRegistry.factories)
            {
                if (factories.Value.Count == 0)
                    continue;

                // Only process the first factory, as the other factories define the same element.
                var factory = factories.Value[0];

                if (!ProcessFactory(factory, schemas, processingData))
                {
                    // Could not process the factory now, because it depends on a yet unprocessed factory.
                    // Defer its processing.
                    deferredFactories.Add(factory);
                }
            }

            // Convert the factories into schemas info.
            foreach (var factories in UxmlObjectFactoryRegistry.factories)
            {
                if (factories.Value.Count == 0)
                    continue;

                // Only process the first factory, as the other factories define the same element.
                var factory = factories.Value[0];

                if (!ProcessFactory(factory, schemas, processingData))
                {
                    // Could not process the factory now, because it depends on a yet unprocessed factory.
                    // Defer its processing.
                    deferredFactories.Add(factory);
                }
            }

            ProcessDeferredSchemaInfo(schemas, processingData, deferredFactories);
        }

        static void ProcessDeferredSchemaInfo<T>(Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData, List<T> deferredSchemaInfo)
        {
            using (var pooled = ListPool<T>.Get(out var deferredSchemaInfoCopy))
            {
                do
                {
                    deferredSchemaInfoCopy.Clear();
                    deferredSchemaInfoCopy.AddRange(deferredSchemaInfo);

                    foreach (var schemaInfo in deferredSchemaInfoCopy)
                    {
                        deferredSchemaInfo.Remove(schemaInfo);
                        var schemaInfoProcessed = false;

                        if (typeof(T) == typeof(IBaseUxmlFactory))
                        {
                            schemaInfoProcessed = ProcessFactory(schemaInfo as IBaseUxmlFactory, schemas, processingData);
                        }
                        if (typeof(T) == typeof(SerializedDataSchemaInfo))
                        {
                            schemaInfoProcessed = ProcessSerializedData(schemaInfo as SerializedDataSchemaInfo, schemas, processingData);
                        }

                        // Could not process the factory now because it depends on a yet unprocessed factory.
                        // Defer its processing again.
                        if (!schemaInfoProcessed)
                            deferredSchemaInfo.Add(schemaInfo);
                    }
                } while (deferredSchemaInfoCopy.Count > deferredSchemaInfo.Count);

                if (deferredSchemaInfo.Count > 0)
                {
                    // log unprocessed schema types
                    var log = new StringBuilder();
                    foreach (var schemaInfo in deferredSchemaInfo)
                    {
                        if (typeof(T) == typeof(IBaseUxmlFactory))
                        {
                            var f = schemaInfo as IBaseUxmlFactory;
                            log.Append($"{f?.uxmlName}, ");
                        }
                        if (typeof(T) == typeof(SerializedDataSchemaInfo))
                        {
                            var f = schemaInfo as SerializedDataSchemaInfo;
                            log.Append($"{f?.uxmlName}, ");
                        }
                    }

                    Debug.Log("Some element types could not be processed because their base type is missing: " + log.ToString().Substring(0, log.Length - 2));
                }
            }
        }

        internal static string GetFileNameForNamespace(string ns)
        {
            return string.IsNullOrEmpty(ns) ? k_GlobalNamespaceSchemaFileName : ns + k_SchemaFileExtension;
        }

        static bool ProcessFactory(IBaseUxmlFactory factory, Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData)
        {
            if (!string.IsNullOrEmpty(factory.substituteForTypeName))
            {
                if (!processingData.IsKnownElementType(factory.substituteForTypeName, factory.substituteForTypeNamespace))
                {
                    // substituteForTypeName is not yet known. Defer processing to later.
                    return false;
                }
            }

            var uxmlNamespace = factory.uxmlNamespace;
            if (!schemas.TryGetValue(uxmlNamespace, out var schemaInfo))
            {
                schemaInfo = new SchemaInfo(uxmlNamespace);
                schemas[uxmlNamespace] = schemaInfo;
            }

            var type = AddElementTypeToXmlSchema(factory, schemaInfo, processingData);
            AddElementToXmlSchema(factory, schemaInfo, type);

            processingData.RegisterElementType(factory.uxmlName, factory.uxmlNamespace);

            return true;
        }

        static XmlSchemaParticle MakeChoiceSequence(IEnumerable<UxmlChildElementDescription> elements)
        {
            if (elements.GetCount() == 0)
            {
                return null;
            }

            var sequence = new XmlSchemaSequence();
            sequence.MinOccurs = 0;
            sequence.MaxOccursString = "unbounded";

            if (elements.GetCount() == 1)
            {
                var enumerator = elements.GetEnumerator();
                enumerator.MoveNext();
                if (enumerator.Current != null)
                {
                    var elementRef = new XmlSchemaElement();
                    elementRef.RefName = new XmlQualifiedName(enumerator.Current.elementName, enumerator.Current.elementNamespace);
                    sequence.Items.Add(elementRef);
                }
            }
            else
            {
                var choice = new XmlSchemaChoice();

                foreach (var element in elements)
                {
                    if (element != null)
                    {
                        var elementRef = new XmlSchemaElement();
                        elementRef.RefName = new XmlQualifiedName(element.elementName, element.elementNamespace);
                        choice.Items.Add(elementRef);
                    }
                }
                if (choice.Items.Count > 0)
                    sequence.Items.Add(choice);
            }

            return sequence;
        }

        static XmlSchemaType AddElementTypeToXmlSchema(IBaseUxmlFactory factory, SchemaInfo schemaInfo, FactoryProcessingHelper processingData)
        {
            // We always have complex types with complex content.
            var elementType = new XmlSchemaComplexType();
            elementType.Name = factory.uxmlName + k_TypeSuffix;

            // protect against adding duplicates
            if (!schemaInfo.schema.Items.NoElementOfTypeMatchesPredicate<XmlSchemaType>(x => x.Name == elementType.Name))
            {
                return elementType;
            }

            var content = new XmlSchemaComplexContent();
            elementType.ContentModel = content;

            // We only support restrictions of base types.
            var restriction = new XmlSchemaComplexContentRestriction();
            content.Content = restriction;

            if (factory.substituteForTypeName == string.Empty)
            {
                restriction.BaseTypeName = new XmlQualifiedName("anyType", k_XmlSchemaNamespace);
            }
            else
            {
                restriction.BaseTypeName = new XmlQualifiedName(factory.substituteForTypeName + k_TypeSuffix, factory.substituteForTypeNamespace);
                schemaInfo.importNamespaces.Add(factory.substituteForTypeNamespace);
            }

            if (factory.canHaveAnyAttribute)
            {
                var anyAttribute = new XmlSchemaAnyAttribute();
                anyAttribute.ProcessContents = XmlSchemaContentProcessing.Lax;
                restriction.AnyAttribute = anyAttribute;
            }

            // For user created types, they may return null for uxmlAttributeDescription, so we need to check in order not to crash.
            if (factory.uxmlAttributesDescription != null)
            {
                foreach (var attrDesc in factory.uxmlAttributesDescription)
                {
                    // For user created types, they may `yield return null` which would create an array with a null, so we need
                    // to check in order not to crash.
                    if (attrDesc != null)
                    {
                        var typeName = AddAttributeTypeToXmlSchema(schemaInfo, attrDesc, factory, processingData);
                        if (typeName != null)
                        {
                            AddAttributeToXmlSchema(restriction, attrDesc, typeName);
                            schemaInfo.importNamespaces.Add(attrDesc.typeNamespace);
                        }
                    }
                }
            }

            // For user created types, they may return null for uxmlChildElementsDescription, so we need to check in order not to crash.
            if (factory.uxmlChildElementsDescription != null)
            {
                var hasChildElements = false;
                foreach (var childDesc in factory.uxmlChildElementsDescription)
                {
                    // For user created types, they may `yield return null` which would create an array with a null, so we need
                    // to check in order not to crash.
                    if (childDesc != null)
                    {
                        hasChildElements = true;
                        schemaInfo.importNamespaces.Add(childDesc.elementNamespace);
                    }
                }

                if (hasChildElements)
                {
                    restriction.Particle = MakeChoiceSequence(factory.uxmlChildElementsDescription);
                }
            }

            schemaInfo.schema.Items.Add(elementType);
            return elementType;
        }

        static void AddElementToXmlSchema(IBaseUxmlFactory factory, SchemaInfo schemaInfo, XmlSchemaType type)
        {
            var element = new XmlSchemaElement();
            element.Name = factory.uxmlName;

            if (type != null)
            {
                element.SchemaTypeName = new XmlQualifiedName(type.Name, factory.uxmlNamespace);
            }

            if (factory.substituteForTypeName != string.Empty)
            {
                element.SubstitutionGroup = new XmlQualifiedName(factory.substituteForTypeName, factory.substituteForTypeNamespace);
            }

            // protect against adding duplicates
            if (schemaInfo.schema.Items.NoElementOfTypeMatchesPredicate<XmlSchemaElement>(x => x.Name == element.Name))
            {
                schemaInfo.schema.Items.Add(element);
            }
        }

        static XmlQualifiedName AddAttributeTypeToXmlSchema(SchemaInfo schemaInfo, UxmlAttributeDescription description, IBaseUxmlFactory factory, FactoryProcessingHelper processingData)
        {
            if (description.name == null)
            {
                return null;
            }

            var attrTypeName = $"{factory.uxmlQualifiedName}_{description.name}_{k_TypeSuffix}";
            var attrTypeNameInBaseElement = $"{factory.substituteForTypeQualifiedName}_{description.name}_{k_TypeSuffix}";

            if (processingData.attributeTypeNames.TryGetValue(attrTypeNameInBaseElement, out var attrRecord))
            {
                // If restriction != baseElement.restriction, we need to declare a new type.
                // Note: we do not support attributes having a less restrictive restriction than its base type.
                if ((description.restriction == null && attrRecord.desc.restriction == null) ||
                    (description.restriction != null && description.restriction.Equals(attrRecord.desc.restriction)))
                {
                    // Register attrTypeName -> attrRecord for potential future derived elements.
                    processingData.attributeTypeNames.TryAdd(attrTypeName, attrRecord);
                    return attrRecord.name;
                }
            }

            XmlQualifiedName xqn;
            FactoryProcessingHelper.AttributeRecord attributeRecord;

            if (description.restriction == null)
            {
                // Type is a built-in type.
                xqn = new XmlQualifiedName(description.type, description.typeNamespace);
                attributeRecord = new FactoryProcessingHelper.AttributeRecord { name = xqn, desc = description };
                processingData.attributeTypeNames.TryAdd(attrTypeName, attributeRecord);
                return xqn;
            }

            var attrTypeNameForSchema = $"{factory.uxmlName}_{description.name}_{k_TypeSuffix}";
            xqn = new XmlQualifiedName(attrTypeNameForSchema, schemaInfo.schema.TargetNamespace);

            var simpleType = new XmlSchemaSimpleType();
            simpleType.Name = attrTypeNameForSchema;

            if (description.restriction is UxmlEnumeration enumRestriction)
            {
                var restriction = new XmlSchemaSimpleTypeRestriction();
                simpleType.Content = restriction;
                restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                foreach (var v in enumRestriction.values)
                {
                    var enumValue = new XmlSchemaEnumerationFacet();
                    enumValue.Value = v;
                    restriction.Facets.Add(enumValue);
                }
            }
            else
            {
                if (description.restriction is UxmlValueMatches regexRestriction)
                {
                    if (regexRestriction.regex == null)
                    {
                        Debug.LogWarning($"{nameof(UxmlValueMatches)} restriction '{description.name}' has null regex value.");
                        return null;
                    }

                    var restriction = new XmlSchemaSimpleTypeRestriction();
                    simpleType.Content = restriction;
                    restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                    var pattern = new XmlSchemaPatternFacet();
                    pattern.Value = regexRestriction.regex;
                    restriction.Facets.Add(pattern);
                }
                else
                {
                    if (description.restriction is UxmlValueBounds bounds)
                    {
                        var restriction = new XmlSchemaSimpleTypeRestriction();
                        simpleType.Content = restriction;
                        restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                        if (!string.IsNullOrEmpty(bounds.min))
                        {
                            XmlSchemaFacet facet = bounds.excludeMin ? new XmlSchemaMinExclusiveFacet() :  new XmlSchemaMinInclusiveFacet();
                            facet.Value = bounds.min;
                            restriction.Facets.Add(facet);
                        }

                        if (!string.IsNullOrEmpty(bounds.max))
                        {
                            XmlSchemaFacet facet = bounds.excludeMax ? new XmlSchemaMaxExclusiveFacet() : new XmlSchemaMaxInclusiveFacet();
                            facet.Value = bounds.max;
                            restriction.Facets.Add(facet);
                        }
                    }
                    else
                    {
                        Debug.Log("Unsupported restriction type.");
                    }
                }
            }

            attributeRecord = new FactoryProcessingHelper.AttributeRecord { name = xqn, desc = description };
            processingData.attributeTypeNames.TryAdd(attrTypeName, attributeRecord);

            schemaInfo.schema.Items.Add(simpleType);
            return xqn;
        }

        static void AddAttributeToXmlSchema(XmlSchemaComplexContentRestriction restriction, UxmlAttributeDescription description, XmlQualifiedName typeName)
        {
            var attr = new XmlSchemaAttribute();
            attr.Name = description.name;
            attr.SchemaTypeName = typeName;

            switch (description.use)
            {
                case UxmlAttributeDescription.Use.Optional:
                    attr.Use = XmlSchemaUse.Optional;

                    // clean up default value
                    var defaultVal = description.defaultValueAsString?.Trim('\0');
                    if (defaultVal is "True" or "False")
                        defaultVal = defaultVal.ToLower();
                    if (description.type != "string" && defaultVal == "")
                        defaultVal = null;

                    attr.DefaultValue = defaultVal;
                    break;

                case UxmlAttributeDescription.Use.Prohibited:
                    attr.Use = XmlSchemaUse.Prohibited;
                    break;

                case UxmlAttributeDescription.Use.Required:
                    attr.Use = XmlSchemaUse.Required;
                    break;

                default:
                    attr.Use = XmlSchemaUse.None;
                    break;
            }

            // avoid duplicate attributes since overriding attributes is supported
            foreach (var existingAttr in restriction.Attributes)
            {
                if (((XmlSchemaAttribute) existingAttr).Name == attr.Name)
                    return;
            }

            restriction.Attributes.Add(attr);
        }

        static bool ProcessSerializedData(SerializedDataSchemaInfo serializedData, Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData)
        {
            if (!string.IsNullOrEmpty(serializedData.baseTypeName))
            {
                if (!processingData.IsKnownElementType(serializedData.baseTypeName, serializedData.baseTypeNamespace))
                {
                    // Base type is not yet known. Defer processing to later.
                    return false;
                }
            }

            if (serializedData.type.ContainsGenericParameters || serializedData.type.IsAbstract)
                return true;

            var uxmlNamespace = serializedData.uxmlNamespace;
            if (!schemas.TryGetValue(uxmlNamespace, out var schemaInfo))
            {
                schemaInfo = new SchemaInfo(uxmlNamespace);
                schemas[uxmlNamespace] = schemaInfo;
            }

            var type = AddElementTypeToXmlSchema(serializedData, schemaInfo, processingData);
            AddElementToXmlSchema(serializedData, schemaInfo, type);

            processingData.RegisterElementType(serializedData.uxmlName, serializedData.uxmlNamespace);

            return true;
        }

        static XmlSchemaType AddElementTypeToXmlSchema(SerializedDataSchemaInfo serializedData, SchemaInfo schemaInfo, FactoryProcessingHelper processingData)
        {
            // We always have complex types with complex content.
            var elementType = new XmlSchemaComplexType();
            elementType.Name = serializedData.uxmlName + k_TypeSuffix;

            // protect against adding duplicates
            if (!schemaInfo.schema.Items.NoElementOfTypeMatchesPredicate<XmlSchemaType>(x => x.Name == elementType.Name))
            {
                return elementType;
            }

            var content = new XmlSchemaComplexContent();
            elementType.ContentModel = content;

            // We only support restrictions of base types.
            var restriction = new XmlSchemaComplexContentRestriction();
            content.Content = restriction;

            if (serializedData.baseTypeName == string.Empty)
            {
                restriction.BaseTypeName = new XmlQualifiedName("anyType", k_XmlSchemaNamespace);
            }
            else
            {
                restriction.BaseTypeName = new XmlQualifiedName(serializedData.baseTypeName + k_TypeSuffix, serializedData.baseTypeNamespace);
                schemaInfo.importNamespaces.Add(serializedData.baseTypeNamespace);
            }

            var anyAttribute = new XmlSchemaAnyAttribute();
            anyAttribute.ProcessContents = XmlSchemaContentProcessing.Lax;
            restriction.AnyAttribute = anyAttribute;

            // For user created types, they may return null for uxmlAttributeDescription, so we need to check in order not to crash.
            if (serializedData.uxmlAttributesDescription != null)
            {
                foreach (var attrDesc in serializedData.uxmlAttributesDescription)
                {
                    // For user created types, they may `yield return null` which would create an array with a null, so we need
                    // to check in order not to crash.
                    if (attrDesc != null)
                    {
                        // update type and restriction to use in the schema
                        attrDesc.UpdateBaseType();
                        attrDesc.UpdateSchemaRestriction();

                        // Ignore Uxml Objects as they're not considered attributes
                        if (attrDesc.isUxmlObject)
                            continue;

                        var typeName =
                            AddAttributeTypeToXmlSchema(schemaInfo, attrDesc, serializedData, processingData);
                        if (typeName != null)
                        {
                            AddAttributeToXmlSchema(restriction, attrDesc, typeName);
                            schemaInfo.importNamespaces.Add(attrDesc.typeNamespace);
                        }
                    }
                }
            }

            // adding skipped attributes to the schema
            foreach (var attributeName in k_SkippedAttributeNames)
            {
                var attrDesc = new UxmlSerializedAttributeDescription
                {
                    name = attributeName,
                    type = typeof(string),
                    defaultValue = null
                };

                // update type to use in the schema
                attrDesc.UpdateBaseType();

                var typeName =
                    AddAttributeTypeToXmlSchema(schemaInfo, attrDesc, serializedData, processingData);
                if (typeName != null)
                {
                    AddAttributeToXmlSchema(restriction, attrDesc, typeName);
                    schemaInfo.importNamespaces.Add(attrDesc.typeNamespace);
                }
            }

            if (serializedData.uxmlChildElementsDescription != null)
            {
                var hasChildElements = false;
                foreach (var childDesc in serializedData.uxmlChildElementsDescription)
                {
                    if (childDesc != null)
                    {
                        hasChildElements = true;
                        schemaInfo.importNamespaces.Add(childDesc.elementNamespace);
                    }
                }

                if (hasChildElements)
                {
                    restriction.Particle = MakeChoiceSequence(serializedData.uxmlChildElementsDescription);
                }
            }

            schemaInfo.schema.Items.Add(elementType);
            return elementType;
        }

        static void AddElementToXmlSchema(SerializedDataSchemaInfo serializedData, SchemaInfo schemaInfo, XmlSchemaType type)
        {
            var element = new XmlSchemaElement();
            element.Name = serializedData.uxmlName;

            if (type != null)
            {
                element.SchemaTypeName = new XmlQualifiedName(type.Name, serializedData.uxmlNamespace);
            }

            if (serializedData.baseTypeName != string.Empty)
            {
                element.SubstitutionGroup = new XmlQualifiedName(serializedData.baseTypeName, serializedData.baseTypeNamespace);
            }

            // protect against adding duplicates
            if (schemaInfo.schema.Items.NoElementOfTypeMatchesPredicate<XmlSchemaElement>(x => x.Name == element.Name))
            {
                schemaInfo.schema.Items.Add(element);
            }
        }

        static XmlQualifiedName AddAttributeTypeToXmlSchema(SchemaInfo schemaInfo, UxmlAttributeDescription description, SerializedDataSchemaInfo serializedData, FactoryProcessingHelper processingData)
        {
            if (description.name == null)
            {
                return null;
            }

            var attrTypeName = $"{serializedData.uxmlQualifiedName}_{description.name}_{k_TypeSuffix}";
            var attrTypeNameInBaseElement = $"{serializedData.baseTypeQualifiedName}_{description.name}_{k_TypeSuffix}";

            if (processingData.attributeTypeNames.TryGetValue(attrTypeNameInBaseElement, out var attrRecord))
            {
                // If restriction != baseElement.restriction, we need to declare a new type.
                // Note: we do not support attributes having a less restrictive restriction than its base type.
                if ((description.restriction == null && attrRecord.desc.restriction == null) ||
                    (description.restriction != null && description.restriction.Equals(attrRecord.desc.restriction)))
                {
                    // Register attrTypeName -> attrRecord for potential future derived elements.
                    processingData.attributeTypeNames.TryAdd(attrTypeName, attrRecord);
                    return attrRecord.name;
                }
            }

            XmlQualifiedName xqn;
            FactoryProcessingHelper.AttributeRecord attributeRecord;

            if (description.restriction == null)
            {
                // Type is a built-in type.
                xqn = new XmlQualifiedName(description.type, description.typeNamespace);
                attributeRecord = new FactoryProcessingHelper.AttributeRecord { name = xqn, desc = description };
                processingData.attributeTypeNames.TryAdd(attrTypeName, attributeRecord);
                return xqn;
            }

            var attrTypeNameForSchema = $"{serializedData.uxmlName}_{description.name}_{k_TypeSuffix}";
            xqn = new XmlQualifiedName(attrTypeNameForSchema, schemaInfo.schema.TargetNamespace);

            var simpleType = new XmlSchemaSimpleType();
            simpleType.Name = attrTypeNameForSchema;

            if (description.restriction is UxmlEnumeration enumRestriction)
            {
                var restriction = new XmlSchemaSimpleTypeRestriction();
                simpleType.Content = restriction;
                restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                foreach (var v in enumRestriction.values)
                {
                    var enumValue = new XmlSchemaEnumerationFacet();
                    enumValue.Value = v;
                    restriction.Facets.Add(enumValue);
                }
            }
            else
            {
                if (description.restriction is UxmlValueMatches regexRestriction)
                {
                    if (regexRestriction.regex == null)
                    {
                        Debug.LogWarning($"{nameof(UxmlValueMatches)} restriction '{description.name}' has null regex value.");
                        return null;
                    }

                    var restriction = new XmlSchemaSimpleTypeRestriction();
                    simpleType.Content = restriction;
                    restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                    var pattern = new XmlSchemaPatternFacet();
                    pattern.Value = regexRestriction.regex;
                    restriction.Facets.Add(pattern);
                }
                else
                {
                    if (description.restriction is UxmlValueBounds bounds)
                    {
                        var restriction = new XmlSchemaSimpleTypeRestriction();
                        simpleType.Content = restriction;
                        restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                        if (!string.IsNullOrEmpty(bounds.min))
                        {
                            XmlSchemaFacet facet;
                            if (bounds.excludeMin)
                            {
                                facet = new XmlSchemaMinExclusiveFacet();
                            }
                            else
                            {
                                facet = new XmlSchemaMinInclusiveFacet();
                            }
                            facet.Value = bounds.min;
                            restriction.Facets.Add(facet);
                        }

                        if (!string.IsNullOrEmpty(bounds.max))
                        {
                            XmlSchemaFacet facet;
                            if (bounds.excludeMax)
                            {
                                facet = new XmlSchemaMaxExclusiveFacet();
                            }
                            else
                            {
                                facet = new XmlSchemaMaxInclusiveFacet();
                            }

                            facet.Value = bounds.max;
                            restriction.Facets.Add(facet);
                        }
                    }
                    else
                    {
                        Debug.Log("Unsupported restriction type.");
                    }
                }
            }

            attributeRecord = new FactoryProcessingHelper.AttributeRecord { name = xqn, desc = description };
            processingData.attributeTypeNames.TryAdd(attrTypeName, attributeRecord);

            schemaInfo.schema.Items.Add(simpleType);
            return xqn;
        }
    }
}
