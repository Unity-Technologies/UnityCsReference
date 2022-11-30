// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    internal delegate bool DrawHandler(GUIStyle style, Rect rect, GUIContent content, DrawStates states);

    internal readonly struct DrawStates
    {
        public DrawStates(bool isHover, bool isActive, bool on, bool hasKeyboardFocus) : this(-1, isHover, isActive, on, hasKeyboardFocus)
        {
        }

        public DrawStates(int controlId, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            this.controlId = controlId;
            this.isHover = isHover;
            this.isActive = isActive;
            this.on = on;
            this.hasKeyboardFocus = hasKeyboardFocus;

            hasTextInput = false;
            drawSelectionAsComposition = false;
            cursorFirstPosition = cursorLastPosition = Vector2.zero;
            selectionColor = cursorColor = Color.red;
        }

        public DrawStates(int controlId, bool isHover, bool isActive, bool on, bool hasKeyboardFocus,
                          bool drawSelectionAsComposition, Vector2 cursorFirstPosition, Vector2 cursorLastPosition,
                          Color cursorColor, Color selectionColor)
            : this(controlId, isHover, isActive, on, hasKeyboardFocus)
        {
            hasTextInput = true;
            this.drawSelectionAsComposition = drawSelectionAsComposition;
            this.cursorFirstPosition = cursorFirstPosition;
            this.cursorLastPosition = cursorLastPosition;
            this.cursorColor = cursorColor;
            this.selectionColor = selectionColor;
        }

        public readonly int controlId;
        public readonly bool isHover;
        public readonly bool isActive;
        public readonly bool on;
        public readonly bool hasKeyboardFocus;

        public readonly bool hasTextInput;
        public readonly bool drawSelectionAsComposition;
        public readonly Vector2 cursorFirstPosition;
        public readonly Vector2 cursorLastPosition;
        public readonly Color cursorColor;
        public readonly Color selectionColor;
    }
}
