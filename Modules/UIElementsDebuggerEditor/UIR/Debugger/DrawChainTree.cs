// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;
using Styles = UnityEditor.UIElements.Debugger.DrawChainDebugger;
using MeshRenderer = UnityEngine.UIElements.UIR.MeshRenderer;

namespace UnityEditor.UIElements.Debugger
{
    interface IChainItem
    {
        string name { get; }
        void DrawProperties();
        void DrawToolbar();
    }

    class NullTreeItem : TreeViewItem, IChainItem
    {
        private static string s_Name = "NULL";

        public NullTreeItem(int depth)
            : base(0, depth, s_Name)
        {
        }

        public string name
        {
            get { return s_Name; }
        }

        public void DrawProperties()
        {}

        public void DrawToolbar()
        {}
    }

    class UIRDataTreeItem : TreeViewItem, IChainItem
    {
        public readonly UIRenderData m_UIRData;

        public UIRDataTreeItem(UIRenderData uirData, int depth, int id)
            : base(id, depth, GetDisplayName(uirData))
        {
            m_UIRData = uirData;
        }

        public string name
        {
            get { return GetDisplayName(m_UIRData); }
        }

        public void DrawProperties()
        {
            EditorGUILayout.RectField("World pos", m_UIRData.visualElement.worldBound);
            EditorGUILayout.LabelField("Previous", m_UIRData.previousData?.visualElement.DebugName());
            EditorGUILayout.LabelField("Next", m_UIRData.nextData?.visualElement.DebugName());
            EditorGUILayout.LabelField("Nested", m_UIRData.nextNestedData?.visualElement.DebugName());
            EditorGUILayout.LabelField("ViewTransform", m_UIRData.effectiveViewTransformData?.visualElement.DebugName());
            EditorGUILayout.LabelField("SkinningTransform", m_UIRData.effectiveSkinningTransformData?.visualElement.DebugName());
            EditorGUILayout.IntField("TransformID", (int)m_UIRData.effectiveSkinningTransformId);
            EditorGUILayout.LabelField("ClippingRect", m_UIRData.effectiveClippingRectData?.visualElement.DebugName());
            EditorGUILayout.IntField("ClippingRectId", (int)m_UIRData.effectiveClippingRectId);
            EditorGUILayout.Toggle("IsMaskClip", m_UIRData.cachedMaskRenderer != null);
        }

        public void DrawToolbar()
        {
        }

        private static string GetDisplayName(UIRenderData uirData)
        {
            return uirData.visualElement.DebugName();
        }
    }

    class RendererTreeItem : TreeViewItem, IChainItem
    {
        public readonly RendererBase m_Renderer;

        public RendererTreeItem(RendererBase renderer, int depth, int id)
            : base(id, depth, GetDisplayName(renderer))
        {
            m_Renderer = renderer;
        }

        public string name
        {
            get { return GetDisplayName(m_Renderer); }
        }

        public void DrawProperties()
        {
            if (m_Renderer.type == RendererTypes.MeshRenderer)
            {
                DrawState(((MeshRenderer)m_Renderer).state);
                return;
            }

            if ((m_Renderer.type & RendererTypes.ContentRenderer) != 0)
            {
                DrawContentRenderer((ContentRendererBase)m_Renderer);
                return;
            }

            if (m_Renderer.type == RendererTypes.ImmediateRenderer)
            {
                DrawImmediateRenderer((ImmediateRenderer)m_Renderer);
                return;
            }
        }

        private static string GetDisplayName(RendererBase renderer)
        {
            return renderer.GetType().Name;
        }

