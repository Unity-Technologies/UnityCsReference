// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(PrefabImporter))]
    [CanEditMultipleObjects]
    internal class PrefabImporterEditor : AssetImporterEditor
    {
        static GUIContent s_OpenContent = new GUIContent("Open Prefab");

        int m_HasMixedBaseVariants = -1;

        void CacheHasMixedBaseVariants()
        {
            if (m_HasMixedBaseVariants >= 0)
                return; // already cached

            var firstBaseVarient = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget);
            if (firstBaseVarient == null)
                return;

            m_HasMixedBaseVariants = 0;
            foreach (var t in assetTargets)
            {
                var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (variantBase != firstBaseVarient)
                {
                    m_HasMixedBaseVariants = 1;
                    break;
                }
            }
        }

        public override bool showImportedObject { get { return false; } }

        internal override void OnHeaderControlsGUI()
        {
            var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget);
            if (variantBase != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    CacheHasMixedBaseVariants();
                    GUIContent variantLabel = new GUIContent("Base");
                    GUILayout.Label(variantLabel);
                    EditorGUI.showMixedValue = m_HasMixedBaseVariants == 1;
                    EditorGUILayout.ObjectField(variantBase, typeof(GameObject), false);
                    EditorGUI.showMixedValue = false;
                }
            }
            else
            {
                // Ensure we take up the same amount of height as regular controls
                GUILayoutUtility.GetRect(10, 10, 16, 16, EditorStyles.layerMaskField);
            }
        }

        public override void OnInspectorGUI()
        {
            // Allow opening prefab even if file is not open for edit.
            // For things with explicit save operations (scenes, prefabs) we allow editing
            // and handle the potential version control conflict at the time when the user saves.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            using (new EditorGUI.DisabledScope(assetTargets.Length > 1))
            {
                if (GUILayout.Button(s_OpenContent))
                {
                    // We only support opening one prefab at a time (so do not use 'targets')
                    PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(assetTarget), null, StageNavigationManager.Analytics.ChangeType.EnterViaAssetInspectorOpenButton);

                    GUIUtility.ExitGUI();
                }
            }
            GUI.enabled = wasEnabled;
        }
    }
}
