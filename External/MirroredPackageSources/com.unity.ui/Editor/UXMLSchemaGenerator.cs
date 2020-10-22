using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using UnityEditor.ProjectWindowCallback;
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

        public static void UpdateSchemaFiles()
        {
            Directory.CreateDirectory(k_SchemaFolder);
            using (var it = GenerateSchemaFiles(k_SchemaFolder + "/").GetEnumerator())
            {
                while (it.MoveNext())
                {
                    string fileName = it.Current;
                    if (it.MoveNext())
                    {
                        string data = it.Current;
                        var action = ScriptableObject.CreateInstance<DoCreateAssetWithContent>();
                        action.filecontent = data;

                        ProjectWindowUtil.EndNameEditAction(action, 0, fileName, null, true);
                        Selection.activeObject = EditorUtility.InstanceIDToObject(0);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        internal static Dictionary<string, string> GetNamespacePrefixDictionary()
        {
            return SchemaInfo.s_NamespacePrefix;
        }

        sealed class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }

        class SchemaInfo
        {
            public SchemaInfo(string uxmlNamespace)
            {
                schema = new XmlSchema();
                schema.ElementFormDefault = XmlSchemaForm.Qualified;
                if (uxmlNamespace != String.Empty)
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

                    s_NamespacePrefix.Add(String.Empty, "global");
                    s_NamespacePrefix.Add(typeof(VisualElement).Namespace, "engine");
                    s_NamespacePrefix.Add(typeof(UxmlSchemaGenerator).Namespace, "editor");

                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    HashSet<string> userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
                    foreach (Assembly assembly in currentDomain.GetAssemblies())
                    {
                        if (!userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                            continue;

                        try
                        {
                            foreach (object nsPrefixAttributeObject in assembly.GetCustomAttributes(typeof(UxmlNamespacePrefixAttribute), false))
                            {
                                UxmlNamespacePrefixAttribute nsPrefixAttribute = (UxmlNamespacePrefixAttribute)nsPrefixAttributeObject;
                                s_NamespacePrefix[nsPrefixAttribute.ns] = nsPrefixAttribute.prefix;
                            }
                        }
                        catch (TypeLoadException e)
                        {
                            Debug.LogWarningFormat("Error while loading types from assembly {0}: {1}", assembly.FullName, e);
                        }
                    }
                }

                string prefix;
                if (s_NamespacePrefix.TryGetValue(ns ?? String.Empty, out prefix))
                {
                    return prefix;
                }

                s_NamespacePrefix[ns] = String.Empty;
                return String.Empty;
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
            Dictionary<string, SchemaInfo> schemas = new Dictionary<string, SchemaInfo>();
            List<IUxmlFactory> deferredFactories = new List<IUxmlFactory>();
            FactoryProcessingHelper processingData = new FactoryProcessingHelper();

            if (baseDir == null)
            {
                baseDir = Application.temporaryCachePath + "/";
            }

            // Convert the factories into schemas info.
            foreach (var factories in VisualElementFactoryRegistry.factories)
            {
                if (factories.Value.Count == 0)
                    continue;

                // Only process the first factory, as the other factories define the same element.
                IUxmlFactory factory = factories.Value[0];
                if (!ProcessFactory(factory, schemas, processingData))
                {
                    // Could not process the factory now, because it depends on a yet unprocessed factory.
                    // Defer its processing.
                    deferredFactories.Add(factory);
                }
            }

            List<IUxmlFactory> deferredFactoriesCopy;
            do
            {
                deferredFactoriesCopy = new List<IUxmlFactory>(deferredFactories);
                foreach (var factory in deferredFactoriesCopy)
                {
                    deferredFactories.Remove(factory);
                    if (!ProcessFactory(factory, schemas, processingData))
                    {
                        // Could not process the factory now, because it depends on a yet unprocessed factory.
                        // Defer its processing again.
                        deferredFactories.Add(factory);
                    }
                }
            }
            while (deferredFactoriesCopy.Count > deferredFactories.Count);

            if (deferredFactories.Count > 0)
            {
                Debug.Log("Some factories could not be processed because their base type is missing.");
            }

            // Compile schemas.
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            XmlSchema masterSchema = new XmlSchema();
            masterSchema.ElementFormDefault = XmlSchemaForm.Qualified;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("xs", k_XmlSchemaNamespace);

            File.Delete(baseDir + k_MainSchemaFileName);

            foreach (var schema in schemas)
            {
                if (schema.Value.schema.TargetNamespace != null)
                {
                    nsmgr.AddNamespace(schema.Value.namepacePrefix, schema.Value.schema.TargetNamespace);

                    // Import schema into the master schema.
                    XmlSchemaImport import = new XmlSchemaImport();
                    import.Namespace = schema.Value.schema.TargetNamespace;
                    string schemaLocation = GetFileNameForNamespace(schema.Value.schema.TargetNamespace);
                    File.Delete(baseDir + schemaLocation);
                    import.SchemaLocation = schemaLocation;
                    masterSchema.Includes.Add(import);
                }
                else
                {
                    XmlSchemaInclude include = new XmlSchemaInclude();
                    string schemaLocation = GetFileNameForNamespace(null);
                    File.Delete(baseDir + schemaLocation);
                    include.SchemaLocation = schemaLocation;
                    masterSchema.Includes.Add(include);
                }

                // Import referenced schemas into this XSD
                foreach (string ns in schema.Value.importNamespaces)
                {
                    if (ns != schema.Value.schema.TargetNamespace && ns != k_XmlSchemaNamespace)
                    {
                        XmlSchemaImport import = new XmlSchemaImport();
                        import.Namespace = ns;
                        import.SchemaLocation = GetFileNameForNamespace(ns);
                        schema.Value.schema.Includes.Add(import);
                    }
                }

                schemaSet.Add(schema.Value.schema);
            }
            schemaSet.Add(masterSchema);
            schemaSet.Compile();

            // Now generate the schema textual data.
            foreach (XmlSchema compiledSchema in schemaSet.Schemas())
            {
                string schemaName = compiledSchema.TargetNamespace;

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

        internal static string GetFileNameForNamespace(string ns)
        {
            return String.IsNullOrEmpty(ns) ? k_GlobalNamespaceSchemaFileName : ns + k_SchemaFileExtension;
        }

        static bool ProcessFactory(IUxmlFactory factory, Dictionary<string, SchemaInfo> schemas, FactoryProcessingHelper processingData)
        {
            if (!string.IsNullOrEmpty(factory.substituteForTypeName))
            {
                if (!processingData.IsKnownElementType(factory.substituteForTypeName, factory.substituteForTypeNamespace))
                {
                    // substituteForTypeName is not yet known. Defer processing to later.
                    return false;
                }
            }

            string uxmlNamespace = factory.uxmlNamespace;
            SchemaInfo schemaInfo;
            if (!schemas.TryGetValue(uxmlNamespace, out schemaInfo))
            {
                schemaInfo = new SchemaInfo(uxmlNamespace);
                schemas[uxmlNamespace] = schemaInfo;
            }

            XmlSchemaType type = AddElementTypeToXmlSchema(factory, schemaInfo, processingData);
            AddElementToXmlSchema(factory, schemaInfo, type);

            processingData.RegisterElementType(factory.uxmlName, factory.uxmlNamespace);

            return true;
        }

        static XmlSchemaParticle MakeChoiceSequence(IEnumerable<UxmlChildElementDescription> elements)
        {
            if (!elements.Any())
            {
                return null;
            }
            else
            {
                XmlSchemaSequence sequence = new XmlSchemaSequence();
                sequence.MinOccurs = 0;
                sequence.MaxOccursString = "unbounded";

                if (elements.Count() == 1)
                {
                    IEnumerator<UxmlChildElementDescription> enumerator = elements.GetEnumerator();
                    enumerator.MoveNext();
                    XmlSchemaElement elementRef = new XmlSchemaElement();
                    elementRef.RefName = new XmlQualifiedName(enumerator.Current.elementName, enumerator.Current.elementNamespace);
                    sequence.Items.Add(elementRef);
                }
                else
                {
                    XmlSchemaChoice choice = new XmlSchemaChoice();

                    foreach (UxmlChildElementDescription element in elements)
                    {
                        XmlSchemaElement elementRef = new XmlSchemaElement();
                        elementRef.RefName = new XmlQualifiedName(element.elementName, element.elementNamespace);
                        choice.Items.Add(elementRef);
                    }
                    sequence.Items.Add(choice);
                }

                return sequence;
            }
        }

        static XmlSchemaType AddElementTypeToXmlSchema(IUxmlFactory factory, SchemaInfo schemaInfo, FactoryProcessingHelper processingData)
        {
            // We always have complex types with complex content.
            XmlSchemaComplexType elementType = new XmlSchemaComplexType();
            elementType.Name = factory.uxmlName + k_TypeSuffix;

            XmlSchemaComplexContent content = new XmlSchemaComplexContent();
            elementType.ContentModel = content;

            // We only support restrictions of base types.
            XmlSchemaComplexContentRestriction restriction = new XmlSchemaComplexContentRestriction();
            content.Content = restriction;

            if (factory.substituteForTypeName == String.Empty)
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
                XmlSchemaAnyAttribute anyAttribute = new XmlSchemaAnyAttribute();
                anyAttribute.ProcessContents = XmlSchemaContentProcessing.Lax;
                restriction.AnyAttribute = anyAttribute;
            }

            foreach (UxmlAttributeDescription attrDesc in factory.uxmlAttributesDescription)
            {
                XmlQualifiedName typeName = AddAttributeTypeToXmlSchema(schemaInfo, attrDesc, factory, processingData);
                if (typeName != null)
                {
                    AddAttributeToXmlSchema(restriction, attrDesc, typeName);
                    schemaInfo.importNamespaces.Add(attrDesc.typeNamespace);
                }
            }

            bool hasChildElements = false;
            foreach (UxmlChildElementDescription childDesc in factory.uxmlChildElementsDescription)
            {
                hasChildElements = true;
                schemaInfo.importNamespaces.Add(childDesc.elementNamespace);
            }

            if (hasChildElements)
            {
                restriction.Particle = MakeChoiceSequence(factory.uxmlChildElementsDescription);
            }

            schemaInfo.schema.Items.Add(elementType);
            return elementType;
        }

        static void AddElementToXmlSchema(IUxmlFactory factory, SchemaInfo schemaInfo, XmlSchemaType type)
        {
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = factory.uxmlName;

            if (type != null)
            {
                element.SchemaTypeName = new XmlQualifiedName(type.Name, factory.uxmlNamespace);
            }

            if (factory.substituteForTypeName != String.Empty)
            {
                element.SubstitutionGroup = new XmlQualifiedName(factory.substituteForTypeName, factory.substituteForTypeNamespace);
            }

            schemaInfo.schema.Items.Add(element);
        }

        static XmlQualifiedName AddAttributeTypeToXmlSchema(SchemaInfo schemaInfo, UxmlAttributeDescription description, IUxmlFactory factory, FactoryProcessingHelper processingData)
        {
            if (description.name == null)
            {
                return null;
            }

            string attrTypeName = factory.uxmlQualifiedName + "_" + description.name + "_" + k_TypeSuffix;
            string attrTypeNameInBaseElement = factory.substituteForTypeQualifiedName + "_" + description.name + "_" + k_TypeSuffix;

            FactoryProcessingHelper.AttributeRecord attrRecord;
            if (processingData.attributeTypeNames.TryGetValue(attrTypeNameInBaseElement, out attrRecord))
            {
                // If restriction != baseElement.restriction, we need to declare a new type.
                // Note: we do not support attributes having a less restrictive restriction than its base type.
                if ((description.restriction == null && attrRecord.desc.restriction == null) ||
                    (description.restriction != null && description.restriction.Equals(attrRecord.desc.restriction)))
                {
                    // Register attrTypeName -> attrRecord for potential future derived elements.
                    processingData.attributeTypeNames.Add(attrTypeName, attrRecord);
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
                processingData.attributeTypeNames.Add(attrTypeName, attributeRecord);
                return xqn;
            }

            string attrTypeNameForSchema = factory.uxmlName + "_" + description.name + "_" + k_TypeSuffix;
            xqn = new XmlQualifiedName(attrTypeNameForSchema, schemaInfo.schema.TargetNamespace);

            XmlSchemaSimpleType simpleType = new XmlSchemaSimpleType();
            simpleType.Name = attrTypeNameForSchema;

            UxmlEnumeration enumRestriction = description.restriction as UxmlEnumeration;
            if (enumRestriction != null)
            {
                XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
                simpleType.Content = restriction;
                restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                foreach (var v in enumRestriction.values)
                {
                    XmlSchemaEnumerationFacet enumValue = new XmlSchemaEnumerationFacet();
                    enumValue.Value = v;
                    restriction.Facets.Add(enumValue);
                }
            }
            else
            {
                UxmlValueMatches regexRestriction = description.restriction as UxmlValueMatches;
                if (regexRestriction != null)
                {
                    XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
                    simpleType.Content = restriction;
                    restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

                    XmlSchemaPatternFacet pattern = new XmlSchemaPatternFacet();
                    pattern.Value = regexRestriction.regex;
                    restriction.Facets.Add(pattern);
                }
                else
                {
                    UxmlValueBounds bounds = description.restriction as UxmlValueBounds;
                    if (bounds != null)
                    {
                        XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
                        simpleType.Content = restriction;
                        restriction.BaseTypeName = new XmlQualifiedName(description.type, description.typeNamespace);

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
                    else
                    {
                        Debug.Log("Unsupported restriction type.");
                    }
                }
            }

            schemaInfo.schema.Items.Add(simpleType);
            attributeRecord = new FactoryProcessingHelper.AttributeRecord { name = xqn, desc = description };
            processingData.attributeTypeNames.Add(attrTypeName, attributeRecord);
            return xqn;
        }

        static void AddAttributeToXmlSchema(XmlSchemaComplexContentRestriction restriction, UxmlAttributeDescription description, XmlQualifiedName typeName)
        {
            XmlSchemaAttribute attr = new XmlSchemaAttribute();
            attr.Name = description.name;
            attr.SchemaTypeName = typeName;

            switch (description.use)
            {
                case UxmlAttributeDescription.Use.Optional:
                    attr.Use = XmlSchemaUse.Optional;
                    attr.DefaultValue = description.defaultValueAsString;
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

            restriction.Attributes.Add(attr);
        }
    }
}
