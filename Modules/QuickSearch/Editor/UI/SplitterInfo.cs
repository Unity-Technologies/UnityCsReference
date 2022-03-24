// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    class SplitterInfo
    {
        public enum Side
        {
            Left,
            Right
        }

        public Side side;
        public float pos;
        public bool active;
        public float lowerLimit;
        public float upperLimit;
        public EditorWindow host;

        public float width
        {
            get
            {
                if (side == Side.Left)
                    return pos;
                return host.position.width - pos;
            }
        }

        public SplitterInfo(Side side, float lowerLimit, float upperLimit, EditorWindow host)
        {
            this.side = side;
            pos = -1;
            active = false;
            this.lowerLimit = lowerLimit;
            this.upperLimit = upperLimit;
            this.host = host;
        }

        public void Init(float initialPosition)
        {
            if (pos < 0)
                SetPosition(initialPosition, host.position.width);
        }

        public void SetPosition(float newPos)
        {
            SetPosition(newPos, host.position.width);
        }

        private void SetPosition(float newPos, float hostWidth)
        {
            if (newPos == -1)
                return;
            var minSize = Mathf.Max(0, hostWidth * lowerLimit);
            var maxSize = Mathf.Min(hostWidth * upperLimit, hostWidth);
            var previousPos = pos;
            pos = Mathf.Max(minSize, Mathf.Min(newPos, maxSize));
            if (previousPos != pos)
                host.Repaint();
        }

        public void Draw(Event evt, Rect area)
        {
            var sliderRect = new Rect(pos - 2f, area.y, 3f, area.height);
            EditorGUIUtility.AddCursorRect(sliderRect, MouseCursor.ResizeHorizontal);

            if (evt.type == EventType.MouseDown && sliderRect.Contains(evt.mousePosition))
            {
                active = true;
                evt.Use();
            }

            if (active)
            {
                SetPosition(evt.mousePosition.x, host.position.width);
                if (evt.type == EventType.MouseDrag)
                    evt.Use();
            }

            if (active && evt.type == EventType.MouseUp)
            {
                evt.Use();
                active = false;
            }
        }

        public void Resize(Vector2 oldSize, Vector2 newSize)
        {
            var newWidth = newSize.x;
            if (side == Side.Left)
                SetPosition(pos, newWidth);
            else
            {
                var widthDiff = newSize.x - oldSize.x;
                SetPosition(pos + widthDiff, newWidth);
            }
        }
    }
}
