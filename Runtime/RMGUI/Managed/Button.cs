// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    [GUISkinStyle("button")]
    // TODO Make public. It's currently internal because it clashes in a doc tool with another class in another namespace.
    // Once the tool is fixed, this becomes public.
    internal class Button : VisualElement
    {
        public Clickable clickable;

        public Button(ClickEvent clickEvent)
        {
            // Click-once behaviour
            clickable = new Clickable(clickEvent);
            AddManipulator(clickable);
        }
    }
}
