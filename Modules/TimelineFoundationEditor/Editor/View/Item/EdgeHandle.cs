// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    [UxmlElement]
    internal partial class EdgeHandle : VisualElement
    {
        public enum Location
        {
            Left,
            Right
        }

        [UxmlAttribute]
        public Location location { get; set; }

        public Cursor cursor
        {
            get => style.cursor.value;
            set
            {
                // Only set the cursor if it's different from the current value and not the default cursor, which equates to a basic Pointer. This prevents cursors set in the stylesheet from being overridden.
                if (cursor != value && value != new StyleCursor(StyleKeyword.Auto).value)
                    style.cursor = value;
            }
        }

        public EdgeHandle()
        {
            focusable = false;
            pickingMode = PickingMode.Position;
        }

        public EdgeHandle(Location location)
        {
            this.location = location;
        }

        public void UnsetCursor()
        {
            style.cursor = StyleKeyword.Null;
        }
    }
}
