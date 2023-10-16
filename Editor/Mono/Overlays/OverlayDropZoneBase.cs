// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    abstract class OverlayDropZoneBase : VisualElement
    {
        public const string className = "unity-overlay-drop-zone";
        const string k_HoveredState = "unity-overlay-drop-zone--hovered";
        const string k_HiddenState = "unity-overlay-drop-zone--hidden";

        protected OverlayInsertIndicator insertIndicator { get; private set; }
        protected OverlayContainer originContainer { get; private set; }
        protected OverlayContainerSection originSection { get; private set; }

        public abstract OverlayContainer targetContainer { get; }
        public abstract OverlayContainerSection targetSection { get; }

        public virtual bool CanAcceptTarget(Overlay overlay) { return true; }
        public abstract void DropOverlay(Overlay overlay);

        protected OverlayDropZoneBase()
        {
            AddToClassList(className);

            pickingMode = PickingMode.Ignore;
            visible = false;
        }

        public void Setup(OverlayInsertIndicator insertIndicator, OverlayContainer originContainer, OverlayContainerSection originSection)
        {
            this.insertIndicator = insertIndicator;
            this.originContainer = originContainer;
            this.originSection = originSection;
        }

        public void Cleanup()
        {
            insertIndicator = null;
            originContainer = null;
        }

        public virtual void Activate(Overlay draggedOverlay)
        {
            var shouldEnable = ShouldEnable(draggedOverlay);
            pickingMode = shouldEnable ? PickingMode.Position : PickingMode.Ignore;
            visible = shouldEnable;
        }

        protected virtual bool ShouldEnable(Overlay draggedOverlay) { return true; }

        public virtual void BeginHover()
        {
            EnableInClassList(k_HoveredState, true);
        }

        public virtual void EndHover()
        {
            EnableInClassList(k_HoveredState, false);
        }

        public virtual void UpdateHover(OverlayDropZoneBase hovered)
        {
        }

        public bool HasSameTargetContainer(OverlayDropZoneBase dropZone)
        {
            if (dropZone == null)
                return false;

            return dropZone.targetContainer == targetContainer && dropZone.targetSection == targetSection;
        }

        protected void SetHidden(bool hidden)
        {
            EnableInClassList(k_HiddenState, hidden);
        }

        public virtual void Deactivate(Overlay draggedOverlay)
        {
            visible = false;
            pickingMode = PickingMode.Ignore; 
        }
    }
}
