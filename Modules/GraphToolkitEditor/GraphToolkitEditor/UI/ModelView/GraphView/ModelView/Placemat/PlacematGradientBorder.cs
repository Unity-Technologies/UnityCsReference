// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class to draw the placemat border
    /// </summary>
    [UnityRestricted]
    internal class PlacematGradientBorder : ImmediateModeElement
    {
        const string k_PlacematborderShaderPath = "Shaders/GraphToolkit/PlacematBorder.shader";
        static Shader s_Shader = EditorGUIUtility.LoadRequired(k_PlacematborderShaderPath) as Shader;
        static Mesh s_Mesh;

        Material m_Mat;

        /// <summary>
        /// Instantiate the <see cref="PlacematGradientBorder"/> class.
        /// </summary>
        public PlacematGradientBorder()
        {
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void OnAttach(AttachToPanelEvent e)
        {
            m_Mat = new Material(s_Shader);
            m_Mat.hideFlags = HideFlags.HideAndDontSave;
            m_Mat.SetColor(k_ColorLightID, m_LightColor);
            m_Mat.SetColor(k_ColorDarkID, m_DarkColor);
        }

        void OnDetach(DetachFromPanelEvent e)
        {
            UnityObject.DestroyImmediate(m_Mat);
            m_Mat = null;
        }

        static readonly int k_ColorLightID = Shader.PropertyToID("_ColorLight");
        static readonly int k_ColorDarkID = Shader.PropertyToID("_ColorDark");
        static readonly int k_BorderID = Shader.PropertyToID("_Border");
        static readonly int k_RadiusID = Shader.PropertyToID("_Radius");
        static readonly int k_SizeID = Shader.PropertyToID("_Size");


        Color m_LightColor;
        Color m_DarkColor;

        /// <summary>
        /// The Light color for the gradient.
        /// </summary>
        public Color LightColor
        {
            get { return m_LightColor; }
            set
            {
                m_LightColor = value;
                m_Mat?.SetColor(k_ColorLightID, value);
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The dark color for the gradient.
        /// </summary>
        public Color DarkColor
        {
            get { return m_DarkColor; }
            set
            {
                m_DarkColor = value;
                m_Mat?.SetColor(k_ColorDarkID, value);
                MarkDirtyRepaint();
            }
        }

        static void CreateMesh()
        {
            if (s_Mesh == null)
            {
                s_Mesh = new Mesh();
                var verticesCount = 28;

                var vertices = new Vector3[verticesCount];
                var uvsBorder = new Vector2[verticesCount];

                for (var ix = 0; ix < 4; ++ix)
                {
                    for (var iy = 0; iy < 4; ++iy)
                    {
                        vertices[ix + iy * 4] = new Vector3(ix < 2 ? -1 : 1, iy < 2 ? -1 : 1, 0);
                        uvsBorder[ix + iy * 4] = new Vector2(ix == 0 || ix == 3 ? 1 : 0, iy == 0 || iy == 3 ? 1 : 0);
                    }
                }

                vertices[16] = vertices[0];
                vertices[17] = vertices[0];
                vertices[18] = vertices[0];
                vertices[19] = vertices[2];
                vertices[20] = vertices[2];
                vertices[21] = vertices[2];
                vertices[22] = vertices[10];
                vertices[23] = vertices[10];
                vertices[24] = vertices[10];
                vertices[25] = vertices[8];
                vertices[26] = vertices[8];
                vertices[27] = vertices[8];

                for (var i = 0; i < 4; i += 1)
                {
                    uvsBorder[16 + i * 3] = new Vector2(0, -1);
                    uvsBorder[17 + i * 3] = new Vector2(-1, -1);
                    uvsBorder[18 + i * 3] = new Vector2(-1, 0);
                }

                var indices = new int[4 * 12];

                for (var ix = 0; ix < 3; ++ix)
                {
                    for (var iy = 0; iy < 3; ++iy)
                    {
                        var quadIndex = (ix + iy * 3);
                        if (quadIndex == 4)
                            continue;
                        else if (quadIndex > 4)
                            --quadIndex;
                        var vertIndex = quadIndex * 4;

                        indices[vertIndex] = ix + iy * 4;
                        indices[vertIndex + 1] = ix + (iy + 1) * 4;
                        indices[vertIndex + 2] = ix + 1 + (iy + 1) * 4;
                        indices[vertIndex + 3] = ix + 1 + iy * 4;
                    }
                }

                indices[32] = 5;
                indices[33] = 16;
                indices[34] = 17;
                indices[35] = 18;

                indices[36] = 6;
                indices[37] = 21;
                indices[38] = 20;
                indices[39] = 19;

                indices[40] = 9;
                indices[41] = 27;
                indices[42] = 26;
                indices[43] = 25;

                indices[44] = 10;
                indices[45] = 22;
                indices[46] = 23;
                indices[47] = 24;

                s_Mesh.vertices = vertices;
                s_Mesh.uv = uvsBorder;
                s_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }
        }

        protected override void ImmediateRepaint()
        {
            CreateMesh();

            float radius = resolvedStyle.borderTopLeftRadius;
            float border = resolvedStyle.borderLeftWidth;

            m_Mat.SetFloat(k_BorderID, border);
            m_Mat.SetFloat(k_RadiusID, radius - border);

            var size = new Vector4(layout.width * .5f - border, layout.height * 0.5f - border, 0, 0);
            m_Mat.SetVector(k_SizeID, size);
            m_Mat.SetPass(0);

            Graphics.DrawMeshNow(s_Mesh, Matrix4x4.Translate(new Vector3(size.x + border, size.y + border, 0)));
        }
    }
}
