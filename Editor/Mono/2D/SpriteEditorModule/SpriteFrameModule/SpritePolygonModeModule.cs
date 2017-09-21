// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal partial class SpritePolygonModeModule : SpriteFrameModuleBase, ISpriteEditorModule
    {
        public SpritePolygonModeModule(ISpriteEditor sw, IEventSystem es, IUndoSystem us, IAssetDatabase ad) :
            base("Sprite Polygon Mode Editor", sw, es, us, ad)
        {}

        // ISpriteEditorModule implemenation
        public override void OnModuleActivate()
        {
            base.OnModuleActivate();
            m_RectsCache = spriteEditor.spriteRects;
            showChangeShapeWindow = polygonSprite;
            if (polygonSprite)
                DeterminePolygonSides();
        }

        public override void OnModuleDeactivate()
        {
            m_RectsCache = null;
        }

        public override bool CanBeActivated()
        {
            return SpriteUtility.GetSpriteImportMode(spriteEditor.spriteEditorDataProvider) == SpriteImportMode.Polygon;
        }

        private bool polygonSprite
        {
            get { return spriteImportMode == SpriteImportMode.Polygon; }
        }

        private void DeterminePolygonSides()
        {
            if (polygonSprite && m_RectsCache.Count == 1)
            {
                SpriteRect sr = m_RectsCache.RectAt(0);
                if (sr.outline.Count == 1)
                    polygonSides = sr.outline[0].Count;
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

        public void GeneratePolygonOutline()
        {
            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                SpriteRect currentRect = m_RectsCache.RectAt(i);

                SpriteOutline newOutline = new SpriteOutline();
                newOutline.AddRange(UnityEditor.Sprites.SpriteUtility.GeneratePolygonOutlineVerticesOfSize(polygonSides, (int)currentRect.rect.width, (int)currentRect.rect.height));

                currentRect.outline.Clear();
                currentRect.outline.Add(newOutline);

                spriteEditor.SetDataModified();
            }
            Repaint();
        }
    }
}
