// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.SpeedTree.Importer
{
    [CustomEditor(typeof(SpeedTree9Importer))]
    [CanEditMultipleObjects]
    internal class SpeedTree9ImporterEditor : AssetImporterTabbedEditor
    {
        private static class Styles
        {
            public static GUIContent ApplyAndGenerate = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and generate materials with new settings.");
            public static GUIContent Regenerate = EditorGUIUtility.TrTextContent("Regenerate Materials", "Regenerate materials from the current importer settings.");
            public static GUIContent RegenerateRemapped = EditorGUIUtility.TrTextContent("Regenerate Materials", "Regenerate the remapped materials from the current import settings.");
            public static GUIContent ApplyAndGenerateRemapped = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and regenerate the remapped materials with new settings.");

            public static readonly string ModelTabName = "Model";
            public static readonly string MaterialsTabName = "Materials";
            public static readonly string WindTabName = "Wind";
        }

        private bool m_HasRemappedMaterials = false;
        private SpeedTreeImporterOutputData m_OutputImporterData = null;
        private SpeedTree9Importer m_STImporter = null;

        internal IEnumerable<SpeedTree9Importer> importers
        {
            get
            {
                SpeedTree9Importer[] st9Importers = new SpeedTree9Importer[targets.Length];

                for (int i = 0; i < targets.Length; ++i)
                {
                    st9Importers[i] = targets[i] as SpeedTree9Importer;
                }

                return st9Importers;
            }
        }

        public override void OnEnable()
        {
            m_STImporter = target as SpeedTree9Importer;

            if (tabs == null)
            {
                tabs = new BaseAssetImporterTabUI[]
                {
                    new SpeedTree9ImporterModelEditor(this),
                    new SpeedTree9ImporterMaterialEditor(this),
                    new SpeedTree9ImporterWindEditor(this)
                };
                m_TabNames = new string[] { Styles.ModelTabName, Styles.MaterialsTabName, Styles.WindTabName };
            }

            m_OutputImporterData = AssetDatabase.LoadAssetAtPath<SpeedTreeImporterOutputData>(m_STImporter.assetPath);
            Debug.Assert(m_OutputImporterData != null);

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

        // None of the ModelImporter sub editors support multi preview.
        public override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI() && targets.Length < 2;
        }

        // Only show the imported GameObject when the Model tab is active.
        public override bool showImportedObject { get { return activeTab is SpeedTree9ImporterModelEditor; } }

        public override GUIContent GetPreviewTitle()
        {
            var tab = activeTab as ModelImporterClipEditor;
            if (tab != null)
                return new GUIContent(tab.selectedClipName);

            return base.GetPreviewTitle();
        }

        internal bool upgradeMaterials
        {
            get
            {
                foreach (var importer in importers)
                {
                    if (importer != null && importer.MaterialsShouldBeRegenerated)
                        return true;
                }
                return false;
            }
        }

        protected override bool OnApplyRevertGUI()
        {
            bool applied = base.OnApplyRevertGUI();

            bool hasModified = HasModified();
            if (tabs == null) // Hitting apply, we lose the tabs object within base.OnApplyRevertGUI()
            {
                if (hasModified)
                    Apply();
                return applied;
            }

            bool doMatsHaveDifferentShaders = (tabs[0] as SpeedTree9ImporterModelEditor).DoMaterialsHaveDifferentShader();

            // We show the "Generate" button when we have extracted materials since the user should have 2 choices:
            // - Apply the importer settings changes on top of the extracted materials (by regenerating them)
            // - Only apply the importer settings changes to the embedded materials and not the extracted ones,
            //   since the users might want to be able to keep their own changes without the importer to erase them.
            m_HasRemappedMaterials = HasRemappedMaterials();

            if (upgradeMaterials || doMatsHaveDifferentShaders || m_HasRemappedMaterials)
            {
                // Force material upgrade when a custom render pipeline is active so that render pipeline-specific material
                // modifications may be applied.
                bool upgrade = upgradeMaterials || (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null);

                if (GUILayout.Button(GetGenerateButtonText(hasModified, upgrade)))
                {
                    // Apply the changes and generate the materials before importing so that asset previews are up-to-date.
                    if (hasModified)
                        Apply();

                    if (upgrade)
                    {
                        foreach (var importer in importers)
                        {
                            importer.SetMaterialsVersionToCurrent();
                        }
                    }

                    GenerateMaterials();

                    if (hasModified || upgrade)
                    {
                        // Necessary since we remap the newly generated materials to the mesh LOD(s).
                        SaveChanges();
                        applied = true;
                    }
                }
            }

            return applied;
        }

        internal GUIContent GetGenerateButtonText(bool modified, bool upgrade)
        {
            if (modified || upgrade)
            {
                if (m_HasRemappedMaterials)
                    return Styles.ApplyAndGenerate;
                else
                    return Styles.ApplyAndGenerateRemapped;
            }
            else
            {
                if (m_HasRemappedMaterials)
                    return Styles.Regenerate;
                else
                    return Styles.RegenerateRemapped;
            }
        }

        private bool HasEmbeddedMaterials
        {
            get
            {
                bool materialsValid = m_OutputImporterData.lodMaterials.materials.Count > 0
                    && m_OutputImporterData.lodMaterials.materials.TrueForAll(p => p.material != null);

                return m_OutputImporterData.hasEmbeddedMaterials && materialsValid;
            }
        }

        private void GenerateMaterials()
        {
            List<string> paths = new List<string>();
            List<string> matFolders = new List<string>();
            List<SpeedTree9Importer> importersWithEmbeddedMaterials = new List<SpeedTree9Importer>();

            // TODO: Add support for multi-edit.
            if (HasEmbeddedMaterials)
            {
                importersWithEmbeddedMaterials.Add(m_STImporter);
            }

            foreach (var importer in importersWithEmbeddedMaterials)
            {
                var remappedAssets = importer.GetExternalObjectMap();

                List<UnityEngine.Object> materials = new List<UnityEngine.Object>();

                foreach (var asset in remappedAssets)
                {
                    if (asset.Value is Material && asset.Value != null)
                    {
                        materials.Add(asset.Value);
                    }
                }

                foreach (var material in materials)
                {
                    string path = AssetDatabase.GetAssetPath(material);
                    paths.Add(path);
                    matFolders.Add(FileUtil.DeleteLastPathNameComponent(path));
                }
            }

            bool doGenerate = true;
            if (paths.Count > 0)
            {
                doGenerate = AssetDatabase.MakeEditable(paths.ToArray(), $"Materials will be checked out in:\n{string.Join("\n", matFolders.ToArray())}");
            }

            if (doGenerate)
            {
                foreach (var importer in importers)
                {
                    importer.RegenerateMaterials();
                }
            }
        }

        private bool HasRemappedMaterials()
        {
            m_HasRemappedMaterials = true;

            if (m_OutputImporterData.materialsIdentifiers.Count == 0)
                return true;

            // if the m_ExternalObjecs map has any unapplied changes, keep the state of the button as is
            if (serializedObject.hasModifiedProperties)
                return m_HasRemappedMaterials;

            m_HasRemappedMaterials = true;
            foreach (var importer in importers)
            {
                var externalObjectMap = importer.GetExternalObjectMap();
                var materialArray = m_OutputImporterData.materialsIdentifiers.ToArray();// importer.SourceMaterials.ToArray();

                int remappedMaterialCount = 0;
                foreach (var entry in externalObjectMap)
                {
                    if (entry.Key.type == typeof(Material) && Array.Exists(materialArray, x => x.name == entry.Key.name && entry.Value != null))
                        ++remappedMaterialCount;
                }

                m_HasRemappedMaterials = m_HasRemappedMaterials && remappedMaterialCount != 0;
                if (!m_HasRemappedMaterials)
                    break;
            }
            return m_HasRemappedMaterials;
        }
    }

    internal abstract class BaseSpeedTree9ImporterTabUI : BaseAssetImporterTabUI
    {
        protected SpeedTreeImporterOutputData m_OutputImporterData;

        internal BaseSpeedTree9ImporterTabUI(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {
        }
        internal override void OnEnable()
        {
            TryLoadOutputImporterData();
        }

        protected IEnumerable<SpeedTree9Importer> importers
        {
            get { return (panelContainer as SpeedTree9ImporterEditor).importers; }
        }

        protected bool upgradeMaterials
        {
            get { return (panelContainer as SpeedTree9ImporterEditor).upgradeMaterials; }
        }

        protected bool TryLoadOutputImporterData()
        {
            m_OutputImporterData = null;

            // Doesn't support multi-edit for now.
            foreach (SpeedTree9Importer importer in importers)
            {
                m_OutputImporterData = AssetDatabase.LoadAssetAtPath<SpeedTreeImporterOutputData>(importer.assetPath);
                break;
            }

            return m_OutputImporterData != null;
        }
    }
}
