// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class InputEvent : EventBase<InputEvent>
    {
        public string previousData { get; protected set; }
        public string newData { get; protected set; }

        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
            previousData = default(string);
            newData = default(string);
        }

        public static InputEvent GetPooled(string previousData, string newData)
        {
            InputEvent e = GetPooled();
            e.previousData = previousData;
            e.newData = newData;
            return e;
        }

        public InputEvent()
        {
            Init();
        }
    }
}
