namespace UnityEngine.UIElements
{
    public abstract class ContextualMenuManager
    {
        public abstract void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler);

        public void DisplayMenu(EventBase triggerEvent, IEventHandler target)
        {
            DropdownMenu menu = new DropdownMenu();

            using (ContextualMenuPopulateEvent cme = ContextualMenuPopulateEvent.GetPooled(triggerEvent, menu, target, this))
            {
                target?.SendEvent(cme);
            }
        }

        protected internal abstract void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent);
    }
}
