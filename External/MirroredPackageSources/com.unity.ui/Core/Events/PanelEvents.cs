namespace UnityEngine.UIElements
{
    public interface IPanelChangedEvent
    {
    }

    public abstract class PanelChangedEventBase<T> : EventBase<T>, IPanelChangedEvent where T : PanelChangedEventBase<T>, new()
    {
        public IPanel originPanel { get; private set; }
        public IPanel destinationPanel { get; private set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            originPanel = null;
            destinationPanel = null;
        }

        public static T GetPooled(IPanel originPanel, IPanel destinationPanel)
        {
            T e = GetPooled();
            e.originPanel = originPanel;
            e.destinationPanel = destinationPanel;
            return e;
        }

        protected PanelChangedEventBase()
        {
            LocalInit();
        }
    }

    public class AttachToPanelEvent : PanelChangedEventBase<AttachToPanelEvent>
    {
    }

    public class DetachFromPanelEvent : PanelChangedEventBase<DetachFromPanelEvent>
    {
    }
}
