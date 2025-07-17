// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.Overlays
{
    class DraggerElement : VisualElement
    {
        const float m_Size = 8;

        readonly VisualElement m_Handle;

        readonly VisualElement m_Visual;

        public VisualElement handle => m_Handle;

        public VisualElement visual => m_Visual;

        const string k_HeightDraggerVisualClassName = "overlay-dynamic-panel-container__height-dragger__visual";
        const string k_HeightDraggerHandleClassName = "overlay-dynamic-panel-container__height-dragger__handle";

        readonly DragManipulator m_Manipulator;
        public event Action<(Vector2 total, Vector2 delta)> translated
        {
            add { m_Manipulator.translated += value; }
            remove { m_Manipulator.translated -= value; }
        }

        public event Action translationBegun
        {
            add { m_Manipulator.translationBegun += value; }
            remove { m_Manipulator.translationBegun -= value; }
        }

        public event Action translationEnded
        {
            add { m_Manipulator.translationEnded += value; }
            remove { m_Manipulator.translationEnded -= value; }
        }

        static int GetCursor(DragDirection direction)
        {
            switch (direction)
            {
                case DragDirection.Vertical:
                    return (int)MouseCursor.ResizeVertical;

                case DragDirection.Horizontal:
                    return (int)MouseCursor.ResizeHorizontal;

                default:
                    return (int)MouseCursor.Arrow;
            }
        }

        public DraggerElement(DragDirection direction)
        {
            Add(m_Handle = new VisualElement());
            m_Handle.AddManipulator(m_Manipulator = new DragManipulator(direction));
            m_Handle.style.position = Position.Absolute;
            m_Handle.style.cursor = new Cursor { defaultCursorId = (int)GetCursor(direction) };
            m_Handle.pickingMode = PickingMode.Position;
            m_Handle.AddToClassList(k_HeightDraggerHandleClassName);

            switch (direction)
            {
                case DragDirection.Vertical:
                    style.width = new Length(100, LengthUnit.Percent);
                    m_Handle.style.top = -m_Size * .5f;
                    m_Handle.style.bottom = -m_Size * .5f;
                    m_Handle.style.left = 0;
                    m_Handle.style.right = 0;
                    break;

                case DragDirection.Horizontal:
                    style.height = new Length(100, LengthUnit.Percent);
                    m_Handle.style.left = -m_Size * .5f;
                    m_Handle.style.right = -m_Size * .5f;
                    m_Handle.style.top = 0;
                    m_Handle.style.bottom = 0;
                    break;

                case DragDirection.All:
                    m_Handle.style.width = m_Size;
                    m_Handle.style.height = m_Size;
                    break;
            }

            m_Handle.Add(m_Visual = new VisualElement());
            m_Visual.AddToClassList(k_HeightDraggerVisualClassName);
        }
    }
}
