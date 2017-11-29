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
    public class SpriteRect
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
        string m_SpriteID;

        GUID m_GUID;

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
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

        internal string originalName
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

        public GUID spriteID
        {
            get
            {
                ValidateGUID();
                return m_GUID;
            }
            set
            {
                m_GUID = value;
                m_SpriteID = m_GUID.ToString();
                ValidateGUID();
            }
        }

        private void ValidateGUID()
        {
            if (m_GUID.Empty())
            {
                // We can't use ISerializationCallbackReceiver because we will hit into Script serialization errors
                m_GUID = new GUID(m_SpriteID);
                if (m_GUID.Empty())
                {
                    m_GUID = GUID.Generate();
                    m_SpriteID = m_GUID.ToString();
                }
            }
        }

        public static GUID GetSpriteIDFromSerializedProperty(SerializedProperty sp)
        {
            return new GUID(sp.FindPropertyRelative("m_SpriteID").stringValue);
        }
    }

    internal class SpriteRectCache : ScriptableObject
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
                m_Rects.RemoveAll(x => x.spriteID == r.spriteID);
        }

        public void ClearAll()
        {
            if (m_Rects != null)
                m_Rects.Clear();
        }

        public int GetIndex(SpriteRect spriteRect)
        {
            if (m_Rects != null && spriteRect != null)
                return m_Rects.FindIndex(p => p.spriteID == spriteRect.spriteID);

            return -1;
        }

        public bool Contains(SpriteRect spriteRect)
        {
            if (m_Rects != null && spriteRect != null)
                return m_Rects.Find(x => x.spriteID == spriteRect.spriteID) != null;

            return false;
        }

        void OnEnable()
        {
            if (m_Rects == null)
                m_Rects = new List<SpriteRect>();
        }
    }
}
