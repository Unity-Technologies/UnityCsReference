// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [Flags]
    enum ResizerDirection
    {
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
    }

    interface IResizeListener
    {
        void OnStartResize();
        void OnResizing();
        void OnStopResize();
    }

    /// <summary>
    /// An element used to interactively resizes its parent.
    /// </summary>
    [UnityRestricted]
    internal class ResizableElement : VisualElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="ResizableElement"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-resizable-element";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResizableElement"/> class.
        /// </summary>
        public ResizableElement()
        {
            var left = new VisualElement { name = "left", pickingMode = PickingMode.Ignore };
            Add(left);
            var subElement = new VisualElement { name = "top-left-resize" };
            left.Add(subElement);
            subElement = new VisualElement { name = "left-resize" };
            left.Add(subElement);
            subElement = new VisualElement { name = "bottom-left-resize" };
            left.Add(subElement);

            var middle = new VisualElement { name = "middle", pickingMode = PickingMode.Ignore };
            Add(middle);
            subElement = new VisualElement { name = "top-resize" };
            middle.Add(subElement);
            subElement = new VisualElement { name = "middle-center", pickingMode = PickingMode.Ignore };
            middle.Add(subElement);
            subElement = new VisualElement { name = "bottom-resize" };
            middle.Add(subElement);

            var right = new VisualElement { name = "right", pickingMode = PickingMode.Ignore };
            Add(right);
            subElement = new VisualElement { name = "top-right-resize" };
            right.Add(subElement);
            subElement = new VisualElement { name = "right-resize" };
            right.Add(subElement);
            subElement = new VisualElement { name = "bottom-right-resize" };
            right.Add(subElement);

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("Resizable.uss");

            foreach (ResizerDirection value in Enum.GetValues(typeof(ResizerDirection)))
            {
                var resizer = this.SafeQ(value.ToString().ToLower() + "-resize");
                if (resizer != null)
                    resizer.AddManipulator(new ElementResizer(this, value));
            }

            foreach (ResizerDirection vertical in new[] { ResizerDirection.Top, ResizerDirection.Bottom })
            {
                foreach (ResizerDirection horizontal in new[] { ResizerDirection.Left, ResizerDirection.Right })
                {
                    var resizer = this.SafeQ(vertical.ToString().ToLower() + "-" + horizontal.ToString().ToLower() + "-resize");
                    if (resizer != null)
                        resizer.AddManipulator(new ElementResizer(this, vertical | horizontal));
                }
            }

            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);
        }
    }
}
