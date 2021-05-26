// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;

namespace UnityEngine
{
    public sealed partial class StaticBatchingUtility
    {
        internal static ProfilerMarker s_CombineMarker = new ProfilerMarker("StaticBatching.Combine");
        internal static ProfilerMarker s_SortMarker = new ProfilerMarker("StaticBatching.SortObjects");
        internal static ProfilerMarker s_MakeBatchMarker = new ProfilerMarker("StaticBatching.MakeBatch");

        static public void Combine(GameObject staticBatchRoot)
        {
            using (s_CombineMarker.Auto())
                InternalStaticBatchingUtility.CombineRoot(staticBatchRoot, null);
        }

        static public void Combine(GameObject[] gos, GameObject staticBatchRoot)
        {
            using (s_CombineMarker.Auto())
                InternalStaticBatchingUtility.CombineGameObjects(gos, staticBatchRoot, false, null);
        }
    }

    internal class InternalStaticBatchingUtility
    {
        // assume 16bit indices
        const int MaxVerticesInBatch = 64000; // a little bit less than 64K - just in case
        const string CombinedMeshPrefix = "Combined Mesh";

        public static void CombineRoot(UnityEngine.GameObject staticBatchRoot, StaticBatcherGOSorter sorter)
        {
            Combine(staticBatchRoot, false, false, sorter);
        }

        static public void Combine(UnityEngine.GameObject staticBatchRoot, bool combineOnlyStatic, bool isEditorPostprocessScene, StaticBatcherGOSorter sorter)
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

            CombineGameObjects(gos, staticBatchRoot, isEditorPostprocessScene, sorter);
        }

        static uint GetMeshFormatHash(Mesh mesh)
        {
            if (mesh == null)
                return 0;
            uint res = 1;
            var count = mesh.vertexAttributeCount;
            for (var i = 0; i < count; ++i)
            {
                var attr = mesh.GetVertexAttribute(i);
                uint bits = (uint)(attr.attribute) | ((uint)(attr.format) << 4) | ((uint)attr.dimension << 8);
                res = res * 2654435761 + bits; // constant from Knuth's multiplicative hash
            }
            return res;
        }

        static GameObject[] SortGameObjectsForStaticBatching(GameObject[] gos, StaticBatcherGOSorter sorter)
        {
            // Note: LINQ sorting *looks* like it would be less efficient than e.g. Array.Sort,
            // but it's actually much faster -- internally it computes sorting keys for GOs
            // basically once; whereas Array.Sort would be calling the comparison operator
            // many times.
            gos = gos.OrderBy(g =>
            {
                var r = StaticBatcherGOSorter.GetRenderer(g);
                return sorter.GetMaterialId(r);
            }).ThenBy(g =>
                {
                    var r = StaticBatcherGOSorter.GetRenderer(g);
                    return sorter.GetLightmapIndex(r);
                }).ThenBy(g =>
                {
                    var mesh = StaticBatcherGOSorter.GetMesh(g);
                    return GetMeshFormatHash(mesh);
                }).ThenBy(g =>
                {
                    var r = StaticBatcherGOSorter.GetRenderer(g);
                    return sorter.GetRendererId(r);
                }).ToArray();
            return gos;
        }

        static public void CombineGameObjects(GameObject[] gos, UnityEngine.GameObject staticBatchRoot, bool isEditorPostprocessScene, StaticBatcherGOSorter sorter)
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

            using (StaticBatchingUtility.s_SortMarker.Auto())
                gos = SortGameObjectsForStaticBatching(gos, sorter ?? new StaticBatcherGOSorter());

            uint prevMeshFormatHash = 0;
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

                if (!StaticBatchingHelper.IsMeshBatchable(instanceMesh))
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
                if (meshRenderer != null)
                {
                    if (meshRenderer.additionalVertexStreams != null)
                    {
                        if (vertexCount != meshRenderer.additionalVertexStreams.vertexCount)
                            continue;
                    }
                    if (meshRenderer.enlightenVertexStream != null)
                    {
                        if (vertexCount != meshRenderer.enlightenVertexStream.vertexCount)
                            continue;
                    }
                }

                var meshFormatHash = GetMeshFormatHash(instanceMesh);

                // flush previous batch if it got large enough, or the mesh vertex format has changed
                if (verticesInBatch + vertexCount > MaxVerticesInBatch || meshFormatHash != prevMeshFormatHash)
                {
                    MakeBatch(meshes, staticBatchRootTransform, batchIndex++);
                    meshes.Clear();
                    verticesInBatch = 0;
                }

                prevMeshFormatHash = meshFormatHash;

                MeshSubsetCombineUtility.MeshInstance instance = new MeshSubsetCombineUtility.MeshInstance();
                instance.meshInstanceID = instanceMesh.GetInstanceID();
                instance.rendererInstanceID = renderer.GetInstanceID();
                if (meshRenderer != null)
                {
                    if (meshRenderer.additionalVertexStreams != null)
                        instance.additionalVertexStreamsMeshInstanceID = meshRenderer.additionalVertexStreams.GetInstanceID();
                    if (meshRenderer.enlightenVertexStream != null)
                        instance.enlightenVertexStreamMeshInstanceID = meshRenderer.enlightenVertexStream.GetInstanceID();
                }
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

            using (StaticBatchingUtility.s_MakeBatchMarker.Auto())
            {
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

                    int subMeshCount = mesh.subMeshInstances.Count;
                    Renderer renderer = mesh.gameObject.GetComponent<Renderer>();
                    renderer.SetStaticBatchInfo(totalSubMeshCount, subMeshCount);
                    renderer.staticBatchRootTransform = staticBatchRootTransform;

                    // For some reason if GOs were created dynamically
                    // then we need to toggle renderer to avoid caching old geometry
                    renderer.enabled = false;
                    renderer.enabled = true;

                    // Remove additionalVertexStreams and enlightenVertexStream, all their data has been copied into the combined mesh.
                    MeshRenderer meshRenderer = renderer as MeshRenderer;
                    if (meshRenderer != null)
                    {
                        meshRenderer.additionalVertexStreams = null;
                        meshRenderer.enlightenVertexStream = null;
                    }

                    totalSubMeshCount += subMeshCount;
                }
            }
        }

        public class StaticBatcherGOSorter
        {
            public virtual long GetMaterialId(Renderer renderer)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                    return 0;

                return renderer.sharedMaterial.GetInstanceID();
            }

            public int GetLightmapIndex(Renderer renderer)
            {
                if (renderer == null)
                    return -1;
                return renderer.lightmapIndex;
            }

            public static Renderer GetRenderer(GameObject go)
            {
                if (go == null)
                    return null;
                MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
                if (filter == null)
                    return null;

                return filter.GetComponent<Renderer>();
            }

            public static Mesh GetMesh(GameObject go)
            {
                if (go == null)
                    return null;
                var filter = go.GetComponent<MeshFilter>();
                if (filter == null)
                    return null;
                return filter.sharedMesh;
            }

            public virtual long GetRendererId(Renderer renderer)
            {
                if (renderer == null)
                    return -1;
                return renderer.GetInstanceID();
            }
        }
    }
} // namespace UnityEngine
