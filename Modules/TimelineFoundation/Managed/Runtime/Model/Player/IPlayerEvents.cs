// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;

namespace Unity.Timeline.Foundation.Model
{
    interface IPlayerEvents
    {
        event Action<DiscreteTime> OnTimeChanged;
        event Action OnPlay;
        event Action OnPause;
        event Action OnStop;
        event Action OnDisable;
        event Action OnEnable;
        event Action OnPlayerDataChanged;
    }

    abstract class PlayerEventsBase : IPlayerEvents
    {
        public event Action<DiscreteTime> OnTimeChanged;
        public event Action OnPlay;
        public event Action OnPause;
        public event Action OnStop;
        public event Action OnDisable;
        public event Action OnEnable;
        public event Action OnPlayerDataChanged;

        protected void TimeChanged(DiscreteTime time)
        {
            OnTimeChanged?.Invoke(time);
        }

        protected void Played()
        {
            OnPlay?.Invoke();
        }

        protected void Paused()
        {
            OnPause?.Invoke();
        }

        protected void Stopped()
        {
            OnStop?.Invoke();
        }

        protected void Disabled()
        {
            OnDisable?.Invoke();
        }

        protected void Enabled()
        {
            OnEnable?.Invoke();
        }

        protected void PlayerDataChanged()
        {
            OnPlayerDataChanged?.Invoke();
        }
    }
}
