// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ExCSS;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using StyleSheet = UnityEngine.StyleSheets.StyleSheet;

namespace UnityEditor.Experimental.UIElements
{
    [ScriptedImporter(1, "uxml", 0)]
    internal class UIElementsViewImporter : ScriptedImporter
    {
        private const string k_XmlTemplate = "<" + k_TemplateNode + @" xmlns:ui=""UnityEngine.Experimental.UIElements"">
  <ui:Label text=""New UXML"" />
</" + k_TemplateNode + ">";

        [MenuItem("Assets/Create/UIElements View")]
        public static void CreateTemplateMenuItem()
        {
            ProjectWindowUtil.CreateAssetWithContent("New UXML.uxml", k_XmlTemplate);
        }

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

            public Error(ImportErrorType error, ImportErrorCode code, object context, Level level, string filePath, IXmlLineInfo xmlLineInfo)
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
                        return "Expected the XML Root element name to be 'Template', found '{0}'";
                    case ImportErrorCode.UsingHasEmptyAlias:
                        return "'Using' declaration requires a non-empty alias";
                    case ImportErrorCode.MissingPathAttributeOnUsing:
                        return "'Using' declaration requires a 'path' attribute referencing another uxml file";
                    case ImportErrorCode.DuplicateUsingAlias:
                        return "Duplicate alias '{0}'";
                    case ImportErrorCode.UnknownElement:
                        return "Could not resolve the element name '{0}'";
                    case ImportErrorCode.UnknownAttribute:
                        return "Unknown attribute: '{0}'";
                    case ImportErrorCode.InvalidCssInStyleAttribute:
                        return "USS in style attribute is invalid: {0}";
                    default:
                        throw new ArgumentOutOfRangeException("Unhandled error code " + errorCode);
                }
            }

            public override string ToString()
            {
                string message = ErrorMessage(code);
                string lineInfo = xmlLineInfo == null ? "" : string.Format(" ({0},{1})", xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
                return string.Format("{0}{1}: {2} - {3}", filePath, lineInfo, error, string.Format(message, context));
            }
        }

        internal class DefaultLogger
        {
            protected List<Error> m_Errors = new List<Error>();
            protected string m_Path;

            internal virtual void LogError(ImportErrorType error, ImportErrorCode code, object context, Error.Level level, IXmlLineInfo xmlLineInfo)
            {
                m_Errors.Add(new Error(error, code, context, level, m_Path, xmlLineInfo));
            }

            internal virtual void BeginImport(string path)
            {
                m_Path = path;
            }

            internal virtual void FinishImport()
            {
                Dictionary<string, VisualTreeAsset> cache = new Dictionary<string, VisualTreeAsset>();

                foreach (var error in m_Errors)
                {
                    VisualTreeAsset obj;
                    if (!cache.TryGetValue(error.filePath, out obj))
                        cache.Add(error.filePath, obj = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(error.filePath));

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
                            throw new ArgumentOutOfRangeException();
                    }
                }

                m_Errors.Clear();
            }
        }

        private const StringComparison k_Comparison = StringComparison.InvariantCulture;
        private const string k_TemplateNode = "UXML";
        private const string k_UsingNode = "Using";
        private const string k_UsingAliasAttr = "alias";
        private const string k_UsingPathAttr = "path";

        internal static DefaultLogger logger = new DefaultLogger();

        private static readonly StringComparer k_Comparer = StringComparer.InvariantCulture;
        private static readonly StringComparer k_AttributeComparer = StringComparer.Ordinal;

        // element type name to asset type mapping
        private static Dictionary<string, Type> s_EltTypes;

        private static HashSet<string> s_ExcludedFields;

        // all known element types
        private static Dictionary<string, Type> elementTypes
        {
            get
            {
                if (s_EltTypes == null)
                {
                    s_EltTypes = new Dictionary<string, Type>(k_Comparer);
                    foreach (var tt in typeof(VisualElementAsset).Assembly.GetTypes())
                    {
                        if (typeof(VisualElementAsset).IsAssignableFrom(tt) &&
                            !tt.IsAbstract &&
                            tt != typeof(TemplateAsset) &&
                            tt.BaseType != null &&
                            tt.BaseType.IsGenericType)
                        {
                            s_EltTypes.Add(tt.BaseType.GetGenericArguments()[0].FullName, tt);
                        }
                    }
                }
                return s_EltTypes;
            }
        }

        public override void OnImportAsset(AssetImportContext args)
        {
            logger.BeginImport(args.assetPath);
            VisualTreeAsset vta;
            if (ImportXml(args.assetPath, out vta))
            {
                foreach (VisualElementAsset vea in vta.visualElementAssets)
                    args.AddSubAsset(string.Format("c_{0}_{1}", vea.name ?? "x", vea.GetType().Name), vea);
            }

            args.SetMainAsset("tree", vta);
            if (!vta.inlineSheet)
                vta.inlineSheet = ScriptableObject.CreateInstance<StyleSheet>();

            args.AddSubAsset("inlineStyle", vta.inlineSheet);
        }

        internal static bool ImportXml(string xmlPath, out VisualTreeAsset vta)
        {
            vta = ScriptableObject.CreateInstance<VisualTreeAsset>();
            vta.visualElementAssets = new List<VisualElementAsset>();

            XDocument doc;

            try
            {
                doc = XDocument.Load(xmlPath, LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                logger.LogError(ImportErrorType.Syntax, ImportErrorCode.InvalidXml, e, Error.Level.Fatal, null);
                return false;
            }

            StyleSheetBuilder ssb = new StyleSheetBuilder();
            LoadXmlRoot(doc, vta, ssb);

            StyleSheet inlineSheet = ScriptableObject.CreateInstance<StyleSheet>();
            inlineSheet.name = "inlineStyle";
            ssb.BuildTo(inlineSheet);
            vta.inlineSheet = inlineSheet;

            return true;
        }

        private static void LoadXmlRoot(XDocument doc, VisualTreeAsset vta, StyleSheetBuilder ssb)
        {
            XElement elt = doc.Root;
            if (!string.Equals(elt.Name.LocalName, k_TemplateNode, k_Comparison))
            {
                logger.LogError(ImportErrorType.Semantic,
                    ImportErrorCode.InvalidRootElement,
                    elt.Name,
                    Error.Level.Fatal,
                    elt);
                return;
            }

            foreach (var child in elt.Elements())
            {
                if (child.Name.LocalName != k_UsingNode)
                {
                    LoadXml(child, null, vta, ssb);
                    continue;
                }

                bool hasPath = false;
                string alias = null;
                string path = null;
                foreach (var xAttribute in child.Attributes())
                {
                    switch (xAttribute.Name.LocalName)
                    {
                        case k_UsingPathAttr:
                            hasPath = true;
                            path = xAttribute.Value;
                            break;
                        case k_UsingAliasAttr:
                            alias = xAttribute.Value;
                            if (alias == String.Empty)
                            {
                                logger.LogError(ImportErrorType.Semantic,
                                    ImportErrorCode.UsingHasEmptyAlias,
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

                if (!hasPath)
                {
                    logger.LogError(ImportErrorType.Semantic,
                        ImportErrorCode.MissingPathAttributeOnUsing,
                        null,
                        Error.Level.Fatal,
                        elt
                        );
                    continue;
                }

                if (String.IsNullOrEmpty(alias))
                    alias = Path.GetFileNameWithoutExtension(path);

                if (vta.AliasExists(alias))
                {
                    logger.LogError(ImportErrorType.Semantic,
                        ImportErrorCode.DuplicateUsingAlias,
                        alias,
                        Error.Level.Fatal,
                        elt
                        );
                    continue;
                }

                vta.RegisterUsing(alias, path);
            }
        }

        private static void LoadXml(XElement elt, VisualElementAsset parent, VisualTreeAsset vta, StyleSheetBuilder ssb)
        {
            VisualElementAsset vea;
            SerializedObject ser;
            Dictionary<string, string> serializedProperties;

            if (!ResolveType(elt, out vea, out ser, out serializedProperties, vta))
            {
                logger.LogError(ImportErrorType.Semantic, ImportErrorCode.UnknownElement, elt.Name.ToString(), Error.Level.Fatal, elt);
                return;
            }

            bool startedRule = ParseAttributes(elt, vea, ser, serializedProperties, ssb);

            var parentId = (parent == null ? 0 : parent.id);
            // id includes the parent id, meaning it's dependent on the whole direct hierarchy
            int id = (parentId << 1) ^ vea.GetHashCode();
            vea.parentId = parentId;
            vea.id = id;

            // each vea will creates 0 or 1 style rule, with one or more properties
            // they don't have selectors and are directly referenced by index
            // it's then applied during tree cloning
            vea.ruleIndex = startedRule ? ssb.EndRule() : -1;
            vta.visualElementAssets.Add(vea);

            if (elt.HasElements)
            {
                foreach (var child in elt.Elements())
                {
                    if (child != null)
                        LoadXml(child, vea, vta, ssb);
                }
            }
        }

        private static bool ResolveType(XElement elt, out VisualElementAsset vea, out SerializedObject ser, out Dictionary<string, string> serializedProperties, VisualTreeAsset visualTreeAsset)
        {
            Type t;

            if (visualTreeAsset.AliasExists(elt.Name.LocalName))
            {
                var tvea = ScriptableObject.CreateInstance<TemplateAsset>();
                tvea.templateAlias = elt.Name.LocalName;
                vea = tvea;
                serializedProperties = new Dictionary<string, string>(k_AttributeComparer);
                ser = new SerializedObject(vea);
                return true;
            }

            string fullName = String.IsNullOrEmpty(elt.Name.NamespaceName)
                ? elt.Name.LocalName
                : elt.Name.NamespaceName + "." + elt.Name.LocalName;

            if (elementTypes.TryGetValue(fullName, out t))
            {
                vea = (VisualElementAsset)ScriptableObject.CreateInstance(t);

                ser = new SerializedObject(vea);
                serializedProperties = GetAssetSerializedFields(ser);
                return true;
            }

            vea = null;
            serializedProperties = null;
            ser = null;
            return false;
        }

        private static bool ParseAttributes(XElement elt, VisualElementAsset res, SerializedObject properties, Dictionary<string, string> serializedProperties, StyleSheetBuilder ssb)
        {
            // underscore means "unnamed element but it would not look pretty in the project window without one"
            res.name = "_" + res.GetType().Name;

            bool startedRule = false;

            foreach (XAttribute xattr in elt.Attributes())
            {
                string attrName = xattr.Name.LocalName;

                // start with special cases
                switch (attrName)
                {
                    case "name":
                        res.name = xattr.Value;
                        continue;
                    case "text":
                        res.text = xattr.Value;
                        continue;
                    case "class":
                        res.classes = xattr.Value.Split(' ');
                        continue;
                    case "style":
                        ExCSS.StyleSheet parsed = new ExCSS.Parser().Parse("* { " + xattr.Value + " }");
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
                        ssb.BeginRule(-1);
                        startedRule = true;
                        StyleSheetImportErrors errors = new StyleSheetImportErrors();
                        foreach (Property prop in parsed.StyleRules[0].Declarations)
                        {
                            ssb.BeginProperty(prop.Name);
                            StyleSheetImporter.VisitValue(errors, ssb, prop.Term);
                            ssb.EndProperty();
                        }
                        // Don't call ssb.EndRule() here, it's done in LoadXml to get the rule index at the same time !
                        continue;
                }

                // then the matching VisualAsset
                if (ParseAssetSerializedFieldValue(res, properties, serializedProperties, attrName, xattr))
                    continue;

                // otherwise the attribute is unknown
                logger.LogError(ImportErrorType.Semantic,
                    ImportErrorCode.UnknownAttribute,
                    attrName,
                    Error.Level.Warning,
                    xattr);
            }
            return startedRule;
        }

        private static bool ParseValue(Type type, string attr, out object value)
        {
            bool success = false;
            value = null;

            if (type == typeof(int))
            {
                int f;
                success = int.TryParse(attr, out f);
                value = f;
            }
            else if (type == typeof(float))
            {
                float f;
                success = float.TryParse(attr, out f);
                value = f;
            }
            else if (type == typeof(string))
            {
                success = true;
                value = attr;
            }
            else if (type == typeof(Color))
            {
                Color32 c;
                if (ColorUtility.DoTryParseHtmlColor(attr, out c))
                {
                    value = (Color)c;
                    success = true;
                }
            }
            else if (type == typeof(Enum))
            {
                value = attr;
                success = true;
            }
            else if (type.IsEnum)
            {
                success = Enum.IsDefined(type, attr);
                if (success)
                    value = Enum.Parse(type, attr);
            }
            else if (type == typeof(bool))
            {
                bool b;
                success = bool.TryParse(attr, out b);
                value = b;
            }

            return success;
        }

        private static Dictionary<string, string> GetAssetSerializedFields(SerializedObject ser)
        {
            Dictionary<string, string> serializedProperties = new Dictionary<string, string>(k_AttributeComparer);

            SerializedProperty it = ser.GetIterator();
            bool enterchildren = true;
            while (it.NextVisible(enterchildren))
            {
                enterchildren = false;
                if (ShouldExcludeAssetSerializedField(it.name))
                    continue;
                string key = it.displayName.Replace(" ", String.Empty);
                key = Char.ToLower(key[0]) + key.Substring(1);
                serializedProperties[key] = it.name;
            }
            return serializedProperties;
        }

        private static bool ShouldExcludeAssetSerializedField(string fieldName)
        {
            if (s_ExcludedFields == null)
            {
                s_ExcludedFields = new HashSet<string>(k_Comparer)
                {
                    "m_Id",
                    "m_ParentId",
                    "m_RuleIndex",
                    "m_Text",
                    "m_Classes",
                };
            }
            return s_ExcludedFields.Contains(fieldName);
        }

        private static bool ParseAssetSerializedFieldValue(VisualElementAsset res, SerializedObject properties, Dictionary<string, string> serializedProperties, string attrName, XAttribute xattr)
        {
            string serPropName;
            if (!serializedProperties.TryGetValue(attrName, out serPropName))
                return false;

            SerializedProperty serProp = properties.FindProperty(serPropName);
            Type type;

            switch (serProp.propertyType)
            {
                case SerializedPropertyType.Integer:
                    type = typeof(int);
                    break;
                case SerializedPropertyType.Boolean:
                    type = typeof(bool);
                    break;
                case SerializedPropertyType.Float:
                    type = typeof(float);
                    break;
                case SerializedPropertyType.String:
                    type = typeof(string);
                    break;
                case SerializedPropertyType.Color:
                    type = typeof(Color);
                    break;
                case SerializedPropertyType.Enum:
                    type = typeof(Enum);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Type of property {0}.{1} is not supported", res.GetType().Name, serProp.name));
            }

            string attr = xattr.Value;
            object value;
            if (!ParseValue(type, attr, out value))
                return false;

            switch (serProp.propertyType)
            {
                case SerializedPropertyType.Integer:
                    serProp.intValue = (int)value;
                    break;
                case SerializedPropertyType.Boolean:
                    serProp.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Float:
                    serProp.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    serProp.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Color:
                    serProp.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.Enum:
                    int enumIndex = Array.FindIndex(serProp.enumNames, s => s == (string)value);
                    if (enumIndex == -1)
                    {
                        Debug.LogErrorFormat("Unknown enum value");
                        return false;
                    }
                    serProp.enumValueIndex = enumIndex;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Type of property {0}.{1} is not supported", res.GetType().Name, serProp.name));
            }

            serProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }

    internal enum ImportErrorCode
    {
        InvalidRootElement,
        DuplicateUsingAlias,
        UnknownElement,
        UnknownAttribute,
        InvalidXml,
        InvalidCssInStyleAttribute,
        MissingPathAttributeOnUsing,
        UsingHasEmptyAlias
    }

    internal enum ImportErrorType
    {
        Syntax,
        Semantic,
        Other,
        Internal,
    }

    static class XmlExtensions
    {
        public static string AttributeValue(this XElement elt, string attributeName)
        {
            var attr = elt.Attribute(attributeName);
            return attr == null ? null : attr.Value;
        }
    }
}
