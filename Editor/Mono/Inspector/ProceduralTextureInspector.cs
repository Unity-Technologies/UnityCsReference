// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// SUBSTANCE HOOK

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;


#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes

namespace UnityEditor
{
    [CustomEditor(typeof(ProceduralTexture))]
    [CanEditMultipleObjects]
    internal class ProceduralTextureInspector : TextureInspector
    {
        private bool m_MightHaveModified = false;

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!EditorApplication.isPlaying && !InternalEditorUtility.ignoreInspectorChanges && m_MightHaveModified)
            {
                m_MightHaveModified = false;
                string[] asset_names = new string[targets.GetLength(0)];
                int i = 0;
                foreach (ProceduralTexture tex in targets)
                {
                    string path = AssetDatabase.GetAssetPath(tex);
                    SubstanceImporter importer = AssetImporter.GetAtPath(path) as SubstanceImporter;
                    if (importer)
                        importer.OnTextureInformationsChanged(tex);
                    path = AssetDatabase.GetAssetPath(tex.GetProceduralMaterial());
                    bool exist = false;
                    for (int j = 0; j < i; ++j)
                    {
                        if (asset_names[j] == path)
                        {
                            exist = true;
                            break;
                        }
                    }
                    if (!exist)
                        asset_names[i++] = path;
                }
                for (int j = 0; j < i; ++j)
                {
                    SubstanceImporter importer = AssetImporter.GetAtPath(asset_names[j]) as SubstanceImporter;
                    if (importer && EditorUtility.IsDirty(importer.GetInstanceID()))
                        AssetDatabase.ImportAsset(asset_names[j], ImportAssetOptions.ForceUncompressedImport);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUI.changed)
                m_MightHaveModified = true;

            // Ensure that materials are updated
            foreach (ProceduralTexture tex in targets)
            {
                if (tex)
                {
                    ProceduralMaterial mat = tex.GetProceduralMaterial();
                    if (mat && mat.isProcessing)
                    {
                        Repaint();
                        SceneView.RepaintAll();
                        GameView.RepaintAll();
                        break;
                    }
                }
            }
        }

        // Same as in ProceduralMaterialInspector
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
            if (target)
            {
                ProceduralMaterial mat = (target as ProceduralTexture).GetProceduralMaterial();
                if (mat && ProceduralMaterialInspector.ShowIsGenerating(mat) && r.width > 50)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Generating...");
            }
        }
    }
}

#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes
