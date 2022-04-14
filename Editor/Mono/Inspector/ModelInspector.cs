// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(Mesh))]
    [CanEditMultipleObjects]
    class ModelInspector : Editor
    {
        Dictionary<Object, MeshPreview> m_MeshPreviews = new Dictionary<Object, MeshPreview>();
        MeshPreview m_DirtyMeshPreview = null;

        static Vector2 m_ScrollPos;

        void OnEnable()
        {
            foreach (var previewTarget in targets)
            {
                var meshPreview = new MeshPreview(previewTarget as Mesh);
                meshPreview.settingsChanged += OnPreviewSettingsChanged;
                m_MeshPreviews.Add(previewTarget, meshPreview);
            }
        }

        void OnDisable()
        {
            foreach (var previewTarget in targets)
            {
                var meshPreview = m_MeshPreviews[previewTarget];
                meshPreview.settingsChanged -= OnPreviewSettingsChanged;
                meshPreview.Dispose();
            }

            m_MeshPreviews.Clear();
        }

        public override void OnPreviewSettings()
        {
            if (m_MeshPreviews.TryGetValue(target, out var targetMeshPreview))
                targetMeshPreview.OnPreviewSettings();

            if (m_DirtyMeshPreview != null)
            {
                foreach (var meshPreview in m_MeshPreviews.Values)
                {
                    if (meshPreview != m_DirtyMeshPreview)
                        meshPreview.CopySettings(m_DirtyMeshPreview);
                }

                m_DirtyMeshPreview = null;
            }
        }

        void OnPreviewSettingsChanged(MeshPreview preview)
        {
            m_DirtyMeshPreview = preview;
        }

        public override Texture2D RenderStaticPreview(
            string assetPath,
            Object[] subAssets,
            int width,
            int height)
        {
            if (m_MeshPreviews.TryGetValue(target, out var meshPreview))
                return meshPreview.RenderStaticPreview(width, height);

            return null;
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = true;

            // Multi-selection, just display total # of verts/indices and bail out
            if (targets?.Length > 1)
            {
                long totalVertices = 0;
                long totalIndices = 0;

                foreach (var obj in targets)
                {
                    if (obj is Mesh m)
                    {
                        totalVertices += m.vertexCount;
                        totalIndices += CalcTotalIndices(m);
                    }
                }

                EditorGUILayout.LabelField($"{targets.Length} meshes selected, {totalVertices} total vertices, {totalIndices} total indices");
                return;
            }

            if (!(target is Mesh mesh))
                return;

            var attributes = mesh.GetVertexAttributes();

            ShowVertexInfo(mesh, attributes);
            ShowIndexInfo(mesh);
            ShowSkinInfo(mesh, attributes);
            ShowBlendShapeInfo(mesh);
            ShowOtherInfo(mesh);

            GUI.enabled = false;
        }

        static void ShowOtherInfo(Mesh mesh)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Bounds Center", mesh.bounds.center.ToString("g4"));
            EditorGUILayout.LabelField("Bounds Size", mesh.bounds.size.ToString("g4"));
            EditorGUILayout.LabelField("Read/Write Enabled", mesh.isReadable.ToString());
            EditorGUI.indentLevel--;
        }

        static void ShowBlendShapeInfo(Mesh mesh)
        {
            var blendShapeCount = mesh.blendShapeCount;
            if (blendShapeCount <= 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Blend Shapes: {blendShapeCount}", EditorStyles.boldLabel);

            var showScroll = blendShapeCount > 10;
            if (showScroll)
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(10 * EditorGUIUtility.singleLineHeight));
            EditorGUI.indentLevel++;
            for (int i = 0; i < blendShapeCount; ++i)
            {
                EditorGUILayout.LabelField($"#{i}: {mesh.GetBlendShapeName(i)} ({mesh.GetBlendShapeFrameCount(i)} frames)");
            }
            EditorGUI.indentLevel--;
            if (showScroll)
                EditorGUILayout.EndScrollView();
        }

        static void ShowSkinInfo(Mesh mesh, VertexAttributeDescriptor[] attributes)
        {
            var boneCount = mesh.bindposes.Length;
            if (boneCount <= 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Skin: {boneCount} bones", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var a in attributes)
            {
                // only list skin related attributes
                if (a.attribute == VertexAttribute.BlendIndices || a.attribute == VertexAttribute.BlendWeight)
                    EditorGUILayout.LabelField(a.attribute.ToString(), GetAttributeString(a));
            }
            EditorGUI.indentLevel--;
        }

        static void ShowIndexInfo(Mesh mesh)
        {
            var indexCount = CalcTotalIndices(mesh);
            var indexSize = mesh.indexFormat == IndexFormat.UInt16 ? 2 : 4;
            var bufferSizeStr = EditorUtility.FormatBytes(indexCount * indexSize);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Indices: {indexCount}, {mesh.indexFormat} format ({bufferSizeStr})", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var subMeshCount = mesh.subMeshCount;
            string subMeshText = subMeshCount == 1 ? "submesh" : "submeshes";
            EditorGUILayout.LabelField($"{mesh.subMeshCount} {subMeshText}:");

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var subMesh = mesh.GetSubMesh(i);
                string topology = subMesh.topology.ToString().ToLowerInvariant();
                string baseVertex = subMesh.baseVertex == 0 ? "" : ", base vertex " + subMesh.baseVertex;

                var divisor = 3;
                switch (subMesh.topology)
                {
                    case MeshTopology.Points: divisor = 1; break;
                    case MeshTopology.Lines: divisor = 2; break;
                    case MeshTopology.Triangles: divisor = 3; break;
                    case MeshTopology.Quads: divisor = 4; break;
                    case MeshTopology.LineStrip: divisor = 2; break; // technically not correct, but eh
                }

                var primCount = subMesh.indexCount / divisor;
                if (subMeshCount > 1)
                {
                    GUILayout.BeginHorizontal();
                    var rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Width(7));
                    rect.x += EditorGUI.indent;
                    var tint = MeshPreview.GetSubMeshTint(i);
                    DrawColorRect(rect, tint);
                }

                EditorGUILayout.LabelField($"#{i}: {primCount} {topology} ({subMesh.indexCount} indices starting from {subMesh.indexStart}){baseVertex}");
                if (subMeshCount > 1)
                {
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }

        static void ShowVertexInfo(Mesh mesh, VertexAttributeDescriptor[] attributes)
        {
            var vertexSize = attributes.Sum(attr => ConvertFormatToSize(attr.format) * attr.dimension);
            var bufferSizeStr = EditorUtility.FormatBytes((long)mesh.vertexCount * vertexSize);
            EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount} ({bufferSizeStr})", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            foreach (var a in attributes)
            {
                // skin related attributes listed separately
                if (a.attribute == VertexAttribute.BlendIndices || a.attribute == VertexAttribute.BlendWeight)
                    continue;
                var title = a.attribute.ToString();
                if (title.Contains("TexCoord"))
                    title = title.Replace("TexCoord", "UV");
                EditorGUILayout.LabelField(title, GetAttributeString(a));
            }
            EditorGUI.indentLevel--;
        }

        static int ConvertFormatToSize(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return 4;
                case VertexAttributeFormat.Float16:
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.SInt16:
                    return 2;
                case VertexAttributeFormat.UNorm8:
                case VertexAttributeFormat.SNorm8:
                case VertexAttributeFormat.UInt8:
                case VertexAttributeFormat.SInt8:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, $"Unknown vertex format {format}");
            }
        }

        static string GetAttributeString(VertexAttributeDescriptor attr)
        {
            var format = attr.format;
            var dimension = attr.dimension;
            var str = $"{format} x {dimension} ({ConvertFormatToSize(format) * dimension} bytes)";
            if (attr.stream != 0)
                str += $", stream {attr.stream}";
            return str;
        }

        static long CalcTotalIndices(Mesh mesh)
        {
            return mesh.GetTotalIndexCount();
        }

        static void DrawColorRect(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            var dimmed = color * new Color(0.2f, 0.2f, 0.2f, 0.5f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), dimmed);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - 1, rect.y, 1, rect.height), dimmed);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), dimmed);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), dimmed);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_MeshPreviews.TryGetValue(target, out var meshPreview))
                meshPreview.OnPreviewGUI(r, background);
        }

        public override string GetInfoString() => MeshPreview.GetInfoString(target as Mesh);
    }
}
