// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace Unity.Experimental.EditorMode
{
    /// <summary>
    /// Manages editor mode instances and the editor window overrides.
    /// </summary>
    internal static class EditorModes
    {
        private interface IModeGenericHelper
        {
            VisualElement GetRoot(EditorWindow window);
            void RegisterAsUnsupported(EditorMode mode, Type type);
            void RegisterAsPassthrough(EditorMode mode, Type type);
            void RemoveOverride(EditorWindow window);
        }

        private class ModeGenericHelper<TMode> : IModeGenericHelper
            where TMode : EditorMode
        {
            public VisualElement GetRoot(EditorWindow window)
            {
                return GetRootElement<TMode>(window);
            }

            public void RegisterAsUnsupported(EditorMode mode, Type type)
            {
                RegisterOverride<TMode, UnsupportedWindowOverride>((TMode)mode, type);
            }

            public void RegisterAsPassthrough(EditorMode mode, Type type)
            {
                RegisterOverride<TMode, PassthroughOverride>((TMode)mode, type);
            }

            public void RemoveOverride(EditorWindow window)
            {
                if (typeof(TMode) == typeof(DefaultEditorMode))
                {
                    return;
                }
                window.RemoveOverride<TMode>();
            }
        }

        // All current editor windows
        private static readonly HashSet<EditorWindow> s_RegisteredWindowsSet = new HashSet<EditorWindow>();
        private static readonly List<EditorWindow> s_RegisteredWindowsList = new List<EditorWindow>();

        // All current visible windows
        private static readonly HashSet<EditorWindow> s_VisibleWindowsSet = new HashSet<EditorWindow>();

        // Factory of overrides per type. This is repopulated every time we switch mode
        private static readonly Dictionary<Type, Func<EditorWindow, IEditorWindowOverride>> s_OverrideFactory = new Dictionary<Type, Func<EditorWindow, IEditorWindowOverride>>();

        // All current overrides
        private static readonly Dictionary<EditorWindow, IEditorWindowOverride> s_Overrides = new Dictionary<EditorWindow, IEditorWindowOverride>();

        internal static EditorMode Current => CurrentMode ?? DefaultMode;
        internal static string CurrentModeName
        {
            get
            {
                var modeName = Current.Name;
                if (string.IsNullOrEmpty(modeName))
                {
                    modeName = ObjectNames.NicifyVariableName(Current.GetType().Name);
                }
                return modeName;
            }
        }

        internal static EditorMode DefaultMode { get; } = new DefaultEditorMode();
        private static EditorMode CurrentMode { get; set; }

        private static EditorMode ExitingMode { get; set; }
        // We only allow registration of overrides when loading the mode, so we cache the one we currently load to check
        // for this.
        private static EditorMode CurrentForRegistration { get; set; }

        private static IModeGenericHelper GenericHelper { get; set; } = new ModeGenericHelper<DefaultEditorMode>();

        /// <summary>
        /// Requests the editor to switch into a new editor mode.
        /// </summary>
        /// <typeparam name="TMode"> The <see cref="EditorMode"/> type to switch to. </typeparam>
        public static void RequestEnterMode<TMode>() where TMode : EditorMode, new()
        {
            // Already in that mode, nothing to do.
            if (Current.GetType() == typeof(TMode))
            {
                return;
            }

            // Check for re-entrance
            ExitingMode = Current;
            try
            {
                Current.OnExitMode();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            ExitingMode = null;

            UnloadMode(Current);
            TMode mode;
            CurrentMode = mode = SwitchMode<TMode>();

            // Make sure overrides are only registered using this instance.
            CurrentForRegistration = Current;
            try
            {
                Current.OnEnterMode(EditorModeContext.CreateContext(mode));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            CurrentForRegistration = null;

            LoadMode(Current);
            ResetAllWindows();
        }

        /// <summary>
        /// Requests the editor to go back to default editor mode.
        /// </summary>
        /// <typeparam name="TMode"> The <see cref="EditorMode"/> type to switch from. </typeparam>
        public static void RequestExitMode<TMode>() where TMode : EditorMode
        {
            if (null != ExitingMode && ExitingMode.GetType() == typeof(TMode))
            {
                return;
            }

            // Can't exit a mode that is not current.
            if (Current.GetType() != typeof(TMode))
            {
                return;
            }

            RequestEnterMode<DefaultEditorMode>();
        }

        /// <summary>
        /// Requests to go back to the default mode, from any particular mode.
        /// </summary>
        public static void RequestDefaultMode()
        {
            RequestEnterMode<DefaultEditorMode>();
        }

        internal static void RegisterOverride<TMode, TOverride, TWindow>(TMode mode)
            where TMode : EditorMode
            where TOverride : EditorWindowOverride<TWindow>, new()
            where TWindow : EditorWindow
        {
            RegisterOverride(mode, typeof(TWindow), w => new TOverride()
            {
                Window = (TWindow)w,
                Root = w.GetRootElement<TMode>(true)
            });
        }

        internal static void RegisterOverride<TMode, TOverride>(TMode mode, Type editorWindowType)
            where TMode : EditorMode
            where TOverride : EditorWindowOverride<EditorWindow>, new()
        {
            RegisterOverride(mode, editorWindowType, w => new TOverride()
            {
                Window = w,
                Root = w.GetRootElement<TMode>(true)
            });
        }

        internal static void RegisterAsUnsupported<TMode>(TMode mode, Type editorWindowType)
            where TMode : EditorMode
        {
            RegisterOverride<TMode, UnsupportedWindowOverride>(mode, editorWindowType);
        }

        internal static void RegisterAsPassthrough<TMode>(TMode mode, Type editorWindowType)
            where TMode : EditorMode
        {
            RegisterOverride<TMode, PassthroughOverride>(mode, editorWindowType);
        }

        internal static void RegisterWindow(EditorWindow window)
        {
            if (s_RegisteredWindowsSet.Add(window))
            {
                s_RegisteredWindowsList.Add(window);
                OnLoadOverride(window);
            }
        }

        internal static void UnregisterWindow(EditorWindow window)
        {
            if (s_RegisteredWindowsSet.Remove(window))
            {
                OnUnloadOverride(window);
                s_RegisteredWindowsList.Remove(window);
            }
        }

        internal static VisualElement GetRootElement(EditorWindow window)
        {
            return GenericHelper.GetRoot(window);
        }

        internal static void OnBecameVisible(EditorWindow window)
        {
            s_VisibleWindowsSet.Add(window);
            CallOnOverride(window, o => o.OnBecameVisible());
        }

        internal static void OnFocus(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnFocus());
        }

        internal static void OnLostFocus(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnLostFocus());
        }

        internal static void Update(EditorWindow window)
        {
            CallOnOverride(window, o => o.Update());
        }

        internal static void ModifierKeysChanged(EditorWindow window)
        {
            CallOnOverride(window, o => o.ModifierKeysChanged());
        }

        internal static void OnBecameInvisible(EditorWindow window)
        {
            s_VisibleWindowsSet.Remove(window);
            CallOnOverride(window, o => o.OnBecameInvisible());
        }

        internal static void OnSelectionChanged(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnSelectionChanged());
        }

        internal static void OnProjectChange(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnProjectChange());
        }

        internal static void OnDidOpenScene(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnDidOpenScene());
        }

        internal static void OnInspectorUpdate(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnInspectorUpdate());
        }

        internal static void OnHierarchyChange(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnHierarchyChange());
        }

        internal static void OnResize(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnResize());
        }

        internal static void AddItemsToMenu(EditorWindow window, GenericMenu menu)
        {
            IEditorWindowOverride @override;
            IHasCustomMenu menuProvider;
            if (s_Overrides.TryGetValue(window, out @override))
            {
                menuProvider = @override as IHasCustomMenu;
            }
            else
            {
                menuProvider = window as IHasCustomMenu;
            }
            if (null != menuProvider)
            {
                menuProvider.AddItemsToMenu(menu);
            }
        }

        internal static bool ShouldInvokeOnGUI(EditorWindow window)
        {
            IEditorWindowOverride @override;
            return !s_Overrides.TryGetValue(window, out @override) || @override.InvokeOnGUIEnabled;
        }

        private static void RegisterOverride<TMode>(TMode mode, Type editorWindowType, Func<EditorWindow, IEditorWindowOverride> createOverride)
        {
            if (null == editorWindowType)
            {
                throw new ArgumentException("The target window type to override cannot be null.");
            }

            if (!editorWindowType.IsSubclassOf(typeof(EditorWindow)))
            {
                throw new ArgumentException($"The target window type to override must derive from `{typeof(EditorWindow).Name}`. Type provided: {editorWindowType.FullName}");
            }

            if (null == mode)
            {
                throw new ArgumentException($"Cannot register an override of an {typeof(EditorWindow).Name} on a null mode.");
            }

            if (!mode.Equals(CurrentForRegistration))
            {
                throw new ArgumentException($"Registering an override for an {nameof(EditorWindow)} can only be done from the {nameof(EditorMode.OnEnterMode)} method of an {nameof(EditorMode)} class, which is called automatically by the {nameof(EditorModes)}.");
            }

            s_OverrideFactory[editorWindowType] = createOverride;
        }

        private static void OnLoadOverride(EditorWindow window)
        {
            if (s_Overrides.ContainsKey(window))
            {
                return;
            }

            var windowType = window.GetType();
            Func<EditorWindow, IEditorWindowOverride> factory = null;

            // Explicit override
            if (s_OverrideFactory.TryGetValue(windowType, out factory))
            {
                ResetWindow(window, factory(window));
                return;
            }

            // Fallback overrides
            if (Current.OverrideMode == EditorOverrideMode.UnsupportedByDefault)
            {
                if (IsPassthroughForCurrentMode(windowType) &&
                    TryRegisterInternal(windowType, out factory, GenericHelper.RegisterAsPassthrough))
                {
                    ResetWindow(window, factory(window));
                    return;
                }

                if (CanBeUnsupported(windowType) &&
                    TryRegisterInternal(windowType, out factory, GenericHelper.RegisterAsUnsupported))
                {
                    ResetWindow(window, factory(window));
                    return;
                }
            }
            else if (Current.OverrideMode == EditorOverrideMode.PassthroughByDefault)
            {
                // Nothing to do.
            }
        }

        private static bool TryRegisterInternal(Type windowType, out Func<EditorWindow, IEditorWindowOverride> factory, Action<EditorMode, Type> del)
        {
            CurrentForRegistration = Current;
            del(Current, windowType);
            CurrentForRegistration = null;
            return s_OverrideFactory.TryGetValue(windowType, out factory);
        }

        private static bool IsPassthroughForCurrentMode(Type windowType)
        {
            return windowType
                .GetCustomAttributes(true)
                .OfType<CustomPassthroughAttribute>()
                .Any(attribute => attribute.EditorModeType == Current.GetType());
        }

        private static bool CanBeUnsupported(Type windowType)
        {
            return !windowType
                .GetCustomAttributes(true)
                .Any(attribute => attribute.GetType() == typeof(CannotBeUnsupportedAttribute));
        }

        private static void ResetWindow(EditorWindow window, IEditorWindowOverride @override)
        {
            s_Overrides.Add(window, @override);

            CallOnOverride(window, o =>
            {
                o.OnEnable();
                o.OnSwitchedToOverride();
            });

            if (s_VisibleWindowsSet.Contains(window) && null != window.m_Parent)
            {
                window.m_Parent.ResetActiveView();
            }
        }

        private static void OnUnloadOverride(EditorWindow window)
        {
            CallOnOverride(window, o => o.OnDisable());
            s_Overrides.Remove(window);
            GenericHelper.RemoveOverride(window);
        }

        private static void CallOnOverride(EditorWindow window, Action<IEditorWindowOverride> del)
        {
            if (!window || window == null)
            {
                return;
            }

            IEditorWindowOverride @override;
            if (s_Overrides.TryGetValue(window, out @override))
            {
                try
                {
                    del(@override);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static TMode SwitchMode<TMode>() where TMode : EditorMode, new()
        {
            // Clear old state.
            s_OverrideFactory.Clear();
            s_Overrides.Clear();

            // Set new state.
            GenericHelper = new ModeGenericHelper<TMode>();
            return new TMode();
        }

        private static VisualElement GetRootElement<TMode>(EditorWindow window) where TMode : EditorMode
        {
            IEditorWindowOverride @override;
            if (s_Overrides.TryGetValue(window, out @override) && @override is PassthroughOverride)
            {
                return window.GetRootElement<DefaultEditorMode>(false);
            }

            return window.GetRootElement<TMode>(false);
        }

        private static void LoadMode(EditorMode mode)
        {
            foreach (var window in new List<EditorWindow>(s_RegisteredWindowsList))
            {
                // Assign and update margins from the host view
                if (s_VisibleWindowsSet.Contains(window))
                {
                    var root = GenericHelper.GetRoot(window);
                    var parent = window.m_Parent;
                    if (!parent.visualTree.Contains(root))
                    {
                        parent.visualTree.Add(root);
                    }
                    parent.UpdateMargins(window);
                }
                OnLoadOverride(window);
            }
        }

        private static void UnloadMode(EditorMode mode)
        {
            foreach (var window in new List<EditorWindow>(s_RegisteredWindowsList))
            {
                if (s_VisibleWindowsSet.Contains(window))
                {
                    var root = GenericHelper.GetRoot(window);
                    var parent = window.m_Parent;
                    if (parent.visualTree.Contains(root))
                    {
                        parent.visualTree.shadow.Remove(root);
                    }
                }

                CallOnOverride(window, o => o.OnDisable());
                GenericHelper.RemoveOverride(window);
            }
        }

        private static void ResetAllWindows()
        {
            foreach (var window in new List<EditorWindow>(s_VisibleWindowsSet))
            {
                if (null != window.m_Parent)
                {
                    window.m_Parent.ResetActiveView();
                }
            }
        }
    }
}
