// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIR;
using UnityEngine.Experimental.UIElements;

namespace UnityEngine.Internal.Experimental.UIElements
{
    /// <summary>
    /// <c>Panel</c> being internal, this wrapper is used in Graphics Tests to perform basic operations that would
    /// otherwise only be possible with reflection. We prefer to avoid using reflection because it is cumbersome and
    /// only detected at runtime which would add latency to the development process.
    /// </summary>
    public class PanelWrapper : ScriptableObject
    {
        private Panel m_Panel;
        private BaseVisualTreeUpdater m_Updater;

        private void OnEnable()
        {
            m_Panel = UIElementsUtility.FindOrCreatePanel(this);
        }

        private void OnDisable()
        {
            if (m_Updater != null)
                m_Updater.Dispose();
            m_Panel = null;
        }

        public bool UIREnabled
        {
            set
            {
                if (m_Updater != null)
                    m_Updater.Dispose();

                if (value)
                    m_Updater = new UIRRepaintUpdater();
                else
                    m_Updater = new VisualTreeRepaintUpdater();

                m_Panel.SetUpdater(m_Updater, VisualTreeUpdatePhase.Repaint);
            }
        }

        public VisualElement visualTree
        {
            get
            {
                return m_Panel.visualTree;
            }
        }

        public void Repaint(Event e)
        {
            m_Panel.Repaint(e);
        }
    }

    internal class UIRRepaintUpdater : BaseVisualTreeUpdater
    {
        private UIRPainter m_Painter = new UIRPainter();
        private List<VisualElement> m_Elements = new List<VisualElement>();

        public override string description
        {
            get { return "UIRRepaintUpdater"; }
        }

