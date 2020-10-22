// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    internal static class SpeedTreeMaterialFixer
    {
        private class Styles
        {
            public readonly string Message = "There are materials referenced by this Game Object that use the wrong version of SpeedTree shader. Do you want to fix it?";
            public readonly GUIContent FixSpeedTreeShaders = EditorGUIUtility.TrTextContent("Fix SpeedTree Shaders");
        }

        private static Styles s_Styles = null;

        private static IEnumerable<MeshRenderer> EnumerateMeshRenderers(GameObject gameObject)
        {
            var lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                foreach (var lod in lodGroup.GetLODs())
                {
                    foreach (var renderer in lod.renderers)
                    {
                        if (renderer is MeshRenderer meshRenderer)
                            yield return meshRenderer;
                    }
                }
            }
            else
            {
                var meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    yield return meshRenderer;
            }
        }

        public static void DoFixerUI(GameObject gameObject)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            var srpAsset = GraphicsSettings.currentRenderPipeline;

            var defaultSpeedTree8Shader = srpAsset != null ? srpAsset.defaultSpeedTree8Shader : null;
            if (defaultSpeedTree8Shader == null)
                defaultSpeedTree8Shader = Shader.Find("Nature/SpeedTree8");

            var defaultSpeedTree7Shader = srpAsset != null ? srpAsset.defaultSpeedTree7Shader : null;
            if (defaultSpeedTree7Shader == null)
                defaultSpeedTree7Shader = Shader.Find("Nature/SpeedTree");

            if (defaultSpeedTree8Shader == null || defaultSpeedTree7Shader == null)
                return;

            HashSet<Material> materialsUsedForBothVersion = null, speedTreeV7MaterialsToFix = null, speedTreeV8MaterialsToFix = null;

            foreach (var meshRenderer in EnumerateMeshRenderers(gameObject))
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    continue;

                var mesh = meshFilter.sharedMesh;
                if (mesh == null || !EditorUtility.IsPersistent(mesh))
                    continue;

                var assetPath = AssetDatabase.GetAssetPath(mesh);
                var speedTreeImporter = AssetImporter.GetAtPath(assetPath) as SpeedTreeImporter;
                if (speedTreeImporter == null)
                    continue;

                if (materialsUsedForBothVersion == null)
                {
                    materialsUsedForBothVersion = new HashSet<Material>();
                    speedTreeV7MaterialsToFix = new HashSet<Material>();
                    speedTreeV8MaterialsToFix = new HashSet<Material>();
                }

                bool meshIsV8 = speedTreeImporter.isV8;
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    // If the material is used for both v7 & v8 mesh: ignore.
                    if (materialsUsedForBothVersion.Contains(material))
                        continue;

                    // We only fix materials with wrong speedtree shader assigned. We don't know if it's "wrong" if user uses their custom shaders.
                    var wrongShader = meshIsV8 ? defaultSpeedTree7Shader : defaultSpeedTree8Shader;
                    if (material.shader != wrongShader)
                        continue;

                    var targetMaterialSet = meshIsV8 ? speedTreeV8MaterialsToFix : speedTreeV7MaterialsToFix;
                    var otherMaterialSet = meshIsV8 ? speedTreeV7MaterialsToFix : speedTreeV8MaterialsToFix;

                    if (otherMaterialSet.Contains(material))
                    {
                        // rare case that the same material is used both for v7 & v8 material:
                        materialsUsedForBothVersion.Add(material);
                        otherMaterialSet.Remove(material);
                    }
                    else
                    {
                        targetMaterialSet.Add(material);
                    }
                }
            }

            // No speedtree meshes.
            if (materialsUsedForBothVersion == null)
                return;

            if (speedTreeV7MaterialsToFix.Count > 0 || speedTreeV8MaterialsToFix.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(s_Styles.Message, MessageType.Error);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(s_Styles.FixSpeedTreeShaders, GUILayout.ExpandWidth(false)))
                {
                    Undo.RecordObjects(speedTreeV7MaterialsToFix.Concat(speedTreeV8MaterialsToFix).ToArray(), "Fix SpeedTree Shaders");
                    foreach (var material in speedTreeV7MaterialsToFix)
                        material.shader = defaultSpeedTree7Shader;
                    foreach (var material in speedTreeV8MaterialsToFix)
                        material.shader = defaultSpeedTree8Shader;
                }
                GUILayout.EndHorizontal();
            }

            if (materialsUsedForBothVersion.Count != 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Some of the materials are used for both SpeedTree 7 and SpeedTree 8 assets. Unity won't be able to fix that. Please create separate materials.", MessageType.Error);
            }
        }
    }
}
