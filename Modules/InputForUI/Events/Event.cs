// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.IntegerTime;
using UnityEngine;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Unified event type, can contain any event type.
    /// Is unmanaged for most event types.
    /// TODO maybe we can make it fully unmanaged if composition string can be fixed size?
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Event : IEventProperties
    {
        public enum Type
        {
            Invalid = 0,
            KeyEvent = 1,
            PointerEvent = 2,
            TextInputEvent = 3,
            IMECompositionEvent = 4,
            CommandEvent = 5,
            NavigationEvent = 6,
        }

        /// <summary>
        /// Order two events between each other in event stream based on their type.
        /// </summary>
        /// <remarks> When both events are of <see cref="PointerEvent"/> this method will compare them based on their
        /// <see cref="eventSource"/></remarks>
        internal static int CompareType(Event a, Event b)
        {
            if (a.type == Type.PointerEvent && b.type == Type.PointerEvent)
            {
                var aEventSource = (int)a.eventSource;
                var bEventSource = (int)b.eventSource;
                return bEventSource.CompareTo(aEventSource); // inverse order, see enum values
            }

            var aType = (int)a.type;
            var bType = (int)b.type;
            return aType.CompareTo(bType); // normal order
        }

        public static Type[] TypesWithState = new[]
        {
            Type.KeyEvent,
            Type.PointerEvent,
            Type.IMECompositionEvent
        };

        private const int kManagedOffset = 8; // align managed object at 64 bit pointer size
        private const int kUnmanagedOffset = 16;

        [FieldOffset(0)] private Type _type;

        // We can't overlap managed objects, so we will store them in object via boxing allocation.
        [FieldOffset(kManagedOffset)] private object _managedEvent;

        [FieldOffset(kUnmanagedOffset)] private KeyEvent _keyEvent;
        [FieldOffset(kUnmanagedOffset)] private PointerEvent _pointerEvent;
        [FieldOffset(kUnmanagedOffset)] private TextInputEvent _textInputEvent;
        [FieldOffset(kUnmanagedOffset)] private CommandEvent _commandEvent;
        [FieldOffset(kUnmanagedOffset)] private NavigationEvent _navigationEvent;

        // -------------------------------------------------------------------------------------------------------------

        public Type type => _type;

        /// <summary>
        /// Returns current underlying event as a managed object.
        /// Beware, shouldn't be exposed as it does boxing allocations.
        /// </summary>
        private IEventProperties asObject => Map<IEventProperties, MapAsObject>();

        /// <summary>
        /// Timestamp of underlying event.
        /// </summary>
        public DiscreteTime timestamp => Map<DiscreteTime, MapAsTimestamp>();

        /// <summary>
        /// DeviceId of underlying event.
        /// </summary>
        public EventSource eventSource => Map<EventSource, MapAsEventSource>();

        /// <summary>
        /// PlayerId of underlying event.
        /// </summary>
        public uint playerId => Map<uint, MapAsPlayerId>();

        /// <summary>
        /// Event modifiers of underlying event.
        /// </summary>
        public EventModifiers eventModifiers => Map<EventModifiers, MapAsEventModifiers>();

        // -------------------------------------------------------------------------------------------------------------

        private void Ensure(Type t)
        {
            Debug.Assert(type == t);
        }

        public override string ToString()
        {
            var mod = eventModifiers.ToString();
            if (!string.IsNullOrEmpty(mod))
                mod = $" ev:{mod}";

            return type == Type.Invalid ? "Invalid" : $"{asObject}{mod} src:{eventSource.ToString()}";
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(KeyEvent keyEvent)
        {
            return new Event
            {
                _type = Type.KeyEvent,
                _keyEvent = keyEvent
            };
        }

        public KeyEvent asKeyEvent
        {
            get
            {
                Ensure(Type.KeyEvent);
                return _keyEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(PointerEvent pointerEvent)
        {
            return new Event
            {
                _type = Type.PointerEvent,
                _pointerEvent = pointerEvent
            };
        }

        public PointerEvent asPointerEvent
        {
            get
            {
                Ensure(Type.PointerEvent);
                return _pointerEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(TextInputEvent textInputEvent)
        {
            return new Event
            {
                _type = Type.TextInputEvent,
                _textInputEvent = textInputEvent
            };
        }

        public TextInputEvent asTextInputEvent
        {
            get
            {
                Ensure(Type.TextInputEvent);
                return _textInputEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(IMECompositionEvent imeCompositionEvent)
        {
            return new Event
            {
                _type = Type.IMECompositionEvent,
                // TODO would be very cool to avoid boxing
                _managedEvent = imeCompositionEvent // Boxing
            };
        }

        public IMECompositionEvent asIMECompositionEvent
        {
            get
            {
                Ensure(Type.IMECompositionEvent);
                return (IMECompositionEvent)_managedEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(CommandEvent commandEvent)
        {
            return new Event
            {
                _type = Type.CommandEvent,
                _commandEvent = commandEvent
            };
        }

        public CommandEvent asCommandEvent
        {
            get
            {
                Ensure(Type.CommandEvent);
                return _commandEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        public static Event From(NavigationEvent navigationEvent)
        {
            return new Event
            {
                _type = Type.NavigationEvent,
                _navigationEvent = navigationEvent
            };
        }

        public NavigationEvent asNavigationEvent
        {
            get
            {
                Ensure(Type.NavigationEvent);
                return _navigationEvent;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Maps a "delegate" to underlying event type.
        /// Uses template based delegates instead of C# delegates to avoid runtime cost and avoid boxing.
        /// </summary>
        /// <param name="fn">Delegate instance.</param>
        /// <typeparam name="TOutputType">Result type.</typeparam>
        /// <typeparam name="TMapType">Delegate instance type.</typeparam>
        /// <returns>Result from delegate.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If event type is unsupported.</exception>
        private TOutputType Map<TOutputType, TMapType>(TMapType fn) where TMapType : IMapFn<TOutputType>
        {
            switch (type)
            {
                case Type.Invalid:
                    return default;
                case Type.KeyEvent:
                    return fn.Map(ref _keyEvent);
                case Type.PointerEvent:
                    return fn.Map(ref _pointerEvent);
                case Type.TextInputEvent:
                    return fn.Map(ref _textInputEvent);
                case Type.IMECompositionEvent:
                {
                    var h = (IMECompositionEvent)_managedEvent;
                    return fn.Map(ref h);
                }
                case Type.CommandEvent:
                    return fn.Map(ref _commandEvent);
                case Type.NavigationEvent:
                    return fn.Map(ref _navigationEvent);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TOutputType Map<TOutputType, TMapType>() where TMapType : IMapFn<TOutputType>, new() =>
            Map<TOutputType, TMapType>(new TMapType());

        private interface IMapFn<TOutputType>
        {
            public TOutputType Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties;
        }

        private struct MapAsObject : IMapFn<IEventProperties>
        {
            public IEventProperties Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties => ev;
        }

        private struct MapAsTimestamp : IMapFn<DiscreteTime>
        {
            public DiscreteTime Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties => ev.timestamp;
        }

        private struct MapAsEventSource : IMapFn<EventSource>
        {
            public EventSource Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties => ev.eventSource;
        }

        private struct MapAsPlayerId : IMapFn<uint>
        {
            public uint Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties => ev.playerId;
        }

        private struct MapAsEventModifiers : IMapFn<EventModifiers>
        {
            public EventModifiers Map<TEventType>(ref TEventType ev) where TEventType : IEventProperties => ev.eventModifiers;
        }
    }
}
