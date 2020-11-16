using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class CheckerboardBackground : VisualElement
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<CheckerboardBackground, UxmlTraits> {}

        static readonly CustomStyleProperty<int> k_CellSizeProperty = new CustomStyleProperty<int>("--cell-size");
        static readonly CustomStyleProperty<Color> k_OddCellColorProperty = new CustomStyleProperty<Color>("--odd-cell-color");
        static readonly CustomStyleProperty<Color> k_EvenCellColorProperty = new CustomStyleProperty<Color>("--even-cell-color");

        const int k_DefaultCellSize = 50;
        const int k_TextureSize = 64;
        const int k_NumberOfQuadsInRow = 4;
        int m_CellSize = k_DefaultCellSize;
        static readonly Color k_DefaultOddCellColor = new Color(0f, 0f, 0f, 0.18f);
        static readonly Color k_DefaultEvenCellColor = new Color(0f, 0f, 0f, 0.38f);

        Color m_OddCellColor = k_DefaultOddCellColor;
        Color m_EvenCellColor = k_DefaultEvenCellColor;

        Texture2D m_Texture;

        public CheckerboardBackground()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            style.position = Position.Absolute;
            generateVisualContent += OnGenerateVisualContent;
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var quadSize = localBound.size / k_NumberOfQuadsInRow;
            for (var x = 0; x < k_NumberOfQuadsInRow; x++)
                for (var y = 0; y < k_NumberOfQuadsInRow; y++)
                    Quad(new Vector2(x * quadSize.x, y * quadSize.y), quadSize, Color.white, m_Texture, context);
        }

        void Quad(Vector2 pos, Vector2 size, Color color, Texture2D texture2D, MeshGenerationContext context)
        {
            var mesh = context.Allocate(4, 6, texture2D);
            var x0 = pos.x;
            var y0 = pos.y;

            var x1 = pos.x + size.x;
            var y1 = pos.y + size.y;

            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x0, y0, Vertex.nearZ),
                tint = color,
                uv = new Vector2(0,0) * mesh.uvRegion.size + mesh.uvRegion.position
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x1, y0, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1,0) * mesh.uvRegion.size + mesh.uvRegion.position
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x0, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(0,1) * mesh.uvRegion.size + mesh.uvRegion.position
            });

            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x1, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1,1) * mesh.uvRegion.size + mesh.uvRegion.position
            });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);

            mesh.SetNextIndex(1);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(2);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_CellSizeProperty, out var cellSizeProperty))
                m_CellSize = cellSizeProperty;

            if (e.customStyle.TryGetValue(k_OddCellColorProperty, out var oddCellColor))
                m_OddCellColor = oddCellColor;

            if (e.customStyle.TryGetValue(k_EvenCellColorProperty, out var evenCellColor))
                m_EvenCellColor = evenCellColor;

            GenerateResources();
        }

        void GenerateResources()
        {
            if(m_Texture != null)
                Object.DestroyImmediate(m_Texture);

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

            var veSize = m_CellSize * k_TextureSize;
            style.width = veSize * k_NumberOfQuadsInRow;
            style.height = veSize * k_NumberOfQuadsInRow;

            m_Texture.Apply(false, true);
        }
    }
}
