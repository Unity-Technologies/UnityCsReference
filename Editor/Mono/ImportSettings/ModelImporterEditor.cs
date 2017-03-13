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
        internal override void OnEnable()
        {
            if (m_SubEditorTypes == null)
            {
                m_SubEditorTypes = new System.Type[3] { typeof(ModelImporterModelEditor), typeof(ModelImporterRigEditor), typeof(ModelImporterClipEditor) };
                m_SubEditorNames = new string[3] {"Model", "Rig", "Animations"};
            }
            base.OnEnable();
        }

        //None of the ModelImporter sub editors support multi preview
        public override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI() && targets.Length < 2;
        }

        // Only show the imported GameObject when the Model tab is active; not when the Animation tab is active
        internal override bool showImportedObject { get { return activeEditor is ModelImporterModelEditor; } }
        protected override bool useAssetDrawPreview { get { return false; } }
    }
}
