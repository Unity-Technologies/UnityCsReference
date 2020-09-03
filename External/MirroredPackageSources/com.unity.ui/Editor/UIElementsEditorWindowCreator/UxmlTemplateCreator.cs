using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static partial class UIElementsTemplate
    {
        private static string GetCurrentFolder()
        {
            string filePath;
            if (Selection.assetGUIDs.Length == 0)
            {
                // No asset selected.
                filePath = "Assets";
            }
            else
            {
                // Get the path of the selected folder or asset.
                filePath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

                // Get the file extension of the selected asset as it might need to be removed.
                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != "")
                {
                    filePath = Path.GetDirectoryName(filePath);
                }
            }

            return filePath;
        }

        [MenuItem("Assets/Create/UI Toolkit/UI Document", false, 610, false)]
        private static void CreateUXMAsset()
        {
            var folder = GetCurrentFolder();
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewUXMLTemplate.uxml");
            var contents = CreateUXMLTemplate(folder);
            var icon = EditorGUIUtility.IconContent<VisualTreeAsset>().image as Texture2D;
            ProjectWindowUtil.CreateAssetWithContent(path, contents, icon);
        }

        public static string CreateUXMLTemplate(string folder, string uxmlContent = "")
        {
            UxmlSchemaGenerator.UpdateSchemaFiles();

            string[] pathComponents = folder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> backDots = new List<string>();
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
            Dictionary<string, string> namespacePrefix = UxmlSchemaGenerator.GetNamespacePrefixDictionary();

            foreach (var prefix in namespacePrefix)
            {
                if (prefix.Key == String.Empty)
                    continue;

                if (prefix.Value != String.Empty)
                {
                    xmlnsList += "    xmlns:" + prefix.Value + "=\"" + prefix.Key + "\"\n";
                }
            }

            string uxmlTemplate = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<engine:{0}
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
{1}    xsi:noNamespaceSchemaLocation=""{2}/UIElements.xsd""
>
    {3}
</engine:{0}>", UXMLImporterImpl.k_RootNode, xmlnsList, schemaDirectory, uxmlContent);

            return uxmlTemplate;
        }
    }
}
