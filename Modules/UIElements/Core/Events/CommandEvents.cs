// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    [EventCategory(EventCategory.Command)]
    public abstract class CommandEventBase<T> : EventBase<T>, ICommandEvent where T : CommandEventBase<T>, new()
    {
        string m_CommandName;
        /// <summary>
        /// The command to validate or execute.
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
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles;
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

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToFocusedElementOrPanelRoot(this, panel);
        }

        protected CommandEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent by the Editor while it determines whether the command will be handled by an element in the panel.
    /// </summary>
    /// <remarks>
    /// See also: [[wiki:UIE-Command-Events|Command Events]].
    /// </remarks>
    public class ValidateCommandEvent : CommandEventBase<ValidateCommandEvent>
    {
        static ValidateCommandEvent()
        {
            SetCreateFunction(() => new ValidateCommandEvent());
        }
    }

    /// <summary>
    /// This event is sent by the Editor when an element in the panel should execute a command.
    /// </summary>
    /// <remarks>
    /// See also: [[wiki:UIE-Command-Events|Command Events]].
    /// </remarks>
    public class ExecuteCommandEvent : CommandEventBase<ExecuteCommandEvent>
    {
        static ExecuteCommandEvent()
        {
            SetCreateFunction(() => new ExecuteCommandEvent());
        }
    }
}
