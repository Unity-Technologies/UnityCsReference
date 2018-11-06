// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public class InputEvent : EventBase<InputEvent>
    {
        public string previousData { get; protected set; }
        public string newData { get; protected set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
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
            LocalInit();
        }
    }
}
