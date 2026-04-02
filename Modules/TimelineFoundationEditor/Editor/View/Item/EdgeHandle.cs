// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    partial class EdgeHandle : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData),
                    new UxmlAttributeNames[]
                    {
                        new(nameof(location), "location"),
                    }, true);
            }

#pragma warning disable 649
            [SerializeField] Location location;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags location_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new EdgeHandle();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (EdgeHandle)obj;
                if (ShouldWriteAttributeValue(location_UxmlAttributeFlags))
                    e.location = location;
            }
            
        }
        public enum Location
        {
            Left,
            Right
        }

        public Location location { get; set; } //used for UXML attribute

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
