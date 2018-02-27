// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.UIAutomation
{
    class FakeCursor
    {
        public enum CursorType
        {
            Normal
        }

        public Vector2 position { get; set; }
        public CursorType currentCursorType = CursorType.Normal;

        struct CursorData
        {
            public Texture2D cursor;
            public Vector2 hotspotOffset;
        }

        static CursorData[] s_MouseCursors;
        static Vector2 s_CursorSize = new Vector2(32, 32);

        bool shouldDrawFakeMouseCursor
        {
            get { return position.x > 0f; }
        }

        public virtual void Draw()
        {
            if (shouldDrawFakeMouseCursor)
            {
                var cursor = GetCursor(currentCursorType);
                if (cursor != null)
                    GUI.DrawTexture(GetCursorDrawRect(currentCursorType), cursor);
                else
                    EditorGUI.DrawRect(new Rect(position.x, position.y, 5, 5), Color.white); // fallback rendering
            }
        }

        Texture2D GetCursor(CursorType cursorType)
        {
            InitIfNeeded();
            return s_MouseCursors[(int)cursorType].cursor;
        }

        Rect GetCursorDrawRect(CursorType cursorType)
        {
            InitIfNeeded();
            Vector2 offset = s_MouseCursors[(int)cursorType].hotspotOffset;
            return new Rect(position.x + offset.x, position.y + offset.y, s_CursorSize.x, s_CursorSize.y);
        }

        void InitIfNeeded()
        {
            if (s_MouseCursors == null)
            {
                s_MouseCursors = new[]
                {
                    new CursorData()
                    {
                        cursor = EditorGUIUtility.Load("Cursors/CursorAI.psd") as Texture2D,
                        hotspotOffset = new Vector2(-13, -10)
                    }
                };
            }
        }
    }
}
