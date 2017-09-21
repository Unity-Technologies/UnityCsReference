// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class Focusable : CallbackEventHandler
    {
        protected Focusable()
        {
            m_FocusIndex = 0;
        }

        public abstract FocusController focusController { get; }

        // See http://w3c.github.io/html/editing.html#the-tabindex-attribute
        private int m_FocusIndex;
        public int focusIndex
        {
            get { return m_FocusIndex; }
            set { m_FocusIndex = value; }
        }

        public virtual bool canGrabFocus
        {
            get { return m_FocusIndex >= 0; }
        }

        public virtual void Focus()
        {
            if (focusController != null)
            {
                focusController.SwitchFocus(canGrabFocus ? this : null);
            }
        }

        public void Blur()
        {
            if (focusController != null && focusController.focusedElement == this)
            {
                focusController.SwitchFocus(null);
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
            {
                MouseDownEvent mde = evt as MouseDownEvent;
                if (mde.button == (int)MouseButton.LeftMouse)
                {
                    Focus();
                }
            }

            if (focusController != null)
            {
                focusController.SwitchFocusOnEvent(evt);
            }
        }
    }

    public class FocusChangeDirection
    {
        static readonly FocusChangeDirection s_Unspecified = new FocusChangeDirection(-1);

        public static FocusChangeDirection unspecified
        {
            get { return s_Unspecified; }
        }

        static readonly FocusChangeDirection s_None = new FocusChangeDirection(0);

        public static FocusChangeDirection none
        {
            get { return s_None; }
        }

        protected static FocusChangeDirection lastValue { get { return s_None; } }

        int m_Value;

        protected FocusChangeDirection(int value)
        {
            m_Value = value;
        }

        public static implicit operator int(FocusChangeDirection fcd)
        {
            return fcd.m_Value;
        }
    }

    public interface IFocusRing
    {
        FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e);

        Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction);
    }

    public class FocusController
    {
        // https://w3c.github.io/uievents/#interface-focusevent

        public FocusController(IFocusRing focusRing)
        {
            this.focusRing = focusRing;
            focusedElement = null;
            imguiKeyboardControl = 0;
        }

        IFocusRing focusRing { get; set; }

        public Focusable focusedElement
        {
            get;
            private set;
        }

        static void AboutToReleaseFocus(Focusable focusable, Focusable willGiveFocusTo, FocusChangeDirection direction)
        {
            FocusOutEvent e = FocusOutEvent.GetPooled(focusable, willGiveFocusTo, direction);
            UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
            FocusOutEvent.ReleasePooled(e);
        }

        static void ReleaseFocus(Focusable focusable, Focusable willGiveFocusTo, FocusChangeDirection direction)
        {
            BlurEvent e = BlurEvent.GetPooled(focusable, willGiveFocusTo, direction);
            UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
            BlurEvent.ReleasePooled(e);
        }

        static void AboutToGrabFocus(Focusable focusable, Focusable willTakeFocusFrom, FocusChangeDirection direction)
        {
            FocusInEvent e = FocusInEvent.GetPooled(focusable, willTakeFocusFrom, direction);
            UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
            FocusInEvent.ReleasePooled(e);
        }

        static void GrabFocus(Focusable focusable, Focusable willTakeFocusFrom, FocusChangeDirection direction)
        {
            FocusEvent e = FocusEvent.GetPooled(focusable, willTakeFocusFrom, direction);
            UIElementsUtility.eventDispatcher.DispatchEvent(e, null);
            FocusEvent.ReleasePooled(e);
        }

        internal void SwitchFocus(Focusable newFocusedElement)
        {
            SwitchFocus(newFocusedElement, FocusChangeDirection.unspecified);
        }

        void SwitchFocus(Focusable newFocusedElement, FocusChangeDirection direction)
        {
            if (newFocusedElement == focusedElement)
            {
                return;
            }

            var oldFocusedElement = focusedElement;

            if (newFocusedElement == null || !newFocusedElement.canGrabFocus)
            {
                if (oldFocusedElement != null)
                {
                    AboutToReleaseFocus(oldFocusedElement, newFocusedElement, direction);
                    focusedElement = null;
                    ReleaseFocus(oldFocusedElement, newFocusedElement, direction);
                }
            }
            else if (newFocusedElement != oldFocusedElement)
            {
                if (oldFocusedElement != null)
                {
                    AboutToReleaseFocus(oldFocusedElement, newFocusedElement, direction);
                }

                AboutToGrabFocus(newFocusedElement, oldFocusedElement, direction);

                focusedElement = newFocusedElement;

                if (oldFocusedElement != null)
                {
                    ReleaseFocus(oldFocusedElement, newFocusedElement, direction);
                }

                GrabFocus(newFocusedElement, oldFocusedElement, direction);
            }
        }

        public void SwitchFocusOnEvent(EventBase e)
        {
            FocusChangeDirection direction = focusRing.GetFocusChangeDirection(focusedElement, e);
            if (direction != FocusChangeDirection.none)
            {
                Focusable f = focusRing.GetNextFocusable(focusedElement, direction);
                SwitchFocus(f, direction);
            }
        }

        internal int imguiKeyboardControl { get; set; }

        internal void SyncIMGUIFocus(IMGUIContainer imguiContainerHavingKeyboardControl)
        {
            // Make sure the focusController state matches GUIUtility.keyboardControl
            if (GUIUtility.keyboardControl != imguiKeyboardControl)
            {
                imguiKeyboardControl = GUIUtility.keyboardControl;

                if (GUIUtility.keyboardControl != 0)
                {
                    SwitchFocus(imguiContainerHavingKeyboardControl, FocusChangeDirection.unspecified);
                }
                else
                {
                    SwitchFocus(null, FocusChangeDirection.unspecified);
                }
            }
        }
    }
}
