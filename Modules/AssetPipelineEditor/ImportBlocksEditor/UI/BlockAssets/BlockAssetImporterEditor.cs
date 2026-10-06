// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    [UnityEngine.Internal.ExcludeFromDocs]
    [CustomEditor(typeof(BlockAssetImporter<,>), true)]
    [CanEditMultipleObjects]
    public class BlockAssetImporterEditor : ScriptedImporterEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var styleSheet = UiHelpers.GetBlockItemStyleSheet();
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            var title = new Label(GetTitleLabel());
            title.AddToClassList("block-asset-title");
            root.Add(title);

            var blocksField = new PropertyField(serializedObject.FindProperty("blockCollection"));
            root.Add(blocksField);

            var applyRevertGUI = new IMGUIContainer(ApplyRevertGUI);
            root.Add(applyRevertGUI);

            return root;
        }

        // Falls back to the type name when the asset path is unavailable (multi-edit, in-memory tests).
        string GetTitleLabel()
        {
            var importer = target as AssetImporter;
            var path = importer != null ? importer.assetPath : null;
            if (!string.IsNullOrEmpty(path))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(name))
                    return ObjectNames.NicifyVariableName(name);
            }
            return GetPrettyTypeName(target.GetType());
        }

        static string GetPrettyTypeName(Type type)
        {
            string typeName = type.Name;
            int backtickIndex = typeName.IndexOf('`');
            if (backtickIndex != -1)
            {
                typeName = typeName.Substring(0, backtickIndex);
            }

            if (typeName.EndsWith("Importer", StringComparison.OrdinalIgnoreCase))
            {
                typeName = typeName.Substring(0, typeName.Length - "Importer".Length);
            }

            return ObjectNames.NicifyVariableName(typeName);
        }
    }
}
