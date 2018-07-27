// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor
{
    internal class SpeedTreeImporterMaterialEditor : BaseSpeedTreeImporterTabUI
    {
        private static class Styles
        {
            public static GUIContent MaterialLocation = EditorGUIUtility.TrTextContent("Location");
            public static GUIContent[] MaterialLocationOpt =
            {
                EditorGUIUtility.TrTextContent("Use External Materials (Legacy)", "Use external materials if found in the project."),
                EditorGUIUtility.TrTextContent("Use Embedded Materials", "Embed the material inside the imported asset.")
            };

            public static GUIContent RemapOptions = EditorGUIUtility.TrTextContent("On Demand Remap");
            public static GUIContent RemapMaterialsInProject = EditorGUIUtility.TrTextContent("Search and Remap...", "Click on this button to search and remap the materials from the project.");

            public static GUIContent ExternalMaterialMappings = EditorGUIUtility.TrTextContent("Remapped Materials", "External materials to use for each embedded material.");
            public static GUIContent NoMaterialMappingsHelp = EditorGUIUtility.TrTextContent("Re-import the asset to see the list of used materials.");

            public static GUIContent Materials = EditorGUIUtility.TrTextContent("Materials");
            public static GUIContent ExtractEmbeddedMaterials = EditorGUIUtility.TrTextContent("Extract Materials...", "Click on this button to extract the embedded materials.");

            public static GUIContent InternalMaterialHelp = EditorGUIUtility.TrTextContent("Materials are embedded inside the imported asset.");
            public static GUIContent MaterialAssignmentsHelp = EditorGUIUtility.TrTextContent("Material assignments can be remapped below.");

            public static GUIContent ExternalMaterialSearchHelp = EditorGUIUtility.TrTextContent("Remap the imported materials to materials from the Unity project.");

            public static GUIContent SelectMaterialFolder = EditorGUIUtility.TrTextContent("Select Materials Folder");
        }

        private SerializedProperty m_MaterialLocation;
        private SerializedProperty m_Materials;
        private SerializedProperty m_ExternalObjects;

        private SerializedProperty m_SupportsEmbeddedMaterials;

        private bool m_ShowMaterialRemapOptions = false;
        private bool m_HasEmbeddedMaterials = false;

        public SpeedTreeImporterMaterialEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        private bool HasEmbeddedMaterials()
        {
            if (m_Materials.arraySize == 0)
                return false;

            // if the m_ExternalObjecs map has any unapplied changes, keep the state of the button as is
            if (m_ExternalObjects.serializedObject.hasModifiedProperties)
                return m_HasEmbeddedMaterials;

            m_HasEmbeddedMaterials = true;
            foreach (var t in m_ExternalObjects.serializedObject.targetObjects)
            {
                var importer = t as SpeedTreeImporter;
                var externalObjectMap = importer.GetExternalObjectMap();
                var materialsList = importer.sourceMaterials;

                int remappedMaterialCount = 0;
                foreach (var entry in externalObjectMap)
                {
                    if (entry.Key.type == typeof(Material) && Array.Exists(materialsList, x => x.name == entry.Key.name))
                        ++remappedMaterialCount;
                }

                m_HasEmbeddedMaterials = m_HasEmbeddedMaterials && remappedMaterialCount != materialsList.Length;
            }
            return m_HasEmbeddedMaterials;
        }

        internal override void OnEnable()
        {
            m_MaterialLocation = serializedObject.FindProperty("m_MaterialLocation");

            m_Materials = serializedObject.FindProperty("m_Materials");
            m_ExternalObjects = serializedObject.FindProperty("m_ExternalObjects");

            m_SupportsEmbeddedMaterials = serializedObject.FindProperty("m_SupportsEmbeddedMaterials");
        }

        public override void OnInspectorGUI()
        {
            ShowMaterialGUI();
        }

        private void ShowMaterialGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.Popup(m_MaterialLocation, Styles.MaterialLocationOpt, Styles.MaterialLocation);

            string materialHelp = string.Empty;
            if (!m_MaterialLocation.hasMultipleDifferentValues)
            {
                if (m_Materials.arraySize > 0 && HasEmbeddedMaterials())
                {
                    // we're generating materials inside the prefab
                    materialHelp = Styles.InternalMaterialHelp.text;
                }

                if (targets.Length == 1 && m_Materials.arraySize > 0 && m_MaterialLocation.intValue != 0)
                {
                    materialHelp += " " + Styles.MaterialAssignmentsHelp.text;
                }

                // display the extract buttons
                if (m_MaterialLocation.intValue != 0)
                {
                    if (ExtractMaterialsGUI())
                        return;
                }
            }

            if (!string.IsNullOrEmpty(materialHelp))
            {
                EditorGUILayout.HelpBox(materialHelp, MessageType.Info);
            }

            if ((targets.Length == 1 || m_SupportsEmbeddedMaterials.hasMultipleDifferentValues == false) && m_SupportsEmbeddedMaterials.boolValue == false
                && m_MaterialLocation.intValue != 0 && !m_MaterialLocation.hasMultipleDifferentValues)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(Styles.NoMaterialMappingsHelp.text, MessageType.Warning);
            }

            // The material remap list
            if (targets.Length == 1 && m_Materials.arraySize > 0 && m_MaterialLocation.intValue != 0 && !m_MaterialLocation.hasMultipleDifferentValues)
            {
                GUILayout.Label(Styles.ExternalMaterialMappings, EditorStyles.boldLabel);

                if (MaterialRemapOptons())
                    return;

                // The list of material names is immutable, whereas the map of external objects can change based on user actions.
                // For each material name, map the external object associated with it.
                // The complexity comes from the fact that we may not have an external object in the map, so we can't make a property out of it
                for (int materialIdx = 0; materialIdx < m_Materials.arraySize; ++materialIdx)
                {
                    var id = m_Materials.GetArrayElementAtIndex(materialIdx);
                    var name = id.FindPropertyRelative("name").stringValue;
                    var type = id.FindPropertyRelative("type").stringValue;
                    var assembly = id.FindPropertyRelative("assembly").stringValue;

                    SerializedProperty materialProp = null;
                    Material material = null;
                    var propertyIdx = 0;

                    for (int externalObjectIdx = 0, count = m_ExternalObjects.arraySize; externalObjectIdx < count; ++externalObjectIdx)
                    {
                        var pair = m_ExternalObjects.GetArrayElementAtIndex(externalObjectIdx);
                        var externalName = pair.FindPropertyRelative("first.name").stringValue;
                        var externalType = pair.FindPropertyRelative("first.type").stringValue;

                        if (externalName == name && externalType == type)
                        {
                            materialProp = pair.FindPropertyRelative("second");
                            material = materialProp != null ? materialProp.objectReferenceValue as Material : null;
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
                                var newIndex = m_ExternalObjects.arraySize++;
                                var pair = m_ExternalObjects.GetArrayElementAtIndex(newIndex);
                                pair.FindPropertyRelative("first.name").stringValue = name;
                                pair.FindPropertyRelative("first.type").stringValue = type;
                                pair.FindPropertyRelative("first.assembly").stringValue = assembly;
                                pair.FindPropertyRelative("second").objectReferenceValue = material;
                            }
                        }
                    }
                }
            }
        }

        private bool ExtractMaterialsGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(Styles.Materials);
                using (new EditorGUI.DisabledScope(!HasEmbeddedMaterials()))
                {
                    if (GUILayout.Button(Styles.ExtractEmbeddedMaterials))
                    {
                        // use the first target for selecting the destination folder, but apply that path for all targets
                        string destinationPath = (target as SpeedTreeImporter).assetPath;
                        destinationPath = EditorUtility.SaveFolderPanel(Styles.SelectMaterialFolder.text,
                            FileUtil.DeleteLastPathNameComponent(destinationPath), "");
                        if (string.IsNullOrEmpty(destinationPath))
                        {
                            // cancel the extraction if the user did not select a folder
                            return false;
                        }
                        destinationPath = FileUtil.GetProjectRelativePath(destinationPath);

                        try
                        {
                            // batch the extraction of the textures
                            AssetDatabase.StartAssetEditing();

                            PrefabUtility.ExtractMaterialsFromAsset(targets, destinationPath);
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
                        }

                        // AssetDatabase.StopAssetEditing() invokes OnEnable(), which invalidates all the serialized properties, so we must return.
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MaterialRemapOptons()
        {
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
                        if (GUILayout.Button(Styles.RemapMaterialsInProject))
                        {
                            try
                            {
                                AssetDatabase.StartAssetEditing();

                                foreach (var t in targets)
                                {
                                    var importer = t as SpeedTreeImporter;
                                    var folderToSearch = importer.materialFolderPath;
                                    folderToSearch = EditorUtility.OpenFolderPanel(Styles.SelectMaterialFolder.text, importer.materialFolderPath, "");
                                    importer.SearchAndRemapMaterials(FileUtil.GetProjectRelativePath(folderToSearch));

                                    AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
                                    AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
                                }
                            }
                            finally
                            {
                                AssetDatabase.StopAssetEditing();
                            }

                            return true;
                        }
                    }
                }
                EditorGUILayout.Space();
            }

            return false;
        }
    }
}
