// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(MonoImporter))]
    internal class MonoScriptImporterInspector : AssetImporterEditor
    {
        const int m_RowHeight = 16;

        private SerializedObject m_TargetObject;
        private SerializedProperty m_Icon;

        internal override void OnHeaderControlsGUI()
        {
            TextAsset textAsset = assetEditor.target as TextAsset;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open...", EditorStyles.miniButton))
            {
                AssetDatabase.OpenAsset(textAsset);
                GUIUtility.ExitGUI();
            }

            if (textAsset as MonoScript)
            {
                if (GUILayout.Button("Execution Order...", EditorStyles.miniButton))//GUILayout.Width(150)))
                {
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings/Script Execution Order");
                    GUIUtility.ExitGUI();
                }
            }
        }

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            if (m_Icon == null)
            {
                m_TargetObject = new SerializedObject(assetEditor.targets);
                m_Icon = m_TargetObject.FindProperty("m_Icon");
            }
            EditorGUI.ObjectIconDropDown(iconRect, assetEditor.targets, true, null, m_Icon);
        }

        // Clear default references
        [MenuItem("CONTEXT/MonoImporter/Reset")]
        static void ResetDefaultReferences(MenuCommand command)
        {
            MonoImporter importer = command.context as MonoImporter;
            importer.SetDefaultReferences(new string[0], new UnityEngine.Object[0]);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(importer));
        }

        static bool IsTypeCompatible(Type type)
        {
            if (type == null || !(type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject))))
                return false;
            return true;
        }

        void ShowFieldInfo(Type type, MonoImporter importer, List<string> names, List<UnityEngine.Object> objects, ref bool didModify)
        {
            // Only show default properties for types that support it (so far only MonoBehaviour derived types)
            if (!IsTypeCompatible(type))
                return;

            ShowFieldInfo(type.BaseType, importer, names, objects, ref didModify);

            FieldInfo[] infos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in infos)
            {
                if (!field.IsPublic)
                {
                    object[] attr = field.GetCustomAttributes(typeof(SerializeField), true);
                    if (attr == null || attr.Length == 0)
                        continue;
                }

                if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object))
                {
                    UnityEngine.Object oldTarget = importer.GetDefaultReference(field.Name);
                    UnityEngine.Object newTarget = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), oldTarget, field.FieldType, false);

                    names.Add(field.Name);
                    objects.Add(newTarget);

                    if (oldTarget != newTarget)
                        didModify = true;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var importer = target as MonoImporter;
            var script = importer.GetScript();

            if (script)
            {
                Type type = script.GetClass();

                // Ignore Editor scripts
                if (!InternalEditorUtility.IsInEditorFolder(importer.assetPath))
                {
                    if (!IsTypeCompatible(type))
                    {
                        EditorGUILayout.HelpBox(
                            "No MonoBehaviour scripts in the file, or their names do not match the file name.",
                            MessageType.Info);
                    }
                }

                var names = new List<string>();
                var objects = new List<UnityEngine.Object>();
                var didModify = false;

                // Make default reference fields show small icons
                using (new EditorGUIUtility.IconSizeScope(new Vector2(m_RowHeight, m_RowHeight)))
                {
                    ShowFieldInfo(type, importer, names, objects, ref didModify);
                }

                if (0 != objects.Count)
                    EditorGUILayout.HelpBox("Default references will only be applied in edit mode.", MessageType.Info);

                if (didModify)
                {
                    importer.SetDefaultReferences(names.ToArray(), objects.ToArray());
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(importer));
                }
            }
        }
    }

    [CustomEditor(typeof(TextAsset))]
    [CanEditMultipleObjects]
    internal class TextAssetInspector : Editor
    {
        private const int kMaxChars = 7000;
        [NonSerialized]
        private GUIStyle m_TextStyle;

        public override void OnInspectorGUI()
        {
            if (m_TextStyle == null)
                m_TextStyle = "ScriptText";

            bool enabledTemp = GUI.enabled;
            GUI.enabled = true;
            TextAsset textAsset = target as TextAsset;
            if (textAsset != null)
            {
                string text;
                if (targets.Length > 1)
                {
                    text = targetTitle;
                }
                else
                {
                    text = textAsset.ToString();
                    if (text.Length > kMaxChars)
                        text = text.Substring(0, kMaxChars) + "...\n\n<...etc...>";
                }
                Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(text), m_TextStyle);
                rect.x = 0;
                rect.y -= 3;
                rect.width = GUIClip.visibleRect.width + 1;
                GUI.Box(rect, text, m_TextStyle);
            }
            GUI.enabled = enabledTemp;
        }
    }


    [CustomEditor(typeof(MonoScript))]
    [CanEditMultipleObjects]
    internal class MonoScriptInspector : TextAssetInspector
    {
        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                var assetPath = AssetDatabase.GetAssetPath(target);
                var assemblyName = Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath);

                // assemblyName is null for MonoScript's inside assemblies.
                if (assemblyName != null)
                {
                    GUILayout.Label("Assembly Information", EditorStyles.boldLabel);

                    EditorGUILayout.LabelField("Filename", assemblyName);

                    var assemblyDefinitionFile = Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(assetPath);

                    if (assemblyDefinitionFile != null)
                    {
                        var assemblyDefintionFileAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assemblyDefinitionFile);

                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.ObjectField("Definition File", assemblyDefintionFileAsset, typeof(TextAsset), false);
                        }
                    }

                    EditorGUILayout.Space();
                }
            }

            base.OnInspectorGUI();
        }
    }
}
