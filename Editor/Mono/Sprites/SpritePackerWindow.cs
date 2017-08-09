// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Sprites
{
    internal class PackerWindow : SpriteUtilityWindow
    {
        private class PackerWindowStyle
        {
            public static readonly GUIContent packLabel = EditorGUIUtility.TextContent("Pack");
            public static readonly GUIContent repackLabel = EditorGUIUtility.TextContent("Repack");
            public static readonly GUIContent viewAtlasLabel = EditorGUIUtility.TextContent("View Atlas:");
            public static readonly GUIContent windowTitle = EditorGUIUtility.TextContent("Sprite Packer");
            public static readonly GUIContent pageContentLabel = EditorGUIUtility.TextContent("Page {0}");
            public static readonly GUIContent packingDisabledLabel = EditorGUIUtility.TextContent("Legacy sprite packing is disabled. Enable it in Edit > Project Settings > Editor.");
            public static readonly GUIContent openProjectSettingButton = EditorGUIUtility.TextContent("Open Project Editor Settings");
        }

        struct Edge
        {
            public UInt16 v0;
            public UInt16 v1;
            public Edge(UInt16 a, UInt16 b)
            {
                v0 = a;
                v1 = b;
            }

            public override bool Equals(object obj)
            {
                Edge item = (Edge)obj;
                return (v0 == item.v0 && v1 == item.v1) || (v0 == item.v1 && v1 == item.v0);
            }

            public override int GetHashCode()
            {
                return (v0 << 16 | v1) ^ (v1 << 16 | v0).GetHashCode();
            }
        };

        private static string[] s_AtlasNamesEmpty = new string[1] { "Sprite atlas cache is empty" };
        private string[] m_AtlasNames = s_AtlasNamesEmpty;
        private int m_SelectedAtlas = 0;

        private static string[] s_PageNamesEmpty = new string[0];
        private string[] m_PageNames = s_PageNamesEmpty;
        private int m_SelectedPage = 0;

        private Sprite m_SelectedSprite = null;


        void OnEnable()
        {
            minSize = new Vector2(400f, 256f);
            titleContent = PackerWindowStyle.windowTitle;

            Reset();
        }

        private void Reset()
        {
            RefreshAtlasNameList();
            RefreshAtlasPageList();

            m_SelectedAtlas = 0;
            m_SelectedPage = 0;
            m_SelectedSprite = null;
        }

        private void RefreshAtlasNameList()
        {
            m_AtlasNames = Packer.atlasNames;

            // Validate
            if (m_SelectedAtlas >= m_AtlasNames.Length)
                m_SelectedAtlas = 0;
        }

        private void RefreshAtlasPageList()
        {
            if (m_AtlasNames.Length > 0)
            {
                string atlas = m_AtlasNames[m_SelectedAtlas];
                Texture2D[] textures = Packer.GetTexturesForAtlas(atlas);
                m_PageNames = new string[textures.Length];
                for (int i = 0; i < textures.Length; ++i)
                    m_PageNames[i] = string.Format(PackerWindowStyle.pageContentLabel.text, i + 1);
            }
            else
            {
                m_PageNames = s_PageNamesEmpty;
            }

            // Validate
            if (m_SelectedPage >= m_PageNames.Length)
                m_SelectedPage = 0;
        }

        private void OnAtlasNameListChanged()
        {
            if (m_AtlasNames.Length > 0)
            {
                string[] atlasNames = Packer.atlasNames;
                string curAtlasName = m_AtlasNames[m_SelectedAtlas];
                string newAtlasName = (atlasNames.Length <= m_SelectedAtlas) ? null : atlasNames[m_SelectedAtlas];
                if (curAtlasName.Equals(newAtlasName))
                {
                    RefreshAtlasNameList();
                    RefreshAtlasPageList();
                    m_SelectedSprite = null;
                    return;
                }
            }

            Reset();
        }

        private bool ValidateIsPackingEnabled()
        {
            if (EditorSettings.spritePackerMode != SpritePackerMode.BuildTimeOnly
                && EditorSettings.spritePackerMode != SpritePackerMode.AlwaysOn)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label(PackerWindowStyle.packingDisabledLabel);
                if (GUILayout.Button(PackerWindowStyle.openProjectSettingButton))
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings/Editor");
                EditorGUILayout.EndVertical();
                return false;
            }

            return true;
        }

        private Rect DoToolbarGUI()
        {
            Rect toolbarRect = new Rect(0 , 0, position.width, k_ToolbarHeight);

            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(toolbarRect, false, false, false, false);
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_AtlasNames.Length > 0;
            toolbarRect = DoAlphaZoomToolbarGUI(toolbarRect);
            GUI.enabled = wasEnabled;

            Rect drawRect = new Rect(EditorGUI.kSpacing, 0, 0, k_ToolbarHeight);
            toolbarRect.width -= drawRect.x;

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                drawRect.width = EditorStyles.toolbarButton.CalcSize(PackerWindowStyle.packLabel).x;
                DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawRect) =>
                    {
                        if (GUI.Button(adjustedDrawRect, PackerWindowStyle.packLabel, EditorStyles.toolbarButton))
                        {
                            Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget, true);
                            m_SelectedSprite = null;
                            RefreshAtlasPageList();
                            RefreshState();
                        }
                    });

                using (new EditorGUI.DisabledScope(Packer.SelectedPolicy == Packer.kDefaultPolicy))
                {
                    drawRect.x += drawRect.width;
                    drawRect.width = EditorStyles.toolbarButton.CalcSize(PackerWindowStyle.repackLabel).x;
                    DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawRect) =>
                        {
                            if (GUI.Button(adjustedDrawRect, PackerWindowStyle.repackLabel, EditorStyles.toolbarButton))
                            {
                                Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget, true, Packer.Execution.ForceRegroup);
                                m_SelectedSprite = null;
                                RefreshAtlasPageList();
                                RefreshState();
                            }
                        });
                }
            }

            const float kAtlasNameWidth = 100;
            const float kPagesWidth = 70;
            const float kPolicyWidth = 100;

            float viewAtlasWidth = GUI.skin.label.CalcSize(PackerWindowStyle.viewAtlasLabel).x;
            float totalWidth = viewAtlasWidth + kAtlasNameWidth + kPagesWidth + kPolicyWidth;

            drawRect.x += EditorGUI.kSpacing; // leave some space from previous control for cosmetic
            toolbarRect.width -= EditorGUI.kSpacing;
            float availableWidth = toolbarRect.width;

            using (new EditorGUI.DisabledScope(m_AtlasNames.Length == 0))
            {
                drawRect.x += drawRect.width;
                drawRect.width = viewAtlasWidth / totalWidth * availableWidth;
                DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        GUI.Label(adjustedDrawArea, PackerWindowStyle.viewAtlasLabel);
                    });

                EditorGUI.BeginChangeCheck();
                drawRect.x += drawRect.width;
                drawRect.width = kAtlasNameWidth / totalWidth * availableWidth;
                DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        m_SelectedAtlas = EditorGUI.Popup(adjustedDrawArea, m_SelectedAtlas, m_AtlasNames, EditorStyles.toolbarPopup);
                    });
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshAtlasPageList();
                    m_SelectedSprite = null;
                }

                EditorGUI.BeginChangeCheck();
                drawRect.x += drawRect.width;
                drawRect.width = kPagesWidth / totalWidth * availableWidth;
                DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        m_SelectedPage = EditorGUI.Popup(adjustedDrawArea, m_SelectedPage, m_PageNames, EditorStyles.toolbarPopup);
                    });

                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedSprite = null;
                }
            }

            EditorGUI.BeginChangeCheck();
            string[] policies = Packer.Policies;
            int selectedPolicy = Array.IndexOf(policies, Packer.SelectedPolicy);
            drawRect.x += drawRect.width;
            drawRect.width = kPolicyWidth / totalWidth * availableWidth;
            DrawToolBarWidget(ref drawRect, ref toolbarRect, (adjustedDrawArea) =>
                {
                    selectedPolicy = EditorGUI.Popup(adjustedDrawArea, selectedPolicy, policies, EditorStyles.toolbarPopup);
                });

            if (EditorGUI.EndChangeCheck())
            {
                Packer.SelectedPolicy = policies[selectedPolicy];
            }

            return toolbarRect;
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject == null)
                return;

            Sprite selectedSprite = Selection.activeObject as Sprite;
            if (selectedSprite != m_SelectedSprite)
            {
                if (selectedSprite != null)
                {
                    string selAtlasName;
                    Texture2D selAtlasTexture;
                    Packer.GetAtlasDataForSprite(selectedSprite, out selAtlasName, out selAtlasTexture);

                    int selAtlasIndex = m_AtlasNames.ToList().FindIndex(delegate(string s) { return selAtlasName == s; });
                    if (selAtlasIndex == -1)
                        return;
                    int selAtlasPage = Packer.GetTexturesForAtlas(selAtlasName).ToList().FindIndex(delegate(Texture2D t) { return selAtlasTexture == t; });
                    if (selAtlasPage == -1)
                        return;

                    m_SelectedAtlas = selAtlasIndex;
                    m_SelectedPage = selAtlasPage;
                    RefreshAtlasPageList();
                }

                m_SelectedSprite = selectedSprite;

                Repaint();
            }
        }

        private void RefreshState()
        {
            // Check if atlas name list changed
            string[] atlasNames = Packer.atlasNames;
            if (!atlasNames.SequenceEqual(m_AtlasNames))
            {
                if (atlasNames.Length == 0)
                {
                    Reset();
                    return;
                }
                else
                {
                    OnAtlasNameListChanged();
                }
            }

            if (m_AtlasNames.Length == 0)
            {
                SetNewTexture(null);
                return;
            }

            // Validate selections
            if (m_SelectedAtlas >= m_AtlasNames.Length)
                m_SelectedAtlas = 0;
            string curAtlasName = m_AtlasNames[m_SelectedAtlas];

            Texture2D[] textures = Packer.GetTexturesForAtlas(curAtlasName);
            if (m_SelectedPage >= textures.Length)
                m_SelectedPage = 0;

            SetNewTexture(textures[m_SelectedPage]);

            // check if the atlas has alpha as an external texture (as in ETC1 atlases with alpha)
            Texture2D[] alphaTextures = Packer.GetAlphaTexturesForAtlas(curAtlasName);
            Texture2D selectedAlphaTexture = (m_SelectedPage < alphaTextures.Length) ? alphaTextures[m_SelectedPage] : null;
            SetAlphaTextureOverride(selectedAlphaTexture);
        }

        public void OnGUI()
        {
            if (!ValidateIsPackingEnabled())
                return;

            Matrix4x4 oldHandlesMatrix = Handles.matrix;
            InitStyles();

            RefreshState();

            // Top menu bar
            Rect toolbarRect = DoToolbarGUI();

            if (m_Texture == null)
                return;

            // Texture view
            EditorGUILayout.BeginHorizontal();
            m_TextureViewRect = new Rect(0f, toolbarRect.yMax, position.width - k_ScrollbarMargin, position.height - k_ScrollbarMargin - toolbarRect.height);
            GUILayout.FlexibleSpace();
            DoTextureGUI();
            string info = string.Format("{1}x{2}, {0}", TextureUtil.GetTextureFormatString(m_Texture.format), m_Texture.width, m_Texture.height);
            EditorGUI.DropShadowLabel(new Rect(m_TextureViewRect.x, m_TextureViewRect.y + 10, m_TextureViewRect.width, 20), info);
            EditorGUILayout.EndHorizontal();

            Handles.matrix = oldHandlesMatrix;
        }

        private void DrawLineUtility(Vector2 from, Vector2 to)
        {
            SpriteEditorUtility.DrawLine(new Vector3(from.x * m_Texture.width + 1f / m_Zoom, from.y * m_Texture.height + 1f / m_Zoom, 0.0f), new Vector3(to.x * m_Texture.width + 1f / m_Zoom, to.y * m_Texture.height + 1f / m_Zoom, 0.0f));
        }

        private Edge[] FindUniqueEdges(UInt16[] indices)
        {
            Edge[] allEdges = new Edge[indices.Length];
            int tris = indices.Length / 3;
            for (int i = 0; i < tris; ++i)
            {
                allEdges[i * 3] = new Edge(indices[i * 3], indices[i * 3 + 1]);
                allEdges[i * 3 + 1] = new Edge(indices[i * 3 + 1], indices[i * 3 + 2]);
                allEdges[i * 3 + 2] = new Edge(indices[i * 3 + 2], indices[i * 3]);
            }

            Edge[] uniqueEdges = allEdges.GroupBy(x => x).Where(x => x.Count() == 1).Select(x => x.First()).ToArray();
            return uniqueEdges;
        }

        protected override void DrawGizmos()
        {
            if (m_SelectedSprite != null && m_Texture != null)
            {
                Vector2[] uvs = SpriteUtility.GetSpriteUVs(m_SelectedSprite, true);
                UInt16[] indices = m_SelectedSprite.triangles;
                Edge[] uniqueEdges = FindUniqueEdges(indices); // Assumes that our mesh has no duplicate vertices

                SpriteEditorUtility.BeginLines(new Color(0.3921f, 0.5843f, 0.9294f, 0.75f)); // Cornflower blue :)
                foreach (Edge e in uniqueEdges)
                    DrawLineUtility(uvs[e.v0], uvs[e.v1]);
                SpriteEditorUtility.EndLines();
            }
        }
    } // class
}
