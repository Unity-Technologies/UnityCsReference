namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for Command events.
    /// </summary>
    public interface ICommandEvent
    {
        /// <summary>
        /// Name of the command.
        /// </summary>
        string commandName { get; }
    }

    /// <summary>
    /// Base class for command events.
    /// </summary>
    public abstract class CommandEventBase<T> : EventBase<T>, ICommandEvent where T : CommandEventBase<T>, new()
    {
        string m_CommandName;
        /// <summary>
        /// Name of the command.
        /// </summary>
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

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
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

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI command event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();
            e.imguiEvent = systemEvent;
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <returns>An initialized event.</returns>
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

    /// <summary>
    /// The event sent to probe which elements accepts a command.
    /// </summary>
    public class ValidateCommandEvent : CommandEventBase<ValidateCommandEvent>
    {
    }

    /// <summary>
    /// The event sent when an element should execute a command.
    /// </summary>
    public class ExecuteCommandEvent : CommandEventBase<ExecuteCommandEvent>
    {
    }
}
