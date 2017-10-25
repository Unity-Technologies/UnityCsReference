// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IChangeEvent
    {
    }

    public class ChangeEvent<T> : EventBase<ChangeEvent<T>>, IChangeEvent
    {
        public T previousValue { get; protected set; }
        public T newValue { get; protected set; }

        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
            previousValue = default(T);
            newValue = default(T);
        }

        public static ChangeEvent<T> GetPooled(T previousValue, T newValue)
        {
            ChangeEvent<T> e = GetPooled();
            e.previousValue = previousValue;
            e.newValue = newValue;
            return e;
        }

        public ChangeEvent()
        {
            Init();
        }
    }
}
