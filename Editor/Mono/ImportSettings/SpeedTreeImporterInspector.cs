// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(SpeedTreeImporter))]
    [CanEditMultipleObjects]
    internal class SpeedTreeImporterInspector : AssetImporterTabbedEditor
    {
        private static class Styles
        {
            public static GUIContent ApplyAndGenerate = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and generate materials with new settings.");
            public static GUIContent Regenerate = EditorGUIUtility.TrTextContent("Regenerate Materials", "Regenerate materials from the current importer settings.");
            public static GUIContent RegenerateRemapped = EditorGUIUtility.TrTextContent("Regenerate Materials", "Regenerate the remapped materials from the current import settings.");
            public static GUIContent ApplyAndGenerateRemapped = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and regenerate the remapped materials with new settings.");
        }

        private SerializedProperty m_MaterialLocation;
        private SerializedProperty m_Materials;
        private SerializedProperty m_ExternalObjects;
        private bool m_HasRemappedMaterials = false;

        public override void OnEnable()
        {
            m_MaterialLocation = serializedObject.FindProperty("m_MaterialLocation");
            m_Materials = serializedObject.FindProperty("m_Materials");
            m_ExternalObjects = serializedObject.FindProperty("m_ExternalObjects");

            if (tabs == null)
            {
                tabs = new BaseAssetImporterTabUI[] { new SpeedTreeImporterModelEditor(this), new SpeedTreeImporterMaterialEditor(this) };
                m_TabNames = new string[] { "Model", "Materials" };
            }
            base.OnEnable();
        }

        public override void OnDisable()
        {
            foreach (var tab in tabs)
            {
                tab.OnDisable();
            }
            base.OnDisable();
        }

        //None of the ModelImporter sub editors support multi preview
        public override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI() && targets.Length < 2;
        }

        public override GUIContent GetPreviewTitle()
        {
            var tab = activeTab as ModelImporterClipEditor;
            if (tab != null)
                return new GUIContent(tab.selectedClipName);

            return base.GetPreviewTitle();
        }

        // Only show the imported GameObject when the Model tab is active
        public override bool showImportedObject { get { return activeTab is SpeedTreeImporterModelEditor; } }

        internal IEnumerable<SpeedTreeImporter> importers
        {
            get { return targets.Cast<SpeedTreeImporter>(); }
        }

        internal bool upgradeMaterials
        {
            get { return importers.Any(i => i.materialsShouldBeRegenerated); }
        }

        protected override bool OnApplyRevertGUI()
        {
            bool applied;
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                RevertButton();
                applied = ApplyButton();
            }

            if (HasRemappedMaterials())
            {
                bool upgrade = upgradeMaterials;
                GUIContent genButtonText = null;
                if (HasModified() || upgrade)
                {
                    if (m_MaterialLocation.intValue == (int)SpeedTreeImporter.MaterialLocation.External)
                    {
                        genButtonText = Styles.ApplyAndGenerate;
                    }
                    else
                    {
                        genButtonText = Styles.ApplyAndGenerateRemapped;
                    }
                }
                else
                {
                    if (m_MaterialLocation.intValue == (int)SpeedTreeImporter.MaterialLocation.External)
                    {
                        genButtonText = Styles.Regenerate;
                    }
                    else
                    {
                        genButtonText = Styles.RegenerateRemapped;
                    }
                }

                if (GUILayout.Button(genButtonText))
                {
                    bool hasModified = HasModified();

                    // Apply the changes and generate the materials before importing so that asset previews are up-to-date.
                    if (hasModified)
                        Apply();

                    if (upgrade)
                    {
                        foreach (var importer in importers)
                            importer.SetMaterialVersionToCurrent();
                    }

                    GenerateMaterials();

                    if (hasModified || upgrade)
                    {
                        ApplyAndImport();
                        applied = true;
                    }
                }
            }

            return applied;
        }

        private void GenerateMaterials()
        {
            var matFolders = importers.Where(im => im.materialLocation == SpeedTreeImporter.MaterialLocation.External).Select(im => im.materialFolderPath).ToList();
            var guids = AssetDatabase.FindAssets("t:Material", matFolders.ToArray());
            var paths = guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();

            var importersWithEmbeddedMaterials = importers.Where(im => im.materialLocation == SpeedTreeImporter.MaterialLocation.InPrefab);
            foreach (var importer in importersWithEmbeddedMaterials)
            {
                var remappedAssets = importer.GetExternalObjectMap();
                var materials = remappedAssets.Where(kv => kv.Value is Material && kv.Value != null).Select(kv => kv.Value);
                foreach (var material in materials)
                {
                    var path = AssetDatabase.GetAssetPath(material);
                    paths.Add(path);
                    matFolders.Add(FileUtil.DeleteLastPathNameComponent(path));
                }
            }

            bool doGenerate = true;
            if (paths.Count() > 0)
                doGenerate = Provider.PromptAndCheckoutIfNeeded(paths.ToArray(), String.Format("Materials will be checked out in:\n{0}", String.Join("\n", matFolders.ToArray())));

            if (doGenerate)
            {
                foreach (var importer in importers)
                    importer.GenerateMaterials();
            }
        }

        private bool HasRemappedMaterials()
        {
            if (m_MaterialLocation.intValue == 0)
            {
                m_HasRemappedMaterials = true;
            }

            if (m_Materials.arraySize == 0)
                return true;

            // if the m_ExternalObjecs map has any unapplied changes, keep the state of the button as is
            if (m_ExternalObjects.serializedObject.hasModifiedProperties)
                return m_HasRemappedMaterials;

            m_HasRemappedMaterials = true;
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

                m_HasRemappedMaterials = m_HasRemappedMaterials && remappedMaterialCount != 0;
            }
            return m_HasRemappedMaterials;
        }
    }
}
