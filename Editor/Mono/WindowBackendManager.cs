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
        Action sizeChanged { get; set; }

        EventInterests eventInterests { get; }
        Action eventInterestsChanged { get; set; }

        Action onGUIHandler { get; }
    }

    internal interface IEditorWindowModel : IWindowModel
    {
        EditorWindow window { get; }

        Action onRegisterWindow { get; set; }
        Action onUnegisterWindow { get; set; }

        RectOffset viewMargins { get; }
        Action viewMarginsChanged { get; set; }

        bool notificationVisible { get; }

        Color playModeTintColor { get; }
        Action playModeTintColorChanged { get; set; }

        Action notificationVisibilityChanged { get; set; }

        Action focused { get; set; }
        Action blurred { get; set; }

        Action<GenericMenu> onDisplayWindowMenu { get; set; }

        Action rootVisualElementCreated { get; set;   }
    }

    internal interface IWindowBackend
    {
        void OnCreate(IWindowModel model);
        void OnDestroy(IWindowModel model);

        bool GetTooltip(Vector2 windowMouseCoordinates, out string tooltip, out Rect screenRectPosition);

        object visualTree { get; }
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

            return GetDefault(model);
        }

        static IWindowBackend GetDefault(IWindowModel model)
        {
            if (model is IEditorWindowModel)
                return new DefaultEditorWindowBackend();
            else
                return new DefaultWindowBackend();
        }
    }
}
