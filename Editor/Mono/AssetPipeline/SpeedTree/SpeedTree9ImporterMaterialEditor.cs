// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.SpeedTree.Importer
{
    class SpeedTree9ImporterMaterialEditor : BaseSpeedTree9ImporterTabUI
    {
        private static class Styles
        {
            public static GUIContent RemapOptions = EditorGUIUtility.TrTextContent("On Demand Remap");
            public static GUIContent RemapMaterialsInProject = EditorGUIUtility.TrTextContent("Search and Remap...", "Click on this button to search and remap the materials from the project.");
            public static GUIContent ExternalMaterialMappings = EditorGUIUtility.TrTextContent("Remapped Materials", "External materials to use for each embedded material.");

            public static GUIContent Materials = EditorGUIUtility.TrTextContent("Materials");
            public static GUIContent ExtractEmbeddedMaterials = EditorGUIUtility.TrTextContent("Extract Materials...", "Click on this button to extract the embedded materials.");

            public static GUIContent InternalMaterialHelp = EditorGUIUtility.TrTextContent("Materials are embedded inside the imported asset.");
            public static GUIContent MaterialAssignmentsHelp = EditorGUIUtility.TrTextContent("Material assignments can be remapped below.");

            public static GUIContent ExternalMaterialSearchHelp = EditorGUIUtility.TrTextContent("Searches the user provided directory and matches the materials that share the same name and LOD with the originally imported material.");
            public static GUIContent SelectMaterialFolder = EditorGUIUtility.TrTextContent("Select Materials Folder");
        }

        private SpeedTree9Importer m_STImporter;
        private SerializedProperty m_ExternalObjects;

        private bool m_ShowMaterialRemapOptions;
        private bool m_HasEmbeddedMaterials;

        private bool ImporterHasEmbeddedMaterials
        {
            get
            {
                bool materialsValid = m_OutputImporterData.lodMaterials.materials.Count > 0
                    && m_OutputImporterData.lodMaterials.materials.TrueForAll(p => p.material != null);

                return m_OutputImporterData.hasEmbeddedMaterials && materialsValid;
            }
        }

        public SpeedTree9ImporterMaterialEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        { }

        internal override void OnEnable()
        {
            base.OnEnable();

            m_STImporter = target as SpeedTree9Importer;

            m_ExternalObjects = serializedObject.FindProperty("m_ExternalObjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ShowMaterialGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private bool HasEmbeddedMaterials()
        {
            if (m_OutputImporterData.materialsIdentifiers.Count == 0 || !ImporterHasEmbeddedMaterials)
                return false;

            // if the m_ExternalObjecs map has any unapplied changes, keep the state of the button as is
            if (m_ExternalObjects.serializedObject.hasModifiedProperties)
                return m_HasEmbeddedMaterials;

            m_HasEmbeddedMaterials = true;
            foreach (var t in m_ExternalObjects.serializedObject.targetObjects)
            {
                var externalObjectMap = m_STImporter.GetExternalObjectMap();
                var materialsList = m_OutputImporterData.materialsIdentifiers.ToArray();// m_STImporter.m_Materials.ToArray();

                int remappedMaterialCount = 0;
                foreach (var entry in externalObjectMap)
                {
                    bool isMatValid = Array.Exists(materialsList, x => x.name == entry.Key.name && entry.Value != null);
                    if (entry.Key.type == typeof(Material) && isMatValid)
                        ++remappedMaterialCount;
                }

                m_HasEmbeddedMaterials = m_HasEmbeddedMaterials && remappedMaterialCount != materialsList.Length;
            }
            return m_HasEmbeddedMaterials;
        }

        private void ShowMaterialGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            string materialHelp = string.Empty;
            int arraySize = m_OutputImporterData.materialsIdentifiers.Count;

            if (arraySize > 0 && HasEmbeddedMaterials())
            {
                // we're generating materials inside the prefab
                materialHelp = Styles.InternalMaterialHelp.text;
            }
            
            if (targets.Length == 1 && arraySize > 0)
            {
                materialHelp += " " + Styles.MaterialAssignmentsHelp.text;
            }

            if (ExtractMaterialsGUI())
            {
                // Necessary to avoid the error "BeginLayoutGroup must be called first".
                GUIUtility.ExitGUI();
                return;
            }
            
            if (!string.IsNullOrEmpty(materialHelp))
            {
                EditorGUILayout.HelpBox(materialHelp, MessageType.Info);
            }

            // The material remap list
            if (targets.Length == 1 && arraySize > 0)
            {
                GUILayout.Label(Styles.ExternalMaterialMappings, EditorStyles.boldLabel);

                if (MaterialRemapOptions())
                    return;

                // The list of material names is immutable, whereas the map of external objects can change based on user actions.
                // For each material name, map the external object associated with it.
                // The complexity comes from the fact that we may not have an external object in the map, so we can't make a property out of it
                for (int materialIdx = 0; materialIdx < arraySize; ++materialIdx)
                {
                    string name = m_OutputImporterData.materialsIdentifiers[materialIdx].name;
                    string type = m_OutputImporterData.materialsIdentifiers[materialIdx].type;

                    SerializedProperty materialProp = null;
                    Material material = null;
                    int propertyIdx = 0;

                    for (int externalObjectIdx = 0, count = m_ExternalObjects.arraySize; externalObjectIdx < count; ++externalObjectIdx)
                    {
                        SerializedProperty pair = m_ExternalObjects.GetArrayElementAtIndex(externalObjectIdx);
                        string externalName = pair.FindPropertyRelative("first.name").stringValue;
                        string externalType = pair.FindPropertyRelative("first.type").stringValue;

                        // Cannot do a strict comparison, since externalType is set to "UnityEngine:Material" (C++)
                        // and type "UnityEngine.Material" (C#).
                        bool typeMatching = externalType.Contains("Material") && type.Contains("Material");

                        if (externalName == name && typeMatching)
                        {
                            materialProp = pair.FindPropertyRelative("second");
                            material = materialProp != null ? materialProp.objectReferenceValue as Material : null;

                            // If 'material' is null, it's likely because it was deleted. So we assign null to 'materialProp'
                            // to avoid the 'missing material' reference error in the UI.
                            materialProp = (material != null) ? pair.FindPropertyRelative("second") : null;
                            propertyIdx = externalObjectIdx;
                            break;
                        }
                    }

                    GUIContent nameLabel = EditorGUIUtility.TextContent(name);
                    nameLabel.tooltip = name;
                    if (materialProp != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.ObjectField(materialProp, typeof(Material), nameLabel);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (materialProp.objectReferenceValue == null)
                            {
                                m_ExternalObjects.DeleteArrayElementAtIndex(propertyIdx);
                            }
                        }
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        material = EditorGUILayout.ObjectField(nameLabel, material, typeof(Material), false) as Material;
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (material != null)
                            {
                                int newIndex = m_ExternalObjects.arraySize++;
                                SerializedProperty pair = m_ExternalObjects.GetArrayElementAtIndex(newIndex);
                                pair.FindPropertyRelative("first.name").stringValue = name;
                                pair.FindPropertyRelative("first.type").stringValue = type;
                                pair.FindPropertyRelative("second").objectReferenceValue = material;
                            }
                        }
                    }
                }
            }
        }

        private bool ExtractMaterialsGUI()
        {
            bool buttonPressed = false;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(Styles.Materials);
                using (new EditorGUI.DisabledScope(!HasEmbeddedMaterials()))
                {
                    buttonPressed = GUILayout.Button(Styles.ExtractEmbeddedMaterials);

                    if (buttonPressed)
                    {
                        // use the first target for selecting the destination folder, but apply that path for all targets
                        string destinationPath = m_STImporter.assetPath;
                        destinationPath = EditorUtility.SaveFolderPanel(Styles.SelectMaterialFolder.text,
                            FileUtil.DeleteLastPathNameComponent(destinationPath), "");
                        if (string.IsNullOrEmpty(destinationPath))
                        {
                            // Cancel the extraction if the user did not select a folder.
                            EditorGUILayout.EndHorizontal();
                            return buttonPressed;
                        }
                        destinationPath = FileUtil.GetProjectRelativePath(destinationPath);

                        try
                        {
                            // batch the extraction of the materials
                            AssetDatabase.StartAssetEditing();

                            PrefabUtility.ExtractMaterialsFromAsset(targets, destinationPath);
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // AssetDatabase.StopAssetEditing() invokes OnEnable(), which invalidates all the serialized properties, so we must return.
            return buttonPressed;
        }

        private bool MaterialRemapOptions()
        {
            bool buttonPressed = false;

            m_ShowMaterialRemapOptions = EditorGUILayout.Foldout(m_ShowMaterialRemapOptions, Styles.RemapOptions);
            if (m_ShowMaterialRemapOptions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(Styles.ExternalMaterialSearchHelp.text, MessageType.Info);

                EditorGUI.indentLevel--;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(assetTarget == null))
                    {
                        buttonPressed = GUILayout.Button(Styles.RemapMaterialsInProject);
                        if (buttonPressed)
                        {
                            bool bStartedAssetEditing = false;
                            try
                            {
                                foreach (var t in targets)
                                {
                                    string folderToSearch = m_STImporter.GetMaterialFolderPath();
                                    folderToSearch = EditorUtility.OpenFolderPanel(Styles.SelectMaterialFolder.text, folderToSearch, "");

                                    bool bUserSelectedAFolder = folderToSearch != ""; // folderToSearch is empty if the user cancels the window
                                    if (bUserSelectedAFolder)
                                    {
                                        string projectRelativePath = FileUtil.GetProjectRelativePath(folderToSearch);
                                        bool bRelativePathIsNotEmpty = projectRelativePath != "";
                                        if (bRelativePathIsNotEmpty)
                                        {
                                            AssetDatabase.StartAssetEditing();
                                            bStartedAssetEditing = true;
                                            m_STImporter.SearchAndRemapMaterials(projectRelativePath);

                                            AssetDatabase.WriteImportSettingsIfDirty(m_STImporter.assetPath);
                                            AssetDatabase.ImportAsset(m_STImporter.assetPath, ImportAssetOptions.ForceUpdate);
                                        }
                                        else
                                        {
                                            Debug.LogWarning("Selected folder is outside of the project's folder hierarchy, please provide a folder from the project.\n");
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (bStartedAssetEditing)
                                {
                                    AssetDatabase.StopAssetEditing();
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.Space();
            }

            return buttonPressed;
        }
    }
}
