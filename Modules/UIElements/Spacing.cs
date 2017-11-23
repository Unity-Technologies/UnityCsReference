// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public struct Spacing
    {
        public float left, top, right, bottom;

        public float horizontal
        {
            get
            {
                return left + right;
            }
        }

        public float vertical
        {
            get
            {
                return top + bottom;
            }
        }

        public Spacing(float left, float top, float right, float bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public static Rect operator+(Rect r, Spacing a)
        {
            r.x -= a.left;
            r.y -= a.top;
            r.width += a.horizontal;
            r.height += a.vertical;
            return r;
        }

        public static Rect operator-(Rect r, Spacing a)
        {
            r.x += a.left;
            r.y += a.top;
            r.width -= a.horizontal;
            r.height -= a.vertical;
            return r;
        }
    }
}
