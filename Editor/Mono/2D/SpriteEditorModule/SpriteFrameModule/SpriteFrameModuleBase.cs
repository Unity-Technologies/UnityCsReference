// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal abstract partial class SpriteFrameModuleBase : ISpriteEditorModule
    {
        protected ISpriteRectCache m_RectsCache;

        protected SpriteFrameModuleBase(string name, ISpriteEditor sw, IEventSystem es, IUndoSystem us, IAssetDatabase ad)
        {
            spriteEditor = sw;
            eventSystem = es;
            undoSystem = us;
            assetDatabase = ad;
            moduleName = name;
        }

        // implements ISpriteEditorModule
        public abstract bool CanBeActivated();

        public virtual void OnModuleActivate()
        {
            spriteImportMode = SpriteUtility.GetSpriteImportMode(spriteEditor.spriteEditorDataProvider);
        }

        public abstract void OnModuleDeactivate();

        public string moduleName
        {
            get;
            private set;
        }

        // injected interfaces
        protected IEventSystem eventSystem
        {
            get;
            private set;
        }

        protected IUndoSystem undoSystem
        {
            get;
            private set;
        }

        protected ISpriteEditor spriteEditor
        {
            get;
            private set;
        }

        protected IAssetDatabase assetDatabase
        {
            get;
            private set;
        }

        protected SpriteRect selected
        {
            get { return spriteEditor.selectedSpriteRect; }
            set { spriteEditor.selectedSpriteRect = value; }
        }

        protected SpriteImportMode spriteImportMode
        {
            get; private set;
        }

        protected string spriteAssetPath
        {
            get { return assetDatabase.GetAssetPath(spriteEditor.selectedTexture); }
        }

        protected ITexture2D previewTexture
        {
            get { return spriteEditor.previewTexture; }
        }

        public bool hasSelected
        {
            get { return spriteEditor.selectedSpriteRect != null; }
        }

        public SpriteAlignment selectedSpriteAlignment
        {
            get { return selected.alignment; }
        }

        public Vector2 selectedSpritePivot
        {
            get { return selected.pivot; }
        }

        public int CurrentSelectedSpriteIndex()
        {
            for (int i = 0; i < m_RectsCache.Count; ++i)
            {
                if (m_RectsCache.RectAt(i) == selected)
                    return i;
            }
            return -1;
        }

        public Vector4 selectedSpriteBorder
        {
            get { return ClampSpriteBorderToRect(selected.border, selected.rect); }
            set
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Change Sprite Border");
                spriteEditor.SetDataModified();
                selected.border = ClampSpriteBorderToRect(value, selected.rect);
            }
        }

        public Rect selectedSpriteRect
        {
            get { return selected.rect; }
            set
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Change Sprite rect");
                spriteEditor.SetDataModified();
                selected.rect = ClampSpriteRect(value, previewTexture.width, previewTexture.height);
            }
        }

        public string selectedSpriteName
        {
            get { return selected.name; }
            set
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Change Sprite Name");
                spriteEditor.SetDataModified();

                string oldName = selected.name;
                string newName = InternalEditorUtility.RemoveInvalidCharsFromFileName(value, true);

                // These can only be changed in sprite multiple mode
                if (string.IsNullOrEmpty(selected.originalName) && (newName != oldName))
                    selected.originalName = oldName;

                // Is the name empty?
                if (string.IsNullOrEmpty(newName))
                    newName = oldName;

                // newName have to be unique. Multiple sprite assets sharing the same name will create problems with animations etc.
                for (int i = 0; i < m_RectsCache.Count; ++i)
                {
                    if (m_RectsCache.RectAt(i).name == newName)
                    {
                        newName = selected.originalName;
                        break;
                    }
                }
                selected.name = newName;
            }
        }

        public int spriteCount
        {
            get { return m_RectsCache.Count; }
        }

        public Vector4 GetSpriteBorderAt(int i)
        {
            return m_RectsCache.RectAt(i).border;
        }

        public Rect GetSpriteRectAt(int i)
        {
            return m_RectsCache.RectAt(i).rect;
        }

        public List<SpriteOutline> GetSpriteOutlineAt(int i)
        {
            return m_RectsCache.RectAt(i).outline;
        }

        public void SetSpritePivotAndAlignment(Vector2 pivot, SpriteAlignment alignment)
        {
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Change Sprite Pivot");
            spriteEditor.SetDataModified();
            selected.alignment = alignment;
            selected.pivot = SpriteEditorUtility.GetPivotValue(alignment, pivot);
        }

        public bool containsMultipleSprites
        {
            get { return spriteImportMode == SpriteImportMode.Multiple; }
        }

        protected void SnapPivot(Vector2 pivot, out Vector2 outPivot, out SpriteAlignment outAlignment)
        {
            Rect rect = selectedSpriteRect;

            // Convert from normalized space to texture space
            Vector2 texturePos = new Vector2(rect.xMin + rect.width * pivot.x, rect.yMin + rect.height * pivot.y);

            Vector2[] snapPoints = GetSnapPointsArray(rect);

            // Snapping is now a firm action, it will always snap to one of the snapping points.
            SpriteAlignment snappedAlignment = SpriteAlignment.Custom;
            float nearestDistance = float.MaxValue;
            for (int alignment = 0; alignment < snapPoints.Length; alignment++)
            {
                float distance = (texturePos - snapPoints[alignment]).magnitude * m_Zoom;
                if (distance < nearestDistance)
                {
                    snappedAlignment = (SpriteAlignment)alignment;
                    nearestDistance = distance;
                }
            }

            outAlignment = snappedAlignment;
            outPivot = ConvertFromTextureToNormalizedSpace(snapPoints[(int)snappedAlignment], rect);
        }

        protected static Rect ClampSpriteRect(Rect rect, float maxX, float maxY)
        {
            // Clamp rect to width height
            Rect newRect = new Rect();

            newRect.xMin = Mathf.Clamp(rect.xMin, 0, maxX - 1);
            newRect.yMin = Mathf.Clamp(rect.yMin, 0, maxY - 1);
            newRect.xMax = Mathf.Clamp(rect.xMax, 1, maxX);
            newRect.yMax = Mathf.Clamp(rect.yMax, 1, maxY);

            // Prevent width and height to be 0 value after clamping.
            if (Mathf.RoundToInt(newRect.width) == 0)
                newRect.width = 1;
            if (Mathf.RoundToInt(newRect.height) == 0)
                newRect.height = 1;

            return SpriteEditorUtility.RoundedRect(newRect);
        }

        protected static Rect FlipNegativeRect(Rect rect)
        {
            Rect newRect = new Rect();

            newRect.xMin = Mathf.Min(rect.xMin, rect.xMax);
            newRect.yMin = Mathf.Min(rect.yMin, rect.yMax);
            newRect.xMax = Mathf.Max(rect.xMin, rect.xMax);
            newRect.yMax = Mathf.Max(rect.yMin, rect.yMax);

            return newRect;
        }

        protected static Vector4 ClampSpriteBorderToRect(Vector4 border, Rect rect)
        {
            Rect flipRect = FlipNegativeRect(rect);
            float w = flipRect.width;
            float h = flipRect.height;

            Vector4 newBorder = new Vector4();

            // Make sure borders are within the width/height and left < right and top < bottom
            newBorder.x = Mathf.RoundToInt(Mathf.Clamp(border.x, 0, Mathf.Min(Mathf.Abs(w - border.z), w))); // Left
            newBorder.z = Mathf.RoundToInt(Mathf.Clamp(border.z, 0, Mathf.Min(Mathf.Abs(w - newBorder.x), w))); // Right

            newBorder.y = Mathf.RoundToInt(Mathf.Clamp(border.y, 0, Mathf.Min(Mathf.Abs(h - border.w), h))); // Bottom
            newBorder.w = Mathf.RoundToInt(Mathf.Clamp(border.w, 0, Mathf.Min(Mathf.Abs(h - newBorder.y), h))); // Top

            return newBorder;
        }
    }
}
