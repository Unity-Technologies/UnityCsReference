// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class Button : VisualElement
    {
        public Clickable clickable;

        public Button(System.Action clickEvent)
        {
            // Click-once behaviour
            clickable = new Clickable(clickEvent);
            this.AddManipulator(clickable);
        }
    }
}
