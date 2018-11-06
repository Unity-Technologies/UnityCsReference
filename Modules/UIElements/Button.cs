// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public class Button : TextElement
    {
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> {}

        public new class UxmlTraits : TextElement.UxmlTraits {}

        public Clickable clickable { get; set; }

        public Button() : this(null)
        {
        }

        public new static readonly string ussClassName = "unity-button";

        public Button(System.Action clickEvent)
        {
            AddToClassList(ussClassName);

            // Click-once behaviour
            clickable = new Clickable(clickEvent);
            this.AddManipulator(clickable);
        }
    }
}
