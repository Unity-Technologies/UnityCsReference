// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Events provider
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class EventProvider
    {
        private static IEventProviderImpl s_impl = new InputManagerProvider();
        private static EventSanitizer s_sanitizer;

        private static IEventProviderImpl s_implMockBackup = null; // storing original impl when using mock
        private static bool s_focusStateBeforeMock;
        private static bool s_focusChangedRegistered;

        /// <summary>
        /// Returns the IEventProviderImpl set as the current active provider, if any.
        /// </summary>
        public static IEventProviderImpl provider => s_impl;

        /// <summary>
        /// Subscribes to event stream.
        /// Handler will be called before dynamic update.
        /// </summary>
        /// <param name="handler">Return true to mark event as handled.</param>
        /// <param name="priority">Relative order of execution, lower numbers come first (TODO should be other way around?).</param>
        /// <param name="playerId">Only receive events for specified playerId</param>
        /// <param name="type">Filters by type, if none provided sends all events</param>
        public static void Subscribe(EventConsumer handler, int priority = 0, int? playerId = null, params Event.Type[] type)
        {
            Bootstrap();

            _registrations.Add(new Registration
            {
                handler = handler,
                priority = priority,
                playerId = playerId,
                _types = new HashSet<Event.Type>(type)
            });
            _registrations.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public static void Unsubscribe(EventConsumer handler)
        {
            _registrations.RemoveAll(x => x.handler == handler);
        }

        /// <summary>
        /// Enable or disables the Provider.
        /// This is useful if InputForUI gets disabled then we can also turn off the provider here
        /// and cleanup any resources it was using.
        /// </summary>
        /// <param name="enable">Specifies whether to enable or disable the provider.</param>
        public static void SetEnabled(bool enable)
        {
            m_IsEnabled = enable;

            if (enable)
                Initialize();
            else
                Shutdown();
        }
        static bool m_IsEnabled = true;
        static bool m_IsInitialized = false;

        internal static void Dispatch(in Event ev)
        {
            // Don't run sanitizers if there are no users. Avoid unexpected InputForUI errors in the console.
            if (_registrations.Count == 0)
                return;

            s_sanitizer.Inspect(ev);

            foreach (var registration in _registrations)
            {
                if (registration._types.Count > 0 && !registration._types.Contains(ev.type))
                    continue;

                if (registration.handler(ev))
                    return; // handled
            }
        }

        // This is a fairly hacky solution for now
        private static List<Registration> _registrations = new();

        private struct Registration
        {
            public EventConsumer handler;
            public int priority;
            public int? playerId;
            public HashSet<Event.Type> _types;
        }

        /// <summary>
        /// Sends KeyEvent and PointerEvents (per mouse / finger / pen) with State type and current state set.
        /// Guaranteed to arrive as first events in the next update.
        /// Use to fill your stored state about device.
        /// </summary>
        /// <param name="type">Filters by type, if none provided sends all events</param>
        public static void RequestCurrentState(params Event.Type[] types)
        {
            foreach (var type in (types is { Length: > 0 } ? types : Event.TypesWithState))
                if(!(s_impl?.RequestCurrentState(type)).GetValueOrDefault())
                    Debug.LogWarning($"Can't provide state for type {type}");
        }

        // Current player count for multiplayer scenarios
        // TODO should this be an event? Maybe create different events for user joined / user left?
        public static uint playerCount => s_impl?.playerCount ?? 0;

        static void Bootstrap()
        {
            if (m_IsEnabled)
                Initialize();
        }

        static void Initialize()
        {
            if (m_IsInitialized)
                return;

            s_sanitizer.Reset();
            s_impl?.Initialize();

            if (!s_focusChangedRegistered)
            {
                Application.focusChanged += OnFocusChanged;
                s_focusChangedRegistered = true;
            }
            m_IsInitialized = true;
        }

        static void Shutdown()
        {
            if (!m_IsInitialized)
                return;

            m_IsInitialized = false;

            if (s_focusChangedRegistered)
            {
                s_focusChangedRegistered = false;
                Application.focusChanged -= OnFocusChanged;
            }

            s_impl?.Shutdown();
        }

        private static void OnFocusChanged(bool focus)
        {
            s_impl?.OnFocusChanged(focus);
        }

        [RequiredByNativeCode]
        internal static void NotifyUpdate()
        {
            // Don't Update if there are no registered users. Avoid unexpected InputForUI errors in the console.
            if (!Application.isPlaying || _registrations.Count == 0 || !m_IsInitialized)
                return;

            s_sanitizer.BeforeProviderUpdate();
            s_impl?.Update();
            s_sanitizer.AfterProviderUpdate();
        }

        /// <summary>
        /// Called by InputSystemProvider defined in a separate package.
        /// </summary>
        internal static void SetInputSystemProvider(IEventProviderImpl impl)
        {
            // The logic for deciding which provider is used is mostly inside InputSystem Package.
            // InputSystem will call SetInputSystemProvider() if it wishes to enable itself.
            // If not, then we ALWAYS fallback to using InputManager here, to ensure we always have a provider.
            //
            // InputSystem will NOT enable itself if PlayerSettings explicitly enables InputManager ONLY.
            // InputSystem will be used (and therefore InputManager will NOT be) if PlayerSettings enables InputSystem
            // (either just InputSystem by itself or enabling Both input backends).
            // InputSystem Package cannot enable itself if it is not installed; therefore in this case InputManager
            // will ALWAYS be used regardless of the PlayerSettings.

            bool wasInitialized = m_IsInitialized;
            Shutdown();

            // Typically we just lazily set the provider here, to be initialized later during Bootstrap().
            s_impl = impl;

            // However if we are changing the provider after the bootstrapping phase occurred
            // then we should ensure the new provider is initialized here.
            if (wasInitialized)
                Initialize();
        }

        /// <summary>
        /// Sets a mock implementation of IEventProviderImpl for testing purposes.
        /// Stores current implementation to restore it via ClearMockProvider call.
        /// </summary>
        internal static void SetMockProvider(IEventProviderImpl impl)
        {
            if (s_implMockBackup == null)
                s_implMockBackup = s_impl;
            s_focusStateBeforeMock = Application.isFocused;

            Shutdown();
            s_impl = impl;
            Initialize();
        }

        /// <summary>
        /// Shutdowns mock implementation and restores previously stored real implementation.
        /// </summary>
        internal static void ClearMockProvider()
        {
            Shutdown();
            s_impl = s_implMockBackup;
            s_implMockBackup = null;
            Initialize();

            if (s_focusStateBeforeMock != Application.isFocused)
                s_impl?.OnFocusChanged(Application.isFocused);
        }

        // For debugging bootstrapping in tests.
        internal static string _providerClassName => s_impl?.GetType().Name;

        internal static RationalTime doubleClickTime
        {
            get
            {
                var doubleClickTimeInMs = UnityEngine.Event.GetDoubleClickTime();
                return new RationalTime(doubleClickTimeInMs, new RationalTime.TicksPerSecond(1000));
            }
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal delegate bool EventConsumer(in Event ev);
}
