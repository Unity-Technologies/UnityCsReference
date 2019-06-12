// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(ModelImporter))]
    [CanEditMultipleObjects]
    internal class ModelImporterEditor : AssetImporterTabbedEditor
    {
        static string s_LocalizedTitle = L10n.Tr("Model Import Settings");

        static string s_C4DDeprecationWarning = L10n.Tr("Starting with the Unity 2019.3 release, direct import of Cinema4D files will require an external plugin. Keep an eye on our External Tools forum for updates.\n\nPlease note that FBX files exported from Cinema4D will still be supported.");

        public override void OnInspectorGUI()
        {
            if (targets.Any(t => t != null && Path.GetExtension(((AssetImporter)t).assetPath).Equals(".c4d", StringComparison.OrdinalIgnoreCase)))
                EditorGUILayout.HelpBox(s_C4DDeprecationWarning, MessageType.Warning);

            base.OnInspectorGUI();
        }

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

        protected override void Apply()
        {
            base.Apply();

            // This is necessary to enforce redrawing the static preview icons in the project browser,
            // because some settings may have changed the preview completely.
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }

        // Only show the imported GameObject when the Model tab is active; not when the Animation tab is active
        public override bool showImportedObject { get { return activeTab is ModelImporterModelEditor; } }

        internal override string targetTitle
        {
            get
            {
                if (assetTargets == null || assetTargets.Length == 1 || !m_AllowMultiObjectAccess)
                    return base.targetTitle;
                else
                    return assetTargets.Length + " " + s_LocalizedTitle;
            }
        }
    }
}
