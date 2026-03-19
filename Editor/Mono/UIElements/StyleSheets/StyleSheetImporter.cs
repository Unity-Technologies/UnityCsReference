// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.UIElements.StyleSheets
{
    // Make sure style sheets importer after allowed dependent assets: textures, fonts and json
    // Has to be higher then AssetImportOrder.kImportOrderLate
    [HelpURL("UIE-USS")]
    [ScriptedImporter(version: 19, ext: "uss", importQueueOffset: 1100)]
    [ExcludeFromPreset]
    class StyleSheetImporter : ScriptedImporter
    {
        public enum ErrorHandling
        {
            Error,
            Warning,
            Ignore
        }

        #pragma warning disable 649
        public bool disableValidation;
        internal bool isWhitelisted;

        [Tooltip("Unsupported selectors are those that are not currently recognized by USS but may be supported in future versions. If you are using USS across multiple versions, you may choose to ignore errors in versions that do not support these selectors.")]
        public ErrorHandling unsupportedSelectorAction;
        #pragma warning restore 649

        private static readonly List<string> s_ValidationPathWhitelist = new List<string>()
        {
            "Packages/com.unity.shadergraph"
        };

        static string[] GatherDependenciesFromSourceFile(string assetPath)
        {
            var contents = File.ReadAllText(FileUtil.PathToAbsolutePath(assetPath));
            if (string.IsNullOrEmpty(contents))
            {
                return Array.Empty<string>();
            }

            try
            {
                return StyleSheetImporterImpl.PopulateDependencies(assetPath);
            }
            catch (Exception)
            {
                // We want to be silent here, all USS syntax errors will be reported during the actual import.
                return Array.Empty<string>();
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            isWhitelisted = IsWhiteListed(ctx);
            string contents = string.Empty;

            try
            {
                contents = File.ReadAllText(FileUtil.PathToAbsolutePath(ctx.assetPath));
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
                    importer.m_Errors.unsupportedSelectorAction = unsupportedSelectorAction;
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
        SerializedProperty m_UnsupportedSelectorAction;

        public override void OnEnable()
        {
            base.OnEnable();

            m_DisableValidation = serializedObject.FindProperty("disableValidation");
            m_UnsupportedSelectorAction = serializedObject.FindProperty("unsupportedSelectorAction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DisableValidation);
            EditorGUILayout.PropertyField(m_UnsupportedSelectorAction);

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
