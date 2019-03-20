// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class Arrow : TextElement
    {
        public enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        public Arrow(Direction direction = Direction.Right, string defaultClass = "arrow")
        {
            AddToClassList(defaultClass);
            SetDirection(direction);
        }

        public void SetDirection(Direction direction)
        {
            if (direction == Direction.Left)
                text = "◄";
            else if (direction == Direction.Right)
                text = "►";
            else if (direction == Direction.Up)
                text = "▲";
            else if (direction == Direction.Down)
                text = "▼";
        }
    }
}
