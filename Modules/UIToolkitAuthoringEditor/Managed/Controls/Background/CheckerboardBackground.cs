// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    [MovedFrom(true, "Unity.UI.Builder", "UnityEditor.UIBuilderModule")]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    partial class CheckerboardBackground : VisualElement
    {
        static readonly CustomStyleProperty<int> k_CellSizeProperty = new CustomStyleProperty<int>("--cell-size");
        static readonly CustomStyleProperty<Color> k_OddCellColorProperty = new CustomStyleProperty<Color>("--odd-cell-color");
        static readonly CustomStyleProperty<Color> k_EvenCellColorProperty = new CustomStyleProperty<Color>("--even-cell-color");

        const int k_DefaultCellSize = 50;
        const int k_TextureSize = 64;
        int m_CellSize = k_DefaultCellSize;
        static readonly Color k_DefaultOddCellColor = new Color(0f, 0f, 0f, 0.18f);
        static readonly Color k_DefaultEvenCellColor = new Color(0f, 0f, 0f, 0.38f);

        Color m_OddCellColor = k_DefaultOddCellColor;
        Color m_EvenCellColor = k_DefaultEvenCellColor;

        Texture2D m_Texture;
        ColorSpace m_TextureColorSpace;

        // test access
        internal Texture2D texture => m_Texture;

        public CheckerboardBackground()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            style.position = Position.Absolute;
            style.width = Length.Percent(100.0f);
            style.height = Length.Percent(100.0f);
            generateVisualContent += OnGenerateVisualContent;
        }

        ~CheckerboardBackground()
        {
            DestroyTexture();
        }

        void DestroyTexture()
        {
            if (m_Texture != null)
                Object.DestroyImmediate(m_Texture);

            m_Texture = null;
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            // If the colorspace changes we need to regenerate the texture (UUM-85114)
            if (PlayerSettings.colorSpace != m_TextureColorSpace)
            {
                GenerateResources();
            }

            // Use worldClip to only render visible area
            var visibleArea = worldClip;
            if (visibleArea.width <= 0 || visibleArea.height <= 0) return;

            var checkerSize = m_CellSize * k_TextureSize;
            var quadSize = new Vector2(checkerSize, checkerSize);

            // Convert world clip coordinates to element-local coordinates
            var elementBounds = worldBound;
            var localVisibleArea = new Rect(
                visibleArea.x - elementBounds.x,
                visibleArea.y - elementBounds.y,
                visibleArea.width,
                visibleArea.height
            );

            // Calculate which quads are actually visible
            var startX = (int) (localVisibleArea.xMin / quadSize.x);
            var endX = (int) (localVisibleArea.xMax / quadSize.x) + 1;
            var startY = (int) (localVisibleArea.yMin / quadSize.y);
            var endY = (int) (localVisibleArea.yMax / quadSize.y) + 1;

            var visibleQuads = (endX - startX) * (endY - startY);
            if (visibleQuads <= 0 || visibleQuads * 4 > UInt16.MaxValue-1) return;

            var mesh = context.Allocate(4 * visibleQuads, 6 * visibleQuads, m_Texture);

            for (var x = startX; x < endX; x++)
                for (var y = startY; y < endY; y++)
                {
                    Quad(mesh, new Vector2(x * quadSize.x, y * quadSize.y), quadSize, Color.white);
                }
        }

        void Quad(MeshWriteData mesh, Vector2 pos, Vector2 size, Color color)
        {
            var x0 = pos.x;
            var y0 = pos.y;

            var x1 = pos.x + size.x;
            var y1 = pos.y + size.y;

            int indexOffset = mesh.currentVertex;

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(x0, y0, Vertex.nearZ),
                tint = color,
                uv = new Vector2(0,0)
            });
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(x1, y0, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1,0)
            });
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(x0, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(0,1)
            });

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(x1, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1,1)
            });

            mesh.SetNextIndex((ushort)(indexOffset + 0));
            mesh.SetNextIndex((ushort)(indexOffset + 1));
            mesh.SetNextIndex((ushort)(indexOffset + 2));

            mesh.SetNextIndex((ushort)(indexOffset + 1));
            mesh.SetNextIndex((ushort)(indexOffset + 3));
            mesh.SetNextIndex((ushort)(indexOffset + 2));
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            bool generateResources = false;

            if (e.customStyle.TryGetValue(k_CellSizeProperty, out var cellSizeProperty))
            {
                if (m_CellSize != cellSizeProperty)
                {
                    m_CellSize = cellSizeProperty;
                    generateResources = true;
                }
            }

            if (e.customStyle.TryGetValue(k_OddCellColorProperty, out var oddCellColor))
            {
                if (m_OddCellColor != oddCellColor)
                {
                    m_OddCellColor = oddCellColor;
                    generateResources = true;
                }
            }

            if (e.customStyle.TryGetValue(k_EvenCellColorProperty, out var evenCellColor))
            {
                if (m_EvenCellColor != evenCellColor)
                {
                    m_EvenCellColor = evenCellColor;
                    generateResources = true;
                }
            }

            if (generateResources || m_Texture == null)
            {
                GenerateResources();
            }
        }

        void GenerateResources()
        {
            DestroyTexture();

            m_TextureColorSpace = PlayerSettings.colorSpace;

            m_Texture = new Texture2D(k_TextureSize, k_TextureSize)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            var even = false;
            for (var x = 0; x < k_TextureSize; x++)
            {
                for (var y = 0; y < k_TextureSize; y++)
                {
                    m_Texture.SetPixel(x, y, even ? m_EvenCellColor : m_OddCellColor);
                    even = !even;
                }
                even = !even;
            }

            // The width and height will not directly be used since we compute a dynamic size in OnGenerateVisualContent
            var veSize = m_CellSize * k_TextureSize;
            m_Texture.Apply(false, true);
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            DestroyTexture();
        }
    }
}
