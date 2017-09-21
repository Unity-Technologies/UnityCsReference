// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor
{
    internal partial class SpriteFrameModule : SpriteFrameModuleBase
    {
        public enum AutoSlicingMethod
        {
            DeleteAll = 0,
            Smart = 1,
            Safe = 2
        }

        private bool[] m_AlphaPixelCache;
        private const int kDefaultColliderAlphaCutoff = 254;
        private const float kDefaultColliderDetail = 0.25f;

        public SpriteFrameModule(ISpriteEditor sw, IEventSystem es, IUndoSystem us, IAssetDatabase ad) :
            base("Sprite Editor", sw, es, us, ad)
        {}

        public override void OnModuleActivate()
        {
            base.OnModuleActivate();
            m_RectsCache = spriteEditor.spriteRects;
            spriteEditor.enableMouseMoveEvent = true;
        }

        public override void OnModuleDeactivate()
        {
            m_RectsCache = null;
        }

        public override bool CanBeActivated()
        {
            return SpriteUtility.GetSpriteImportMode(spriteEditor.spriteEditorDataProvider) != SpriteImportMode.Polygon;
        }

        private string GetUniqueName(string prefix, int startIndex)
        {
            while (true)
            {
                var name = prefix + "_" + startIndex++;

                var nameUsed = false;
                for (int i = 0; i < m_RectsCache.Count; ++i)
                {
                    if (m_RectsCache.RectAt(i).name == name)
                    {
                        nameUsed = true;
                        break;
                    }
                }

                if (!nameUsed)
                    return name;
            }
        }

        // Aurajoki-Sweep Rect Sorting(tm)
        // 1. Find top-most rectangle
        // 2. Sweep it vertically to find out all rects from that "row"
        // 3. goto 1.
        // This will give us nicely sorted left->right top->down list of rectangles
        // Works for most sprite sheets pretty nicely
        private List<Rect> SortRects(List<Rect> rects)
        {
            List<Rect> result = new List<Rect>();

            while (rects.Count > 0)
            {
                // Because the slicing algorithm works from bottom-up, the topmost rect is the last one in the array
                Rect r = rects[rects.Count - 1];
                Rect sweepRect = new Rect(0, r.yMin, previewTexture.width, r.height);

                List<Rect> rowRects = RectSweep(rects, sweepRect);

                if (rowRects.Count > 0)
                    result.AddRange(rowRects);
                else
                {
                    // We didn't find any rects, just dump the remaining rects and continue
                    result.AddRange(rects);
                    break;
                }
            }
            return result;
        }

        private List<Rect> RectSweep(List<Rect> rects, Rect sweepRect)
        {
            if (rects == null || rects.Count == 0)
                return new List<Rect>();

            List<Rect> containedRects = new List<Rect>();

            foreach (Rect rect in rects)
            {
                if (rect.Overlaps(sweepRect))
                    containedRects.Add(rect);
            }

            // Remove found rects from original list
            foreach (Rect rect in containedRects)
                rects.Remove(rect);

            // Sort found rects by x position
            containedRects.Sort((a, b) => a.x.CompareTo(b.x));

            return containedRects;
        }

        private void AddSprite(Rect frame, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod, ref int index)
        {
            if (slicingMethod != AutoSlicingMethod.DeleteAll)
            {
                // Smart: Whenever we overlap, we just modify the existing rect and keep its other properties
                // Safe: We only add new rect if it doesn't overlap existing one

                SpriteRect existingSprite = GetExistingOverlappingSprite(frame);
                if (existingSprite != null)
                {
                    if (slicingMethod == AutoSlicingMethod.Smart)
                    {
                        existingSprite.rect = frame;
                        existingSprite.alignment = (SpriteAlignment)alignment;
                        existingSprite.pivot = pivot;
                    }
                }
                else
                    AddSpriteWithUniqueName(frame, alignment, pivot, kDefaultColliderAlphaCutoff, kDefaultColliderDetail, index++);
            }
            else
                AddSprite(frame, alignment, pivot, kDefaultColliderAlphaCutoff, kDefaultColliderDetail, GetSpriteNamePrefix() + "_" + index++);
        }

        private SpriteRect GetExistingOverlappingSprite(Rect rect)
        {
            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                Rect existingRect = m_RectsCache.RectAt(i).rect;
                if (existingRect.Overlaps(rect))
                    return m_RectsCache.RectAt(i);
            }
            return null;
        }

        private bool PixelHasAlpha(int x, int y, ITexture2D texture)
        {
            if (m_AlphaPixelCache == null)
            {
                m_AlphaPixelCache = new bool[texture.width * texture.height];
                Color32[] pixels = texture.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                    m_AlphaPixelCache[i] = pixels[i].a != 0;
            }
            int index = y * (int)texture.width + x;
            return m_AlphaPixelCache[index];
        }

        private SpriteRect AddSprite(Rect rect, int alignment, Vector2 pivot, int colliderAlphaCutoff, float colliderDetail, string name)
        {
            SpriteRect spriteRect = new SpriteRect();

            spriteRect.rect = rect;
            spriteRect.alignment = (SpriteAlignment)alignment;
            spriteRect.pivot = pivot;

            spriteRect.name = name;
            spriteRect.originalName = spriteRect.name;
            spriteRect.border = Vector4.zero;
            spriteEditor.SetDataModified();

            m_RectsCache.AddRect(spriteRect);
            spriteEditor.SetDataModified();

            return spriteRect;
        }

        public SpriteRect AddSpriteWithUniqueName(Rect rect, int alignment, Vector2 pivot, int colliderAlphaCutoff, float colliderDetail, int nameIndexingHint)
        {
            string name = GetUniqueName(GetSpriteNamePrefix(), nameIndexingHint);
            return AddSprite(rect, alignment, pivot, colliderAlphaCutoff, colliderDetail, name);
        }

        private string GetSpriteNamePrefix()
        {
            return Path.GetFileNameWithoutExtension(spriteAssetPath);
        }

        public void DoAutomaticSlicing(int minimumSpriteSize, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod)
        {
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Automatic Slicing");

            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.ClearAll();

            var textureToUse = spriteEditor.GetReadableTexture2D();
            List<Rect> frames = new List<Rect>(InternalSpriteUtility.GenerateAutomaticSpriteRectangles((UnityTexture2D)textureToUse, minimumSpriteSize, 0));
            frames = SortRects(frames);
            int index = 0;

            foreach (Rect frame in frames)
                AddSprite(frame, alignment, pivot, slicingMethod, ref index);

            selected = null;
            spriteEditor.SetDataModified();
            Repaint();
        }

        public void DoGridSlicing(Vector2 size, Vector2 offset, Vector2 padding, int alignment, Vector2 pivot)
        {
            var textureToUse = spriteEditor.GetReadableTexture2D();
            Rect[] frames = InternalSpriteUtility.GenerateGridSpriteRectangles((UnityTexture2D)textureToUse, offset, size, padding);

            int index = 0;
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Grid Slicing");
            m_RectsCache.ClearAll();

            foreach (Rect frame in frames)
                AddSprite(frame, alignment, pivot, kDefaultColliderAlphaCutoff, kDefaultColliderDetail, GetSpriteNamePrefix() + "_" + index++);

            selected = null;
            spriteEditor.SetDataModified();
            Repaint();
        }

        public void ScaleSpriteRect(Rect r)
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Scale sprite");
                selected.rect = ClampSpriteRect(r, previewTexture.width, previewTexture.height);
                selected.border = ClampSpriteBorderToRect(selected.border, selected.rect);
                spriteEditor.SetDataModified();
            }
        }

        public void TrimAlpha()
        {
            var texture = spriteEditor.GetReadableTexture2D();
            if (texture == null)
                return;

            Rect rect = selected.rect;

            int xMin = (int)rect.xMax;
            int xMax = (int)rect.xMin;
            int yMin = (int)rect.yMax;
            int yMax = (int)rect.yMin;

            for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
            {
                for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                {
                    if (PixelHasAlpha(x, y, texture))
                    {
                        xMin = Mathf.Min(xMin, x);
                        xMax = Mathf.Max(xMax, x);
                        yMin = Mathf.Min(yMin, y);
                        yMax = Mathf.Max(yMax, y);
                    }
                }
            }
            // Case 582309: Return an empty rectangle if no pixel has an alpha
            if (xMin > xMax || yMin > yMax)
                rect = new Rect(0, 0, 0, 0);
            else
                rect = new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);

            if (rect.width <= 0 && rect.height <= 0)
            {
                m_RectsCache.RemoveRect(selected);
                spriteEditor.SetDataModified();
                selected = null;
            }
            else
            {
                rect = ClampSpriteRect(rect, texture.width, texture.height);
                if (selected.rect != rect)
                    spriteEditor.SetDataModified();

                selected.rect = rect;
            }
        }

        public void DuplicateSprite()
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Duplicate sprite");
                selected = AddSpriteWithUniqueName(selected.rect, (int)selected.alignment, selected.pivot, kDefaultColliderAlphaCutoff, kDefaultColliderDetail, 0);
            }
        }

        public void CreateSprite(Rect rect)
        {
            rect = ClampSpriteRect(rect, previewTexture.width, previewTexture.height);
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Create sprite");
            selected = AddSpriteWithUniqueName(rect, 0, Vector2.zero, kDefaultColliderAlphaCutoff, kDefaultColliderDetail, 0);
        }

        public void DeleteSprite()
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Delete sprite");
                m_RectsCache.RemoveRect(selected);
                selected = null;
                spriteEditor.SetDataModified();
            }
        }
    }
}
