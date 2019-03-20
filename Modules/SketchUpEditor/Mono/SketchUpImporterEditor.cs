// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(SketchUpImporter))]
    [CanEditMultipleObjects]
    internal class SketchUpImporterEditor : ModelImporterEditor
    {
        public override void OnEnable()
        {
            if (tabs == null)
            {
                tabs = new BaseAssetImporterTabUI[] { new SketchUpImporterModelEditor(this), new ModelImporterMaterialEditor(this) };
                m_TabNames = new string[] {"Sketch Up", "Materials"};
            }
            base.OnEnable();
        }

        // Only show the imported GameObject when the Model tab is active; not when the Animation tab is active
        public override bool showImportedObject { get { return activeTab is SketchUpImporterModelEditor; } }
    }
}
