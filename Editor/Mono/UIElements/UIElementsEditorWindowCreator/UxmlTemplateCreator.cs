// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;

static partial class UIElementsTemplate
{
    public static string CreateUXMLTemplate(string folder)
    {
        UxmlSchemaGenerator.UpdateSchemaFiles();

        string[] pathComponents = folder.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
        Boo.Lang.List<string> backDots = new Boo.Lang.List<string>();
        foreach (var s in pathComponents)
        {
            if (s == ".")
            {
                continue;
            }
            if (s == ".." && backDots.Count > 0)
            {
                backDots.RemoveAt(backDots.Count - 1);
            }
            else
            {
                backDots.Add("..");
            }
        }
        backDots.Add(UxmlSchemaGenerator.k_SchemaFolder);
        string schemaDirectory = string.Join("/", backDots.ToArray());

        string xmlnsList = String.Empty;
        string schemaLocationList = String.Empty;
        Dictionary<string, string> namespacePrefix = UxmlSchemaGenerator.GetNamespacePrefixDictionary();

        foreach (var prefix in namespacePrefix)
        {
            if (prefix.Key == String.Empty)
                continue;

            if (prefix.Value != String.Empty)
            {
                xmlnsList += "    xmlns:" + prefix.Value + "=\"" + prefix.Key + "\"\n";
            }
            schemaLocationList += "                        " + prefix.Key + " " + schemaDirectory + "/" +
                UxmlSchemaGenerator.GetFileNameForNamespace(prefix.Key) + "\n";
        }

        // The noNamespaceSchemaLocation attribute should be sufficient to reference all namespaces
        // but Rider does not support it very well, so we add schemaLocation to make it happy.
        string uxmlTemplate = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<engine:{0}
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
{1}
xsi:noNamespaceSchemaLocation=""{2}/UIElements.xsd""
xsi:schemaLocation=""
{3}""
>
    <engine:Label text=""Hello World! From UXML"" />
</engine:{0}>", UXMLImporterImpl.k_RootNode, xmlnsList, schemaDirectory, schemaLocationList);

        return uxmlTemplate;
    }
}
