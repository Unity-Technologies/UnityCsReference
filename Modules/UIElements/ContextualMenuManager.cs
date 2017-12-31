// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal abstract class ContextualMenuManager
    {
        public abstract void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler);

        public void DisplayMenu(EventBase triggerEvent, IEventHandler target)
        {
            bool doDisplay;
            ContextualMenu menu = new ContextualMenu();
            using (ContextualMenuPopulateEvent cme = ContextualMenuPopulateEvent.GetPooled(triggerEvent, menu, target))
            {
                UIElementsUtility.eventDispatcher.DispatchEvent(cme, null);
                doDisplay = cme.isDefaultPrevented == false;
            }

            if (doDisplay)
            {
                menu.PrepareForDisplay(triggerEvent);
                DoDisplayMenu(menu, triggerEvent);
            }
        }

        protected abstract void DoDisplayMenu(ContextualMenu menu, EventBase triggerEvent);
    }
}
