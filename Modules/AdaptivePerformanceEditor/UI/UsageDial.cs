// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AdaptivePerformance.UI.Editor
{
    /// <summary>
    /// Usage Dial is a VisualElement to display a values with a color dial.
    /// </summary>
    public class UsageDial : VisualElement
    {
        Texture2D m_BaseTexture;
        Texture2D m_Texture;

        const string k_UsageDialStyle = "AdaptivePerformance/StyleSheets/Profiler/UsageDialStyle.uss";
        const string k_UsageDial = "AdaptivePerformance/UXML/Profiler/UsageDial.uxml";

        /// <summary>
        /// A value the usage dial should represent.
        /// </summary>
        public int Value
        {
            get { return m_Value; }
            set
            {
                SetValue(value);
            }
        }

        /// <summary>
        /// If the usage dial should show the percentage label.
        /// </summary>
        public bool ShowLabel
        {
            get { return m_showLabel; }
            set { m_showLabel = value; }
        }
        // initial values
        int m_Value = 55;
        byte m_ValueCutoff = 79;
        int m_ThresholdYellowPercentage = 50;
        int m_ThresholdRedPercentage = 75;
        byte m_ThresholdYellow = 0;
        byte m_ThresholdRed = 0;
        static readonly Color32 k_Green = new Color32(136, 176, 49, byte.MaxValue);
        static readonly Color32 k_Yellow = new Color32(221, 124, 69, byte.MaxValue);
        static readonly Color32 k_Red = new Color32(219, 89, 81, byte.MaxValue);
        VisualElement m_IndicatorRoot;
        Label m_Label;
        bool m_showLabel;

        static readonly ushort[] k_Indices = new ushort[]
        {
            0, 1, 2,
            1, 3, 2,
        };

        /// <summary>
        /// Constructor, setups the usage dial with its correct percentages.
        /// </summary>
        public UsageDial()
        {
            var usageDialView = EditorGUIUtility.LoadRequired(k_UsageDial) as VisualTreeAsset;
            var usageDialStyle = EditorGUIUtility.LoadRequired(k_UsageDialStyle) as StyleSheet;
            usageDialView.CloneTree(this);
            this.styleSheets.Add(usageDialStyle);
            // add the background image.
            AddToClassList("usage-dial__background-image");
            Init(m_Value, m_ThresholdYellowPercentage, m_ThresholdRedPercentage, true);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent = GenerateVisualContent;
        }

        void Init(int value, int yellow, int red, bool showLabel)
        {
            m_Value = value;
            m_showLabel = showLabel;
            InitVisualChildElements();
            SetThresholds(yellow, red, force: true);
        }

        void InitVisualChildElements()
        {
            m_IndicatorRoot = this.Q("usage-dial__root");
            m_Label = this.Q<Label>("usage-dial__label");
        }

        /// <summary>
        /// Finalizer destorys the dial texture if still allocated.
        /// </summary>
        ~UsageDial()
        {
            if (m_Texture)
                UnityEngine.Object.DestroyImmediate(m_Texture);
            m_Texture = null;
        }

        /// <summary>
        /// Change the percentage levels of the dial.
        /// </summary>
        /// <param name="yellowPercentage"></param>
        /// <param name="redPercentage"></param>
        /// <param name="force">Force and intemediate update.</param>
        public void SetThresholds(int yellowPercentage, int redPercentage, bool force = false)
        {
            if (!force && yellowPercentage == m_ThresholdYellowPercentage && redPercentage == m_ThresholdRedPercentage)
                return;
            m_ThresholdYellowPercentage = yellowPercentage;
            m_ThresholdRedPercentage = redPercentage;
            m_ThresholdYellow = (byte)(byte.MaxValue * (yellowPercentage / 100f));
            m_ThresholdRed = (byte)(byte.MaxValue * (redPercentage / 100f));
        }
        // Update the ring to reflect warning level.
        // Note call this function to repaint everything including the threshold updates.
        void SetValue(int percentage, bool force = false)
        {
            if (!force && m_Value == percentage)
                return;

            if (m_IndicatorRoot == null)
            {
                InitVisualChildElements();
            }
            var f = percentage / 100f;
            if (m_IndicatorRoot != null)
                m_IndicatorRoot.style.rotate = Quaternion.Euler(0, 0, 180 * f);
            if (m_Label != null)
            {
                if (m_showLabel)
                    m_Label.text = string.Format("{0:0}%", percentage);
                else
                    m_Label.text = "";
            }

            m_Value = percentage;
            m_ValueCutoff = (byte)((percentage / 100f) * byte.MaxValue);
            RegenerateTexture();
            MarkDirtyRepaint();
        }

        void GenerateVisualContent(MeshGenerationContext obj)
        {
            if (m_Texture == null)
            {
                RegenerateTexture();
            }
            Quad(contentRect.position, contentRect.size, Color.white, m_Texture, obj);
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
                uv = new Vector2(0, 1)
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x1, y0, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1, 1)
            });
            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x0, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(0, 0)
            });

            mesh.SetNextVertex(new Vertex()
            {
                position = new Vector3(x1, y1, Vertex.nearZ),
                tint = color,
                uv = new Vector2(1, 0)
            });

            mesh.SetAllIndices(k_Indices);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            RegenerateTexture();
        }

        void RegenerateTexture()
        {
            if (m_BaseTexture == null)
                m_BaseTexture = resolvedStyle.backgroundImage.texture;

            if (m_BaseTexture == null)
                return;

            if (m_Texture != null)
            {
                if (m_Texture.width != m_BaseTexture.width || m_Texture.height != m_BaseTexture.height)
                    UnityEngine.Object.DestroyImmediate(m_Texture);
            }

            m_Texture = new Texture2D(m_BaseTexture.width, m_BaseTexture.height, TextureFormat.RGBA32, false, true);
            m_Texture.name = "UsageDial Generated";
            m_Texture.wrapMode = TextureWrapMode.Clamp;
            m_Texture.filterMode = FilterMode.Point;
            m_Texture.hideFlags = HideFlags.HideAndDontSave;

            var rawTexture = m_Texture.GetRawTextureData<Color32>();
            var rawBaseTexture = m_BaseTexture.GetRawTextureData<Color32>();

            unsafe
            {
                var ptr = rawTexture.GetUnsafePtr();
                var ptr2 = rawBaseTexture.GetUnsafePtr();
                UnsafeUtility.MemCpy(ptr, ptr2, rawTexture.Length * UnsafeUtility.SizeOf<Color32>());

                Color32* c = (Color32*)ptr;
                for (int i = 0; i < rawTexture.Length; ++i, ++c)
                {
                    var a = c->a;
                    if (a <= 0)
                        continue;

                    if (c->r > m_ValueCutoff)
                        a = (byte)(a / 2);

                    if (c->r > m_ThresholdRed)
                        *c = k_Red;
                    else if (c->r > m_ThresholdYellow)
                        *c = k_Yellow;
                    else
                        *c = k_Green;

                    c->a = a;
                }
            }
            m_Texture.Apply(false, true);
        }
    }
}
