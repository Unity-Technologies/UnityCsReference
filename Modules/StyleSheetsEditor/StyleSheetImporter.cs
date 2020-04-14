// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.StyleSheets
{
    // Make sure style sheets importer after allowed dependent assets: textures, fonts and json
    // Has to be higher then AssetImportOrder.kImportOrderLate
    [ScriptedImporter(version: 10, ext: "uss", importQueueOffset: 1100)]
    [ExcludeFromPreset]
    class StyleSheetImporter : ScriptedImporter
    {
        #pragma warning disable 649
        public bool disableValidation;
        internal bool isWhitelisted;
        #pragma warning restore 649

        private static readonly List<string> s_ValidationPathWhitelist = new List<string>()
        {
            "Packages/com.unity.shadergraph"
        };

        public override void OnImportAsset(AssetImportContext ctx)
        {
            isWhitelisted = IsWhiteListed(ctx);
            string contents = string.Empty;

            try
            {
                contents = File.ReadAllText(ctx.assetPath);
            }
            catch (IOException exc)
            {
                ctx.LogImportError($"IOException : {exc.Message}");
            }
            finally
            {
                StyleSheet asset = ScriptableObject.CreateInstance<StyleSheet>();
                asset.hideFlags = HideFlags.NotEditable;

                if (!string.IsNullOrEmpty(contents))
                {
                    var importer = new StyleSheetImporterImpl(ctx);
                    importer.disableValidation = disableValidation | isWhitelisted;
                    importer.Import(asset, contents);
                }

                // make sure to produce a style sheet object in all cases
                ctx.AddObjectToAsset("stylesheet", asset);
                ctx.SetMainObject(asset);
            }
        }

        internal bool IsWhiteListed(AssetImportContext ctx)
        {
            if (!disableValidation)
                foreach (var path in s_ValidationPathWhitelist)
                    if (ctx.assetPath.StartsWith(path))
                        return true;
            return false;
        }
    }

    [CustomEditor(typeof(StyleSheetImporter))]
    [CanEditMultipleObjects]
    class StyleSheetImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty m_DisableValidation;

        public override void OnEnable()
        {
            base.OnEnable();

            m_DisableValidation = serializedObject.FindProperty("disableValidation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DisableValidation);

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
