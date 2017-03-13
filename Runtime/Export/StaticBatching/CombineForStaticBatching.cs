// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public sealed partial class StaticBatchingUtility
    {
        static public void Combine(GameObject staticBatchRoot)
        {
            InternalStaticBatchingUtility.CombineRoot(staticBatchRoot);
        }

        static public void Combine(GameObject[] gos, GameObject staticBatchRoot)
        {
            InternalStaticBatchingUtility.CombineGameObjects(gos, staticBatchRoot, false);
        }
    }

    internal class InternalStaticBatchingUtility
    {
        // assume 16bit indices
        const int MaxVerticesInBatch = 64000; // a little bit less than 64K - just in case
        const string CombinedMeshPrefix = "Combined Mesh";

        static public void CombineRoot(UnityEngine.GameObject staticBatchRoot)
        {
            Combine(staticBatchRoot, false, false);
        }

        static public void Combine(UnityEngine.GameObject staticBatchRoot, bool combineOnlyStatic, bool isEditorPostprocessScene)
        {
            GameObject[] gos = (GameObject[])UnityEngine.Object.FindObjectsOfType(typeof(GameObject));

            List<GameObject> filteredGos = new List<GameObject>();
            foreach (GameObject go in gos)
            {
                if (staticBatchRoot != null)
                    if (!go.transform.IsChildOf(staticBatchRoot.transform))
                        continue;

                if (combineOnlyStatic && !go.isStaticBatchable)
                    continue;

                filteredGos.Add(go);
            }

            gos = filteredGos.ToArray();

            CombineGameObjects(gos, staticBatchRoot, isEditorPostprocessScene);
        }

        static public void CombineGameObjects(GameObject[] gos, UnityEngine.GameObject staticBatchRoot, bool isEditorPostprocessScene)
        {
            Matrix4x4 staticBatchInverseMatrix = Matrix4x4.identity;
            Transform staticBatchRootTransform = null;
            if (staticBatchRoot)
            {
                staticBatchInverseMatrix = staticBatchRoot.transform.worldToLocalMatrix;
                staticBatchRootTransform = staticBatchRoot.transform;
            }

            int batchIndex = 0;
            int verticesInBatch = 0;
            List<MeshSubsetCombineUtility.MeshContainer> meshes = new List<MeshSubsetCombineUtility.MeshContainer>();

            Array.Sort(gos, new SortGO());

            foreach (GameObject go in gos)
            {
                MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
                if (filter == null)
                    continue;

                Mesh instanceMesh = filter.sharedMesh;

                // reject if has no mesh or (mesh not readable and not called from Editor PostprocessScene.cs)
                // Editor is allowed to modify meshes even if they are marked as read-only e.g. Applicatiopn.LoadLevel() called from a script inside the editor player
                if (instanceMesh == null || (!isEditorPostprocessScene && !instanceMesh.canAccess))
                    continue;

                Renderer renderer = filter.GetComponent<Renderer>();

                // reject if has not renderer or renderer is disabled
                if (renderer == null || !renderer.enabled)
                    continue;

                // reject if already combined for static batching
                if (renderer.staticBatchIndex != 0)
                    continue;

                Material[] materials = renderer.sharedMaterials;

                // reject if any of the material's shader is using DisableBatching tag
                if (materials.Any(m => m != null && m.shader != null && m.shader.disableBatching != DisableBatchingType.False))
                    continue;

                int vertexCount = instanceMesh.vertexCount;
                // Use same tests as MeshCombiner::IsMeshBatchable to stay consistent with C++ code
                if (vertexCount == 0)
                    continue;

                MeshRenderer meshRenderer = renderer as MeshRenderer;
                if ((meshRenderer != null) && (meshRenderer.additionalVertexStreams != null))
                {
                    if (vertexCount != meshRenderer.additionalVertexStreams.vertexCount)
                        continue;
                }

                // check if we have enough space inside the current batch
                if (verticesInBatch + vertexCount > MaxVerticesInBatch)
                {
                    MakeBatch(meshes, staticBatchRootTransform, batchIndex++);
                    meshes.Clear();
                    verticesInBatch = 0;
                }

                MeshSubsetCombineUtility.MeshInstance instance = new MeshSubsetCombineUtility.MeshInstance();
                instance.meshInstanceID = instanceMesh.GetInstanceID();
                instance.rendererInstanceID = renderer.GetInstanceID();
                if (meshRenderer != null && meshRenderer.additionalVertexStreams != null)
                    instance.additionalVertexStreamsMeshInstanceID = meshRenderer.additionalVertexStreams.GetInstanceID();

                instance.transform = staticBatchInverseMatrix * filter.transform.localToWorldMatrix;
                instance.lightmapScaleOffset = renderer.lightmapScaleOffset;
                instance.realtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset;

                MeshSubsetCombineUtility.MeshContainer mesh = new MeshSubsetCombineUtility.MeshContainer();
                mesh.gameObject = go;
                mesh.instance = instance;
                mesh.subMeshInstances = new List<MeshSubsetCombineUtility.SubMeshInstance>();

                //;;Debug.Log("New static mesh (" + go.name + ")verts: " + instanceMesh.vertexCount +
                //  ", tris: " + instanceMesh.triangles.Length +
                //  ", materials: " + renderer.sharedMaterials.Length +
                //  ", subs: " + instanceMesh.subMeshCount
                //  );

                meshes.Add(mesh);

                if (materials.Length > instanceMesh.subMeshCount)
                {
                    Debug.LogWarning("Mesh '" + instanceMesh.name + "' has more materials (" + materials.Length + ") than subsets (" + instanceMesh.subMeshCount + ")", renderer);
                    // extra materials don't have a meaning and it screws the rendering as Unity
                    // tries to render with those extra materials.
                    Material[] newMats = new Material[instanceMesh.subMeshCount];
                    for (int i = 0; i < instanceMesh.subMeshCount; ++i)
                        newMats[i] = renderer.sharedMaterials[i];
                    renderer.sharedMaterials = newMats;
                    materials = newMats;
                }

                for (int m = 0; m < System.Math.Min(materials.Length, instanceMesh.subMeshCount); ++m)
                {
                    //;;Debug.Log("   new subset : " + m + ", tris " + instanceMesh.GetTriangles(m).Length);
                    MeshSubsetCombineUtility.SubMeshInstance subMeshInstance = new MeshSubsetCombineUtility.SubMeshInstance();
                    subMeshInstance.meshInstanceID = filter.sharedMesh.GetInstanceID();
                    subMeshInstance.vertexOffset = verticesInBatch;
                    subMeshInstance.subMeshIndex = m;
                    subMeshInstance.gameObjectInstanceID = go.GetInstanceID();
                    subMeshInstance.transform = instance.transform;
                    mesh.subMeshInstances.Add(subMeshInstance);
                }
                verticesInBatch += instanceMesh.vertexCount;
            }

            MakeBatch(meshes, staticBatchRootTransform, batchIndex);
        }

        static private void MakeBatch(List<MeshSubsetCombineUtility.MeshContainer> meshes, Transform staticBatchRootTransform, int batchIndex)
        {
            if (meshes.Count < 2)
                return;

            List<MeshSubsetCombineUtility.MeshInstance> meshInstances = new List<MeshSubsetCombineUtility.MeshInstance>();
            List<MeshSubsetCombineUtility.SubMeshInstance> allSubMeshInstances = new List<MeshSubsetCombineUtility.SubMeshInstance>();
            foreach (MeshSubsetCombineUtility.MeshContainer mesh in meshes)
            {
                meshInstances.Add(mesh.instance);
                allSubMeshInstances.AddRange(mesh.subMeshInstances);
            }

            string combinedMeshName = CombinedMeshPrefix;
            combinedMeshName += " (root: " + ((staticBatchRootTransform != null) ? staticBatchRootTransform.name : "scene") + ")";
            if (batchIndex > 0)
                combinedMeshName += " " + (batchIndex + 1);

            Mesh combinedMesh = StaticBatchingHelper.InternalCombineVertices(meshInstances.ToArray(), combinedMeshName);
            StaticBatchingHelper.InternalCombineIndices(allSubMeshInstances.ToArray(), combinedMesh);
            int totalSubMeshCount = 0;

            foreach (MeshSubsetCombineUtility.MeshContainer mesh in meshes)
            {
                // Changing the mesh resets the static batch info, so we have to assign sharedMesh first
                MeshFilter filter = (MeshFilter)mesh.gameObject.GetComponent(typeof(MeshFilter));
                filter.sharedMesh = combinedMesh;

                int subMeshCount = mesh.subMeshInstances.Count();
                Renderer renderer = mesh.gameObject.GetComponent<Renderer>();
                renderer.SetStaticBatchInfo(totalSubMeshCount, subMeshCount);
                renderer.staticBatchRootTransform = staticBatchRootTransform;

                // For some reason if GOs were created dynamically
                // then we need to toggle renderer to avoid caching old geometry
                renderer.enabled = false;
                renderer.enabled = true;

                // Remove the additionalVertexStreamsMesh, all its data has been copied into the combined mesh.
                MeshRenderer meshRenderer = renderer as MeshRenderer;
                if (meshRenderer != null)
                    meshRenderer.additionalVertexStreams = null;

                totalSubMeshCount += subMeshCount;
            }
        }

        internal class SortGO : IComparer
        {
            int IComparer.Compare(object a, object b)
            {
                if (a == b)
                    return 0;

                Renderer aRenderer = GetRenderer(a as GameObject);
                Renderer bRenderer = GetRenderer(b as GameObject);

                int compare = GetMaterialId(aRenderer).CompareTo(GetMaterialId(bRenderer));
                if (compare == 0)
                    compare = GetLightmapIndex(aRenderer).CompareTo(GetLightmapIndex(bRenderer));
                return compare;
            }

            static private int GetMaterialId(Renderer renderer)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                    return 0;
                return renderer.sharedMaterial.GetInstanceID();
            }

            static private int GetLightmapIndex(Renderer renderer)
            {
                if (renderer == null)
                    return -1;
                return renderer.lightmapIndex;
            }

            static private Renderer GetRenderer(GameObject go)
            {
                if (go == null)
                    return null;
                MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
                if (filter == null)
                    return null;

                return filter.GetComponent<Renderer>();
            }
        }
    }
} // namespace UnityEngine
