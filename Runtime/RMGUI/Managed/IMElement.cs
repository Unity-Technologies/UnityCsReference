// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    internal interface IOnGUIHandler : IRecyclable
    {
        int id { get; }

        // return true to stop event propagation
        bool OnGUI(Event evt);
        void GenerateControlID();
    }

    abstract class IMElement : VisualElement, IOnGUIHandler
    {
        public FocusType focusType { get; set; }

        // Accesses this element's control ID for compatiblity with IMGUI.
        // The control ID is generated when the VisualElement is created or when it's reused in a GUI pass.
        // This value can be NonInteractiveControlID for non-interactive UIElements.
        public int id { get; protected set; }

        // IMElements have their GUIStyle set directly, and don't use RMGUI Style Sheet at the moment
        public new GUIStyle style
        {
            get { return m_GUIStyle; }
            set
            {
                m_GUIStyle = value;
            }
        }

        public new Rect position { get; set; }

        public const int NonInteractiveControlID = 0;

        protected IMElement() :
            base()
        {
            this.focusType = FocusType.Passive;
            this.id = NonInteractiveControlID;
        }

        #region IRecyclable Implementation
        public bool isTrashed { get; set; }

        public virtual void OnTrash()
        {
        }

        public virtual void OnReuse()
        {
            position = new Rect(0, 0, 0, 0);
            enabled = true;
            id = NonInteractiveControlID;
        }

        #endregion

        public virtual bool OnGUI(Event evt)
        {
            bool used = false;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    used = DoMouseDown(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.MouseMove:
                    used = DoMouseMove(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.MouseDrag:
                    used = DoMouseDrag(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.MouseUp:
                    used = DoMouseUp(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.KeyDown:
                    used = DoKeyDown(new KeyboardEventArgs(evt.character, evt.keyCode, evt.modifiers));
                    break;

                case EventType.KeyUp:
                    used = DoKeyUp(new KeyboardEventArgs(evt.character, evt.keyCode, evt.modifiers));
                    break;

                case EventType.Repaint:
                    DoRepaint(new StylePainter(evt.mousePosition));
                    break;

                case EventType.DragUpdated:
                    used = DoDragUpdated(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;
            }

            if (used)
            {
                evt.Use();
            }

            return used;
        }

        public void AssignControlID(int id)
        {
            this.id = id;
        }

        public void GenerateControlID()
        {
            id = DoGenerateControlID();
        }

        // VisualElement subclasses must implement this by returning a valide unique ID in this GUI pass.
        // Alternatively, return NonInteractiveControlID for controls that the user can't interact with.
        // TODO remove this from RMGUI, only there as a transition
        protected abstract int DoGenerateControlID();

        protected virtual bool DoMouseDown(MouseEventArgs args)
        {
            return false;
        }

        protected virtual bool DoMouseMove(MouseEventArgs args)
        {
            return false;
        }

        protected virtual bool DoMouseUp(MouseEventArgs args)
        {
            return false;
        }

        protected virtual bool DoKeyDown(KeyboardEventArgs args)
        {
            return false;
        }

        protected virtual bool DoKeyUp(KeyboardEventArgs args)
        {
            return false;
        }

        protected virtual bool DoMouseDrag(MouseEventArgs args)
        {
            return false;
        }

        protected virtual bool DoDragUpdated(MouseEventArgs args)
        {
            return false;
        }
    }
}