        private void DrawState(State state)
        {
            EditorGUILayout.LabelField(Styles.rendererStatePropertiesContent, Styles.KInspectorTitle);

            if (state == null)
                return;

            EditorGUILayout.LabelField("Key", "0x" + state.key.ToString("X"));
            EditorGUILayout.ObjectField("Material", state.material, typeof(Material), false);

            var atlasName = state.custom != null ? state.custom.name : "";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Atlas", atlasName);
            EditorGUILayout.ObjectField("", state.custom, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();

            var fontName = state.font != null ? state.font.name : "";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Font", fontName);
            EditorGUILayout.ObjectField("", state.font, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContentRenderer(ContentRendererBase renderer)
        {
            if (renderer.type == RendererTypes.ScissorClipRenderer)
            {
                EditorGUILayout.RectField("Clipping Rect", ((ScissorClipRenderer)renderer).scissorArea);
            }
            else if (renderer.type == RendererTypes.MaskRenderer)
            {
                DrawMaskRenderer(renderer as MaskRenderer);
            }
            else if (renderer.type == RendererTypes.ZoomPanRenderer)
            {
                DrawZoomPanRenderer(renderer as ZoomPanRenderer);
            }
        }

        private void DrawImmediateRenderer(ImmediateRenderer renderer)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            EditorGUILayout.LabelField("Transform");
            for (int row = 0; row < 4; ++row)
                EditorGUILayout.Vector4Field("", renderer.worldTransform.GetRow(row));
            EditorGUILayout.EndVertical();
            EditorGUILayout.RectField("Clipping Rect", renderer.worldClip);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawMaskRenderer(MaskRenderer maskRenderer)
        {
            var maskNode = maskRenderer.maskRegister;
            EditorGUILayout.LabelField("Clipping Mask", Styles.KInspectorTitle);
            MeshNodePreview.DrawClippingMask(maskNode);
        }

        private void DrawZoomPanRenderer(ZoomPanRenderer zoomPanRenderer)
        {
            var matrix = zoomPanRenderer.viewMatrix;
            EditorGUILayout.Vector4Field("", matrix.GetRow(0));
            EditorGUILayout.Vector4Field("", matrix.GetRow(1));
            EditorGUILayout.Vector4Field("", matrix.GetRow(2));
            EditorGUILayout.Vector4Field("", matrix.GetRow(3));
        }

        public void DrawToolbar()
        {}
    }

    class MeshNodeTreeItem : TreeViewItem, IChainItem
    {
        private static string s_Name = "Mesh Node";
        private static bool s_ShowVertices = true;
        private static bool s_ShowTriangle;
        public readonly MeshNode m_MeshNode;

        public MeshNodeTreeItem(MeshNode meshNode, int depth, int id)
            : base(id, depth, s_Name)
        {
            m_MeshNode = meshNode;
        }

        public string name
        {
            get { return s_Name + " Preview"; }
        }

        public void DrawProperties()
        {
            var rendererItem = parent as RendererTreeItem;
            if (rendererItem == null)
                return;

            var meshRenderer = rendererItem.m_Renderer as MeshRenderer;
            if (meshRenderer == null)
                return;

            MeshNodePreview.Draw(meshRenderer.state, m_MeshNode, s_ShowTriangle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (s_ShowVertices)
            {
                EditorGUILayout.LabelField("Vertices", Styles.KInspectorTitle, GUILayout.Height(60));
                foreach (var vert in MeshNodePreview.vertices)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Vector3Field("Position", vert.position);
                    EditorGUILayout.Vector2Field("UV", vert.uv);
                    EditorGUILayout.ColorField("Tint", vert.tint);
                    EditorGUILayout.LabelField("TransformID", vert.transformID.ToString("N0"));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
            }
        }

        public void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(GUIContent.Temp("Preview:"), EditorStyles.toolbarButton, GUILayout.Width(60));
            s_ShowTriangle = GUILayout.Toggle(s_ShowTriangle, GUIContent.Temp("Show Triangles"), EditorStyles.toolbarButton);
            s_ShowVertices = GUILayout.Toggle(s_ShowVertices, GUIContent.Temp("Show Vertices"), EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
        }
    }

    class DrawChainTreeView : UnityEditor.IMGUI.Controls.TreeView
    {
        private Texture2D m_IconWarnSmall;
        private int m_BuildItemId;
        private bool m_ShowInspect;
        private bool m_ShowRawView;

        public DrawChainTreeView(TreeViewState state)
            : base(state)
        {
            showBorder = true;
            m_IconWarnSmall = EditorGUIUtility.LoadIcon("console.warnicon.sml");
        }

        public RendererBase rendererChain;
        public UIRenderData uirDataChain;

        public IChainItem GetSelectedItem(int selectedId)
        {
            return FindRows(new List<int> {selectedId}).FirstOrDefault() as IChainItem;
        }

        public void Collapse()
        {
            int selectedId = GetSelection().First();
            var item = FindRows(new List<int> {selectedId}).FirstOrDefault();
            CollapseAll();
            while (item != null && item.depth >= 0)
            {
                SetExpanded(item.id, true);
                item = item.parent;
            }
        }

        public void ShowInspect(bool show)
        {
            if (m_ShowInspect != show)
            {
                var globalState = new State();
                State state = null;
                InspectRecursive(rootItem, ref state, ref globalState, show);
            }

            m_ShowInspect = show;
        }

        public void SetRawView(bool showRawView)
        {
            if (m_ShowRawView == showRawView)
                return;

            m_ShowRawView = showRawView;
            Reload();
        }

        private void InspectRecursive(TreeViewItem item, ref State state, ref State globalState, bool show)
        {
            if (item is RendererTreeItem)
            {
                bool batchBreaks = false;

                State newState = null;
                var meshRenderer = (item as RendererTreeItem).m_Renderer as MeshRenderer;
                if (meshRenderer != null)
                    newState = meshRenderer.state;

                var maskRenderer = (item as RendererTreeItem).m_Renderer as MaskRenderer;
                if (maskRenderer != null)
                    newState = maskRenderer.state;

                if ((item as RendererTreeItem).m_Renderer is ImmediateRenderer)
                {
                    globalState = new State();
                    batchBreaks = true;
                }

                if ((item as RendererTreeItem).m_Renderer is ScissorClipRenderer)
                    batchBreaks = true;

                if (newState != null)
                {
                    StateFields overrides = globalState.OverrideWith(newState);
                    batchBreaks = batchBreaks || overrides != StateFields.None;

                    state = newState;
                }

                if (!show)
                {
                    // Reset
                    item.icon = null;
                }
                else if (batchBreaks)
                {
                    // State change
                    item.icon = m_IconWarnSmall;
                }
            }
            else if (item is MeshNodeTreeItem)
            {
            }

            if (item.children != null)
            {
                foreach (var child in item.children)
                {
                    InspectRecursive(child, ref state, ref globalState, show);
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1);
            if (uirDataChain != null)
            {
                m_BuildItemId = 1;
                if (m_ShowRawView)
                    TraverseRendererRecursive(root, uirDataChain.innerBegin);
                else
                    TraverseUIRData(root, uirDataChain);
            }
            else
            {
                root.AddChild(new NullTreeItem(root.depth + 1));
            }
            m_ShowInspect = false;
            return root;
        }

        void TraverseRendererRecursive(TreeViewItem tree, RendererBase renderer)
        {
            if (renderer == null)
                return;

            var item = new RendererTreeItem(renderer, tree.depth + 1, m_BuildItemId++);
            tree.AddChild(item);

            var meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
                TraverseNode(item, meshRenderer.meshChain);

            var contentRenderer = renderer as ContentRendererBase;
            if (contentRenderer != null)
                TraverseRendererRecursive(item, renderer.contents);

            TraverseRendererRecursive(tree, renderer.next);
        }

        private void TraverseUIRData(TreeViewItem tree, UIRenderData uirData)
        {
            if (uirData != null)
            {
                var child = new UIRDataTreeItem(uirData, tree.depth + 1, m_BuildItemId++);
                tree.AddChild(child);

                TraverseInnerRenderers(child, uirData.innerBegin, uirData.innerNestedEnd ?? uirData.innerEnd);

                if (uirData.nextNestedData != null)
                    TraverseUIRData(child, uirData.nextNestedData);

                TraverseUIRData(tree, uirData.nextData);
            }
        }

        private void TraverseInnerRenderers(TreeViewItem tree, RendererBase beginRenderer, RendererBase endRenderer)
        {
            if (beginRenderer == null)
                return;

            var renderer = beginRenderer;
            while (renderer != null)
            {
                var child = new RendererTreeItem(renderer, tree.depth + 1, m_BuildItemId++);
                tree.AddChild(child);

                var meshRenderer = renderer as MeshRenderer;
                if (meshRenderer != null)
                    TraverseNode(child, meshRenderer.meshChain);

                if (endRenderer == null || renderer == endRenderer)
                    renderer = null;
                else if ((renderer.type & RendererTypes.ContentRenderer) != 0)
                    // This traversal assumes that we are within the inner renderers of an element. In that case,
                    // the "next" pointers of content renderers must not be followed.
                    renderer = renderer.contents;
                else
                    renderer = renderer.next;
            }
        }

        private void TraverseNode(TreeViewItem tree, MeshNode meshNode)
        {
            if (meshNode != null)
            {
                var nodeItem = new MeshNodeTreeItem(meshNode, tree.depth + 1, m_BuildItemId++);
                tree.AddChild(nodeItem);
                TraverseNode(tree, meshNode.next);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu pm = new GenericMenu();

            pm.AddItem(new GUIContent("Expand"), false, () => this.SetExpandedRecursive(id, true));
            pm.AddItem(new GUIContent("Collapse"), false, () => this.SetExpandedRecursive(id, false));

            pm.ShowAsContext();
        }
    }

    internal static class MeshNodePreview
    {
        private static MeshNode s_MeshNode;
        private static int s_PreviewSize = 300;
        private static bool s_ShowTriangles;
        private static RenderTexture s_PreviewTexture;

        public static Matrix4x4 s_PreviewProjection;

        public static Vertex[] vertices;
        public static UInt16[] indices;

        public static void Draw(State state, MeshNode meshNode, bool showTriangles)
        {
            if (meshNode != s_MeshNode || showTriangles != s_ShowTriangles)
            {
                s_MeshNode = meshNode;
                s_ShowTriangles = showTriangles;
                CopyMeshNodeVerticesAndIndices(meshNode, out vertices, out indices);

                DrawMeshNodePreview(vertices, indices, state, showTriangles);
            }

            DrawPreviewTexture();
        }

        public static void DrawClippingMask(MeshNode meshNode)
        {
            if (meshNode != s_MeshNode)
            {
                s_MeshNode = meshNode;
                CopyMeshNodeVerticesAndIndices(meshNode, out vertices, out indices);

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].tint = Color.white;
                    vertices[i].position.z = MeshRenderer.k_PosZ;
                }

                DrawMeshNodePreview(vertices, indices, new State(), false);
            }

            DrawPreviewTexture();
        }

        private static void DrawPreviewTexture()
        {
            var area = GUILayoutUtility.GetRect(s_PreviewSize, s_PreviewSize, s_PreviewSize, s_PreviewSize);
            var previewArea = new Rect((area.width / 2) - (s_PreviewSize / 2), area.y, s_PreviewSize, s_PreviewSize);
            EditorGUI.DrawPreviewTexture(previewArea, s_PreviewTexture);
        }

        private static void DrawMeshNodePreview(Vertex[] vertices, UInt16[] indices, State state, bool drawTriangles = false)
        {
            if (s_PreviewTexture == null)
            {
                s_PreviewTexture = new RenderTexture(s_PreviewSize, s_PreviewSize, 32,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            }

            var mat = state.material;
            if (mat == null)
            {
                var shader = Shader.Find(UIRUtility.k_DefaultShaderName);
                mat = new Material(shader);
            }

            mat.mainTexture = state.custom;

            var currentRT = RenderTexture.active;
            RenderTexture.active = s_PreviewTexture;

            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            mat.SetPass(0);
            GL.LoadProjectionMatrix(s_PreviewProjection);
            GL.Begin(GL.TRIANGLES);

            foreach (UInt16 index in indices)
            {
                var vertex = vertices[index];
                GL.Color(vertex.tint);
                GL.TexCoord2(vertex.uv.x, vertex.uv.y);
                GL.Vertex3(vertex.position.x, vertex.position.y, vertex.position.z);
            }
            GL.End();

            if (drawTriangles)
            {
                GL.Begin(GL.LINES);
                GL.Color(Color.red);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var v0 = vertices[indices[i]];
                    var v1 = vertices[indices[i + 1]];
                    var v2 = vertices[indices[i + 2]];
                    GL.Vertex3(v0.position.x, v0.position.y, v0.position.z);
                    GL.Vertex3(v1.position.x, v1.position.y, v1.position.z);
                    GL.Vertex3(v1.position.x, v1.position.y, v1.position.z);
                    GL.Vertex3(v2.position.x, v2.position.y, v2.position.z);
                    GL.Vertex3(v2.position.x, v2.position.y, v2.position.z);
                    GL.Vertex3(v0.position.x, v0.position.y, v0.position.z);
                }
                GL.End();
            }

            GL.PopMatrix();

            RenderTexture.active = currentRT;

            if (state.material == null)
                UIRUtility.Destroy(mat);
        }

        private static void CopyMeshNodeVerticesAndIndices(MeshNode node, out Vertex[] vertices, out UInt16[] indices)
        {
            MeshHandle mesh = (MeshHandle)node.mesh;
            Page page = mesh.allocPage;

            vertices = new Vertex[mesh.allocVerts.size];
            indices = new ushort[mesh.allocIndices.size];

            for (uint i = 0; i < mesh.allocVerts.size; i++)
                vertices[i] = page.vertices.cpuData[(int)(i + mesh.allocVerts.start)];
            for (uint i = 0; i < mesh.allocIndices.size; i++)
                indices[i] = (UInt16)(page.indices.cpuData[(int)(i + mesh.allocIndices.start)] - (UInt16)mesh.allocVerts.start);
        }
    }
}
