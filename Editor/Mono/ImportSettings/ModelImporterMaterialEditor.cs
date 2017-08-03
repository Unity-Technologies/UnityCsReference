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

        class Styles
        {
            public GUIContent ImportMaterials = EditorGUIUtility.TextContent("Import Materials");

            public GUIContent MaterialLocation = EditorGUIUtility.TextContent("Material Location");
            public GUIContent[] MaterialLocationOpt =
            {
                EditorGUIUtility.TextContent("Use External Materials (Legacy)|Use external materials if found in the project."),
                EditorGUIUtility.TextContent("Use Embedded Materials|Embed the material inside the imported asset.")
            };

            public GUIContent MaterialName = EditorGUIUtility.TextContent("Material Naming");
            public GUIContent[] MaterialNameOptMain =
            {
                EditorGUIUtility.TextContent("By Base Texture Name"),
                EditorGUIUtility.TextContent("From Model's Material"),
                EditorGUIUtility.TextContent("Model Name + Model's Material"),
            };
            public GUIContent[] MaterialNameOptAll =
            {
                EditorGUIUtility.TextContent("By Base Texture Name"),
                EditorGUIUtility.TextContent("From Model's Material"),
                EditorGUIUtility.TextContent("Model Name + Model's Material"),
                EditorGUIUtility.TextContent("Texture Name or Model Name + Model's Material (Obsolete)"),
            };
            public GUIContent MaterialSearch = EditorGUIUtility.TextContent("Material Search");
            public GUIContent[] MaterialSearchOpt =
            {
                EditorGUIUtility.TextContent("Local Materials Folder"),
                EditorGUIUtility.TextContent("Recursive-Up"),
                EditorGUIUtility.TextContent("Project-Wide")
            };

            public GUIContent AutoMapExternalMaterials = EditorGUIUtility.TextContent("Map External Materials|Map the external materials found in the project automatically every time the asset is reimported.");

            public GUIContent NoMaterialHelp = EditorGUIUtility.TextContent("Do not generate materials. Use Unity's default material instead.");

            public GUIContent ExternalMaterialHelpStart = EditorGUIUtility.TextContent("For each imported material, Unity first looks for an existing material named %MAT%.");
            public GUIContent[] ExternalMaterialNameHelp =
            {
                EditorGUIUtility.TextContent("[BaseTextureName]"),
                EditorGUIUtility.TextContent("[MaterialName]"),
                EditorGUIUtility.TextContent("[ModelFileName]-[MaterialName]"),
                EditorGUIUtility.TextContent("[BaseTextureName] or [ModelFileName]-[MaterialName] if no base texture can be found"),
            };
            public GUIContent[] ExternalMaterialSearchHelp =
            {
                EditorGUIUtility.TextContent("Unity will look for it in the local Materials folder."),
                EditorGUIUtility.TextContent("Unity will do a recursive-up search for it in all Materials folders up to the Assets folder."),
                EditorGUIUtility.TextContent("Unity will search for it anywhere inside the Assets folder.")
            };
            public GUIContent ExternalMaterialHelpEnd = EditorGUIUtility.TextContent("If no external material asset is found, the material is embedded inside the imported asset.");

            public GUIContent InternalMaterialHelp = EditorGUIUtility.TextContent("Materials are embedded inside the imported asset.");

            public GUIContent MaterialAssignmentsHelp = EditorGUIUtility.TextContent("Material assignments can be remapped below.");

            public GUIContent ExternalMaterialMappings = EditorGUIUtility.TextContent("Remapped Materials|External materials to use for each embedded material.");

            public GUIContent NoMaterialMappingsHelp = EditorGUIUtility.TextContent("Re-import the asset to see the list of used materials.");

            public GUIContent Textures = EditorGUIUtility.TextContent("Textures");
            public GUIContent ExtractEmbeddedTextures = EditorGUIUtility.TextContent("Extract To...|Click on this button to extract the embedded textures.");

            public GUIContent Materials = EditorGUIUtility.TextContent("Materials");
            public GUIContent ExtractEmbeddedMaterials = EditorGUIUtility.TextContent("Extract To...|Click on this button to extract the embedded materials.");
        }
        static Styles styles;

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
            if (styles == null)
                styles = new Styles();

            MaterialsGUI();
        }

        private void TexturesGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(styles.Textures);

                using (
                    new EditorGUI.DisabledScope(!m_HasEmbeddedTextures.boolValue &&
                        !m_HasEmbeddedTextures.hasMultipleDifferentValues))
                {
                    if (GUILayout.Button(styles.ExtractEmbeddedTextures))
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

        void MaterialsGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_ImportMaterials, styles.ImportMaterials);

            string materialHelp = string.Empty;
            if (!m_ImportMaterials.hasMultipleDifferentValues)
            {
                if (m_ImportMaterials.boolValue)
                {
                    EditorGUILayout.Popup(m_MaterialLocation, styles.MaterialLocationOpt, styles.MaterialLocation);
                    if (!m_MaterialLocation.hasMultipleDifferentValues)
                    {
                        if (m_MaterialLocation.intValue == 0)
                        {
                            // (legacy) we're generating materials in the Materials folder
                            EditorGUILayout.Popup(m_MaterialName,
                                m_ShowAllMaterialNameOptions ? styles.MaterialNameOptAll : styles.MaterialNameOptMain,
                                styles.MaterialName);
                            EditorGUILayout.Popup(m_MaterialSearch, styles.MaterialSearchOpt, styles.MaterialSearch);

                            materialHelp =
                                styles.ExternalMaterialHelpStart.text.Replace("%MAT%",
                                    styles.ExternalMaterialNameHelp[m_MaterialName.intValue].text) + "\n" +
                                styles.ExternalMaterialSearchHelp[m_MaterialSearch.intValue].text + "\n" +
                                styles.ExternalMaterialHelpEnd.text;
                        }
                        else if (m_Materials.arraySize > 0)
                        {
                            // we're generating materials inside the prefab
                            materialHelp = styles.InternalMaterialHelp.text;
                        }
                    }

                    if (targets.Length == 1 && m_Materials.arraySize > 0)
                    {
                        materialHelp += " " + styles.MaterialAssignmentsHelp.text;
                    }

                    // display the extract buttons
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(styles.Materials);
                        using (new EditorGUI.DisabledScope(!HasEmbeddedMaterials()))
                        {
                            if (GUILayout.Button(styles.ExtractEmbeddedMaterials))
                            {
                                // use the first target for selecting the destination folder, but apply that path for all targets
                                string destinationPath = (target as ModelImporter).assetPath;
                                destinationPath = EditorUtility.SaveFolderPanel("Select Materials Folder",
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

                                    PrefabUtility.ExtractMaterialsFromAsset(targets, destinationPath);
                                }
                                finally
                                {
                                    AssetDatabase.StopAssetEditing();
                                }

                                // AssetDatabase.StopAssetEditing() invokes OnEnable(), which invalidates all the serialized properties, so we must return.
                                return;
                            }
                        }
                    }
                    TexturesGUI();
                }
                else
                {
                    // we're not importing materials
                    materialHelp = styles.NoMaterialHelp.text;
                }
            }

            if (!string.IsNullOrEmpty(materialHelp))
            {
                EditorGUILayout.HelpBox(materialHelp, MessageType.Info);
            }

            if ((targets.Length == 1 || m_SupportsEmbeddedMaterials.hasMultipleDifferentValues == false) && m_SupportsEmbeddedMaterials.boolValue == false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(styles.NoMaterialMappingsHelp.text, MessageType.Warning);
            }

            // hidden for multi-selection
            if (m_ImportMaterials.boolValue && targets.Length == 1 && m_Materials.arraySize > 0)
            {
                GUILayout.Label(styles.ExternalMaterialMappings, EditorStyles.boldLabel);

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
