// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    // No interference from OverlayCanvas. This overlay completely controls when it is visible or hidden.
    interface IControlVisibility
    {
    }

    // Overlay is only displayed in the active scene view, when visible is true.
    public interface ITransientOverlay
    {
        bool visible { get; }
    }

    abstract class TransientSceneViewOverlay : IMGUIOverlay, ITransientOverlay
    {
        public abstract bool visible { get; }
        // Used by SRP https://github.com/Unity-Technologies/Graphics/commit/9e7999d30a1a5189ab9d30e5341a2e48962db453
        // Remove once VFX overlays are using ITransientOverlay
        internal virtual bool ShouldDisplay() => visible;
    }

    public abstract class IMGUIOverlay : Overlay
    {
        internal IMGUIContainer drawingContainer { get; private set; }

        public sealed override VisualElement CreatePanelContent()
        {
            rootVisualElement.pickingMode = PickingMode.Position;
            var container = new IMGUIContainer();
            container.onGUIHandler = () => OnPanelGUIHandler(container);
            return container;
        }

        void OnPanelGUIHandler(IMGUIContainer container)
        {
            if (!displayed)
                return;

            drawingContainer = container;
            OnGUI();
            drawingContainer = null;

            if (Event.current.isMouse)
                Event.current.Use();
        }

        public abstract void OnGUI();
    }
}
