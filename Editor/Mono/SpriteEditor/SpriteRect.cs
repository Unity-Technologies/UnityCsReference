// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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

        static public List<SpriteOutline> AcquireOutline(SerializedProperty outlineSP)
        {
            var outline = new List<SpriteOutline>();
            for (int j = 0; j < outlineSP.arraySize; ++j)
            {
                SpriteOutline o = new SpriteOutline();
                SerializedProperty outlinePathSO = outlineSP.GetArrayElementAtIndex(j);
                for (int k = 0; k < outlinePathSO.arraySize; ++k)
                {
                    Vector2 vector2 = outlinePathSO.GetArrayElementAtIndex(k).vector2Value;
                    o.Add(vector2);
                }
                outline.Add(o);
            }

            return outline;
        }

        static public void ApplyOutlineChanges(SerializedProperty outlineSP, List<SpriteOutline> outline)
        {
            outlineSP.ClearArray();
            for (int j = 0; j < outline.Count; ++j)
            {
                outlineSP.InsertArrayElementAtIndex(j);
                SpriteOutline o = outline[j];
                SerializedProperty outlinePathSO = outlineSP.GetArrayElementAtIndex(j);
                outlinePathSO.ClearArray();
                for (int k = 0; k < o.Count; ++k)
                {
                    outlinePathSO.InsertArrayElementAtIndex(k);
                    outlinePathSO.GetArrayElementAtIndex(k).vector2Value = o[k];
                }
            }
        }

        public void ApplyToSerializedProperty(SerializedProperty sp)
        {
            sp.FindPropertyRelative("m_Rect").rectValue = rect;
            sp.FindPropertyRelative("m_Border").vector4Value = border;
            sp.FindPropertyRelative("m_Name").stringValue = name;
            sp.FindPropertyRelative("m_Alignment").intValue = (int)alignment;
            sp.FindPropertyRelative("m_Pivot").vector2Value = pivot;
            sp.FindPropertyRelative("m_TessellationDetail").floatValue = tessellationDetail;

            SerializedProperty outlineSP = sp.FindPropertyRelative("m_Outline");
            outlineSP.ClearArray();
            if (outline != null)
                ApplyOutlineChanges(outlineSP, outline);

            SerializedProperty physicsShapeSP = sp.FindPropertyRelative("m_PhysicsShape");
            physicsShapeSP.ClearArray();
            if (physicsShape != null)
                ApplyOutlineChanges(physicsShapeSP, physicsShape);
        }

        public void LoadFromSerializedProperty(SerializedProperty sp)
        {
            rect = sp.FindPropertyRelative("m_Rect").rectValue;
            border = sp.FindPropertyRelative("m_Border").vector4Value;
            name = sp.FindPropertyRelative("m_Name").stringValue;
            alignment = (SpriteAlignment)sp.FindPropertyRelative("m_Alignment").intValue;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, sp.FindPropertyRelative("m_Pivot").vector2Value);
            tessellationDetail = sp.FindPropertyRelative("m_TessellationDetail").floatValue;

            SerializedProperty outlineSP = sp.FindPropertyRelative("m_Outline");
            outline = AcquireOutline(outlineSP);

            outlineSP = sp.FindPropertyRelative("m_PhysicsShape");
            physicsShape = AcquireOutline(outlineSP);
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
