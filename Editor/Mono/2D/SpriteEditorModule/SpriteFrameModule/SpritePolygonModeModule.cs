// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    [RequireSpriteDataProvider(typeof(ISpriteOutlineDataProvider), typeof(ITextureDataProvider))]
    internal partial class SpritePolygonModeModule : SpriteFrameModuleBase
    {
        List<List<Vector2[]>> m_Outline;

        public SpritePolygonModeModule(ISpriteEditor sw, IEventSystem es, IUndoSystem us, IAssetDatabase ad) :
            base("Sprite Polygon Mode Editor", sw, es, us, ad)
        {}

        // ISpriteEditorModule implemenation
        public override void OnModuleActivate()
        {
            base.OnModuleActivate();
            m_Outline = new List<List<Vector2[]>>();

            for (int i = 0; i < m_RectsCache.spriteRects.Count; ++i)
            {
                var rect = m_RectsCache.spriteRects[i];
                m_Outline.Add(spriteEditor.GetDataProvider<ISpriteOutlineDataProvider>().GetOutlines(rect.spriteID));
            }

            showChangeShapeWindow = polygonSprite;
            if (polygonSprite)
                DeterminePolygonSides();
        }

        public override bool CanBeActivated()
        {
            return SpriteUtility.GetSpriteImportMode(spriteEditor.GetDataProvider<ISpriteEditorDataProvider>()) == SpriteImportMode.Polygon;
        }

        private bool polygonSprite
        {
            get { return spriteImportMode == SpriteImportMode.Polygon; }
        }

        private void DeterminePolygonSides()
        {
            if (polygonSprite && m_RectsCache.spriteRects.Count == 1 && m_Outline.Count == 1 && m_Outline[0].Count == 1)
            {
                polygonSides = m_Outline[0][0].Length;
            }
            else
                // If for reasons we cannot determine the sides of the polygon, fall back to 0 (Square)
                polygonSides = 0;
        }

        public int GetPolygonSideCount()
        {
            DeterminePolygonSides();
            return polygonSides;
        }

        public int polygonSides
        {
            get;
            set;
        }

        public List<Vector2[]> GetSpriteOutlineAt(int i)
        {
            return m_Outline[i];
        }

        public void GeneratePolygonOutline()
        {
            for (int i = 0; i < m_RectsCache.spriteRects.Count; i++)
            {
                SpriteRect currentRect = m_RectsCache.spriteRects[i];

                var result = UnityEditor.Sprites.SpriteUtility.GeneratePolygonOutlineVerticesOfSize(polygonSides, (int)currentRect.rect.width, (int)currentRect.rect.height);

                m_Outline.Clear();
                var newOutlineList = new List<Vector2[]>();
                newOutlineList.Add(result);
                m_Outline.Add(newOutlineList);

                spriteEditor.SetDataModified();
            }
            Repaint();
        }
    }
}
