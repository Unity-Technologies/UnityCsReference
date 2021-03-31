// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ExtensionManager
    {
        class EventDispatcher
        {
            private static readonly string k_ExtensionErrorMessage = L10n.Tr("[Package Manager Window] Package manager extension failed with error: {0}");

            private IWindow m_Window = null;

            // This is to keep track of the selection that happens during the initialization process
            private Tuple<IPackage, IPackageVersion> m_DelayedPackageSelection = null;

            // We keep a dictionary of objects so that we only create one instance for one type
            // and if a developer implements multiple interfaces with one class we won't create multiple instances
            private readonly Dictionary<Type, object> m_HandlerObjects = new Dictionary<Type, object>();

            private List<IWindowCreatedHandler> m_WindowCreatedHandlers;
            public virtual List<IWindowCreatedHandler> windowCreatedHandlers =>
                m_WindowCreatedHandlers ?? (m_WindowCreatedHandlers = CreateImplementedInstances<IWindowCreatedHandler>());

            private List<IWindowDestroyHandler> m_WindowDestroyHandlers;
            public virtual List<IWindowDestroyHandler> windowDestroyHandlers =>
                m_WindowDestroyHandlers ?? (m_WindowDestroyHandlers = CreateImplementedInstances<IWindowDestroyHandler>());

            private List<IPackageSelectionChangedHandler> m_PackageSelectionChangedHandlers;
            public virtual List<IPackageSelectionChangedHandler> packageSelectionChangedHandlers =>
                m_PackageSelectionChangedHandlers ?? (m_PackageSelectionChangedHandlers = CreateImplementedInstances<IPackageSelectionChangedHandler>());

            private List<T> CreateImplementedInstances<T>()
            {
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => p.IsClass && !p.IsAbstract && typeof(T).IsAssignableFrom(p));

                var result = new List<T>();
                foreach (var type in types)
                {
                    if (m_HandlerObjects.TryGetValue(type, out var value))
                        result.Add((T)value);
                    else
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(type);
                            m_HandlerObjects[type] = instance;
                            result.Add((T)instance);
                        }
                        catch (MissingMethodException e)
                        {
                            Debug.LogWarning($"[Package Manager Window] A default constructor for {type} is required for the package manager extension to function properly.\n{e.Message}");
                        }
                    }
                }
                return result;
            }

            public void SendWindowCreatedEvent(IWindow window)
            {
                m_Window = window;
                foreach (var extension in windowCreatedHandlers)
                {
                    try
                    {
                        var args = new WindowCreatedArgs { window = m_Window };
                        extension.OnWindowCreated(args);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(string.Format(k_ExtensionErrorMessage, exception));
                    }
                }

                // This is the delayed `OnPackageSelectionChanged` call during initialization. Check `OnPackageSelectionChanged` for more details.
                if (m_DelayedPackageSelection != null)
                {
                    SendPackageSelectionChangedEvent(m_DelayedPackageSelection.Item1, m_DelayedPackageSelection.Item2);
                    m_DelayedPackageSelection = null;
                }
            }

            public void SendWindowDestroyEvent()
            {
                if (m_Window == null)
                    return;

                foreach (var handler in windowDestroyHandlers)
                {
                    try
                    {
                        var args = new WindowDestroyArgs { window = m_Window };
                        handler.OnWindowDestroy(args);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(string.Format(k_ExtensionErrorMessage, exception));
                    }
                }
                m_Window = null;
            }

            public void SendPackageSelectionChangedEvent(IPackage package, IPackageVersion version)
            {
                // Due to the way the Package Manager window initializes, the first `OnPackageSelectionChanged` would happen before the `OnWindowCreated`
                // That means the first time `OnPackageSelectionChanged` is triggered, the window is not fully ready and `m_Window` is null
                // Hence we want to delay calling this first `OnPackageSelectionChanged` until the window is created in `OnWindowCreated`
                if (m_Window == null)
                {
                    m_DelayedPackageSelection = new Tuple<IPackage, IPackageVersion>(package, version);
                    return;
                }

                foreach (var extension in packageSelectionChangedHandlers)
                {
                    try
                    {
                        var args = new PackageSelectionArgs { package = package, packageVersion = version, window = m_Window };
                        extension.OnPackageSelectionChanged(args);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Package manager extension failed with error: {0}"), exception));
                    }
                }
            }
        }

        private EventDispatcher m_EventDispatcher = new EventDispatcher();
        public virtual void SendPackageSelectionChangedEvent(IPackage package, IPackageVersion version) => m_EventDispatcher.SendPackageSelectionChangedEvent(package, version);

        private IWindow m_Window = null;
        private VisualElement m_DetailsExtensionContainer;
        private VisualElement m_ToolbarExtensionContainer;

        private VisualElement m_PackageActionContainer;

        private List<DetailsExtension> m_DetailsExtensions = new List<DetailsExtension>();
        private List<PackageAction> m_PackageActions = new List<PackageAction>();

        private DropdownButton m_CollapsedPackageActions;

        private PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(PackageManagerPrefs packageManagerPrefs)
        {
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        public virtual void OnWindowCreated(IWindow window, VisualElement detailsExtensionContainer, VisualElement toolbarExtensionsContainer)
        {
            m_Window = window;
            m_DetailsExtensionContainer = detailsExtensionContainer;
            m_ToolbarExtensionContainer = toolbarExtensionsContainer;

            m_CollapsedPackageActions = new DropdownButton();
            m_CollapsedPackageActions.alwaysShowDropdown = true;
            m_CollapsedPackageActions.text = L10n.Tr("Extensions");
            m_CollapsedPackageActions.onBeforeShowDropdown += CollapsedPackageActionsOnBeforeShowDropdown;
            UIUtils.SetElementDisplay(m_CollapsedPackageActions, false);
            m_ToolbarExtensionContainer.Add(m_CollapsedPackageActions);

            m_PackageActionContainer = new VisualElement();
            m_PackageActionContainer.style.flexDirection = FlexDirection.Row;
            m_ToolbarExtensionContainer.Add(m_PackageActionContainer);

            m_ToolbarExtensionContainer.RegisterCallback<GeometryChangedEvent>(evt => RefreshPackageActionsBasedOnWidth());

            m_EventDispatcher.SendWindowCreatedEvent(window);
        }

        public virtual void OnWindowDestroy()
        {
            if (m_Window == null)
                return;

            m_EventDispatcher.SendWindowDestroyEvent();

            m_DetailsExtensionContainer.Clear();
            m_DetailsExtensions.Clear();

            m_PackageActionContainer.Clear();
            m_PackageActions.Clear();
            m_PackageActionContainer.RemoveFromHierarchy();
            m_CollapsedPackageActions.RemoveFromHierarchy();

            m_Window = null;
        }

        private void CollapsedPackageActionsOnBeforeShowDropdown()
        {
            var newDropdownMenu = new DropdownMenu();
            foreach (var extension in m_PackageActions.Where(a => a.visible))
            {
                var packageActionText = !string.IsNullOrEmpty(extension.text) ? extension.text : extension.tooltip;
                if (!extension.visibleDropdownItems.Any())
                    newDropdownMenu.AppendAction(packageActionText, a => { extension.action?.Invoke(m_Window.activeSelection); });
                else
                {
                    if (extension.action != null)
                        newDropdownMenu.AppendAction($"{packageActionText}/{packageActionText}", a => { extension.action?.Invoke(m_Window.activeSelection); });
                    foreach (var item in extension.visibleDropdownItems)
                        newDropdownMenu.AppendAction($"{packageActionText}/{item.text}", a => { item.action?.Invoke(m_Window.activeSelection); }, item.statusCallback);
                }
            }
            m_CollapsedPackageActions.menu = newDropdownMenu;
        }

        private void RefreshPackageActionsBasedOnWidth()
        {
            var childrenWidth = m_PackageActions.Sum(a => a.visible ? a.dropdownButton.estimatedWidth : 0.0f);
            UIUtils.SetElementDisplay(m_ToolbarExtensionContainer, childrenWidth != 0);
            var showCollapsedButton = childrenWidth > m_ToolbarExtensionContainer.rect.width;
            if (showCollapsedButton == UIUtils.IsElementVisible(m_CollapsedPackageActions))
                return;

            UIUtils.SetElementDisplay(m_CollapsedPackageActions, showCollapsedButton);
            UIUtils.SetElementDisplay(m_PackageActionContainer, !showCollapsedButton);
        }

        public DetailsExtension CreateDetailsExtension()
        {
            var result = new DetailsExtension(m_PackageManagerPrefs);
            result.onPriorityChanged += OnDetailsExtensionPriorityChanged;

            m_DetailsExtensions.Add(result);
            m_DetailsExtensionContainer.Add(result);
            OnDetailsExtensionPriorityChanged();
            return result;
        }

        private void OnDetailsExtensionPriorityChanged()
        {
            if (IsSorted(m_DetailsExtensions))
                return;
            m_DetailsExtensions.Sort(CompareExtensions);

            m_DetailsExtensionContainer.Clear();
            foreach (var extension in m_DetailsExtensions)
                m_DetailsExtensionContainer.Add(extension);
        }

        private PackageAction CreatePackageAction()
        {
            var result = new PackageAction(m_Window);
            result.onPriorityChanged += OnPackageActionPriorityChanged;
            result.onVisibleChanged += RefreshPackageActionsBasedOnWidth;

            m_PackageActions.Add(result);
            m_PackageActionContainer.Add(result.dropdownButton);
            OnPackageActionPriorityChanged();
            RefreshPackageActionsBasedOnWidth();
            return result;
        }

        public PackageAction CreatePackageActionButton()
        {
            return CreatePackageAction();
        }

        public PackageAction CreatePackageActionMenu()
        {
            var result = CreatePackageAction();
            result.dropdownButton.alwaysShowDropdown = true;
            return result;
        }

        private void OnPackageActionPriorityChanged()
        {
            if (IsSorted(m_PackageActions))
                return;
            m_PackageActions.Sort(CompareExtensions);

            m_PackageActionContainer.Clear();
            foreach (var extension in m_PackageActions)
                m_PackageActionContainer.Add(extension.dropdownButton);
        }

        public static int CompareExtensions(IExtension e1, IExtension e2)
        {
            return e1.priority - e2.priority;
        }

        private static bool IsSorted<T>(List<T> extensions) where T : IExtension
        {
            for (var i = 0; i < extensions.Count - 1; ++i)
                if (extensions[i + 1].priority < extensions[i].priority)
                    return false;
            return true;
        }
    }
}
