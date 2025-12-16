// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Assemblies;
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
        public const string SchemaFolder = "UIElementsSchema";

        [MenuItem("Assets/Update UXML Schema", false, secondaryPriority = 3)]
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
            GenerateSchemaFiles(SchemaFolder);
        }

        internal class SchemaInfo
        {
            public SchemaInfo(string uxmlNamespace)
            {
                schema = new XmlSchema { ElementFormDefault = XmlSchemaForm.Qualified };
                if (uxmlNamespace != string.Empty)
                {
                    schema.TargetNamespace = uxmlNamespace;
                }
                namepacePrefix = GetPrefixForNamespace(uxmlNamespace);
            }

            public XmlSchema schema { get; }
            public string namepacePrefix { get; }
            public HashSet<string> importNamespaces { get; } = new();

            static Dictionary<string, string> s_NamespacePrefix { get; }

            static SchemaInfo()
            {
                s_NamespacePrefix = new Dictionary<string, string>
                {
                    { string.Empty, "global" },
                    { typeof(VisualElement).Namespace, "engine" },
                    { typeof(UxmlSchemaGenerator).Namespace, "editor" },
                };

                var userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
                foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
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

            static string GetPrefixForNamespace(string ns)
            {
                if (string.IsNullOrEmpty(ns))
                    return string.Empty;

                if (s_NamespacePrefix.TryGetValue(ns, out var prefix))
                    return prefix;

                s_NamespacePrefix[ns] = string.Empty;
                return string.Empty;
            }
        }

        const string k_DefaultNamespace = "UnityEngine.UIElements";
        const string k_XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        const string k_TypeSuffix = "Type";
        const string k_SchemaFileExtension = ".xsd";
        const string k_MainSchemaFileName = "UIElements" + k_SchemaFileExtension;
        const string k_GlobalNamespaceSchemaFileName = "GlobalNamespace" + k_SchemaFileExtension;

        internal class SchemaGenerator
        {
            const string k_XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

            static readonly XmlQualifiedName s_StringTypeQualifiedName = new XmlQualifiedName("string", k_XmlSchemaNamespace);
            static readonly XmlQualifiedName s_BoolTypeQualifiedName = new XmlQualifiedName("boolean", k_XmlSchemaNamespace);
            static readonly XmlQualifiedName s_BaseTypeAnyType = new XmlQualifiedName("anyType", k_XmlSchemaNamespace);
            static readonly XmlQualifiedName s_VisualElementName = new XmlQualifiedName(nameof(VisualElement), k_DefaultNamespace);

            public Dictionary<string, SchemaInfo> schemas { get; } = new();

            public Dictionary<Type, XmlQualifiedName> m_AttributeTypes = new()
            {
                { typeof(string), s_StringTypeQualifiedName },
                { typeof(short), new XmlQualifiedName("short", k_XmlSchemaNamespace) },
                { typeof(ushort), new XmlQualifiedName("unsignedShort", k_XmlSchemaNamespace) },
                { typeof(int), new XmlQualifiedName("int", k_XmlSchemaNamespace) },
                { typeof(uint), new XmlQualifiedName("unsignedInt", k_XmlSchemaNamespace) },
                { typeof(long), new XmlQualifiedName("long", k_XmlSchemaNamespace) },
                { typeof(ulong), new XmlQualifiedName("unsignedLong", k_XmlSchemaNamespace) },
                { typeof(float), new XmlQualifiedName("float", k_XmlSchemaNamespace) },
                { typeof(double), new XmlQualifiedName("double", k_XmlSchemaNamespace) },
                { typeof(bool), s_BoolTypeQualifiedName },
                { typeof(sbyte), new XmlQualifiedName("byte", k_XmlSchemaNamespace) },
                { typeof(byte), new XmlQualifiedName("unsignedByte", k_XmlSchemaNamespace) }
            };

            readonly Dictionary<Type, HashSet<string>> m_ProcessedTypes = new();

            /// <summary>
            /// Generates the schemas for all the types in <see cref="UxmlSerializedDataRegistry.SerializedDataTypes"/>.
            /// </summary>
            public void Generate()
            {
                foreach (var serializedDataType in UxmlSerializedDataRegistry.SerializedDataTypes.Values)
                {
                    var desc = UxmlSerializedDataRegistry.GetDescription(serializedDataType.DeclaringType.FullName);
                    AddElementType(desc);
                }

                AddSpecialElements();
            }

            /// <summary>
            /// Writes the compiles schemas to disk.
            /// </summary>
            /// <param name="directory"></param>
            public void WriteSchemaFiles(string directory)
            {
                directory ??= Application.temporaryCachePath;
                Directory.CreateDirectory(directory);

                // Compile schemas.
                var schemaSet = new XmlSchemaSet();
                var masterSchema = new XmlSchema { ElementFormDefault = XmlSchemaForm.Qualified };
                schemaSet.Add(masterSchema);

                foreach (var schemaInfo in schemas.Values)
                {
                    XmlSchemaExternal schemaExternal;
                    if (schemaInfo.schema.TargetNamespace != null)
                    {
                        // Import schema into the master schema.
                        schemaExternal = new XmlSchemaImport { Namespace = schemaInfo.schema.TargetNamespace };
                        schemaExternal.SchemaLocation = GetFileNameForNamespace(schemaInfo.schema.TargetNamespace);
                    }
                    else
                    {
                        schemaExternal = new XmlSchemaInclude();
                        schemaExternal.SchemaLocation = GetFileNameForNamespace(null);
                    }

                    var fileName = Path.Combine(directory, schemaExternal.SchemaLocation);
                    File.Delete(fileName);
                    masterSchema.Includes.Add(schemaExternal);

                    // Import referenced schemas into this XSD
                    foreach (var ns in schemaInfo.importNamespaces)
                    {
                        if (ns != schemaInfo.schema.TargetNamespace && ns != k_XmlSchemaNamespace)
                        {
                            schemaInfo.schema.Includes.Add(new XmlSchemaImport
                            {
                                Namespace = ns,
                                SchemaLocation = GetFileNameForNamespace(ns)
                            });
                        }
                    }

                    schemaSet.Add(schemaInfo.schema);
                }

                try
                {
                    schemaSet.Compile();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }

                // Now generate the schema textual data.

                // We dont want to include the BOM - https://unity.slack.com/archives/C06TQ0QMQ/p1701774372020839
                var encoding = new UTF8Encoding(false);
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

                    var fileName = Path.Combine(directory, schemaName);
                    using (var writer = new XmlTextWriter(fileName, encoding) { Formatting = Formatting.Indented })
                    {
                        compiledSchema.Write(writer);
                    }
                }
            }

            /// <summary>
            /// Returns the file name to use for the provided namespace. 
            /// If <paramref name="ns"/> is null or empty then the global namespace file name will be returned.
            /// </summary>
            /// <param name="ns"></param>
            /// <returns></returns>
            static string GetFileNameForNamespace(string ns)
            {
                return string.IsNullOrEmpty(ns) ? k_GlobalNamespaceSchemaFileName : ns + k_SchemaFileExtension;
            }

            /// <summary>
            /// Returns the <see cref="SchemaInfo"/> for the <paramref name="uxmlNamespace"/>.
            /// </summary>
            /// <param name="uxmlNamespace"></param>
            /// <returns></returns>
            internal SchemaInfo GetSchemaInfo(string uxmlNamespace)
            {
                uxmlNamespace ??= string.Empty;
                if (!schemas.TryGetValue(uxmlNamespace, out var schemaInfo))
                {
                    schemaInfo = new SchemaInfo(uxmlNamespace);
                    schemas[uxmlNamespace] = schemaInfo;
                }
                return schemaInfo;
            }

            void AddSpecialElements()
            {
                // UXML
                var uxmlType = AddFakeElement(k_DefaultNamespace, "UXML");

                var uxmlChildren = new XmlSchemaChoice
                {
                    MinOccurs = 0,
                    MaxOccursString = "unbounded"
                };
                uxmlType.type.Particle = uxmlChildren;
                uxmlChildren.Items.Add(new XmlSchemaElement { RefName = s_VisualElementName });
                uxmlType.type.Attributes.Add(new XmlSchemaAttribute { Name = "class", SchemaTypeName = s_StringTypeQualifiedName });
                uxmlType.type.Attributes.Add(new XmlSchemaAttribute { Name = "editor-extension-mode", SchemaTypeName = s_BoolTypeQualifiedName });

                // Style
                var styleType = AddFakeElement(k_DefaultNamespace, "Style");
                styleType.type.Attributes.Add(new XmlSchemaAttribute { Name = "name", SchemaTypeName = s_StringTypeQualifiedName });
                styleType.type.Attributes.Add(new XmlSchemaAttribute { Name = "path", SchemaTypeName = s_StringTypeQualifiedName });
                styleType.type.Attributes.Add(new XmlSchemaAttribute { Name = "src", SchemaTypeName = s_StringTypeQualifiedName });

                // Template
                var templateType = AddFakeElement(k_DefaultNamespace, "Template");
                templateType.type.Attributes.Add(new XmlSchemaAttribute { Name = "name", SchemaTypeName = s_StringTypeQualifiedName });
                templateType.type.Attributes.Add(new XmlSchemaAttribute { Name = "path", SchemaTypeName = s_StringTypeQualifiedName });
                templateType.type.Attributes.Add(new XmlSchemaAttribute { Name = "src", SchemaTypeName = s_StringTypeQualifiedName });

                // Instance
                var instanceType = AddFakeElement(k_DefaultNamespace, "Instance");
                templateType.type.Attributes.Add(new XmlSchemaAttribute { Name = "template", SchemaTypeName = s_StringTypeQualifiedName });

                // AttributeOverrides
                var attributeOverridesType = AddFakeElement(k_DefaultNamespace, "AttributeOverrides");
                templateType.type.Attributes.Add(new XmlSchemaAttribute { Name = "element-name", SchemaTypeName = s_StringTypeQualifiedName, Use = XmlSchemaUse.Required });
            }

            (XmlSchemaElement element, XmlSchemaComplexType type) AddFakeElement(string ns, string uxmlName)
            {
                var typeName = uxmlName + k_TypeSuffix;

                var xmlElementType = new XmlSchemaComplexType
                {
                    Name = typeName,
                };
                var schemaInfo = GetSchemaInfo(ns);
                schemaInfo.schema.Items.Add(xmlElementType);

                var attributes = xmlElementType.Attributes;

                var choice = new XmlSchemaChoice
                {
                    MinOccurs = 0,
                    MaxOccursString = "unbounded"
                };
                xmlElementType.Particle = choice;
                choice.Items.Add(new XmlSchemaElement { RefName = s_VisualElementName });

                // Add attribute wildcard to support derived types when using restrictions
                xmlElementType.AnyAttribute = new XmlSchemaAnyAttribute
                {
                    ProcessContents = XmlSchemaContentProcessing.Lax
                };

                // Add element to the schema.
                var element = new XmlSchemaElement
                {
                    Name = uxmlName,
                    SchemaTypeName = new XmlQualifiedName(xmlElementType.Name, ns)
                };
                schemaInfo.schema.Items.Add(element);

                return (element, xmlElementType);
            }

            /// <summary>
            /// Determines if a type should use extension instead of restriction for derivation.
            /// Returns true if the type adds new attributes or has UxmlObject children.
            /// </summary>
            bool ShouldUseExtension(UxmlSerializedDataDescription description, UxmlSerializedDataDescription baseType)
            {
                if (baseType == null)
                    return false; // No base type, not using extension or restriction

                // Check if this type adds new attributes not in the base type
                var baseTypeAttributes = m_ProcessedTypes.ContainsKey(baseType.serializedDataType.DeclaringType)
                    ? m_ProcessedTypes[baseType.serializedDataType.DeclaringType]
                    : null;

                if (baseTypeAttributes != null)
                {
                    foreach (var attr in description.serializedAttributes)
                    {
                        if (!baseTypeAttributes.Contains(attr.name))
                        {
                            // Found a new attribute - need extension
                            return true;
                        }
                    }
                }

                // Check if this type has UxmlObject attributes (child elements)
                foreach (var attr in description.serializedAttributes)
                {
                    if (attr is UxmlSerializedUxmlObjectAttributeDescription)
                    {
                        // Has UxmlObject children - need extension for flexibility
                        return true;
                    }
                }

                return false; // No new attributes or UxmlObjects - can use restriction
            }

            /// <summary>
            /// Adds the UxmlElement or UxmlObject type to the schema and returns the qualified XmlSchemaComplexType name.
            /// If the type has already been added then it will not be added again and the type name will just be returned.
            /// </summary>
            /// <param name="description"></param>
            /// <returns></returns>
            XmlQualifiedName AddElementType(UxmlSerializedDataDescription description)
            {
                var typeName = description.uxmlName + k_TypeSuffix;
                var elementType = description.serializedDataType.DeclaringType;
                if (m_ProcessedTypes.ContainsKey(elementType))
                    return new XmlQualifiedName(typeName, elementType.Namespace);

                var elementTypeAttributes = new HashSet<string>();
                m_ProcessedTypes.Add(elementType, elementTypeAttributes);

                var xmlElementType = new XmlSchemaComplexType
                {
                    Name = typeName,
                };
                var schemaInfo = GetSchemaInfo(elementType.Namespace);
                schemaInfo.schema.Items.Add(xmlElementType);

                (var baseTypeName, var baseType) = GetElementBaseType(description);

                var attributes = xmlElementType.Attributes;
                bool useExtension = false;

                // Determine whether to use extension or restriction based on type characteristics
                if (baseTypeName != null)
                {
                    useExtension = ShouldUseExtension(description, baseType);

                    if (useExtension)
                    {
                        // Use extension - allows adding new attributes and child elements
                        var complexContentExtension = new XmlSchemaComplexContentExtension { BaseTypeName = baseTypeName };
                        schemaInfo.importNamespaces.Add(complexContentExtension.BaseTypeName.Namespace);

                        xmlElementType.ContentModel = new XmlSchemaComplexContent { Content = complexContentExtension };
                        attributes = complexContentExtension.Attributes;
                    }
                    else
                    {
                        // Use restriction - for types that don't add new features
                        var complexContentRestriction = new XmlSchemaComplexContentRestriction { BaseTypeName = baseTypeName };
                        schemaInfo.importNamespaces.Add(complexContentRestriction.BaseTypeName.Namespace);

                        xmlElementType.ContentModel = new XmlSchemaComplexContent { Content = complexContentRestriction };
                        attributes = complexContentRestriction.Attributes;
                    }
                }

                AddAttributes(description, attributes, elementTypeAttributes, xmlElementType, schemaInfo);

                // Add attribute wildcard ONLY for restriction-based types
                // Extension-based types don't need wildcards as they can add attributes directly
                if (!useExtension)
                {
                    if (baseTypeName != null)
                    {
                        // Add anyAttribute to the restriction to match the base type's wildcard
                        var restriction = xmlElementType.ContentModel?.Content as XmlSchemaComplexContentRestriction;
                        if (restriction != null)
                        {
                            restriction.AnyAttribute = new XmlSchemaAnyAttribute
                            {
                                ProcessContents = XmlSchemaContentProcessing.Lax
                            };
                        }
                    }
                    else
                    {
                        // Base types need an attribute wildcard to allow derived restriction types to add attributes
                        xmlElementType.AnyAttribute = new XmlSchemaAnyAttribute
                        {
                            ProcessContents = XmlSchemaContentProcessing.Lax
                        };
                    }
                }

                // Setup expected child types
                if (baseTypeName == null)
                {
                    // Base types define their own child element choices
                    var rootChoice = GetRootChoice(xmlElementType);
                    rootChoice.MinOccurs = 0;
                    rootChoice.MaxOccursString = "unbounded";

                    foreach (var childType in description.uxmlSupportedChildTypes)
                    {
                        var desc = UxmlSerializedDataRegistry.GetDescription(childType.FullName);
                        if (desc != null)
                        {
                            var childElementType = AddElementType(desc);
                            rootChoice.Items.Add(new XmlSchemaElement
                            {
                                RefName = new XmlQualifiedName(desc.uxmlName, childElementType.Namespace)
                            });
                        }
                    }
                }
                else if (useExtension)
                {
                    // Extension-based derived types can freely add child elements via GetRootChoice
                    // UxmlObjects will be added by AddAttributeUxmlObjectType if present
                    var rootChoice = GetRootChoice(xmlElementType);
                    if (rootChoice.Items.Count > 0)
                    {
                        // UxmlObjects were added, ensure choice has proper min/max
                        rootChoice.MinOccurs = 0;
                        rootChoice.MaxOccursString = "unbounded";
                    }
                }
                else
                {
                    // Restriction-based derived types must have a particle that restricts the base type
                    var rootChoice = GetRootChoice(xmlElementType);

                    // Only add the generic VisualElement reference if UxmlObjects haven't already added elements
                    // With restrictions, we cannot have more choice members than the base type
                    if (rootChoice.Items.Count == 0)
                    {
                        rootChoice.MinOccurs = 0;
                        rootChoice.MaxOccursString = "unbounded";
                        rootChoice.Items.Add(new XmlSchemaElement
                        {
                            RefName = s_VisualElementName
                        });
                    }
                    else
                    {
                        // UxmlObjects were already added to the choice, set the min/max on the choice
                        rootChoice.MinOccurs = 0;
                        rootChoice.MaxOccursString = "unbounded";
                    }
                }

                // Add element to the schema.
                var element = new XmlSchemaElement
                {
                    Name = description.uxmlName,
                    SchemaTypeName = new XmlQualifiedName(xmlElementType.Name, elementType.Namespace),
                };

                // A substitution group allows you to define that one element can be used in place of another element.
                // Used to support polymorphism when setting what types can be children of an element.
                if (baseTypeName != null)
                {
                    element.SubstitutionGroup = new XmlQualifiedName(baseType.uxmlName, baseTypeName.Namespace);
                }

                schemaInfo.schema.Items.Add(element);

                return new XmlQualifiedName(typeName, elementType.Namespace);
            }

            /// <summary>
            /// Adds all the attributes and inherited attributes that do not belong to any parent UxmlElement/UxmlObject for the type. 
            /// </summary>
            /// <param name="description"></param>
            /// <param name="attributes"></param>
            /// <param name="handledAttributes"></param>
            /// <param name="elementType"></param>
            /// <param name="schemaInfo"></param>
            void AddAttributes(UxmlSerializedDataDescription description, XmlSchemaObjectCollection attributes, HashSet<string> handledAttributes, XmlSchemaComplexType elementType, SchemaInfo schemaInfo)
            {
                HashSet<string> baseTypeAttributes = null;
                if (GetElementBaseType(description).baseTypeDescription is { } baseType)
                {
                    baseTypeAttributes = m_ProcessedTypes[baseType.serializedDataType.DeclaringType];
                }

                // For user created types, they may return null for uxmlAttributeDescription, so we need to check in order not to crash.
                foreach (var attributeDescription in description.serializedAttributes)
                {
                    handledAttributes.Add(attributeDescription.name);

                    // With restrictions + anyAttribute wildcards, inherited attributes are allowed through the wildcard.
                    // We only define new attributes specific to this type, not inherited ones.
                    // Redefining an inherited attribute with the same type would be invalid in XSD.
                    if (baseTypeAttributes?.Contains(attributeDescription.name) == true)
                    {
                        continue;
                    }

                    // Handle UxmlObjects
                    if (attributeDescription is UxmlSerializedUxmlObjectAttributeDescription objectAttributeDescription)
                    {
                        AddAttributeUxmlObjectType(description, objectAttributeDescription, elementType, schemaInfo);
                        continue;
                    }

                    var attributeQualifiedName = AddAttributeType(attributeDescription);
                    if (attributeQualifiedName == null)
                        continue;

                    var xmlAttribute = new XmlSchemaAttribute
                    {
                        Name = attributeDescription.name,
                        SchemaTypeName = attributeQualifiedName,
                    };

                    var defaultValue = ExtractDefaultValueForAttribute(attributeDescription, attributeQualifiedName);
                    if (!string.IsNullOrEmpty(defaultValue))
                        xmlAttribute.DefaultValue = defaultValue;

                    attributes.Add(xmlAttribute);
                    schemaInfo.importNamespaces.Add(attributeQualifiedName.Namespace);
                }

                // Add special common UXML attributes for VisualElement base type
                // "class" and "style" are handled by VisualElementAsset, not UxmlSerializedData
                // so we need to add them explicitly for proper schema validation
                var elementTypeDeclaringType = description.serializedDataType.DeclaringType;
                if (elementTypeDeclaringType == typeof(VisualElement))
                {
                    // Add "class" attribute (optional string for CSS class names)
                    if (!handledAttributes.Contains("class"))
                    {
                        attributes.Add(new XmlSchemaAttribute
                        {
                            Name = "class",
                            SchemaTypeName = s_StringTypeQualifiedName,
                            Use = XmlSchemaUse.Optional
                        });
                        handledAttributes.Add("class");
                    }

                    // Add "style" attribute (optional string for inline styles)
                    if (!handledAttributes.Contains("style"))
                    {
                        attributes.Add(new XmlSchemaAttribute
                        {
                            Name = "style",
                            SchemaTypeName = s_StringTypeQualifiedName,
                            Use = XmlSchemaUse.Optional
                        });
                        handledAttributes.Add("style");
                    }
                }
            }

            static string ExtractDefaultValueForAttribute(UxmlSerializedAttributeDescription attributeDescription, XmlQualifiedName uxmlType)
            {
                try
                {
                    if (attributeDescription.defaultValue == null ||
                        !UxmlAttributeConverter.TryConvertToString(attributeDescription.defaultValue, null, out var defaultValue) ||
                        defaultValue == null)
                        return null;

                    // Cleanup the default value.

                    // Bools shoudld be lowercase.
                    if (ReferenceEquals(uxmlType, s_BoolTypeQualifiedName))
                        defaultValue = defaultValue.ToLower();
                    // Remove null terminator from strings, this can happen when converting a char to a string.
                    else if (ReferenceEquals(uxmlType, s_StringTypeQualifiedName))
                        defaultValue = defaultValue.Trim('\0');

                    return defaultValue;
                }
                catch (NotImplementedException)
                {
                    // We ignore these as we have some unimplemented converters
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return null;
            }

            (XmlQualifiedName baseTypeName, UxmlSerializedDataDescription baseTypeDescription) GetElementBaseType(UxmlSerializedDataDescription description)
            {
                // Find the next base type that has a description.
                var baseType = description.serializedDataType.DeclaringType.BaseType;
                while (baseType != null)
                {
                    var desc = UxmlSerializedDataRegistry.GetDescription(baseType.FullName);
                    if (desc != null)
                    {
                        // We can handle it like a normal element type.
                        return (AddElementType(desc), desc);
                    }
                    baseType = baseType.BaseType;
                }

                return default;
            }

            /// <summary>
            /// Adds the attributes type to the schema and returns the qualified name. 
            /// Returns null if the type is not supported and the attribute should be ignored.
            /// </summary>
            /// <param name="attributeDescription"></param>
            /// <returns></returns>
            XmlQualifiedName AddAttributeType(UxmlSerializedAttributeDescription attributeDescription)
            {
                if (!m_AttributeTypes.TryGetValue(attributeDescription.type, out var xmlQualifiedName))
                {
                    if (attributeDescription.type.IsEnum)
                    {
                        xmlQualifiedName = AddEnumType(attributeDescription.type);
                    }
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(attributeDescription.type))
                    {
                        xmlQualifiedName = s_StringTypeQualifiedName;
                    }
                    else if (UxmlAttributeConverter.TryGetConverter(attributeDescription.type, out var converter))
                    {
                        // Treat as string for now.
                        xmlQualifiedName = s_StringTypeQualifiedName;
                    }

                    m_AttributeTypes[attributeDescription.type] = xmlQualifiedName;
                }

                return xmlQualifiedName;
            }

            XmlSchemaGroupBase GetRootSequence(XmlSchemaComplexType elementType)
            {
                if (elementType?.ContentModel?.Content is XmlSchemaComplexContentRestriction restriction)
                {
                    if (restriction.Particle == null)
                    {
                        var rootSequence = new XmlSchemaSequence();
                        restriction.Particle = rootSequence;
                        return rootSequence;
                    }
                    else
                    {
                        return restriction.Particle as XmlSchemaSequence;
                    }
                }
                else
                {
                    if (elementType.Particle == null)
                    {
                        var rootSequence = new XmlSchemaSequence();
                        elementType.Particle = rootSequence;
                        return rootSequence;
                    }
                    else
                    {
                        return elementType.Particle as XmlSchemaSequence;
                    }
                }
            }

            XmlSchemaChoice GetRootChoice(XmlSchemaComplexType elementType)
            {
                // Handle both restrictions and extensions
                if (elementType?.ContentModel?.Content is XmlSchemaComplexContentRestriction restriction)
                {
                    if (restriction.Particle == null)
                    {
                        var rootChoice = new XmlSchemaChoice();
                        restriction.Particle = rootChoice;
                        return rootChoice;
                    }
                    else
                    {
                        return restriction.Particle as XmlSchemaChoice;
                    }
                }
                else if (elementType?.ContentModel?.Content is XmlSchemaComplexContentExtension extension)
                {
                    if (extension.Particle == null)
                    {
                        var rootChoice = new XmlSchemaChoice();
                        extension.Particle = rootChoice;
                        return rootChoice;
                    }
                    else
                    {
                        return extension.Particle as XmlSchemaChoice;
                    }
                }
                else
                {
                    if (elementType.Particle == null)
                    {
                        var rootChoice = new XmlSchemaChoice();
                        elementType.Particle = rootChoice;
                        return rootChoice;
                    }
                    else
                    {
                        return elementType.Particle as XmlSchemaChoice;
                    }
                }
            }

            /// <summary>
            /// Adds UxmlObjects as child elements to the schema.
            /// </summary>
            /// <param name="description"></param>
            /// <param name="attributeDescription"></param>
            /// <param name="elementType"></param>
            /// <param name="schemaInfo"></param>
            void AddAttributeUxmlObjectType(UxmlSerializedDataDescription description, UxmlSerializedUxmlObjectAttributeDescription attributeDescription, XmlSchemaComplexType elementType, SchemaInfo schemaInfo)
            {
                // Define choice of child elements (consistent with other element handling)
                var rootChoice = GetRootChoice(elementType);

                // Create the attribute root element
                if (!string.IsNullOrEmpty(attributeDescription.rootName))
                {
                    var attributeTypeName = description.uxmlName + attributeDescription.name + k_TypeSuffix;
                    var rootType = new XmlSchemaComplexType { Name = attributeTypeName, ContentModel = new XmlSchemaComplexContent() };

                    var restriction = new XmlSchemaComplexContentRestriction { BaseTypeName = s_BaseTypeAnyType };
                    rootType.ContentModel.Content = restriction;

                    schemaInfo.schema.Items.Add(rootType);
                    rootChoice.Items.Add(new XmlSchemaElement
                    {
                        Name = attributeDescription.rootName,
                        SchemaTypeName = new XmlQualifiedName(attributeTypeName, description.serializedDataType.Namespace),

                        // We only have one root element.
                        MinOccurs = 0,
                        MaxOccurs = 1,

                        // This tells the serializer that this element is not qualified with a namespace.
                        Form = XmlSchemaForm.Unqualified
                    });

                    var nestedChoice = new XmlSchemaChoice();
                    if (attributeDescription.isList)
                        nestedChoice.MaxOccursString = "unbounded";
                    rootChoice = nestedChoice;
                    restriction.Particle = rootChoice;
                }

                // Track which elements have already been added to avoid duplicates
                var existingElements = new HashSet<XmlQualifiedName>();
                foreach (var item in rootChoice.Items)
                {
                    if (item is XmlSchemaElement existingElement && existingElement.RefName != null)
                    {
                        existingElements.Add(existingElement.RefName);
                    }
                }

                foreach (var acceptedType in attributeDescription.uxmlObjectAcceptedTypes)
                {
                    var acceptedTypeDescription = UxmlSerializedDataRegistry.GetDescription(acceptedType.DeclaringType.FullName);
                    if (acceptedTypeDescription == null)
                        continue;

                    var qualifiedName = new XmlQualifiedName(acceptedTypeDescription.uxmlName, acceptedType.DeclaringType.Namespace);

                    // Skip if this element is already in the choice (prevents ambiguous content model)
                    if (existingElements.Contains(qualifiedName))
                        continue;

                    var element = new XmlSchemaElement
                    {
                        RefName = qualifiedName
                    };
                    rootChoice.Items.Add(element);
                    existingElements.Add(qualifiedName);

                    // We need to apply limits to the number of elements here as we can not do it in the root.
                    if (!attributeDescription.isList && !string.IsNullOrEmpty(attributeDescription.rootName))
                    {
                        element.MinOccurs = 0;
                        element.MaxOccurs = 1;
                    }

                    schemaInfo.importNamespaces.Add(acceptedType.DeclaringType.Namespace);
                }
            }

            /// <summary>
            /// Adds the enum type to the schema and returns the qualified name.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            XmlQualifiedName AddEnumType(Type type)
            {
                var schemaInfo = GetSchemaInfo(type.Namespace);

                var simpleType = new XmlSchemaSimpleType();
                simpleType.Name = type.Name + k_TypeSuffix;
                schemaInfo.schema.Items.Add(simpleType);

                var restriction = new XmlSchemaSimpleTypeRestriction();
                simpleType.Content = restriction;
                restriction.BaseTypeName = s_StringTypeQualifiedName;

                foreach (var name in Enum.GetNames(type))
                {
                    var enumValue = new XmlSchemaEnumerationFacet();
                    enumValue.Value = name;
                    restriction.Facets.Add(enumValue);
                }

                return new XmlQualifiedName(simpleType.Name, type.Namespace);
            }
        }

        /// <summary>
        /// Generates the schema files.
        /// </summary>
        /// <param name="baseDir"></param>
        internal static void GenerateSchemaFiles(string baseDir = null)
        {
            try
            {
                var schemaData = new SchemaGenerator();
                EditorUtility.DisplayProgressBar(L10n.Tr("Generating UXML Schema Files"), L10n.Tr("Please wait..."), 0.0f);

                schemaData.Generate();

                EditorUtility.DisplayProgressBar(L10n.Tr("Generating UXML Schema Files"), L10n.Tr("Please wait..."), 0.75f);

                schemaData.WriteSchemaFiles(baseDir);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
