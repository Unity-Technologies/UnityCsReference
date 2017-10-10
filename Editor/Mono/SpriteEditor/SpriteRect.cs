// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor.Experimental.U2D;
using UnityEditorInternal;

namespace UnityEditor
{
    [Serializable]
    internal class SpriteRect
    {
        [SerializeField]
        string m_Name;

        [SerializeField]
        string m_OriginalName;

        [SerializeField]
        Vector2 m_Pivot;

        [SerializeField]
        SpriteAlignment m_Alignment;

        [SerializeField]
        Vector4 m_Border;

        [SerializeField]
        Rect m_Rect;

        [SerializeField]
        List<SpriteOutline> m_Outline = new List<SpriteOutline>();

        [SerializeField]
        List<SpriteOutline> m_PhysicsShape = new List<SpriteOutline>();

        [SerializeField]
        float m_TessellationDetail; // sprite detail for mesh generation on a per-sprite basis

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string originalName
        {
            get
            {
                if (m_OriginalName == null)
                {
                    m_OriginalName = name;
                }
                return m_OriginalName;
            }

            set { m_OriginalName = value; }
        }


        public Vector2 pivot
        {
            get { return m_Pivot; }
            set { m_Pivot = value; }
        }

        public SpriteAlignment alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        public Vector4 border
        {
            get { return m_Border; }
            set { m_Border = value; }
        }

        public Rect rect
        {
            get { return m_Rect; }
            set { m_Rect = value; }
        }

        public List<SpriteOutline> outline
        {
            get { return m_Outline; }
            set { m_Outline = value; }
        }

        public List<SpriteOutline> physicsShape
        {
            get { return m_PhysicsShape; }
            set { m_PhysicsShape = value; }
        }

        public float tessellationDetail
        {
            get { return m_TessellationDetail; }
            set { m_TessellationDetail = value; }
        }

        static public List<SpriteOutline> AcquireOutline(List<Vector2[]> outlineSP)
        {
            var outline = new List<SpriteOutline>();
            if (outlineSP != null)
            {
                for (int j = 0; j < outlineSP.Count; ++j)
                {
                    SpriteOutline o = new SpriteOutline();
                    o.m_Path.AddRange(outlineSP[j]);
                    outline.Add(o);
                }
            }

            return outline;
        }

        static public List<Vector2[]> ApplyOutlineChanges(List<SpriteOutline> outline)
        {
            var result = new List<Vector2[]>();
            if (outline != null)
            {
                for (int j = 0; j < outline.Count; ++j)
                {
                    result.Add(outline[j].m_Path.ToArray());
                }
            }

            return result;
        }

        public void LoadFromSpriteData(SpriteDataBase sp)
        {
            rect = sp.rect;
            border = sp.border;
            name = sp.name;
            alignment = sp.alignment;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, sp.pivot);
            tessellationDetail = sp.tessellationDetail;
            outline = AcquireOutline(sp.outline);
            physicsShape = AcquireOutline(sp.physicsShape);
        }

        public void ApplyToSpriteData(SpriteDataBase sp)
        {
            sp.rect = rect;
            sp.border = border;
            sp.name = name;
            sp.alignment = alignment;
            sp.pivot = pivot;
            sp.tessellationDetail = tessellationDetail;
            sp.outline = ApplyOutlineChanges(outline);
            sp.physicsShape = ApplyOutlineChanges(physicsShape);
        }
    }


    // We need this so that undo/redo works
    [Serializable]
    internal class SpriteOutline
    {
        [SerializeField]
        public List<Vector2> m_Path = new List<Vector2>();

        public void Add(Vector2 point)
        {
            m_Path.Add(point);
        }

        public void Insert(int index, Vector2 point)
        {
            m_Path.Insert(index, point);
        }

        public void RemoveAt(int index)
        {
            m_Path.RemoveAt(index);
        }

        public Vector2 this[int index]
        {
            get { return m_Path[index]; }
            set { m_Path[index] = value; }
        }

        public int Count
        {
            get { return m_Path.Count; }
        }

        public void AddRange(IEnumerable<Vector2> addRange)
        {
            m_Path.AddRange(addRange);
        }
    }

    [Serializable]
    internal class SpriteRectCache : ScriptableObject,  ISpriteRectCache
    {
        [SerializeField]
        public List<SpriteRect> m_Rects;

        public int Count
        {
            get { return m_Rects != null ? m_Rects.Count : 0; }
        }

        public SpriteRect RectAt(int i)
        {
            return i >= Count || i < 0 ? null : m_Rects[i];
        }

        public void AddRect(SpriteRect r)
        {
            if (m_Rects != null)
                m_Rects.Add(r);
        }

        public void RemoveRect(SpriteRect r)
        {
            if (m_Rects != null)
                m_Rects.Remove(r);
        }

        public void ClearAll()
        {
            if (m_Rects != null)
                m_Rects.Clear();
        }

        public int GetIndex(SpriteRect spriteRect)
        {
            if (m_Rects != null)
                return m_Rects.FindIndex(p => p.Equals(spriteRect));

            return 0;
        }

        public bool Contains(SpriteRect spriteRect)
        {
            if (m_Rects != null)
                return m_Rects.Contains(spriteRect);

            return false;
        }

        void OnEnable()
        {
            if (m_Rects == null)
                m_Rects = new List<SpriteRect>();
        }
    }
}
