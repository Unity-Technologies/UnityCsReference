// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.AssetImporters;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor
{
    [CustomEditor(typeof(AssemblyDefinitionReferenceImporter))]
    [CanEditMultipleObjects]
    internal class AssemblyDefinitionReferenceImporterInspector : AssetImporterEditor
    {
        internal class Styles
        {
            public static readonly GUIContent assemblyDefinition = EditorGUIUtility.TrTextContent("Assembly Definition");
            public static readonly GUIContent loadError = EditorGUIUtility.TrTextContent("Load error");
            public static readonly GUIContent useGUID = EditorGUIUtility.TrTextContent("Use GUID", "Use the Assembly Definition asset GUID instead of name for referencing. Allows the referenced assembly to be renamed without having to update references.");
        }

        GUIStyle m_TextStyle;

        class AssemblyDefinitionReferenceState : ScriptableObject
        {
            public string path
            {
                get { return AssetDatabase.GetAssetPath(asset); }
            }

            public AssemblyDefinitionReferenceAsset asset;
            public AssemblyDefinitionImporterInspector.AssemblyDefinitionReference reference;
            public bool useGUIDs;
        }

        SerializedProperty m_Reference;
        SerializedProperty m_ReferenceName;
        SerializedProperty m_ReferenceAsset;
        SerializedProperty m_UseGUIDs;

        Exception extraDataInitializeException;

        public override bool showImportedObject { get { return false; } }

        public override void OnEnable()
        {
            base.OnEnable();

            //Ensure UIElements handles the IMGUI container with margins
            alwaysAllowExpansion = true;

            m_Reference = extraDataSerializedObject.FindProperty("reference");
            m_ReferenceName = m_Reference.FindPropertyRelative("name");
            m_ReferenceAsset = m_Reference.FindPropertyRelative("asset");
            m_UseGUIDs = extraDataSerializedObject.FindProperty("useGUIDs");
        }

        public override void OnInspectorGUI()
        {
            if (extraDataInitializeException != null)
            {
                ShowLoadErrorExceptionGUI(extraDataInitializeException);
                ApplyRevertGUI();
                return;
            }

            extraDataSerializedObject.Update();

            EditorGUILayout.PropertyField(m_UseGUIDs, Styles.useGUID);

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(Styles.assemblyDefinition, m_ReferenceAsset.objectReferenceValue, typeof(AssemblyDefinitionAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                m_ReferenceAsset.objectReferenceValue = obj;
                if (m_ReferenceAsset.objectReferenceValue != null)
                {
                    var data = CustomScriptAssemblyData.FromJson(((AssemblyDefinitionAsset)obj).text);
                    m_ReferenceName.stringValue = data.name;
                }
                else
                {
                    m_ReferenceName.stringValue = string.Empty;
                }
            }

            extraDataSerializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();
            SaveAndUpdateAssemblyDefinitionReferenceStates(extraDataTargets.Cast<AssemblyDefinitionReferenceState>().ToArray());
        }

        void ShowLoadErrorExceptionGUI(Exception e)
        {
            if (m_TextStyle == null)
                m_TextStyle = "ScriptText";

            GUILayout.Label(Styles.loadError, EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(e.Message), m_TextStyle);
            EditorGUI.HelpBox(rect, e.Message, MessageType.Error);
        }

        protected override Type extraDataType => typeof(AssemblyDefinitionReferenceState);

        protected override void InitializeExtraDataInstance(UnityEngine.Object extraData, int targetIndex)
        {
            try
            {
                LoadAssemblyDefinitionReferenceState((AssemblyDefinitionReferenceState)extraData, ((AssetImporter)targets[targetIndex]).assetPath);
                extraDataInitializeException = null;
            }
            catch (Exception e)
            {
                extraDataInitializeException = e;
            }
        }

        [MenuItem("CONTEXT/AssemblyDefinitionReferenceImporter/Reset")]
        internal static void ContextReset(MenuCommand command)
        {
            var templatePath = AssetsMenuUtility.GetScriptTemplatePath(ScriptTemplate.AsmRef_NewAssemblyReference);
            Debug.Assert(!string.IsNullOrEmpty(templatePath));

            var templateContent = File.ReadAllText(templatePath);
            var importer = command.context as AssemblyDefinitionReferenceImporter;

            if (importer != null)
            {
                var assetPath = importer.assetPath;
                templateContent = ProjectWindowUtil.PreprocessScriptAssetTemplate(assetPath, templateContent);
                File.WriteAllText(assetPath, templateContent);
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        static void LoadAssemblyDefinitionReferenceState(AssemblyDefinitionReferenceState state, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionReferenceAsset>(path);
            if (asset == null)
                return;

            var data = CustomScriptAssemblyReferenceData.FromJson(asset.text);
            if (data == null)
                return;

            state.asset = asset;
            state.useGUIDs = string.IsNullOrEmpty(data.reference) || CompilationPipeline.GetAssemblyDefinitionReferenceType(data.reference) == AssemblyDefinitionReferenceType.Guid;
            state.reference = new AssemblyDefinitionImporterInspector.AssemblyDefinitionReference { name = data.reference, serializedReference = data.reference };
            state.reference.Load(data.reference, state.useGUIDs);
        }

        static void SaveAndUpdateAssemblyDefinitionReferenceStates(AssemblyDefinitionReferenceState[] states)
        {
            foreach (var state in states)
            {
                SaveAndUpdateAssemblyDefinitionReferenceState(state);
            }
        }

        static void SaveAndUpdateAssemblyDefinitionReferenceState(AssemblyDefinitionReferenceState state)
        {
            CustomScriptAssemblyReferenceData data = new CustomScriptAssemblyReferenceData();
            if (state.asset != null)
            {
                data.reference = state.useGUIDs ? CompilationPipeline.GUIDToAssemblyDefinitionReferenceGUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(state.reference.asset))) : state.reference.name;
            }

            var json = CustomScriptAssemblyReferenceData.ToJson(data);
            File.WriteAllText(state.path, json);
            AssetDatabase.ImportAsset(state.path);
        }
    }
}
