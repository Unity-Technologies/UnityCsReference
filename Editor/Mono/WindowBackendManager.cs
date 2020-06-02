// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal interface IWindowModel
    {
        Vector2 size { get; }

        EventInterests eventInterests { get; }

        Action onGUIHandler { get; }

        IWindowBackend windowBackend { get; set; }
    }

    internal interface IEditorWindowModel : IWindowModel
    {
        EditorWindow window { get; }

        RectOffset viewMargins { get; }
        bool notificationVisible { get; }

        Color playModeTintColor { get; }

        Action onSplitterGUIHandler { get; set; }

        IEditorWindowBackend editorWindowBackend { get; set; }
    }

    internal interface IWindowBackend
    {
        void OnCreate(IWindowModel model);
        void OnDestroy(IWindowModel model);

        bool GetTooltip(Vector2 windowMouseCoordinates, out string tooltip, out Rect screenRectPosition);

        object visualTree { get; }

        void SizeChanged();
        void EventInterestsChanged();
    }

    internal interface IEditorWindowBackend : IWindowBackend
    {
        void PlayModeTintColorChanged();
        void NotificationVisibilityChanged();
        void Focused();
        void Blurred();
        void OnRegisterWindow();
        void OnUnregisterWindow();
        void OnDisplayWindowMenu(GenericMenu menu);
        void ViewMarginsChanged();
    }

    internal interface IEditorWindowBackendSystem
    {
        IWindowBackend GetBackendForWindow(IWindowModel model);
        bool ValidateBackendCompatibility(IWindowBackend backend, IWindowModel model, ref bool isCompatible);
    }
    internal static class EditorWindowBackendManager
    {
        private static List<IEditorWindowBackendSystem> sRegisteredSystems = new List<IEditorWindowBackendSystem>();

        static internal void RegisterWindowSystem(IEditorWindowBackendSystem system)
        {
            sRegisteredSystems.Add(system);
        }

        static internal void UnregisterWindowSystem(IEditorWindowBackendSystem system)
        {
            sRegisteredSystems.Remove(system);
        }

        internal static bool IsBackendCompatible(IWindowBackend backend, IWindowModel model)
        {
            if (backend == null)
            {
                return false;
            }

            bool isCompatible = false;

            //Last registered system has higherpriority
            for (int i = sRegisteredSystems.Count - 1; i >= 0; --i)
            {
                if (sRegisteredSystems[i].ValidateBackendCompatibility(backend, model, ref isCompatible))
                {
                    return isCompatible;
                }
            }

            return true;
        }

        internal static IWindowBackend GetBackend(IWindowModel model)
        {
            //Last registered system has higherpriority
            for (int i = sRegisteredSystems.Count - 1; i >= 0; --i)
            {
                var backend = sRegisteredSystems[i].GetBackendForWindow(model);
                if (backend != null)
                {
                    return backend;
                }
            }

            return EditorUIService.instance.GetDefaultWindowBackend(model);
        }
    }
}
