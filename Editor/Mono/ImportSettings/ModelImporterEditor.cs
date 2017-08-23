// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(ModelImporter))]
    [CanEditMultipleObjects]
    internal class ModelImporterEditor : AssetImporterTabbedEditor
    {
        public override void OnEnable()
        {
            if (tabs == null)
            {
                tabs = new BaseAssetImporterTabUI[] { new ModelImporterModelEditor(this), new ModelImporterRigEditor(this), new ModelImporterClipEditor(this), new ModelImporterMaterialEditor(this) };
                m_TabNames = new string[] {"Model", "Rig", "Animation", "Materials"};
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

        // Only show the imported GameObject when the Model tab is active; not when the Animation tab is active
        public override bool showImportedObject { get { return activeTab is ModelImporterModelEditor; } }
    }
}
