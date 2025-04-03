// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

                // If the path points to a file, asset should be created in the directory that file is in
                if (File.Exists(filePath))
                {
                    filePath = Path.GetDirectoryName(filePath);
                }
            }

            return filePath;
        }

        [MenuItem("Assets/Create/UI Toolkit/UI Document", false, 610, false)]
        private static void CreateUXMLAsset()
        {
            var folder = GetCurrentFolder();
            var contents = CreateUXMLTemplate(folder);
            var icon = EditorGUIUtility.IconContent<VisualTreeAsset>().image as Texture2D;
            ProjectWindowUtil.CreateAssetWithContent("NewUXMLTemplate.uxml", contents, icon);
        }

        public static string CreateUXMLTemplate(string folder, string uxmlContent = "")
        {
            if (!Directory.Exists(UxmlSchemaGenerator.k_SchemaFolder))
                UxmlSchemaGenerator.UpdateSchemaFiles(true);

            var pathComponents = folder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var backDots = new List<string>();
            foreach (var s in pathComponents)
            {
                switch (s)
                {
                    case ".":
                        continue;
                    case ".." when backDots.Count > 0:
                        backDots.RemoveAt(backDots.Count - 1);
                        break;
                    default:
                        backDots.Add("..");
                        break;
                }
            }
            backDots.Add(UxmlSchemaGenerator.k_SchemaFolder);
            var schemaDirectory = string.Join("/", backDots.ToArray());

            var uxmlTemplate = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<ui:{0}
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns:ui=""UnityEngine.UIElements""
    xmlns:uie=""UnityEditor.UIElements""
    xsi:noNamespaceSchemaLocation=""{1}/UIElements.xsd""
>
    {2}
</ui:{0}>", UXMLImporterImpl.k_RootNode, schemaDirectory, uxmlContent);

            return uxmlTemplate;
        }
    }
}
