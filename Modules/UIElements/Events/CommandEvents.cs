// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public interface ICommandEvent
    {
        string commandName { get; }
    }

    public abstract class CommandEventBase<T> : EventBase<T>, ICommandEvent where T : CommandEventBase<T>, new()
    {
        string m_CommandName;
        public string commandName
        {
            get
            {
                if (m_CommandName == null && imguiEvent != null)
                {
                    return imguiEvent.commandName;
                }

                return m_CommandName;
            }

            protected set { m_CommandName = value; }
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles | EventPropagation.Cancellable;
            commandName = null;
        }

        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();
            e.imguiEvent = systemEvent;
            return e;
        }

        public static T GetPooled(string commandName)
        {
            T e = GetPooled();
            e.commandName = commandName;
            return e;
        }

        protected CommandEventBase()
        {
            LocalInit();
        }
    }

    public class ValidateCommandEvent : CommandEventBase<ValidateCommandEvent>
    {
    }

    public class ExecuteCommandEvent : CommandEventBase<ExecuteCommandEvent>
    {
    }
}
