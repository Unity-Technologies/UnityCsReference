using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.StyleSheets
{
    [CustomEditor(typeof(ThemeStyleSheetImporter))]
    class ThemeStyleSheetImporterEditor : ScriptedImporterEditor
    {
        ReorderableList m_ThemeStyleSheetsList;
        ReorderableList m_InheritedThemesThemeList;

        SerializedProperty m_StyleSheetsProperty;
        SerializedProperty m_InheritedThemesProperty;

        public override void OnEnable()
        {
            base.OnEnable();

            m_StyleSheetsProperty = extraDataSerializedObject.FindProperty("StyleSheets");
            m_InheritedThemesProperty = extraDataSerializedObject.FindProperty("InheritedThemes");

            m_ThemeStyleSheetsList = MakeReorderableList(m_StyleSheetsProperty, "Style Sheets");
            m_InheritedThemesThemeList = MakeReorderableList(m_InheritedThemesProperty, "Inherited Themes");
        }

        ReorderableList MakeReorderableList(SerializedProperty serializedProperty, string header)
        {
            var reorderableList = new ReorderableList(extraDataSerializedObject,
                serializedProperty,
                true, true, true, true);

            reorderableList.drawElementCallback += (rect, index, active, focused) =>
            {
                DrawStyleSheetElement(rect, index, serializedProperty);
            };

            reorderableList.onAddCallback += list =>
            {
                list.serializedProperty.arraySize += 1;
                var element = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                element.objectReferenceValue = null;
            };

            reorderableList.drawHeaderCallback += rect =>
            {
                EditorGUI.LabelField(rect, header);
            };

            return reorderableList;
        }

        void DrawStyleSheetElement(Rect rect, int index, SerializedProperty property)
        {
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            var element = property.GetArrayElementAtIndex(index);
            var label = element.objectReferenceValue == null ? L10n.Tr("(Missing Reference)") : ObjectNames.NicifyVariableName(element.objectReferenceValue.name);
            EditorGUI.ObjectField(rect, element, GUIContent.Temp(label));
        }

        protected override void Apply()
        {
            var stringBuilder = new StringBuilder();
            var state = extraDataTarget as ThemeAssetDefinitionState;
            var targetPath = AssetDatabase.GetAssetPath(target);
            AddStyleSheets(stringBuilder, state.InheritedThemes.Cast<StyleSheet>());
            stringBuilder.AppendLine();
            AddStyleSheets(stringBuilder, state.StyleSheets);

            var rulesContent = GetThemeRulesContent(targetPath);
            if (!string.IsNullOrEmpty(rulesContent))
            {
                stringBuilder.Append(rulesContent);
            }

            File.WriteAllText(targetPath, stringBuilder.ToString());
        }

        public string GetThemeRulesContent(string targetPath)
        {
            var stringBuilder = new StringBuilder();
            foreach (var line in  File.ReadAllLines(targetPath))
            {
                if (!line.StartsWith("@import"))
                    stringBuilder.AppendLine(line);
            }

            return stringBuilder.ToString();
        }

        void AddStyleSheets(StringBuilder stringBuilder, IEnumerable<StyleSheet> styleSheets)
        {
            var targetPath = AssetDatabase.GetAssetPath(target);
            var targetDirectory = Path.GetDirectoryName(targetPath);
            targetDirectory = targetDirectory.Replace('\\', '/') + "/";

            foreach (var styleSheet in styleSheets)
            {
                var styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
                if (string.IsNullOrEmpty(styleSheetPath))
                    continue;

                string url;
                if (styleSheetPath.StartsWith(targetDirectory))
                    url = styleSheetPath.Remove(0, targetDirectory.Length);
                else
                    url = $"/{styleSheetPath}";

                stringBuilder.AppendLine($"@import url(\"{url}\");");
            }
        }

        public override void OnInspectorGUI()
        {
            extraDataSerializedObject.Update();
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(assetTarget.name), EditorStyles.largeLabel);
            EditorGUILayout.Space();
            m_InheritedThemesThemeList.DoLayoutList();

            EditorGUILayout.Space();
            m_ThemeStyleSheetsList.DoLayoutList();

            extraDataSerializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        protected override Type extraDataType => typeof(ThemeAssetDefinitionState);
        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            LoadThemeAssetDefinitionState((ThemeAssetDefinitionState)extraData, ((AssetImporter)targets[targetIndex]).assetPath);
        }

        static void LoadThemeAssetDefinitionState(ThemeAssetDefinitionState extraData, string assetPath)
        {
            extraData.StyleSheets = new List<StyleSheet>();
            extraData.InheritedThemes = new List<ThemeStyleSheet>();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);

            foreach (var importStruct in styleSheet.imports)
            {
                if (importStruct.styleSheet is ThemeStyleSheet themeStyleSheet)
                    extraData.InheritedThemes.Add(themeStyleSheet);
                else
                    extraData.StyleSheets.Add(importStruct.styleSheet);
            }
        }
    }
}