        protected override void Dispose(bool disposing)
        {
            m_Painter.Dispose(disposing);
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.Repaint) == VersionChangeType.Repaint)
            {
                if (!m_Elements.Contains(ve))
                    m_Elements.Add(ve);
            }
        }

        public override void Update()
        {
            if (m_Elements.Count > 0)
            {
                foreach (var ve in m_Elements)
                {
                    m_Painter.currentElement = ve;
                    ve.Repaint(m_Painter);
                }
                m_Elements.Clear();
            }

            m_Painter.Draw();
        }
    }

    internal unsafe class UIRPainter : IStylePainterInternal
    {
        struct Vertex
        {
            public Vector3 Position;
            public Color32 Tint;
            public Vector2 UV;
            public float TransformID;
            public float Flags;
        }

        const int kMaxVertices = 1024;
        const int kMaxIndices = 4096;
        const int kMaxRanges = 1024;

        Utility.GPUBuffer<Vertex> m_VertexGPUBuffer = new Utility.GPUBuffer<Vertex>(kMaxVertices, Utility.GPUBufferType.Vertex);
        Utility.GPUBuffer<UInt16> m_IndexGPUBuffer = new Utility.GPUBuffer<UInt16>(kMaxIndices, Utility.GPUBufferType.Index);
        NativeArray<Vertex> m_VertexData = new NativeArray<Vertex>(kMaxVertices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<UInt16> m_IndexData = new NativeArray<UInt16>(kMaxIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        int m_VertexOffset = 0;
        int m_IndexOffset = 0;

        NativeArray<GfxUpdateBufferRange> m_VertexUpdateRanges = new NativeArray<GfxUpdateBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeArray<GfxUpdateBufferRange> m_IndexUpdateRanges = new NativeArray<GfxUpdateBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        int m_VertexUpdateOffset = 0;
        int m_IndexUpdateOffset = 0;

        NativeArray<DrawBufferRange> m_DrawRanges = new NativeArray<DrawBufferRange>(kMaxRanges, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        int m_DrawRangeCount = 0;

        internal VisualElement currentElement;

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_VertexGPUBuffer.Dispose();
                m_IndexGPUBuffer.Dispose();
                m_VertexData.Dispose();
                m_IndexData.Dispose();
                m_VertexUpdateRanges.Dispose();
                m_IndexUpdateRanges.Dispose();
                m_DrawRanges.Dispose();
            }
        }

        public void Draw()
        {
            Utility.DrawRanges(m_IndexGPUBuffer, m_VertexGPUBuffer, m_DrawRanges.Slice(0, m_DrawRangeCount));
        }

        public float opacity { get { return 1.0f; } set {} }

        public void DrawRect(RectStylePainterParameters painterParams)
        {
            var rect = painterParams.rect;
            var color = painterParams.color;

            var m = currentElement.worldTransform;
            m_VertexData[m_VertexOffset + 0] = new Vertex() { Position = m.MultiplyPoint(new Vector2(rect.x, rect.y)), Tint = color, UV = Vector2.zero, TransformID = 0.0f, Flags = 0.0f };
            m_VertexData[m_VertexOffset + 1] = new Vertex() { Position = m.MultiplyPoint(new Vector2(rect.x + rect.width, rect.y)), Tint = color, UV = Vector2.zero, TransformID = 0.0f, Flags = 0.0f };
            m_VertexData[m_VertexOffset + 2] = new Vertex() { Position = m.MultiplyPoint(new Vector2(rect.x, rect.y + rect.height)), Tint = color, UV = Vector2.zero, TransformID = 0.0f, Flags = 0.0f };
            m_VertexData[m_VertexOffset + 3] = new Vertex() { Position = m.MultiplyPoint(new Vector2(rect.x + rect.width, rect.y + rect.height)), Tint = color, UV = Vector2.zero, TransformID = 0.0f, Flags = 0.0f };

            int vertexStride = m_VertexGPUBuffer.ElementStride;
            m_VertexUpdateRanges[m_VertexUpdateOffset] = new GfxUpdateBufferRange() {
                source = new UIntPtr(m_VertexData.Slice(m_VertexOffset, 4).GetUnsafeReadOnlyPtr()),
                offsetFromWriteStart = 0,
                size = 4 * (UInt32)vertexStride
            };
            m_VertexGPUBuffer.UpdateRanges(m_VertexUpdateRanges.Slice(m_VertexUpdateOffset, 1), m_VertexOffset * vertexStride, (m_VertexOffset + 4) * vertexStride);
            ++m_VertexUpdateOffset;

            m_IndexData[m_IndexOffset + 0] = (UInt16)(m_VertexOffset + 0);
            m_IndexData[m_IndexOffset + 1] = (UInt16)(m_VertexOffset + 1);
            m_IndexData[m_IndexOffset + 2] = (UInt16)(m_VertexOffset + 2);
            m_IndexData[m_IndexOffset + 3] = (UInt16)(m_VertexOffset + 2);
            m_IndexData[m_IndexOffset + 4] = (UInt16)(m_VertexOffset + 1);
            m_IndexData[m_IndexOffset + 5] = (UInt16)(m_VertexOffset + 3);

            int indexStride = m_IndexGPUBuffer.ElementStride;
            m_IndexUpdateRanges[m_IndexUpdateOffset] = new GfxUpdateBufferRange() {
                source = new UIntPtr(m_IndexData.Slice(m_IndexOffset, 6).GetUnsafeReadOnlyPtr()),
                offsetFromWriteStart = 0,
                size = 6 * (UInt32)indexStride
            };
            m_IndexGPUBuffer.UpdateRanges(m_IndexUpdateRanges.Slice(m_IndexUpdateOffset, 1), m_IndexOffset * indexStride, (m_IndexOffset + 6) * indexStride);
            ++m_IndexUpdateOffset;


            var drawRange = m_DrawRanges[m_DrawRangeCount];
            drawRange.firstIndex = m_IndexOffset;
            drawRange.indexCount = 6;
            drawRange.minIndexVal = m_VertexOffset;
            drawRange.vertsReferenced = 4;
            m_DrawRanges[m_DrawRangeCount++] = drawRange;

            m_VertexOffset += 4;
            m_IndexOffset += 6;
        }

        public void DrawMesh(MeshStylePainterParameters painterParameters)
        {
            // Unused
        }

        public void DrawText(TextStylePainterParameters painterParams)
        {
            // Unused
        }

        public void DrawTexture(TextureStylePainterParameters painterParams)
        {
            // Unused
        }

        public void DrawImmediate(System.Action callback)
        {
            // Unused
        }

        public void DrawBackground()
        {
            IStyle style = currentElement.style;
            if (style.backgroundColor != Color.clear)
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.border.SetWidth(0.0f);
                DrawRect(painterParams);
            }
        }

        public void DrawBorder()
        {
            // Unused
        }

        public void DrawText(string text)
        {
            // Unused
        }
    }
}
