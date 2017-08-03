// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class RepeatButton : VisualElement
    {
        public RepeatButton(System.Action clickEvent, long delay, long interval)
        {
            this.AddManipulator(new Clickable(clickEvent, delay, interval));
        }
    }
}
