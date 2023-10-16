// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

using UnityEngine.Rendering;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal sealed class BoneRenderer
        {
            private List<Matrix4x4> m_BoneMatrices;
            private List<Vector4> m_BoneColors;
            private List<Matrix4x4> m_BoneLeafMatrices;
            private List<Vector4> m_BoneLeafColors;

            private static Mesh s_Mesh;
            private static Material s_Material;

            private const float k_Epsilon = 1e-5f;
            private const float k_BoneScale = 0.08f;
            private const float k_BoneLeafScale = 0.24f;
            private const int k_BoneVertexCount = 4;
            private const int k_BoneLeafDiscResolution = 30;

            public enum SubMeshType
            {
                BoneFaces,
                BoneWire,
                BoneLeafWire,
                Count
            }

            public BoneRenderer()
            {
                m_BoneMatrices = new List<Matrix4x4>();
                m_BoneColors = new List<Vector4>();
                m_BoneLeafMatrices = new List<Matrix4x4>();
                m_BoneLeafColors = new List<Vector4>();
            }

            public void AddBoneInstance(Vector3 start, Vector3 end, Color color)
            {
                float length = (end - start).magnitude;
                if (length < k_Epsilon)
                    return;

                m_BoneMatrices.Add(ComputeBoneMatrix(start, end, length));
                m_BoneColors.Add(new Vector4(color.r, color.g, color.b, color.a));
            }

            public void AddBoneLeafInstance(Vector3 position, Quaternion rotation, float radius, Color color)
            {
                radius *= k_BoneLeafScale;
                if (radius < k_Epsilon)
                    return;

                m_BoneLeafMatrices.Add(Matrix4x4.TRS(position, rotation, new Vector3(radius, radius, radius)));
                m_BoneLeafColors.Add(new Vector4(color.r, color.g, color.b, color.a));
            }

            public void ClearInstances()
            {
                m_BoneMatrices.Clear();
                m_BoneColors.Clear();
                m_BoneLeafMatrices.Clear();
                m_BoneLeafColors.Clear();
            }

            public static Material material
            {
                get
                {
                    if (!s_Material)
                    {
                        Shader shader = (Shader)EditorGUIUtility.LoadRequired("SceneView/BoneHandles.shader");
                        s_Material = new Material(shader);
                        s_Material.enableInstancing = true;
                    }

                    return s_Material;
                }
            }

            public static Mesh mesh
            {
                get
                {
                    if (!s_Mesh)
                    {
                        s_Mesh = new Mesh();
                        s_Mesh.name = "BoneRendererMesh";
                        s_Mesh.subMeshCount = (int)SubMeshType.Count;
                        s_Mesh.hideFlags = HideFlags.DontSave;

                        // Bone vertices
                        List<Vector3> vertices = new List<Vector3>(k_BoneVertexCount + k_BoneLeafDiscResolution * 3);
                        vertices.Add(new Vector3(0.0f, 1.0f, 0.0f));
                        vertices.Add(new Vector3(0.0f, 0.0f, -1.0f));
                        vertices.Add(new Vector3(-0.9f, 0.0f, 0.5f));
                        vertices.Add(new Vector3(0.9f, 0.0f, 0.5f));

                        // Bone leaf vertices
                        Vector3[] tmp = new Vector3[k_BoneLeafDiscResolution];
                        Handles.SetDiscSectionPoints(tmp, Vector3.zero, Vector3.up, Vector3.right, 360f, 1f);
                        vertices.AddRange(tmp);
                        Handles.SetDiscSectionPoints(tmp, Vector3.zero, Vector3.right, Vector3.up, 360f, 1f);
                        vertices.AddRange(tmp);
                        Handles.SetDiscSectionPoints(tmp, Vector3.zero, Vector3.forward, Vector3.up, 360f, 1f);
                        vertices.AddRange(tmp);
                        s_Mesh.vertices = vertices.ToArray();

                        // Build indices for different sub meshes
                        int[] boneFaceIndices = new int[]
                        {
                            0, 2, 1,
                            0, 1, 3,
                            0, 3, 2,
                            1, 2, 3
                        };
                        s_Mesh.SetIndices(boneFaceIndices, MeshTopology.Triangles, (int)SubMeshType.BoneFaces);

                        int[] boneWireIndices = new int[]
                        {
                            0, 1, 0, 2, 0, 3, 1, 2, 2, 3, 3, 1
                        };
                        s_Mesh.SetIndices(boneWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneWire);

                        int counter = 0;
                        int[] boneLeafWireIndices = new int[(vertices.Count - k_BoneVertexCount - 3) * 2];
                        for (int i = k_BoneVertexCount + 1; i < vertices.Count; ++i)
                        {
                            if (((i - k_BoneVertexCount) % k_BoneLeafDiscResolution) == 0)
                                continue;

                            boneLeafWireIndices[counter++] = i - 1;
                            boneLeafWireIndices[counter++] = i;
                        }
                        s_Mesh.SetIndices(boneLeafWireIndices, MeshTopology.Lines, (int)SubMeshType.BoneLeafWire);
                    }

                    return s_Mesh;
                }
            }

            public int boneCount
            {
                get { return m_BoneMatrices.Count; }
            }

            public int boneLeafCount
            {
                get { return m_BoneLeafMatrices.Count; }
            }

            private static Matrix4x4 ComputeBoneMatrix(Vector3 start, Vector3 end, float length)
            {
                Vector3 direction = (end - start) / length;
                Vector3 tangent = Vector3.Cross(direction, Vector3.up);
                if (Vector3.SqrMagnitude(tangent) < 0.1f)
                    tangent = Vector3.Cross(direction, Vector3.right);
                tangent.Normalize();
                Vector3 bitangent = Vector3.Cross(direction, tangent);

                float scale = length * k_BoneScale;

                return new Matrix4x4(
                    new Vector4(tangent.x   * scale,  tangent.y   * scale,  tangent.z   * scale , 0f),
                    new Vector4(direction.x * length, direction.y * length, direction.z * length, 0f),
                    new Vector4(bitangent.x * scale,  bitangent.y * scale,  bitangent.z * scale , 0f),
                    new Vector4(start.x, start.y, start.z, 1f));
            }

            private static int RenderChunkCount(int totalCount)
            {
                return Mathf.CeilToInt((totalCount / (float)Graphics.kMaxDrawMeshInstanceCount));
            }

            private static T[] GetRenderChunk<T>(List<T> array, int chunkIndex)
            {
                int rangeCount = (chunkIndex < (RenderChunkCount(array.Count) - 1)) ?
                    Graphics.kMaxDrawMeshInstanceCount : array.Count - (chunkIndex * Graphics.kMaxDrawMeshInstanceCount);

                return array.GetRange(chunkIndex * Graphics.kMaxDrawMeshInstanceCount, rangeCount).ToArray();
            }

            public static Vector3[] GetBoneWireVertices(Vector3 start, Vector3 end)
            {
                float length = (end - start).magnitude;
                if (length < k_Epsilon)
                    return null;

                Matrix4x4 matrix = ComputeBoneMatrix(start, end, length);

                Vector3[] pts = new Vector3[k_BoneVertexCount];
                for (int i = 0; i < k_BoneVertexCount; ++i)
                    pts[i] = matrix.MultiplyPoint3x4(mesh.vertices[i]);

                int[] boneLines = mesh.GetIndices((int)SubMeshType.BoneWire);
                Vector3[] lines = new Vector3[boneLines.Length];
                for (int i = 0; i < boneLines.Length; ++i)
                    lines[i] = pts[boneLines[i]];

                return lines;
            }

            public void Render()
            {
                if (boneCount == 0)
                    return;

                Material mat = material;
                mat.SetPass(0);

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                CommandBuffer cb = new CommandBuffer();

                Matrix4x4[] matrices = null;

                int chunkCount = RenderChunkCount(boneCount);
                for (int i = 0; i < chunkCount; ++i)
                {
                    cb.Clear();
                    matrices = GetRenderChunk(m_BoneMatrices, i);
                    propertyBlock.SetVectorArray("_Color", GetRenderChunk(m_BoneColors, i));

                    material.DisableKeyword("WIRE_ON");
                    cb.DrawMeshInstanced(mesh, (int)SubMeshType.BoneFaces, material, 0, matrices, matrices.Length, propertyBlock);
                    Graphics.ExecuteCommandBuffer(cb);

                    cb.Clear();
                    material.EnableKeyword("WIRE_ON");
                    cb.DrawMeshInstanced(mesh, (int)SubMeshType.BoneWire, material, 0, matrices, matrices.Length, propertyBlock);
                    Graphics.ExecuteCommandBuffer(cb);
                }

                if (boneLeafCount == 0)
                    return;

                chunkCount = RenderChunkCount(boneLeafCount);
                cb.Clear();
                material.EnableKeyword("WIRE_ON");

                for (int i = 0; i < chunkCount; ++i)
                {
                    matrices = GetRenderChunk(m_BoneLeafMatrices, i);
                    propertyBlock.SetVectorArray("_Color", GetRenderChunk(m_BoneLeafColors, i));
                    cb.DrawMeshInstanced(mesh, (int)SubMeshType.BoneLeafWire, material, 0, matrices, matrices.Length, propertyBlock);
                }
                Graphics.ExecuteCommandBuffer(cb);
            }
        }

        internal static float DistanceToPolygone(Vector3[] vertices)
        {
            return HandleUtility.DistanceToPolyLine(vertices);
        }

        internal static void DoBoneHandle(Transform target, BoneRenderer renderer)
        {
            DoBoneHandle(target, null, renderer);
        }

        internal static void DoBoneHandle(Transform target, Dictionary<Transform, bool> validBones, BoneRenderer renderer)
        {
            int id = target.name.GetHashCode();
            Event evt = Event.current;

            bool hasValidChildBones = false;
            if (validBones != null)
            {
                foreach (Transform child in target)
                {
                    if (validBones.ContainsKey(child))
                    {
                        hasValidChildBones = true;
                        break;
                    }
                }
            }

            Vector3 basePoint = target.position;

            List<Vector3> endPoints = new List<Vector3>();
            // [case 525602] do not draw root.
            if (!hasValidChildBones && target.parent != null)
            {
                endPoints.Add(target.position + (target.position - target.parent.position) * 0.4f);
            }
            else
            {
                foreach (Transform child in target)
                {
                    // Only render bone connections to valid bones
                    // (except if no child bones are valid - then draw all connections)
                    if (validBones != null && !validBones.ContainsKey(child))
                        continue;

                    endPoints.Add(child.position);
                }
            }

            for (int i = 0; i < endPoints.Count; i++)
            {
                Vector3 endPoint = endPoints[i];


                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Layout:
                    case EventType.MouseMove:
                    {
                        // TODO : This is slow and should be revisited prior to exposing bone handles
                        Vector3[] vertices = BoneRenderer.GetBoneWireVertices(basePoint, endPoint);
                        if (vertices != null)
                            HandleUtility.AddControl(id, DistanceToPolygone(vertices));

                        break;
                    }
                    case EventType.MouseDown:
                    {
                        // am I closest to the thingy?
                        if (!evt.alt  && HandleUtility.nearestControl == id && evt.button == 0)
                        {
                            GUIUtility.hotControl = id; // Grab mouse focus
                            if (evt.shift)
                            {
                                Object[] selected = Selection.objects;
                                if (ArrayUtility.Contains(selected, target) == false)
                                {
                                    ArrayUtility.Add(ref selected, target);
                                    Selection.objects = selected;
                                }
                            }
                            else
                                Selection.activeObject = target;

                            EditorGUIUtility.PingObject(target);

                            evt.Use();
                        }
                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if (!evt.alt && GUIUtility.hotControl == id)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new UnityEngine.Object[] {target};
                            DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(target));

                            // having a hot control set during drag makes the control eat the drag events
                            // and dragging of bones no longer works over the avatar configure window
                            // see case 912016
                            GUIUtility.hotControl = 0;

                            evt.Use();
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                    case EventType.Repaint:
                    {
                        color = GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id ? Handles.preselectionColor : color;
                        if (hasValidChildBones)
                            renderer.AddBoneInstance(basePoint, endPoint, color);
                        else
                            renderer.AddBoneLeafInstance(basePoint, target.rotation, (endPoint - basePoint).magnitude, color);
                    }
                    break;
                }
            }
        }
    }
}
