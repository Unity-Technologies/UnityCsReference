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
        SerializedProperty m_MaterialName;
        SerializedProperty m_MaterialSearch;
        SerializedProperty m_MaterialLocation;

        SerializedProperty m_Materials;
        SerializedProperty m_ExternalObjects;

        SerializedProperty m_HasEmbeddedTextures;

        SerializedProperty m_SupportsEmbeddedMaterials;

        SerializedProperty m_UseSRGBMaterialColor;

        SerializedProperty m_MaterialImportMode;

        private bool m_HasEmbeddedMaterials = false;

        class ExternalObjectCache
        {
            public Material material = null;
            public int propertyIdx = 0;
        }

        class MaterialCache
        {
            public string name;
            public string type;
            public string assembly;
        }

        List<MaterialCache> m_MaterialsCache = new List<MaterialCache>();
        Dictionary<Tuple<string, string>, ExternalObjectCache> m_ExternalObjectsCache = new Dictionary<Tuple<string, string>, ExternalObjectCache>();
        Object m_CacheCurrentTarget = null;

        static class Styles
        {
            public static GUIContent MaterialLocation = EditorGUIUtility.TrTextContent("Location");

            public static GUIContent MaterialName = EditorGUIUtility.TrTextContent("Naming");
            public static GUIContent[] MaterialNameOptMain =
            {
                EditorGUIUtility.TrTextContent("By Base Texture Name"),
                EditorGUIUtility.TrTextContent("From Model's Material"),
                EditorGUIUtility.TrTextContent("Model Name + Model's Material"),
            };
            public static GUIContent[] MaterialNameOptAll =
            {
                EditorGUIUtility.TrTextContent("By Base Texture Name"),
                EditorGUIUtility.TrTextContent("From Model's Material"),
                EditorGUIUtility.TrTextContent("Model Name + Model's Material"),
                EditorGUIUtility.TrTextContent("Texture Name or Model Name + Model's Material (Obsolete)"),
            };
            public static GUIContent MaterialSearch = EditorGUIUtility.TrTextContent("Search");
            public static GUIContent[] MaterialSearchOpt =
            {
                EditorGUIUtility.TrTextContent("Local Materials Folder"),
                EditorGUIUtility.TrTextContent("Recursive-Up"),
                EditorGUIUtility.TrTextContent("Project-Wide")
            };

            public static GUIContent NoMaterialHelp = EditorGUIUtility.TrTextContent("Do not generate materials. Use Unity's default material instead.");

            public static GUIContent ExternalMaterialHelpStart = EditorGUIUtility.TrTextContent("For each imported material, Unity first looks for an existing material named %MAT%.");
            public static GUIContent[] ExternalMaterialNameHelp =
            {
                EditorGUIUtility.TrTextContent("[BaseTextureName]"),
                EditorGUIUtility.TrTextContent("[MaterialName]"),
                EditorGUIUtility.TrTextContent("[ModelFileName]-[MaterialName]"),
                EditorGUIUtility.TrTextContent("[BaseTextureName] or [ModelFileName]-[MaterialName] if no base texture can be found"),
            };
            public static GUIContent[] ExternalMaterialSearchHelp =
            {
                EditorGUIUtility.TrTextContent("Unity will look for it in the local Materials folder."),
                EditorGUIUtility.TrTextContent("Unity will do a recursive-up search for it in all Materials folders up to the Assets folder."),
                EditorGUIUtility.TrTextContent("Unity will search for it anywhere inside the Assets folder.")
            };
            public static GUIContent ExternalMaterialHelpEnd = EditorGUIUtility.TrTextContent("If it doesn't exist, a new one is created in the local Materials folder.");

            public static GUIContent InternalMaterialHelp = EditorGUIUtility.TrTextContent("Materials are embedded inside the imported asset.");

            public static GUIContent MaterialAssignmentsHelp = EditorGUIUtility.TrTextContent("Material assignments can be remapped below.");

            public static GUIContent ExternalMaterialMappings = EditorGUIUtility.TrTextContent("Remapped Materials", "External materials to use for each embedded material.");

            public static GUIContent NoMaterialMappingsHelp = EditorGUIUtility.TrTextContent("Re-import the asset to see the list of used materials.");

            public static GUIContent Textures = EditorGUIUtility.TrTextContent("Textures");
            public static GUIContent ExtractEmbeddedTextures = EditorGUIUtility.TrTextContent("Extract Textures...", "Click on this button to extract the embedded textures.");

            public static GUIContent Materials = EditorGUIUtility.TrTextContent("Materials");
            public static GUIContent ExtractEmbeddedMaterials = EditorGUIUtility.TrTextContent("Extract Materials...", "Click on this button to extract the embedded materials.");

            public static GUIContent RemapOptions = EditorGUIUtility.TrTextContent("On Demand Remap");
            public static GUIContent RemapMaterialsInProject = EditorGUIUtility.TrTextContent("Search and Remap", "Click on this button to search and remap the materials from the project.");

            public static GUIContent SRGBMaterialColor = EditorGUIUtility.TrTextContent("sRGB Albedo Colors", "Albedo colors in gamma space. Disable this for projects using linear color space.");

            public static GUIContent MaterialCreationMode = EditorGUIUtility.TrTextContent("Material Creation Mode", "Select the method used to generate materials during the import process.");
        }

        public ModelImporterMaterialEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        private void UpdateShowAllMaterialNameOptions()
        {
            // We need to display BasedOnTextureName_Or_ModelNameAndMaterialName obsolete option for objects which use this option
#pragma warning disable 618
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
                    if (entry.Key.type == typeof(Material) && Array.Exists(materialsList, x => x.name == entry.Key.name))
                        ++remappedMaterialCount;
                }

                m_HasEmbeddedMaterials = m_HasEmbeddedMaterials && remappedMaterialCount != materialsList.Length;
            }
            return m_HasEmbeddedMaterials;
        }

        internal override void OnEnable()
        {
            // Material
            m_MaterialName = serializedObject.FindProperty("m_MaterialName");
            m_MaterialSearch = serializedObject.FindProperty("m_MaterialSearch");
            m_MaterialLocation = serializedObject.FindProperty("m_MaterialLocation");

            m_Materials = serializedObject.FindProperty("m_Materials");
            m_ExternalObjects = serializedObject.FindProperty("m_ExternalObjects");

            m_HasEmbeddedTextures = serializedObject.FindProperty("m_HasEmbeddedTextures");

            m_SupportsEmbeddedMaterials = serializedObject.FindProperty("m_SupportsEmbeddedMaterials");

            m_UseSRGBMaterialColor = serializedObject.FindProperty("m_UseSRGBMaterialColor");

            m_MaterialImportMode = serializedObject.FindProperty("m_MaterialImportMode");

            if (m_CacheCurrentTarget != target)
            {
                m_CacheCurrentTarget = target;
                BuildMaterialsCache();
                BuildExternalObjectsCache();
            }

            Undo.undoRedoPerformed += ResetValues;
        }

        internal override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetValues;
            base.OnDisable();
        }

        private void BuildMaterialsCache()
        {
            // do not set if multiple selection.
            if (m_Materials.hasMultipleDifferentValues)
                return;

            m_MaterialsCache.Clear();
            for (int materialIdx = 0; materialIdx < m_Materials.arraySize; ++materialIdx)
            {
                var mat = new MaterialCache();
                var id = m_Materials.GetArrayElementAtIndex(materialIdx);
                mat.name = id.FindPropertyRelative("name").stringValue;
                mat.type = id.FindPropertyRelative("type").stringValue;
                mat.assembly = id.FindPropertyRelative("assembly").stringValue;
                m_MaterialsCache.Add(mat);
            }
        }

        private void BuildExternalObjectsCache()
        {
            // do not set if multiple selection.
            if (m_ExternalObjects.hasMultipleDifferentValues)
                return;

            m_ExternalObjectsCache.Clear();
            for (int externalObjectIdx = 0, count = m_ExternalObjects.arraySize; externalObjectIdx < count; ++externalObjectIdx)
            {
                var pair = m_ExternalObjects.GetArrayElementAtIndex(externalObjectIdx);

                var cachedObject = new ExternalObjectCache();
                var materialProp = pair.FindPropertyRelative("second");
                cachedObject.material = materialProp != null ? materialProp.objectReferenceValue as Material : null;
                cachedObject.propertyIdx = externalObjectIdx;
                var externalName = pair.FindPropertyRelative("first.name").stringValue;
                var externalType = pair.FindPropertyRelative("first.type").stringValue;

                m_ExternalObjectsCache.Add(new Tuple<string, string>(externalName, externalType), cachedObject);
            }
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

        private bool MaterialRemapOptions()
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
                    using (new EditorGUI.DisabledScope(assetTarget == null))
                    {
                        if (GUILayout.Button(Styles.RemapMaterialsInProject))
                        {
                            Undo.RecordObjects(targets, "Search and Remap Materials");
                            foreach (var t in targets)
                            {
                                var importer = t as ModelImporter;
                                // SearchAndReplaceMaterials will ensure the material name and search options get saved, while all other pending changes stay pending.
                                importer.SearchAndRemapMaterials((ModelImporterMaterialName)m_MaterialName.intValue, (ModelImporterMaterialSearch)m_MaterialSearch.intValue);
                            }

                            ResetValues();
                            EditorGUIUtility.ExitGUI();

                            return true;
                        }
                    }
                }
                EditorGUILayout.Space();
            }

            return false;
        }

        void DoMaterialsGUI()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox(L10n.Tr("Material Editing is not supported on multiple selection"), MessageType.Info);
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();

            UpdateShowAllMaterialNameOptions();

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var prop = new EditorGUI.PropertyScope(horizontal.rect, Styles.MaterialCreationMode, m_MaterialImportMode))
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = (int)(ModelImporterMaterialImportMode)EditorGUILayout.EnumPopup(prop.content, (ModelImporterMaterialImportMode)m_MaterialImportMode.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_MaterialImportMode.intValue = newValue;
                    }
                }
            }

            string materialHelp = string.Empty;
            if (!m_MaterialImportMode.hasMultipleDifferentValues)
            {
                if (m_MaterialImportMode.intValue != (int)ModelImporterMaterialImportMode.None)
                {
                    if (m_MaterialImportMode.intValue == (int)ModelImporterMaterialImportMode.LegacyImport)
                    {
                        EditorGUILayout.PropertyField(m_UseSRGBMaterialColor, Styles.SRGBMaterialColor);
                    }

                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var prop = new EditorGUI.PropertyScope(horizontal.rect, Styles.MaterialLocation, m_MaterialLocation))
                        {
                            EditorGUI.BeginChangeCheck();
                            var newValue = (int)(ModelImporterMaterialLocation)EditorGUILayout.EnumPopup(prop.content, (ModelImporterMaterialLocation)m_MaterialLocation.intValue);
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_MaterialLocation.intValue = newValue;
                            }
                        }
                    }

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
            if (m_MaterialImportMode.intValue != (int)ModelImporterMaterialImportMode.None && targets.Length == 1 && m_Materials.arraySize > 0 && m_MaterialLocation.intValue != 0 && !m_MaterialLocation.hasMultipleDifferentValues && !m_Materials.hasMultipleDifferentValues && !m_ExternalObjects.hasMultipleDifferentValues)
            {
                GUILayout.Label(Styles.ExternalMaterialMappings, EditorStyles.boldLabel);

                MaterialRemapOptions();

                DoMaterialRemapList();
            }
        }

        internal override void ResetValues()
        {
            serializedObject.Update();
            BuildMaterialsCache();
            BuildExternalObjectsCache();
        }

        void DoMaterialRemapList()
        {
            // OnEnabled is not called consistently when the asset gets reimported, we need to rebuild the cache here if it's outdated.
            if (m_ExternalObjects.arraySize != m_ExternalObjectsCache.Count())
                ResetValues();
            // The list of material names is immutable, whereas the map of external objects can change based on user actions.
            // For each material name, map the external object associated with it.
            // The complexity comes from the fact that we may not have an external object in the map, so we can't make a property out of it
            for (int materialIdx = 0; materialIdx < m_MaterialsCache.Count; ++materialIdx)
            {
                var mat = m_MaterialsCache[materialIdx];

                ExternalObjectCache cachedExternalObject;

                GUIContent nameLabel = EditorGUIUtility.TextContent(mat.name);
                nameLabel.tooltip = mat.name;

                Material material = m_ExternalObjectsCache.TryGetValue(new Tuple<string, string>(mat.name, mat.type), out cachedExternalObject) ? cachedExternalObject.material : null;

                EditorGUI.BeginChangeCheck();
                material = EditorGUILayout.ObjectField(nameLabel, material, typeof(Material), false) as Material;
                if (EditorGUI.EndChangeCheck())
                {
                    if (cachedExternalObject != null)
                    {
                        if (material == null)
                        {
                            m_ExternalObjects.DeleteArrayElementAtIndex(cachedExternalObject.propertyIdx);
                        }
                        else
                        {
                            var pair = m_ExternalObjects.GetArrayElementAtIndex(cachedExternalObject.propertyIdx);
                            pair.FindPropertyRelative("second").objectReferenceValue = material;
                        }
                    }
                    else if (material != null)
                    {
                        m_ExternalObjects.arraySize++;
                        var pair = m_ExternalObjects.GetArrayElementAtIndex(m_ExternalObjects.arraySize - 1);
                        pair.FindPropertyRelative("first.name").stringValue = mat.name;
                        pair.FindPropertyRelative("first.type").stringValue = mat.type;
                        pair.FindPropertyRelative("first.assembly").stringValue = mat.assembly;
                        pair.FindPropertyRelative("second").objectReferenceValue = material;
                        // ExternalObjects is serialized as a map, so items are reordered when deserializing.
                        // We need to update the serializedObject to trigger the reordering before rebuilding the cache.
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }

                    BuildExternalObjectsCache();
                    break;
                }
            }
        }
    }
}
