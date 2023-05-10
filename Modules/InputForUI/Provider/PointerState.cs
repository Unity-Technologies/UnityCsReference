// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;

namespace UnityEngine.InputForUI
{
    internal struct PointerState
    {
        public PointerEvent.Button LastPressedButton { get; private set; }

        private PointerEvent.ButtonsState _buttonsState;
        public PointerEvent.ButtonsState ButtonsState => _buttonsState;

        public DiscreteTime NextPressTime { get; private set; }
        public int ClickCount { get; private set; }

        public Vector2 LastPosition { get; private set; }
        public int LastDisplayIndex { get; private set; }
        public bool LastPositionValid { get; private set; }

        private static readonly DiscreteTime kClickDelay = new((double)UnityEngine.Event.GetDoubleClickTime() / 1000.0);

        public void OnButtonDown(DiscreteTime currentTime, PointerEvent.Button button)
        {
            if (LastPressedButton != button || currentTime >= NextPressTime)
                ClickCount = 0;

            LastPressedButton = button;
            _buttonsState.Set(button, true);

            ClickCount++;
            NextPressTime = currentTime + kClickDelay;
        }

        public void OnButtonUp(DiscreteTime currentTime, PointerEvent.Button button)
        {
            if (LastPressedButton != button)
                ClickCount = 1;
            _buttonsState.Set(button, false);
        }

        public void OnButtonChange(DiscreteTime currentTime, PointerEvent.Button button, bool previousState, bool newState)
        {
            if (newState && !previousState)
                OnButtonDown(currentTime, button);
            else if (!newState && previousState)
                OnButtonUp(currentTime, button);
        }

        public void OnMove(DiscreteTime currentTime, Vector2 position, int displayIndex)
        {
            LastPosition = position;
            LastDisplayIndex = displayIndex;
            LastPositionValid = true;
        }

        public void Reset()
        {
            LastPressedButton = PointerEvent.Button.None;
            ButtonsState.Reset();

            NextPressTime = DiscreteTime.Zero;
            ClickCount = 0;

            LastPosition = Vector2.zero;
            LastDisplayIndex = 0;
            LastPositionValid = false;
        }
    }
}
