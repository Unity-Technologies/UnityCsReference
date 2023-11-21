// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    class CheckerboardBackground : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new CheckerboardBackground();
        }

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

        // test access
        internal Texture2D texture => m_Texture;

        public CheckerboardBackground()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            style.position = Position.Absolute;
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

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            if (parent != null)
            {
                parent.RegisterCallback<GeometryChangedEvent>(e => { UpdateWidthAndHeight(); });
            }
        }

        void UpdateWidthAndHeight()
        {
            if (parent != null)
            {
                var parentSize = new Vector2(parent.worldClip.width, parent.worldClip.height);
                var veSize = m_CellSize * k_TextureSize;
                var quadSize = new Vector2(veSize, veSize);

                int dimX = (int)Mathf.Max(Mathf.Ceil(parentSize.x / quadSize.x), 1.0f);
                int dimY = (int)Mathf.Max(Mathf.Ceil(parentSize.y / quadSize.y), 1.0f);

                style.width = dimX * quadSize.x;
                style.height = dimY * quadSize.y;

                MarkDirtyRepaint();
            }
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var veSize = m_CellSize * k_TextureSize;
            var quadSize = new Vector2(veSize, veSize);

            int dimX = (int)(resolvedStyle.width / quadSize.x) + 1;
            int dimY = (int)(resolvedStyle.height / quadSize.x) + 1;

            float offsetX = ((int)(worldClip.x - worldBound.x) / (m_CellSize * 2)) * (m_CellSize * 2);
            float offsetY = ((int)(worldClip.y - worldBound.y) / (m_CellSize * 2)) * (m_CellSize * 2);

            int dim = dimX * dimY;
            var mesh = context.Allocate(4 * dim, 6 * dim, m_Texture);

            for (var x = 0; x < dimX; x++)
                for (var y = 0; y < dimY; y++)
                {
                    Quad(mesh, new Vector2(offsetX + x * quadSize.x, offsetY + y * quadSize.y), quadSize, Color.white);
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
            style.width = veSize;
            style.height = veSize;

            m_Texture.Apply(false, true);
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            DestroyTexture();
        }
    }
}
