// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ModelImporterMaterialEditor : BaseAssetImporterTabUI
    {
        bool m_ShowAllMaterialNameOptions = true;
        bool m_ShowMaterialRemapOptions = false;

        // Material
        SerializedProperty m_ImportMaterials;
        SerializedProperty m_MaterialName;
        SerializedProperty m_MaterialSearch;
        SerializedProperty m_MaterialLocation;

        SerializedProperty m_Materials;
        SerializedProperty m_ExternalObjects;

        SerializedProperty m_HasEmbeddedTextures;

        SerializedProperty m_SupportsEmbeddedMaterials;

        private bool m_HasEmbeddedMaterials = false;

        static class Styles
        {
            public static GUIContent ImportMaterials = EditorGUIUtility.TextContent("Import Materials");

            public static GUIContent MaterialLocation = EditorGUIUtility.TextContent("Location");
            public static GUIContent[] MaterialLocationOpt =
            {
                EditorGUIUtility.TextContent("Use External Materials (Legacy)|Use external materials if found in the project."),
                EditorGUIUtility.TextContent("Use Embedded Materials|Embed the material inside the imported asset.")
            };

            public static GUIContent MaterialName = EditorGUIUtility.TextContent("Naming");
            public static GUIContent[] MaterialNameOptMain =
            {
                EditorGUIUtility.TextContent("By Base Texture Name"),
                EditorGUIUtility.TextContent("From Model's Material"),
                EditorGUIUtility.TextContent("Model Name + Model's Material"),
            };
            public static GUIContent[] MaterialNameOptAll =
            {
                EditorGUIUtility.TextContent("By Base Texture Name"),
                EditorGUIUtility.TextContent("From Model's Material"),
                EditorGUIUtility.TextContent("Model Name + Model's Material"),
                EditorGUIUtility.TextContent("Texture Name or Model Name + Model's Material (Obsolete)"),
            };
            public static GUIContent MaterialSearch = EditorGUIUtility.TextContent("Search");
            public static GUIContent[] MaterialSearchOpt =
            {
                EditorGUIUtility.TextContent("Local Materials Folder"),
                EditorGUIUtility.TextContent("Recursive-Up"),
                EditorGUIUtility.TextContent("Project-Wide")
            };

            public static GUIContent NoMaterialHelp = EditorGUIUtility.TextContent("Do not generate materials. Use Unity's default material instead.");

            public static GUIContent ExternalMaterialHelpStart = EditorGUIUtility.TextContent("For each imported material, Unity first looks for an existing material named %MAT%.");
            public static GUIContent[] ExternalMaterialNameHelp =
            {
                EditorGUIUtility.TextContent("[BaseTextureName]"),
                EditorGUIUtility.TextContent("[MaterialName]"),
                EditorGUIUtility.TextContent("[ModelFileName]-[MaterialName]"),
                EditorGUIUtility.TextContent("[BaseTextureName] or [ModelFileName]-[MaterialName] if no base texture can be found"),
            };
            public static GUIContent[] ExternalMaterialSearchHelp =
            {
                EditorGUIUtility.TextContent("Unity will look for it in the local Materials folder."),
                EditorGUIUtility.TextContent("Unity will do a recursive-up search for it in all Materials folders up to the Assets folder."),
                EditorGUIUtility.TextContent("Unity will search for it anywhere inside the Assets folder.")
            };
            public static GUIContent ExternalMaterialHelpEnd = EditorGUIUtility.TextContent("If it doesn't exist, a new one is created in the local Materials folder.");

            public static GUIContent InternalMaterialHelp = EditorGUIUtility.TextContent("Materials are embedded inside the imported asset.");

            public static GUIContent MaterialAssignmentsHelp = EditorGUIUtility.TextContent("Material assignments can be remapped below.");

            public static GUIContent ExternalMaterialMappings = EditorGUIUtility.TextContent("Remapped Materials|External materials to use for each embedded material.");

            public static GUIContent NoMaterialMappingsHelp = EditorGUIUtility.TextContent("Re-import the asset to see the list of used materials.");

            public static GUIContent Textures = EditorGUIUtility.TextContent("Textures");
            public static GUIContent ExtractEmbeddedTextures = EditorGUIUtility.TextContent("Extract Textures...|Click on this button to extract the embedded textures.");

            public static GUIContent Materials = EditorGUIUtility.TextContent("Materials");
            public static GUIContent ExtractEmbeddedMaterials = EditorGUIUtility.TextContent("Extract Materials...|Click on this button to extract the embedded materials.");

            public static GUIContent RemapOptions = EditorGUIUtility.TextContent("On Demand Remap");
            public static GUIContent RemapMaterialsInProject = EditorGUIUtility.TextContent("Search and Remap|Click on this button to search and remap the materials from the project.");
        }

        public ModelImporterMaterialEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        private void UpdateShowAllMaterialNameOptions()
        {
            // We need to display BasedOnTextureName_Or_ModelNameAndMaterialName obsolete option for objects which use this option
#pragma warning disable 618
            m_MaterialName = serializedObject.FindProperty("m_MaterialName");
            m_ShowAllMaterialNameOptions = (m_MaterialName.intValue == (int)ModelImporterMaterialName.BasedOnTextureName_Or_ModelNameAndMaterialName);
#pragma warning restore 618
        }

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
                var importer = t as ModelImporter;
                var externalObjectMap = importer.GetExternalObjectMap();
                var materialsList = importer.sourceMaterials;

                int remappedMaterialCount = 0;
                foreach (var entry in externalObjectMap)
                {
                    if (entry.Key.type == typeof(Material))
                        ++remappedMaterialCount;
                }

                m_HasEmbeddedMaterials = m_HasEmbeddedMaterials && remappedMaterialCount != materialsList.Length;
            }
            return m_HasEmbeddedMaterials;
        }

        internal override void OnEnable()
        {
            // Material
            m_ImportMaterials = serializedObject.FindProperty("m_ImportMaterials");
            m_MaterialName = serializedObject.FindProperty("m_MaterialName");
            m_MaterialSearch = serializedObject.FindProperty("m_MaterialSearch");
            m_MaterialLocation = serializedObject.FindProperty("m_MaterialLocation");

            m_Materials = serializedObject.FindProperty("m_Materials");
            m_ExternalObjects = serializedObject.FindProperty("m_ExternalObjects");

            m_HasEmbeddedTextures = serializedObject.FindProperty("m_HasEmbeddedTextures");

            m_SupportsEmbeddedMaterials = serializedObject.FindProperty("m_SupportsEmbeddedMaterials");

            UpdateShowAllMaterialNameOptions();
        }

        internal override void ResetValues()
        {
            base.ResetValues();
            UpdateShowAllMaterialNameOptions();
        }

        internal override void PostApply()
        {
            UpdateShowAllMaterialNameOptions();
        }

        public override void OnInspectorGUI()
        {
            DoMaterialsGUI();
        }

        private void ExtractTexturesGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(Styles.Textures);

                using (
                    new EditorGUI.DisabledScope(!m_HasEmbeddedTextures.boolValue &&
                        !m_HasEmbeddedTextures.hasMultipleDifferentValues))
                {
                    if (GUILayout.Button(Styles.ExtractEmbeddedTextures))
                    {
                        // when extracting textures, we must handle the case when multiple selected assets could generate textures with the same name at the user supplied path
                        // we proceed as follows:
                        // 1. each asset extracts the textures in a separate temp folder
                        // 2. we remap the extracted assets to the respective asset importer
                        // 3. we generate unique names for each asset and move them to the user supplied path
                        // 4. we re-import all the assets to have the internal materials linked to the newly extracted textures

                        List<Tuple<Object, string>> outputsForTargets = new List<Tuple<Object, string>>();
                        // use the first target for selecting the destination folder, but apply that path for all targets
                        string destinationPath = (target as ModelImporter).assetPath;
                        destinationPath = EditorUtility.SaveFolderPanel("Select Textures Folder",
                                FileUtil.DeleteLastPathNameComponent(destinationPath), "");
                        if (string.IsNullOrEmpty(destinationPath))
                        {
                            // cancel the extraction if the user did not select a folder
                            return;
                        }
                        destinationPath = FileUtil.GetProjectRelativePath(destinationPath);

                        try
                        {
                            // batch the extraction of the textures
                            AssetDatabase.StartAssetEditing();

                            foreach (var t in targets)
                            {
                                var tempPath = FileUtil.GetUniqueTempPathInProject();
                                tempPath = tempPath.Replace("Temp", UnityEditorInternal.InternalEditorUtility.GetAssetsFolder());
                                outputsForTargets.Add(Tuple.Create(t, tempPath));

                                var importer = t as ModelImporter;
                                importer.ExtractTextures(tempPath);
                            }
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
                        }

                        try
                        {
                            // batch the remapping and the reimport of the assets
                            AssetDatabase.Refresh();
                            AssetDatabase.StartAssetEditing();

                            foreach (var item in outputsForTargets)
                            {
                                var importer = item.Item1 as ModelImporter;

                                var guids = AssetDatabase.FindAssets("t:Texture", new string[] {item.Item2});

                                foreach (var guid in guids)
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(guid);
                                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                                    if (tex == null)
                                        continue;

                                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(tex), tex);

                                    var newPath = Path.Combine(destinationPath, FileUtil.UnityGetFileName(path));
                                    newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
                                    AssetDatabase.MoveAsset(path, newPath);
                                }

                                AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);

                                AssetDatabase.DeleteAsset(item.Item2);
                            }
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
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
                        string destinationPath = (target as ModelImporter).assetPath;
                        destinationPath = EditorUtility.SaveFolderPanel("Select Materials Folder",
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
                EditorGUILayout.Popup(m_MaterialName,
                    m_ShowAllMaterialNameOptions ? Styles.MaterialNameOptAll : Styles.MaterialNameOptMain,
                    Styles.MaterialName);
                EditorGUILayout.Popup(m_MaterialSearch, Styles.MaterialSearchOpt, Styles.MaterialSearch);

                string searchHelp = Styles.ExternalMaterialHelpStart.text.Replace("%MAT%", Styles.ExternalMaterialNameHelp[m_MaterialName.intValue].text) + "\n" +
                    Styles.ExternalMaterialSearchHelp[m_MaterialSearch.intValue].text;

                EditorGUILayout.HelpBox(searchHelp, MessageType.Info);

                EditorGUI.indentLevel--;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Styles.RemapMaterialsInProject))
                    {
                        try
                        {
                            AssetDatabase.StartAssetEditing();

                            foreach (var t in targets)
                            {
                                var importer = t as ModelImporter;
                                // SearchAndReplaceMaterials will ensure the material name and search options get saved, while all other pending changes stay pending.
                                importer.SearchAndRemapMaterials((ModelImporterMaterialName)m_MaterialName.intValue, (ModelImporterMaterialSearch)m_MaterialSearch.intValue);

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
                EditorGUILayout.Space();
            }

            return false;
        }

        void DoMaterialsGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_ImportMaterials, Styles.ImportMaterials);

            string materialHelp = string.Empty;
            if (!m_ImportMaterials.hasMultipleDifferentValues)
            {
                if (m_ImportMaterials.boolValue)
                {
                    EditorGUILayout.Popup(m_MaterialLocation, Styles.MaterialLocationOpt, Styles.MaterialLocation);
                    if (!m_MaterialLocation.hasMultipleDifferentValues)
                    {
                        if (m_MaterialLocation.intValue == 0)
                        {
                            // (legacy) we're generating materials in the Materials folder
                            EditorGUILayout.Popup(m_MaterialName,
                                m_ShowAllMaterialNameOptions ? Styles.MaterialNameOptAll : Styles.MaterialNameOptMain,
                                Styles.MaterialName);
                            EditorGUILayout.Popup(m_MaterialSearch, Styles.MaterialSearchOpt, Styles.MaterialSearch);

                            materialHelp =
                                Styles.ExternalMaterialHelpStart.text.Replace("%MAT%",
                                    Styles.ExternalMaterialNameHelp[m_MaterialName.intValue].text) + "\n" +
                                Styles.ExternalMaterialSearchHelp[m_MaterialSearch.intValue].text + "\n" +
                                Styles.ExternalMaterialHelpEnd.text;
                        }
                        else if (m_Materials.arraySize > 0 && HasEmbeddedMaterials())
                        {
                            // we're generating materials inside the prefab
                            materialHelp = Styles.InternalMaterialHelp.text;
                        }
                    }

                    if (targets.Length == 1 && m_Materials.arraySize > 0 && m_MaterialLocation.intValue != 0)
                    {
                        materialHelp += " " + Styles.MaterialAssignmentsHelp.text;
                    }

                    // display the extract buttons
                    if (m_MaterialLocation.intValue != 0 && !m_MaterialLocation.hasMultipleDifferentValues)
                    {
                        ExtractTexturesGUI();
                        if (ExtractMaterialsGUI())
                            return;
                    }
                }
                else
                {
                    // we're not importing materials
                    materialHelp = Styles.NoMaterialHelp.text;
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

            // hidden for multi-selection
            if (m_ImportMaterials.boolValue && targets.Length == 1 && m_Materials.arraySize > 0 && m_MaterialLocation.intValue != 0 && !m_MaterialLocation.hasMultipleDifferentValues)
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
    }
}
